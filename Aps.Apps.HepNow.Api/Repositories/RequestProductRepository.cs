namespace Aps.Apps.HepNow.Repositories;

public class RequestProductRepository
 : Repository<RequestProducts,DataContext>,IRequestProductRepository
{
public RequestProductRepository (IDbContextFactory<DataContext> dbContextFactory)
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
