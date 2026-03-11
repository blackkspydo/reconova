using Reconova.Domain.Common;

namespace Reconova.Domain.Entities.Admin;

public class ConfigChangeHistory : BaseEntity
{
    public string ConfigKey { get; set; } = string.Empty;
    public string OldValue { get; set; } = string.Empty;
    public string NewValue { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public Guid ChangedByUserId { get; set; }
    public bool IsRolledBack { get; set; }
    public DateTime? RolledBackAt { get; set; }
    public Guid? RolledBackByUserId { get; set; }
    public string? RollbackReason { get; set; }
}
