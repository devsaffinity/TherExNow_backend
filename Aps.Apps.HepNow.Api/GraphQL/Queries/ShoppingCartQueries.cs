using Aps.CommonBack.Base.Attributes;
namespace Aps.Apps.HepNow.GraphQL.Queries;

[ExtendObjectType(typeof(Query))]
[GraphBase(nameof(ShoppingCartQueries))]
public class ShoppingCartQueries : QueryBase<ShoppingCart, ShoppingCartInput, IShoppingCartService, Users>
{
    #region functions
    [GraphQLIgnore]
    public override ResponseBase<ShoppingCart> Get(ClaimsPrincipal claims, [Service] IShoppingCartService ShoppingCartService, int ShoppingCartId) => null;

    [GraphQLName("shoppingCart_getShoppingCarts")]
    public ListResponseBase<ShoppingCart> GetItems([Authentication] Authentication authentication, [Service] IShoppingCartService ShoppingCartService)
    {
        if (authentication.Status != ResponseStatus.Success)
            return authentication.Status;

        return ShoppingCartService.GetAll(a => a.UserId == authentication.CurrentUser.Id);
    }
    #endregion
}
