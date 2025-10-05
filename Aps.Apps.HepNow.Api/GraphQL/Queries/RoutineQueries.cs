using Aps.CommonBack.Base.Attributes;
namespace Aps.Apps.HepNow.GraphQL.Queries;

[ExtendObjectType(typeof(Query))]
[GraphBase(nameof(RoutineQueries))]
public class RoutineQueries : QueryBase<Routines, RoutineInput, IRoutineService, Users>
{
    #region functions
    [GraphQLIgnore]
    public override ResponseBase<Routines> Get(ClaimsPrincipal claims, [Service(ServiceKind.Default)] IRoutineService service, int entityId) => null;
    [GraphQLIgnore]
    public override ListResponseBase<Routines> GetItems(ClaimsPrincipal claims, [Service(ServiceKind.Default)] IRoutineService service) => null;

    [GraphQLName("routine_getRoutine")]
    public SingleResponseBase<Routines> GetRoutine([Authentication] Authentication authentication, [Service] IRoutineService service, int entityId)
    {
        if (authentication.Status != ResponseStatus.Success)
            return authentication.Status;

        return service.GetRoutine(entityId, authentication.CurrentUser.Id);
    }

    [GraphQLName("routine_getRoutines")]
    public ListResponseBase<Routines> GetRoutines([Authentication] Authentication authentication, [Service] IRoutineService service)
    {
        if (authentication.Status != ResponseStatus.Success)
            return authentication.Status;

        return service.GetAll(predicate: a => a.UserId == authentication.CurrentUser.Id);
    }
    #endregion
}
