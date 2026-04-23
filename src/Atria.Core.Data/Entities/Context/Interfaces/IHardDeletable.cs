using System.ComponentModel.DataAnnotations.Schema;

namespace Atria.Core.Data.Entities.Context.Interfaces;

public interface IHardDeletable
{
    /// <summary>
    /// Gets or sets a value indicating whether an entity has to be hard deleted by EF.
    /// </summary>
    [NotMapped]
    public bool IsHardDeleted { get; set; }
}
