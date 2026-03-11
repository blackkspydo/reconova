using Reconova.Domain.Common;

namespace Reconova.Domain.Entities.Scanning;

public class Screenshot : BaseEntity
{
    public Guid SubdomainId { get; set; }
    public string Url { get; set; } = string.Empty;
    public string StoragePath { get; set; } = string.Empty;
    public DateTime TakenAt { get; set; }

    // Navigation
    public Subdomain Subdomain { get; set; } = null!;
}
