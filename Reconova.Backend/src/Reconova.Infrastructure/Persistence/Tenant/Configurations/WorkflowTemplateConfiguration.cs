using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Reconova.Domain.Entities.Workflows;

namespace Reconova.Infrastructure.Persistence.Tenant.Configurations;

public class WorkflowTemplateConfiguration : IEntityTypeConfiguration<WorkflowTemplate>
{
    public void Configure(EntityTypeBuilder<WorkflowTemplate> builder)
    {
        builder.ToTable("workflow_templates");

        builder.HasKey(w => w.Id);
        builder.Property(w => w.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
        builder.Property(w => w.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(w => w.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
        builder.Property(w => w.Description).HasColumnName("description").HasMaxLength(1000);
        builder.Property(w => w.Steps).HasColumnName("steps").HasColumnType("jsonb").IsRequired();
        builder.Property(w => w.IsActive).HasColumnName("is_active").HasDefaultValue(true);
        builder.Property(w => w.IsSystemTemplate).HasColumnName("is_system_template").HasDefaultValue(false);
        builder.Property(w => w.Version).HasColumnName("version").HasDefaultValue(1);
        builder.Property(w => w.CreatedByUserId).HasColumnName("created_by_user_id").IsRequired();
        builder.Property(w => w.TotalCreditCost).HasColumnName("total_credit_cost").HasDefaultValue(0);
        builder.Property(w => w.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
        builder.Property(w => w.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()");

        builder.HasIndex(w => w.TenantId);
    }
}
