using Aps.CommonBack.Base.Attributes;
namespace Aps.Apps.HepNow.GraphQL.Mutations;

[ExtendObjectType(typeof(Mutation))]
[GraphBase(nameof(ShoppingCartMutations))]
public class ShoppingCartMutations : MutationBase<ShoppingCart, ShoppingCartInput, IShoppingCartService, Users>
{
    #region functions
    public ResponseBase<ShoppingCart> Update([Authentication] Authentication authentication, [Service] IShoppingCartService service, ShoppingCartInput input)
    {
        if (authentication.Status != ResponseStatus.Success)
            return authentication.Status;

        input.UserId = authentication.CurrentUser.Id;
        return service.Update(input);
    }

    public ResponseBase<ShoppingCart> Create([Authentication] Authentication authentication, [Service] IShoppingCartService service, ShoppingCartInput input)
    {
        if (authentication.Status != ResponseStatus.Success)
            return authentication.Status;

        input.UserId = authentication.CurrentUser.Id;
        return service.Add(input);
    }

    public ResponseBase Delete([Authentication] Authentication authentication, [Service] IShoppingCartService service, int entityId)
    {
        if (authentication.Status != ResponseStatus.Success)
            return authentication.Status;

        return service.Remove(entityId, authentication.CurrentUser.Id).Status;
    }

    [GraphQLName("shoppingCart_deleteShoppingCarts")]
    public ResponseBase DeleteAll([Authentication] Authentication authentication, [Service] IShoppingCartService service)
    {
        if (authentication.Status != ResponseStatus.Success)
            return authentication.Status;

        return service.RemoveAll(authentication.CurrentUser.Id);
    }
    #endregion
}
