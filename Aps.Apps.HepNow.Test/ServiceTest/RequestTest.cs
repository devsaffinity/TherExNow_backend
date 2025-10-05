namespace Aps.Apps.HepNow.Test.ServiceTest
{
    public class RequestTest: TestBase
    {
        private readonly IRequestService requestService;

        public RequestTest(IRequestService requestService)
        {
            this.requestService = requestService;
        }

        [Theory]
        [InlineData("4599 Parkway Street", "San Diego", "California", "154528", "red", 3, DatabaseFixture.NormalUserId, DatabaseFixture.FirstProductId)]
        public void Create(string address, string city, string state, string zipCode, string color, int count, int userId, int productId)
        {
            var input = new RequestInput
            {
                Address = address,
                City = city,
                State = state,
                ZipCode = zipCode,
                UserId = userId,
                RequestProducts = new List<RequestProductInput> 
                {
                    new RequestProductInput
                    {
                        Color = color,
                        Count = count,
                        ProductId = productId,
                    }
                }
            };

            var response = requestService.Add(input);
            Assert.True(response.Status == ResponseStatus.Success
                && response.Result.Id > 0
                && response.Result.Address == address
                && response.Result.City == city
                && response.Result.State == state
                && response.Result.ZipCode == zipCode
                && response.Result.RequestProducts.Count == 1);
        }

        [Theory]
        [InlineData(RequestStatus.Accepted, DatabaseFixture.NormalUserId)]
        public void UpdateStatus(RequestStatus requestStatus, int userId)
        {
            var request = new Requests { UserId = userId };
            var response = requestService.Add(request);
            Assert.True(response.Status == ResponseStatus.Success && response.Result.Id > 0);

            response = requestService.Update(new RequestUpdateInput
            {
                Id = response.Result.Id,
                RequestStatus = requestStatus
            });

            Assert.True(response.Status == ResponseStatus.Success
                && response.Result.Id > 0
                && response.Result.RequestStatus == requestStatus);
        }

        [Theory]
        [InlineData(DatabaseFixture.NormalUserId, DatabaseFixture.FirstRequestId)]
        public void Get(int userId, int requestId)
        {
            var response = requestService.GetRequest(requestId, userId, UserType.NormalUser);
            Assert.True(response.Status == ResponseStatus.Success && response.Result.Id == requestId) ;
        }

        [Theory]
        [InlineData(DatabaseFixture.NormalUserId, DatabaseFixture.FirstRequestId)]
        public void GetAll(int userId, int requestId)
        {
            var response = requestService.GetRequests(userId, UserType.NormalUser);
            Assert.True(response.Status == ResponseStatus.Success
                && response.Result.Any(a => a.Request.Id == requestId));
        }
    }
}
