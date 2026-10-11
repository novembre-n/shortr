using Shortr.Domain;

namespace Shortr.Application;

// The business logic layer: validates input, generates codes, retries on
// collision, persists. Everything above it (controller) maps outcomes to
// HTTP; everything below it (repository) is faked in unit tests via ILinkRepository.
public sealed class LinkService
{
    public const int MaxCodeAttempts = 3;

    private readonly ShortCodeGenerator _generator;
    private readonly ILinkRepository _repository;
    private readonly TimeProvider _timeProvider;

    public LinkService(
        ShortCodeGenerator generator,
        ILinkRepository repository,
        TimeProvider timeProvider)
    {
        _generator = generator;
        _repository = repository;
        _timeProvider = timeProvider;
    }

    public async Task<Link> CreateAsync(string originalUrl, CancellationToken cancellationToken = default)
    {
        ValidateUrl(originalUrl);

        var createdAt = _timeProvider.GetUtcNow().UtcDateTime;

        for (var attempt = 1; attempt <= MaxCodeAttempts; attempt++)
        {
            var code = _generator.Generate();
            var link = new Link
            {
                Code = code,
                OriginalUrl = originalUrl,
                CreatedAtUtc = createdAt,
            };

            // AddAsync returns false when the unique constraint rejects the code.
            if (await _repository.AddAsync(link, cancellationToken))
            {
                return link;
            }

            // Code taken — loop, next code.
        }

        // 62^7 code space, 3 misses in a row: genuinely unexpected.
        throw new InvalidOperationException(
            $"Could not generate a unique code after {MaxCodeAttempts} attempts.");
    }

    public async Task<Link?> ResolveAsync(string code, CancellationToken cancellationToken = default)
    {
        return await _repository.GetByCodeAsync(code, cancellationToken);
    }

    // Validation lives here, not in the domain model — the model stays
    // Dapper-friendly (public setters), the service enforces the rules.
    private static void ValidateUrl(string originalUrl)
    {
        if (string.IsNullOrWhiteSpace(originalUrl)
            || !Uri.TryCreate(originalUrl, UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            throw new ArgumentException("URL must be an absolute http(s) URL.", nameof(originalUrl));
        }
    }
}