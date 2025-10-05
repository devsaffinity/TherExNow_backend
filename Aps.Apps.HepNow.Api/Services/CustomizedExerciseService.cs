using Aps.Apps.HepNow.Models.Entities;
using Aps.CommonBack.Base.Generics.Responses;

namespace Aps.Apps.HepNow.Services;

public class CustomizedExerciseService: ServiceBase<CustomizedExercise, CustomizedExerciseInput>, ICustomizedExerciseService
{
    public CustomizedExerciseService(ICustomizedExerciseRepository repository, IExerciseRepository exerciseRepository): base(repository)
    {
        this.repository = repository;
        this.exerciseRepository = exerciseRepository;
    }

    #region props
    private ICustomizedExerciseRepository repository;
    private readonly IExerciseRepository exerciseRepository;

    public ResponseBase<CustomizedExercise> Get(int userId, int exerciseId)
    {
        if (!repository.Any(a => a.ExerciseId == exerciseId && a.UserId == userId))
        {
            var exercise = exerciseRepository.Where(a => a.Id == exerciseId)
                .Include(a => a.ExerciseProducts)
                .ThenInclude(a => a.Product)
                .FirstOrDefault();

            if (exercise == null)
                return CustomResponseStatus.ExerciseNotFound;

            var entity = exercise.Adapt<CustomizedExercise>();
            entity.ExerciseId = exerciseId;
            entity.UserId = userId;
            entity.Exercise = exercise;
            return entity;
        }

        return repository.Where(a => a.ExerciseId == exerciseId && a.UserId == userId)
                .Include(a => a.Exercise)
                .ThenInclude(a => a.ExerciseProducts)
                .ThenInclude(a => a.Product)
                .FirstOrDefault();
    }

    public ResponseBase<CustomizedExercise> Update(int userId, CustomizedExerciseInput input)
    {
        var entity = repository.Where(a => a.ExerciseId == input.ExerciseId && a.UserId == userId).FirstOrDefault();

        if (entity == null)
        {
            if (exerciseRepository.Find(input.ExerciseId) == null)
                return CustomResponseStatus.ExerciseNotFound;

            entity = input.Adapt<CustomizedExercise>();
            entity.UserId = userId;
            repository.Add(entity);
        }
        else
        {
            input.Adapt(entity);
            repository.Update(entity);
        }
        return entity;
    }
    #endregion

    #region functions
    #endregion
}
