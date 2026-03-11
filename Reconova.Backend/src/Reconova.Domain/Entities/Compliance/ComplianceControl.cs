using Reconova.Domain.Common;

namespace Reconova.Domain.Entities.Compliance;

public class ComplianceControl : BaseEntity
{
    public Guid FrameworkId { get; set; }
    public string ControlId { get; set; } = string.Empty; // e.g., "1.1.1"
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Category { get; set; }
    public string? AutomationQuery { get; set; } // JSON — maps to scan checks
    public bool IsAutomatable { get; set; }
    public int SortOrder { get; set; }
    public string? MinSecurityRecommendationsJson { get; set; } // JSON array of {recommendation, priority}

    // Navigation
    public ComplianceFramework Framework { get; set; } = null!;
    public ICollection<ControlCheckMapping> CheckMappings { get; set; } = new List<ControlCheckMapping>();
}
