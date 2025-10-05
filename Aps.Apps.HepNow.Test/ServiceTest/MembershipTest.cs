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
    }
}
