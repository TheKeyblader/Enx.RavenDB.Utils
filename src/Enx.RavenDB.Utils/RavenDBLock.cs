using Raven.Client.Documents;
using Raven.Client.Documents.Operations.CompareExchange;

namespace Enx.RavenDB.Utils;

/// <summary>
/// A simple distributed mutual-exclusion lock built on top of RavenDB
/// <see href="https://ravendb.net/docs/article-page/latest/csharp/client-api/operations/compare-exchange/overview">
/// compare-exchange</see> values.
/// </summary>
/// <remarks>
/// <para>
/// The lock is represented by a single compare-exchange entry keyed by the resource name. Acquiring
/// the lock atomically creates that entry; releasing it (via the returned <see cref="IAsyncDisposable"/>)
/// deletes it. Because compare-exchange operations are linearizable across the cluster, only one caller
/// can hold the lock at any time.
/// </para>
/// <para>
/// Each acquisition records an expiry instant (<c>now + duration</c>). If the holder dies without
/// releasing the lock, another waiter is allowed to take it over once that instant has passed, which
/// prevents a crashed process from deadlocking the resource forever. Choose <c>duration</c> larger than
/// the longest expected critical section.
/// </para>
/// </remarks>
public static class SimpleRavenDBLock
{
    /// <summary>
    /// The payload stored in the compare-exchange entry backing the lock.
    /// </summary>
    private sealed class SharedResource
    {
        /// <summary>The UTC instant until which the lock is considered held.</summary>
        public DateTime? ReservedUntil { get; init; }
    }

    /// <summary>
    /// Releases the lock by deleting its compare-exchange entry when disposed.
    /// </summary>
    private sealed class Disposable(IDocumentStore store, string resourceName, long index) : IAsyncDisposable
    {
        /// <inheritdoc />
        public async ValueTask DisposeAsync() =>
            await store.Operations
                .SendAsync(new DeleteCompareExchangeValueOperation<SharedResource>(resourceName, index))
                .ConfigureAwait(false);
    }

    /// <summary>
    /// Acquires the distributed lock for <paramref name="resourceName"/>, asynchronously waiting until
    /// it becomes available.
    /// </summary>
    /// <param name="store">The document store connected to the cluster that arbitrates the lock.</param>
    /// <param name="resourceName">
    /// The compare-exchange key identifying the resource being guarded. Callers competing for the same
    /// resource must use the same key.
    /// </param>
    /// <param name="duration">
    /// How long the lock stays valid before it may be taken over by another waiter. Acts as a safety
    /// net against holders that crash without releasing the lock.
    /// </param>
    /// <param name="cancellationToken">A token used to stop waiting for the lock.</param>
    /// <returns>
    /// An <see cref="IAsyncDisposable"/> that releases the lock when disposed. Dispose it (ideally with
    /// <c>await using</c>) as soon as the critical section completes.
    /// </returns>
    /// <exception cref="OperationCanceledException">The wait was cancelled via <paramref name="cancellationToken"/>.</exception>
    public static async Task<IAsyncDisposable> CreateAsync(
        IDocumentStore store,
        string resourceName,
        TimeSpan duration,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentException.ThrowIfNullOrEmpty(resourceName);

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            DateTime now = DateTime.UtcNow;

            SharedResource resource = new()
            {
                ReservedUntil = now.Add(duration)
            };

            // Index 0 means "create only if the key does not already exist".
            CompareExchangeResult<SharedResource> putResult = await store.Operations
                .SendAsync(
                    new PutCompareExchangeValueOperation<SharedResource>(resourceName, resource, 0),
                    token: cancellationToken)
                .ConfigureAwait(false);

            if (putResult.Successful)
            {
                return new Disposable(store, resourceName, putResult.Index);
            }

            // The lock is already held. If it has expired, try to atomically take it over using the
            // current index so that only one waiter can win the race.
            if (putResult.Value?.ReservedUntil < now)
            {
                CompareExchangeResult<SharedResource> takeOverResult = await store.Operations
                    .SendAsync(
                        new PutCompareExchangeValueOperation<SharedResource>(resourceName, resource, putResult.Index),
                        token: cancellationToken)
                    .ConfigureAwait(false);

                if (takeOverResult.Successful)
                {
                    return new Disposable(store, resourceName, takeOverResult.Index);
                }
            }

            await Task.Delay(20, cancellationToken).ConfigureAwait(false);
        }
    }
}
