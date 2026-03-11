using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Reconova.Domain.Entities.Scanning;

namespace Reconova.Infrastructure.Persistence.Tenant.Configurations;

public class TechnologyConfiguration : IEntityTypeConfiguration<Technology>
{
    public void Configure(EntityTypeBuilder<Technology> builder)
    {
        builder.ToTable("technologies");

        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
        builder.Property(t => t.SubdomainId).HasColumnName("subdomain_id").IsRequired();
        builder.Property(t => t.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
        builder.Property(t => t.Version).HasColumnName("version").HasMaxLength(100);
        builder.Property(t => t.Category).HasColumnName("category").HasMaxLength(100);
        builder.Property(t => t.FirstSeenAt).HasColumnName("first_seen_at");
        builder.Property(t => t.LastSeenAt).HasColumnName("last_seen_at");
        builder.Property(t => t.DiscoveredByScanId).HasColumnName("discovered_by_scan_id");
        builder.Property(t => t.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
        builder.Property(t => t.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()");

        builder.HasIndex(t => new { t.SubdomainId, t.Name }).IsUnique();

        builder.HasOne(t => t.Subdomain)
            .WithMany(s => s.Technologies)
            .HasForeignKey(t => t.SubdomainId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
