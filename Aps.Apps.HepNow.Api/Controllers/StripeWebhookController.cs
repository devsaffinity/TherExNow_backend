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

                // Stripe redelivers events on retry; skip if we've already processed this exact event.
                if (webhookLogRepository.Any(a => a.EventId == stripeEvent.Id))
                {
                    _logger.LogInformation("Stripe webhook event {EventId} already processed, skipping", stripeEvent.Id);
                    return Ok();
                }

                switch (stripeEvent.Type)
                {
                    /* Intentionally left disabled: invoice.paid (below) already creates the membership
                     * for a new subscription's first invoice, so wiring this up too would create a
                     * second, duplicate membership for every new subscriber. Re-enabling this needs a
                     * decision on which event should own membership creation, tested against Stripe
                     * before going live.
                    case EventTypes.CheckoutSessionCompleted:
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

                    case EventTypes.CustomerSubscriptionDeleted:
                    case EventTypes.CustomerSubscriptionUpdated:
                        {
                            var sub = (Stripe.Subscription)stripeEvent.Data.Object;
                            var isCanceled = sub.Status == "canceled" || sub.CancelAtPeriodEnd;
                            if (!isCanceled)
                                break;

                            var userIdForSub = userRepository.Where(a => a.StripeCustomerId == sub.CustomerId).Select(a => a.Id).FirstOrDefault();
                            if (userIdForSub == 0)
                            {
                                _logger.LogWarning("Stripe subscription {EventType} referenced unknown customer {CustomerId}", stripeEvent.Type, sub.CustomerId);
                                break;
                            }

                            var activeMembership = membershipRepository.Where(a => a.UserId == userIdForSub && a.EndDate > DateTime.UtcNow)
                                .OrderByDescending(a => a.Id).FirstOrDefault();
                            if (activeMembership != null)
                            {
                                activeMembership.EndDate = DateTime.UtcNow;
                                membershipRepository.Update(activeMembership);
                                _logger.LogInformation("Stripe subscription cancelled for user {UserId}", userIdForSub);
                            }
                            break;
                        }

                    case EventTypes.InvoicePaid:
                        var invoice = (Invoice)stripeEvent.Data.Object;
                        if (!(invoice.BillingReason == "subscription_create" || invoice.BillingReason == "subscription_cycle"))
                            break;

                        var userId = userRepository.Where(a => a.StripeCustomerId == invoice.CustomerId).Select(a => a.Id).FirstOrDefault();

                        if (userId == 0)
                        {
                            _logger.LogWarning("Stripe invoice.paid referenced unknown customer {CustomerId}, skipping membership update", invoice.CustomerId);
                            break;
                        }

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

                webhookLogRepository.Add(new WebhookLog { Provider = MembershipProvider.Stripe, Data = json, EventId = stripeEvent.Id });
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
