using Reconova.Domain.Common.Enums;

namespace Reconova.Application.DTOs.Admin;

public record SystemConfigDto(
    Guid Id,
    string Key,
    string Value,
    string DefaultValue,
    ConfigDataType DataType,
    string? Description,
    string Category,
    bool IsCritical,
    bool IsSensitive,
    bool RequiresRestart,
    string? MinValue,
    string? MaxValue,
    string? AllowedValues,
    string? Unit,
    bool HasPendingRequest,
    DateTime UpdatedAt
);

public record UpdateConfigRequest(
    string Value,
    string Reason
);

public record ConfigChangeHistoryDto(
    Guid Id,
    string ConfigKey,
    string OldValue,
    string NewValue,
    string Reason,
    Guid ChangedByUserId,
    string ChangedByEmail,
    bool IsRolledBack,
    DateTime? RolledBackAt,
    DateTime CreatedAt
);

public record ConfigHistoryListRequest(
    int Page = 1,
    int PageSize = 25,
    string? ConfigKey = null,
    string? Category = null,
    DateTime? DateFrom = null,
    DateTime? DateTo = null,
    string? Search = null
);

public record RollbackConfigRequest(string Reason);

public record ConfigChangeRequestDto(
    Guid Id,
    string ConfigKey,
    string CurrentValue,
    string ProposedValue,
    string Reason,
    Guid RequestedByUserId,
    string RequestedByEmail,
    ConfigApprovalStatus Status,
    Guid? ReviewedByUserId,
    string? ReviewedByEmail,
    DateTime? ReviewedAt,
    string? ReviewReason,
    DateTime ExpiresAt,
    DateTime CreatedAt
);

public record CreateConfigChangeRequestDto(
    string ConfigKey,
    string ProposedValue,
    string Reason
);

public record RejectConfigRequestDto(string Reason);

public record AuditLogDto(
    Guid Id,
    Guid? TenantId,
    Guid? UserId,
    string? UserEmail,
    AuditAction Action,
    string EntityType,
    string? EntityId,
    string? IpAddress,
    string? Description,
    bool IsAdminAction,
    DateTime CreatedAt
);

public record AuditLogListRequest(
    int Page = 1,
    int PageSize = 25,
    Guid? TenantId = null,
    Guid? UserId = null,
    AuditAction? Action = null,
    string? EntityType = null,
    DateTime? DateFrom = null,
    DateTime? DateTo = null,
    string? Search = null
);

public record ImpersonateRequest(Guid UserId, string Reason);

public record CacheStatusDto(
    DateTime? LastInvalidatedAt,
    int CachedConfigCount
);
