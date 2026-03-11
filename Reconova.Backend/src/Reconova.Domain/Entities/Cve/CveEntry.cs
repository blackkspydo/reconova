using Reconova.Domain.Common;
using Reconova.Domain.Common.Enums;

namespace Reconova.Domain.Entities.Cve;

public class CveEntry : BaseEntity
{
    public string CveId { get; set; } = string.Empty; // e.g., "CVE-2024-12345"
    public string? Description { get; set; }
    public SeverityLevel Severity { get; set; }
    public double? CvssV3Score { get; set; }
    public string? CvssV3Vector { get; set; }
    public string? AffectedProducts { get; set; } // JSON
    public string? References { get; set; } // JSON
    public DateTime? PublishedDate { get; set; }
    public DateTime? LastModifiedDate { get; set; }
    public string? Source { get; set; } // NVD, MITRE
    public string? Cpe { get; set; } // Common Platform Enumeration
    public Guid? FeedSourceId { get; set; }
    public string? RawJson { get; set; }
}
