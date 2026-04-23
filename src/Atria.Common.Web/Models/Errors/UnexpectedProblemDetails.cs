using Microsoft.AspNetCore.Mvc;

namespace Atria.Common.Web.Models.Errors;

public class UnexpectedProblemDetails : ProblemDetails
{
    public DateTimeOffset Timestamp { get; set; }

    public string TraceId { get; set; }
}
