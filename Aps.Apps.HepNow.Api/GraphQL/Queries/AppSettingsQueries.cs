using Aps.CommonBack.Base.Attributes;
namespace Aps.Apps.HepNow.GraphQL.Queries;

[ExtendObjectType(typeof(Query))]
[GraphBase(nameof(AppSettingsQueries))]
public class AppSettingsQueries : QueryBase<AppSettings, AppSettingsInput, IAppSettingsService, Users>
{
    #region functions
    [GraphQLIgnore]
    public override ResponseBase<AppSettings> Get(ClaimsPrincipal claims, [Service] IAppSettingsService service, int appSettingsId) => null;
    [GraphQLIgnore]
    public override ListResponseBase<AppSettings> GetItems(ClaimsPrincipal claims, [Service] IAppSettingsService service) => null;
    
    [GraphQLName("appSettings_getAppSettings")]
    public ListResponseBase<AppSettings> GetAppSettings([Authentication] Authentication authentication, [Service] IAppSettingsService service)
    {
        return service.GetAll(a => true);
    }
    #endregion
}
