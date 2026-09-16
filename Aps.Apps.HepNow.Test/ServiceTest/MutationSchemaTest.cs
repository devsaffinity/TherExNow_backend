using Aps.Apps.HepNow.Extensions;
using Aps.Apps.HepNow.GraphQL.ObjectTypes;
using HotChocolate;
using HotChocolate.Execution;
using HotChocolate.Types.Pagination;
using Microsoft.Extensions.DependencyInjection;

namespace Aps.Apps.HepNow.Test.ServicesTest
{
    public class MutationSchemaTest
    {
        private static readonly Lazy<Task<ISchema>> Schema = new(BuildSchemaAsync);

        /// <summary>Builds the schema the way IServiceExtensions.AddServices does, without a database or HTTP.</summary>
        private static async Task<ISchema> BuildSchemaAsync()
        {
            var services = new ServiceCollection();
            services.AddLogging();

            var graphql = services.AddGraphQLServer()
                .AddQueryType<QueryType>()
                .AddMutationType<MutationType>()
                .AddSubscriptionType<SubscriptionType>()
                .AddFiltering()
                .AddSorting()
                .AddProjections()
                .AddAuthorization()
                .AddInMemorySubscriptions()
                .ConfigureSchema(schema => schema.SetPagingOptions(new PagingOptions { InferCollectionSegmentNameFromField = false }));

            IServiceExtensions.AddQueryMutationSubscription(services, graphql);

            var executor = await services.BuildServiceProvider()
                .GetRequiredService<IRequestExecutorResolver>()
                .GetRequestExecutorAsync();
            return executor.Schema;
        }

        [Fact]
        public async Task NoMutationIsServedByTheBaseLibraryDefaults()
        {
            // MutationBase's Create/Update/Delete only check that the caller is signed in. Any of them
            // a mutation class leaves unhidden becomes a root field -- that is how `delete(entityId)`
            // let any member hard-delete any user -- so every class must hide them with [GraphQLIgnore].
            var schema = await Schema.Value;

            var inherited = schema.MutationType!.Fields
                .Where(f => f.ResolverMember?.DeclaringType?.Assembly.GetName().Name?.StartsWith("Aps.CommonBack") == true)
                .Select(f => f.Name);

            Assert.Empty(inherited);
        }

        [Theory]
        [InlineData("user_deleteUser", "userId")]
        [InlineData("category_deleteCategory", "entityId")]
        [InlineData("exercise_deleteExercise", "entityId")]
        [InlineData("exerciseProduct_deleteExerciseProduct", "entityId")]
        [InlineData("favoriteExercise_deleteFavoriteExercise", "exerciseId")]
        [InlineData("plan_deletePlan", "entityId")]
        [InlineData("product_deleteProduct", "entityId")]
        [InlineData("shoppingCart_deleteShoppingCart", "entityId")]
        public async Task DeleteMutationsKeepTheNamesTheClientsCall(string field, string argument)
        {
            var schema = await Schema.Value;

            Assert.True(schema.MutationType!.Fields.TryGetField(field, out var mutation), $"{field} is missing from the schema");
            Assert.Contains(mutation.Arguments, a => a.Name == argument);
        }
    }
}
