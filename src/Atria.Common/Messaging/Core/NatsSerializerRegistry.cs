using NATS.Client.Core;
using NATS.Client.Serializers.Json;

namespace Atria.Common.Messaging.Core;

public sealed class NatsSerializerRegistry : INatsSerializerRegistry
{
    public static readonly NatsSerializerRegistry Default = new();

    public INatsSerialize<T> GetSerializer<T>() =>
        typeof(T) == typeof(byte[])
            ? (INatsSerialize<T>)NatsRawSerializer<byte[]>.Default
            : NatsJsonSerializer<T>.Default;

    public INatsDeserialize<T> GetDeserializer<T>() =>
        typeof(T) == typeof(byte[])
            ? (INatsDeserialize<T>)NatsRawSerializer<byte[]>.Default
            : NatsJsonSerializer<T>.Default;
}
