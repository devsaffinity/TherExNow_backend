namespace Aps.Apps.HepNow.Test.ServiceTest
{
    public class CategoryTest: TestBase
    {
        private readonly ICategoryService categoryService;
        private readonly ISubCategoryService subCategoryService;

        public CategoryTest(ICategoryService categoryService, ISubCategoryService subCategoryService)
        {
            this.categoryService = categoryService;
            this.subCategoryService = subCategoryService;
        }

        [Theory]
        [InlineData("unique category name for create", "unique subcategory name for create")]
        public void Create(string categoryName, string subcategoryName)
        {
            var input = new CategoryInput
            {
                Name = categoryName,
                SubCategories = new List<SubCategoryForCategoryInput> 
                {
                    new SubCategoryForCategoryInput 
                    {
                        Name = subcategoryName 
                    } 
                }
            };

            var response = categoryService.Add(input);
            Assert.True(response.Status == ResponseStatus.Success
                && response.Result.Id > 0
                && response.Result.Name == categoryName);
        }

        [Theory]
        [InlineData("unique category name for update", "unique subcategory name for update", "new unique subcategory name for update")]
        public void Update(string categoryName, string subcategoryName, string newSubcategoryName)
        {
            var input = new CategoryInput
            {
                Name = "temp category name",
                SubCategories = new List<SubCategoryForCategoryInput>(),
            };

            var response = categoryService.Add(input);
            Assert.True(response.Status == ResponseStatus.Success && response.Result.Id > 0);

            input = new CategoryInput
            {
                Id = response.Result.Id,
                Name = categoryName,
                SubCategories = new List<SubCategoryForCategoryInput>
                {
                    new SubCategoryForCategoryInput
                    {
                        Name = subcategoryName
                    }
                }
            };

            response = categoryService.Update(input);
            var category = categoryService.Get(input.Id.GetValueOrDefault()).Result;
            var subcategories = subCategoryService.GetAll(a => a.CategoryId == input.Id).Result;

            Assert.True(response.Status == ResponseStatus.Success
                && category.Name == categoryName
                && subcategories.Count() == 1
                && subcategories.First().Name == subcategoryName);

            input.SubCategories = new List<SubCategoryForCategoryInput>
            {
                new SubCategoryForCategoryInput
                {
                    Id = subcategories.First().Id,
                    Name = newSubcategoryName
                }
            };

            response = categoryService.Update(input);
            category = categoryService.Get(input.Id.GetValueOrDefault()).Result;
            subcategories = subCategoryService.GetAll(a => a.CategoryId == input.Id).Result;

            Assert.True(response.Status == ResponseStatus.Success
                && category.Name == categoryName
                && subcategories.Count() == 1
                && subcategories.First().Name == newSubcategoryName);
        }

        [Fact]
        public void Delete() 
        {
            var input = new CategoryInput
            {
                Name = "",
            };

            var addResponse = categoryService.Add(input);
            Assert.True(addResponse.Status == ResponseStatus.Success && addResponse.Result.Id > 0);

            var deleteResponse = categoryService.Remove(addResponse.Result.Id);
            Assert.Equal(deleteResponse.Status, ResponseStatus.Success);

            deleteResponse = categoryService.Remove(addResponse.Result.Id);
            Assert.Equal(deleteResponse.Status, ResponseStatus.NotFound);
        }

        [Theory]
        [InlineData(DatabaseFixture.FirstCategoryId)]
        public void GetAll(int categoryId)
        {
            var response = categoryService.GetAll(a => true);
            Assert.True(response.Status == ResponseStatus.Success
                && response.Result.Any(a => a.Id == categoryId));
        }
    }
}
