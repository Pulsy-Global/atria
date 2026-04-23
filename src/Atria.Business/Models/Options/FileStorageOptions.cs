namespace Atria.Business.Models.Options;

public class FileStorageOptions
{
    public const string SectionName = "FileStorage";

    public string UploadsPath { get; set; } = "uploads";

    public string FilterPath { get; set; } = "filters";

    public string FunctionPath { get; set; } = "functions";
}
