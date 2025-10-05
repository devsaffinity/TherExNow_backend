namespace Aps.Apps.HepNow.Repositories;

public class ExerciseRepository
 : Repository<Exercises,DataContext>,IExerciseRepository
{
public ExerciseRepository (IDbContextFactory<DataContext> dbContextFactory)
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
