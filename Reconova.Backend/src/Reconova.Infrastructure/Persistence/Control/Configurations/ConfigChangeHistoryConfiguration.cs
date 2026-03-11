using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Reconova.Domain.Entities.Admin;

namespace Reconova.Infrastructure.Persistence.Control.Configurations;

public class ConfigChangeHistoryConfiguration : IEntityTypeConfiguration<ConfigChangeHistory>
{
    public void Configure(EntityTypeBuilder<ConfigChangeHistory> builder)
    {
        builder.ToTable("config_change_histories");

        builder.HasKey(h => h.Id);
        builder.Property(h => h.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
        builder.Property(h => h.ConfigKey).HasColumnName("config_key").HasMaxLength(200).IsRequired();
        builder.Property(h => h.OldValue).HasColumnName("old_value").HasMaxLength(2000).IsRequired();
        builder.Property(h => h.NewValue).HasColumnName("new_value").HasMaxLength(2000).IsRequired();
        builder.Property(h => h.Reason).HasColumnName("reason").HasMaxLength(500).IsRequired();
        builder.Property(h => h.ChangedByUserId).HasColumnName("changed_by_user_id").IsRequired();
        builder.Property(h => h.IsRolledBack).HasColumnName("is_rolled_back").HasDefaultValue(false);
        builder.Property(h => h.RolledBackAt).HasColumnName("rolled_back_at");
        builder.Property(h => h.RolledBackByUserId).HasColumnName("rolled_back_by_user_id");
        builder.Property(h => h.RollbackReason).HasColumnName("rollback_reason").HasMaxLength(500);
        builder.Property(h => h.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
        builder.Property(h => h.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()");

        builder.HasIndex(h => h.ConfigKey);
        builder.HasIndex(h => h.CreatedAt);
    }
}
