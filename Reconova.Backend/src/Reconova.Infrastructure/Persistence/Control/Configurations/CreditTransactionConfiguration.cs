using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Reconova.Domain.Entities.Billing;

namespace Reconova.Infrastructure.Persistence.Control.Configurations;

public class CreditTransactionConfiguration : IEntityTypeConfiguration<CreditTransaction>
{
    public void Configure(EntityTypeBuilder<CreditTransaction> builder)
    {
        builder.ToTable("credit_transactions");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
        builder.Property(c => c.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(c => c.Type).HasColumnName("type").HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(c => c.Amount).HasColumnName("amount").IsRequired();
        builder.Property(c => c.BalanceBefore).HasColumnName("balance_before");
        builder.Property(c => c.BalanceAfter).HasColumnName("balance_after");
        builder.Property(c => c.Description).HasColumnName("description").HasMaxLength(500);
        builder.Property(c => c.ReferenceType).HasColumnName("reference_type").HasMaxLength(100);
        builder.Property(c => c.ReferenceId).HasColumnName("reference_id");
        builder.Property(c => c.PerformedByUserId).HasColumnName("performed_by_user_id");
        builder.Property(c => c.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
        builder.Property(c => c.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()");

        builder.HasIndex(c => c.TenantId);
        builder.HasIndex(c => c.CreatedAt);
        builder.HasIndex(c => new { c.TenantId, c.Type });
    }
}
