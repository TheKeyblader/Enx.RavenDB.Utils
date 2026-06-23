using Xunit;

namespace Enx.RavenDB.Utils.Tests;

public class DocumentStoreExtensionsTests : RavenTestBase
{
    [Fact]
    public async Task EnsureDatabaseExistsAsync_WhenDatabaseExists_ReturnsFalse()
    {
        using var store = GetDocumentStore();

        Assert.False(await store.EnsureDatabaseExistsAsync());
    }

    [Fact]
    public async Task EnsureDatabaseExistsAsync_WhenDatabaseMissing_CreatesItOnce()
    {
        using var store = GetDocumentStore();
        var database = "ensure-" + Guid.NewGuid().ToString("N");

        Assert.True(await store.EnsureDatabaseExistsAsync(database));
        Assert.False(await store.EnsureDatabaseExistsAsync(database));
    }
}
