using Atria.Core.Data.Context.Handlers.Abstractions;
using Atria.Core.Data.Entities.Feeds;
using Atria.Core.Data.Entities.Outputs;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Atria.Core.Data.Context.Handlers;

public class SearchContentHandler : ISaveChangesHandler
{
    public Task HandleAsync(ChangeTracker changeTracker)
    {
        var feedEntries = changeTracker.Entries<Feed>()
            .Where(x => x.State == EntityState.Added || x.State == EntityState.Modified)
            .ToList();

        foreach (var entry in feedEntries)
        {
            var feed = entry.Entity;

            feed.SearchContent = string.Join(
                " ",
                new[] { feed.Name, feed.Description, feed.NetworkId, feed.Version, feed.DataType.ToString(), feed.ErrorHandling.ToString() }
                    .Where(s => !string.IsNullOrEmpty(s))
                    .Select(s => s!.ToLowerInvariant())
                    .Distinct());
        }

        var outputEntries = changeTracker.Entries<Output>()
            .Where(x => x.State == EntityState.Added || x.State == EntityState.Modified)
            .ToList();

        foreach (var entry in outputEntries)
        {
            var output = entry.Entity;

            output.SearchContent = string.Join(
                " ",
                new[] { output.Name, output.Description, output.Type.ToString() }
                    .Where(s => !string.IsNullOrEmpty(s))
                    .Select(s => s!.ToLowerInvariant())
                    .Distinct());
        }

        return Task.CompletedTask;
    }
}
