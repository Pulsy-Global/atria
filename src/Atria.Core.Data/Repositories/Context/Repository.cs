using Atria.Core.Data.Entities.Context.Interfaces;
using Atria.Core.Data.Extensions;
using Atria.Core.Data.Models.Query;
using Atria.Core.Data.Repositories.Context.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Atria.Core.Data.Repositories.Context;

public class Repository<TKey, TEntity> : IRepository<TKey, TEntity>
where TEntity : class
{
    public Repository(DbContext context)
    {
        Context = context;
    }

    protected DbContext Context { get; }

    public virtual async Task<TEntity> CreateAsync(TEntity entity, CancellationToken cancellationToken)
    {
        return (await Context.AddAsync(entity, cancellationToken)).Entity;
    }

    public virtual async Task CreateRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken)
    {
        await Context.AddRangeAsync(entities, cancellationToken);
    }

    public virtual void Create(TEntity entity)
    {
        Context.Add(entity);
    }

    public virtual void CreateRange(IEnumerable<TEntity> entities)
    {
        Context.AddRange(entities);
    }

    public virtual void Update(TEntity entity)
    {
        Context.Update(entity);
    }

    public virtual void UpdateRange(IEnumerable<TEntity> entities)
    {
        Context.UpdateRange(entities);
    }

    public virtual Task UpsertAsync(TEntity entity)
    {
        return Context.Upsert(entity).RunAsync();
    }

    public virtual Task UpsertRangeAsync(IEnumerable<TEntity> entities)
    {
        return Context.UpsertRange(entities).RunAsync();
    }

    public virtual void Delete(TEntity entity, bool hardDelete = false)
    {
        Context.Remove(entity);

        if (hardDelete && entity is IHardDeletable hardDeletable)
        {
            hardDeletable.IsHardDeleted = true;
        }
    }

    public virtual void DeleteRange(IEnumerable<TEntity> entities, bool hardDelete = false)
    {
        foreach (var entity in entities)
        {
            Delete(entity, hardDelete);
        }
    }

    public virtual async Task<TEntity?> GetAsync(TKey id, CancellationToken cancellationToken)
    {
        return await Context.FindAsync<TEntity>([id], cancellationToken);
    }

    public async Task<TEntity?> GetAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken, bool ignoreFilters = false)
    {
        return await GetSet(ignoreFilters).FirstOrDefaultAsync(predicate, cancellationToken);
    }

    public async Task<TEntity?> GetAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken, params Expression<Func<TEntity, object>>[] includes)
    {
        return await GetAsync(predicate, cancellationToken, false, includes);
    }

    public async Task<TEntity?> GetAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken, bool ignoreFilters, params Expression<Func<TEntity, object>>[] includes)
    {
        return await GetSet(ignoreFilters, includes).FirstOrDefaultAsync(predicate, cancellationToken);
    }

    public async Task<List<TEntity>> GetListAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken, bool ignoreFilters = false, params Expression<Func<TEntity, object>>[] includes)
    {
        return await GetSet(ignoreFilters, includes).Where(predicate).ToListAsync(cancellationToken);
    }

    public virtual async Task<bool> ExistsAsync(TKey id, CancellationToken cancellationToken)
    {
        return await Context.FindAsync<TEntity>([id], cancellationToken) != null;
    }

    public async Task<bool> ExistsAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken, bool ignoreFilters = false)
    {
        return await GetSet(ignoreFilters).AnyAsync(predicate, cancellationToken);
    }

    public virtual Task<List<TEntity>> GetAllAsync(CancellationToken cancellationToken)
    {
        return GetSet().ToListAsync(cancellationToken);
    }

    public virtual Task ExecuteDeleteAsync(CancellationToken cancellationToken = default)
    {
        return GetSet().ExecuteDeleteAsync(cancellationToken);
    }

    public virtual Task ExecuteDeleteAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default)
    {
        return GetSet().Where(predicate).ExecuteDeleteAsync(cancellationToken);
    }

    public virtual async Task<int> CountAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken)
    {
        return await GetSet().CountAsync(predicate, cancellationToken);
    }

    public virtual async Task<int> CountAsync(CancellationToken cancellationToken)
    {
        return await GetSet().CountAsync(cancellationToken);
    }

    public virtual async Task<List<TEntity>> FilterWithQueryOptions(QueryOptions<TEntity> queryOptions, CancellationToken cancellationToken, bool ignoreFilters = false)
    {
        return await GetSet()
            .AsQueryable()
            .ApplyQueryOptions(queryOptions)
            .ToListAsync();
    }

    protected IQueryable<TEntity> GetSet(bool ignoreFilters = false, params Expression<Func<TEntity, object>>[] includes)
    {
        IQueryable<TEntity> set = Context.Set<TEntity>();

        if (ignoreFilters)
        {
            set = set.IgnoreQueryFilters();
        }

        if (includes != null && includes.Any())
        {
            foreach (var include in includes)
            {
                set = set.Include(include);
            }
        }

        return set;
    }
}
