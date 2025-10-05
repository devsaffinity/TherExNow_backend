using Microsoft.Extensions.Options;
using Aps.Apps.HepNow.Models.Dtos;
using Stripe;
using Stripe.Checkout;

namespace Aps.Apps.HepNow.Services;

public class MembershipService : ServiceBase<Memberships, MembershipInput>, IMembershipService
{
    public MembershipService(IMembershipRepository repository, IPlanRepository planRepository
        , IUserRepository userRepository
        , IRoutineRepository routineRepository
        , IOptions<StripeSettings> stripeSettings, IConfiguration configuration) : base(repository)
    {
        this.repository = repository;
        this.planRepository = planRepository;
        this.userRepository = userRepository;
        this.routineRepository = routineRepository;
        this.stripeSettings = stripeSettings;
        this.configuration = configuration;
        StripeConfiguration.ApiKey = stripeSettings.Value.Secretkey;
    }

    #region props
    private IMembershipRepository repository;
    private readonly IPlanRepository planRepository;
    private readonly IUserRepository userRepository;
    private readonly IRoutineRepository routineRepository;
    private readonly IOptions<StripeSettings> stripeSettings;
    private readonly IConfiguration configuration;

    #endregion
    #region functions
    public override ResponseBase<Memberships> Add(MembershipInput input)
    {
        var plan = planRepository.Find(input.PlanId);
        if (plan == null)
            return CustomResponseStatus.PlanNotFound;

        var lastPlan = repository.Get(a => a.UserId == input.UserId && a.EndDate > DateTime.UtcNow);
        if (lastPlan != null)
        {
            lastPlan.EndDate = DateTime.UtcNow;
            repository.Update(lastPlan);
        }

        input.StartDate = DateTime.UtcNow;

        if (plan.PriceYear > 0 && plan.PriceYear == input.PaymentAmount)
            input.EndDate = input.StartDate.AddYears(1);
        else
            input.EndDate = input.StartDate.AddMonths(1);

        return base.Add(input);
    }

    public SingleResponseBase<Memberships> GetCurrentMembership(int userId)
    {
        var query = repository.Where(a => a.UserId == userId && a.StartDate < DateTime.UtcNow && a.EndDate > DateTime.UtcNow);
        return SingleResponseBase<Memberships>.Success(query);
    }

    public ResponseBase<bool> CheckMaxTryFreePlan(int userId, OptionsForMaxTryFreePlan optionsForMaxTryFreePlan)
    {
        if (repository.Any(a => a.UserId == userId && a.StartDate < DateTime.UtcNow && a.EndDate > DateTime.UtcNow && a.PaymentAmount > 0))
            return true;

        switch(optionsForMaxTryFreePlan)
        {
            case OptionsForMaxTryFreePlan.Email:
                return !userRepository.Any(r => r.Id == userId && r.SentEmailCount >= 1);

            case OptionsForMaxTryFreePlan.Sms:
                return !userRepository.Any(r => r.Id == userId && r.SentSmsCount >= 1);

            case OptionsForMaxTryFreePlan.Routine:
                return routineRepository.Where(r => r.UserId == userId).Count() < 2;

            default:
                return true;
        }
    }

    public ResponseBase AddFreePlan(int userId)
    {
        var freePlanId = planRepository.Where(p => p.PriceMonth == 0).Select(p => p.Id).FirstOrDefault();
        if (freePlanId <= 0)
            return ResponseStatus.NotFound;

        if (repository.Any(a => a.UserId == userId))
            return ResponseStatus.Success;

        Add(new MembershipInput
        {
            PlanId = freePlanId,
            UserId = userId,
            PaymentAmount = 0,
        });
        return ResponseStatus.Success;
    }

    public ResponseBase<IntentDto> CreateIntentForPayment(string email, double amount)
    {

        var options = new PaymentIntentCreateOptions
        {
            Amount = Convert.ToInt32(amount * 100),
            Currency = "usd",
            ReceiptEmail = email
        };
        var service = new PaymentIntentService();
        var intent = service.Create(options);

        var result = new IntentDto
        {
            ClientSecret = intent.ClientSecret,
            IntentId = intent.Id,
            PublishableKey = stripeSettings.Value.PublishableKey,
        };
        return ResponseBase<IntentDto>.Success(result);
    }

    public async Task<ResponseBase<string>> CreateSubscription(int planId, Users user, bool forYear)
    {
        if (user.StripeCustomerId == null)
        {
            if (user.Email == null)
                return ResponseStatus.NotEnoghData;

            var customerResult = CreateCustomer(user);

            if (!customerResult)
                return ResponseStatus.Failed;
        }

        // Find selected package by client
        var plan = await planRepository.FindAsync(planId);
        if (plan == null)
        {
            return ResponseStatus.NotFound;
        }

        var options = new SessionCreateOptions
        {
            Customer = user.StripeCustomerId,
            SuccessUrl = configuration.GetSection("SubscriptionSuccessUrl").Value,
            CancelUrl = configuration.GetSection("SubscriptionCancelUrl").Value,
            Mode = "subscription",

            Metadata = new Dictionary<string, string>()
            {
                { "userId", user.Id.ToString() },
                { "subscriptionPlanId", planId.ToString() },
            },

            SubscriptionData = new SessionSubscriptionDataOptions()
            {
                Metadata = new Dictionary<string, string>()
                {
                   { "userId", user.Id.ToString() },
                   { "subscriptionPlanId", planId.ToString() },
               }
            },

            LineItems = new List<SessionLineItemOptions>
            {
               new() 
               {
                 Price = forYear ? plan.PriceIdYear: plan.PriceIdMonth,
                 Quantity = 1,
               },
            },
        };

        try
        {
            // Create subscription
            var service = new SessionService();
            var session = await service.CreateAsync(options);

            return ResponseBase<string>.Success(session.Url);
        }
        catch
        {
            return ResponseStatus.Failed;
        }
    }

    public async Task<ResponseStatus> CancelSubscription(Users user)
    {
        var service = new SubscriptionService();
        var list = await service.ListAsync(new SubscriptionListOptions { Customer = user.StripeCustomerId });
        foreach (var item in list) 
            await service.CancelAsync(item.Id);
        return ResponseStatus.Success;
    }

    public virtual bool CreateCustomer(Users user)
    {
        var options = new CustomerCreateOptions
        {
            Email = user.Email,
            Name = user.FullName,
        };

        var service = new CustomerService();
        var customer = service.Create(options);

        if (customer?.Id != null)
        {
            user.StripeCustomerId = customer.Id;
            userRepository.Update(user);
        }

        return true;
    }
    #endregion
}
