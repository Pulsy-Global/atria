namespace Atria.Business.Models.Options;

public class LocalStorageOptions
{
    public const string SectionName = "FileSystem:Local";

    public string BasePath { get; set; } = "uploads";
}
