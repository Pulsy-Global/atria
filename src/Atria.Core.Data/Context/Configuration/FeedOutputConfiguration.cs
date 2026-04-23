using Atria.Core.Data.Entities.Feeds;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Atria.Core.Data.Context.Configuration;

public class FeedOutputConfiguration : IEntityTypeConfiguration<FeedOutput>
{
    public void Configure(EntityTypeBuilder<FeedOutput> builder)
    {
        builder.HasKey(e => e.Id);

        builder.HasIndex(e => new { e.FeedId, e.OutputId }).IsUnique();
    }
}
