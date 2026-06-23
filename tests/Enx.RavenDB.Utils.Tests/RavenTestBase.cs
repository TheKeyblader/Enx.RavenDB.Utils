using Raven.TestDriver;

namespace Enx.RavenDB.Utils.Tests;

/// <summary>
/// Base class spinning up an embedded RavenDB server via <see cref="RavenTestDriver"/>.
/// Each <c>GetDocumentStore()</c> call yields an isolated, automatically-cleaned database.
/// </summary>
public abstract class RavenTestBase : RavenTestDriver
{
    static RavenTestBase()
    {
        var options = new TestServerOptions();

        // RavenDB 7.x requires a license to start. Run the embedded test server in
        // community mode rather than failing when none is configured.
        options.Licensing.ThrowOnInvalidOrMissingLicense = false;

        // Allow CI / developers to supply a (free) developer license when available.
        var license = Environment.GetEnvironmentVariable("RAVENDB_LICENSE");
        if (!string.IsNullOrWhiteSpace(license))
        {
            options.Licensing.License = license;
        }

        // Must be configured once, before the first embedded server starts.
        ConfigureServer(options);
    }
}
