using Raven.Client.Documents.Operations.CompareExchange;
using Xunit;

namespace Enx.RavenDB.Utils.Tests;

public class SimpleRavenDBLockTests : RavenTestBase
{
    private static string NewResourceName() => "locks/" + Guid.NewGuid().ToString("N");

    [Fact]
    public async Task CreateAsync_WhenFree_AcquiresLock()
    {
        using var store = GetDocumentStore();
        var token = TestContext.Current.CancellationToken;
        var resource = NewResourceName();

        await using var handle = await SimpleRavenDBLock.CreateAsync(store, resource, TimeSpan.FromMinutes(5), token);

        var entry = await store.Operations.SendAsync(
            new GetCompareExchangeValueOperation<object>(resource), token: token);
        Assert.NotNull(entry);
    }

    [Fact]
    public async Task DisposeAsync_ReleasesLock()
    {
        using var store = GetDocumentStore();
        var token = TestContext.Current.CancellationToken;
        var resource = NewResourceName();

        var handle = await SimpleRavenDBLock.CreateAsync(store, resource, TimeSpan.FromMinutes(5), token);
        await handle.DisposeAsync();

        var entry = await store.Operations.SendAsync(
            new GetCompareExchangeValueOperation<object>(resource), token: token);
        Assert.Null(entry);
    }

    [Fact]
    public async Task CreateAsync_WhenHeld_BlocksUntilReleased()
    {
        using var store = GetDocumentStore();
        var token = TestContext.Current.CancellationToken;
        var resource = NewResourceName();

        var first = await SimpleRavenDBLock.CreateAsync(store, resource, TimeSpan.FromMinutes(5), token);

        var secondTask = SimpleRavenDBLock.CreateAsync(store, resource, TimeSpan.FromMinutes(5), token);

        // The second acquisition must not succeed while the first one is still held.
        await Task.Delay(300, token);
        Assert.False(secondTask.IsCompleted);

        await first.DisposeAsync();

        var second = await secondTask;
        await second.DisposeAsync();
    }

    [Fact]
    public async Task CreateAsync_WhenLockExpired_TakesOverAndReleasesCleanly()
    {
        using var store = GetDocumentStore();
        var token = TestContext.Current.CancellationToken;
        var resource = NewResourceName();

        // Acquire a short-lived lock and deliberately never release it (simulating a crashed holder).
        _ = await SimpleRavenDBLock.CreateAsync(store, resource, TimeSpan.FromMilliseconds(100), token);
        await Task.Delay(250, token);

        // A new waiter should take over the expired lock...
        var second = await SimpleRavenDBLock.CreateAsync(store, resource, TimeSpan.FromMinutes(5), token);
        // ...and release it cleanly, which only works if the take-over index is tracked correctly.
        await second.DisposeAsync();

        var entry = await store.Operations.SendAsync(
            new GetCompareExchangeValueOperation<object>(resource), token: token);
        Assert.Null(entry);
    }

    [Fact]
    public async Task CreateAsync_NullStore_Throws()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => SimpleRavenDBLock.CreateAsync(null!, "locks/1", TimeSpan.FromMinutes(1), TestContext.Current.CancellationToken));
    }
}
