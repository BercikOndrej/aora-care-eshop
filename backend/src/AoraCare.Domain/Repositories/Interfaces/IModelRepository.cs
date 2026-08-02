namespace AoraCare.Domain.Repositories.Interfaces;

/// <summary>
///     Generic data access abstraction for <typeparamref name="TEntity"/>.
///     <see cref="GetByIdAsync"/> always returns a change-tracked entity, since callers
///     fetching a single entity by id typically intend to mutate and persist it via
///     <see cref="IUnitOfWork"/>. <see cref="GetAllAsync"/> and <see cref="GetAllForUpdateAsync"/>
///     remain split by caller intent: the plain overload is read-only (no change-tracking) for
///     querying/projection, while the "ForUpdate" overload is change-tracked for mutation.
/// </summary>
/// <typeparam name="TEntity">
///     The entity type this repository manages.
/// </typeparam>
public interface IModelRepository<TEntity>
    where TEntity : class
{
    /// <summary>
    ///     Gets all entities for read-only purposes (no change tracking).
    /// </summary>
    Task<IReadOnlyList<TEntity>> GetAllAsync(CancellationToken ct = default);

    /// <summary>
    ///     Gets all entities with change tracking enabled, for callers that intend to mutate them.
    /// </summary>
    Task<IReadOnlyList<TEntity>> GetAllForUpdateAsync(CancellationToken ct = default);

    /// <summary>
    ///     Gets a single entity by id, with change tracking enabled.
    /// </summary>
    /// <param name="id">
    ///     The identifier of the entity to get.
    /// </param>
    /// <param name="ct">
    ///     Token to cancel the operation.
    /// </param>
    /// <returns>
    ///     The entity, or <see langword="null"/> if no entity with the given id exists.
    /// </returns>
    Task<TEntity?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    ///     Marks an entity to be inserted on the next <see cref="IUnitOfWork.SaveChangesAsync"/>.
    /// </summary>
    void Add(TEntity entity);

    /// <summary>
    ///     Marks an entity to be removed on the next <see cref="IUnitOfWork.SaveChangesAsync"/>.
    /// </summary>
    void Remove(TEntity entity);
}
