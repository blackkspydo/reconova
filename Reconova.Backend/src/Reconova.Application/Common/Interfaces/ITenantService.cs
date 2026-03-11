using Reconova.Application.Common.Models;
using Reconova.Application.DTOs.Tenancy;

namespace Reconova.Application.Common.Interfaces;

public interface ITenantService
{
    Task<TenantDetailDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PagedResult<TenantDto>> GetTenantsAsync(TenantListRequest request, CancellationToken cancellationToken = default);
    Task<TenantDto> CreateAsync(CreateTenantRequest request, CancellationToken cancellationToken = default);
    Task<TenantDto> UpdateAsync(Guid id, UpdateTenantRequest request, CancellationToken cancellationToken = default);
    Task SuspendAsync(Guid id, SuspendTenantRequest request, CancellationToken cancellationToken = default);
    Task ReactivateAsync(Guid id, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<TenantDto> GetCurrentTenantAsync(CancellationToken cancellationToken = default);
}
