using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Reconova.Domain.Entities.Compliance;

namespace Reconova.Infrastructure.Persistence.Tenant.Configurations;

public class ComplianceControlConfiguration : IEntityTypeConfiguration<ComplianceControl>
{
    public void Configure(EntityTypeBuilder<ComplianceControl> builder)
    {
        builder.ToTable("compliance_controls");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
        builder.Property(c => c.FrameworkId).HasColumnName("framework_id").IsRequired();
        builder.Property(c => c.ControlId).HasColumnName("control_id").HasMaxLength(50).IsRequired();
        builder.Property(c => c.Title).HasColumnName("title").HasMaxLength(500).IsRequired();
        builder.Property(c => c.Description).HasColumnName("description").HasMaxLength(2000);
        builder.Property(c => c.Category).HasColumnName("category").HasMaxLength(200);
        builder.Property(c => c.AutomationQuery).HasColumnName("automation_query").HasColumnType("jsonb");
        builder.Property(c => c.IsAutomatable).HasColumnName("is_automatable").HasDefaultValue(false);
        builder.Property(c => c.SortOrder).HasColumnName("sort_order").HasDefaultValue(0);
        builder.Property(c => c.MinSecurityRecommendationsJson).HasColumnName("min_security_recommendations_json").HasColumnType("jsonb");
        builder.Property(c => c.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
        builder.Property(c => c.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()");

        builder.HasIndex(c => new { c.FrameworkId, c.ControlId }).IsUnique();

        builder.HasOne(c => c.Framework)
            .WithMany(f => f.Controls)
            .HasForeignKey(c => c.FrameworkId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
