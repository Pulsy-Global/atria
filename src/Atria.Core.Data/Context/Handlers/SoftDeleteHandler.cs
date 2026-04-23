using Atria.Core.Data.Context.Handlers.Abstractions;
using Atria.Core.Data.Entities.Context.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Atria.Core.Data.Context.Handlers;

public class SoftDeleteHandler : ISaveChangesHandler
{
    public async Task HandleAsync(ChangeTracker changeTracker)
    {
        await CascadeDeleteAsync(changeTracker);
        ApplySoftDelete(changeTracker);
    }

    // Walks cascade navigations of every Deleted entry and marks children Deleted,
    // auto-loading navigations that were not Include'd by the caller. Prevents orphans
    // in soft-deleted graphs, where EF's native cascade is suppressed by ApplySoftDelete.
    private static async Task CascadeDeleteAsync(ChangeTracker changeTracker)
    {
        var queue = new Queue<EntityEntry>(
            changeTracker.Entries().Where(e => e.State == EntityState.Deleted));

        var visited = new HashSet<object>(ReferenceEqualityComparer.Instance);

        while (queue.Count > 0)
        {
            var entry = queue.Dequeue();

            if (!visited.Add(entry.Entity))
            {
                continue;
            }

            foreach (var nav in entry.Navigations)
            {
                if (nav.Metadata is not INavigation navigation)
                {
                    continue;
                }

                var fk = navigation.ForeignKey;

                if (fk.DeleteBehavior != DeleteBehavior.Cascade &&
                    fk.DeleteBehavior != DeleteBehavior.ClientCascade)
                {
                    continue;
                }

                if (fk.PrincipalEntityType != entry.Metadata)
                {
                    continue;
                }

                if (!nav.IsLoaded)
                {
                    await nav.LoadAsync();
                }

                foreach (var child in EnumerateChildren(nav))
                {
                    if (child is IAuditDeleted audited && audited.DeletedAt != null)
                    {
                        continue;
                    }

                    var childEntry = entry.Context.Entry(child);

                    if (childEntry.State != EntityState.Deleted)
                    {
                        childEntry.State = EntityState.Deleted;
                    }

                    queue.Enqueue(childEntry);
                }
            }
        }
    }

    private static void ApplySoftDelete(ChangeTracker changeTracker)
    {
        var now = DateTimeOffset.UtcNow;

        var entries = changeTracker.Entries()
            .Where(e => e.State == EntityState.Deleted)
            .ToList();

        foreach (var entry in entries)
        {
            var hardDelete = entry.Entity is IHardDeletable hardDeletable && hardDeletable.IsHardDeleted;

            if (!hardDelete && entry.Entity is IAuditDeleted auditable)
            {
                entry.State = EntityState.Unchanged;
                auditable.DeletedAt = now;
            }
        }
    }

    private static IEnumerable<object> EnumerateChildren(NavigationEntry nav)
    {
        switch (nav)
        {
            case CollectionEntry collection when collection.CurrentValue != null:
                foreach (var item in collection.CurrentValue)
                {
                    if (item != null)
                    {
                        yield return item;
                    }
                }

                break;

            case ReferenceEntry reference when reference.CurrentValue != null:
                yield return reference.CurrentValue;
                break;
        }
    }
}
