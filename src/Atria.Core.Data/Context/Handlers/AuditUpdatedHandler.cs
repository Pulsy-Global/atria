using Atria.Core.Data.Context.Handlers.Abstractions;
using Atria.Core.Data.Entities.Context.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Atria.Core.Data.Context.Handlers;

public class AuditUpdatedHandler : ISaveChangesHandler
{
    public Task HandleAsync(ChangeTracker changeTracker)
    {
        var updated = changeTracker.Entries()
            .Where(x => x.State == EntityState.Modified)
            .Where(CheckIfChanged)
            .Select(x => x.Entity)
            .ToList();

        updated.ForEach(entity =>
        {
            if (entity is IAuditUpdated auditable)
            {
                auditable.UpdatedAt = DateTimeOffset.UtcNow;
            }
        });

        return Task.CompletedTask;
    }

    private bool CheckIfChanged(EntityEntry entityEntry)
    {
        return entityEntry.Properties.Any(x => CheckIfChanged(x));
    }

    private bool CheckIfChanged(PropertyEntry propertyEntry)
    {
        if (propertyEntry.IsTemporary)
        {
            return true;
        }

        var oneOfValuesNull = (propertyEntry.OriginalValue == null && propertyEntry.CurrentValue != null) ||
                              (propertyEntry.OriginalValue != null && propertyEntry.CurrentValue == null);

        if (oneOfValuesNull)
        {
            return true;
        }

        if (propertyEntry.OriginalValue == null && propertyEntry.CurrentValue == null)
        {
            return false;
        }

        return !propertyEntry?.OriginalValue?.Equals(propertyEntry.CurrentValue) ?? false;
    }
}
