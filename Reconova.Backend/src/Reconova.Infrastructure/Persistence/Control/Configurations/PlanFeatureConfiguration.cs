using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Reconova.Domain.Entities.FeatureFlags;

namespace Reconova.Infrastructure.Persistence.Control.Configurations;

public class PlanFeatureConfiguration : IEntityTypeConfiguration<PlanFeature>
{
    public void Configure(EntityTypeBuilder<PlanFeature> builder)
    {
        builder.ToTable("plan_features");

        builder.HasKey(pf => pf.Id);
        builder.Property(pf => pf.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
        builder.Property(pf => pf.PlanId).HasColumnName("plan_id").IsRequired();
        builder.Property(pf => pf.FeatureFlagId).HasColumnName("feature_flag_id").IsRequired();
        builder.Property(pf => pf.IsEnabled).HasColumnName("is_enabled").HasDefaultValue(true);
        builder.Property(pf => pf.Value).HasColumnName("value").HasMaxLength(500);
        builder.Property(pf => pf.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
        builder.Property(pf => pf.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()");

        builder.HasIndex(pf => new { pf.PlanId, pf.FeatureFlagId }).IsUnique();

        builder.HasOne(pf => pf.Plan)
            .WithMany(p => p.PlanFeatures)
            .HasForeignKey(pf => pf.PlanId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(pf => pf.FeatureFlag)
            .WithMany(f => f.PlanFeatures)
            .HasForeignKey(pf => pf.FeatureFlagId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
