namespace Aps.Apps.HepNow.Repositories;

public class AppSettingsRepository : Repository<AppSettings, DataContext>, IAppSettingsRepository
{
    public AppSettingsRepository(IDbContextFactory<DataContext> dbContextFactory) : base(dbContextFactory)
    {
        context = dbContextFactory.CreateDbContext();
    }

    #region props
    private DataContext context;
    #endregion

    #region functions
    #endregion
}
