using Atria.Common.Helpers.Json;
using System.Text.Json;

namespace Atria.Pipeline.Stores;

internal static class BlockDataJsonSerializer
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        MaxDepth = AtriaJsonSerializerOptions.MaxDepth,
    };

    public static byte[] SerializeToUtf8Bytes<T>(T data)
        => JsonSerializer.SerializeToUtf8Bytes(data, JsonOptions);

    public static T? Deserialize<T>(ReadOnlySpan<byte> json)
        => JsonSerializer.Deserialize<T>(json, JsonOptions);
}
