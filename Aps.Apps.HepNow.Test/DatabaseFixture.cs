using Aps.Apps.HepNow.Data;
using Microsoft.EntityFrameworkCore;

namespace Aps.Apps.HepNow.Test
{
    public class DatabaseFixture : IDisposable
    {
        private readonly DataContext context;
        public const int AdminUserId = 1;
        public const int NormalUserId = 2;
        public const int FirstPlanId = 1;
        public const int FirstProductId = 1;
        public const int FirstCategoryId = 1;
        public const int FirstSubcategoryId = 1;
        public const int FirstExerciseId = 1;
        public const int FirstRoutineId = 1;
        public const int FirstRequestId = 1;
        public const int FirstNotificationId = 1;

        public DatabaseFixture(IDbContextFactory<DataContext> contextFactory)
        {
            context = contextFactory.CreateDbContext();
            context.Database.EnsureDeleted();
            context.Database.EnsureCreated();
            Init();
        }

        public void Dispose()
        {
            context.Database.EnsureDeleted();
        }

        private void Init()
        {
            var admin = new Users
            {
                UserType = UserType.Admin,
                FullName = "James Ward",
                PhoneNumber = "+16185443149",
                City = "Seattle",
                State = "Washington",
                ZipCode = "98119",
                Address = "1115 Church Street",
                Occupation = "sample Occupation",
                Email = "JamesDWard@teleworm.us",
                Gender = Gender.Male,
                Language = "English",
                ExternalId = "fwejk345k34lka",
                PhotoUrl = "/url1",
                YearsOfExperience = 4,
                PlatformType = PlatformType.Web,
                SentEmailCount = 1,
                SentSmsCount = 1,
            };
            context.Add(admin);
            context.SaveChanges();

            var normalUser = new Users
            {
                UserType = UserType.NormalUser,
                FullName = "Julie Scott",
                PhoneNumber = "+13046483799",
                City = "Fort Gay",
                State = "West Virginia",
                ZipCode = "25514",
                Address = "2067 Hall Valley Drive",
                Occupation = "sample Occupation",
                Email = "JulieJScott@armyspy.com",
                Gender = Gender.Female,
                Language = "Spanish",
                ExternalId = "wef3546lnfg",
                PhotoUrl = "/url2",
                YearsOfExperience = 1,
                PlatformType = PlatformType.App,
                SentEmailCount = 5,
                SentSmsCount = 3,
            };
            context.Add(normalUser);
            context.SaveChanges();

            context.Add(new Categories
            {
                Name = "Elbow and hand",
                SubCategories = new List<SubCategories>
                {
                    new SubCategories 
                    {
                        Name = "Isometric"
                    } 
                }
            });
            context.SaveChanges();

            context.Add(new Exercises
            {
                Hold = 1,
                HoldType = HoldType.Min,
                Note = "sample note",
                Perform = 2,
                PerformType = PerformType.Day,
                PhotoUrl = "/url1",
                OtherMediaUrl = "['/url2']",
                Name = "exercise1",
                Reps = 3,
                Set = 4,
                SubCategoryId = FirstSubcategoryId,
                FavoriteExercises = new List<FavoriteExercises>
                {
                    new FavoriteExercises
                    {
                        UserId = NormalUserId
                    }
                },
            });
            context.SaveChanges();

            context.Add(new Routines
            {
                Name = "routine name",
                Description = "sample description",
                FileUrl = "/url1",
                UserId = NormalUserId,
                RoutineExercises = new List<RoutineExercises> 
                {
                    new RoutineExercises 
                    {
                        ExerciseId = FirstExerciseId,
                        Hold = 1,
                        HoldType = HoldType.Min,
                        Note = "note",
                        Perform = 2,
                        PerformType = PerformType.Week,
                        Reps = 3,
                        Set = 4,
                    }
                }
            });
            context.SaveChanges();


            context.Add(new Products
            {
                Name = "Ball",
                Colors = "['red', 'blue', 'green']",
                Description = "sample description",
                PhotoUrl = "/url1",
                OtherMediaUrl = "['/url2', '/url3']",
                Price = 10,
            });
            context.SaveChanges();

            context.Add(new ShoppingCart
            {
                Count = 3,
                ProductId = FirstProductId,
                Color = "red",
                UserId = NormalUserId,
            });
            context.SaveChanges();

            context.Add(new Plans
            {
                Title = "Premium",
                Description = "sample description",
                Features = "{}",
                PriceMonth = 0,
                PriceYear = 0,
                Memberships = new List<Memberships> 
                { 
                    new Memberships
                    {
                        PaymentAmount = 20, 
                        UserId = AdminUserId,
                        StartDate = DateTime.UtcNow,
                        EndDate = DateTime.UtcNow.AddMonths(1),
                    }
                }
            });
            context.SaveChanges();

            context.Add(new Requests
            {
                Address = "3718 Chenoweth Drive",
                City = "Nashville",
                State = "Tennessee",
                ZipCode = "37211",
                UserId = NormalUserId,
                RequestStatus = RequestStatus.Waiting,
                RequestProducts = new List<RequestProducts>
                {
                    new RequestProducts
                    { 
                        Color = "blue",
                        Count = 3,
                        Price = 10,
                        ProductId = FirstProductId,
                    }
                }
            });
            context.SaveChanges();

            context.Add(new Notification
            {
                UserId = NormalUserId,
                NotificationType = NotificationType.RequestCreated,
                RequestId = FirstRequestId,
                IsRead = false
            });
            context.SaveChanges();
        }
    }
}
