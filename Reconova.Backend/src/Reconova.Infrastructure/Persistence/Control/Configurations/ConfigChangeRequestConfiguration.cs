using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Reconova.Domain.Entities.Admin;

namespace Reconova.Infrastructure.Persistence.Control.Configurations;

public class ConfigChangeRequestConfiguration : IEntityTypeConfiguration<ConfigChangeRequest>
{
    public void Configure(EntityTypeBuilder<ConfigChangeRequest> builder)
    {
        builder.ToTable("config_change_requests");

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
        builder.Property(r => r.ConfigKey).HasColumnName("config_key").HasMaxLength(200).IsRequired();
        builder.Property(r => r.CurrentValue).HasColumnName("current_value").HasMaxLength(2000).IsRequired();
        builder.Property(r => r.ProposedValue).HasColumnName("proposed_value").HasMaxLength(2000).IsRequired();
        builder.Property(r => r.Reason).HasColumnName("reason").HasMaxLength(500).IsRequired();
        builder.Property(r => r.RequestedByUserId).HasColumnName("requested_by_user_id").IsRequired();
        builder.Property(r => r.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(r => r.ReviewedByUserId).HasColumnName("reviewed_by_user_id");
        builder.Property(r => r.ReviewedAt).HasColumnName("reviewed_at");
        builder.Property(r => r.ReviewReason).HasColumnName("review_reason").HasMaxLength(500);
        builder.Property(r => r.ExpiresAt).HasColumnName("expires_at").IsRequired();
        builder.Property(r => r.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
        builder.Property(r => r.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()");

        builder.HasIndex(r => r.ConfigKey);
        builder.HasIndex(r => r.Status);
        builder.HasIndex(r => new { r.ConfigKey, r.Status });
    }
}
