using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Reconova.Domain.Entities.Integrations;

namespace Reconova.Infrastructure.Persistence.Tenant.Configurations;

public class NotificationRuleConfiguration : IEntityTypeConfiguration<NotificationRule>
{
    public void Configure(EntityTypeBuilder<NotificationRule> builder)
    {
        builder.ToTable("notification_rules");

        builder.HasKey(n => n.Id);
        builder.Property(n => n.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
        builder.Property(n => n.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(n => n.IntegrationConfigId).HasColumnName("integration_config_id").IsRequired();
        builder.Property(n => n.EventType).HasColumnName("event_type").HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(n => n.MinSeverity).HasColumnName("min_severity").HasConversion<string>().HasMaxLength(50);
        builder.Property(n => n.Filters).HasColumnName("filters").HasColumnType("jsonb");
        builder.Property(n => n.IsActive).HasColumnName("is_active").HasDefaultValue(true);
        builder.Property(n => n.CreatedByUserId).HasColumnName("created_by_user_id").IsRequired();
        builder.Property(n => n.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
        builder.Property(n => n.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()");

        builder.HasIndex(n => n.TenantId);

        builder.HasOne(n => n.IntegrationConfig)
            .WithMany(i => i.NotificationRules)
            .HasForeignKey(n => n.IntegrationConfigId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
