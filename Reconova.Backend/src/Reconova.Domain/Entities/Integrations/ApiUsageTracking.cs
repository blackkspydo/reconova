using Reconova.Domain.Common;

namespace Reconova.Domain.Entities.Integrations;

public class ApiUsageTracking : BaseEntity
{
    public Guid ApiKeyId { get; set; }
    public Guid? TenantId { get; set; }
    public Guid? ScanJobId { get; set; }
    public string Provider { get; set; } = string.Empty;
    public int CallsMade { get; set; }
    public DateTime TrackedAt { get; set; }

    // Navigation
    public PlatformApiKey ApiKey { get; set; } = null!;
}
