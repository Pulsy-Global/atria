using Atria.Common.Web.OData.Visitors;
using Atria.Core.Data.Models.Query;
using MapsterMapper;
using System.Linq.Expressions;
using System.Reflection;

namespace Atria.Core.Data.Extensions;

public static class MapperExtensions
{
    private static MethodInfo _rebindPredicate =
        typeof(MapperExtensions).GetMethod(nameof(RebindPredicate)) !;

    private static MethodInfo _rebindKeySelector =
        typeof(MapperExtensions).GetMethod(nameof(RebindKeySelector)) !;

    public static QueryOptions<TEntity> MapQueryOptions<TDto, TEntity>(
        this IMapper mapper,
        QueryOptions<TDto> queryOptions)
    {
        var result = new QueryOptions<TEntity>();

        if (queryOptions.Filter != null)
        {
            result.Filter = RebindPredicate<TDto, TEntity>(queryOptions.Filter, mapper);
        }

        if (queryOptions.OrderByOptions != null)
        {
            var propertyType = queryOptions.OrderByOptions.PropertyType;
            var sourceLambda = (LambdaExpression)queryOptions.OrderByOptions.OrderBy;

            var rebind = (Func<LambdaExpression, IMapper, LambdaExpression>)Delegate
                .CreateDelegate(
                    typeof(Func<LambdaExpression, IMapper, LambdaExpression>),
                    _rebindKeySelector.MakeGenericMethod(
                        typeof(TDto),
                        typeof(TEntity),
                        propertyType));

            var rebounded = rebind(sourceLambda, mapper);

            result.OrderByOptions = new OrderByOptions
            {
                OrderBy = rebounded,
                PropertyType = propertyType,
                Direction = queryOptions.OrderByOptions.Direction,
            };
        }

        result.Skip = queryOptions.Skip;
        result.Top = queryOptions.Top;
        result.SearchQuery = queryOptions.SearchQuery;

        return result;
    }

    public static Expression<Func<TTarget, bool>> RebindPredicate<TSource, TTarget>(
        Expression<Func<TSource, bool>> predicate,
        IMapper mapper)
    {
        var targetParam = Expression.Parameter(
            typeof(TTarget),
            predicate.Parameters[0].Name);

        var body = new ODataExpressionRebinder(predicate.Parameters[0], targetParam, mapper)
            .Visit(predicate.Body);

        return Expression.Lambda<Func<TTarget, bool>>(body, targetParam);
    }

    public static LambdaExpression RebindKeySelector<TSource, TTarget, TProperty>(
        LambdaExpression keySelector,
        IMapper mapper)
    {
        var sourceParam = keySelector.Parameters[0];

        var targetParam = Expression.Parameter(
            typeof(TTarget),
            sourceParam.Name);

        var body = new ODataExpressionRebinder(sourceParam, targetParam, mapper)
            .Visit(keySelector.Body);

        var delegateType = typeof(Func<,>).MakeGenericType(
            typeof(TTarget),
            typeof(TProperty));

        return Expression.Lambda(delegateType, body, targetParam);
    }
}
