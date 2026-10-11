using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;

namespace Shortr.Persistence;

// Caller opens and disposes the connection. Opening a SQLite connection is cheap (it's just a file).
public sealed class SqliteConnectionFactory
{
    private readonly string _connectionString;

    public SqliteConnectionFactory(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException(
                "Connection string 'Default' is not configured.");
    }

    public SqliteConnection Create() => new(_connectionString);
}