using Atria.Core.Data.Entities.Tags;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Atria.Core.Data.Context.Configuration;

public class FeedTagConfiguration : IEntityTypeConfiguration<FeedTag>
{
    public void Configure(EntityTypeBuilder<FeedTag> builder)
    {
        builder.HasKey(e => e.Id);

        builder.HasIndex(e => new { e.FeedId, e.TagId })
            .IsUnique();

        builder.HasOne(e => e.Feed)
            .WithMany(e => e.FeedTags)
            .HasForeignKey(e => e.FeedId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Tag)
            .WithMany(e => e.FeedTags)
            .HasForeignKey(e => e.TagId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
