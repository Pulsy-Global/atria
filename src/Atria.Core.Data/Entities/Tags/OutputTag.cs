using Atria.Core.Data.Entities.Context;
using Atria.Core.Data.Entities.Outputs;
using System.ComponentModel.DataAnnotations;

namespace Atria.Core.Data.Entities.Tags;

public class OutputTag : BaseEntity<Guid>
{
    [Required]
    public Guid OutputId { get; set; }

    [Required]
    public Guid TagId { get; set; }

    public Output Output { get; set; } = null!;

    public Tag Tag { get; set; } = null!;
}
