using Reconova.Domain.Common.Enums;

namespace Reconova.Application.DTOs.Billing;

public record SubscriptionPlanDto(
    Guid Id,
    string Name,
    SubscriptionTier Tier,
    decimal MonthlyPrice,
    decimal AnnualPrice,
    int MonthlyCredits,
    int MaxUsers,
    int MaxDomains,
    int MaxScansPerDay,
    string? Features
);

public record TenantSubscriptionDto(
    Guid Id,
    Guid TenantId,
    string PlanName,
    SubscriptionTier Tier,
    SubscriptionStatus Status,
    bool IsAnnual,
    DateTime CurrentPeriodStart,
    DateTime CurrentPeriodEnd,
    int CreditsRemaining,
    int MonthlyCredits,
    DateTime CreatedAt
);

public record CreateSubscriptionRequest(
    Guid PlanId,
    bool IsAnnual,
    string? PaymentMethodId
);

public record CancelSubscriptionRequest(string? Reason);

public record CreditTransactionDto(
    Guid Id,
    CreditTransactionType Type,
    int Amount,
    int BalanceBefore,
    int BalanceAfter,
    string? Description,
    DateTime CreatedAt
);

public record CreditBalanceDto(
    int Balance,
    int MonthlyAllocation,
    DateTime? NextResetDate,
    IReadOnlyList<CreditTransactionDto> RecentTransactions
);

public record CreditPackDto(
    Guid Id,
    string Name,
    int Credits,
    decimal Price,
    string? Description
);

public record PurchaseCreditPackRequest(
    Guid CreditPackId,
    string? PaymentMethodId
);

public record AdminCreditAdjustmentRequest(
    int Amount,
    string Reason
);
