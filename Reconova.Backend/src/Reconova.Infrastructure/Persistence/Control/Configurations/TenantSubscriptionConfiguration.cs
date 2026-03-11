using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Reconova.Domain.Entities.Billing;

namespace Reconova.Infrastructure.Persistence.Control.Configurations;

public class TenantSubscriptionConfiguration : IEntityTypeConfiguration<TenantSubscription>
{
    public void Configure(EntityTypeBuilder<TenantSubscription> builder)
    {
        builder.ToTable("tenant_subscriptions");

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
        builder.Property(s => s.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(s => s.PlanId).HasColumnName("plan_id").IsRequired();
        builder.Property(s => s.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(s => s.IsAnnual).HasColumnName("is_annual").HasDefaultValue(false);
        builder.Property(s => s.StartDate).HasColumnName("start_date").IsRequired();
        builder.Property(s => s.CurrentPeriodStart).HasColumnName("current_period_start").IsRequired();
        builder.Property(s => s.CurrentPeriodEnd).HasColumnName("current_period_end").IsRequired();
        builder.Property(s => s.CancelledAt).HasColumnName("cancelled_at");
        builder.Property(s => s.ExpiresAt).HasColumnName("expires_at");
        builder.Property(s => s.StripeSubscriptionId).HasColumnName("stripe_subscription_id").HasMaxLength(255);
        builder.Property(s => s.StripeCustomerId).HasColumnName("stripe_customer_id").HasMaxLength(255);
        builder.Property(s => s.CreditsRemaining).HasColumnName("credits_remaining").HasDefaultValue(0);
        builder.Property(s => s.CreditsUsedThisPeriod).HasColumnName("credits_used_this_period").HasDefaultValue(0);
        builder.Property(s => s.CreditsResetAt).HasColumnName("credits_reset_at");
        builder.Property(s => s.PendingPlanId).HasColumnName("pending_plan_id");
        builder.Property(s => s.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
        builder.Property(s => s.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()");

        builder.HasIndex(s => s.TenantId);
        builder.HasIndex(s => s.StripeSubscriptionId).IsUnique().HasFilter("stripe_subscription_id IS NOT NULL");

        builder.HasOne(s => s.Tenant)
            .WithMany()
            .HasForeignKey(s => s.TenantId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(s => s.Plan)
            .WithMany(p => p.Subscriptions)
            .HasForeignKey(s => s.PlanId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
