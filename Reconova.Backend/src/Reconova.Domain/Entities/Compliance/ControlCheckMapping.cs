using Reconova.Domain.Common;

namespace Reconova.Domain.Entities.Compliance;

public class ControlCheckMapping : BaseEntity
{
    public Guid ControlId { get; set; }
    public string CheckType { get; set; } = string.Empty; // subdomain_enum, port_scan, vuln_scan, etc.
    public string? SeverityThreshold { get; set; } // CRITICAL, HIGH, MEDIUM, LOW or null
    public string? PassConditionJson { get; set; } // JSON — conditions for control to pass
    public string? RecommendationJson { get; set; } // JSON — remediation recommendations

    // Navigation
    public ComplianceControl Control { get; set; } = null!;
}
