namespace Aps.Apps.HepNow.Services.Contracts;

public interface IFavoriteExerciseService: IServiceBase<FavoriteExercises, FavoriteExerciseInput>
{
    ResponseBase DeleteByExerciseId(int userId, int exerciseId);
}
