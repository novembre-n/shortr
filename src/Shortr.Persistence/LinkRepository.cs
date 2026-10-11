using System.Globalization;
using Dapper;
using Microsoft.Data.Sqlite;
using Shortr.Application;
using Shortr.Domain;

namespace Shortr.Persistence;

public sealed class LinkRepository : ILinkRepository
{
    // Dapper's default DateTime parse loses the UTC flag (SQLite TEXT has no
    // timezone marker). Mapping the date as string and parsing it ourselves
    // keeps Kind=Utc through the round-trip — the write side stores "o" of a
    // UTC value, so the text genuinely is UTC.
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
                // ISO-8601 ("o") so the text sorts chronologically and parses losslessly.
                CreatedAtUtc = link.CreatedAtUtc.ToString("o", CultureInfo.InvariantCulture),
            }, cancellationToken: cancellationToken);
            await connection.ExecuteAsync(command);
            return true;
        }
        catch (SqliteException ex) when (ex.SqliteErrorCode == 19)
        {
            // SQLITE_CONSTRAINT: code already taken — caller retries with a new code.
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