using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Reconova.Domain.Entities.Scanning;

namespace Reconova.Infrastructure.Persistence.Tenant.Configurations;

public class ScanScheduleConfiguration : IEntityTypeConfiguration<ScanSchedule>
{
    public void Configure(EntityTypeBuilder<ScanSchedule> builder)
    {
        builder.ToTable("scan_schedules");

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
        builder.Property(s => s.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(s => s.DomainId).HasColumnName("domain_id").IsRequired();
        builder.Property(s => s.WorkflowId).HasColumnName("workflow_id").IsRequired();
        builder.Property(s => s.CronExpression).HasColumnName("cron_expression").HasMaxLength(100).IsRequired();
        builder.Property(s => s.Enabled).HasColumnName("enabled").HasDefaultValue(true);
        builder.Property(s => s.EstimatedCredits).HasColumnName("estimated_credits");
        builder.Property(s => s.LastRunAt).HasColumnName("last_run_at");
        builder.Property(s => s.NextRunAt).HasColumnName("next_run_at");
        builder.Property(s => s.CreatedByUserId).HasColumnName("created_by_user_id").IsRequired();
        builder.Property(s => s.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
        builder.Property(s => s.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()");

        builder.HasIndex(s => new { s.DomainId, s.WorkflowId });

        builder.HasOne(s => s.Domain)
            .WithMany()
            .HasForeignKey(s => s.DomainId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(s => s.Workflow)
            .WithMany()
            .HasForeignKey(s => s.WorkflowId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
