using Reconova.Domain.Common;
using Reconova.Domain.Common.Enums;

namespace Reconova.Domain.Entities.Integrations;

public class IntegrationConfig : BaseEntity
{
    public Guid TenantId { get; set; }
    public IntegrationType Type { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? WebhookUrl { get; set; }
    public string? EncryptedApiKey { get; set; }
    public string? Configuration { get; set; } // JSON — type-specific config
    public bool IsActive { get; set; } = true;
    public DateTime? LastUsedAt { get; set; }
    public int FailureCount { get; set; }
    public DateTime? LastFailureAt { get; set; }
    public Guid CreatedByUserId { get; set; }

    // Navigation
    public ICollection<NotificationRule> NotificationRules { get; set; } = new List<NotificationRule>();
}
