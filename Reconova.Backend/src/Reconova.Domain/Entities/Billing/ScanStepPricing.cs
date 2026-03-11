using Reconova.Domain.Common;

namespace Reconova.Domain.Entities.Billing;

public class ScanStepPricing : BaseEntity
{
    public string CheckType { get; set; } = string.Empty; // subdomain_enum, port_scan, etc.
    public Guid TierId { get; set; } // FK to SubscriptionPlan
    public int CreditsPerDomain { get; set; }
    public string? Description { get; set; }

    // Navigation
    public SubscriptionPlan Plan { get; set; } = null!;
}
