using Aps.Apps.HepNow.Models.Dtos;

namespace Aps.Apps.HepNow.Services.Contracts;

public interface  IRequestService: IServiceBase<Requests,RequestInput>
{
    ResponseBase<Requests> Update(RequestUpdateInput input);
    SingleResponseBase<Requests> GetRequest(int requestId, int userId, UserType userType);
    ListResponseBase<RequestDto> GetRequests(int userId, UserType userType);

    // Builds an itemised HTML summary (product name, colour, qty, line price and order total)
    // for the order-confirmation emails. Lives here because the service owns the product repository.
    string BuildOrderSummaryHtml(RequestInput input);
}
