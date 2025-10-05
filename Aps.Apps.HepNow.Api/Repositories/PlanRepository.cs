namespace Aps.Apps.HepNow.Repositories;

public class PlanRepository
 : Repository<Plans,DataContext>,IPlanRepository
{
public PlanRepository (IDbContextFactory<DataContext> dbContextFactory)
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
