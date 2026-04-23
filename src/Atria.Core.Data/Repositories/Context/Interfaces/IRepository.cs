using System.Linq.Expressions;

namespace Atria.Core.Data.Repositories.Context.Interfaces;

public interface IRepository<in TKey, TEntity>
    where TEntity : class
{
    Task<TEntity?> GetAsync(TKey id, CancellationToken ct);

    Task<TEntity?> GetAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken ct, bool ignoreFilters = false);

    Task<TEntity?> GetAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken ct, params Expression<Func<TEntity, object>>[] includes);

    Task<TEntity?> GetAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken ct, bool ignoreFilters, params Expression<Func<TEntity, object>>[] includes);

    Task<List<TEntity>> GetListAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken ct, bool ignoreFilters = false, params Expression<Func<TEntity, object>>[] includes);

    Task<List<TEntity>> GetAllAsync(CancellationToken ct);

    void Create(TEntity entity);

    void CreateRange(IEnumerable<TEntity> entities);

    Task<TEntity> CreateAsync(TEntity entity, CancellationToken ct);

    Task CreateRangeAsync(IEnumerable<TEntity> entities, CancellationToken ct);

    void Update(TEntity entity);

    void UpdateRange(IEnumerable<TEntity> entities);

    Task UpsertAsync(TEntity entity);

    Task UpsertRangeAsync(IEnumerable<TEntity> entities);

    void Delete(TEntity entity, bool hardDelete = false);

    void DeleteRange(IEnumerable<TEntity> entities, bool hardDelete = false);

    Task<bool> ExistsAsync(TKey id, CancellationToken ct);

    Task<bool> ExistsAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken ct, bool ignoreFilters = false);

    Task<int> CountAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken ct);

    Task<int> CountAsync(CancellationToken ct);

    /// <summary>
    /// Execute deletion when calling without using SaveChangesAsync.
    /// </summary>
    /// <param name="predicate">Delete condition.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task ExecuteDeleteAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken ct = default);

    /// <summary>
    /// Execute deletion when calling without using SaveChangesAsync.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task ExecuteDeleteAsync(CancellationToken ct = default);
}
