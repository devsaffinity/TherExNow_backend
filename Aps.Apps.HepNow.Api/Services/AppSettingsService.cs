namespace Aps.Apps.HepNow.Services;

public class AppSettingsService : ServiceBase<AppSettings, AppSettingsInput>, IAppSettingsService
{
    #region props
    private IAppSettingsRepository repository;
    #endregion

    public AppSettingsService(IAppSettingsRepository repository) : base(repository)
    {
        this.repository = repository;
    }

    #region functions
    #endregion
}
