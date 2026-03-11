using Reconova.Application.DTOs.Integrations;

namespace Reconova.Application.Common.Interfaces;

public interface IIntegrationService
{
    Task<IReadOnlyList<IntegrationConfigDto>> GetIntegrationsAsync(CancellationToken cancellationToken = default);
    Task<IntegrationConfigDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IntegrationConfigDto> CreateAsync(CreateIntegrationRequest request, CancellationToken cancellationToken = default);
    Task<IntegrationConfigDto> UpdateAsync(Guid id, UpdateIntegrationRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<TestIntegrationResponse> TestAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<NotificationRuleDto>> GetRulesAsync(CancellationToken cancellationToken = default);
    Task<NotificationRuleDto> CreateRuleAsync(CreateNotificationRuleRequest request, CancellationToken cancellationToken = default);
    Task<NotificationRuleDto> UpdateRuleAsync(Guid id, UpdateNotificationRuleRequest request, CancellationToken cancellationToken = default);
    Task DeleteRuleAsync(Guid id, CancellationToken cancellationToken = default);
}
