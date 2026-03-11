using Reconova.Application.Common.Models;
using Reconova.Application.DTOs.Billing;

namespace Reconova.Application.Common.Interfaces;

public interface IBillingService
{
    Task<IReadOnlyList<SubscriptionPlanDto>> GetPlansAsync(CancellationToken cancellationToken = default);
    Task<TenantSubscriptionDto> GetCurrentSubscriptionAsync(CancellationToken cancellationToken = default);
    Task<TenantSubscriptionDto> SubscribeAsync(CreateSubscriptionRequest request, CancellationToken cancellationToken = default);
    Task<TenantSubscriptionDto> ChangePlanAsync(Guid planId, CancellationToken cancellationToken = default);
    Task CancelSubscriptionAsync(CancelSubscriptionRequest request, CancellationToken cancellationToken = default);
    Task<CreditBalanceDto> GetCreditBalanceAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CreditPackDto>> GetCreditPacksAsync(CancellationToken cancellationToken = default);
    Task<CreditTransactionDto> PurchaseCreditPackAsync(PurchaseCreditPackRequest request, CancellationToken cancellationToken = default);
    Task<PagedResult<CreditTransactionDto>> GetCreditHistoryAsync(int page, int pageSize, CancellationToken cancellationToken = default);
    Task<CreditTransactionDto> AdminAdjustCreditsAsync(Guid tenantId, AdminCreditAdjustmentRequest request, CancellationToken cancellationToken = default);
    Task HandleStripeWebhookAsync(string json, string signature, CancellationToken cancellationToken = default);
}
