namespace Atria.Common.Messaging.Core;

public class MessagingKVConfig
{
    public int MaxAgeMinutes { get; set; } = 1440;

    public int MaxAgeSeconds { get; set; }

    public long? MaxSizeMb { get; set; }

    public int History { get; set; } = 1;

    public int Replicas { get; set; } = 1;

    public bool Compression { get; set; }
}
