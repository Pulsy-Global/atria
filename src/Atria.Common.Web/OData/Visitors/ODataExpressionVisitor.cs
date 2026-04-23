using System.Linq.Expressions;

namespace Atria.Common.Web.OData.Visitors;

public class ODataExpressionVisitor<T> : ExpressionVisitor
{
    private readonly ParameterExpression _parameter;

    public ODataExpressionVisitor(ParameterExpression parameter)
    {
        _parameter = parameter;
    }

    public Expression ToLambdaExpression(Expression expression)
    {
        return Visit(expression);
    }

    protected override Expression VisitMember(MemberExpression node)
    {
        if (node.Member.ReflectedType != typeof(T))
        {
            return base.VisitMember(node);
        }

        var result = Expression.Property(_parameter, node.Member.Name);

        return result;
    }
}
