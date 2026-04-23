using Atria.Common.Web.Swagger.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Atria.Common.Web.Swagger.Options;

public class SwaggerServicesOptions
{
    public List<SwaggerDoc> Docs { get; set; }

    public List<Type> CustomGenericTypes { get; set; }

    public Action<SwaggerGenOptions> AdditionalOptions { get; set; }
}
