namespace Aps.Apps.HepNow.Services;

public class RoutineService : ServiceBase<Routines, RoutineInput>, IRoutineService
{
    public RoutineService(IRoutineRepository repository, IRoutineExerciseRepository routineExerciseRepository) : base(repository)
    {
        this.repository = repository;
        this.routineExerciseRepository = routineExerciseRepository;
    }

    #region functions
    private IRoutineRepository repository;
    private readonly IRoutineExerciseRepository routineExerciseRepository;

    public SingleResponseBase<Routines> GetRoutine(int routineId, int userId)
    {
        if (repository.Any(r => r.Id == routineId && r.UserId == userId))
            return SingleResponseBase<Routines>.Success(repository.Where(r => r.Id == routineId && r.UserId == userId));
        else
            return ResponseStatus.NotFound;
    }

    public override ResponseBase<Routines> Update(RoutineInput input)
    {
        var routine = repository.Where(a => a.Id == input.Id).Include(a => a.RoutineExercises).FirstOrDefault();
        if (routine is null)
            return ResponseStatus.NotFound;

        routine.Name = input.Name;
        routine.Description = input.Description;
        routine.FileUrl = input.FileUrl;
        repository.Update(routine, saveNow: false);

        if (input.RoutineExercises != null)
        {
            //Delete RoutineExercise
            var deletedRoutineExercises = new List<RoutineExercises>();
            foreach (var routineExercise in routine.RoutineExercises)
            {
                if (!input.RoutineExercises.Any(l => l.Id == routineExercise.Id))
                    deletedRoutineExercises.Add(routineExercise);
            }
            routineExerciseRepository.RemoveRange(deletedRoutineExercises);

            //Update RoutineExercise
            foreach (var routineExercise in input.RoutineExercises.Where(l => l.Id > 0))
            {
                var entity = routineExerciseRepository.GetById(routineExercise.Id.Value);
                routineExercise.Adapt(entity);
                routineExerciseRepository.Update(entity, saveNow: false);
            }

            //Create RoutineExercise
            foreach (var routineExercise in input.RoutineExercises.Where(l => l.Id.GetValueOrDefault() <= 0))
            {
                var entity = routineExercise.Adapt<RoutineExercises>();
                entity.RoutineId = input.Id.Value;
                routineExerciseRepository.Add(entity, saveNow: false);
            }
        }

        // Update/Create above deferred their save (saveNow: false); committing once here groups
        // them into a single write instead of one round-trip per row.
        routineExerciseRepository.SaveChanges(routineExerciseRepository.GetDbContext());
        repository.SaveChanges(repository.GetDbContext());

        return ResponseBase<Routines>.Success(repository.Where(a => a.Id == input.Id).Include(a => a.RoutineExercises).FirstOrDefault()); ;
    }
    #endregion
}
