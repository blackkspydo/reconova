using Reconova.Domain.Common;
using Reconova.Domain.Common.Enums;

namespace Reconova.Domain.Entities.Scanning;

public class ScanResult : BaseEntity
{
    public Guid ScanJobId { get; set; }
    public string ResultType { get; set; } = string.Empty; // subdomain_enum, port_scan, tech_detect, vuln_scan, etc.
    public string? Target { get; set; } // Domain/subdomain targeted
    public string Data { get; set; } = string.Empty; // JSON
    public string? Source { get; set; } // Tool name: subfinder, nmap, nuclei, etc.
    public SeverityLevel? Severity { get; set; }

    // Navigation
    public ScanJob ScanJob { get; set; } = null!;
}
