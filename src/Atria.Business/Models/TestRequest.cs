using Atria.Core.Data.Entities.Enums;

namespace Atria.Business.Models;

public class TestRequest
{
    public string BlockchainId { get; set; }

    public AtriaDataType DataType { get; set; }

    public string BlockNumber { get; set; }

    public string? FilterCode { get; set; }

    public string? FunctionCode { get; set; }

    public bool ExecuteOutputs { get; set; } = false;

    public List<string>? OutputsIds { get; set; } = null;
}
