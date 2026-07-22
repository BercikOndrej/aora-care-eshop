namespace AoraCare.Domain.Repositories;

/// <summary>
///     Commits pending repository changes as a single transaction.
/// </summary>
public interface IUnitOfWork
{
    /// <summary>
    ///     Persists all tracked changes to the database.
    /// </summary>
    Task SaveChangesAsync();
}
