using Reconova.Domain.Common;
using Reconova.Domain.Common.Enums;

namespace Reconova.Domain.Entities.Admin;

public class SystemConfig : BaseEntity
{
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string DefaultValue { get; set; } = string.Empty;
    public ConfigDataType DataType { get; set; }
    public string? Description { get; set; }
    public string Category { get; set; } = string.Empty;
    public bool IsCritical { get; set; }
    public bool IsSensitive { get; set; }
    public bool RequiresRestart { get; set; }
    public string? MinValue { get; set; }
    public string? MaxValue { get; set; }
    public string? AllowedValues { get; set; } // comma-separated
    public string? Unit { get; set; }
    public Guid? LastModifiedByUserId { get; set; }
}
