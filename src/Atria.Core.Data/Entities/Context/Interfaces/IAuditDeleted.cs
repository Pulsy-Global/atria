namespace Atria.Core.Data.Entities.Context.Interfaces;

public interface IAuditDeleted
{
    DateTimeOffset? DeletedAt { get; set; }
}
