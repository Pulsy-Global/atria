namespace Atria.Business.Models.Options;

public class S3StorageOptions
{
    public const string SectionName = "FileSystem:S3";
    public string Endpoint { get; set; }
    public string BucketName { get; set; }
    public string AccessKey { get; set; }
    public string SecretKey { get; set; }
    public string? Region { get; set; }
    public bool ForcePathStyle { get; set; } = true;
    public bool DisablePayloadSigning { get; set; } = true;
}
