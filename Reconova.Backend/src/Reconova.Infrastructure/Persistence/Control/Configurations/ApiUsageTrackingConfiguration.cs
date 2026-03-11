using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Reconova.Domain.Entities.Integrations;

namespace Reconova.Infrastructure.Persistence.Control.Configurations;

public class ApiUsageTrackingConfiguration : IEntityTypeConfiguration<ApiUsageTracking>
{
    public void Configure(EntityTypeBuilder<ApiUsageTracking> builder)
    {
        builder.ToTable("api_usage_tracking");

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
        builder.Property(a => a.ApiKeyId).HasColumnName("api_key_id").IsRequired();
        builder.Property(a => a.TenantId).HasColumnName("tenant_id");
        builder.Property(a => a.ScanJobId).HasColumnName("scan_job_id");
        builder.Property(a => a.Provider).HasColumnName("provider").HasMaxLength(100).IsRequired();
        builder.Property(a => a.CallsMade).HasColumnName("calls_made");
        builder.Property(a => a.TrackedAt).HasColumnName("tracked_at").HasDefaultValueSql("now()");
        builder.Property(a => a.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
        builder.Property(a => a.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()");

        builder.HasIndex(a => new { a.ApiKeyId, a.TenantId });
        builder.HasIndex(a => a.TrackedAt);

        builder.HasOne(a => a.ApiKey)
            .WithMany()
            .HasForeignKey(a => a.ApiKeyId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
