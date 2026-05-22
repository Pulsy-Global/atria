using Atria.Common.Helpers.Json;
using NATS.Client.Core;
using System.Buffers;
using System.Text.Json;

namespace Atria.Common.Messaging.Core;

internal sealed class AtriaNatsJsonSerializer<T> : INatsSerializer<T>
{
    private static readonly JsonSerializerOptions JsonOptions =
        AtriaJsonSerializerOptions.Create(JsonSerializerOptions.Default);

    private static readonly JsonReaderOptions JsonReaderOptions = new()
    {
        MaxDepth = AtriaJsonSerializerOptions.MaxDepth,
    };

    private static readonly JsonWriterOptions JsonWriterOptions = new()
    {
        Indented = false,
        SkipValidation = true,
    };

    public void Serialize(IBufferWriter<byte> bufferWriter, T value)
    {
        using var writer = new Utf8JsonWriter(bufferWriter, JsonWriterOptions);
        JsonSerializer.Serialize(writer, value, JsonOptions);
    }

    public T? Deserialize(in ReadOnlySequence<byte> buffer)
    {
        if (buffer.Length == 0)
        {
            return default;
        }

        var reader = new Utf8JsonReader(buffer, JsonReaderOptions);
        return JsonSerializer.Deserialize<T>(ref reader, JsonOptions);
    }

    public INatsSerializer<T> CombineWith(INatsSerializer<T> next)
    {
        throw new NotSupportedException();
    }

    T? INatsDeserialize<T>.Deserialize(in ReadOnlySequence<byte> buffer)
        => Deserialize(buffer);
}
