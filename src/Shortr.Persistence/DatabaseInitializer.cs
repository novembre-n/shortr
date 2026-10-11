using Microsoft.Data.Sqlite;

namespace Shortr.Persistence;

// Idempotent: schema.sql uses IF NOT EXISTS, so this runs on every startup.
// The schema is embedded in the assembly so this works regardless of the
// current working directory (dotnet run, published output, tests).
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