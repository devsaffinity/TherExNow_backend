namespace Aps.Apps.HepNow.Test.ServiceTest
{
    public class ShoppingCartTest: TestBase
    {
        private readonly IShoppingCartService shoppingCartService;

        public ShoppingCartTest(IShoppingCartService shoppingCartService)
        {
            this.shoppingCartService = shoppingCartService;
        }

        [Theory]
        [InlineData(DatabaseFixture.FirstProductId, DatabaseFixture.NormalUserId, "green", 5)]
        public void Create(int productId, int userId, string color, int count)
        {
            var input = new ShoppingCartInput
            {
                ProductId = productId,
                Color = color,
                Count = count,
                UserId = userId,
            };

            var response = shoppingCartService.Add(input);
            Assert.True(response.Status == ResponseStatus.Success 
                && response.Result.ProductId == input.ProductId
                && response.Result.UserId == input.UserId
                && response.Result.Color == input.Color);
        }

        [Theory]
        [InlineData(DatabaseFixture.FirstProductId, DatabaseFixture.NormalUserId, "green", 5)]
        public void Update(int productId, int userId, string color, int count)
        {
            var addResponse = shoppingCartService.Add(new ShoppingCart { UserId = userId, ProductId = productId });
            Assert.True(addResponse.Status == ResponseStatus.Success);

            var input = new ShoppingCartInput
            {
                Id = addResponse.Result.Id,
                ProductId = productId,
                Color = color,
                Count = count,
                UserId = userId,
            };

            var response = shoppingCartService.Update(input);
            Assert.True(response.Status == ResponseStatus.Success 
                && response.Result.ProductId == input.ProductId
                && response.Result.UserId == input.UserId
                && response.Result.Color == input.Color);
        }

        [Theory]
        [InlineData(DatabaseFixture.FirstProductId, DatabaseFixture.NormalUserId, "blue", 3)]
        public void Delete(int productId, int userId, string color, int count) 
        {
            var input = new ShoppingCartInput
            {
                ProductId = productId,
                Color = color,
                Count = count,
                UserId = userId,
            };

            var response = shoppingCartService.Add(input);
            Assert.True(response.Status == ResponseStatus.Success);

            var deleteResponse = shoppingCartService.Remove(response.Result.Id, response.Result.UserId);
            Assert.True(deleteResponse.Status == ResponseStatus.Success);
        }

        [Theory]
        [InlineData(DatabaseFixture.NormalUserId)]
        public void GetAll(int userId)
        {
            var response = shoppingCartService.GetAll(a => a.UserId == userId);
            Assert.True(response.Status == ResponseStatus.Success && response.Result.Any());
        }
    }
}
