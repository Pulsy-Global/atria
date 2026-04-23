using NATS.Client.JetStream.Models;

namespace Atria.Common.Messaging.Core;

public class MessagingStreamConfig
{
    public string Name { get; set; } = "Atria";

    public string[] Subjects { get; set; } = [];

    public int MaxSizeMb { get; set; } = -1;

    public int MaxAgeMinutes { get; set; } = 60;

    public long MaxMessages { get; set; } = -1;

    public long MaxMessagesPerSubject { get; set; } = -1;

    public StreamConfigDiscard DiscardPolicy { get; set; } = StreamConfigDiscard.Old;

    public bool DiscardNewPerSubject { get; set; }

    public int Replicas { get; set; } = 1;

    public long GetMaxSizeBytes() => MaxSizeMb <= 0 ? -1 : MaxSizeMb * 1024L * 1024L;
    public TimeSpan GetMaxAge() => TimeSpan.FromMinutes(MaxAgeMinutes);
}
