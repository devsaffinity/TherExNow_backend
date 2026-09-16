using System.Security.Claims;
using Aps.Apps.HepNow.GraphQL.Mutations;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace Aps.Apps.HepNow.Test.ServicesTest
{
    public class UserRoleTest : TestBase
    {
        private readonly IUserService userService;
        private readonly IMembershipService membershipService;
        private readonly INotificationService notificationService;
        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly IConfiguration configuration;

        public UserRoleTest(IUserService userService, IMembershipService membershipService, INotificationService notificationService,
            IHttpContextAccessor httpContextAccessor, IConfiguration configuration)
        {
            this.userService = userService;
            this.membershipService = membershipService;
            this.notificationService = notificationService;
            this.httpContextAccessor = httpContextAccessor;
            this.configuration = configuration;
        }

        /// <summary>The principal AuthMiddleware hands to resolvers: a verified token plus the caller's row, or null.</summary>
        private static ClaimsPrincipal SignedIn(string externalId, string email, Users? row)
        {
            var claims = new List<Claim>
            {
                new("user_id", externalId),
                new("email", email),
                new("email_verified", "true"),
                new("user", JsonConvert.SerializeObject(row)),
            };
            return new ClaimsPrincipal(new ClaimsIdentity(claims, "Firebase"));
        }

        [Fact]
        public async Task SignUp_AskingToBeAdmin_CreatesANormalUser()
        {
            const string externalId = "role-signup-uid", email = "role-signup@example.com";

            var response = await new UserMutations().SignUp(SignedIn(externalId, email, row: null), userService, membershipService,
                new UserInput { UserType = UserType.Admin, FullName = "Would-be Admin" }, notificationService, httpContextAccessor, configuration);

            Assert.Equal(ResponseStatus.Success, response.Status);
            Assert.Equal(UserType.NormalUser, userService.Get(response.Result.Id).Result.UserType);
        }

        [Fact]
        public void Update_MemberCannotMakeThemselvesAdmin()
        {
            const string externalId = "role-update-uid", email = "role-update@example.com";
            var member = userService.SignUp(externalId, email, new UserInput { FullName = "Member" }).Result;

            var response = new UserMutations().Update(SignedIn(externalId, email, member), userService,
                new UserInput { UserType = UserType.Admin });

            Assert.Equal(ResponseStatus.Success, response.Status);
            Assert.Equal(UserType.NormalUser, userService.Get(member.Id).Result.UserType);
        }

        [Fact]
        public void Update_AdminCanStillChangeAMembersRole()
        {
            var member = userService.SignUp("role-promote-uid", "role-promote@example.com", new UserInput { FullName = "Member" }).Result;
            var admin = userService.Get(DatabaseFixture.AdminUserId).Result;

            var response = new UserMutations().Update(SignedIn(admin.ExternalId, admin.Email!, admin), userService,
                new UserInput { Id = member.Id, UserType = UserType.Admin });

            Assert.Equal(ResponseStatus.Success, response.Status);
            Assert.Equal(UserType.Admin, userService.Get(member.Id).Result.UserType);
        }
    }
}
