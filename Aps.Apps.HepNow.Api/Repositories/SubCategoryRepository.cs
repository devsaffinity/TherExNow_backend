namespace Aps.Apps.HepNow.Repositories;

public class SubCategoryRepository : Repository<SubCategories, DataContext>, ISubCategoryRepository
{
    #region props
    private DataContext context;
    #endregion

    public SubCategoryRepository(IDbContextFactory<DataContext> dbContextFactory) : base(dbContextFactory)
    {
        context = dbContextFactory.CreateDbContext();
    }

    #region functions
    #endregion
}
