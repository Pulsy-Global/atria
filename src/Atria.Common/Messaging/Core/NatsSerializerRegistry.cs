using NATS.Client.Core;

namespace Atria.Common.Messaging.Core;

public sealed class NatsSerializerRegistry : INatsSerializerRegistry
{
    public static readonly NatsSerializerRegistry Default = new();

    public INatsSerialize<T> GetSerializer<T>() =>
        typeof(T) == typeof(byte[])
            ? (INatsSerialize<T>)NatsRawSerializer<byte[]>.Default
            : new AtriaNatsJsonSerializer<T>();

    public INatsDeserialize<T> GetDeserializer<T>() =>
        typeof(T) == typeof(byte[])
            ? (INatsDeserialize<T>)NatsRawSerializer<byte[]>.Default
            : new AtriaNatsJsonSerializer<T>();
}
