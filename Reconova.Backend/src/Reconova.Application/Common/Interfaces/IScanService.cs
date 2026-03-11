using Reconova.Application.Common.Models;
using Reconova.Application.DTOs.Scanning;

namespace Reconova.Application.Common.Interfaces;

public interface IScanService
{
    // Domains
    Task<IReadOnlyList<DomainDto>> GetDomainsAsync(CancellationToken cancellationToken = default);
    Task<DomainDto> GetDomainByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<DomainDto> CreateDomainAsync(CreateDomainRequest request, CancellationToken cancellationToken = default);
    Task DeleteDomainAsync(Guid id, CancellationToken cancellationToken = default);
    Task<DomainVerificationDto> InitiateVerificationAsync(Guid domainId, CancellationToken cancellationToken = default);
    Task<DomainDto> VerifyDomainAsync(Guid domainId, CancellationToken cancellationToken = default);

    // Subdomains
    Task<PagedResult<SubdomainDto>> GetSubdomainsAsync(Guid domainId, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<SubdomainDto> GetSubdomainDetailAsync(Guid subdomainId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PortDto>> GetSubdomainPortsAsync(Guid subdomainId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TechnologyDto>> GetSubdomainTechnologiesAsync(Guid subdomainId, CancellationToken cancellationToken = default);

    // Scan Jobs
    Task<ScanJobDto> CreateScanAsync(CreateScanRequest request, CancellationToken cancellationToken = default);
    Task<ScanJobDto> GetScanByIdAsync(Guid scanId, CancellationToken cancellationToken = default);
    Task<PagedResult<ScanJobDto>> GetScansAsync(ScanListRequest request, CancellationToken cancellationToken = default);
    Task CancelScanAsync(Guid scanId, CancellationToken cancellationToken = default);

    // Vulnerabilities
    Task<PagedResult<VulnerabilityDto>> GetVulnerabilitiesAsync(VulnerabilityListRequest request, CancellationToken cancellationToken = default);
    Task<VulnerabilityDto> GetVulnerabilityByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task MarkVulnerabilityResolvedAsync(Guid id, CancellationToken cancellationToken = default);
}
