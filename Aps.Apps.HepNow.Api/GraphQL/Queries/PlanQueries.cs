using Aps.CommonBack.Base.Attributes;
namespace Aps.Apps.HepNow.GraphQL.Queries;

[ExtendObjectType(typeof(Query))]
[GraphBase(nameof(PlanQueries))]
public class PlanQueries: QueryBase<Plans, PlanInput, IPlanService, Users>
{
    #region functions
    [GraphQLIgnore]
    public override ResponseBase<Plans> Get(ClaimsPrincipal claims, [Service] IPlanService planService, int planId) => null;

    [GraphQLName("plan_getPlans")]
    public ListResponseBase<Plans> GetItems([Authentication] Authentication authentication, [Service] IPlanService service)
    {
        return service.GetAll(predicate: a => true);
    }
    #endregion
}
