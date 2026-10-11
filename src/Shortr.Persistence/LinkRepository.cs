using System.Globalization;
using Dapper;
using Microsoft.Data.Sqlite;
using Shortr.Application;
using Shortr.Domain;

namespace Shortr.Persistence;

public sealed class LinkRepository : ILinkRepository
{
    // The default DateTime parse of Dapper loses the UTC flag. A SQLite TEXT
    // column has no timezone marker. This class maps the date column as a
    // string. This class then parses the string as UTC. The write side
    // stores the ISO-8601 format of a UTC value. The text is therefore UTC.
    private sealed class LinkRow
    {
        public string Code { get; set; } = null!;
        public string OriginalUrl { get; set; } = null!;
        public string CreatedAtUtc { get; set; } = null!;
    }

    private readonly SqliteConnectionFactory _factory;

    public LinkRepository(SqliteConnectionFactory factory) => _factory = factory;

    public async Task<bool> AddAsync(Link link, CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO Links (Code, OriginalUrl, CreatedAtUtc)
            VALUES (@Code, @OriginalUrl, @CreatedAtUtc);
            """;

        using var connection = _factory.Create();
        await connection.OpenAsync(cancellationToken);
        try
        {
            var command = new CommandDefinition(sql, new
            {
                Code = link.Code,
                OriginalUrl = link.OriginalUrl,
                // Use the ISO-8601 format "o". The text then sorts in time order
                // and parses without loss.
                CreatedAtUtc = link.CreatedAtUtc.ToString("o", CultureInfo.InvariantCulture),
            }, cancellationToken: cancellationToken);
            await connection.ExecuteAsync(command);
            return true;
        }
        catch (SqliteException ex) when (ex.SqliteErrorCode == 19)
        {
            // SQLITE_CONSTRAINT: the code exists already. The caller then
            // generates a new code.
            return false;
        }
    }

    public async Task<Link?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT Id, Code, OriginalUrl, CreatedAtUtc
            FROM Links
            WHERE Code = @code;
            """;

        using var connection = _factory.Create();
        await connection.OpenAsync(cancellationToken);

        var command = new CommandDefinition(sql, new { code }, cancellationToken: cancellationToken);
        var row = await connection.QuerySingleOrDefaultAsync<LinkRow>(command);

        if (row is null)
        {
            return null;
        }

        var createdAt = DateTime.Parse(
            row.CreatedAtUtc,
            CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);

        return new Link
        {
            Code = row.Code,
            OriginalUrl = row.OriginalUrl,
            CreatedAtUtc = createdAt,
        };
    }
}