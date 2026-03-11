using Reconova.Domain.Common;
using Reconova.Domain.Common.Enums;
using Reconova.Domain.Entities.Identity;

namespace Reconova.Domain.Entities.Billing;

public class TenantSubscription : BaseEntity
{
    public Guid TenantId { get; set; }
    public Guid PlanId { get; set; }
    public SubscriptionStatus Status { get; set; } = SubscriptionStatus.Active;
    public bool IsAnnual { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime CurrentPeriodStart { get; set; }
    public DateTime CurrentPeriodEnd { get; set; }
    public DateTime? CancelledAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public string? StripeSubscriptionId { get; set; }
    public string? StripeCustomerId { get; set; }
    public int CreditsRemaining { get; set; }
    public int CreditsUsedThisPeriod { get; set; }
    public DateTime? CreditsResetAt { get; set; }
    public Guid? PendingPlanId { get; set; }

    // Navigation
    public Tenant Tenant { get; set; } = null!;
    public SubscriptionPlan Plan { get; set; } = null!;
}
