using Xunit;

namespace Aps.Apps.HepNow.Test
{
    [CollectionDefinition("Database Collection", DisableParallelization = true)]
    public class DatabaseCollection : ICollectionFixture<DatabaseFixture> { }
}
