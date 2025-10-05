namespace Aps.Apps.HepNow.Test.ServiceTest
{
    public class ExerciseTest: TestBase
    {
        private readonly IExerciseService exerciseService;

        public ExerciseTest(IExerciseService exerciseService)
        {
            this.exerciseService = exerciseService;
        }

        [Theory]
        [InlineData("exercise name for create", 1, HoldType.Sec, 2, PerformType.Week, 3, 4, "note", "/url1", "['/url2']", DatabaseFixture.FirstSubcategoryId)]
        public void Create(string exerciseName, int hold, HoldType holdType, int perform, PerformType performType, int reps, int set
            , string note, string photoUrl, string otherMediaUrl, int subcategoryId)
        {
            var input = new ExerciseInput
            {
                Name = exerciseName,
                SubCategoryId = subcategoryId,
                Hold = hold,
                HoldType = holdType,
                Perform = perform,
                PerformType = performType,
                Note = note,
                PhotoUrl = photoUrl,
                OtherMediaUrl = otherMediaUrl,
                Reps = reps,
                Set = set,
            };

            var response = exerciseService.Add(input);
            Assert.True(response.Status == ResponseStatus.Success
                && response.Result.Id > 0
                && response.Result.Name == exerciseName
                && response.Result.SubCategoryId == subcategoryId
                && response.Result.Hold == hold
                && response.Result.HoldType == holdType
                && response.Result.Perform == perform
                && response.Result.PerformType == performType
                && response.Result.Note == note
                && response.Result.PhotoUrl == photoUrl
                && response.Result.OtherMediaUrl == otherMediaUrl
                && response.Result.Reps == reps
                && response.Result.Set == set);
        }

        [Theory]
        [InlineData("exercise name for update", 1, HoldType.Sec, 2, PerformType.Week, 3, 4, "note", "/url1", "['/url2']", DatabaseFixture.FirstSubcategoryId)]
        public void Update(string exerciseName, int hold, HoldType holdType, int perform, PerformType performType, int reps, int set
            , string note, string photoUrl, string otherMediaUrl, int subcategoryId)
        {
            var exercise = new Exercises
            {
                Name = "temp exercise name",
                SubCategoryId = subcategoryId,
            };

            var response = exerciseService.Add(exercise);
            Assert.True(response.Status == ResponseStatus.Success && response.Result.Id > 0);

            var input = new ExerciseInput
            {
                Id = response.Result.Id,
                Name = exerciseName,
                SubCategoryId = subcategoryId,
                Hold = hold,
                HoldType = holdType,
                Perform = perform,
                PerformType = performType,
                Note = note,
                PhotoUrl = photoUrl,
                OtherMediaUrl = otherMediaUrl,
                Reps = reps,
                Set = set,
            };

            response = exerciseService.Update(input);
            Assert.True(response.Status == ResponseStatus.Success
                && response.Result.Id > 0
                && response.Result.Name == exerciseName
                && response.Result.SubCategoryId == subcategoryId
                && response.Result.Hold == hold
                && response.Result.HoldType == holdType
                && response.Result.Perform == perform
                && response.Result.PerformType == performType
                && response.Result.Note == note
                && response.Result.PhotoUrl == photoUrl
                && response.Result.OtherMediaUrl == otherMediaUrl
                && response.Result.Reps == reps
                && response.Result.Set == set);
        }

        [Theory]
        [InlineData(DatabaseFixture.FirstSubcategoryId)]
        public void Delete(int subcategoryId) 
        {
            var exercises = new Exercises
            {
                Name = "temp exercise name",
                SubCategoryId = subcategoryId,
            };

            var addResponse = exerciseService.Add(exercises);
            Assert.True(addResponse.Status == ResponseStatus.Success && addResponse.Result.Id > 0);

            var deleteResponse = exerciseService.Remove(addResponse.Result.Id);
            Assert.Equal(deleteResponse.Status, ResponseStatus.Success);
        }

        [Theory]
        [InlineData(DatabaseFixture.FirstExerciseId, DatabaseFixture.NormalUserId)]
        public void GetExercises(int exerciseId, int userId)
        {
            var response = exerciseService.GetExercises(userId);
            Assert.True(response.Status == ResponseStatus.Success
                && response.Result.Any(a => a.Exercise.Id == exerciseId && a.FavoriteCount > 0 && a.IsFavorite == true));
        }
    }
}
