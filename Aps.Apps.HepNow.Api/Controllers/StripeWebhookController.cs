using Microsoft.AspNetCore.Mvc;
using Stripe;

namespace Aps.Apps.HepNow.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StripeWebhookController : ControllerBase
    {
        private readonly ILogger<StripeWebhookController> _logger;

        public StripeWebhookController(ILogger<StripeWebhookController> logger)
        {
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromServices] IConfiguration configuration, [FromServices]IMembershipRepository membershipRepository
            , [FromServices] IUserRepository userRepository, [FromServices] IWebhookLogRepository webhookLogRepository)
        {
            var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
            var endpointSecret = configuration.GetValue<string>("StripeSettings:WebhookSecret");

            try
            {
                var stripeEvent = EventUtility.ConstructEvent(
                    json,
                    Request.Headers["Stripe-Signature"],
                    endpointSecret
                );

                switch (stripeEvent.Type)
                {
                    /*case EventTypes.CheckoutSessionCompleted:
                        var session = stripeEvent.Data.Object as Stripe.Checkout.Session;
                        var userId = int.Parse(session.Metadata.GetValueOrDefault("userId"));
                        var planId = int.Parse(session.Metadata.GetValueOrDefault("planId"));
                        var expireDate = session.ExpiresAt;//DateTime.Parse(session.Metadata.GetValueOrDefault("expireDate"));
                        var price = Convert.ToDecimal(session.AmountTotal.Value / 100); //decimal.Parse(session.Metadata.GetValueOrDefault("price"));

                        var lastMembership = membershipRepository.Where(a => a.UserId == userId).OrderByDescending(a => a.Id).FirstOrDefault();
                        if (lastMembership != null)
                        {
                            if (lastMembership.EndDate > DateTime.UtcNow)
                            {
                                lastMembership.EndDate = DateTime.UtcNow;
                                membershipRepository.Update(lastMembership);
                            }
                        }
                        
                        membershipRepository.Add(new Memberships
                        {
                            Provider = MembershipProvider.Stripe,
                            StartDate = DateTime.UtcNow,
                            EndDate = expireDate,
                            PlanId = planId,
                            PaymentAmount = price,
                            UserId = userId,
                        });
                        break;*/

                    case EventTypes.CustomerSubscriptionCreated:
                        var subscription = stripeEvent.Data.Object as Stripe.Subscription;
                        break;

                    case EventTypes.InvoicePaid:
                        var invoice = (Invoice)stripeEvent.Data.Object;
                        if (!(invoice.BillingReason == "subscription_create" || invoice.BillingReason == "subscription_cycle"))
                            break;

                        var userId = userRepository.Where(a => a.StripeCustomerId == invoice.CustomerId).Select(a => a.Id).FirstOrDefault();

                        var lastMembership = membershipRepository.Where(a => a.UserId == userId).OrderByDescending(a => a.Id).FirstOrDefault();
                        if (lastMembership != null)
                        {
                            if (lastMembership.EndDate > DateTime.UtcNow)
                            {
                                lastMembership.EndDate = DateTime.UtcNow;
                                membershipRepository.Update(lastMembership);
                            }
                        }
                        var lines = invoice.Lines.FirstOrDefault();

                        membershipRepository.Add(new Memberships
                        {
                            Provider = MembershipProvider.Stripe,
                            StartDate = lines.Period.Start,
                            EndDate = lines.Period.End,
                            PlanId = int.Parse(lines.Metadata["planId"]),
                            PaymentAmount = Convert.ToDecimal(lines.Amount) / 100m,
                            UserId = userId,
                        });

                        _logger.LogInformation("Stripe invoice paid (renewal succeeded) for user {UserId}", userId);
                        break;

                    case EventTypes.InvoicePaymentFailed:
                        _logger.LogWarning("Stripe invoice payment failed (renewal failed)");
                        break;
                }

                webhookLogRepository.Add(new WebhookLog { Provider = MembershipProvider.Stripe, Data = json });
                return Ok();
            }
            catch (StripeException e)
            {
                _logger.LogError(e, "Error handling Stripe webhook");
                webhookLogRepository.Add(new WebhookLog { Provider = MembershipProvider.Stripe, Data = json, Exception = e.Message });
                return BadRequest();
            }
        }
    }
}
