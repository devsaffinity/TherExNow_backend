using Aps.CommonBack.Base.Attributes;
namespace Aps.Apps.HepNow.GraphQL.Queries;

[ExtendObjectType(typeof(Query))]
[GraphBase(nameof(CustomizedExerciseQueries))]
public class CustomizedExerciseQueries : QueryBase<CustomizedExercise, CustomizedExerciseInput, ICustomizedExerciseService, Users>
{
    #region functions
    [GraphQLIgnore]
    public override ResponseBase<CustomizedExercise> Get(ClaimsPrincipal claims, [Service(ServiceKind.Default)] ICustomizedExerciseService service, int entityId) => null;
    [GraphQLIgnore]
    public override ListResponseBase<CustomizedExercise> GetItems(ClaimsPrincipal claims, [Service(ServiceKind.Default)] ICustomizedExerciseService service) => null;

    [GraphQLName("customizedExercise_getCustomizedExercise")]
    public ResponseBase<CustomizedExercise> GetCustomizedExercise([Authentication] Authentication authentication, [Service] ICustomizedExerciseService service, int exerciseId)
    {
        if (authentication.Status != ResponseStatus.Success)
            return authentication.Status;

        return service.Get(authentication.CurrentUser.Id, exerciseId);
    }

    [GraphQLName("customizedExercise_getAll")]
    public ListResponseBase<CustomizedExercise> GetAll([Authentication] Authentication authentication, [Service] ICustomizedExerciseService service)
    {
        if (authentication.Status != ResponseStatus.Success)
            return authentication.Status;

        return service.GetAll(a => a.UserId == authentication.CurrentUser.Id);
    }
    #endregion
}
