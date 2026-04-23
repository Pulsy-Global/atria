using Atria.Core.Data.Context.Converters;
using Atria.Core.Data.Entities.Outputs;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Atria.Core.Data.Context.Configuration;

public class OutputConfiguration : IEntityTypeConfiguration<Output>
{
    public void Configure(EntityTypeBuilder<Output> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.SearchContent);

        builder.HasIndex(e => e.SearchContent)
            .HasMethod("gin")
            .HasOperators("gin_trgm_ops");

        builder.HasIndex(e => e.Type);

        builder.Property(e => e.Config)
            .HasColumnType("jsonb")
            .HasConversion<OutputConfigConverter>();

        builder.HasMany(e => e.FeedOutputs)
            .WithOne(e => e.Output)
            .HasForeignKey(e => e.OutputId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
