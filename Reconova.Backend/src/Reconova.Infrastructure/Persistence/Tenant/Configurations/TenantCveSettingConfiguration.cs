using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Reconova.Domain.Entities.Cve;

namespace Reconova.Infrastructure.Persistence.Tenant.Configurations;

public class TenantCveSettingConfiguration : IEntityTypeConfiguration<TenantCveSetting>
{
    public void Configure(EntityTypeBuilder<TenantCveSetting> builder)
    {
        builder.ToTable("tenant_cve_settings");

        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
        builder.Property(t => t.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(t => t.DigestTimeUtc).HasColumnName("digest_time_utc").HasMaxLength(5).HasDefaultValue("09:00");
        builder.Property(t => t.DigestEnabled).HasColumnName("digest_enabled").HasDefaultValue(true);
        builder.Property(t => t.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
        builder.Property(t => t.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()");

        builder.HasIndex(t => t.TenantId).IsUnique();
    }
}
