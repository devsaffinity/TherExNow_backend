namespace Aps.Apps.HepNow.Services.Contracts;

public interface INotificationService: IServiceBase<Notification, NotificationInput>
{
    Task Send(NotificationInput notificationInput);
    ResponseBase<Notification> SetShown(int userId, int notificationId);
    ResponseBase<Notification> SetRead(int userId, int notificationId);
    void SetIsReadForAll(int userId);
}
