using System.Linq.Expressions;
using System.Reflection;

namespace Atria.Common.Web.OData.Visitors;

public class ODataAnyLambdaRewriter : ExpressionVisitor
{
    private readonly ParameterExpression _oldParam;
    private readonly ParameterExpression _newParam;
    private readonly PropertyInfo _targetProperty;

    public ODataAnyLambdaRewriter(
        ParameterExpression oldParam,
        ParameterExpression newParam,
        PropertyInfo targetProperty)
    {
        _oldParam = oldParam;
        _newParam = newParam;
        _targetProperty = targetProperty;
    }

    protected override Expression VisitParameter(ParameterExpression node)
    {
        if (node == _oldParam)
        {
            return Expression.Property(_newParam, _targetProperty);
        }

        return base.VisitParameter(node);
    }
}
