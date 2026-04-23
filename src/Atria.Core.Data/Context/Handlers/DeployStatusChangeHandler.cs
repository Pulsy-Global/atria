using Atria.Core.Data.Context.Handlers.Abstractions;
using Atria.Core.Data.Entities.Deploys;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Atria.Core.Data.Context.Handlers;

public class DeployStatusChangeHandler : ISaveChangesHandler
{
    public async Task HandleAsync(ChangeTracker changeTracker)
    {
        var deployEntries = changeTracker.Entries<Deploy>()
            .Where(e => e.State == EntityState.Modified)
            .ToList();

        foreach (var entry in deployEntries)
        {
            var statusProperty = entry.Property(d => d.Status);

            if (statusProperty.IsModified)
            {
                var originalStatus = statusProperty.OriginalValue;
                var currentStatus = statusProperty.CurrentValue;

                if (originalStatus != currentStatus)
                {
                    var statusChange = new DeployStatusChange
                    {
                        DeployId = entry.Entity.Id,
                        FromStatus = originalStatus,
                        ToStatus = currentStatus,
                    };

                    var context = entry.Context;

                    await context
                        .Set<DeployStatusChange>()
                        .AddAsync(statusChange);
                }
            }
        }
    }
}
