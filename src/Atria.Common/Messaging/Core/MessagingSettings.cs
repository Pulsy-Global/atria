namespace Atria.Common.Messaging.Core;

public class MessagingSettings
{
    public string Url { get; set; } = "localhost:4222";

    public string? Username { get; set; }

    public string? Password { get; set; }

    public string? ServiceName { get; set; }

    public string JetStreamPrefix { get; set; } = "js";

    public string KVBucketName { get; set; } = "atria-kv";

    public string DefaultFeedStream { get; set; } = "Atria";

    public Dictionary<string, MessagingStreamConfig> Streams { get; set; } = new();

    public Dictionary<string, MessagingKVConfig> KVs { get; set; } = new();

    public MessagingStreamConfig? DefaultStream { get; set; }
}
