using System.Text;
using Aps.Apps.HepNow.Generics.Responses;
using Aps.Apps.HepNow.Models.Dtos;

namespace Aps.Apps.HepNow.Services;

public class RequestService : ServiceBase<Requests, RequestInput>, IRequestService
{
    public RequestService(IRequestRepository repository, IProductRepository productRepository) : base(repository)
    {
        this.repository = repository;
        this.productRepository = productRepository;
    }

    #region props
    private IRequestRepository repository;
    private readonly IProductRepository productRepository;
    #endregion

    #region functions
    public override ResponseBase<Requests> Add(RequestInput input)
    {
        input.RequestStatus = RequestStatus.Waiting;
        foreach (var requestProduct in input.RequestProducts)
        {
            var product = productRepository.GetById(requestProduct.ProductId);
            if (product == null)
                return CustomResponseStatus.ProductNotFound;

            requestProduct.Price = product.Price;
        }
        return base.Add(input);
    }

    public ResponseBase<Requests> Update(RequestUpdateInput input)
    {
        var entity = repository.GetById(input.Id);
        if (entity == null)
            return ResponseStatus.NotFound;

        repository.Update(input.Adapt(entity));
        return entity;
    }

    public SingleResponseBase<Requests> GetRequest(int requestId, int userId, UserType userType)
    {
        var request = repository.GetById(requestId);
        if (request == null)
            return ResponseStatus.NotFound;

        if (request.UserId == userId || userType == UserType.Admin)
            return SingleResponseBase<Requests>.Success(repository.Where(r => r.Id == requestId));
        else
            return ResponseStatus.NotAllowd;
    }

    public ListResponseBase<RequestDto> GetRequests(int userId, UserType userType)
    {
        var result = repository.Where(a => !a.IsDeleted && (userType == UserType.Admin || a.UserId == userId))
                  .IgnoreQueryFilters()
                  .Select(a => new RequestDto
                  {
                      User = a.User,
                      Request = a,
                      ProductCount = a.RequestProducts.Sum(p => p.Count)
                  });

        return ListResponseBase<RequestDto>.Success(result);
    }

    public string BuildOrderSummaryHtml(RequestInput input)
    {
        var lines = new StringBuilder();
        decimal total = 0m;

        if (input?.RequestProducts != null)
        {
            foreach (var requestProduct in input.RequestProducts)
            {
                var product = productRepository.GetById(requestProduct.ProductId);
                var name = product?.Name ?? $"Product #{requestProduct.ProductId}";
                // Price was stamped from the product during Add; fall back to the live product price.
                var price = requestProduct.Price != 0 ? requestProduct.Price : (product?.Price ?? 0m);
                var lineTotal = price * requestProduct.Count;
                total += lineTotal;

                var color = string.IsNullOrWhiteSpace(requestProduct.Color) ? "" : $" ({requestProduct.Color})";
                lines.Append($"{name}{color} — {requestProduct.Count} x ${price:0.00} = ${lineTotal:0.00}<br>");
            }
        }

        lines.Append($"<b>Total: ${total:0.00}</b>");
        return lines.ToString();
    }
    #endregion
}
