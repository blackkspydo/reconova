using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Reconova.Domain.Entities.Compliance;

namespace Reconova.Infrastructure.Persistence.Tenant.Configurations;

public class ControlCheckMappingConfiguration : IEntityTypeConfiguration<ControlCheckMapping>
{
    public void Configure(EntityTypeBuilder<ControlCheckMapping> builder)
    {
        builder.ToTable("control_check_mappings");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
        builder.Property(c => c.ControlId).HasColumnName("control_id").IsRequired();
        builder.Property(c => c.CheckType).HasColumnName("check_type").HasMaxLength(50).IsRequired();
        builder.Property(c => c.SeverityThreshold).HasColumnName("severity_threshold").HasMaxLength(20);
        builder.Property(c => c.PassConditionJson).HasColumnName("pass_condition_json").HasColumnType("jsonb");
        builder.Property(c => c.RecommendationJson).HasColumnName("recommendation_json").HasColumnType("jsonb");
        builder.Property(c => c.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
        builder.Property(c => c.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()");

        builder.HasOne(c => c.Control)
            .WithMany(ctrl => ctrl.CheckMappings)
            .HasForeignKey(c => c.ControlId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
