
namespace Aps.Apps.HepNow.Test.ServicesTest
{
    public class UserTest: TestBase
    {
        private readonly IUserService userService;

        public UserTest(IUserService userService)
        {
            this.userService = userService;
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
        public void GetAll()
        {
            var response = userService.GetAll(a => true);
            Assert.True(response.Status == ResponseStatus.Success
                && response.Result.Any());
        }
    }
}
