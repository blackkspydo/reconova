using Reconova.Domain.Common;

namespace Reconova.Domain.Entities.Workflows;

public class WorkflowTemplate : BaseEntity
{
    public Guid TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Steps { get; set; } = "[]"; // JSON array of scan steps
    public bool IsActive { get; set; } = true;
    public bool IsSystemTemplate { get; set; }
    public int Version { get; set; } = 1;
    public Guid CreatedByUserId { get; set; }
    public int TotalCreditCost { get; set; }

    // Navigation
    public ICollection<Workflow> Workflows { get; set; } = new List<Workflow>();
}
