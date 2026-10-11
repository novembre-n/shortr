using System.Data;
using Shortr.Persistence;

namespace Shortr.UnitTests;

public sealed class SqliteConnectionFactoryTests : IDisposable
{
    private readonly TestDatabase _database = new();

    [Fact]
    public void Create_FromConfiguredConnectionString_ReturnsOpenableConnection()
    {
        // Arrange — a factory wired to this test's temp database
        var factory = _database.CreateFactory();

        // Act — the one behavior under test: hand out an openable connection
        using var connection = factory.Create();
        connection.Open();

        // Assert
        Assert.Equal(ConnectionState.Open, connection.State);
    }

    public void Dispose() => _database.Dispose();
}