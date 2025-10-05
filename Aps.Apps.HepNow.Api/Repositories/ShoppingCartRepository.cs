namespace Aps.Apps.HepNow.Repositories;

public class ShoppingCartRepository : Repository<ShoppingCart, DataContext>, IShoppingCartRepository
{
    public ShoppingCartRepository(IDbContextFactory<DataContext> dbContextFactory) : base(dbContextFactory)
    {
        context = dbContextFactory.CreateDbContext();
    }

    #region props
    private DataContext context;
    #endregion

    #region functions
    #endregion
}
