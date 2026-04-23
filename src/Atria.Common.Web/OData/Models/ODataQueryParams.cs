using Atria.Common.Web.OData.Binders;
using Microsoft.AspNetCore.Mvc;
using System.Linq.Expressions;

namespace Atria.Common.Web.OData.Models;

[ModelBinder(typeof(ODataQueryParamsBinder))]
public class ODataQueryParams<T> : ODataQueryParamsGeneric
{
    [FromQuery(Name = "$orderby")]
    public ODataQueryOrderBy? OrderByOptions { get; set; }

    [FromQuery(Name = "$filter")]
    public ODataQueryFilter<T>? Filter { get; set; }

    [FromQuery(Name = "$skip")]
    public int? Skip { get; set; }

    [FromQuery(Name = "$top")]
    public int? Top { get; set; }

    [FromQuery(Name = "$search")]
    public string? Search { get; set; }

    public ODataQueryParams(
        Expression filter,
        ODataQueryOrderBy? orderByQueryOptions,
        int? skip,
        int? top,
        string? search)
    {
        Filter = new ODataQueryFilter<T>(filter);
        OrderByOptions = orderByQueryOptions;
        Skip = skip;
        Top = top;
        Search = search;
    }
}
