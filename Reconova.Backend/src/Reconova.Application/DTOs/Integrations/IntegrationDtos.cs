using Reconova.Domain.Common.Enums;

namespace Reconova.Application.DTOs.Integrations;

public record IntegrationConfigDto(
    Guid Id,
    IntegrationType Type,
    string Name,
    string? WebhookUrl,
    bool IsActive,
    DateTime? LastUsedAt,
    int FailureCount,
    int NotificationRuleCount,
    DateTime CreatedAt
);

public record CreateIntegrationRequest(
    IntegrationType Type,
    string Name,
    string? WebhookUrl,
    string? ApiKey,
    string? Configuration
);

public record UpdateIntegrationRequest(
    string? Name,
    string? WebhookUrl,
    string? ApiKey,
    string? Configuration,
    bool? IsActive
);

public record TestIntegrationResponse(
    bool Success,
    string? ErrorMessage,
    int? ResponseTimeMs
);

public record NotificationRuleDto(
    Guid Id,
    Guid IntegrationConfigId,
    string IntegrationName,
    NotificationEventType EventType,
    SeverityLevel? MinSeverity,
    bool IsActive,
    DateTime CreatedAt
);

public record CreateNotificationRuleRequest(
    Guid IntegrationConfigId,
    NotificationEventType EventType,
    SeverityLevel? MinSeverity,
    string? Filters
);

public record UpdateNotificationRuleRequest(
    NotificationEventType? EventType,
    SeverityLevel? MinSeverity,
    string? Filters,
    bool? IsActive
);
