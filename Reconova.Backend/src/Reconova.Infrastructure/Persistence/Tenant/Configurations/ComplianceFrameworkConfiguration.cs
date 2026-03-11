using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Reconova.Domain.Entities.Compliance;

namespace Reconova.Infrastructure.Persistence.Tenant.Configurations;

public class ComplianceFrameworkConfiguration : IEntityTypeConfiguration<ComplianceFramework>
{
    public void Configure(EntityTypeBuilder<ComplianceFramework> builder)
    {
        builder.ToTable("compliance_frameworks");

        builder.HasKey(f => f.Id);
        builder.Property(f => f.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
        builder.Property(f => f.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
        builder.Property(f => f.Code).HasColumnName("code").HasMaxLength(50).IsRequired();
        builder.Property(f => f.Description).HasColumnName("description").HasMaxLength(2000);
        builder.Property(f => f.Version).HasColumnName("version").HasMaxLength(20).IsRequired();
        builder.Property(f => f.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(f => f.IsSystemFramework).HasColumnName("is_system_framework").HasDefaultValue(false);
        builder.Property(f => f.TenantId).HasColumnName("tenant_id");
        builder.Property(f => f.ControlCount).HasColumnName("control_count").HasDefaultValue(0);
        builder.Property(f => f.PublishedAt).HasColumnName("published_at");
        builder.Property(f => f.GracePeriodDays).HasColumnName("grace_period_days");
        builder.Property(f => f.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
        builder.Property(f => f.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()");

        builder.HasIndex(f => f.Code);
    }
}
