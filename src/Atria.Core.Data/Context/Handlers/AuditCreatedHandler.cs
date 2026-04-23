using Atria.Core.Data.Context.Handlers.Abstractions;
using Atria.Core.Data.Entities.Context.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Atria.Core.Data.Context.Handlers;

public class AuditCreatedHandler : ISaveChangesHandler
{
    public Task HandleAsync(ChangeTracker changeTracker)
    {
        var added = changeTracker.Entries()
            .Where(x => x.State == EntityState.Added)
            .Select(x => x.Entity)
            .ToList();

        added.ForEach(entity =>
        {
            if (entity is IAuditCreated auditable)
            {
                auditable.CreatedAt = DateTimeOffset.UtcNow;
            }
        });

        return Task.CompletedTask;
    }
}
