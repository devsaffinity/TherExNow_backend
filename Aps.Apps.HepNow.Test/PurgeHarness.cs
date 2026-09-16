using System.Linq.Expressions;
using Aps.Apps.HepNow.Data;
using Aps.Apps.HepNow.Extensions;
using Aps.CommonBack.Base.Models.Entities;
using HotChocolate.Subscriptions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Aps.Apps.HepNow.Test
{
    /// <summary>
    /// A real relational database for account deletion, which needs transactions, ExecuteDelete and enforced
    /// foreign keys -- none of which the in-memory provider has. SQLite by default. Set THEREXNOW_TEST_MYSQL to
    /// a server connection string without a database (a disposable local server, never a shared one) to run
    /// the same tests against MySQL; each harness creates and drops its own database there.
    /// Firebase, S3 and Stripe are always fakes, and no appsettings file is loaded, so nothing can reach a real
    /// service.
    /// </summary>
    public sealed class PurgeHarness : IAsyncDisposable
    {
        public const string MySqlVariable = "THEREXNOW_TEST_MYSQL";

        private readonly SqliteConnection? keepAlive;
        private readonly IServiceScope scope;

        public FakeFirebaseAccounts Firebase { get; } = new();
        public FakeMediaStorage Media { get; } = new();
        public FakeStripe Stripe { get; } = new();
        public ServiceProvider Provider { get; }
        public bool IsMySql { get; }

        public PurgeHarness(string env = "prd", string stripeKey = "sk_test_unit")
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddSingleton<IConfiguration>(new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["env"] = env,
                ["S3Credentials:BucketName"] = "therexnow-bucket",
            }).Build());

            var mysqlServer = Environment.GetEnvironmentVariable(MySqlVariable);
            if (string.IsNullOrEmpty(mysqlServer))
            {
                // A named shared-cache database lives as long as one connection to it stays open.
                var connectionString = $"Data Source=purge-{Guid.NewGuid():N};Mode=Memory;Cache=Shared;Foreign Keys=True";
                keepAlive = new SqliteConnection(connectionString);
                keepAlive.Open();
                services.AddPooledDbContextFactory<DataContext>(o => o.UseSqlite(connectionString));
            }
            else
            {
                IsMySql = true;
                var connectionString = $"{mysqlServer.TrimEnd(';')};Database=purge_{Guid.NewGuid():N}";
                services.AddPooledDbContextFactory<DataContext>(o => o.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 4, 0))));
            }

            services.AddHttpContextAccessor();
            IServiceExtensions.AddRepositoryAndService(services);
            services.AddScoped<ITopicEventSender, UnitTestTopicEventSender>();

            services.AddSingleton<IFirebaseAccounts>(Firebase);
            services.AddSingleton<IMediaStorage>(Media);
            services.AddSingleton(Stripe.Client(stripeKey));

            Provider = services.BuildServiceProvider();
            scope = Provider.CreateScope();

            using var ctx = NewContext();
            ctx.Database.EnsureCreated();
        }

        public IAccountPurgeService Purge => Resolve<IAccountPurgeService>();

        public T Resolve<T>() where T : notnull => scope.ServiceProvider.GetRequiredService<T>();

        public DataContext NewContext() => Provider.GetRequiredService<IDbContextFactory<DataContext>>().CreateDbContext();

        public async Task<Catalogue> SeedCatalogueAsync()
        {
            await using var ctx = NewContext();
            var subCategory = new SubCategories { Name = "Mobility", Category = new Categories { Name = "Knee" } };
            var exercise = new Exercises { Name = "Squat", SubCategory = subCategory, PhotoUrl = "a1b2c3d4-shared-demo.jpg", OtherMediaUrl = "['b2c3d4e5-demo.mp4']" };
            var product = new Products { Name = "Band", Price = 10 };
            var plan = new Plans { Title = "Free", PriceMonth = 0 };
            ctx.AddRange(exercise, product, plan);
            await ctx.SaveChangesAsync();
            return new Catalogue(exercise.Id, product.Id, plan.Id);
        }

        public async Task<int> SeedAdminAsync(string uid)
        {
            await using var ctx = NewContext();
            var admin = new Users { ExternalId = uid, Email = $"{uid}@example.com", UserType = UserType.Admin };
            ctx.Add(admin);
            await ctx.SaveChangesAsync();
            return admin.Id;
        }

        /// <summary>
        /// A member with every kind of row an account can own, including the soft-deleted children normal use
        /// leaves behind (an emptied cart, a removed favourite, an exercise dropped from a routine).
        /// </summary>
        public async Task<int> SeedMemberAsync(Catalogue catalogue, string uid, MemberOptions? options = null)
        {
            options ??= new MemberOptions();
            var now = DateTime.UtcNow;
            await using var ctx = NewContext();

            var user = new Users
            {
                ExternalId = uid,
                ExternalId2 = options.LinkedUid,
                Email = options.Email ?? $"{uid}@example.com",
                FullName = "Member " + uid,
                UserType = options.UserType,
                StripeCustomerId = options.StripeCustomerId,
                PhotoUrl = options.PhotoUrl,
                IsDeleted = options.SoftDeleted,
            };
            ctx.Add(user);
            await ctx.SaveChangesAsync();

            var request = new Requests
            {
                UserId = user.Id,
                Address = "1 Main Street",
                RequestStatus = options.WaitingOrder ? RequestStatus.Waiting : RequestStatus.Accepted,
                RequestProducts = new List<RequestProducts> { new() { ProductId = catalogue.ProductId, Count = 1, Price = 10 } },
            };
            ctx.AddRange(
                request,
                new Routines
                {
                    UserId = user.Id,
                    Name = "Knee plan",
                    FileUrl = options.RoutineFileUrl,
                    RoutineExercises = new List<RoutineExercises>
                    {
                        new() { ExerciseId = catalogue.ExerciseId },
                        new() { ExerciseId = catalogue.ExerciseId, IsDeleted = true },
                    },
                },
                new CustomizedExercise { UserId = user.Id, ExerciseId = catalogue.ExerciseId, OtherMediaUrl = options.CustomizedMedia },
                new FavoriteExercises { UserId = user.Id, ExerciseId = catalogue.ExerciseId },
                new FavoriteExercises { UserId = user.Id, ExerciseId = catalogue.ExerciseId, IsDeleted = true },
                new Memberships { UserId = user.Id, PlanId = catalogue.PlanId, StartDate = now, EndDate = now.AddYears(100), PurchaseToken = options.PurchaseToken },
                new ShoppingCart { UserId = user.Id, ProductId = catalogue.ProductId, Count = 1 },
                new ShoppingCart { UserId = user.Id, ProductId = catalogue.ProductId, Count = 2, IsDeleted = true },
                new UserActiveDay { UserId = user.Id, Day = now.Date },
                new PrintData { UserId = user.Id, Data = "{\"exercises\":[]}" });
            await ctx.SaveChangesAsync();

            ctx.Add(new Notification { UserId = user.Id, NotificationType = NotificationType.FreePlanPurchased });
            if (options.NotifyAdminId != null)
                ctx.Add(new Notification { UserId = options.NotifyAdminId.Value, RequestId = request.Id, NotificationType = NotificationType.RequestCreated });
            await ctx.SaveChangesAsync();

            return user.Id;
        }

        /// <summary>Rows per table that belong to the account, soft-deleted ones included.</summary>
        public async Task<Dictionary<string, int>> RowsOwnedByAsync(int userId)
        {
            await using var ctx = NewContext();
            var requestIds = await ctx.Set<Requests>().IgnoreQueryFilters().Where(r => r.UserId == userId).Select(r => r.Id).ToListAsync();
            var routineIds = await ctx.Set<Routines>().IgnoreQueryFilters().Where(r => r.UserId == userId).Select(r => r.Id).ToListAsync();

            return new Dictionary<string, int>
            {
                ["Users"] = await ctx.Set<Users>().IgnoreQueryFilters().CountAsync(u => u.Id == userId),
                ["Requests"] = requestIds.Count,
                ["RequestProducts"] = await ctx.Set<RequestProducts>().IgnoreQueryFilters().CountAsync(p => requestIds.Contains(p.RequestId)),
                ["Notification"] = await ctx.Set<Notification>().IgnoreQueryFilters().CountAsync(n => n.UserId == userId || (n.RequestId != null && requestIds.Contains(n.RequestId.Value))),
                ["Routines"] = routineIds.Count,
                ["RoutineExercises"] = await ctx.Set<RoutineExercises>().IgnoreQueryFilters().CountAsync(e => routineIds.Contains(e.RoutineId)),
                ["CustomizedExercise"] = await ctx.Set<CustomizedExercise>().IgnoreQueryFilters().CountAsync(c => c.UserId == userId),
                ["FavoriteExercises"] = await ctx.Set<FavoriteExercises>().IgnoreQueryFilters().CountAsync(f => f.UserId == userId),
                ["Memberships"] = await ctx.Set<Memberships>().IgnoreQueryFilters().CountAsync(m => m.UserId == userId),
                ["ShoppingCart"] = await ctx.Set<ShoppingCart>().IgnoreQueryFilters().CountAsync(c => c.UserId == userId),
                ["UserActiveDay"] = await ctx.Set<UserActiveDay>().IgnoreQueryFilters().CountAsync(d => d.UserId == userId),
                ["PrintData"] = await ctx.Set<PrintData>().IgnoreQueryFilters().CountAsync(p => p.UserId == userId),
            };
        }

        public async Task<int> CountAsync<T>(Expression<Func<T, bool>> predicate) where T : EntityDef
        {
            await using var ctx = NewContext();
            return await ctx.Set<T>().IgnoreQueryFilters().CountAsync(predicate);
        }

        public async Task<T?> FindAsync<T>(int id) where T : EntityDef
        {
            await using var ctx = NewContext();
            return await ctx.Set<T>().IgnoreQueryFilters().AsNoTracking().FirstOrDefaultAsync(e => e.Id == id);
        }

        public async Task ExecuteSqlAsync(string sql)
        {
            await using var ctx = NewContext();
            await ctx.Database.ExecuteSqlRawAsync(sql);
        }

        public async ValueTask DisposeAsync()
        {
            if (IsMySql)
            {
                await using var ctx = NewContext();
                await ctx.Database.EnsureDeletedAsync();
            }

            scope.Dispose();
            await Provider.DisposeAsync();
            keepAlive?.Dispose();
        }
    }

    public sealed record Catalogue(int ExerciseId, int ProductId, int PlanId);

    public sealed class MemberOptions
    {
        public string? Email { get; init; }
        public string? LinkedUid { get; init; }
        public UserType UserType { get; init; } = UserType.NormalUser;
        public bool SoftDeleted { get; init; }
        public bool WaitingOrder { get; init; }
        public string? StripeCustomerId { get; init; }
        public string? PhotoUrl { get; init; }
        public string? RoutineFileUrl { get; init; }
        public string? CustomizedMedia { get; init; }
        public string? PurchaseToken { get; init; }
        public int? NotifyAdminId { get; init; }
    }
}
