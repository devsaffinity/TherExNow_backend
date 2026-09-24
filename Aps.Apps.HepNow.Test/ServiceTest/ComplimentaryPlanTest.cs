using Aps.Apps.HepNow.Generics.Responses;

namespace Aps.Apps.HepNow.Test.ServiceTest
{
    /// <summary>
    /// The admin-only "grant a plan without a payment" path: comp accounts, testers, support fixes.
    /// A granted membership must read as premium everywhere, but must never look like revenue.
    /// </summary>
    public class ComplimentaryPlanTest : TestBase
    {
        private readonly IMembershipService membershipService;
        private readonly IUserService userService;
        private readonly IPlanService planService;

        public ComplimentaryPlanTest(IMembershipService membershipService, IUserService userService, IPlanService planService)
        {
            this.membershipService = membershipService;
            this.userService = userService;
            this.planService = planService;
        }

        private int PaidPlanId(decimal monthly = 25)
        {
            return planService.Add(new PlanInput
            {
                Title = $"Paid plan {System.Guid.NewGuid()}",
                Description = "comp test",
                Features = "{}",
                PriceMonth = monthly,
                PriceYear = monthly * 12,
            }).Result.Id;
        }

        private int NewUserId()
        {
            return userService.Add(new Users()).Result.Id;
        }

        [Fact]
        public void Grant_PutsTheUserOnThePaidPlan_AtItsRealPrice()
        {
            var userId = NewUserId();

            var response = membershipService.GrantComplimentaryPlan(userId, PaidPlanId(25), months: 3);

            Assert.Equal(ResponseStatus.Success, response.Status);
            // The real price, so every "paymentAmount != 0" premium check treats it as paid.
            Assert.Equal(25, response.Result.PaymentAmount);
            Assert.Equal(MembershipProvider.Complimentary, response.Result.Provider);
            Assert.True(response.Result.EndDate > DateTime.UtcNow.AddMonths(2));
        }

        [Fact]
        public void Grant_MakesTheUserAPremiumMember()
        {
            var userId = NewUserId();

            membershipService.GrantComplimentaryPlan(userId, PaidPlanId(), months: 1);

            var current = membershipService.GetCurrentMembership(userId);
            Assert.Equal(ResponseStatus.Success, current.Status);
            // This is exactly the signal the app reads to unlock premium.
            Assert.NotEqual(0, current.Result.PaymentAmount);
        }

        [Fact]
        public void Grant_EndsAnExistingMembership_SoTheUserIsNeverOnTwo()
        {
            var userId = NewUserId();

            membershipService.GrantComplimentaryPlan(userId, PaidPlanId(), months: 1);
            membershipService.GrantComplimentaryPlan(userId, PaidPlanId(), months: 1);

            // GetCurrentMembership returns the single active one; a second active row would make it throw.
            var current = membershipService.GetCurrentMembership(userId);
            Assert.Equal(ResponseStatus.Success, current.Status);
            Assert.True(current.Result.Id > 0);
        }

        [Fact]
        public void Grant_RefusesTheFreePlan()
        {
            var userId = NewUserId();

            // The seeded plan is the free tier (price 0); a comp grant on it means nothing.
            var response = membershipService.GrantComplimentaryPlan(userId, DatabaseFixture.FirstPlanId, months: 1);

            Assert.Equal(CustomResponseStatus.PlanNotFound, response.Status);
        }

        [Fact]
        public void Grant_RefusesAnUnknownUser()
        {
            var response = membershipService.GrantComplimentaryPlan(int.MaxValue, PaidPlanId(), months: 1);

            Assert.Equal(ResponseStatus.UserNotFound, response.Status);
        }

        [Fact]
        public void Grant_RefusesAnUnknownPlan()
        {
            var response = membershipService.GrantComplimentaryPlan(NewUserId(), int.MaxValue, months: 1);

            Assert.Equal(CustomResponseStatus.PlanNotFound, response.Status);
        }

        [Fact]
        public void Grant_RefusesANonPositiveDuration()
        {
            var response = membershipService.GrantComplimentaryPlan(NewUserId(), PaidPlanId(), months: 0);

            Assert.Equal(ResponseStatus.NotEnoghData, response.Status);
        }
    }
}
