namespace Aps.Apps.HepNow.Test.ServiceTest
{
    public class SubCategoryTest: TestBase
    {
        private readonly ISubCategoryService subCategoryService;

        public SubCategoryTest(ISubCategoryService subCategoryService)
        {
            this.subCategoryService = subCategoryService;
        }

        [Theory]
        [InlineData("unique subcategory name for create", DatabaseFixture.FirstCategoryId)]
        public void Create(string subcategoryName, int categoryId)
        {
            var input = new SubCategoryInput
            {
                Name = subcategoryName,
                CategoryId = categoryId,
            };

            var response = subCategoryService.Add(input);
            Assert.True(response.Status == ResponseStatus.Success
                && response.Result.Id > 0
                && response.Result.Name == subcategoryName);
        }

        [Theory]
        [InlineData("new unique subcategory name for update", DatabaseFixture.FirstCategoryId)]
        public void Update(string newSubcategoryName, int categoryId)
        {
            var input = new SubCategoryInput
            {
                Name = "temp subcategory name",
                CategoryId = categoryId,
            };

            var response = subCategoryService.Add(input);
            Assert.True(response.Status == ResponseStatus.Success && response.Result.Id > 0);

            input = new SubCategoryInput
            {
                Name = newSubcategoryName,
                Id = response.Result.Id,
                CategoryId = categoryId,
            };

            response = subCategoryService.Update(input);
            Assert.True(response.Status == ResponseStatus.Success && response.Result.Id > 0
                && response.Result.Name == newSubcategoryName);
        }

        [Theory]
        [InlineData(DatabaseFixture.FirstCategoryId)]
        public void Delete(int categoryId) 
        {
            var input = new SubCategoryInput
            {
                Name = "",
                CategoryId = categoryId,
            };

            var addResponse = subCategoryService.Add(input);
            Assert.True(addResponse.Status == ResponseStatus.Success && addResponse.Result.Id > 0);

            var deleteResponse = subCategoryService.Remove(addResponse.Result.Id);
            Assert.Equal(deleteResponse.Status, ResponseStatus.Success);
        }

        [Theory]
        [InlineData(DatabaseFixture.FirstCategoryId, DatabaseFixture.FirstSubcategoryId)]
        public void GetByCategoryId(int categoryId, int subcategoryId)
        {
            var response = subCategoryService.GetByCategoryId(categoryId);
            Assert.True(response.Status == ResponseStatus.Success
                && response.Result.Any(a => a.Id == subcategoryId));
        }
    }
}
