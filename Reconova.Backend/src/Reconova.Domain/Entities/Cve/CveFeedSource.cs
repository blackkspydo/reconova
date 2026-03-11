using Reconova.Domain.Common;

namespace Reconova.Domain.Entities.Cve;

public class CveFeedSource : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty; // NVD_JSON, NVD_API
    public string? ApiKeyEncrypted { get; set; }
    public int SyncIntervalMinutes { get; set; } = 60;
    public bool IsActive { get; set; } = true;
    public DateTime? LastSyncAt { get; set; }
    public DateTime? NextSyncAt { get; set; }
    public int? LastSyncCount { get; set; }
    public string? LastSyncError { get; set; }
    public int ConsecutiveFailures { get; set; }
}
