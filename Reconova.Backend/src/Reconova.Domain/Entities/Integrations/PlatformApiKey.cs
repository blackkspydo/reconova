using Reconova.Domain.Common;
using Reconova.Domain.Common.Enums;

namespace Reconova.Domain.Entities.Integrations;

public class PlatformApiKey : BaseEntity
{
    public string Provider { get; set; } = string.Empty; // Shodan, SecurityTrails, Censys, etc.
    public string ApiKeyEncrypted { get; set; } = string.Empty; // AES-256 encrypted
    public int RateLimit { get; set; }
    public int MonthlyQuota { get; set; }
    public PlatformApiKeyStatus Status { get; set; } = PlatformApiKeyStatus.Active;
    public int UsageCount { get; set; } // Resets monthly
    public DateTime? UsageResetAt { get; set; }
    public Guid AddedByUserId { get; set; }
}
