using Atria.Common.Web.OData.Helpers;
using MapsterMapper;
using System.Linq.Expressions;
using System.Numerics;
using System.Reflection;

namespace Atria.Common.Web.OData.Visitors;

public class ODataExpressionRebinder : ExpressionVisitor
{
    private readonly ParameterExpression _sourceParam;
    private readonly ParameterExpression _targetParam;

    private readonly Dictionary<string, string> _propertyMappings;
    private readonly Dictionary<string, (string CollectionProperty, string ItemProperty)> _collectionMappings;

    public ODataExpressionRebinder(
        ParameterExpression sourceParam,
        ParameterExpression targetParam)
    {
        _sourceParam = sourceParam;
        _targetParam = targetParam;

        _propertyMappings = ODataMapsterMappingHelper
            .GetPropertyMappingsFromMapster(_sourceParam.Type, _targetParam.Type, null);

        _collectionMappings = ODataCollectionMappingHelper
            .GetCollectionMappings(_sourceParam.Type);
    }

    public ODataExpressionRebinder(
        ParameterExpression sourceParam,
        ParameterExpression targetParam,
        IMapper mapper)
    {
        _sourceParam = sourceParam;
        _targetParam = targetParam;

        _propertyMappings = ODataMapsterMappingHelper
            .GetPropertyMappingsFromMapster(_sourceParam.Type, _targetParam.Type, mapper);

        _collectionMappings = ODataCollectionMappingHelper
            .GetCollectionMappings(_sourceParam.Type);
    }

    protected override Expression VisitParameter(ParameterExpression node)
    {
        return node == _sourceParam ? _targetParam : base.VisitParameter(node);
    }

    protected override Expression VisitMember(MemberExpression node)
    {
        var visitedExpr = Visit(node.Expression);

        if (visitedExpr == null)
        {
            return node;
        }

        if (node.Member.MemberType is not (MemberTypes.Property or MemberTypes.Field))
        {
            return Expression.MakeMemberAccess(visitedExpr, node.Member);
        }

        var memberName = node.Member.Name;

        if (_propertyMappings.TryGetValue(memberName, out var mappedName))
        {
            memberName = mappedName;
        }

        var property = visitedExpr.Type.GetProperty(
            memberName,
            BindingFlags.Public | BindingFlags.Instance);

        if (property != null)
        {
            return Expression.Property(visitedExpr, property);
        }

        var field = visitedExpr.Type.GetField(
            memberName,
            BindingFlags.Public | BindingFlags.Instance);

        if (field != null)
        {
            return Expression.Field(visitedExpr, field);
        }

        return Expression.MakeMemberAccess(visitedExpr, node.Member);
    }

    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        if (IsEnumerableAnyMethod(node) && TryGetCollectionMember(node, out var memberExpr, out var mapping))
        {
            return TransformAnyExpression(memberExpr, node.Arguments[1], mapping);
        }

        return base.VisitMethodCall(node);
    }

    protected override Expression VisitBinary(BinaryExpression node)
    {
        var left = Visit(node.Left);
        var right = Visit(node.Right);

        if (left == node.Left && right == node.Right)
        {
            return base.VisitBinary(node);
        }

        if (left.Type != right.Type)
        {
            if (left.Type == typeof(BigInteger?) && right.Type == typeof(decimal?))
            {
                right = Expression.Convert(right, typeof(BigInteger?));
            }
            else if (left.Type == typeof(decimal?) && right.Type == typeof(BigInteger?))
            {
                left = Expression.Convert(left, typeof(BigInteger?));
            }
        }

        return Expression.MakeBinary(node.NodeType, left, right);
    }

    private static bool IsEnumerableAnyMethod(MethodCallExpression node)
    {
        return node.Method.Name == "Any" &&
               node.Method.DeclaringType == typeof(Enumerable) &&
               node.Arguments.Count == 2;
    }

    private bool TryGetCollectionMember(
        MethodCallExpression node,
        out MemberExpression memberExpr,
        out (string CollectionProperty, string ItemProperty) mapping)
    {
        memberExpr = null!;
        mapping = default;

        if (node.Arguments[0] is not MemberExpression member)
        {
            return false;
        }

        if (!_collectionMappings.TryGetValue(member.Member.Name, out mapping))
        {
            return false;
        }

        memberExpr = member;
        return true;
    }

    private Expression TransformAnyExpression(
        MemberExpression memberExpr,
        Expression lambdaExpr,
        (string CollectionProperty, string ItemProperty) mapping)
    {
        var visitedMember = Visit(memberExpr.Expression);

        if (visitedMember == null)
        {
            return Expression.Constant(false);
        }

        var targetType = visitedMember.Type;

        var targetCollectionProperty = targetType
            .GetProperty(mapping.CollectionProperty);

        if (targetCollectionProperty == null)
        {
            return Expression.Constant(false);
        }

        var collectionItemType = targetCollectionProperty.PropertyType
            .GetGenericArguments()
            .FirstOrDefault();

        if (collectionItemType == null)
        {
            return Expression.Constant(false);
        }

        var targetItemProperty = collectionItemType
            .GetProperty(mapping.ItemProperty);

        if (targetItemProperty == null)
        {
            return Expression.Constant(false);
        }

        var collectionAccess = Expression.Property(
            visitedMember, targetCollectionProperty);

        LambdaExpression? originalLambda = null;

        if (lambdaExpr is UnaryExpression unary && unary.Operand is LambdaExpression lambda)
        {
            originalLambda = lambda;
        }
        else if (lambdaExpr is LambdaExpression directLambda)
        {
            originalLambda = directLambda;
        }

        if (originalLambda == null)
        {
            return Expression.Constant(false);
        }

        var originalParam = originalLambda.Parameters[0];
        var newParam = Expression.Parameter(collectionItemType, originalParam.Name);

        var bodyRewriter = new ODataAnyLambdaRewriter(
            originalParam,
            newParam,
            targetItemProperty);

        var newBody = bodyRewriter.Visit(originalLambda.Body);

        var newLambda = Expression.Lambda(newBody, newParam);

        var anyMethod = typeof(Enumerable)
            .GetMethods()
            .First(m => m.Name == "Any" && m.GetParameters().Length == 2)
            .MakeGenericMethod(collectionItemType);

        return Expression.Call(anyMethod, collectionAccess, newLambda);
    }
}
