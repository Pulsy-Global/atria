using System.Text.Json;

namespace Atria.Common.Helpers.Json.Validators;

public static class JsonValidator
{
    public static bool IsValidJson(object? data)
    {
        var json = JsonSerializer.Serialize(data);

        return IsValidJson(json);
    }

    public static bool IsValidJson(string jsonString)
    {
        if (string.IsNullOrWhiteSpace(jsonString))
        {
            return false;
        }

        try
        {
            using var doc = JsonDocument.Parse(jsonString);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
