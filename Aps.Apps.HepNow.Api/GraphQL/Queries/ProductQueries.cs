using Aps.CommonBack.Base.Attributes;
namespace Aps.Apps.HepNow.GraphQL.Queries;

[ExtendObjectType(typeof(Query))]
[GraphBase(nameof(ProductQueries))]
public class ProductQueries : QueryBase<Products, ProductInput, IProductService, Users>
{
    #region functions
    [GraphQLIgnore]
    public override ResponseBase<Products> Get(ClaimsPrincipal claims, [Service] IProductService productService, int productId) => null;

    [GraphQLName("product_getProducts")]
    public ListResponseBase<Products> GetItems([Authentication] Authentication authentication, [Service] IProductService service)
    {
        return service.GetAll(predicate: a => true);
    }
    
    [GraphQLName("product_getSimilarProducts")]
    public ListResponseBase<Products> GetSimilarProducts([Authentication] Authentication authentication, [Service] IProductService service, int productId)
    {
        return service.GetSimilarProducts(productId);
    }
    #endregion
}
