namespace Atria.Core.Data.Entities.Context.Interfaces;

public interface IAuditCreated
{
    DateTimeOffset CreatedAt { get; set; }
}
