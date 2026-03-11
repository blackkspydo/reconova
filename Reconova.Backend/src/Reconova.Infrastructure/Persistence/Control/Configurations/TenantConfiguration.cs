using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Reconova.Domain.Entities.Identity;

using TenantEntity = Reconova.Domain.Entities.Identity.Tenant;

namespace Reconova.Infrastructure.Persistence.Control.Configurations;

public class TenantConfiguration : IEntityTypeConfiguration<TenantEntity>
{
    public void Configure(EntityTypeBuilder<TenantEntity> builder)
    {
        builder.ToTable("tenants");

        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
        builder.Property(t => t.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
        builder.Property(t => t.Slug).HasColumnName("slug").HasMaxLength(200).IsRequired();
        builder.Property(t => t.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(t => t.OwnerId).HasColumnName("owner_id").IsRequired();
        builder.Property(t => t.CompanyName).HasColumnName("company_name").HasMaxLength(200);
        builder.Property(t => t.Industry).HasColumnName("industry").HasMaxLength(100);
        builder.Property(t => t.MaxUsers).HasColumnName("max_users").HasDefaultValue(5);
        builder.Property(t => t.MaxDomains).HasColumnName("max_domains").HasDefaultValue(10);
        builder.Property(t => t.SuspendedAt).HasColumnName("suspended_at");
        builder.Property(t => t.SuspensionReason).HasColumnName("suspension_reason").HasMaxLength(500);
        builder.Property(t => t.SuspendedByUserId).HasColumnName("suspended_by_user_id");
        builder.Property(t => t.GracePeriodEndsAt).HasColumnName("grace_period_ends_at");
        builder.Property(t => t.IsDeleted).HasColumnName("is_deleted").HasDefaultValue(false);
        builder.Property(t => t.DeletedAt).HasColumnName("deleted_at");
        builder.Property(t => t.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
        builder.Property(t => t.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()");

        builder.HasIndex(t => t.Slug).IsUnique().HasFilter("is_deleted = false");
        builder.HasIndex(t => t.OwnerId);
        builder.HasIndex(t => t.Status);

        builder.HasQueryFilter(t => !t.IsDeleted);

        builder.HasOne(t => t.Owner)
            .WithMany()
            .HasForeignKey(t => t.OwnerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.Database)
            .WithOne(d => d.Tenant)
            .HasForeignKey<TenantDatabase>(d => d.TenantId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
