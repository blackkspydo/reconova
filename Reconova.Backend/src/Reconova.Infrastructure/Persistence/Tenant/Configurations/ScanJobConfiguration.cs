using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Reconova.Domain.Entities.Scanning;

namespace Reconova.Infrastructure.Persistence.Tenant.Configurations;

public class ScanJobConfiguration : IEntityTypeConfiguration<ScanJob>
{
    public void Configure(EntityTypeBuilder<ScanJob> builder)
    {
        builder.ToTable("scan_jobs");

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
        builder.Property(s => s.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(s => s.DomainId).HasColumnName("domain_id").IsRequired();
        builder.Property(s => s.Type).HasColumnName("type").HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(s => s.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(s => s.CreditCost).HasColumnName("credit_cost").HasDefaultValue(0);
        builder.Property(s => s.InitiatedByUserId).HasColumnName("initiated_by_user_id").IsRequired();
        builder.Property(s => s.WorkflowId).HasColumnName("workflow_id");
        builder.Property(s => s.Configuration).HasColumnName("configuration").HasColumnType("jsonb");
        builder.Property(s => s.StepsJson).HasColumnName("steps_json").HasColumnType("jsonb");
        builder.Property(s => s.TotalCredits).HasColumnName("total_credits").HasDefaultValue(0);
        builder.Property(s => s.CurrentStep).HasColumnName("current_step");
        builder.Property(s => s.StartedAt).HasColumnName("started_at");
        builder.Property(s => s.CompletedAt).HasColumnName("completed_at");
        builder.Property(s => s.ErrorMessage).HasColumnName("error_message").HasMaxLength(2000);
        builder.Property(s => s.ResultCount).HasColumnName("result_count");
        builder.Property(s => s.VulnerabilityCount).HasColumnName("vulnerability_count");
        builder.Property(s => s.ProgressPercentage).HasColumnName("progress_percentage");
        builder.Property(s => s.CancelledByUserId).HasColumnName("cancelled_by_user_id");
        builder.Property(s => s.CancellationReason).HasColumnName("cancellation_reason").HasMaxLength(500);
        builder.Property(s => s.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
        builder.Property(s => s.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()");

        builder.HasIndex(s => s.TenantId);
        builder.HasIndex(s => s.DomainId);
        builder.HasIndex(s => s.Status);
        builder.HasIndex(s => s.CreatedAt);

        builder.HasOne(s => s.Domain)
            .WithMany(d => d.ScanJobs)
            .HasForeignKey(s => s.DomainId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
