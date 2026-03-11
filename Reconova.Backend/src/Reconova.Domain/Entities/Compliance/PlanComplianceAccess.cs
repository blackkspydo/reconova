using Reconova.Domain.Common;
using Reconova.Domain.Entities.Billing;

namespace Reconova.Domain.Entities.Compliance;

public class PlanComplianceAccess : BaseEntity
{
    public Guid PlanId { get; set; }
    public Guid FrameworkId { get; set; }
    public bool Enabled { get; set; } = true;

    // Navigation
    public SubscriptionPlan Plan { get; set; } = null!;
    public ComplianceFramework Framework { get; set; } = null!;
}
