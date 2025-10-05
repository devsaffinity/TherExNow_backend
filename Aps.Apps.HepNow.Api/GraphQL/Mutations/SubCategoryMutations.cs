using Aps.CommonBack.Base.Attributes;
namespace Aps.Apps.HepNow.GraphQL.Mutations;

[ExtendObjectType(typeof(Mutation))]
[GraphBase(nameof(SubCategoryMutations))]
public class SubCategoryMutations : MutationBase<SubCategories, SubCategoryInput, ISubCategoryService, Users>
{
    [GraphQLIgnore]
    public override ResponseBase<SubCategories> Create(ClaimsPrincipal claims, [Service] ISubCategoryService service, SubCategoryInput input) => null;
    [GraphQLIgnore]
    public override ResponseBase<SubCategories> Update(ClaimsPrincipal claims, [Service] ISubCategoryService service, SubCategoryInput input) => null;
    [GraphQLIgnore]
    public override ResponseBase Delete(ClaimsPrincipal claims, [Service] ISubCategoryService service, int entityId) => null;

    #region functions
    [GraphQLName("subcategory_createSubcategory")]
    public ResponseBase<SubCategories> Create([Authentication] Authentication authentication, [Service] ISubCategoryService service, SubCategoryInput input)
    {
        if (authentication.Status != ResponseStatus.Success)
            return authentication.Status;

        if (authentication.CurrentUser.UserType != UserType.Admin)
            return CustomResponseStatus.CurrentUserIsNotAdmin;

        input.Name = input.Name.Trim();
        if (service.Any(a => a.CategoryId == input.CategoryId && a.Name == input.Name).Status == ResponseStatus.Success)
            return ResponseStatus.AlreadyExists;

        return service.Add(input);
    }

    [GraphQLName("subcategory_updateSubcategory")]
    public ResponseBase<SubCategories> Update([Authentication] Authentication authentication, [Service] ISubCategoryService service, SubCategoryInput input)
    {
        if (authentication.Status != ResponseStatus.Success)
            return authentication.Status;

        if (authentication.CurrentUser.UserType != UserType.Admin)
            return CustomResponseStatus.CurrentUserIsNotAdmin;

        input.Name = input.Name.Trim();
        if (service.Any(a => a.CategoryId == input.CategoryId && a.Name == input.Name && a.Id != input.Id).Status == ResponseStatus.Success)
            return ResponseStatus.AlreadyExists;

        return service.Update(input);
    }

    [GraphQLName("subcategory_deleteSubcategory")]
    public ResponseBase Delete([Authentication] Authentication authentication, [Service] ISubCategoryService service, int entityId)
    {
        if (authentication.Status != ResponseStatus.Success)
            return authentication.Status;

        if (authentication.CurrentUser.UserType != UserType.Admin)
            return CustomResponseStatus.CurrentUserIsNotAdmin;

        return service.Remove(entityId).Status;
    }
    #endregion
}
