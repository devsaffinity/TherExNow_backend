namespace Aps.Apps.HepNow.Repositories;

public class RoutineRepository
 : Repository<Routines,DataContext>,IRoutineRepository
{
public RoutineRepository (IDbContextFactory<DataContext> dbContextFactory)
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
