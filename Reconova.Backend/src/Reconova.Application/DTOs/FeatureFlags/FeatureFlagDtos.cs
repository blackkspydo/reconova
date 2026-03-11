using Reconova.Domain.Common.Enums;

namespace Reconova.Application.DTOs.FeatureFlags;

public record FeatureFlagDto(
    Guid Id,
    string Key,
    string Name,
    string? Description,
    FeatureFlagType Type,
    bool IsEnabled,
    string? DefaultValue,
    int? RolloutPercentage,
    DateTime CreatedAt
);

public record FeatureFlagEvaluationDto(
    string Key,
    bool IsEnabled,
    string? Value
);

public record CreateFeatureFlagRequest(
    string Key,
    string Name,
    string? Description,
    FeatureFlagType Type,
    string? DefaultValue,
    int? RolloutPercentage
);

public record UpdateFeatureFlagRequest(
    string? Name,
    string? Description,
    bool? IsEnabled,
    string? DefaultValue,
    int? RolloutPercentage
);

public record TenantFeatureOverrideDto(
    Guid Id,
    Guid TenantId,
    string TenantName,
    string FeatureFlagKey,
    bool IsEnabled,
    string? Value,
    string? Reason,
    DateTime? ExpiresAt,
    DateTime CreatedAt
);

public record SetTenantOverrideRequest(
    Guid FeatureFlagId,
    bool IsEnabled,
    string? Value,
    string? Reason,
    DateTime? ExpiresAt
);
