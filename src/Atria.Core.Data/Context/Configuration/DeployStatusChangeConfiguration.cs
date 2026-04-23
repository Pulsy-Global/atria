using Atria.Core.Data.Entities.Deploys;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Atria.Core.Data.Context.Configuration;

public class DeployStatusChangeConfiguration : IEntityTypeConfiguration<DeployStatusChange>
{
    public void Configure(EntityTypeBuilder<DeployStatusChange> builder)
    {
        builder.HasKey(e => e.Id);

        builder.HasIndex(e => e.DeployId);
        builder.HasIndex(e => e.CreatedAt);
        builder.HasIndex(e => new { e.DeployId, e.CreatedAt });

        builder.HasOne(e => e.Deploy)
            .WithMany(e => e.StatusChanges)
            .HasForeignKey(e => e.DeployId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
