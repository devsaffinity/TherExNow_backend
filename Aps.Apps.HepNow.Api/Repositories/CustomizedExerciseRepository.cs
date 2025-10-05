namespace Aps.Apps.HepNow.Repositories;

public class CustomizedExerciseRepository
 : Repository<CustomizedExercise,DataContext>,ICustomizedExerciseRepository
{
public CustomizedExerciseRepository (IDbContextFactory<DataContext> dbContextFactory)
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
