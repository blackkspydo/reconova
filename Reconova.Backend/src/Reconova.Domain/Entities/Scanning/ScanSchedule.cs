using Reconova.Domain.Common;
using Reconova.Domain.Entities.Workflows;

namespace Reconova.Domain.Entities.Scanning;

public class ScanSchedule : BaseEntity
{
    public Guid TenantId { get; set; }
    public Guid DomainId { get; set; }
    public Guid WorkflowId { get; set; }
    public string CronExpression { get; set; } = string.Empty;
    public bool Enabled { get; set; } = true;
    public int EstimatedCredits { get; set; }
    public DateTime? LastRunAt { get; set; }
    public DateTime? NextRunAt { get; set; }
    public Guid CreatedByUserId { get; set; }

    // Navigation
    public Domain Domain { get; set; } = null!;
    public Workflow Workflow { get; set; } = null!;
}
