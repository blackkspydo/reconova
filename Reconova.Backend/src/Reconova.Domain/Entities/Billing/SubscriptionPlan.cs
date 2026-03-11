using Reconova.Domain.Common;
using Reconova.Domain.Common.Enums;
using Reconova.Domain.Entities.FeatureFlags;

namespace Reconova.Domain.Entities.Billing;

public class SubscriptionPlan : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public SubscriptionTier Tier { get; set; }
    public decimal MonthlyPrice { get; set; }
    public decimal AnnualPrice { get; set; }
    public int MonthlyCredits { get; set; }
    public int MaxUsers { get; set; }
    public int MaxDomains { get; set; }
    public int MaxScansPerDay { get; set; }
    public bool IsActive { get; set; } = true;
    public string? StripeMonthlyPriceId { get; set; }
    public string? StripeAnnualPriceId { get; set; }
    public string? Features { get; set; } // JSON

    // Navigation
    public ICollection<TenantSubscription> Subscriptions { get; set; } = new List<TenantSubscription>();
    public ICollection<PlanFeature> PlanFeatures { get; set; } = new List<PlanFeature>();
}
