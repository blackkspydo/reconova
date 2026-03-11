using Reconova.Domain.Common;
using Reconova.Domain.Entities.Billing;

namespace Reconova.Domain.Entities.FeatureFlags;

public class PlanFeature : BaseEntity
{
    public Guid PlanId { get; set; }
    public Guid FeatureFlagId { get; set; }
    public bool IsEnabled { get; set; } = true;
    public string? Value { get; set; }

    // Navigation
    public SubscriptionPlan Plan { get; set; } = null!;
    public FeatureFlag FeatureFlag { get; set; } = null!;
}
