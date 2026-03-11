using Reconova.Domain.Common;
using Reconova.Domain.Common.Enums;

namespace Reconova.Domain.Entities.Integrations;

public class NotificationRule : BaseEntity
{
    public Guid TenantId { get; set; }
    public Guid IntegrationConfigId { get; set; }
    public NotificationEventType EventType { get; set; }
    public SeverityLevel? MinSeverity { get; set; }
    public string? Filters { get; set; } // JSON — additional conditions
    public bool IsActive { get; set; } = true;
    public Guid CreatedByUserId { get; set; }

    // Navigation
    public IntegrationConfig IntegrationConfig { get; set; } = null!;
}
