namespace Atria.Core.Data.Entities.Context.Interfaces;

public interface IAuditUpdated
{
    DateTimeOffset? UpdatedAt { get; set; }
}
