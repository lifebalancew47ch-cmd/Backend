namespace LifeBalance.Dashboard.Application.Common.Interfaces;

/// <summary>
/// Defines the unit-of-work contract for the Application layer.
/// Abstracts the persistence mechanism to keep Application decoupled from Infrastructure.
/// </summary>
public interface IUnitOfWork
{
    /// <summary>
    /// Commits all pending changes to the underlying store.
    /// </summary>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
