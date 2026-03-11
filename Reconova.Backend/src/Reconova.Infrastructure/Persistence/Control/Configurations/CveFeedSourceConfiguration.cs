using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Reconova.Domain.Entities.Cve;

namespace Reconova.Infrastructure.Persistence.Control.Configurations;

public class CveFeedSourceConfiguration : IEntityTypeConfiguration<CveFeedSource>
{
    public void Configure(EntityTypeBuilder<CveFeedSource> builder)
    {
        builder.ToTable("cve_feed_sources");

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
        builder.Property(s => s.Name).HasColumnName("name").HasMaxLength(100).IsRequired();
        builder.Property(s => s.Url).HasColumnName("url").HasMaxLength(500).IsRequired();
        builder.Property(s => s.Type).HasColumnName("type").HasMaxLength(50).IsRequired();
        builder.Property(s => s.ApiKeyEncrypted).HasColumnName("api_key_encrypted").HasMaxLength(500);
        builder.Property(s => s.SyncIntervalMinutes).HasColumnName("sync_interval_minutes").HasDefaultValue(60);
        builder.Property(s => s.IsActive).HasColumnName("is_active").HasDefaultValue(true);
        builder.Property(s => s.LastSyncAt).HasColumnName("last_sync_at");
        builder.Property(s => s.NextSyncAt).HasColumnName("next_sync_at");
        builder.Property(s => s.LastSyncCount).HasColumnName("last_sync_count");
        builder.Property(s => s.LastSyncError).HasColumnName("last_sync_error").HasMaxLength(1000);
        builder.Property(s => s.ConsecutiveFailures).HasColumnName("consecutive_failures").HasDefaultValue(0);
        builder.Property(s => s.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
        builder.Property(s => s.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()");
    }
}
