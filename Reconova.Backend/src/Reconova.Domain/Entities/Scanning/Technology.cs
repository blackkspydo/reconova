using Reconova.Domain.Common;

namespace Reconova.Domain.Entities.Scanning;

public class Technology : BaseEntity
{
    public Guid SubdomainId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Version { get; set; }
    public string? Category { get; set; }
    public DateTime? FirstSeenAt { get; set; }
    public DateTime? LastSeenAt { get; set; }
    public Guid? DiscoveredByScanId { get; set; }

    // Navigation
    public Subdomain Subdomain { get; set; } = null!;
}
