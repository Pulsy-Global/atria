using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Atria.Common.Web.Models.Errors;

public class DefaultValidationProblemDetails : ValidationProblemDetails
{
    public string? ErrorCode { get; set; }

    public DefaultValidationProblemDetails(ModelStateDictionary modelState)
        : base(modelState)
    {
    }

    public DefaultValidationProblemDetails(IDictionary<string, string[]> errors)
        : base(errors)
    {
    }
}
