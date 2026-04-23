using Microsoft.AspNetCore.Mvc;

namespace Atria.Common.Web.Models.Errors;

public class DefaultProblemDetails : ProblemDetails
{
    public string ErrorCode { get; set; }
}
