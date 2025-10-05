using Aps.CommonBack.Base.Attributes;
namespace Aps.Apps.HepNow.GraphQL.Mutations;

[ExtendObjectType(typeof(Mutation))]
[GraphBase(nameof(NotificationMutations))]
public class NotificationMutations : MutationBase<Notification, NotificationInput, INotificationService, Users>
{
    #region functions
    [GraphQLIgnore]
    public override ResponseBase<Notification> Create(ClaimsPrincipal claims, [Service(ServiceKind.Default)] INotificationService service, NotificationInput input) => null;
    [GraphQLIgnore]
    public override ResponseBase Delete(ClaimsPrincipal claims, [Service(ServiceKind.Default)] INotificationService service, int entityId) => null;
    [GraphQLIgnore]
    public override ResponseBase<Notification> Update(ClaimsPrincipal claims, [Service(ServiceKind.Default)] INotificationService service, NotificationInput input) => null;


    [GraphQLName("notification_setShown")]
    public ResponseBase<Notification> SetShown([Authentication] Authentication authentication, [Service] INotificationService service, int notificationId)
    {
        if (authentication.Status != ResponseStatus.Success)
            return authentication.Status;

        return service.SetShown(authentication.CurrentUser.Id, notificationId);
    }

    [GraphQLName("notification_setRead")]
    public ResponseBase<Notification> SetRead([Authentication] Authentication authentication, [Service] INotificationService service, int notificationId)
    {
        if (authentication.Status != ResponseStatus.Success)
            return authentication.Status;

        return service.SetRead(authentication.CurrentUser.Id, notificationId);
    }

    [GraphQLName("notification_delete")]
    public ResponseStatus Delete([Authentication] Authentication authentication, [Service] INotificationService service, int notificationId)
    {
        if (authentication.Status != ResponseStatus.Success)
            return authentication.Status;

        return service.Remove(notificationId);
    }

    #endregion
}
