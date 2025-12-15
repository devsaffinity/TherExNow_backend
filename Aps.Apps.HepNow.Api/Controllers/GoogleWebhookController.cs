using Aps.Apps.HepNow.Repositories;
using Aps.CommonBack.Base.Repositories;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace Aps.Apps.HepNow.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class GoogleWebhookController : ControllerBase
    {
        [HttpPost]
        public IActionResult Post([FromBody] JsonElement pubSubMessage, [FromServices] IMembershipRepository repository, [FromServices] IPlanRepository planRepository
            , [FromServices] IWebhookLogRepository webhookLogRepository)
        {
            var messageBase64 = pubSubMessage.GetProperty("message").GetProperty("data").GetString();
            var messageJson = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(messageBase64));
            var notification = System.Text.Json.JsonSerializer.Deserialize<GoogleNotification>(messageJson);

            webhookLogRepository.Add(new WebhookLog { Provider = MembershipProvider.Google, Data = messageJson });
            switch (notification.SubscriptionNotification?.NotificationType)
            {
                case 1: // SUBSCRIPTION_RECOVERED
                case 2: // SUBSCRIPTION_RENEWED
                //case 4: //SUBSCRIPTION_PURCHASED
                    var lastMembership = repository.Where(a => a.PurchaseToken == notification.SubscriptionNotification.PurchaseToken).OrderByDescending(a => a.Id).FirstOrDefault();

                    if (lastMembership != null)
                    {
                        if (lastMembership.EndDate > DateTime.UtcNow)
                        {
                            lastMembership.EndDate = DateTime.UtcNow;
                            repository.Update(lastMembership);
                        }

                        repository.Add(new Memberships
                        {
                            Provider = MembershipProvider.Google,
                            StartDate = DateTime.UtcNow,
                            EndDate = DateTimeOffset.FromUnixTimeMilliseconds(long.Parse(notification.SubscriptionNotification.ExpiryTimeMillis)).UtcDateTime,
                            UserId = lastMembership.UserId,
                            PaymentAmount = lastMembership.PaymentAmount,
                            PlanId = planRepository.Where(a => a.PriceMonth > 0 || a.PriceYear > 0).Select(a => a.Id).FirstOrDefault(),
                        });
                    }
                    break;
                case 3: // SUBSCRIPTION_CANCELED
                    lastMembership = repository.Where(a => a.PurchaseToken == notification.SubscriptionNotification.PurchaseToken && a.EndDate > DateTime.UtcNow).OrderByDescending(a => a.Id).FirstOrDefault();

                    if (lastMembership != null)
                    {
                        lastMembership.EndDate = DateTime.UtcNow;
                        repository.Update(lastMembership);
                    }
                    break;
            }

            return Ok();
        }
    }

    public class GoogleNotification
    {
        public SubscriptionNotification SubscriptionNotification { get; set; }
    }

    public class SubscriptionNotification
    {
        public int NotificationType { get; set; }
        public string PurchaseToken { get; set; }
        public string SubscriptionId { get; set; }
        public string ExpiryTimeMillis { get; set; }
    }
}
