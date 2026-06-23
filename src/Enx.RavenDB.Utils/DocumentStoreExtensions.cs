using Raven.Client.Documents;
using Raven.Client.Documents.Operations;
using Raven.Client.Exceptions;
using Raven.Client.Exceptions.Database;
using Raven.Client.ServerWide;
using Raven.Client.ServerWide.Operations;

namespace Enx.RavenDB.Utils;

/// <summary>
/// Utility extensions for <see cref="IDocumentStore"/>.
/// </summary>
public static class DocumentStoreExtensions
{
    /// <summary>
    /// Ensures a database with the given name exists, creating it if necessary.
    /// </summary>
    /// <param name="store">The document store.</param>
    /// <param name="database">
    /// The database name. When <see langword="null"/>, the store's default
    /// <see cref="IDocumentStore.Database"/> is used.
    /// </param>
    /// <param name="token">A cancellation token.</param>
    /// <returns><see langword="true"/> if the database was created; otherwise <see langword="false"/>.</returns>
    public static async Task<bool> EnsureDatabaseExistsAsync(
        this IDocumentStore store,
        string? database = null,
        CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(store);

        database ??= store.Database;
        ArgumentException.ThrowIfNullOrEmpty(database);

        try
        {
            await store.Maintenance.ForDatabase(database)
                .SendAsync(new GetStatisticsOperation(), token)
                .ConfigureAwait(false);
            return false;
        }
        catch (DatabaseDoesNotExistException)
        {
            try
            {
                await store.Maintenance.Server
                    .SendAsync(new CreateDatabaseOperation(new DatabaseRecord(database)), token)
                    .ConfigureAwait(false);
                return true;
            }
            catch (ConcurrencyException)
            {
                // The database was created concurrently between our check and create. That is fine.
                return false;
            }
        }
    }
}
