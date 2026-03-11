using Reconova.Domain.Common.Enums;

namespace Reconova.Application.DTOs.Tenancy;

public record TenantDto(
    Guid Id,
    string Name,
    string Slug,
    TenantStatus Status,
    Guid OwnerId,
    string? CompanyName,
    string? Industry,
    int MaxUsers,
    int MaxDomains,
    int CurrentUserCount,
    int CurrentDomainCount,
    DateTime CreatedAt
);

public record TenantDetailDto(
    Guid Id,
    string Name,
    string Slug,
    TenantStatus Status,
    Guid OwnerId,
    string OwnerEmail,
    string? CompanyName,
    string? Industry,
    int MaxUsers,
    int MaxDomains,
    int CurrentUserCount,
    int CurrentDomainCount,
    bool DatabaseProvisioned,
    SubscriptionSummaryDto? Subscription,
    DateTime? SuspendedAt,
    string? SuspensionReason,
    DateTime CreatedAt
);

public record SubscriptionSummaryDto(
    string PlanName,
    SubscriptionTier Tier,
    SubscriptionStatus Status,
    int CreditsRemaining,
    int MonthlyCredits,
    DateTime CurrentPeriodEnd
);

public record CreateTenantRequest(
    string Name,
    string? CompanyName,
    string? Industry,
    Guid OwnerId
);

public record UpdateTenantRequest(
    string? Name,
    string? CompanyName,
    string? Industry,
    int? MaxUsers,
    int? MaxDomains
);

public record SuspendTenantRequest(string Reason);

public record TenantListRequest(
    int Page = 1,
    int PageSize = 20,
    string? Search = null,
    TenantStatus? Status = null
);
