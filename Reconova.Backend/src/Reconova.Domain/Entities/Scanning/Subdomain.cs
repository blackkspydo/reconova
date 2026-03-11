using Reconova.Domain.Common;

namespace Reconova.Domain.Entities.Scanning;

public class Subdomain : BaseEntity
{
    public Guid DomainId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? IpAddress { get; set; }
    public bool IsAlive { get; set; }
    public int? HttpStatusCode { get; set; }
    public string? WebServer { get; set; }
    public DateTime? FirstSeenAt { get; set; }
    public DateTime? LastSeenAt { get; set; }
    public string? Source { get; set; } // Discovery source: subfinder, amass, etc.
    public Guid? DiscoveredByScanId { get; set; }

    // Navigation
    public Domain Domain { get; set; } = null!;
    public ICollection<Port> Ports { get; set; } = new List<Port>();
    public ICollection<Technology> Technologies { get; set; } = new List<Technology>();
    public ICollection<Screenshot> Screenshots { get; set; } = new List<Screenshot>();
}
