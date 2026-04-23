namespace Atria.Feed.Ingestor.Config.Options;

public class IngestorHttpClientOptions
{
    public Dictionary<string, string> OnReorgHeaders { get; set; } = new();
}
