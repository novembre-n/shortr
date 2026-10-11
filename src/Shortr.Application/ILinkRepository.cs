using Shortr.Domain;

namespace Shortr.Application;

// Interface lives in Application so callers depend on the abstraction,
// not on Dapper/SQLite. Implemented in Shortr.Persistence, wired in the API's DI.
public interface ILinkRepository
{
    // Returns false when the code already exists (unique constraint) instead of throwing.
    public Task<bool> AddAsync(Link link, CancellationToken cancellationToken = default);

    public Task<Link?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
}