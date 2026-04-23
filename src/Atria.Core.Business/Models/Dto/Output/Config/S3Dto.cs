using Atria.Core.Business.Mapper.Attributes;
using Atria.Core.Data.Entities.Enums;
using Atria.Core.Data.Entities.Outputs.Config;

namespace Atria.Core.Business.Models.Dto.Output.Config;

[ConfigMapping(OutputType.S3, typeof(S3OutputConfig), typeof(S3Dto))]
public class S3Dto : ConfigBaseDto
{
    public string BucketName { get; set; }

    public string Region { get; set; }

    public string AccessKeyId { get; set; }

    public string SecretAccessKey { get; set; }

    public string Prefix { get; set; }

    public string FileFormat { get; set; } = "json";

    public bool CompressionEnabled { get; set; } = false;

    public int TimeoutSeconds { get; set; } = 30;
}
