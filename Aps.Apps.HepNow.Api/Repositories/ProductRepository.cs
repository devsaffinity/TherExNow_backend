namespace Aps.Apps.HepNow.Repositories;

public class ProductRepository
 : Repository<Products,DataContext>,IProductRepository
{
public ProductRepository (IDbContextFactory<DataContext> dbContextFactory)
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
