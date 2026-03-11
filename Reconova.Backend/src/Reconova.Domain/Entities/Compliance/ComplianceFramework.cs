using Reconova.Domain.Common;
using Reconova.Domain.Common.Enums;

namespace Reconova.Domain.Entities.Compliance;

public class ComplianceFramework : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty; // e.g., "PCI-DSS-4.0"
    public string? Description { get; set; }
    public string Version { get; set; } = "1.0";
    public FrameworkStatus Status { get; set; } = FrameworkStatus.Draft;
    public bool IsSystemFramework { get; set; }
    public Guid? TenantId { get; set; }
    public int ControlCount { get; set; }
    public DateTime? PublishedAt { get; set; }
    public int? GracePeriodDays { get; set; }

    // Navigation
    public ICollection<ComplianceControl> Controls { get; set; } = new List<ComplianceControl>();
    public ICollection<ComplianceAssessment> Assessments { get; set; } = new List<ComplianceAssessment>();
}
