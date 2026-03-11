using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Reconova.Domain.Entities.Compliance;

namespace Reconova.Infrastructure.Persistence.Tenant.Configurations;

public class ComplianceAssessmentConfiguration : IEntityTypeConfiguration<ComplianceAssessment>
{
    public void Configure(EntityTypeBuilder<ComplianceAssessment> builder)
    {
        builder.ToTable("compliance_assessments");

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
        builder.Property(a => a.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(a => a.FrameworkId).HasColumnName("framework_id").IsRequired();
        builder.Property(a => a.DomainId).HasColumnName("domain_id").IsRequired();
        builder.Property(a => a.OverallStatus).HasColumnName("overall_status").HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(a => a.ComplianceScore).HasColumnName("compliance_score").HasDefaultValue(0);
        builder.Property(a => a.TotalControls).HasColumnName("total_controls").HasDefaultValue(0);
        builder.Property(a => a.PassedControls).HasColumnName("passed_controls").HasDefaultValue(0);
        builder.Property(a => a.FailedControls).HasColumnName("failed_controls").HasDefaultValue(0);
        builder.Property(a => a.NotAssessedControls).HasColumnName("not_assessed_controls").HasDefaultValue(0);
        builder.Property(a => a.AssessedAt).HasColumnName("assessed_at");
        builder.Property(a => a.AssessedByUserId).HasColumnName("assessed_by_user_id");
        builder.Property(a => a.ScanJobId).HasColumnName("scan_job_id");
        builder.Property(a => a.ControlResults).HasColumnName("control_results").HasColumnType("jsonb");
        builder.Property(a => a.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
        builder.Property(a => a.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()");

        builder.HasIndex(a => new { a.TenantId, a.FrameworkId, a.DomainId });

        builder.HasOne(a => a.Framework)
            .WithMany(f => f.Assessments)
            .HasForeignKey(a => a.FrameworkId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(a => a.Domain)
            .WithMany()
            .HasForeignKey(a => a.DomainId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
