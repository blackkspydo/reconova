using Reconova.Application.DTOs.FeatureFlags;

namespace Reconova.Application.Common.Interfaces;

public interface IFeatureFlagService
{
    Task<IReadOnlyList<FeatureFlagDto>> GetAllFlagsAsync(CancellationToken cancellationToken = default);
    Task<FeatureFlagDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<FeatureFlagDto> CreateAsync(CreateFeatureFlagRequest request, CancellationToken cancellationToken = default);
    Task<FeatureFlagDto> UpdateAsync(Guid id, UpdateFeatureFlagRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<FeatureFlagEvaluationDto> EvaluateAsync(string key, Guid tenantId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<FeatureFlagEvaluationDto>> EvaluateAllAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TenantFeatureOverrideDto>> GetOverridesAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<TenantFeatureOverrideDto> SetOverrideAsync(Guid tenantId, SetTenantOverrideRequest request, CancellationToken cancellationToken = default);
    Task RemoveOverrideAsync(Guid tenantId, Guid featureFlagId, CancellationToken cancellationToken = default);
}
