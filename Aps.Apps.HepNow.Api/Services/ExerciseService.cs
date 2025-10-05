using Aps.Apps.HepNow.Models.Dtos;
using Aps.Apps.HepNow.Models.Entities;

namespace Aps.Apps.HepNow.Services;

public class ExerciseService : ServiceBase<Exercises, ExerciseInput>, IExerciseService
{
    public ExerciseService(IExerciseRepository repository
        , IRoutineExerciseRepository routineExerciseRepository
        , IFavoriteExerciseRepository favoriteExerciseRepository
        , IExerciseProductRepository exerciseProductRepository
        , INotificationService notificationService
        ) : base(repository)
    {
        this.repository = repository;
        this.routineExerciseRepository = routineExerciseRepository;
        this.favoriteExerciseRepository = favoriteExerciseRepository;
        this.exerciseProductRepository = exerciseProductRepository;
        this.notificationService = notificationService;
    }

    #region props
    private IExerciseRepository repository;
    private readonly IRoutineExerciseRepository routineExerciseRepository;
    private readonly IFavoriteExerciseRepository favoriteExerciseRepository;
    private readonly IExerciseProductRepository exerciseProductRepository;
    private readonly INotificationService notificationService;
    #endregion

    #region functions
    public ResponseBase<Exercises> Remove(int entityId, string description)
    {
        if (routineExerciseRepository.Any(a => a.ExerciseId == entityId))
        {
            var userIdList = new List<int>();
            foreach(var item in routineExerciseRepository.Where(a => a.ExerciseId == entityId).Include(a => a.Routine).ToList())
            {
                userIdList.Add(item.Routine.UserId);
                routineExerciseRepository.Remove(item);
            }

            foreach (var userId in userIdList.Distinct())
            {
                notificationService.Send(new NotificationInput
                {
                    ExerciseId = entityId,
                    NotificationType = NotificationType.ExerciseDeleted,
                    UserId = userId,
                    Description = description,
                });
            }
        }

        favoriteExerciseRepository.RemoveRange(favoriteExerciseRepository.Where(a => a.ExerciseId == entityId).ToList());

        exerciseProductRepository.RemoveRange(exerciseProductRepository.Where(a => a.ExerciseId == entityId).ToList());

        return base.Remove(entityId);
    }

    public ListResponseBase<ExerciseDto> GetExercises(int? userId)
    {
        var query = repository.GetDbSet().Select(a => new ExerciseDto
        {
            Exercise = a,
            Products = a.ExerciseProducts.Select(p => p.Product),
            IsFavorite = userId == null ? false : a.FavoriteExercises.Any(f => f.UserId == userId),
            FavoriteCount = a.FavoriteExercises.Count,
        });
        
        return ListResponseBase<ExerciseDto>.Success(query);
    }

    public ListResponseBase<Products> GetProducts(int exerciseId)
    {
        var query = repository.Where<ExerciseProducts>(a => a.Product.IsActive == true && a.ExerciseId == exerciseId).Select(a => a.Product);
        return ListResponseBase<Products>.Success(query);
    }
    #endregion
}
