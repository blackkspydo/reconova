using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Reconova.Domain.Entities.Scanning;

namespace Reconova.Infrastructure.Persistence.Tenant.Configurations;

public class ScanResultConfiguration : IEntityTypeConfiguration<ScanResult>
{
    public void Configure(EntityTypeBuilder<ScanResult> builder)
    {
        builder.ToTable("scan_results");

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
        builder.Property(r => r.ScanJobId).HasColumnName("scan_job_id").IsRequired();
        builder.Property(r => r.ResultType).HasColumnName("result_type").HasMaxLength(50).IsRequired();
        builder.Property(r => r.Target).HasColumnName("target").HasMaxLength(500);
        builder.Property(r => r.Data).HasColumnName("data").HasColumnType("jsonb").IsRequired();
        builder.Property(r => r.Source).HasColumnName("source").HasMaxLength(100);
        builder.Property(r => r.Severity).HasColumnName("severity").HasConversion<string>().HasMaxLength(50);
        builder.Property(r => r.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
        builder.Property(r => r.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()");

        builder.HasIndex(r => r.ScanJobId);

        builder.HasOne(r => r.ScanJob)
            .WithMany(s => s.Results)
            .HasForeignKey(r => r.ScanJobId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
