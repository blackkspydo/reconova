using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Reconova.Infrastructure.Persistence.Tenant.Configurations;

public class DomainConfiguration : IEntityTypeConfiguration<Reconova.Domain.Entities.Scanning.Domain>
{
    public void Configure(EntityTypeBuilder<Reconova.Domain.Entities.Scanning.Domain> builder)
    {
        builder.ToTable("domains");

        builder.HasKey(d => d.Id);
        builder.Property(d => d.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
        builder.Property(d => d.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(d => d.Name).HasColumnName("name").HasMaxLength(255).IsRequired();
        builder.Property(d => d.VerificationStatus).HasColumnName("verification_status").HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(d => d.VerificationToken).HasColumnName("verification_token").HasMaxLength(500);
        builder.Property(d => d.VerificationMethod).HasColumnName("verification_method").HasMaxLength(50);
        builder.Property(d => d.VerifiedAt).HasColumnName("verified_at");
        builder.Property(d => d.LastScanAt).HasColumnName("last_scan_at");
        builder.Property(d => d.IsDeleted).HasColumnName("is_deleted").HasDefaultValue(false);
        builder.Property(d => d.DeletedAt).HasColumnName("deleted_at");
        builder.Property(d => d.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
        builder.Property(d => d.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()");

        builder.HasIndex(d => new { d.TenantId, d.Name }).IsUnique().HasFilter("is_deleted = false");
        builder.HasIndex(d => d.TenantId);

        builder.HasQueryFilter(d => !d.IsDeleted);
    }
}
