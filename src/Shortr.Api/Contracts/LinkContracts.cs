namespace Shortr.Api.Contracts;

public sealed record CreateLinkRequest(string Url);

public sealed record CreateLinkResponse(string Code, string ShortUrl, string OriginalUrl);