using Reconova.Domain.Common;
using Reconova.Domain.Common.Enums;

namespace Reconova.Domain.Entities.Compliance;

public class ControlResult : BaseEntity
{
    public Guid AssessmentId { get; set; }
    public Guid ControlId { get; set; }
    public ControlResultStatus Status { get; set; } = ControlResultStatus.NotAssessed;
    public string? EvidenceJson { get; set; } // JSON — scan evidence
    public string? RecommendationJson { get; set; } // JSON — remediation recommendations

    // Navigation
    public ComplianceAssessment Assessment { get; set; } = null!;
    public ComplianceControl Control { get; set; } = null!;
}
