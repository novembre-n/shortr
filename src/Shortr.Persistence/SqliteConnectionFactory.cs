using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;

namespace Shortr.Persistence;

// The caller opens the connection. The caller disposes the connection.
// A SQLite connection is a handle to a file. The open operation is cheap.
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