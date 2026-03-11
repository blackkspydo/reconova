using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Reconova.Domain.Entities.Billing;

namespace Reconova.Infrastructure.Persistence.Control.Configurations;

public class ScanStepPricingConfiguration : IEntityTypeConfiguration<ScanStepPricing>
{
    public void Configure(EntityTypeBuilder<ScanStepPricing> builder)
    {
        builder.ToTable("scan_step_pricing");

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
        builder.Property(s => s.CheckType).HasColumnName("check_type").HasMaxLength(50).IsRequired();
        builder.Property(s => s.TierId).HasColumnName("tier_id").IsRequired();
        builder.Property(s => s.CreditsPerDomain).HasColumnName("credits_per_domain").IsRequired();
        builder.Property(s => s.Description).HasColumnName("description").HasMaxLength(200);
        builder.Property(s => s.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
        builder.Property(s => s.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()");

        builder.HasIndex(s => new { s.CheckType, s.TierId }).IsUnique();

        builder.HasOne(s => s.Plan)
            .WithMany()
            .HasForeignKey(s => s.TierId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
