using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Reconova.Domain.Entities.Admin;

namespace Reconova.Infrastructure.Persistence.Control.Configurations;

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("audit_logs");

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
        builder.Property(a => a.TenantId).HasColumnName("tenant_id");
        builder.Property(a => a.UserId).HasColumnName("user_id");
        builder.Property(a => a.UserEmail).HasColumnName("user_email").HasMaxLength(255);
        builder.Property(a => a.Action).HasColumnName("action").HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(a => a.EntityType).HasColumnName("entity_type").HasMaxLength(100).IsRequired();
        builder.Property(a => a.EntityId).HasColumnName("entity_id").HasMaxLength(100);
        builder.Property(a => a.OldValues).HasColumnName("old_values").HasColumnType("jsonb");
        builder.Property(a => a.NewValues).HasColumnName("new_values").HasColumnType("jsonb");
        builder.Property(a => a.IpAddress).HasColumnName("ip_address").HasMaxLength(45);
        builder.Property(a => a.UserAgent).HasColumnName("user_agent").HasMaxLength(500);
        builder.Property(a => a.Description).HasColumnName("description").HasMaxLength(1000);
        builder.Property(a => a.IsAdminAction).HasColumnName("is_admin_action").HasDefaultValue(false);
        builder.Property(a => a.IsImpersonation).HasColumnName("is_impersonation").HasDefaultValue(false);
        builder.Property(a => a.ImpersonatedByUserId).HasColumnName("impersonated_by_user_id");
        builder.Property(a => a.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
        builder.Property(a => a.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()");

        builder.HasIndex(a => a.TenantId);
        builder.HasIndex(a => a.UserId);
        builder.HasIndex(a => a.Action);
        builder.HasIndex(a => a.CreatedAt);
        builder.HasIndex(a => a.EntityType);
    }
}
