using Reconova.Domain.Common;
using Reconova.Domain.Common.Enums;

namespace Reconova.Domain.Entities.Scanning;

public class ScanJob : BaseEntity
{
    public Guid TenantId { get; set; }
    public Guid DomainId { get; set; }
    public ScanType Type { get; set; }
    public ScanStatus Status { get; set; } = ScanStatus.Queued;
    public int CreditCost { get; set; }
    public Guid InitiatedByUserId { get; set; }
    public Guid? WorkflowId { get; set; }
    public string? Configuration { get; set; } // JSON
    public string? StepsJson { get; set; } // JSON — workflow steps snapshot with pricing, immutable
    public int TotalCredits { get; set; }
    public int? CurrentStep { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? ErrorMessage { get; set; }
    public int? ResultCount { get; set; }
    public int? VulnerabilityCount { get; set; }
    public double? ProgressPercentage { get; set; }
    public Guid? CancelledByUserId { get; set; }
    public string? CancellationReason { get; set; }

    // Navigation
    public Domain Domain { get; set; } = null!;
    public ICollection<ScanResult> Results { get; set; } = new List<ScanResult>();
    public ICollection<Vulnerability> Vulnerabilities { get; set; } = new List<Vulnerability>();
}
