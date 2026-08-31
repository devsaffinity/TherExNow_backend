
using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace Aps.Apps.HepNow.Test.ServicesTest
{
    public class UserTest: TestBase
    {
        private readonly IUserService userService;
        private readonly IHttpContextAccessor httpContextAccessor;

        public UserTest(IUserService userService, IHttpContextAccessor httpContextAccessor)
        {
            this.userService = userService;
            this.httpContextAccessor = httpContextAccessor;
        }

        /// <summary>Puts a Firebase ID token's claims on the ambient request, as JwtBearer would.</summary>
        private void SignedInAs(string email, bool emailVerified)
        {
            var claims = new List<Claim>
            {
                new("email", email),
                new("email_verified", emailVerified ? "true" : "false"),
            };

            httpContextAccessor.HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Firebase")),
            };
        }

        [Theory]
        [InlineData("dfg4xk6lsdf546", UserType.NormalUser, "test@test.com", "Steven Wiley")]
        public void Signup(string externalId, UserType userType, string email, string fullName)
        {
            var input = new UserInput 
            {
                UserType = userType,
                FullName = fullName,
            };
            var response = userService.SignUp(externalId, email, input);
            Assert.Equal(response.Status, ResponseStatus.Success);

            response = userService.SignUp(externalId, email, input);
            Assert.Equal(ResponseStatus.AlreadyExists, response.Status);
        }

        [Theory]
        [InlineData("k34jkcj158wqkos", UserType.NormalUser)]
        public void DeactivateUser(string externalId, UserType userType)
        {
            var newUser = new Users 
            {
                ExternalId = externalId,
                UserType = userType 
            };
            var response = userService.Add(newUser);
            Assert.Equal(response.Status, ResponseStatus.Success);
            Assert.Equal(userService.Get(newUser.Id).Result.ExternalId, externalId);

            response = userService.Remove(newUser.Id);
            Assert.Equal(response.Status, ResponseStatus.Success);
            Assert.True(userService.Get(newUser.Id).Result.IsDeleted);
        }

        [Theory]
        [InlineData("d32wl334gsda23", "SteveVBrower@dayrep.com", "Steve Brower", " +1254863258")]
        public void Update(string externalId, string email, string fullName, string phoneNumber)
        {
            var response = userService.SignUp(externalId, email);
            Assert.Equal(response.Status, ResponseStatus.Success);

            var input = new UserInput
            {
                Id = response.Result.Id,
                FullName = fullName,
                PhoneNumber = phoneNumber
            };
            response = userService.Update(input);
            Assert.Equal(response.Status, ResponseStatus.Success);
            Assert.Equal(response.Result.FullName, fullName);
            Assert.Equal(response.Result.PhoneNumber, phoneNumber);
        }

        [Fact]
        public void SignUp_WithUnverifiedEmail_CannotClaimSomeoneElsesAccount()
        {
            const string victimEmail = "unverified-takeover-victim@example.com";
            var victim = userService.SignUp("victim-uid-1", victimEmail, new UserInput { FullName = "Victim" });
            Assert.Equal(ResponseStatus.Success, victim.Status);

            // Firebase hands out a valid ID token the moment an email/password account is created,
            // so an attacker can register the victim's address and present this token without ever
            // following the confirmation link.
            SignedInAs(victimEmail, emailVerified: false);

            var attack = userService.SignUp("attacker-uid-1", victimEmail, new UserInput { Email = victimEmail });

            Assert.Equal(ResponseStatus.AlreadyExists, attack.Status);

            var row = userService.Get(victim.Result.Id).Result;
            Assert.NotEqual("attacker-uid-1", row.ExternalId);
            Assert.NotEqual("attacker-uid-1", row.ExternalId2);
        }

        [Fact]
        public void SignUp_WithVerifiedEmail_LinksSecondProviderToTheSameAccount()
        {
            const string email = "verified-linker@example.com";
            var original = userService.SignUp("verified-uid-1", email, new UserInput { FullName = "Owner" });
            Assert.Equal(ResponseStatus.Success, original.Status);

            // Same person returning through a provider that verified the address for us.
            SignedInAs(email, emailVerified: true);

            var linked = userService.SignUp("verified-uid-2", email, new UserInput { Email = email });

            Assert.Equal(ResponseStatus.Success, linked.Status);
            Assert.Equal(original.Result.Id, linked.Result.Id);
            Assert.Equal("verified-uid-2", userService.Get(original.Result.Id).Result.ExternalId2);
        }

        [Fact]
        public void GetAll()
        {
            var response = userService.GetAll(a => true);
            Assert.True(response.Status == ResponseStatus.Success
                && response.Result.Any());
        }
    }
}
