using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Reconova.Domain.Entities.Scanning;

namespace Reconova.Infrastructure.Persistence.Tenant.Configurations;

public class ScreenshotConfiguration : IEntityTypeConfiguration<Screenshot>
{
    public void Configure(EntityTypeBuilder<Screenshot> builder)
    {
        builder.ToTable("screenshots");

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
        builder.Property(s => s.SubdomainId).HasColumnName("subdomain_id").IsRequired();
        builder.Property(s => s.Url).HasColumnName("url").HasMaxLength(2048).IsRequired();
        builder.Property(s => s.StoragePath).HasColumnName("storage_path").HasMaxLength(500).IsRequired();
        builder.Property(s => s.TakenAt).HasColumnName("taken_at").IsRequired();
        builder.Property(s => s.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
        builder.Property(s => s.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()");

        builder.HasIndex(s => s.SubdomainId);

        builder.HasOne(s => s.Subdomain)
            .WithMany(sd => sd.Screenshots)
            .HasForeignKey(s => s.SubdomainId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
