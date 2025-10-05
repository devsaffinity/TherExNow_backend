using Aps.Apps.HepNow.Models.Dtos;
using Aps.CommonBack.Base.Attributes;
namespace Aps.Apps.HepNow.GraphQL.Queries;

[ExtendObjectType(typeof(Query))]
[GraphBase(nameof(RequestQueries))]

public class RequestQueries : QueryBase<Requests, RequestInput, IRequestService, Users>
{
    #region functions
    [GraphQLName("request_getRequest")]
    public SingleResponseBase<Requests> GetRequest([Authentication] Authentication authentication, [Service] IRequestService requestService, int entityId)
    {
        if (authentication.Status != ResponseStatus.Success)
            return authentication.Status;

        return requestService.GetRequest(entityId, authentication.CurrentUser.Id, authentication.CurrentUser.UserType);
    }

    [GraphQLName("request_getRequests")]
    public ListResponseBase<RequestDto> GetRequests([Authentication] Authentication authentication, [Service] IRequestService requestService)
    {
        if (authentication.Status != ResponseStatus.Success)
            return authentication.Status;

        return requestService.GetRequests(authentication.CurrentUser.Id, authentication.CurrentUser.UserType);
    }
    #endregion
}
