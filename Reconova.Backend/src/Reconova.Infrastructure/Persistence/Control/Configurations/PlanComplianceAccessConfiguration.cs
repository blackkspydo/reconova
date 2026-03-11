using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Reconova.Domain.Entities.Compliance;

namespace Reconova.Infrastructure.Persistence.Control.Configurations;

public class PlanComplianceAccessConfiguration : IEntityTypeConfiguration<PlanComplianceAccess>
{
    public void Configure(EntityTypeBuilder<PlanComplianceAccess> builder)
    {
        builder.ToTable("plan_compliance_access");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
        builder.Property(p => p.PlanId).HasColumnName("plan_id").IsRequired();
        builder.Property(p => p.FrameworkId).HasColumnName("framework_id").IsRequired();
        builder.Property(p => p.Enabled).HasColumnName("enabled").HasDefaultValue(true);
        builder.Property(p => p.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
        builder.Property(p => p.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()");

        builder.HasIndex(p => new { p.PlanId, p.FrameworkId }).IsUnique();

        builder.HasOne(p => p.Plan)
            .WithMany()
            .HasForeignKey(p => p.PlanId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(p => p.Framework)
            .WithMany()
            .HasForeignKey(p => p.FrameworkId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
