using Aps.Apps.HepNow.Generics.Responses;

namespace Aps.Apps.HepNow.Test.ServiceTest
{
    public class MembershipTest: TestBase
    {
        private readonly IMembershipService memberhsipService;
        private readonly IUserService userService;

        public MembershipTest(IMembershipService membershipService, IUserService userService)
        {
            this.memberhsipService = membershipService;
            this.userService = userService;
        }

        [Theory]
        [InlineData(30, DatabaseFixture.FirstPlanId)]
        public void Create(decimal price, int planId)
        {
            var user = new Users();
            var userResponse = userService.Add(user);

            var input = new MembershipInput
            {
                PlanId = planId,
                PaymentAmount = price,
                UserId = userResponse.Result.Id, 
            };

            var response = memberhsipService.Add(input);
            Assert.True(response.Status == ResponseStatus.Success && response.Result.Id > 0);
        }

        [Theory]
        [InlineData(DatabaseFixture.AdminUserId)]
        public void GetCurrentMembership(int userId)
        {
            var response = memberhsipService.GetCurrentMembership(userId);
            Assert.True(response.Status == ResponseStatus.Success && response.Result.Id > 0);
        }

        [Fact]
        public async Task CancelSubscription_WithoutAStripeCustomer_NeverAsksStripeForSubscriptions()
        {
            var stripe = UseFakeStripe();

            // Stripe reads a list call with no customer filter as "every subscription on the account".
            var status = await memberhsipService.CancelSubscription(new Users { Id = 987654, StripeCustomerId = null });

            Assert.Equal(ResponseStatus.Success, status);
            Assert.Empty(stripe.Requests);
        }

        [Fact]
        public async Task CancelSubscription_CancelsOnlyTheCustomersActiveSubscription()
        {
            var stripe = UseFakeStripe();

            var status = await memberhsipService.CancelSubscription(new Users { Id = 987655, StripeCustomerId = "cus_unit" });

            Assert.Equal(ResponseStatus.Success, status);
            Assert.Collection(stripe.Requests,
                list =>
                {
                    Assert.Equal(HttpMethod.Get, list.Method);
                    Assert.Contains("customer=cus_unit", list.RequestUri!.Query);
                },
                cancel =>
                {
                    Assert.Equal(HttpMethod.Delete, cancel.Method);
                    Assert.Equal("/v1/subscriptions/sub_active", cancel.RequestUri!.AbsolutePath);
                });
        }

        /// <summary>
        /// Routes the Stripe SDK to an in-process fake. The dummy key goes in first so that, if the fake
        /// were ever bypassed, Stripe would refuse the call instead of acting on a real account.
        /// </summary>
        private static FakeStripe UseFakeStripe()
        {
            var fake = new FakeStripe();
            Stripe.StripeConfiguration.ApiKey = "sk_test_unit";
            Stripe.StripeConfiguration.StripeClient = new Stripe.StripeClient("sk_test_unit",
                httpClient: new Stripe.SystemNetHttpClient(new HttpClient(fake)));
            return fake;
        }

        private sealed class FakeStripe : HttpMessageHandler
        {
            public List<HttpRequestMessage> Requests { get; } = new();

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                Requests.Add(request);

                var body = request.Method == HttpMethod.Get
                    ? """{"object":"list","url":"/v1/subscriptions","has_more":false,"data":[{"id":"sub_active","object":"subscription","status":"active"},{"id":"sub_old","object":"subscription","status":"canceled"}]}"""
                    : """{"id":"sub_active","object":"subscription","status":"canceled"}""";

                return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK)
                {
                    Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json"),
                });
            }
        }
    }
}
