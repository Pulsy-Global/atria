using System.Linq.Expressions;

namespace Atria.Core.Data.Models.Query;

public class QueryOptions<TEntity>
{
    public Expression<Func<TEntity, bool>>? Filter { get; set; }

    public OrderByOptions? OrderByOptions { get; set; }

    public int? Skip { get; set; }

    public int? Top { get; set; }

    public string? SearchQuery { get; set; }
}
