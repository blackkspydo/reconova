using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Reconova.Domain.Entities.FeatureFlags;

namespace Reconova.Infrastructure.Persistence.Control.Configurations;

public class FeatureFlagConfiguration : IEntityTypeConfiguration<FeatureFlag>
{
    public void Configure(EntityTypeBuilder<FeatureFlag> builder)
    {
        builder.ToTable("feature_flags");

        builder.HasKey(f => f.Id);
        builder.Property(f => f.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
        builder.Property(f => f.Key).HasColumnName("key").HasMaxLength(200).IsRequired();
        builder.Property(f => f.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
        builder.Property(f => f.Description).HasColumnName("description").HasMaxLength(500);
        builder.Property(f => f.Type).HasColumnName("type").HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(f => f.IsEnabled).HasColumnName("is_enabled").HasDefaultValue(true);
        builder.Property(f => f.DefaultEnabled).HasColumnName("default_enabled").HasDefaultValue(true);
        builder.Property(f => f.Module).HasColumnName("module").HasMaxLength(50);
        builder.Property(f => f.AllowedPlans).HasColumnName("allowed_plans").HasColumnType("jsonb");
        builder.Property(f => f.RolloutPercentage).HasColumnName("rollout_percentage");
        builder.Property(f => f.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
        builder.Property(f => f.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()");

        builder.HasIndex(f => f.Key).IsUnique();
    }
}
