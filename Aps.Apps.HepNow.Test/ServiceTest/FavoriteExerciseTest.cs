namespace Aps.Apps.HepNow.Test.ServiceTest
{
    public class FavoriteExerciseTest: TestBase
    {
        private readonly IFavoriteExerciseService favoriteExerciseService;
        private readonly IExerciseService exerciseService;

        public FavoriteExerciseTest(IFavoriteExerciseService favoriteExerciseService, IExerciseService exerciseService)
        {
            this.favoriteExerciseService = favoriteExerciseService;
            this.exerciseService = exerciseService;
        }

        [Theory]
        [InlineData("exercise name", DatabaseFixture.FirstSubcategoryId, DatabaseFixture.NormalUserId)]
        public void Create(string exerciseName, int subcategoryId, int userId)
        {
            var exerciseInput = new ExerciseInput
            {
                Name = exerciseName,
                SubCategoryId = subcategoryId,
            };
            var exerciseResponse = exerciseService.Add(exerciseInput);

            var favoriteInput = new FavoriteExerciseInput
            {
                ExerciseId = exerciseResponse.Result.Id,
                UserId = userId,
            };

            var favoriteResponse = favoriteExerciseService.Add(favoriteInput);
            Assert.True(favoriteResponse.Status == ResponseStatus.Success 
                && favoriteResponse.Result.ExerciseId == favoriteInput.ExerciseId
                && favoriteResponse.Result.UserId == favoriteInput.UserId);

            favoriteResponse = favoriteExerciseService.Add(favoriteInput);
            Assert.True(favoriteResponse.Status == ResponseStatus.AlreadyExists);
        }

        [Theory]
        [InlineData("exercise name", DatabaseFixture.FirstSubcategoryId, DatabaseFixture.NormalUserId)]
        public void Delete(string exerciseName, int subcategoryId, int userId) 
        {
            var exerciseInput = new ExerciseInput
            {
                Name = exerciseName,
                SubCategoryId = subcategoryId,
            };
            var exerciseResponse = exerciseService.Add(exerciseInput);

            var favoriteInput = new FavoriteExerciseInput
            {
                ExerciseId = exerciseResponse.Result.Id,
                UserId = userId,
            };

            var favoriteResponse = favoriteExerciseService.Add(favoriteInput);
            Assert.True(favoriteResponse.Status == ResponseStatus.Success);

            var deleteResponse = favoriteExerciseService.DeleteByExerciseId(favoriteInput.UserId, favoriteInput.ExerciseId);
            Assert.True(deleteResponse.Status == ResponseStatus.Success);
        }

        [Theory]
        [InlineData(DatabaseFixture.NormalUserId, DatabaseFixture.FirstExerciseId)]
        public void GetFavorites(int userId, int exerciseId)
        {
            var response = favoriteExerciseService.GetAll(a => a.UserId == userId);
            Assert.True(response.Status == ResponseStatus.Success
                && response.Result.Any(a => a.Exercise.Id == exerciseId && a.UserId == userId));
        }
    }
}
