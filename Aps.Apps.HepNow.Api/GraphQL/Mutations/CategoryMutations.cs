using Aps.CommonBack.Base.Attributes;
namespace Aps.Apps.HepNow.GraphQL.Mutations;

[ExtendObjectType(typeof(Mutation))]
[GraphBase(nameof(CategoryMutations))]
public class CategoryMutations : MutationBase<Categories, CategoryInput, ICategoryService, Users>
{
    #region functions
    [GraphQLName("category_createCategory")]
    public ResponseBase<Categories> Create([Authentication] Authentication authentication, [Service] ICategoryService service, CategoryInput input)
    {
        if (authentication.Status != ResponseStatus.Success)
            return authentication.Status;

        if (authentication.CurrentUser.UserType != UserType.Admin)
            return CustomResponseStatus.CurrentUserIsNotAdmin;

        return service.Add(input);
    }

    [GraphQLName("category_updateCategory")]
    public ResponseBase<Categories> Update([Authentication] Authentication authentication, [Service] ICategoryService service, CategoryInput input)
    {
        if (authentication.Status != ResponseStatus.Success)
            return authentication.Status;

        if (authentication.CurrentUser.UserType != UserType.Admin)
            return CustomResponseStatus.CurrentUserIsNotAdmin;

        return service.Update(input);
    }

    [GraphQLName("category_deleteCategory")]
    public ResponseBase Delete([Authentication] Authentication authentication, [Service] ICategoryService service, int entityId)
    {
        if (authentication.Status != ResponseStatus.Success)
            return authentication.Status;

        if (authentication.CurrentUser.UserType != UserType.Admin)
            return CustomResponseStatus.CurrentUserIsNotAdmin;

        return service.Remove(entityId).Status;
    }
    #endregion
}
