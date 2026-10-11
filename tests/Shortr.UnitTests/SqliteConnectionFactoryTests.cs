using System.Data;
using Shortr.Persistence;

namespace Shortr.UnitTests;

public sealed class SqliteConnectionFactoryTests : IDisposable
{
    private readonly TestDatabase _database = new();

    [Fact]
    public void Create_FromConfiguredConnectionString_ReturnsOpenableConnection()
    {
        // Arrange: build a factory that points to the temp database of this test.
        var factory = _database.CreateFactory();

        // Act: get a connection. Open the connection.
        using var connection = factory.Create();
        connection.Open();

        // Assert: the state of the connection is Open.
        Assert.Equal(ConnectionState.Open, connection.State);
    }

    public void Dispose() => _database.Dispose();
}