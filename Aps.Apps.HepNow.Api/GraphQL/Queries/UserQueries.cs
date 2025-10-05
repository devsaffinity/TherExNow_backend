using Aps.Apps.HepNow.Models.Dtos;
using Aps.CommonBack.Base;
using Aps.CommonBack.Base.Attributes;
namespace Aps.Apps.HepNow.GraphQL.Queries;

[ExtendObjectType(typeof(Query))]
[GraphBase(nameof(UserQueries))]
public class UserQueries : UserQueryBase<Users, UserInput, IUserService>
{
    #region functions
    [GraphQLIgnore]
    public override ResponseBase<Users> Get(ClaimsPrincipal claims, [Service] IUserService userService, int userId) => null;

    [GraphQLName("user_getUsers")]
    public ListResponseBase<UserDto> GetUsers([Authentication] Authentication authentication, [Service] IUserService userService)
    {
        if (authentication.Status != ResponseStatus.Success)
            return authentication.Status;

        if (authentication.CurrentUser.UserType != UserType.Admin)
            return CustomResponseStatus.CurrentUserIsNotAdmin;

        return userService.GetUsers();
    }

    [GraphQLName("user_getEmailVerified")]
    public ResponseBase<EmailVerifiedDto> GetEmailVerified([Service] IUserService userService, string userIdInFirebase)
    {
        return userService.GetEmailVerified(userIdInFirebase);
    }

    [GraphQLName("user_signIn")]
    public ResponseBase<Users> UserSignIn(ClaimsPrincipal claims, [Service] IUserService service)
    {
        var response = Utils.IsAuthenticated<Users>(claims);
        if (response.Status == ResponseStatus.Success)
        {
            service.AddUserActiveDay(response.Result.Id);
        }
        return response;
    }
    #endregion
}
