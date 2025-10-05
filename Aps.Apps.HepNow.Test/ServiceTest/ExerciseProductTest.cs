namespace Aps.Apps.HepNow.Test.ServiceTest
{
    public class ExerciseProductTest: TestBase
    {
        private readonly IExerciseProductService exerciseProductService;
        private readonly IExerciseService exerciseService;

        public ExerciseProductTest(IExerciseProductService exerciseProductService, IExerciseService exerciseService)
        {
            this.exerciseProductService = exerciseProductService;
            this.exerciseService = exerciseService;
        }

        [Theory]
        [InlineData(DatabaseFixture.FirstSubcategoryId, DatabaseFixture.FirstProductId)]
        public void Create(int subcategoryId, int productId)
        {
            var exerciseInput = new ExerciseInput
            {
                Name = Guid.NewGuid().ToString(),
                SubCategoryId = subcategoryId,
            };
            var exerciseResponse = exerciseService.Add(exerciseInput);

            var input = new ExerciseProductInput
            {
                ExerciseId = exerciseResponse.Result.Id,
                ProductId = productId
            };

            var response = exerciseProductService.Add(input);
            Assert.True(response.Status == ResponseStatus.Success 
                && response.Result.ExerciseId == input.ExerciseId
                && response.Result.ProductId == input.ProductId);
        }

        [Theory]
        [InlineData(DatabaseFixture.FirstSubcategoryId, DatabaseFixture.FirstProductId)]
        public void Delete(int subcategoryId, int productId)
        {
            var exerciseInput = new ExerciseInput
            {
                Name = Guid.NewGuid().ToString(),
                SubCategoryId = subcategoryId,
            };
            var exerciseResponse = exerciseService.Add(exerciseInput);

            var input = new ExerciseProductInput
            {
                ExerciseId = exerciseResponse.Result.Id,
                ProductId = productId,
            };

            var response = exerciseProductService.Add(input);
            Assert.True(response.Status == ResponseStatus.Success);

            var deleteResponse = exerciseProductService.Remove(response.Result.Id);
            Assert.True(deleteResponse.Status == ResponseStatus.Success);
        }
    }
}
