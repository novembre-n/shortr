using Shortr.Domain;

namespace Shortr.Application;

// This class holds the business logic. The class validates the input,
// generates the code, and retries after a collision. The class persists
// the link through the repository. The controller maps the outcomes to
// HTTP responses. Unit tests replace the repository with a fake. The
// fake implements the interface ILinkRepository.
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

            // The code exists already. Continue the loop. Generate a new code.
        }

        // The code space holds 62 to the power of 7 codes. Three collisions
        // in a row do not occur in practice. This exception signals an
        // unexpected condition.
        throw new InvalidOperationException(
            $"Could not generate a unique code after {MaxCodeAttempts} attempts.");
    }

    public async Task<Link?> ResolveAsync(string code, CancellationToken cancellationToken = default)
    {
        return await _repository.GetByCodeAsync(code, cancellationToken);
    }

    // The validation lives in this class. The validation does not live in
    // the domain model. The model keeps public setters for Dapper. This
    // class enforces the rules for the input.
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