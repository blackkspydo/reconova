using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Reconova.Domain.Entities.Cve;

namespace Reconova.Infrastructure.Persistence.Control.Configurations;

public class ProductAliasConfiguration : IEntityTypeConfiguration<ProductAlias>
{
    public void Configure(EntityTypeBuilder<ProductAlias> builder)
    {
        builder.ToTable("product_aliases");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
        builder.Property(p => p.CanonicalName).HasColumnName("canonical_name").HasMaxLength(200).IsRequired();
        builder.Property(p => p.Alias).HasColumnName("alias").HasMaxLength(200).IsRequired();
        builder.Property(p => p.CreatedByUserId).HasColumnName("created_by_user_id").IsRequired();
        builder.Property(p => p.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
        builder.Property(p => p.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()");

        builder.HasIndex(p => new { p.CanonicalName, p.Alias }).IsUnique();
    }
}
