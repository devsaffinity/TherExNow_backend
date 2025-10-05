namespace Aps.Apps.HepNow.Repositories;

public class RequestRepository
 : Repository<Requests,DataContext>,IRequestRepository
{
public RequestRepository (IDbContextFactory<DataContext> dbContextFactory)
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
