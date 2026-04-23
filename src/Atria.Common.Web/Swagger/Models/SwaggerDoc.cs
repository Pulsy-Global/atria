namespace Atria.Common.Web.Swagger.Models;

public class SwaggerDoc
{
    public string Version { get; set; }

    public string Title { get; set; }

    public string Area { get; set; }

    public string GetId()
    {
        if (!string.IsNullOrEmpty(Area))
        {
            return Area + "-" + Version;
        }

        return Version;
    }
}
