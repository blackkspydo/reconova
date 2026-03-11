using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Reconova.Domain.Entities.Cve;

namespace Reconova.Infrastructure.Persistence.Control.Configurations;

public class CveEntryConfiguration : IEntityTypeConfiguration<CveEntry>
{
    public void Configure(EntityTypeBuilder<CveEntry> builder)
    {
        builder.ToTable("cve_entries");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
        builder.Property(c => c.CveId).HasColumnName("cve_id").HasMaxLength(20).IsRequired();
        builder.Property(c => c.Description).HasColumnName("description").HasMaxLength(5000);
        builder.Property(c => c.Severity).HasColumnName("severity").HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(c => c.CvssV3Score).HasColumnName("cvss_v3_score");
        builder.Property(c => c.CvssV3Vector).HasColumnName("cvss_v3_vector").HasMaxLength(200);
        builder.Property(c => c.AffectedProducts).HasColumnName("affected_products").HasColumnType("jsonb");
        builder.Property(c => c.References).HasColumnName("references").HasColumnType("jsonb");
        builder.Property(c => c.PublishedDate).HasColumnName("published_date");
        builder.Property(c => c.LastModifiedDate).HasColumnName("last_modified_date");
        builder.Property(c => c.Source).HasColumnName("source").HasMaxLength(50);
        builder.Property(c => c.Cpe).HasColumnName("cpe").HasMaxLength(500);
        builder.Property(c => c.FeedSourceId).HasColumnName("feed_source_id");
        builder.Property(c => c.RawJson).HasColumnName("raw_json").HasColumnType("jsonb");
        builder.Property(c => c.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
        builder.Property(c => c.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()");

        builder.HasIndex(c => c.CveId).IsUnique();
        builder.HasIndex(c => c.Severity);
        builder.HasIndex(c => c.PublishedDate);
    }
}
