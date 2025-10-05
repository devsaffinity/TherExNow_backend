using Aps.CommonBack.Base.Attributes;
namespace Aps.Apps.HepNow.GraphQL.Mutations;

[ExtendObjectType(typeof(Mutation))]
[GraphBase(nameof(CustomizedExerciseMutations))]
public class CustomizedExerciseMutations : MutationBase<CustomizedExercise, CustomizedExerciseInput, ICustomizedExerciseService, Users>
{
    #region functions
    [GraphQLName("customizedExercise_updateCustomizedExercise")]
    public ResponseBase<CustomizedExercise> Update([Authentication] Authentication authentication, [Service] ICustomizedExerciseService service, CustomizedExerciseInput input)
    { 
        if (authentication.Status != ResponseStatus.Success)
            return authentication.Status;

        return service.Update(authentication.CurrentUser.Id, input);
    }

    [GraphQLIgnore]
    public override ResponseBase<CustomizedExercise> Create(ClaimsPrincipal claims, [Service(ServiceKind.Default)] ICustomizedExerciseService service, CustomizedExerciseInput input) => null;

    [GraphQLIgnore]
    public override ResponseBase Delete(ClaimsPrincipal claims, [Service] ICustomizedExerciseService service, int exerciseId) => null;
    #endregion
}
