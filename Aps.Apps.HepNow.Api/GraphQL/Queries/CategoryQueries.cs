using Aps.CommonBack.Base.Attributes;
namespace Aps.Apps.HepNow.GraphQL.Queries;

[ExtendObjectType(typeof(Query))]
[GraphBase(nameof(CategoryQueries))]
public class CategoryQueries : QueryBase<Categories, CategoryInput, ICategoryService, Users>
{
    #region functions
    [GraphQLIgnore]
    public override ResponseBase<Categories> Get(ClaimsPrincipal claims, [Service] ICategoryService categoryService, int categoryId) => null;

    [GraphQLName("category_getCategories")]
    public override ListResponseBase<Categories> GetItems(ClaimsPrincipal claims, [Service] ICategoryService service)
    {
        return service.GetAll(predicate: a => true);
    }
    #endregion
}
