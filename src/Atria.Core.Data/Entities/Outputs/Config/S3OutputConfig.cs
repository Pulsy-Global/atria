using Atria.Core.Data.Entities.Enums;

namespace Atria.Core.Data.Entities.Outputs.Config;

public class S3OutputConfig : OutputConfigBase
{
    public override OutputType OutputType => OutputType.S3;

    public string BucketName { get; set; }

    public string Region { get; set; }

    public string AccessKeyId { get; set; }

    public string SecretAccessKey { get; set; }

    public string Prefix { get; set; }

    public string FileFormat { get; set; } = "json";

    public bool CompressionEnabled { get; set; } = false;

    public int TimeoutSeconds { get; set; } = 30;
}
