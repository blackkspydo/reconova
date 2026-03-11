using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Reconova.Domain.Entities.Compliance;

namespace Reconova.Infrastructure.Persistence.Tenant.Configurations;

public class TenantComplianceSelectionConfiguration : IEntityTypeConfiguration<TenantComplianceSelection>
{
    public void Configure(EntityTypeBuilder<TenantComplianceSelection> builder)
    {
        builder.ToTable("tenant_compliance_selections");

        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
        builder.Property(t => t.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(t => t.FrameworkId).HasColumnName("framework_id").IsRequired();
        builder.Property(t => t.EnabledAt).HasColumnName("enabled_at").IsRequired();
        builder.Property(t => t.DisabledAt).HasColumnName("disabled_at");
        builder.Property(t => t.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
        builder.Property(t => t.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()");

        builder.HasIndex(t => new { t.TenantId, t.FrameworkId });
    }
}
