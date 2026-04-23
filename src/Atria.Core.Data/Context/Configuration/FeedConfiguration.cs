using Atria.Core.Data.Entities.Feeds;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Atria.Core.Data.Context.Configuration;

public class FeedConfiguration : IEntityTypeConfiguration<Feed>
{
    public void Configure(EntityTypeBuilder<Feed> builder)
    {
        builder.HasKey(e => e.Id);

        builder.HasIndex(e => e.Name);

        builder.HasIndex(e => e.NetworkId);
        builder.HasIndex(e => e.Status);

        builder.Property(e => e.SearchContent);

        builder.HasIndex(e => e.SearchContent)
            .HasMethod("gin")
            .HasOperators("gin_trgm_ops");

        builder.Property(e => e.StartBlock)
            .HasColumnType("numeric(20,0)")
            .HasConversion<decimal?>();

        builder.Property(e => e.EndBlock)
            .HasColumnType("numeric(20,0)")
            .HasConversion<decimal?>();

        builder.HasMany(e => e.FeedOutputs)
            .WithOne(e => e.Feed)
            .HasForeignKey(e => e.FeedId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
