namespace Aps.Apps.HepNow.Services.Contracts;

public interface IProductService : IServiceBase<Products, ProductInput>
{
    ListResponseBase<Products> GetSimilarProducts(int productId);
}
