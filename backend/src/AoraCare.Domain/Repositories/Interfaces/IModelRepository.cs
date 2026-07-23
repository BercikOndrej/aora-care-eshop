namespace AoraCare.Domain.Repositories.Interfaces;

/// <summary>
///     Generic data access abstraction for <typeparamref name="TEntity"/>.
///     Read methods are split by caller intent instead of an EF-specific tracking flag:
///     plain overloads return read-only (no change-tracking) results for querying/projection,
///     while "ForUpdate" overloads return change-tracked entities for callers that intend
///     to mutate them and persist the changes via <see cref="IUnitOfWork"/>.
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
    ///     Gets a single entity by id for read-only purposes (no change tracking).
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
    ///     Gets a single entity by id with change tracking enabled, for callers that intend to mutate it.
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
    Task<TEntity?> GetByIdForUpdateAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    ///     Marks an entity to be inserted on the next <see cref="IUnitOfWork.SaveChangesAsync"/>.
    /// </summary>
    void Add(TEntity entity);

    /// <summary>
    ///     Marks an entity to be removed on the next <see cref="IUnitOfWork.SaveChangesAsync"/>.
    /// </summary>
    void Remove(TEntity entity);
}
