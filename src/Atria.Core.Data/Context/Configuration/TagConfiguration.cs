using Atria.Core.Data.Entities.Tags;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Atria.Core.Data.Context.Configuration;

public class TagConfiguration : IEntityTypeConfiguration<Tag>
{
    public void Configure(EntityTypeBuilder<Tag> builder)
    {
        builder.HasKey(e => e.Id);

        builder.HasIndex(e => new { e.Name, e.Type })
            .IsUnique()
            .HasFilter("\"DeletedAt\" IS NULL");

        builder.HasIndex(e => e.Type);

        builder.HasMany(e => e.FeedTags)
            .WithOne(e => e.Tag)
            .HasForeignKey(e => e.TagId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(e => e.OutputTags)
            .WithOne(e => e.Tag)
            .HasForeignKey(e => e.TagId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
