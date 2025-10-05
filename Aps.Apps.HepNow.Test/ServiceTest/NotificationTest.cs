namespace Aps.Apps.HepNow.Test.ServiceTest
{
    public class NotificationTest: TestBase
    {
        private readonly INotificationService notificationService;

        public NotificationTest(INotificationService notificationService)
        {
            this.notificationService = notificationService;
        }

        [Theory]
        [InlineData(DatabaseFixture.NormalUserId, DatabaseFixture.FirstNotificationId)]
        public void SetRead(int userId, int notificationId)
        {
            var response = notificationService.SetShown(userId, notificationId);
            Assert.True(response.Status == ResponseStatus.Success && response.Result.Id > 0);
        }

        [Theory]
        [InlineData(DatabaseFixture.NormalUserId, DatabaseFixture.FirstNotificationId)]
        public void GetAll(int userId, int notificationId)
        {
            var response = notificationService.GetAll(a => a.UserId == userId);
            Assert.True(response.Status == ResponseStatus.Success && response.Result.Any(a => a.Id == notificationId));
        }
    }
}
