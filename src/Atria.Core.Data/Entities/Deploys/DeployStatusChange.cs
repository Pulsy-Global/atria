using Atria.Core.Data.Entities.Context;
using Atria.Core.Data.Entities.Context.Interfaces;
using Atria.Core.Data.Entities.Enums;
using System.ComponentModel.DataAnnotations;

namespace Atria.Core.Data.Entities.Deploys;

public class DeployStatusChange : BaseEntity<Guid>, IAuditCreated, IAuditDeleted
{
    [Required]
    public Guid DeployId { get; set; }

    [Required]
    public DeployStatus FromStatus { get; set; }

    [Required]
    public DeployStatus ToStatus { get; set; }

    public Deploy Deploy { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? DeletedAt { get; set; }
}
