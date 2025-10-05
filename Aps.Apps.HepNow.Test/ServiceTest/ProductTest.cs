namespace Aps.Apps.HepNow.Test.ServiceTest
{
    public class ProductTest: TestBase
    {
        private readonly IProductService productService;

        public ProductTest(IProductService productService)
        {
            this.productService = productService;
        }

        [Theory]
        [InlineData("unique product name for create", "description", "/url1", "['/url2', '/url3']", "['blue']", 8)]
        public void Create(string name, string description, string photoUrl, string otherMediaUrl, string colors, decimal price)
        {
            var input = new ProductInput
            {
                Name = name,
                Description = description,
                PhotoUrl = photoUrl,
                OtherMediaUrl = otherMediaUrl,
                Colors = colors,
                Price = price,
            };

            var response = productService.Add(input);
            Assert.True(response.Status == ResponseStatus.Success
                && response.Result.Id > 0
                && response.Result.Name == name
                && response.Result.Description == description
                && response.Result.PhotoUrl == photoUrl
                && response.Result.OtherMediaUrl == otherMediaUrl
                && response.Result.Colors == colors
                && response.Result.Price == price);
        }

        [Theory]
        [InlineData("unique product name for update", "description", "/url1", "['/url2', '/url3']", "['blue']", 8)]
        public void Update(string name, string description, string photoUrl, string otherMediaUrl, string colors, decimal price)
        {
            var product = new Products { Name = Guid.NewGuid().ToString() };
            var response = productService.Add(product);
            Assert.True(response.Status == ResponseStatus.Success && response.Result.Id > 0);

            var input = new ProductInput
            {
                Id = response.Result.Id,
                Name = name,
                Description = description,
                PhotoUrl = photoUrl,
                OtherMediaUrl = otherMediaUrl,
                Colors = colors,
                Price = price,
            };

            response = productService.Update(input);
            Assert.True(response.Status == ResponseStatus.Success
                && response.Result.Id > 0
                && response.Result.Name == name
                && response.Result.Description == description
                && response.Result.PhotoUrl == photoUrl
                && response.Result.OtherMediaUrl == otherMediaUrl
                && response.Result.Colors == colors
                && response.Result.Price == price);
        }

        [Fact]
        public void Delete() 
        {
            var product = new Products();
            var addResponse = productService.Add(product);
            Assert.True(addResponse.Status == ResponseStatus.Success && addResponse.Result.Id > 0);

            var deleteResponse = productService.Remove(addResponse.Result.Id);
            Assert.Equal(deleteResponse.Status, ResponseStatus.Success);
        }

        [Theory]
        [InlineData(DatabaseFixture.FirstProductId)]
        public void GetAll(int productId)
        {
            var response = productService.GetAll(a => true);
            Assert.True(response.Status == ResponseStatus.Success
                && response.Result.Any(a => a.Id == productId));
        }
    }
}
