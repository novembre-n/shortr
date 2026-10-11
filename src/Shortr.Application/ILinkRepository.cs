using Shortr.Domain;

namespace Shortr.Application;

// This interface lives in Application. Callers depend on the interface.
// Callers do not depend on Dapper or SQLite. The class LinkRepository
// implements this interface. Program.cs wires the class into the DI container.
public interface ILinkRepository
{
    // AddAsync returns false when the code exists already. The unique
    // constraint causes this result. The method does not throw.
    public Task<bool> AddAsync(Link link, CancellationToken cancellationToken = default);

    // GetByCodeAsync returns the link for the code. The method returns null
    // when the code does not exist.
    public Task<Link?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
}