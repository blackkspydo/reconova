using Reconova.Domain.Common;
using Reconova.Domain.Common.Enums;

namespace Reconova.Domain.Entities.Admin;

public class ApiVersion : BaseEntity
{
    public string Version { get; set; } = string.Empty; // e.g., "v1"
    public ApiVersionStatus Status { get; set; } = ApiVersionStatus.Current;
    public DateTime? DeprecatedAt { get; set; }
    public DateTime? SunsetDate { get; set; }
    public string? ChangeLog { get; set; } // JSON
    public string? MigrationGuide { get; set; }
}
