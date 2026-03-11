using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Reconova.Domain.Common.Enums;
using Reconova.Domain.Entities.Compliance;

namespace Reconova.Infrastructure.Persistence.Tenant.Configurations;

public class ControlResultConfiguration : IEntityTypeConfiguration<ControlResult>
{
    public void Configure(EntityTypeBuilder<ControlResult> builder)
    {
        builder.ToTable("control_results");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
        builder.Property(c => c.AssessmentId).HasColumnName("assessment_id").IsRequired();
        builder.Property(c => c.ControlId).HasColumnName("control_id").IsRequired();
        builder.Property(c => c.Status).HasColumnName("status").HasMaxLength(20).HasConversion<string>().HasDefaultValue(ControlResultStatus.NotAssessed);
        builder.Property(c => c.EvidenceJson).HasColumnName("evidence_json").HasColumnType("jsonb");
        builder.Property(c => c.RecommendationJson).HasColumnName("recommendation_json").HasColumnType("jsonb");
        builder.Property(c => c.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
        builder.Property(c => c.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()");

        builder.HasIndex(c => new { c.AssessmentId, c.ControlId }).IsUnique();

        builder.HasOne(c => c.Assessment)
            .WithMany(a => a.Results)
            .HasForeignKey(c => c.AssessmentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(r => r.Control)
            .WithMany()
            .HasForeignKey(r => r.ControlId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
