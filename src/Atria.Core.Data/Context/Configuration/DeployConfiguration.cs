using Atria.Core.Data.Entities.Deploys;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Atria.Core.Data.Context.Configuration;

public class DeployConfiguration : IEntityTypeConfiguration<Deploy>
{
    public void Configure(EntityTypeBuilder<Deploy> builder)
    {
        builder.HasKey(e => e.Id);

        builder.HasIndex(e => e.FeedId);
        builder.HasIndex(e => e.CreatedAt);
        builder.HasIndex(e => new { e.FeedId, e.Version });

        builder.Property(e => e.Status)
            .HasConversion<string>();

        builder.HasOne(e => e.Feed)
            .WithMany(f => f.Deploys)
            .HasForeignKey(e => e.FeedId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
