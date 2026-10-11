using Shortr.Domain;
using Shortr.Persistence;

namespace Shortr.UnitTests;

public sealed class LinkRepositoryTests : IDisposable
{
    private readonly TestDatabase _database = new();
    private readonly LinkRepository _repository;

    public LinkRepositoryTests()
    {
        // Each test starts from a real, initialized database — same state
        // production has when a request arrives.
        var factory = _database.CreateFactory();
        new DatabaseInitializer(factory).Initialize();
        _repository = new LinkRepository(factory);
    }

    private static Link NewLink(string code = "abc1234", string url = "https://example.com") =>
        new()
        {
            Code = code,
            OriginalUrl = url,
            CreatedAtUtc = new DateTime(2026, 10, 11, 12, 0, 0, DateTimeKind.Utc),
        };

    [Fact]
    public async Task AddAsync_NewLink_ReturnsTrue()
    {
        var result = await _repository.AddAsync(NewLink());

        Assert.True(result);
    }

    [Fact]
    public async Task AddAsync_And_GetByCodeAsync_RoundTripsAllFields()
    {
        var link = NewLink();

        await _repository.AddAsync(link);
        var loaded = await _repository.GetByCodeAsync(link.Code);

        // Every field must survive the DB round-trip — this is the test
        // that would have caught the Dapper mapping bug.
        Assert.NotNull(loaded);
        Assert.Equal(link.Code, loaded!.Code);
        Assert.Equal(link.OriginalUrl, loaded.OriginalUrl);
        Assert.Equal(link.CreatedAtUtc, loaded.CreatedAtUtc);
    }

    [Fact]
    public async Task AddAsync_DuplicateCode_ReturnsFalse()
    {
        await _repository.AddAsync(NewLink(code: "dup0001"));

        // The unique constraint rejects the second insert — the collision
        // contract the retry protocol is built on.
        var result = await _repository.AddAsync(NewLink(code: "dup0001", url: "https://other.com"));

        Assert.False(result);
    }

    [Fact]
    public async Task AddAsync_AfterDuplicate_OriginalIsIntact()
    {
        var original = NewLink(code: "keep0001", url: "https://original.com");
        await _repository.AddAsync(original);

        // A rejected insert must leave the winning row untouched.
        await _repository.AddAsync(NewLink(code: "keep0001", url: "https://intruder.com"));
        var loaded = await _repository.GetByCodeAsync("keep0001");

        Assert.Equal("https://original.com", loaded!.OriginalUrl);
    }

    [Fact]
    public async Task GetByCodeAsync_UnknownCode_ReturnsNull()
    {
        var loaded = await _repository.GetByCodeAsync("zzz9999");

        Assert.Null(loaded);
    }

    public void Dispose() => _database.Dispose();
}