using Aps.CommonBack.Base.Attributes;
namespace Aps.Apps.HepNow.GraphQL.Mutations;

[ExtendObjectType(typeof(Mutation))]
[GraphBase(nameof(RequestMutations))]
public class RequestMutations : MutationBase<Requests, RequestInput, IRequestService, Users>
{
    #region functions
    [GraphQLName("request_createRequest")]
    public async Task<ResponseBase<Requests>> Create([Authentication] Authentication authentication
        , [Service] IRequestService service
        , [Service] IUserService userService
        , [Service] INotificationService notificationService
        , RequestInput input)
    {
        if (authentication.Status != ResponseStatus.Success)
            return authentication.Status;

        input.UserId = authentication.CurrentUser.Id;
        var response = service.Add(input);

        if (response.Status == ResponseStatus.Success)
        {
            var userList = userService.GetAll(a => a.UserType == UserType.Admin).Result.ToList();
            userList.Add(authentication.CurrentUser);

            foreach(var user in userList)
            {
                NotificationInput notificationInput = new()
                {
                    RequestId = response.Result.Id,
                    NotificationType = NotificationType.RequestCreated,
                    UserId = user.Id,
                };
                await notificationService.Send(notificationInput);
            }
        }

        return response;
    }

    [GraphQLName("request_updateRequest")]
    public async Task<ResponseBase<Requests>> Update([Authentication] Authentication authentication
        , [Service] IRequestService service
        , [Service] INotificationService notificationService
        , RequestUpdateInput input)
    {
        if (authentication.Status != ResponseStatus.Success)
            return authentication.Status;

        if (authentication.CurrentUser.UserType != UserType.Admin)
            return CustomResponseStatus.CurrentUserIsNotAdmin;

        var response = service.Update(input);
        if (response.Status == ResponseStatus.Success && (input.RequestStatus == RequestStatus.Accepted || input.RequestStatus == RequestStatus.Rejected))
        {
            NotificationInput notificationInput = new()
            {
                RequestId = response.Result.Id,
                NotificationType =  input.RequestStatus == RequestStatus.Accepted ? NotificationType.RequestAccepted : NotificationType.RequestRejected,
                UserId = response.Result.UserId,
            };
            await notificationService.Send(notificationInput);
        }

        return response;
    }

    [GraphQLIgnore]
    public override ResponseBase Delete(ClaimsPrincipal claims, [Service(ServiceKind.Default)] IRequestService service, int entityId) => null;
    #endregion
}
