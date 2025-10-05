using Aps.CommonBack.Base.Attributes;
namespace Aps.Apps.HepNow.GraphQL.Mutations;

[ExtendObjectType(typeof(Mutation))]
[GraphBase(nameof(AppSettingsMutations))]
public class AppSettingsMutations : MutationBase<AppSettings, AppSettingsInput, IAppSettingsService, Users>
{
    [GraphQLIgnore]
    public override ResponseBase<AppSettings> Create(ClaimsPrincipal claims, [Service(ServiceKind.Default)] IAppSettingsService service, AppSettingsInput input) => null;
    [GraphQLIgnore]
    public override ResponseBase Delete(ClaimsPrincipal claims, [Service(ServiceKind.Default)] IAppSettingsService service, int entityId) => null;
    [GraphQLIgnore]
    public override ResponseBase<AppSettings> Update(ClaimsPrincipal claims, [Service(ServiceKind.Default)] IAppSettingsService service, AppSettingsInput input) => null;

    #region functions
    [GraphQLName("appSettings_updateAppSettings")]
    public ResponseBase<AppSettings> Update([Authentication] Authentication authentication, [Service] IAppSettingsService service, AppSettingsInput input)
    {
        if (authentication.Status != ResponseStatus.Success)
            return authentication.Status;

        if (authentication.CurrentUser.UserType != UserType.Admin)
            return CustomResponseStatus.CurrentUserIsNotAdmin;

        var entity = service.GetAll(a => true).Result.FirstOrDefault();
        if (entity == null)
        {
            return service.Add(input);
        }
        else
        {
            input.Id = entity.Id;
            return service.Update(input);
        }
    }
    #endregion
}
