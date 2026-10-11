using Shortr.Application;
using Shortr.Domain;

namespace Shortr.UnitTests;

// Hand-written fake, not a mocking library: the fake IS the test's stand-in
// for the real (already integration-tested) repository. Queue<bool> lets a
// test script the answers ("reject once, then accept") so the retry protocol
// is tested deterministically despite the random generator.
public sealed class FakeLinkRepository : ILinkRepository
{
    public List<Link> Links { get; } = new();

    public int AddCalls { get; private set; }

    public bool AlwaysReject { get; set; }

    public Queue<bool> AddAsyncResultQueue { get; set; } = new();

    public Task<bool> AddAsync(Link link, CancellationToken cancellationToken = default)
    {
        AddCalls++;

        // Scripted answers replace only the collision decision — on true the
        // link is still stored, exactly like the real repository's INSERT.
        var accepted = AddAsyncResultQueue.Count > 0
            ? AddAsyncResultQueue.Dequeue()
            : !AlwaysReject && Links.All(l => l.Code != link.Code);

        if (accepted)
        {
            Links.Add(link);
        }

        return Task.FromResult(accepted);
    }

    public Task<Link?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Links.FirstOrDefault(l => l.Code == code));
    }
}

public sealed class LinkServiceTests
{
    private static LinkService CreateService(FakeLinkRepository repository) =>
        new(new ShortCodeGenerator(), repository, TimeProvider.System);

    [Fact]
    public async Task CreateAsync_ValidUrl_PersistsAndReturnsLink()
    {
        var repository = new FakeLinkRepository();
        var service = CreateService(repository);

        var link = await service.CreateAsync("https://example.com/long/path");

        Assert.Equal("https://example.com/long/path", link.OriginalUrl);
        Assert.Equal(7, link.Code.Length);
        Assert.Single(repository.Links);
        Assert.Equal(link.Code, repository.Links.Single().Code);
        Assert.Equal(1, repository.AddCalls);
    }

    [Fact]
    public async Task CreateAsync_SetsCreatedAtFromTimeProvider()
    {
        var repository = new FakeLinkRepository();
        var service = CreateService(repository);

        var before = DateTime.UtcNow;
        var link = await service.CreateAsync("https://example.com");
        var after = DateTime.UtcNow;

        Assert.InRange(link.CreatedAtUtc, before, after);
    }

    private static readonly bool[] RejectedThenAccepted = { false, true };

    [Fact]
    public async Task CreateAsync_Collision_RetriesWithNewCode()
    {
        var repository = new FakeLinkRepository();
        // Script the repository's answers: reject once (collision), accept next.
        repository.AddAsyncResultQueue = new Queue<bool>(RejectedThenAccepted);
        var service = CreateService(repository);

        var link = await service.CreateAsync("https://second.com");

        Assert.Equal(2, repository.AddCalls);
        Assert.Single(repository.Links);
        Assert.Equal("https://second.com", repository.Links.Single().OriginalUrl);
    }

    [Fact]
    public async Task CreateAsync_AllAttemptsCollide_Throws()
    {
        var repository = new FakeLinkRepository { AlwaysReject = true };
        var service = CreateService(repository);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateAsync("https://example.com"));

        Assert.Equal(3, repository.AddCalls);
    }

    [Fact]
    public async Task CreateAsync_InvalidUrl_ThrowsWithoutTouchingRepository()
    {
        var repository = new FakeLinkRepository();
        var service = CreateService(repository);

        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateAsync("not-a-url"));

        Assert.Equal(0, repository.AddCalls);
    }

    [Fact]
    public async Task ResolveAsync_KnownCode_ReturnsLink()
    {
        var repository = new FakeLinkRepository();
        repository.Links.Add(new Link
        {
            Code = "abc1234",
            OriginalUrl = "https://example.com",
            CreatedAtUtc = DateTime.UtcNow,
        });
        var service = CreateService(repository);

        var resolved = await service.ResolveAsync("abc1234");

        Assert.NotNull(resolved);
        Assert.Equal("https://example.com", resolved!.OriginalUrl);
    }

    [Fact]
    public async Task ResolveAsync_UnknownCode_ReturnsNull()
    {
        var service = CreateService(new FakeLinkRepository());

        var resolved = await service.ResolveAsync("zzz9999");

        Assert.Null(resolved);
    }
}