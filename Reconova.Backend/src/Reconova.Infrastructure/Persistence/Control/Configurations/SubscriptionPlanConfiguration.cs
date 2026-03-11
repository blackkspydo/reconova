using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Reconova.Domain.Entities.Billing;

namespace Reconova.Infrastructure.Persistence.Control.Configurations;

public class SubscriptionPlanConfiguration : IEntityTypeConfiguration<SubscriptionPlan>
{
    public void Configure(EntityTypeBuilder<SubscriptionPlan> builder)
    {
        builder.ToTable("subscription_plans");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
        builder.Property(p => p.Name).HasColumnName("name").HasMaxLength(100).IsRequired();
        builder.Property(p => p.Tier).HasColumnName("tier").HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(p => p.MonthlyPrice).HasColumnName("monthly_price").HasPrecision(10, 2);
        builder.Property(p => p.AnnualPrice).HasColumnName("annual_price").HasPrecision(10, 2);
        builder.Property(p => p.MonthlyCredits).HasColumnName("monthly_credits");
        builder.Property(p => p.MaxUsers).HasColumnName("max_users");
        builder.Property(p => p.MaxDomains).HasColumnName("max_domains");
        builder.Property(p => p.MaxScansPerDay).HasColumnName("max_scans_per_day");
        builder.Property(p => p.IsActive).HasColumnName("is_active").HasDefaultValue(true);
        builder.Property(p => p.StripeMonthlyPriceId).HasColumnName("stripe_monthly_price_id").HasMaxLength(255);
        builder.Property(p => p.StripeAnnualPriceId).HasColumnName("stripe_annual_price_id").HasMaxLength(255);
        builder.Property(p => p.Features).HasColumnName("features").HasColumnType("jsonb");
        builder.Property(p => p.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
        builder.Property(p => p.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()");

        builder.HasIndex(p => p.Tier);
    }
}
