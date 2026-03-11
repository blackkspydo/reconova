using Reconova.Domain.Common;
using Reconova.Domain.Common.Enums;

namespace Reconova.Domain.Entities.Scanning;

public class Port : BaseEntity
{
    public Guid SubdomainId { get; set; }
    public int PortNumber { get; set; }
    public string Protocol { get; set; } = "tcp";
    public PortState State { get; set; } = PortState.Open;
    public string? ServiceName { get; set; }
    public string? ServiceVersion { get; set; }
    public string? Banner { get; set; }
    public DateTime? FirstSeenAt { get; set; }
    public DateTime? LastSeenAt { get; set; }
    public Guid? DiscoveredByScanId { get; set; }

    // Navigation
    public Subdomain Subdomain { get; set; } = null!;
}
