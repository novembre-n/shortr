namespace Shortr.Domain;

public class Link
{
    public long Id { get; set; }
    public required string Code { get; set; }
    public required string OriginalUrl { get; set; }

    // SQLite has no datetime type; stored as ISO-8601 text, which sorts correctly as strings.
    public DateTime CreatedAtUtc { get; set; }
}