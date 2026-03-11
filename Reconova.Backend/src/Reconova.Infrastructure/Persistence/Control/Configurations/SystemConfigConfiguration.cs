using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Reconova.Domain.Entities.Admin;

namespace Reconova.Infrastructure.Persistence.Control.Configurations;

public class SystemConfigConfiguration : IEntityTypeConfiguration<SystemConfig>
{
    public void Configure(EntityTypeBuilder<SystemConfig> builder)
    {
        builder.ToTable("system_configs");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
        builder.Property(c => c.Key).HasColumnName("key").HasMaxLength(200).IsRequired();
        builder.Property(c => c.Value).HasColumnName("value").HasMaxLength(2000).IsRequired();
        builder.Property(c => c.DefaultValue).HasColumnName("default_value").HasMaxLength(2000).IsRequired();
        builder.Property(c => c.DataType).HasColumnName("data_type").HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(c => c.Description).HasColumnName("description").HasMaxLength(500);
        builder.Property(c => c.Category).HasColumnName("category").HasMaxLength(100).IsRequired();
        builder.Property(c => c.IsCritical).HasColumnName("is_critical").HasDefaultValue(false);
        builder.Property(c => c.IsSensitive).HasColumnName("is_sensitive").HasDefaultValue(false);
        builder.Property(c => c.RequiresRestart).HasColumnName("requires_restart").HasDefaultValue(false);
        builder.Property(c => c.MinValue).HasColumnName("min_value").HasMaxLength(100);
        builder.Property(c => c.MaxValue).HasColumnName("max_value").HasMaxLength(100);
        builder.Property(c => c.AllowedValues).HasColumnName("allowed_values").HasMaxLength(1000);
        builder.Property(c => c.Unit).HasColumnName("unit").HasMaxLength(50);
        builder.Property(c => c.LastModifiedByUserId).HasColumnName("last_modified_by_user_id");
        builder.Property(c => c.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
        builder.Property(c => c.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()");

        builder.HasIndex(c => c.Key).IsUnique();
        builder.HasIndex(c => c.Category);
    }
}
