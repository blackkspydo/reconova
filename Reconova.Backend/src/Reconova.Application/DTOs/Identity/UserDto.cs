using Reconova.Domain.Common.Enums;

namespace Reconova.Application.DTOs.Identity;

public record UserDto(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    UserRole Role,
    UserStatus Status,
    Guid? TenantId,
    bool EmailVerified,
    bool TwoFactorEnabled,
    DateTime? LastLoginAt,
    DateTime CreatedAt
);

public record CreateUserRequest(
    string Email,
    string Password,
    string FirstName,
    string LastName,
    UserRole Role
);

public record UpdateUserRequest(
    string? FirstName,
    string? LastName,
    UserRole? Role,
    UserStatus? Status
);

public record UserListRequest(
    int Page = 1,
    int PageSize = 20,
    string? Search = null,
    UserRole? Role = null,
    UserStatus? Status = null
);

public record SessionDto(
    Guid Id,
    string IpAddress,
    string UserAgent,
    DateTime CreatedAt,
    DateTime ExpiresAt,
    bool IsImpersonation,
    bool IsCurrent
);
