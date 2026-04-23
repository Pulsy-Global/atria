using Atria.Common.Web.Models.Abstractions;
using Atria.Common.Web.OData.Attributes;
using Atria.Core.Data.Entities.Enums;
using Newtonsoft.Json;

namespace Atria.Core.Business.Models.Dto.Feed;

public class FeedDto : IODataDto
{
    public Guid Id { get; set; }

    public string Name { get; set; }

    public string Version { get; set; }

    public string? Description { get; set; }

    public FeedStatus Status { get; set; }

    public string NetworkId { get; set; }

    public AtriaDataType DataType { get; set; }

    [JsonIgnore]
    public ulong? StartBlockNumeric { get; set; }

    [JsonIgnore]
    public ulong? EndBlockNumeric { get; set; }

    [ODataHandleAsString(nameof(StartBlockNumeric))]
    public string? StartBlock { get; set; }

    [ODataHandleAsString(nameof(EndBlockNumeric))]
    public string? EndBlock { get; set; }

    public ErrorHandlingStrategy ErrorHandling { get; set; }

    public string? FilterCode { get; set; }

    public string? FunctionCode { get; set; }

    [ODataCollectionMapping("FeedTags", "TagId")]
    public List<Guid> TagIds { get; set; } = new List<Guid>();

    [ODataCollectionMapping("FeedOutputs", "OutputId")]
    public List<Guid> OutputIds { get; set; } = new List<Guid>();

    public int BlockDelay { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }
}
