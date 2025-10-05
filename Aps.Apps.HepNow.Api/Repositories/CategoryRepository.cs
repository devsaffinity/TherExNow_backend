namespace Aps.Apps.HepNow.Repositories;

public class CategoryRepository: Repository<Categories, DataContext>, ICategoryRepository
{
    public CategoryRepository(IDbContextFactory<DataContext> dbContextFactory): base(dbContextFactory)
    {
        context = dbContextFactory.CreateDbContext();
    }

    #region props
    private DataContext context;

    #endregion
    #region functions
    #endregion
}
