using Atria.Common.Web.OData.Converters;
using Microsoft.OData.UriParser;
using System.ComponentModel;
using System.Linq.Expressions;

namespace Atria.Common.Web.OData.Models;

[TypeConverter(typeof(ODataFromStringConverter))]
public class ODataQueryOrderBy
{
    public Expression OrderBy { get; set; }

    public Type PropertyType { get; set; }

    public OrderByDirection Direction { get; set; }
}
