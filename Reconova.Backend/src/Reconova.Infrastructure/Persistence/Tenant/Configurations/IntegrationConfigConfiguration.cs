using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Reconova.Domain.Entities.Integrations;

namespace Reconova.Infrastructure.Persistence.Tenant.Configurations;

public class IntegrationConfigConfiguration : IEntityTypeConfiguration<IntegrationConfig>
{
    public void Configure(EntityTypeBuilder<IntegrationConfig> builder)
    {
        builder.ToTable("integration_configs");

        builder.HasKey(i => i.Id);
        builder.Property(i => i.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
        builder.Property(i => i.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(i => i.Type).HasColumnName("type").HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(i => i.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
        builder.Property(i => i.WebhookUrl).HasColumnName("webhook_url").HasMaxLength(1000);
        builder.Property(i => i.EncryptedApiKey).HasColumnName("encrypted_api_key").HasMaxLength(1000);
        builder.Property(i => i.Configuration).HasColumnName("configuration").HasColumnType("jsonb");
        builder.Property(i => i.IsActive).HasColumnName("is_active").HasDefaultValue(true);
        builder.Property(i => i.LastUsedAt).HasColumnName("last_used_at");
        builder.Property(i => i.FailureCount).HasColumnName("failure_count").HasDefaultValue(0);
        builder.Property(i => i.LastFailureAt).HasColumnName("last_failure_at");
        builder.Property(i => i.CreatedByUserId).HasColumnName("created_by_user_id").IsRequired();
        builder.Property(i => i.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
        builder.Property(i => i.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()");

        builder.HasIndex(i => i.TenantId);
    }
}
