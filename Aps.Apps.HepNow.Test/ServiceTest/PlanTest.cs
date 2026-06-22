namespace Aps.Apps.HepNow.Test.ServiceTest
{
    public class PlanTest: TestBase
    {
        private readonly IPlanService planService;

        public PlanTest(IPlanService planService)
        {
            this.planService = planService;
        }

        [Theory]
        [InlineData("unique plan name for create", "description", "{ 'allowShare' : true}", 10)]
        public void Create(string title, string description, string features, decimal price)
        {
            var input = new PlanInput
            {
                Title = title,
                Description = description,
                Features = features,
                PriceMonth = price,
                PriceYear = price * 12
            };

            var response = planService.Add(input);
            Assert.True(response.Status == ResponseStatus.Success
                && response.Result.Id > 0
                && response.Result.Title == title
                && response.Result.Description == description
                && response.Result.Features == features
                && response.Result.PriceMonth == price);
        }

        [Theory]
        [InlineData("unique plan name for update", "description", "{ 'allowShare' : true}", 10)]
        public void Update(string title, string description, string features, decimal price)
        {
            var plan = new Plans { Title = Guid.NewGuid().ToString() };
            var response = planService.Add(plan);
            Assert.True(response.Status == ResponseStatus.Success && response.Result.Id > 0);

            var input = new PlanInput
            {
                Id = response.Result.Id,
                Title = title,
                Description = description,
                Features = features,
                PriceMonth = price
            };

            response = planService.Update(input);
            Assert.True(response.Status == ResponseStatus.Success
                && response.Result.Id > 0
                && response.Result.Title == title
                && response.Result.Description == description
                && response.Result.Features == features
                && response.Result.PriceMonth == price);
        }

        [Fact]
        public void Delete() 
        {
            var plan = new Plans { Title = Guid.NewGuid().ToString() };
            var addResponse = planService.Add(plan);
            Assert.True(addResponse.Status == ResponseStatus.Success && addResponse.Result.Id > 0);

            var deleteResponse = planService.Remove(addResponse.Result.Id);
            Assert.Equal(deleteResponse.Status, ResponseStatus.Success);
        }

        [Theory]
        [InlineData(DatabaseFixture.FirstPlanId)]
        public void GetAll(int planId)
        {
            var response = planService.GetAll(a => true);
            Assert.True(response.Status == ResponseStatus.Success
                && response.Result.Any(a => a.Id == planId));
        }
    }
}
