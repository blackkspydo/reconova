using Reconova.Domain.Common;
using Reconova.Domain.Common.Enums;

namespace Reconova.Domain.Entities.Scanning;

public class Domain : BaseEntity, ISoftDeletable
{
    public Guid TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public DomainVerificationStatus VerificationStatus { get; set; } = DomainVerificationStatus.Pending;
    public string? VerificationToken { get; set; }
    public string? VerificationMethod { get; set; } // DNS_TXT, FILE, META_TAG
    public DateTime? VerifiedAt { get; set; }
    public DateTime? LastScanAt { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }

    // Navigation
    public ICollection<Subdomain> Subdomains { get; set; } = new List<Subdomain>();
    public ICollection<ScanJob> ScanJobs { get; set; } = new List<ScanJob>();
}
