using Reconova.Application.Common.Models;
using Reconova.Application.DTOs.Admin;

namespace Reconova.Application.Common.Interfaces;

public interface ISystemConfigService
{
    Task<IReadOnlyList<SystemConfigDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<SystemConfigDto> GetByKeyAsync(string key, CancellationToken cancellationToken = default);
    Task<string> RevealSensitiveAsync(string key, CancellationToken cancellationToken = default);
    Task<SystemConfigDto> UpdateAsync(string key, UpdateConfigRequest request, CancellationToken cancellationToken = default);
    Task<PagedResult<ConfigChangeHistoryDto>> GetHistoryAsync(ConfigHistoryListRequest request, CancellationToken cancellationToken = default);
    Task<ConfigChangeHistoryDto> RollbackAsync(Guid historyId, RollbackConfigRequest request, CancellationToken cancellationToken = default);
    Task<ConfigChangeRequestDto> CreateChangeRequestAsync(CreateConfigChangeRequestDto request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ConfigChangeRequestDto>> GetPendingRequestsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ConfigChangeRequestDto>> GetRecentDecisionsAsync(CancellationToken cancellationToken = default);
    Task<ConfigChangeRequestDto> ApproveRequestAsync(Guid requestId, CancellationToken cancellationToken = default);
    Task<ConfigChangeRequestDto> RejectRequestAsync(Guid requestId, RejectConfigRequestDto request, CancellationToken cancellationToken = default);
    Task<CacheStatusDto> GetCacheStatusAsync(CancellationToken cancellationToken = default);
    Task InvalidateCacheAsync(CancellationToken cancellationToken = default);
    Task<T> GetValueAsync<T>(string key, CancellationToken cancellationToken = default);
}
