using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Reconova.Domain.Entities.Billing;

namespace Reconova.Infrastructure.Persistence.Control.Configurations;

public class CreditPackConfiguration : IEntityTypeConfiguration<CreditPack>
{
    public void Configure(EntityTypeBuilder<CreditPack> builder)
    {
        builder.ToTable("credit_packs");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
        builder.Property(c => c.Name).HasColumnName("name").HasMaxLength(100).IsRequired();
        builder.Property(c => c.Credits).HasColumnName("credits").IsRequired();
        builder.Property(c => c.Price).HasColumnName("price").HasPrecision(10, 2).IsRequired();
        builder.Property(c => c.StripePriceId).HasColumnName("stripe_price_id").HasMaxLength(255);
        builder.Property(c => c.IsActive).HasColumnName("is_active").HasDefaultValue(true);
        builder.Property(c => c.Description).HasColumnName("description").HasMaxLength(500);
        builder.Property(c => c.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
        builder.Property(c => c.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()");
    }
}
