using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Reconova.Domain.Common.Enums;
using Reconova.Domain.Entities.Scanning;

namespace Reconova.Infrastructure.Persistence.Tenant.Configurations;

public class PortConfiguration : IEntityTypeConfiguration<Port>
{
    public void Configure(EntityTypeBuilder<Port> builder)
    {
        builder.ToTable("ports");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
        builder.Property(p => p.SubdomainId).HasColumnName("subdomain_id").IsRequired();
        builder.Property(p => p.PortNumber).HasColumnName("port_number").IsRequired();
        builder.Property(p => p.Protocol).HasColumnName("protocol").HasMaxLength(10).HasDefaultValue("tcp");
        builder.Property(p => p.State).HasColumnName("state").HasMaxLength(20).HasConversion<string>().HasDefaultValue(PortState.Open);
        builder.Property(p => p.ServiceName).HasColumnName("service_name").HasMaxLength(200);
        builder.Property(p => p.ServiceVersion).HasColumnName("service_version").HasMaxLength(200);
        builder.Property(p => p.Banner).HasColumnName("banner").HasMaxLength(2000);
        builder.Property(p => p.FirstSeenAt).HasColumnName("first_seen_at");
        builder.Property(p => p.LastSeenAt).HasColumnName("last_seen_at");
        builder.Property(p => p.DiscoveredByScanId).HasColumnName("discovered_by_scan_id");
        builder.Property(p => p.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
        builder.Property(p => p.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()");

        builder.HasIndex(p => new { p.SubdomainId, p.PortNumber, p.Protocol }).IsUnique();

        builder.HasOne(p => p.Subdomain)
            .WithMany(s => s.Ports)
            .HasForeignKey(p => p.SubdomainId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
