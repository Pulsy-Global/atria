using System.Linq.Expressions;

namespace Atria.Common.Web.OData.Visitors;

public class ODataParameterReplacer : ExpressionVisitor
{
    private readonly Expression _oldExpression;
    private readonly Expression _newExpression;

    public ODataParameterReplacer(Expression oldExpression, Expression newExpression)
    {
        _oldExpression = oldExpression;
        _newExpression = newExpression;
    }

    protected override Expression VisitParameter(ParameterExpression node)
    {
        return node == _oldExpression ? _newExpression : base.VisitParameter(node);
    }

    protected override Expression VisitMember(MemberExpression node)
    {
        if (node.Expression == _oldExpression)
        {
            return _newExpression;
        }

        return base.VisitMember(node);
    }
}
