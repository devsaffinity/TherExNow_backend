using Aps.CommonBack.Base.Repositories;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Text.Json;

namespace Aps.Apps.HepNow.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AppleWebhookController : ControllerBase
    {
        private readonly ILogger<AppleWebhookController> _logger;

        public AppleWebhookController(ILogger<AppleWebhookController> logger)
        {
            _logger = logger;
        }

        [HttpPost]
        public IActionResult Post([FromBody] AppleNotificationRequest request, [FromServices]IMembershipRepository repository, [FromServices] IPlanRepository planRepository
            , [FromServices] IWebhookLogRepository webhookLogRepository)
        {
            try
            {
                webhookLogRepository.Add(new WebhookLog { Provider = MembershipProvider.Apple, Data = request.signedPayload });

                var handler = new JwtSecurityTokenHandler();
                var jwt = handler.ReadJwtToken(request.signedPayload);
                var notificationType = jwt.Payload["notificationType"]?.ToString();
                if (!jwt.Payload.TryGetValue("data", out var dataObj))
                    return BadRequest("Missing data in payload");

                var dataJson = System.Text.Json.JsonSerializer.Serialize(dataObj);
                using var doc = JsonDocument.Parse(dataJson);
                var data = doc.RootElement;

                var originalTransactionId = data.GetProperty("originalTransactionId").GetString();
                var signedTransactionInfo = data.GetProperty("signedTransactionInfo").GetString();

                var txJwt = handler.ReadJwtToken(signedTransactionInfo);
                var txPayload = txJwt.Payload;

                var expiresDateMs = txPayload["expiresDate"]?.ToString();

                DateTime? expiresDate = null;
                if (!string.IsNullOrEmpty(expiresDateMs))
                {
                    expiresDate = DateTimeOffset
                        .FromUnixTimeMilliseconds(long.Parse(expiresDateMs))
                        .UtcDateTime;
                }

                if (notificationType == "DID_RENEW")
                {
                    var lastMembership = repository.Where(a => a.AppleTransactionId == originalTransactionId).OrderByDescending(a => a.Id).FirstOrDefault();

                    if (lastMembership != null)
                    {
                        if (lastMembership.EndDate > DateTime.UtcNow)
                        {
                            lastMembership.EndDate = DateTime.UtcNow;
                            repository.Update(lastMembership);
                        }

                        repository.Add(new Memberships
                        {
                            Provider = MembershipProvider.Apple,
                            StartDate = DateTime.UtcNow,
                            EndDate = expiresDate.Value, //lastMembership.IsMonthly ? DateTime.UtcNow.AddMonths(1) : DateTime.UtcNow.AddYears(1),
                            UserId = lastMembership.UserId,
                            PaymentAmount = lastMembership.PaymentAmount,
                            PlanId = planRepository.Where(a => a.PriceMonth > 0 || a.PriceYear > 0).Select(a => a.Id).FirstOrDefault(),
                            AppleTransactionId = originalTransactionId,
                        });
                    }
                }
                else if (notificationType == "CANCEL" || notificationType == "REFUND" || notificationType == "REVOKE" || notificationType == "EXPIRED")
                {
                    var lastMembership = repository.Where(a => a.AppleTransactionId == originalTransactionId && a.EndDate > DateTime.UtcNow).OrderByDescending(a => a.Id).FirstOrDefault();

                    if (lastMembership != null)
                    {
                        lastMembership.EndDate = DateTime.UtcNow;
                        repository.Update(lastMembership);
                    }
                }

                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling Apple webhook");
                return BadRequest();
            }
        }
    }

    public class AppleNotificationRequest
    {
        public string signedPayload { get; set; }
    }
}
