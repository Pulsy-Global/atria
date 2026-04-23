using Atria.Common.Web.OData.Converters;
using System.ComponentModel;
using System.Linq.Expressions;

namespace Atria.Common.Web.OData.Models;

[TypeConverter(typeof(ODataFromStringConverter))]
public class ODataQueryFilter<T>
{
    public Expression<Func<T, bool>> Expression { get; set; }

    public ODataQueryFilter(Expression exp)
    {
        Expression = (Expression<Func<T, bool>>)exp;
    }
}
