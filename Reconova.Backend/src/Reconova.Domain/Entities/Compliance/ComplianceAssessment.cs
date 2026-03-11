using Reconova.Domain.Common;
using Reconova.Domain.Common.Enums;
using Reconova.Domain.Entities.Scanning;

namespace Reconova.Domain.Entities.Compliance;

public class ComplianceAssessment : BaseEntity
{
    public Guid TenantId { get; set; }
    public Guid FrameworkId { get; set; }
    public Guid DomainId { get; set; }
    public ComplianceStatus OverallStatus { get; set; } = ComplianceStatus.NotAssessed;
    public double ComplianceScore { get; set; }
    public int TotalControls { get; set; }
    public int PassedControls { get; set; }
    public int FailedControls { get; set; }
    public int NotAssessedControls { get; set; }
    public DateTime? AssessedAt { get; set; }
    public Guid? AssessedByUserId { get; set; }
    public Guid? ScanJobId { get; set; }
    public string? ControlResults { get; set; } // JSON (legacy, prefer ControlResult entities)

    // Navigation
    public ComplianceFramework Framework { get; set; } = null!;
    public Scanning.Domain? Domain { get; set; }
    public ICollection<ControlResult> Results { get; set; } = new List<ControlResult>();
}
