using Reconova.Domain.Common;
using Reconova.Domain.Common.Enums;
using Reconova.Domain.Entities.Scanning;

namespace Reconova.Domain.Entities.Workflows;

public class Workflow : BaseEntity
{
    public Guid TenantId { get; set; }
    public Guid TemplateId { get; set; }
    public Guid DomainId { get; set; }
    public WorkflowStatus Status { get; set; } = WorkflowStatus.Active;
    public int CurrentStep { get; set; }
    public int TotalSteps { get; set; }
    public Guid InitiatedByUserId { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? ErrorMessage { get; set; }
    public string? StepResults { get; set; } // JSON

    // Navigation
    public WorkflowTemplate Template { get; set; } = null!;
    public Scanning.Domain? Domain { get; set; }
}
