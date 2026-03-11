using Reconova.Domain.Common;
using Reconova.Domain.Common.Enums;

namespace Reconova.Domain.Entities.Identity;

public class TenantDatabase : BaseEntity
{
    public Guid TenantId { get; set; }
    public string ServerHost { get; set; } = string.Empty;
    public int ServerPort { get; set; } = 5432;
    public string DatabaseName { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string EncryptedPassword { get; set; } = string.Empty;
    public bool IsProvisioned { get; set; }
    public TenantDatabaseStatus Status { get; set; } = TenantDatabaseStatus.Provisioning;
    public DateTime? ProvisionedAt { get; set; }
    public string? MigrationVersion { get; set; }
    public string? TemplateVersion { get; set; }
    public int RetryCount { get; set; }
    public DateTime? LastRetryAt { get; set; }
    public DateTime? BackedUpAt { get; set; }

    // Navigation
    public Tenant Tenant { get; set; } = null!;

    public string GetConnectionString()
    {
        return $"Host={ServerHost};Port={ServerPort};Database={DatabaseName};Username={Username};Password={{decrypted}};";
    }
}
