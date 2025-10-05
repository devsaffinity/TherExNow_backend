using Aps.CommonBack.Base.Attributes;
namespace Aps.Apps.HepNow.GraphQL.Mutations;

[ExtendObjectType(typeof(Mutation))]
[GraphBase(nameof(ProductMutations))]
public class ProductMutations : MutationBase<Products, ProductInput, IProductService, Users>
{

    #region functions
    [GraphQLName("product_createProduct")]
    public ResponseBase<Products> Create([Authentication] Authentication authentication, [Service] IProductService service, ProductInput input)
    {
        if (authentication.Status != ResponseStatus.Success)
            return authentication.Status;

        if (authentication.CurrentUser.UserType != UserType.Admin)
            return CustomResponseStatus.CurrentUserIsNotAdmin;

        input.Name = input.Name.Trim();

        /*if (service.Any(a => a.Name == input.Name).Status == ResponseStatus.Success)
            return CustomResponseStatus.DuplicateName;*/

        return service.Add(input);
    }

    [GraphQLName("product_updateProduct")]
    public ResponseBase<Products> Update([Authentication] Authentication authentication, [Service] IProductService service, ProductInput input)
    {
        if (authentication.Status != ResponseStatus.Success)
            return authentication.Status;

        if (authentication.CurrentUser.UserType != UserType.Admin)
            return CustomResponseStatus.CurrentUserIsNotAdmin;

        if (!string.IsNullOrEmpty(input.Name))
        {
            input.Name = input.Name.Trim();
            /*if (service.Any(a => a.Name == input.Name && a.Id != input.Id).Status == ResponseStatus.Success)
                return CustomResponseStatus.DuplicateName;*/
        }

        return service.Update(input);
    }

    [GraphQLName("product_deleteProduct")]
    public ResponseBase Delete([Authentication] Authentication authentication, [Service] IProductService service, int entityId)
    {
        if (authentication.Status != ResponseStatus.Success)
            return authentication.Status;

        if (authentication.CurrentUser.UserType != UserType.Admin)
            return CustomResponseStatus.CurrentUserIsNotAdmin;

        return service.Remove(entityId).Status;
    }
    #endregion
}
