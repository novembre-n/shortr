using Shortr.Persistence;

namespace Shortr.UnitTests;

public sealed class DatabaseInitializerTests : IDisposable
{
    private readonly TestDatabase _database = new();

    private long CountSchemaObjects(string name)
    {
        // sqlite_master holds the catalog: one row per table/index in the file.
        // This is SQLite's own way of answering "does this object exist?".
        var factory = _database.CreateFactory();
        using var connection = factory.Create();
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText =
            "SELECT COUNT(*) FROM sqlite_master WHERE name = $name";
        command.Parameters.AddWithValue("$name", name);

        var result = command.ExecuteScalar();
        return (long)result!;
    }

    [Fact]
    public void Initialize_CreatesLinksTable()
    {
        var initializer = new DatabaseInitializer(_database.CreateFactory());

        initializer.Initialize();

        Assert.Equal(1, CountSchemaObjects("Links"));
    }

    [Fact]
    public void Initialize_CreatesIndexOnCode()
    {
        var initializer = new DatabaseInitializer(_database.CreateFactory());

        initializer.Initialize();

        Assert.Equal(1, CountSchemaObjects("idx_links_code"));
    }

    [Fact]
    public void Initialize_Twice_DoesNotFail()
    {
        var initializer = new DatabaseInitializer(_database.CreateFactory());

        initializer.Initialize();
        initializer.Initialize();

        Assert.Equal(1, CountSchemaObjects("Links"));
    }

    public void Dispose() => _database.Dispose();
}