using Atria.Core.Data.Context.Handlers.Abstractions;
using Atria.Core.Data.Entities.Feeds;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Atria.Core.Data.Context.Handlers;

public class FeedStatusChangeHandler : ISaveChangesHandler
{
    public async Task HandleAsync(ChangeTracker changeTracker)
    {
        var feedEntries = changeTracker.Entries<Feed>()
            .Where(e => e.State == EntityState.Modified)
            .ToList();

        foreach (var entry in feedEntries)
        {
            var statusProperty = entry.Property(f => f.Status);

            if (statusProperty.IsModified)
            {
                var originalStatus = statusProperty.OriginalValue;
                var currentStatus = statusProperty.CurrentValue;

                if (originalStatus != currentStatus)
                {
                    var statusChange = new FeedStatusChange
                    {
                        FeedId = entry.Entity.Id,
                        FromStatus = originalStatus,
                        ToStatus = currentStatus,
                    };

                    var context = entry.Context;

                    await context
                        .Set<FeedStatusChange>()
                        .AddAsync(statusChange);
                }
            }
        }
    }
}
