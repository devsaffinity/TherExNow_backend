namespace Aps.Apps.HepNow.Test.ServiceTest
{
    public class DashboardTest : TestBase
    {
        private readonly IDashboardService dashboardService;

        public DashboardTest(IDashboardService dashboardService)
        {
            this.dashboardService = dashboardService;
        }

        [Fact]
        public void GetTodayRevenueAndActiveUsers()
        {
            var response = dashboardService.GetTodayRevenueAndActiveUsers();
            Assert.True(response.Status == ResponseStatus.Success);
        }

        [Fact]
        public void GetActiveUsers()
        {
            var response = dashboardService.GetActiveUsers(DateTime.Now.AddDays(-1), DateTime.Now.AddDays(1));
            Assert.True(response.Status == ResponseStatus.Success);
        }

        [Fact]
        public void GetRevenuePlan()
        {
            var response = dashboardService.GetRevenuePlan(DateTime.Now.AddDays(-1), DateTime.Now.AddDays(1));
            Assert.True(response.Status == ResponseStatus.Success && response.Result.Any() && response.Result.First().Amount > 0);
        }

        [Fact]
        public void GetRevenueShop()
        {
            var response = dashboardService.GetRevenueShop(DateTime.Now.AddDays(-1), DateTime.Now.AddDays(1));
            Assert.True(response.Status == ResponseStatus.Success && response.Result.Any() && response.Result.First().Amount > 0);
        }

        [Fact]
        public void GetExerciseInCategory()
        {
            var response = dashboardService.GetExerciseInCategory();
            Assert.True(response.Status == ResponseStatus.Success && response.Result.Any());
        }
    }
}
