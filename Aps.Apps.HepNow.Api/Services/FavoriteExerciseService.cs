using Aps.Apps.HepNow.Models.Entities;

namespace Aps.Apps.HepNow.Services;

public class FavoriteExerciseService : ServiceBase<FavoriteExercises, FavoriteExerciseInput>, IFavoriteExerciseService
{
    public FavoriteExerciseService(IFavoriteExerciseRepository repository, IExerciseRepository exerciseRepository) : base(repository)
    {
        this.repository = repository;
        this.exerciseRepository = exerciseRepository;
    }

    #region props
    private IFavoriteExerciseRepository repository;
    private readonly IExerciseRepository exerciseRepository;
    #endregion

    #region functions
    public override ResponseBase<FavoriteExercises> Add(FavoriteExerciseInput input)
    {
        if (repository.Any(a => a.UserId == input.UserId && a.ExerciseId == input.ExerciseId))
            return ResponseStatus.AlreadyExists;

        var response = base.Add(input);
        if (response.Status == ResponseStatus.Success)
        {
            UpdateFavoriteCount(input.ExerciseId);
        }
        return response;
    }

    public ResponseBase DeleteByExerciseId(int userId, int exerciseId)
    {
        var entity = repository.Where(a => a.UserId == userId && a.ExerciseId == exerciseId).FirstOrDefault();
        if (entity == null)
            return ResponseStatus.NotFound;

        repository.Remove(entity);
        UpdateFavoriteCount(exerciseId);
        return ResponseStatus.Success;
    }

    private void UpdateFavoriteCount(int exerciseId)
    {
        var exercise = exerciseRepository.Find(exerciseId);
        if (exercise != null)
        {
            exercise.FavoriteCount = repository.Where(a => a.ExerciseId == exerciseId).Count();
            exerciseRepository.Update(exercise);
        }
    }
    #endregion
}
