using Microsoft.OData.UriParser;
using System.Linq.Expressions;

namespace Atria.Core.Data.Models.Query;

public class OrderByOptions
{
    public Expression OrderBy { get; set; }

    public Type PropertyType { get; set; }

    public OrderByDirection Direction { get; set; }
}
