using Atria.Core.Data.Context.Handlers.Abstractions;
using Atria.Core.Data.Entities.Deploys;
using Atria.Core.Data.Entities.Feeds;
using Atria.Core.Data.Entities.Outputs;
using Atria.Core.Data.Entities.Tags;
using Atria.Core.Data.Extensions;
using Microsoft.EntityFrameworkCore;

namespace Atria.Core.Data.Context;

public class AtriaDbContext : DbContext
{
    private readonly IEnumerable<ISaveChangesHandler> _changesHandlers;

    public AtriaDbContext(
        DbContextOptions<AtriaDbContext> options,
        IEnumerable<ISaveChangesHandler> changesHandlers)
        : base(options)
    {
        ChangeTracker.LazyLoadingEnabled = false;
        _changesHandlers = changesHandlers;
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.HasPostgresExtension("pg_trgm");

        var assembly = typeof(AtriaDbContext).Assembly;

        base.OnModelCreating(builder);

        builder.ApplyConfigurationsFromAssembly(assembly);
        builder.EnableSoftDelete();
    }

    public DbSet<Feed> Feeds { get; set; }
    public DbSet<Deploy> Deploys { get; set; }
    public DbSet<FeedOutput> FeedOutputs { get; set; }
    public DbSet<FeedStatusChange> FeedStatusChanges { get; set; }
    public DbSet<DeployStatusChange> DeployStatusChanges { get; set; }
    public DbSet<Output> Outputs { get; set; }
    public DbSet<Tag> Tags { get; set; }
    public DbSet<FeedTag> FeedTags { get; set; }
    public DbSet<OutputTag> OutputTags { get; set; }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken)
    {
        ChangeTracker.DetectChanges();

        foreach (var handler in _changesHandlers)
        {
            await handler.HandleAsync(ChangeTracker);
        }

        var result = await base.SaveChangesAsync(cancellationToken);

        return result;
    }
}
