using Reconova.Domain.Common;
using Reconova.Domain.Common.Enums;

namespace Reconova.Domain.Entities.Admin;

public class ConfigChangeRequest : BaseEntity
{
    public string ConfigKey { get; set; } = string.Empty;
    public string CurrentValue { get; set; } = string.Empty;
    public string ProposedValue { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public Guid RequestedByUserId { get; set; }
    public ConfigApprovalStatus Status { get; set; } = ConfigApprovalStatus.Pending;
    public Guid? ReviewedByUserId { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? ReviewReason { get; set; }
    public DateTime ExpiresAt { get; set; }
}
