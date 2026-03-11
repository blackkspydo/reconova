using Reconova.Domain.Common;
using Reconova.Domain.Entities.Identity;

namespace Reconova.Domain.Entities.FeatureFlags;

public class TenantFeatureOverride : BaseEntity
{
    public Guid TenantId { get; set; }
    public Guid FeatureFlagId { get; set; }
    public bool IsEnabled { get; set; }
    public string? Value { get; set; }
    public string? Reason { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public Guid SetByUserId { get; set; }

    // Navigation
    public FeatureFlag FeatureFlag { get; set; } = null!;
    public Tenant Tenant { get; set; } = null!;
}
