using Xunit;

namespace Enx.RavenDB.Utils.Tests;

public class DocumentStoreExtensionsTests : RavenTestBase
{
    [Fact]
    public async Task EnsureDatabaseExistsAsync_WhenDatabaseExists_ReturnsFalse()
    {
        using var store = GetDocumentStore();
        var token = TestContext.Current.CancellationToken;

        Assert.False(await store.EnsureDatabaseExistsAsync(token: token));
    }

    [Fact]
    public async Task EnsureDatabaseExistsAsync_WhenDatabaseMissing_CreatesItOnce()
    {
        using var store = GetDocumentStore();
        var database = "ensure-" + Guid.NewGuid().ToString("N");
        var token = TestContext.Current.CancellationToken;

        Assert.True(await store.EnsureDatabaseExistsAsync(database, token));
        Assert.False(await store.EnsureDatabaseExistsAsync(database, token));
    }
}
