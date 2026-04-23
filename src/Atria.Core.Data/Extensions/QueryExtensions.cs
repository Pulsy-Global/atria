using Atria.Common.Web.OData.Models;
using Atria.Core.Data.Models.Query;
using Microsoft.OData.UriParser;
using System.Linq.Expressions;
using System.Reflection;

namespace Atria.Core.Data.Extensions;

public static class QueryExtensions
{
    public static QueryOptions<T> ToQueryOptions<T>(this ODataQueryParams<T>? odataParams)
    {
        if (odataParams == null)
        {
            return new QueryOptions<T>();
        }

        return new QueryOptions<T>
        {
            Filter = odataParams.Filter?.Expression,
            OrderByOptions = odataParams.OrderByOptions == null
                ? null
                : new OrderByOptions
                {
                    OrderBy = odataParams.OrderByOptions.OrderBy,
                    PropertyType = odataParams.OrderByOptions.PropertyType,
                    Direction = odataParams.OrderByOptions.Direction,
                },
            Skip = odataParams.Skip,
            Top = odataParams.Top,
            SearchQuery = odataParams.Search,
        };
    }

    public static IQueryable<T> ApplyQueryOptions<T>(
            this IQueryable<T> queryable,
            QueryOptions<T>? queryOptions)
    {
        if (queryOptions == null)
        {
            return queryable;
        }

        return queryable
            .Filter(queryOptions)
            .Order(queryOptions)
            .TakePage(queryOptions);
    }

    public static IQueryable<T> Filter<T>(
        this IQueryable<T> queryable,
        QueryOptions<T> queryOptions)
    {
        if (queryOptions?.Filter != null)
        {
            return queryable.Where(queryOptions.Filter);
        }

        return queryable;
    }

    public static IQueryable<T> Order<T>(
        this IQueryable<T> queryable,
        QueryOptions<T> queryOptions)
    {
        if (queryOptions?.OrderByOptions?.OrderBy != null)
        {
            var orderByParams = new object[]
            {
                queryable,
                queryOptions.OrderByOptions.OrderBy,
            };

            Expression<Func<IQueryable<T>, IQueryable>> orderByExpression;

            if (queryOptions.OrderByOptions.Direction ==
                OrderByDirection.Ascending)
            {
                orderByExpression = x =>
                    x.OrderBy<T, object?>((o) => null);
            }
            else
            {
                orderByExpression = x =>
                    x.OrderByDescending<T, object?>((o) => null);
            }

            MethodInfo orderByGeneric =
                ((MethodCallExpression)orderByExpression.Body)
                    .Method
                    .GetGenericMethodDefinition()
                    .MakeGenericMethod(
                        typeof(T),
                        queryOptions.OrderByOptions.PropertyType);

            return (IQueryable<T>)orderByGeneric.Invoke(
                queryable,
                orderByParams) !;
        }

        return queryable;
    }

    public static IQueryable<T> TakePage<T>(
        this IQueryable<T> queryable,
        QueryOptions<T> queryOptions)
    {
        var querySoFar = queryable;

        if (queryOptions?.Skip != null)
        {
            querySoFar = querySoFar.Skip(queryOptions.Skip.Value);
        }

        if (queryOptions?.Top != null)
        {
            querySoFar = querySoFar.Take(queryOptions.Top.Value);
        }

        return querySoFar;
    }
}
