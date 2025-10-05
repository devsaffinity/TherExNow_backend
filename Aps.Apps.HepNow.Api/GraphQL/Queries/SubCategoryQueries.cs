using Aps.CommonBack.Base.Attributes;
namespace Aps.Apps.HepNow.GraphQL.Queries;

[ExtendObjectType(typeof(Query))]
[GraphBase(nameof(SubCategoryQueries))]
public class SubCategoryQueries : QueryBase<SubCategories, SubCategoryInput, ISubCategoryService, Users>
{
    #region functions
    [GraphQLIgnore]
    public override ResponseBase<SubCategories> Get(ClaimsPrincipal claims, [Service(ServiceKind.Default)] ISubCategoryService service, int entityId) => null;
    [GraphQLIgnore]
    public override ListResponseBase<SubCategories> GetItems(ClaimsPrincipal claims, [Service(ServiceKind.Default)] ISubCategoryService service) => null;

    [GraphQLName("subcategory_getSubcategoriesByCategoryId")]
    public ListResponseBase<SubCategories> GetByCategoryId([Service] ISubCategoryService service, int categoryId)
    {
        return service.GetByCategoryId(categoryId);
    }
    #endregion
}
