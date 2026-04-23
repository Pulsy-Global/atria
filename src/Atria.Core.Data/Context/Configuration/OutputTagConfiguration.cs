using Atria.Core.Data.Entities.Tags;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Atria.Core.Data.Context.Configuration;

public class OutputTagConfiguration : IEntityTypeConfiguration<OutputTag>
{
    public void Configure(EntityTypeBuilder<OutputTag> builder)
    {
        builder.HasKey(e => e.Id);

        builder.HasIndex(e => new { e.OutputId, e.TagId })
            .IsUnique();

        builder.HasOne(e => e.Output)
            .WithMany(e => e.OutputTags)
            .HasForeignKey(e => e.OutputId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Tag)
            .WithMany(e => e.OutputTags)
            .HasForeignKey(e => e.TagId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
