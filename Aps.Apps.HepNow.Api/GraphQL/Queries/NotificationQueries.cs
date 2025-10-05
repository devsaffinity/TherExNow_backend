using Aps.CommonBack.Base.Attributes;
namespace Aps.Apps.HepNow.GraphQL.Queries;

[ExtendObjectType(typeof(Query))]
[GraphBase(nameof(NotificationQueries))]
public class NotificationQueries: QueryBase<Notification, NotificationInput, INotificationService, Users>
{
    #region functions
    [GraphQLIgnore]
    public override ResponseBase<Notification> Get(ClaimsPrincipal claims, [Service] INotificationService notificationService, int notificationId) => null;

    [GraphQLIgnore]
    public override ListResponseBase<Notification> GetItems(ClaimsPrincipal claims, [Service(ServiceKind.Default)] INotificationService service) => null;

    [GraphQLName("notification_getNotifications")]
    public ListResponseBase<Notification> GetAll([Authentication] Authentication authentication, [Service] INotificationService service)
    {
        if (authentication.Status != ResponseStatus.Success)
            return authentication.Status;

        return service.GetAll(predicate: a => a.UserId == authentication.CurrentUser.Id);
    }

    [GraphQLName("notification_getNotificationsWithSetRead")]
    public ListResponseBase<Notification> GetNotificationsWithSetRead([Authentication] Authentication authentication, [Service] INotificationService service)
    {
        if (authentication.Status != ResponseStatus.Success)
            return authentication.Status;

        service.SetIsReadForAll(authentication.CurrentUser.Id);
        return service.GetAll(predicate: a => a.UserId == authentication.CurrentUser.Id);
    }
    #endregion
}
