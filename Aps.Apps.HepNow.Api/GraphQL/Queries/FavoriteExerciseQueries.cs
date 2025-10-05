using Aps.CommonBack.Base.Attributes;
namespace Aps.Apps.HepNow.GraphQL.Queries;

[ExtendObjectType(typeof(Query))]
[GraphBase(nameof(FavoriteExerciseQueries))]
public class FavoriteExerciseQueries : QueryBase<FavoriteExercises, FavoriteExerciseInput, IFavoriteExerciseService, Users>
{
    #region functions
    [GraphQLIgnore]
    public override ResponseBase<FavoriteExercises> Get(ClaimsPrincipal claims, [Service] IFavoriteExerciseService favoriteExerciseService, int favoriteexerciseId) => null;

    [GraphQLName("favoriteExercise_getFavoriteExercises")]
    public ListResponseBase<FavoriteExercises> GetItems([Authentication] Authentication authentication, [Service] IFavoriteExerciseService favoriteExerciseService)
    {
        if (authentication.Status != ResponseStatus.Success)
            return authentication.Status;

        return favoriteExerciseService.GetAll(a => a.UserId == authentication.CurrentUser.Id);
    }
    #endregion
}
