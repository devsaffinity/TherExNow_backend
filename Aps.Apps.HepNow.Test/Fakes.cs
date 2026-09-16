using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using Newtonsoft.Json.Linq;
using Stripe;

namespace Aps.Apps.HepNow.Test
{
    /// <summary>Firebase Authentication without the network: every uid exists unless listed as missing.</summary>
    public sealed class FakeFirebaseAccounts : IFirebaseAccounts
    {
        private readonly object gate = new();

        public HashSet<string> Missing { get; } = new();
        public HashSet<string> FailOn { get; } = new();
        public List<string> Deleted { get; } = new();

        public Task<bool> ExistsAsync(string uid)
        {
            lock (gate)
                return Task.FromResult(!Missing.Contains(uid) && !Deleted.Contains(uid));
        }

        public Task DeleteAsync(string uid)
        {
            lock (gate)
            {
                if (FailOn.Contains(uid))
                    throw new InvalidOperationException("Firebase is unavailable");

                Deleted.Add(uid);
            }
            return Task.CompletedTask;
        }
    }

    public sealed class FakeMediaStorage : IMediaStorage
    {
        public bool CanDelete { get; set; } = true;
        public List<string> Deleted { get; } = new();

        public Task<int> DeleteAllVersionsAsync(string key)
        {
            lock (Deleted)
                Deleted.Add(key);
            return Task.FromResult(1);
        }
    }

    /// <summary>
    /// Stands in for api.stripe.com behind the real Stripe.net client: answers subscription list, search, cancel
    /// and customer delete from an in-memory set, and records every request. Nothing leaves the process.
    /// </summary>
    public sealed class FakeStripe : HttpMessageHandler
    {
        private readonly object gate = new();

        public List<FakeSubscription> Subscriptions { get; } = new();
        public List<string> Requests { get; } = new();
        public List<string> DeletedCustomers { get; } = new();

        /// <summary>When set, every request fails with a Stripe error, as during an outage or a revoked key.</summary>
        public bool Fail { get; set; }

        public IStripeClient Client(string apiKey = "sk_test_unit") =>
            new StripeClient(apiKey, httpClient: new SystemNetHttpClient(new HttpClient(this), 0, null, false));

        public List<string> Cancelled
        {
            get { lock (gate) return Requests.Where(r => r.StartsWith("DELETE /v1/subscriptions/")).Select(r => r.Substring("DELETE /v1/subscriptions/".Length)).ToList(); }
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var path = request.RequestUri!.AbsolutePath;
            var query = HttpUtility.ParseQueryString(request.RequestUri.Query);

            lock (gate)
            {
                Requests.Add($"{request.Method} {path}");

                if (Fail)
                    return Respond(HttpStatusCode.BadRequest, new JObject { ["error"] = new JObject { ["type"] = "invalid_request_error", ["code"] = "unit_failure", ["message"] = "Stripe is failing on purpose" } });

                if (request.Method == HttpMethod.Get && path == "/v1/subscriptions")
                {
                    var customer = query["customer"];
                    return Respond(HttpStatusCode.OK, List("list", Subscriptions.Where(s => s.CustomerId == customer), expandCustomer: false));
                }

                if (request.Method == HttpMethod.Get && path == "/v1/subscriptions/search")
                {
                    var userId = Regex.Match(query["query"] ?? "", @"metadata\['userId'\]:'(\d+)'").Groups[1].Value;
                    var expand = query.GetValues("expand[0]")?.Contains("data.customer") == true || query.GetValues("expand[]")?.Contains("data.customer") == true;
                    return Respond(HttpStatusCode.OK, List("search_result", Subscriptions.Where(s => s.Metadata.TryGetValue("userId", out var id) && id == userId), expand));
                }

                if (request.Method == HttpMethod.Delete && path.StartsWith("/v1/subscriptions/"))
                {
                    var subscription = Subscriptions.Single(s => s.Id == path.Substring("/v1/subscriptions/".Length));
                    subscription.Status = "canceled";
                    return Respond(HttpStatusCode.OK, subscription.ToJson(expandCustomer: false));
                }

                if (request.Method == HttpMethod.Delete && path.StartsWith("/v1/customers/"))
                {
                    var id = path.Substring("/v1/customers/".Length);
                    DeletedCustomers.Add(id);
                    return Respond(HttpStatusCode.OK, new JObject { ["id"] = id, ["object"] = "customer", ["deleted"] = true });
                }
            }

            return Respond(HttpStatusCode.NotFound, new JObject { ["error"] = new JObject { ["type"] = "invalid_request_error", ["message"] = $"FakeStripe has no route for {request.Method} {path}" } });
        }

        private static JObject List(string kind, IEnumerable<FakeSubscription> items, bool expandCustomer) => new()
        {
            ["object"] = kind,
            ["url"] = "/v1/subscriptions",
            ["has_more"] = false,
            ["next_page"] = null,
            ["data"] = new JArray(items.Select(s => s.ToJson(expandCustomer))),
        };

        private static Task<HttpResponseMessage> Respond(HttpStatusCode status, JObject body) =>
            Task.FromResult(new HttpResponseMessage(status) { Content = new StringContent(body.ToString(), Encoding.UTF8, "application/json") });
    }

    public sealed class FakeSubscription
    {
        public string Id { get; init; } = "";
        public string CustomerId { get; init; } = "";
        public string? CustomerEmail { get; init; }
        public string Status { get; set; } = "active";
        public Dictionary<string, string> Metadata { get; init; } = new();

        public JObject ToJson(bool expandCustomer) => new()
        {
            ["id"] = Id,
            ["object"] = "subscription",
            ["status"] = Status,
            ["customer"] = expandCustomer
                ? new JObject { ["id"] = CustomerId, ["object"] = "customer", ["email"] = CustomerEmail }
                : CustomerId,
            ["metadata"] = JObject.FromObject(Metadata),
        };
    }
}
