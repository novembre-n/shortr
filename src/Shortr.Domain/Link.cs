namespace Shortr.Domain;

public class Link
{
    public long Id { get; set; }
    public required string Code { get; set; }
    public required string OriginalUrl { get; set; }

    // SQLite has no datetime type. The value is stored as ISO-8601 text.
    // ISO-8601 text sorts in the correct order as a string.
    public DateTime CreatedAtUtc { get; set; }
}