using Aps.Apps.HepNow.Models.Dtos;

namespace Aps.Apps.HepNow.Services.Contracts;

public interface  IRequestService: IServiceBase<Requests,RequestInput>
{
    ResponseBase<Requests> Update(RequestUpdateInput input);
    SingleResponseBase<Requests> GetRequest(int requestId, int userId, UserType userType);
    ListResponseBase<RequestDto> GetRequests(int userId, UserType userType);
}
