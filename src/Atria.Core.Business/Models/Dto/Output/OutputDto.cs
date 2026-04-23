using Atria.Common.Web.Models.Abstractions;
using Atria.Common.Web.OData.Attributes;
using Atria.Core.Business.Models.Dto.Output.Config;
using Atria.Core.Data.Entities.Enums;

namespace Atria.Core.Business.Models.Dto.Output;

public class OutputDto : IODataDto
{
    public Guid Id { get; set; }

    public string Name { get; set; }

    public string? Description { get; set; }

    public OutputType Type { get; set; }

    public ConfigBaseDto Config { get; set; }

    [ODataCollectionMapping("OutputTags", "TagId")]
    public List<Guid> TagIds { get; set; } = new List<Guid>();

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }
}
