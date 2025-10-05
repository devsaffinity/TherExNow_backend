namespace Aps.Apps.HepNow.Repositories;

public class ExerciseProductRepository
 : Repository<ExerciseProducts,DataContext>,IExerciseProductRepository
{
public ExerciseProductRepository (IDbContextFactory<DataContext> dbContextFactory)
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
