using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Reconova.Domain.Entities.FeatureFlags;

namespace Reconova.Infrastructure.Persistence.Control.Configurations;

public class TenantFeatureOverrideConfiguration : IEntityTypeConfiguration<TenantFeatureOverride>
{
    public void Configure(EntityTypeBuilder<TenantFeatureOverride> builder)
    {
        builder.ToTable("tenant_feature_overrides");

        builder.HasKey(o => o.Id);
        builder.Property(o => o.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
        builder.Property(o => o.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(o => o.FeatureFlagId).HasColumnName("feature_flag_id").IsRequired();
        builder.Property(o => o.IsEnabled).HasColumnName("is_enabled").IsRequired();
        builder.Property(o => o.Value).HasColumnName("value").HasMaxLength(500);
        builder.Property(o => o.Reason).HasColumnName("reason").HasMaxLength(500);
        builder.Property(o => o.ExpiresAt).HasColumnName("expires_at");
        builder.Property(o => o.SetByUserId).HasColumnName("set_by_user_id").IsRequired();
        builder.Property(o => o.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
        builder.Property(o => o.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()");

        builder.HasIndex(o => new { o.TenantId, o.FeatureFlagId }).IsUnique();

        builder.HasOne(o => o.FeatureFlag)
            .WithMany(f => f.TenantOverrides)
            .HasForeignKey(o => o.FeatureFlagId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(o => o.Tenant)
            .WithMany()
            .HasForeignKey(o => o.TenantId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
