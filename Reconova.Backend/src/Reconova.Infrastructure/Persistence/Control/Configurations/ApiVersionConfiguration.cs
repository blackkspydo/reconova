using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Reconova.Domain.Entities.Admin;

namespace Reconova.Infrastructure.Persistence.Control.Configurations;

public class ApiVersionConfiguration : IEntityTypeConfiguration<ApiVersion>
{
    public void Configure(EntityTypeBuilder<ApiVersion> builder)
    {
        builder.ToTable("api_versions");

        builder.HasKey(v => v.Id);
        builder.Property(v => v.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
        builder.Property(v => v.Version).HasColumnName("version").HasMaxLength(20).IsRequired();
        builder.Property(v => v.Status).HasColumnName("status").HasMaxLength(20).HasConversion<string>().IsRequired();
        builder.Property(v => v.DeprecatedAt).HasColumnName("deprecated_at");
        builder.Property(v => v.SunsetDate).HasColumnName("sunset_date");
        builder.Property(v => v.ChangeLog).HasColumnName("change_log").HasColumnType("jsonb");
        builder.Property(v => v.MigrationGuide).HasColumnName("migration_guide").HasMaxLength(5000);
        builder.Property(v => v.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
        builder.Property(v => v.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()");

        builder.HasIndex(v => v.Version).IsUnique();
    }
}
