using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Reconova.Domain.Entities.Scanning;

namespace Reconova.Infrastructure.Persistence.Tenant.Configurations;

public class SubdomainConfiguration : IEntityTypeConfiguration<Subdomain>
{
    public void Configure(EntityTypeBuilder<Subdomain> builder)
    {
        builder.ToTable("subdomains");

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
        builder.Property(s => s.DomainId).HasColumnName("domain_id").IsRequired();
        builder.Property(s => s.Name).HasColumnName("name").HasMaxLength(500).IsRequired();
        builder.Property(s => s.IpAddress).HasColumnName("ip_address").HasMaxLength(45);
        builder.Property(s => s.IsAlive).HasColumnName("is_alive").HasDefaultValue(false);
        builder.Property(s => s.HttpStatusCode).HasColumnName("http_status_code");
        builder.Property(s => s.WebServer).HasColumnName("web_server").HasMaxLength(200);
        builder.Property(s => s.FirstSeenAt).HasColumnName("first_seen_at");
        builder.Property(s => s.LastSeenAt).HasColumnName("last_seen_at");
        builder.Property(s => s.Source).HasColumnName("source").HasMaxLength(100);
        builder.Property(s => s.DiscoveredByScanId).HasColumnName("discovered_by_scan_id");
        builder.Property(s => s.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
        builder.Property(s => s.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()");

        builder.HasIndex(s => new { s.DomainId, s.Name }).IsUnique();
        builder.HasIndex(s => s.DomainId);

        builder.HasOne(s => s.Domain)
            .WithMany(d => d.Subdomains)
            .HasForeignKey(s => s.DomainId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
