using Microsoft.Data.Sqlite;

namespace Shortr.Persistence;

// This method is idempotent. The file schema.sql uses IF NOT EXISTS.
// The method runs safely on every startup. The schema is embedded in the
// assembly. The method works in this way with any current directory:
// under dotnet run, in published output, and in tests.
public sealed class DatabaseInitializer
{
    private const string SchemaResourceName = "Shortr.Persistence.schema.sql";

    private readonly SqliteConnectionFactory _factory;

    public DatabaseInitializer(SqliteConnectionFactory factory) => _factory = factory;

    public void Initialize()
    {
        using var connection = _factory.Create();
        connection.Open();

        var schema = ReadEmbeddedSchema();
        using var command = connection.CreateCommand();
        command.CommandText = schema;
        command.ExecuteNonQuery();
    }

    private static string ReadEmbeddedSchema()
    {
        var assembly = typeof(DatabaseInitializer).Assembly;
        using var stream = assembly.GetManifestResourceStream(SchemaResourceName)
            ?? throw new InvalidOperationException(
                $"Embedded resource '{SchemaResourceName}' not found.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}