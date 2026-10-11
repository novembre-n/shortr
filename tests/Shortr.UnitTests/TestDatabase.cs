using Microsoft.Extensions.Configuration;
using Shortr.Persistence;

namespace Shortr.UnitTests;

// One disposable test database per test:
// a unique temp file path, a real SqliteConnectionFactory pointed at it,
// and deletion on dispose. No test touches the dev shortr.db or another
// test's data. SQLite creates the file lazily at the first query.
public sealed class TestDatabase : IDisposable
{
    private readonly string _filePath;

    public TestDatabase()
    {
        _filePath = Path.Combine(Path.GetTempPath(), $"shortr_test_{Guid.NewGuid():N}.db");
    }

    // Builds the factory exactly as production builds it: from IConfiguration.
    // In-memory config stands in for appsettings.json.
    public SqliteConnectionFactory CreateFactory()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = $"Data Source={_filePath}",
            })
            .Build();

        return new SqliteConnectionFactory(configuration);
    }

    public void Dispose()
    {
        // Best effort — cleanup must never fail a test.
        try { File.Delete(_filePath); } catch { /* file may not exist or be locked */ }
    }
}