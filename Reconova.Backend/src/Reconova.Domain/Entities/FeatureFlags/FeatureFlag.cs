using Reconova.Domain.Common;
using Reconova.Domain.Common.Enums;

namespace Reconova.Domain.Entities.FeatureFlags;

public class FeatureFlag : BaseEntity
{
    public string Key { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public FeatureFlagType Type { get; set; }
    public bool IsEnabled { get; set; } = true;
    public bool DefaultEnabled { get; set; } = true;
    public string? Module { get; set; } // Category: scanning, compliance, integrations, etc.
    public string? AllowedPlans { get; set; } // JSON array of SubscriptionTier
    public int? RolloutPercentage { get; set; }

    // Navigation
    public ICollection<PlanFeature> PlanFeatures { get; set; } = new List<PlanFeature>();
    public ICollection<TenantFeatureOverride> TenantOverrides { get; set; } = new List<TenantFeatureOverride>();
}
