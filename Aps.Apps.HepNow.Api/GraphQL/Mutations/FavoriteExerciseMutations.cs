using Aps.CommonBack.Base.Attributes;
namespace Aps.Apps.HepNow.GraphQL.Mutations;

[ExtendObjectType(typeof(Mutation))]
[GraphBase(nameof(FavoriteExerciseMutations))]
public class FavoriteExerciseMutations : MutationBase<FavoriteExercises, FavoriteExerciseInput, IFavoriteExerciseService, Users>
{
    #region functions
    [GraphQLIgnore]
    public override ResponseBase<FavoriteExercises> Update(ClaimsPrincipal claims, [Service] IFavoriteExerciseService service, FavoriteExerciseInput input) => null;
    [GraphQLIgnore]
    public override ResponseBase Delete(ClaimsPrincipal claims, [Service(ServiceKind.Default)] IFavoriteExerciseService service, int entityId) => null;

    public ResponseBase<FavoriteExercises> Create([Authentication] Authentication authentication, [Service] IFavoriteExerciseService service, FavoriteExerciseInput input)
    {
        if (authentication.Status != ResponseStatus.Success)
            return authentication.Status;

        input.UserId = authentication.CurrentUser.Id;
        return service.Add(input);
    }

    // Named explicitly: MutationType names this from the first public method called "Delete", which
    // can now be the ignored override above, and the client depends on this exact field name.
    [GraphQLName("favoriteExercise_deleteFavoriteExercise")]
    public ResponseBase Delete([Authentication] Authentication authentication, [Service] IFavoriteExerciseService service, int exerciseId)
    {
        if (authentication.Status != ResponseStatus.Success)
            return authentication.Status;

        return service.DeleteByExerciseId(authentication.CurrentUser.Id, exerciseId).Status;
    }
    #endregion
}
