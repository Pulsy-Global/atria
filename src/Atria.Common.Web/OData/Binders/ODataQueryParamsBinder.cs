using Atria.Common.Extensions;
using Atria.Common.Web.OData.Attributes;
using Atria.Common.Web.OData.Models;
using Atria.Common.Web.OData.Visitors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.OData.Abstracts;
using Microsoft.AspNetCore.OData.Extensions;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OData.Edm;
using Microsoft.OData.ModelBuilder;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Atria.Common.Web.OData.Binders;

public class ODataQueryParamsBinder : IModelBinder
{
    private const string ModelKeyPrefix = "Microsoft.AspNet.OData.Model+";

    private static readonly MethodInfo _createODataQueryOptions =
        typeof(ODataQueryParamsBinder)
            .GetMethod(nameof(CreateODataQueryOptions)) !;

    private static readonly MethodInfo _toFilterExpression =
        typeof(ODataQueryParamsBinder)
            .GetMethod(nameof(ToFilterExpression)) !;

    private static readonly MethodInfo _handleQueryParameters =
        typeof(ODataQueryParamsBinder)
            .GetMethod(nameof(HandleQueryParemeters)) !;

    private static readonly MethodInfo _createQueryOptions =
        typeof(ODataQueryParamsBinder)
            .GetMethod(nameof(CreateQueryParams)) !;

    private static readonly MethodInfo _createOrderByLambda =
        typeof(ODataQueryParamsBinder)
            .GetMethod(nameof(CreateOrderByLambda)) !;

    private static readonly MethodInfo _toOrderByExpression =
        typeof(ODataQueryParamsBinder)
            .GetMethod(nameof(ToOrderByExpression)) !;

    public static ODataQueryOptions<T> CreateODataQueryOptions<T>(
        ODataQueryContext context,
        HttpRequest request)
    {
        return new ODataQueryOptions<T>(context, request);
    }

    public static Expression<Func<T, bool>> ToFilterExpression<T>(
        FilterQueryOption filter)
    {
        var queryable = Enumerable.Empty<T>().AsQueryable() as IQueryable;

        queryable = filter.ApplyTo(queryable, new ODataQuerySettings());

        var parameter = Expression.Parameter(typeof(T), "$it");
        var expression = CreateLambdaExpression<T>(queryable, parameter);

        var lambda = Expression.Lambda<Func<T, bool>>(
            expression.Body,
            parameter);

        return lambda;
    }

    public static Expression ToOrderByExpression<T>(
        OrderByQueryOption orderBy)
    {
        var queryable = Enumerable.Empty<T>().AsQueryable() as IQueryable;

        queryable = orderBy.ApplyTo(queryable, new ODataQuerySettings());

        var parameter = Expression.Parameter(typeof(T), "$it");
        var expression = CreateLambdaExpression<T>(queryable, parameter);

        var createOrderByLambda = (Func<
            Expression,
            ParameterExpression,
            Expression>)Delegate.CreateDelegate(
                typeof(Func<Expression, ParameterExpression, Expression>),
                _createOrderByLambda.MakeGenericMethod(
                    typeof(T),
                    expression.ReturnType));

        var lambda = createOrderByLambda(expression.Body, parameter);

        return lambda;
    }

    public static Expression CreateOrderByLambda<T, TProperty>(
        Expression body,
        ParameterExpression parameter)
    {
        return Expression.Lambda<Func<T, TProperty>>(body, parameter);
    }

    public static IQueryCollection HandleQueryParemeters<T>(
        IQueryCollection query)
    {
        var modelTypes = typeof(T)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(x => Attribute.IsDefined(x, typeof(ODataHandleAsString)));

        foreach (var modelType in modelTypes)
        {
            var queryHandleField = modelType
                .GetCustomAttribute<ODataHandleAsString>();

            var modelTypeToMapping = queryHandleField?.MapTo != null
                ? typeof(T).GetProperty(queryHandleField.MapTo)
                : modelType;

            if (modelTypeToMapping == null)
            {
                throw new ArgumentNullException(nameof(modelTypeToMapping));
            }

            var queryDictionary = query.ToDictionary(x => x.Key, x => x.Value);

            var (removeQuotes, isBigInteger) = modelTypeToMapping.PropertyType switch
            {
                var type when type.IsBigIntegerType() => (true, true),
                var type when type.IsNumericType() => (true, false),
                var type when type.IsDateTimeType() => (true, false),
                _ => (false, false)
            };

            string replacement = (isBigInteger, removeQuotes) switch
            {
                (true, true) => $"{modelTypeToMapping.Name} $2 cast($3, Edm.Int64)",
                (false, true) => $"{modelTypeToMapping.Name} $2 $3",
                (false, false) => $"{modelTypeToMapping.Name} $2 '$3'",
                _ => $"{modelTypeToMapping.Name} $2 '$3'"
            };

            if (queryDictionary.ContainsKey("$filter"))
            {
                queryDictionary["$filter"] = Regex.Replace(
                    query["$filter"] !, @$"((?i)\b{modelType.Name}\b)[ \t]+([^\s]+)[ \t]+'([^\s]+)'", replacement);
            }

            if (queryDictionary.ContainsKey("$orderby"))
            {
                queryDictionary["$orderby"] = Regex.Replace(
                    queryDictionary["$orderby"] !,
                    @$"(?i)\b{modelType.Name}\b",
                    modelTypeToMapping.Name,
                    RegexOptions.IgnoreCase);
            }

            query = new QueryCollection(queryDictionary);
        }

        return query;
    }

    public static ODataQueryParams<T> CreateQueryParams<T>(
        Expression expression,
        ODataQueryOrderBy orderByQueryOptions,
        int? skip,
        int? top,
        string? search)
    {
        return new ODataQueryParams<T>(
            expression,
            orderByQueryOptions,
            skip,
            top,
            search);
    }

    public Task BindModelAsync(ModelBindingContext bindingContext)
    {
        if (bindingContext == null)
        {
            throw new ArgumentNullException(nameof(bindingContext));
        }

        var request = bindingContext.HttpContext.Request;

        if (request == null)
        {
            throw new ArgumentException(
                "Request cannot be empty.",
                nameof(bindingContext));
        }

        var actionDescriptor = bindingContext.ActionContext.ActionDescriptor;

        if (actionDescriptor == null)
        {
            throw new ArgumentException(
                "Descriptor of action cannot be empty",
                nameof(bindingContext));
        }

        var entityClrType = GetModelClrType(bindingContext.ModelType);

        var handleQueryParameters =
            (Func<IQueryCollection, IQueryCollection>)Delegate
                .CreateDelegate(
                    typeof(Func<IQueryCollection, IQueryCollection>),
                    _handleQueryParameters.MakeGenericMethod(entityClrType));

        request.Query = handleQueryParameters(request.Query);

        if (request.HttpContext.Features.Get<IODataFeature>() == null)
        {
            IODataFeature odataFeature = new ODataFeature()
            {
                RoutePrefix = "v{version}",
            };

            request.HttpContext.Features.Set(odataFeature);
        }

        var model = GetOrCreateEdmModel(actionDescriptor, request, entityClrType);

        var entitySetContext = new ODataQueryContext(
            model,
            entityClrType,
            request.ODataFeature().Path);

        var createODataQueryOptions =
            (Func<ODataQueryContext, HttpRequest, ODataQueryOptions>)Delegate
                .CreateDelegate(
                    typeof(Func<
                        ODataQueryContext,
                        HttpRequest,
                        ODataQueryOptions>),
                    _createODataQueryOptions
                        .MakeGenericMethod(entityClrType));

        var options = createODataQueryOptions(entitySetContext, request);

        var filterExpression = BuildFilterExpression(entityClrType, options);
        var orderBy = BuildOrderBy(entityClrType, options);

        int? skip = null;
        if (options.Skip != null)
        {
            skip = options.Skip.Value;
        }

        int? top = null;
        if (options.Top != null)
        {
            top = options.Top.Value;
        }

        string? search = null;
        if (options.Search != null)
        {
            search = options.Search.RawValue;
        }

        var query = CreateQueryParams(
            entityClrType,
            filterExpression,
            orderBy,
            skip,
            top,
            search);

        bindingContext.Result = ModelBindingResult.Success(query);

        return Task.CompletedTask;
    }

    private static LambdaExpression CreateLambdaExpression<T>(
        IQueryable queryable,
        ParameterExpression parameter)
    {
        var expression = queryable.Expression;

        var visitor = new ODataExpressionVisitor<T>(parameter);

        var methodCallExpressionFilter = (MethodCallExpression)expression;

        var unquotedLambdaExpression = (LambdaExpression)Unquote(
            visitor.ToLambdaExpression(methodCallExpressionFilter.Arguments[1]));

        return unquotedLambdaExpression;
    }

    private static Expression Unquote(Expression quote)
    {
        return quote.NodeType == ExpressionType.Quote
        ? Unquote(((UnaryExpression)quote).Operand)
        : quote;
    }

    private static Type GetModelClrType(Type parameterType)
    {
        if (parameterType.IsGenericType &&
            parameterType.GetGenericTypeDefinition() ==
                typeof(ODataQueryParams<>))
        {
            return parameterType.GetGenericArguments().Single();
        }

        throw new InvalidOperationException("Not Odata query parameters.");
    }

    private static IEdmModel? GetEdmModel(
        ActionDescriptor actionDescriptor,
        HttpRequest request,
        Type entityClrType)
    {
        IEdmModel? model = null;

        var modelClrTypeKey = ModelKeyPrefix + entityClrType.FullName;

        if (actionDescriptor.Properties.TryGetValue(
            modelClrTypeKey,
            out object? modelAsObject))
        {
            model = modelAsObject as IEdmModel;
        }
        else
        {
            var assemblyResolver = request.HttpContext.RequestServices
                .GetRequiredService<IAssemblyResolver>();

            var builder = new ODataConventionModelBuilder(
               assemblyResolver,
               isQueryCompositionMode: true);

            var enumTypes = entityClrType
                .GetRuntimeFields()
                .Where(el => el.FieldType.IsEnum)
                .Select(el => el.FieldType)
                .ToList();

            foreach (var enumType in enumTypes)
            {
                builder.AddEnumType(enumType);
            }

            var entityTypeConfiguration = builder
                .AddEntityType(entityClrType);

            builder.AddEntitySet(
                entityClrType.Name,
                entityTypeConfiguration);

            model = builder.GetEdmModel();

            actionDescriptor.Properties
                .Add(modelClrTypeKey, model);
        }

        return model;
    }

    private static IEdmModel GetOrCreateEdmModel(
        ActionDescriptor actionDescriptor,
        HttpRequest request,
        Type entityClrType)
    {
        var edmModel = request.ODataFeature().Model;

        if (edmModel == null || edmModel == EdmCoreModel.Instance)
        {
            var routeServices = request.GetRouteServices();
            edmModel = routeServices?.GetService<IEdmModel>();
        }

        return edmModel ?? GetEdmModel(actionDescriptor, request, entityClrType) !;
    }

    private static Expression? BuildFilterExpression(
        Type entityClrType,
        ODataQueryOptions options)
    {
        if (options.Filter == null)
        {
            return null;
        }

        var toFilterExpression =
            (Func<FilterQueryOption, Expression>)Delegate
                .CreateDelegate(
                    typeof(Func<FilterQueryOption, Expression>),
                    _toFilterExpression.MakeGenericMethod(entityClrType));

        return toFilterExpression(options.Filter);
    }

    private static ODataQueryOrderBy? BuildOrderBy(
        Type entityClrType,
        ODataQueryOptions options)
    {
        if (options.OrderBy == null)
        {
            return null;
        }

        if (options.OrderBy.OrderByNodes.Count > 1)
        {
            throw new InvalidOperationException(
                "Sorting is possible by one field only");
        }

        var toOrderByExpression =
            (Func<OrderByQueryOption, Expression>)Delegate
                .CreateDelegate(
                    typeof(Func<OrderByQueryOption, Expression>),
                    _toOrderByExpression.MakeGenericMethod(entityClrType));

        var expression = toOrderByExpression(options.OrderBy);
        var returnType = ((LambdaExpression)expression).ReturnType;

        return new ODataQueryOrderBy
        {
            OrderBy = expression,
            PropertyType = returnType,
            Direction = options.OrderBy.OrderByNodes.First().Direction,
        };
    }

    private static ODataQueryParamsGeneric CreateQueryParams(
        Type entityClrType,
        Expression? filterExpression,
        ODataQueryOrderBy? orderBy,
        int? skip,
        int? top,
        string? search)
    {
        var createQueryParams = (Func<
            Expression?,
            ODataQueryOrderBy?,
            int?,
            int?,
            string?,
            ODataQueryParamsGeneric>)Delegate
                .CreateDelegate(
                    typeof(Func<
                        Expression?,
                        ODataQueryOrderBy?,
                        int?,
                        int?,
                        string?,
                        ODataQueryParamsGeneric>),
                    _createQueryOptions.MakeGenericMethod(entityClrType));

        return createQueryParams(
            filterExpression,
            orderBy,
            skip,
            top,
            search);
    }
}
