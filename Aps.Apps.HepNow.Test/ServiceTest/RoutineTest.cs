namespace Aps.Apps.HepNow.Test.ServiceTest
{
    public class RoutineTest: TestBase
    {
        private readonly IRoutineService routineService;

        public RoutineTest(IRoutineService routineService)
        {
            this.routineService = routineService;
        }

        [Theory]
        [InlineData("unique Routine name for create", "description", "/url", 1, HoldType.Sec, 2, PerformType.Hour, 3, 4, "note", DatabaseFixture.NormalUserId, DatabaseFixture.FirstExerciseId)]
        public void Create(string name, string description, string fileUrl, int hold, HoldType holdType, int perform, PerformType performType
            , int reps, int set, string note, int userId, int exerciseId)
        {
            var input = new RoutineInput
            {
                Name = name,
                Description = description,
                FileUrl = fileUrl,
                UserId = userId,
                RoutineExercises = new List<RoutineExerciseInput>
                {
                    new RoutineExerciseInput
                    {
                        ExerciseId = exerciseId,
                        Hold = hold,
                        HoldType = holdType,
                        Perform = perform,
                        PerformType = performType,
                        Note = note,
                        Reps = reps,
                        Set = set,
                    }
                }
            };

            var exerciseInput = input.RoutineExercises.First();

            var response = routineService.Add(input);
            Assert.True(response.Status == ResponseStatus.Success
                && response.Result.Id > 0
                && response.Result.Name == input.Name
                && response.Result.Description == input.Description
                && response.Result.FileUrl == input.FileUrl
                && response.Result.UserId == input.UserId
                && response.Result.RoutineExercises.Any()
                && response.Result.RoutineExercises.First().ExerciseId == exerciseInput.ExerciseId
                && response.Result.RoutineExercises.First().Hold == exerciseInput.Hold
                && response.Result.RoutineExercises.First().HoldType == exerciseInput.HoldType
                && response.Result.RoutineExercises.First().Perform == exerciseInput.Perform
                && response.Result.RoutineExercises.First().PerformType == exerciseInput.PerformType
                && response.Result.RoutineExercises.First().Note == exerciseInput.Note
                && response.Result.RoutineExercises.First().Reps == exerciseInput.Reps
                && response.Result.RoutineExercises.First().Set == exerciseInput.Set);
        }

        [Theory]
        [InlineData("unique Routine name for update", "description", "/url", 1, HoldType.Sec, 2, PerformType.Hour, 3, 4, "note", DatabaseFixture.NormalUserId, DatabaseFixture.FirstExerciseId)]
        public void Update(string name, string description, string fileUrl, int hold, HoldType holdType, int perform, PerformType performType
            , int reps, int set, string note, int userId, int exerciseId)
        {
            var routine = new Routines 
            {
                Name = Guid.NewGuid().ToString(),
                UserId = userId,
            };
            var addResponse = routineService.Add(routine);

            var input = new RoutineInput
            {
                Id = addResponse.Result.Id,
                Name = name,
                Description = description,
                FileUrl = fileUrl,
                UserId = userId,
                RoutineExercises = new List<RoutineExerciseInput>
                {
                    new RoutineExerciseInput
                    {
                        ExerciseId = exerciseId,
                        Hold = hold,
                        HoldType = holdType,
                        Perform = perform,
                        PerformType = performType,
                        Note = note,
                        Reps = reps,
                        Set = set,
                    }
                }
            };

            var exerciseInput = input.RoutineExercises.First();

            var response = routineService.Update(input);
            Assert.True(response.Status == ResponseStatus.Success
                && response.Result.Id == input.Id.Value
                && response.Result.Name == input.Name
                && response.Result.Description == input.Description
                && response.Result.FileUrl == input.FileUrl
                && response.Result.UserId == input.UserId
                && response.Result.RoutineExercises.Any()
                && response.Result.RoutineExercises.First().ExerciseId == exerciseInput.ExerciseId
                && response.Result.RoutineExercises.First().Hold == exerciseInput.Hold
                && response.Result.RoutineExercises.First().HoldType == exerciseInput.HoldType
                && response.Result.RoutineExercises.First().Perform == exerciseInput.Perform
                && response.Result.RoutineExercises.First().PerformType == exerciseInput.PerformType
                && response.Result.RoutineExercises.First().Note == exerciseInput.Note
                && response.Result.RoutineExercises.First().Reps == exerciseInput.Reps
                && response.Result.RoutineExercises.First().Set == exerciseInput.Set);
        }

        [Theory]
        [InlineData("unique Routine name for deduped exercise update", "description", "/url", 1, HoldType.Sec, 2, PerformType.Hour, 3, 4, "note", DatabaseFixture.NormalUserId, DatabaseFixture.FirstExerciseId)]
        public void Update_WithExerciseAlreadyDeduplicated_SavesSingleEntry(string name, string description, string fileUrl, int hold, HoldType holdType, int perform, PerformType performType
            , int reps, int set, string note, int userId, int exerciseId)
        {
            // Simulates what RoutineMutations.Update passes to the service after
            // collapsing duplicate exerciseIds from the raw client input.
            var routine = new Routines
            {
                Name = Guid.NewGuid().ToString(),
                UserId = userId,
            };
            var addResponse = routineService.Add(routine);

            var input = new RoutineInput
            {
                Id = addResponse.Result.Id,
                Name = name,
                Description = description,
                FileUrl = fileUrl,
                UserId = userId,
                RoutineExercises = new List<RoutineExerciseInput>
                {
                    new RoutineExerciseInput
                    {
                        ExerciseId = exerciseId,
                        Hold = hold,
                        HoldType = holdType,
                        Perform = perform,
                        PerformType = performType,
                        Note = note,
                        Reps = reps,
                        Set = set,
                    }
                }
            };

            var response = routineService.Update(input);
            Assert.True(response.Status == ResponseStatus.Success
                && response.Result.RoutineExercises.Count(re => re.ExerciseId == exerciseId) == 1);
        }

        [Theory]
        [InlineData(DatabaseFixture.NormalUserId)]
        public void Delete(int userId)
        {
            var Routine = new Routines
            {
                Name = Guid.NewGuid().ToString(),
                UserId = userId 
            };
            var addResponse = routineService.Add(Routine);
            Assert.True(addResponse.Status == ResponseStatus.Success && addResponse.Result.Id > 0);

            var deleteResponse = routineService.Remove(addResponse.Result.Id);
            Assert.Equal(deleteResponse.Status, ResponseStatus.Success);
        }

        [Theory]
        [InlineData(DatabaseFixture.FirstRoutineId, DatabaseFixture.NormalUserId)]
        public void Get(int routineId, int userId)
        {
            var response = routineService.GetRoutine(routineId, userId);
            Assert.True(response.Status == ResponseStatus.Success
                && response.Result.Id == routineId);
        }

        [Theory]
        [InlineData(DatabaseFixture.NormalUserId)]
        public void GetAll(int userId)
        {
            var response = routineService.GetAll(a => a.UserId == userId);
            Assert.True(response.Status == ResponseStatus.Success
                && response.Result.Count() > 0);
        }
    }
}
