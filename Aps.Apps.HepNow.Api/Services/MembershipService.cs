using AppleReceiptVerifierNET;
using Aps.Apps.HepNow.Models.Dtos;
using Google.Apis.AndroidPublisher.v3;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Microsoft.Extensions.Options;
using Stripe;
using Stripe.Checkout;
using System.Text;

namespace Aps.Apps.HepNow.Services;

public class MembershipService : ServiceBase<Memberships, MembershipInput>, IMembershipService
{
    public MembershipService(IMembershipRepository repository, IPlanRepository planRepository
        , IUserRepository userRepository
        , IRoutineRepository routineRepository
        , IOptions<StripeSettings> stripeSettings, IConfiguration configuration
        , [Service] INotificationService notificationService) : base(repository)
    {
        this.repository = repository;
        this.planRepository = planRepository;
        this.userRepository = userRepository;
        this.routineRepository = routineRepository;
        this.stripeSettings = stripeSettings;
        this.configuration = configuration;
        this.notificationService = notificationService;
        StripeConfiguration.ApiKey = stripeSettings.Value.Secretkey;
    }

    #region props
    private IMembershipRepository repository;
    private readonly IPlanRepository planRepository;
    private readonly IUserRepository userRepository;
    private readonly IRoutineRepository routineRepository;
    private readonly IOptions<StripeSettings> stripeSettings;
    private readonly IConfiguration configuration;
    private readonly INotificationService notificationService;

    #endregion
    #region functions
    public override ResponseBase<Memberships> Add(MembershipInput input)
    {
        var plan = planRepository.Find(input.PlanId);
        if (plan == null)
            return CustomResponseStatus.PlanNotFound;

        var lastMembership = repository.Get(a => a.UserId == input.UserId && a.EndDate > DateTime.UtcNow);
        if (lastMembership != null)
        {
            lastMembership.EndDate = DateTime.UtcNow;
            repository.Update(lastMembership);
        }

        input.StartDate = DateTime.UtcNow;

        if (input.IsFree)
            input.EndDate = DateTime.UtcNow.AddYears(100);
        else if (plan.PriceYear > 0 && plan.PriceYear == input.PaymentAmount)
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

    public async Task<ResponseBase<bool>> CheckMaxTryFreePlan(int userId, NotificationType notificationType)
    {
        if (repository.Any(a => a.UserId == userId && a.StartDate < DateTime.UtcNow && a.EndDate > DateTime.UtcNow && a.PaymentAmount > 0))
            return true;

        var user = userRepository.Find(userId);
        int count;

        if (notificationType == NotificationType.MaxTryFreePlanForRoutine)
            count = routineRepository.Where(r => r.UserId == userId).Count();
        else
            count = user.SentEmailCount + user.SentSmsCount + user.PrintCount + user.QrCodeCount;

        if (count >= 1 && !repository.Any<Notification>(a => a.UserId == userId && a.NotificationType == notificationType))
        {
            NotificationInput notificationInput = new()
            {
                NotificationType = notificationType,
                UserId = user.Id,
            };
            await notificationService.Send(notificationInput);
        }

        return count < 2;
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
            IsFree = true,
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

    public async Task<ResponseBase<string>> CreateSubscription(Users user, bool forYear, string successUrl, string cancelUrl)
    {
        if (user.StripeCustomerId == null)
        {
            if (user.Email == null)
                return ResponseStatus.NotEnoghData;

            var customerResult = CreateCustomer(user.Id);

            if (!customerResult)
                return ResponseStatus.Failed;

            await Task.Delay(1000); //prevent duplicate customer
        }

        // Find selected package by client
        var plan = forYear ? planRepository.Where(a => a.PriceIdYear != null && a.PriceYear > 0).FirstOrDefault()
                           : planRepository.Where(a => a.PriceIdMonth != null && a.PriceMonth > 0).FirstOrDefault();
        if (plan == null)
        {
            return ResponseStatus.NotFound;
        }

        var options = new SessionCreateOptions
        {
            PaymentMethodTypes = new List<string>
            {
                "card"
            },
            Customer = user.StripeCustomerId,
            SuccessUrl = successUrl,// + "?session_id={CHECKOUT_SESSION_ID}",
            CancelUrl = cancelUrl,
            Mode = "subscription",

            Metadata = new Dictionary<string, string>()
            {
                { "userId", user.Id.ToString() },
                { "planId", plan.Id.ToString() },
                { "expireDate", forYear ? DateTime.UtcNow.AddYears(1).ToString() : DateTime.UtcNow.AddMonths(1).ToString() },
                { "price", forYear ? plan.PriceYear.ToString() : plan.PriceMonth.ToString() },
            },

            SubscriptionData = new SessionSubscriptionDataOptions()
            {
                Metadata = new Dictionary<string, string>()
                {
                   { "userId", user.Id.ToString() },
                   { "planId", plan.Id.ToString() },
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

        var service = new SessionService();
        var session = await service.CreateAsync(options);

        return ResponseBase<string>.Success(session.Url);
    }

    public async Task<ResponseStatus> CancelSubscription(Users user)
    {
        var service = new SubscriptionService();
        var list = await service.ListAsync(new SubscriptionListOptions { Customer = user.StripeCustomerId });
        foreach (var item in list)
            if (item.Status == "active")
                await service.CancelAsync(item.Id);

        var lastMembership = repository.Where(a => a.UserId == user.Id && a.EndDate > DateTime.UtcNow).OrderByDescending(a => a.Id).FirstOrDefault();

        if (lastMembership != null)
        {
            lastMembership.EndDate = DateTime.UtcNow;
            repository.Update(lastMembership);
        }

        return ResponseStatus.Success;
    }

    public virtual bool CreateCustomer(int userId)
    {
        var user = userRepository.Find(userId);
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

    public async Task<ResponseBase<Memberships>> VerifyReceiptApple(int userId, string base64Receipt)
    {
        var verifierOptions = new AppleReceiptVerifierOptions()
        {
            AppSecret = configuration.GetValue<string>("AppleSharedSecret"),
            // Set to 'false' in production; only 'true' if you want to support sandbox receipts in your production environment
            AcceptTestEnvironmentReceipts = true,
        };

        var verifier = new AppleReceiptVerifier(verifierOptions, new HttpClient());
        // The second parameter 'checkSubscriptionStatus' is important for auto-renewable subscriptions
        var response = await verifier.VerifyReceiptAsync(base64Receipt);

        if (!response.IsValid)
        {
            verifierOptions = new AppleReceiptVerifierOptions()
            {
                AppSecret = configuration.GetValue<string>("AppleSharedSecret"),
                AcceptTestEnvironmentReceipts = false,
            };

            verifier = new AppleReceiptVerifier(verifierOptions, new HttpClient());
            response = await verifier.VerifyReceiptAsync(base64Receipt);
            if (!response.IsValid)
                return ResponseStatus.Failed;
        }

        // Receipt is valid. Check response.LatestReceiptInfo for subscription status,
        // or response.Receipt for purchase details.

        var lastMembership = repository.Get(a => a.UserId == userId && a.EndDate > DateTime.UtcNow);
        if (lastMembership != null)
        {
            lastMembership.EndDate = DateTime.UtcNow;
            repository.Update(lastMembership);
        }

        var plan = planRepository.Where(a => a.PriceMonth > 0 || a.PriceYear > 0).FirstOrDefault();
        var isMonthly = response.LatestReceiptInfo.FirstOrDefault().ProductId.ToLower().Contains("month");
        var originalTransactionId = response.LatestReceiptInfo.FirstOrDefault().OriginalTransactionId.ToString();
        if (repository.Any(a => a.AppleTransactionId == originalTransactionId))
            return ResponseStatus.AlreadyExists;

        return new(repository.Add(new Memberships
        {
            Provider = MembershipProvider.Apple,
            StartDate = DateTime.UtcNow,
            EndDate = response.LatestReceiptInfo.FirstOrDefault().ExpiresDate.UtcDateTime,
            UserId = userId,
            PlanId = plan.Id,
            AppleTransactionId = originalTransactionId,
            IsMonthly = isMonthly,
            PaymentAmount = isMonthly ? plan.PriceMonth.GetValueOrDefault() : plan.PriceYear.GetValueOrDefault(),
        }));
    }

    public async Task<ResponseBase<Memberships>> VerifyReceiptGoogle(int userId, string packageName, string productId, string purchaseToken)
    {
        var credential = GoogleCredential
            .FromFile("googleserviceaccount.json")
            .CreateScoped(AndroidPublisherService.Scope.Androidpublisher);

        var service = new AndroidPublisherService(new BaseClientService.Initializer()
        {
            HttpClientInitializer = credential,
            ApplicationName = "TherEXNow"
        });

        //var requestTemp = service.Purchases.Subscriptions.Get(packageName, productId, purchaseToken);
        //var s = await requestTemp.ExecuteAsync();

        var request = service.Purchases.Subscriptionsv2.Get(packageName, purchaseToken);
        var response = await request.ExecuteAsync();
        //DateTime? expireDate = DateTime.Now; //response.ExpiryTimeMillis > 0 ? DateTimeOffset.FromUnixTimeMilliseconds(response.ExpiryTimeMillis.GetValueOrDefault()).Date : null;
        //response.LineItems.FirstOrDefault().OfferDetails.BasePlanId == "monthly"
        //response.SubscriptionState == "SUBSCRIPTION_STATE_ACTIVE"
        if (response.SubscriptionState == "SUBSCRIPTION_STATE_ACTIVE")
        {
            var acknowledge = service.Purchases.Subscriptions.Acknowledge(new Google.Apis.AndroidPublisher.v3.Data.SubscriptionPurchasesAcknowledgeRequest
            {
                DeveloperPayload = "your-developer-payload" // Optional, if you want to add additional data
            }, packageName, productId, purchaseToken);

            var lastMembership = repository.Get(a => a.UserId == userId && a.EndDate > DateTime.UtcNow);
            if (lastMembership != null)
            {
                lastMembership.EndDate = DateTime.UtcNow;
                repository.Update(lastMembership);
            }

            return repository.Add(new Memberships
            {
                Provider = MembershipProvider.Google,
                StartDate = DateTime.UtcNow,
                EndDate = response.LineItems.FirstOrDefault().OfferDetails.BasePlanId.Contains("monthly") ? DateTime.UtcNow.AddMonths(1) : DateTime.UtcNow.AddYears(1),
                UserId = userId,
                PlanId = planRepository.Where(a => a.PriceMonth > 0 || a.PriceYear > 0).Select(a => a.Id).FirstOrDefault(),
                PurchaseToken = purchaseToken,
                //PaymentAmount = response.PriceAmountMicros.Value / 1000000,
                PaymentAmount = planRepository.Where(a => a.PriceMonth > 0 || a.PriceYear > 0).Select(a => a.PriceMonth).FirstOrDefault().Value,
            });
            //return ResponseStatus.Success;
        }
        else
        {
            return ResponseStatus.Failed;
        }
    }
    #endregion
}