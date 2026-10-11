using Microsoft.Extensions.Configuration;
using Shortr.Persistence;

namespace Shortr.UnitTests;

// This class gives one disposable SQLite database to each test.
// The class holds a unique temp file path. The method CreateFactory builds
// a real SqliteConnectionFactory that points to this path. The method
// Dispose deletes the file. No test writes to the dev database shortr.db.
// SQLite creates the file at the first query. The file does not exist
// before that query.
public sealed class TestDatabase : IDisposable
{
    private readonly string _filePath;

    public TestDatabase()
    {
        _filePath = Path.Combine(Path.GetTempPath(), $"shortr_test_{Guid.NewGuid():N}.db");
    }

    // This method builds the factory in the same way as production code.
    // The method uses IConfiguration for this purpose. In-memory
    // configuration takes the place of appsettings.json.
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
        // Delete the file. Do not throw on failure. Cleanup must not
        // cause a test to fail. The file may not exist or may be locked.
        try { File.Delete(_filePath); } catch { }
    }
}