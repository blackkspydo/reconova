using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Reconova.Domain.Common.Enums;
using Reconova.Domain.Entities.Integrations;

namespace Reconova.Infrastructure.Persistence.Control.Configurations;

public class PlatformApiKeyConfiguration : IEntityTypeConfiguration<PlatformApiKey>
{
    public void Configure(EntityTypeBuilder<PlatformApiKey> builder)
    {
        builder.ToTable("platform_api_keys");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
        builder.Property(p => p.Provider).HasColumnName("provider").HasMaxLength(100).IsRequired();
        builder.Property(p => p.ApiKeyEncrypted).HasColumnName("api_key_encrypted").HasMaxLength(500).IsRequired();
        builder.Property(p => p.RateLimit).HasColumnName("rate_limit");
        builder.Property(p => p.MonthlyQuota).HasColumnName("monthly_quota");
        builder.Property(p => p.Status).HasColumnName("status").HasMaxLength(50).HasConversion<string>().HasDefaultValue(PlatformApiKeyStatus.Active);
        builder.Property(p => p.UsageCount).HasColumnName("usage_count").HasDefaultValue(0);
        builder.Property(p => p.UsageResetAt).HasColumnName("usage_reset_at");
        builder.Property(p => p.AddedByUserId).HasColumnName("added_by_user_id").IsRequired();
        builder.Property(p => p.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
        builder.Property(p => p.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()");

        builder.HasIndex(p => p.Provider);
    }
}
