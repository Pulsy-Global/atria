using Atria.Core.Data.Entities.Feeds;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Atria.Core.Data.Context.Configuration;

public class FeedStatusChangeConfiguration : IEntityTypeConfiguration<FeedStatusChange>
{
    public void Configure(EntityTypeBuilder<FeedStatusChange> builder)
    {
        builder.HasKey(e => e.Id);

        builder.HasIndex(e => e.FeedId);
        builder.HasIndex(e => e.CreatedAt);
        builder.HasIndex(e => new { e.FeedId, e.CreatedAt });

        builder.HasOne(e => e.Feed)
            .WithMany(e => e.StatusChanges)
            .HasForeignKey(e => e.FeedId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
