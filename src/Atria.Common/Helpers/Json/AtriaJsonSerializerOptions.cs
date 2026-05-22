using System.Text.Json;

namespace Atria.Common.Helpers.Json;

public static class AtriaJsonSerializerOptions
{
    public const int MaxDepth = 256;

    public static JsonSerializerOptions Create(JsonSerializerOptions? defaults = null)
    {
        var options = defaults == null
            ? new JsonSerializerOptions()
            : new JsonSerializerOptions(defaults);

        options.MaxDepth = MaxDepth;
        return options;
    }
}
