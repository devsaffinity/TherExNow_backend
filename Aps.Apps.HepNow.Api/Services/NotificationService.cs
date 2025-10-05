using HotChocolate.Subscriptions;

namespace Aps.Apps.HepNow.Services;

public class NotificationService : ServiceBase<Notification, NotificationInput>, INotificationService
{
    #region props
    private readonly INotificationRepository repository;
    private readonly ITopicEventSender topicSender;
    #endregion

    public NotificationService(INotificationRepository repository, ITopicEventSender topicSender) : base(repository)
    {
        this.repository = repository;
        this.topicSender = topicSender;
    }

    #region functions
    public async Task Send(NotificationInput notificationInput)
    {
        Notification notification = notificationInput.Adapt<Notification>();
        repository.Add(notification);

        try
        {
            await topicSender.SendAsync($"{notification.UserId}_NotificationAdded", notification);
        }
        catch { }
    }

    public void SetIsReadForAll(int userId)
    {
        var list = repository.Where(a => a.UserId == userId && !a.IsRead).ToList();
        list.ForEach(a => a.IsRead = true);
        repository.UpdateRange(list);
    }

    public ResponseBase<Notification> SetShown(int userId, int notificationId)
    {
        var entity = repository.GetById(notificationId);
        if (entity == null)
            return ResponseStatus.NotFound;

        if (entity.UserId != userId)
            return ResponseStatus.NotAllowd;

        entity.IsShown = true;
        repository.Update(entity);
        return entity;
    }

    public ResponseBase<Notification> SetRead(int userId, int notificationId)
    {
        var entity = repository.GetById(notificationId);
        if (entity == null)
            return ResponseStatus.NotFound;

        if (entity.UserId != userId)
            return ResponseStatus.NotAllowd;

        entity.IsRead = true;
        repository.Update(entity);
        return entity;
    }
    #endregion
}
