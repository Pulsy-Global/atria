using Atria.Core.Business.Models.Dto.Output.Config;
using Atria.Core.Business.Models.Dto.Output.Config.Attributes;
using Atria.Core.Business.Models.Dto.Output.Config.Converters;
using Atria.Core.Data.Entities.Enums;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace Atria.Core.Business.Models.Dto.Output;

[JsonConverter(typeof(DynamicTypeConverter))]
public class CreateOutputDto
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; }

    [MaxLength(500)]
    public string? Description { get; set; }

    public OutputType Type { get; set; }

    [DynamicType(nameof(Type))]
    public ConfigBaseDto Config { get; set; }

    public List<Guid> TagIds { get; set; } = new List<Guid>();
}
