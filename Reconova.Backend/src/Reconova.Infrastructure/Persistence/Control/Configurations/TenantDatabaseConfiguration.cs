using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Reconova.Domain.Common.Enums;
using Reconova.Domain.Entities.Identity;

namespace Reconova.Infrastructure.Persistence.Control.Configurations;

public class TenantDatabaseConfiguration : IEntityTypeConfiguration<TenantDatabase>
{
    public void Configure(EntityTypeBuilder<TenantDatabase> builder)
    {
        builder.ToTable("tenant_databases");

        builder.HasKey(d => d.Id);
        builder.Property(d => d.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
        builder.Property(d => d.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(d => d.ServerHost).HasColumnName("server_host").HasMaxLength(255).IsRequired();
        builder.Property(d => d.ServerPort).HasColumnName("server_port").HasDefaultValue(5432);
        builder.Property(d => d.DatabaseName).HasColumnName("database_name").HasMaxLength(100).IsRequired();
        builder.Property(d => d.Username).HasColumnName("username").HasMaxLength(100).IsRequired();
        builder.Property(d => d.EncryptedPassword).HasColumnName("encrypted_password").HasMaxLength(500).IsRequired();
        builder.Property(d => d.IsProvisioned).HasColumnName("is_provisioned").HasDefaultValue(false);
        builder.Property(d => d.Status).HasColumnName("status").HasMaxLength(50).HasConversion<string>().HasDefaultValue(TenantDatabaseStatus.Provisioning);
        builder.Property(d => d.ProvisionedAt).HasColumnName("provisioned_at");
        builder.Property(d => d.MigrationVersion).HasColumnName("migration_version").HasMaxLength(50);
        builder.Property(d => d.TemplateVersion).HasColumnName("template_version").HasMaxLength(50);
        builder.Property(d => d.RetryCount).HasColumnName("retry_count").HasDefaultValue(0);
        builder.Property(d => d.LastRetryAt).HasColumnName("last_retry_at");
        builder.Property(d => d.BackedUpAt).HasColumnName("backed_up_at");
        builder.Property(d => d.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
        builder.Property(d => d.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()");

        builder.HasIndex(d => d.TenantId).IsUnique();
    }
}
