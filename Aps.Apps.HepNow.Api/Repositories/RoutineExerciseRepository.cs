namespace Aps.Apps.HepNow.Repositories;

public class RoutineExerciseRepository
 : Repository<RoutineExercises,DataContext>,IRoutineExerciseRepository
{
public RoutineExerciseRepository (IDbContextFactory<DataContext> dbContextFactory)
:base(dbContextFactory)
{
	context = dbContextFactory.CreateDbContext();
}

#region props
	private DataContext context;

#endregion
#region functions
#endregion
}
