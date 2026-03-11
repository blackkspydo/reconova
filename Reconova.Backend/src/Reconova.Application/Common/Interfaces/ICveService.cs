using Reconova.Application.Common.Models;
using Reconova.Application.DTOs.Cve;

namespace Reconova.Application.Common.Interfaces;

public interface ICveService
{
    Task<PagedResult<CveEntryDto>> SearchAsync(CveSearchRequest request, CancellationToken cancellationToken = default);
    Task<CveEntryDto> GetByIdAsync(string cveId, CancellationToken cancellationToken = default);
    Task<PagedResult<VulnerabilityAlertDto>> GetAlertsAsync(int page, int pageSize, CancellationToken cancellationToken = default);
    Task AcknowledgeAlertAsync(Guid alertId, CancellationToken cancellationToken = default);
    Task ResolveAlertAsync(Guid alertId, CancellationToken cancellationToken = default);
}
