using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Reconova.Domain.Entities.Workflows;

namespace Reconova.Infrastructure.Persistence.Tenant.Configurations;

public class WorkflowConfiguration : IEntityTypeConfiguration<Workflow>
{
    public void Configure(EntityTypeBuilder<Workflow> builder)
    {
        builder.ToTable("workflows");

        builder.HasKey(w => w.Id);
        builder.Property(w => w.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
        builder.Property(w => w.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(w => w.TemplateId).HasColumnName("template_id").IsRequired();
        builder.Property(w => w.DomainId).HasColumnName("domain_id").IsRequired();
        builder.Property(w => w.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(w => w.CurrentStep).HasColumnName("current_step").HasDefaultValue(0);
        builder.Property(w => w.TotalSteps).HasColumnName("total_steps").IsRequired();
        builder.Property(w => w.InitiatedByUserId).HasColumnName("initiated_by_user_id").IsRequired();
        builder.Property(w => w.StartedAt).HasColumnName("started_at");
        builder.Property(w => w.CompletedAt).HasColumnName("completed_at");
        builder.Property(w => w.ErrorMessage).HasColumnName("error_message").HasMaxLength(2000);
        builder.Property(w => w.StepResults).HasColumnName("step_results").HasColumnType("jsonb");
        builder.Property(w => w.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
        builder.Property(w => w.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()");

        builder.HasIndex(w => w.TenantId);
        builder.HasIndex(w => w.Status);

        builder.HasOne(w => w.Template)
            .WithMany(t => t.Workflows)
            .HasForeignKey(w => w.TemplateId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(w => w.Domain)
            .WithMany()
            .HasForeignKey(w => w.DomainId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
