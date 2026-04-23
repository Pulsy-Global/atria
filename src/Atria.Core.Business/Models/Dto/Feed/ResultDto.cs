namespace Atria.Core.Business.Models.Dto.Feed;

public class ResultDto
{
    public ulong SeqNumber { get; set; }
    public string Data { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public int SizeBytes { get; set; }
    public bool IsTestExecution { get; set; }
    public string? BlockNumber { get; set; }
}
