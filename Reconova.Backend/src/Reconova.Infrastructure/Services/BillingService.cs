using Microsoft.EntityFrameworkCore;
using Reconova.Application.Common.Interfaces;
using Reconova.Application.Common.Models;
using Reconova.Application.DTOs.Billing;
using Reconova.Domain.Common.Enums;
using Reconova.Domain.Common.Exceptions;
using Reconova.Domain.Common.Interfaces;
using Reconova.Domain.Entities.Billing;
using Reconova.Infrastructure.Persistence.Control;

namespace Reconova.Infrastructure.Services;

public class BillingService : IBillingService
{
    private readonly ControlDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public BillingService(ControlDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<SubscriptionPlanDto>> GetPlansAsync(CancellationToken cancellationToken)
    {
        var plans = await _context.SubscriptionPlans
            .AsNoTracking()
            .Where(p => p.IsActive)
            .OrderBy(p => p.MonthlyPrice)
            .ToListAsync(cancellationToken);

        return plans.Select(p => new SubscriptionPlanDto(
            p.Id, p.Name, p.Tier, p.MonthlyPrice, p.AnnualPrice,
            p.MonthlyCredits, p.MaxUsers, p.MaxDomains, p.MaxScansPerDay, p.Features
        )).ToList();
    }

    public async Task<TenantSubscriptionDto> GetCurrentSubscriptionAsync(CancellationToken cancellationToken)
    {
        var tenantId = _currentUser.TenantId
            ?? throw new BusinessRuleException("NO_TENANT", "No tenant context");

        var sub = await _context.TenantSubscriptions
            .AsNoTracking()
            .Include(s => s.Plan)
            .FirstOrDefaultAsync(s => s.TenantId == tenantId &&
                (s.Status == SubscriptionStatus.Active || s.Status == SubscriptionStatus.PastDue),
                cancellationToken)
            ?? throw new NotFoundException("Subscription", tenantId);

        return MapSubToDto(sub);
    }

    public async Task<TenantSubscriptionDto> SubscribeAsync(CreateSubscriptionRequest request, CancellationToken cancellationToken)
    {
        var tenantId = _currentUser.TenantId
            ?? throw new BusinessRuleException("NO_TENANT", "No tenant context");

        var plan = await _context.SubscriptionPlans.FindAsync(new object[] { request.PlanId }, cancellationToken)
            ?? throw new NotFoundException("SubscriptionPlan", request.PlanId);

        var existing = await _context.TenantSubscriptions
            .FirstOrDefaultAsync(s => s.TenantId == tenantId && s.Status == SubscriptionStatus.Active, cancellationToken);

        if (existing != null)
            throw new ConflictException("ACTIVE_SUBSCRIPTION", "Tenant already has an active subscription. Use change-plan instead.");

        var subscription = new TenantSubscription
        {
            TenantId = tenantId,
            PlanId = plan.Id,
            IsAnnual = request.IsAnnual,
            StartDate = DateTime.UtcNow,
            CurrentPeriodStart = DateTime.UtcNow,
            CurrentPeriodEnd = request.IsAnnual
                ? DateTime.UtcNow.AddYears(1)
                : DateTime.UtcNow.AddMonths(1),
            CreditsRemaining = plan.MonthlyCredits,
            CreditsResetAt = DateTime.UtcNow.AddMonths(1)
        };

        _context.TenantSubscriptions.Add(subscription);

        _context.CreditTransactions.Add(new CreditTransaction
        {
            TenantId = tenantId,
            Type = CreditTransactionType.MonthlyAllocation,
            Amount = plan.MonthlyCredits,
            BalanceBefore = 0,
            BalanceAfter = plan.MonthlyCredits,
            Description = $"Initial {plan.Name} plan credit allocation"
        });

        await _context.SaveChangesAsync(cancellationToken);

        return MapSubToDto(await _context.TenantSubscriptions
            .Include(s => s.Plan)
            .FirstAsync(s => s.Id == subscription.Id, cancellationToken));
    }

    public async Task<TenantSubscriptionDto> ChangePlanAsync(Guid planId, CancellationToken cancellationToken)
    {
        var tenantId = _currentUser.TenantId
            ?? throw new BusinessRuleException("NO_TENANT", "No tenant context");

        var subscription = await _context.TenantSubscriptions
            .Include(s => s.Plan)
            .FirstOrDefaultAsync(s => s.TenantId == tenantId && s.Status == SubscriptionStatus.Active, cancellationToken)
            ?? throw new NotFoundException("Subscription", tenantId);

        var newPlan = await _context.SubscriptionPlans.FindAsync(new object[] { planId }, cancellationToken)
            ?? throw new NotFoundException("SubscriptionPlan", planId);

        if (newPlan.Id == subscription.PlanId)
            throw new BusinessRuleException("SAME_PLAN", "Already on this plan");

        if (newPlan.MonthlyPrice > subscription.Plan.MonthlyPrice)
        {
            subscription.PlanId = newPlan.Id;
        }
        else
        {
            subscription.PendingPlanId = newPlan.Id;
        }

        await _context.SaveChangesAsync(cancellationToken);

        return MapSubToDto(await _context.TenantSubscriptions
            .Include(s => s.Plan)
            .FirstAsync(s => s.Id == subscription.Id, cancellationToken));
    }

    public async Task CancelSubscriptionAsync(CancelSubscriptionRequest request, CancellationToken cancellationToken)
    {
        var tenantId = _currentUser.TenantId
            ?? throw new BusinessRuleException("NO_TENANT", "No tenant context");

        var subscription = await _context.TenantSubscriptions
            .FirstOrDefaultAsync(s => s.TenantId == tenantId && s.Status == SubscriptionStatus.Active, cancellationToken)
            ?? throw new NotFoundException("Subscription", tenantId);

        subscription.Status = SubscriptionStatus.Cancelled;
        subscription.CancelledAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<CreditBalanceDto> GetCreditBalanceAsync(CancellationToken cancellationToken)
    {
        var tenantId = _currentUser.TenantId
            ?? throw new BusinessRuleException("NO_TENANT", "No tenant context");

        var subscription = await _context.TenantSubscriptions
            .AsNoTracking()
            .Include(s => s.Plan)
            .FirstOrDefaultAsync(s => s.TenantId == tenantId &&
                (s.Status == SubscriptionStatus.Active || s.Status == SubscriptionStatus.PastDue),
                cancellationToken);

        var recentTransactions = await _context.CreditTransactions
            .AsNoTracking()
            .Where(t => t.TenantId == tenantId)
            .OrderByDescending(t => t.CreatedAt)
            .Take(10)
            .Select(t => new CreditTransactionDto(
                t.Id, t.Type, t.Amount,
                t.BalanceBefore, t.BalanceAfter,
                t.Description, t.CreatedAt
            ))
            .ToListAsync(cancellationToken);

        return new CreditBalanceDto(
            subscription?.CreditsRemaining ?? 0,
            subscription?.Plan.MonthlyCredits ?? 0,
            subscription?.CreditsResetAt,
            recentTransactions
        );
    }

    public async Task<IReadOnlyList<CreditPackDto>> GetCreditPacksAsync(CancellationToken cancellationToken)
    {
        var packs = await _context.CreditPacks
            .AsNoTracking()
            .Where(p => p.IsActive)
            .OrderBy(p => p.Credits)
            .ToListAsync(cancellationToken);

        return packs.Select(p => new CreditPackDto(p.Id, p.Name, p.Credits, p.Price, p.Description)).ToList();
    }

    public async Task<CreditTransactionDto> PurchaseCreditPackAsync(PurchaseCreditPackRequest request, CancellationToken cancellationToken)
    {
        var tenantId = _currentUser.TenantId
            ?? throw new BusinessRuleException("NO_TENANT", "No tenant context");

        var pack = await _context.CreditPacks.FindAsync(new object[] { request.CreditPackId }, cancellationToken)
            ?? throw new NotFoundException("CreditPack", request.CreditPackId);

        var subscription = await _context.TenantSubscriptions
            .FirstOrDefaultAsync(s => s.TenantId == tenantId && s.Status == SubscriptionStatus.Active, cancellationToken)
            ?? throw new BusinessRuleException("NO_SUBSCRIPTION", "No active subscription");

        var balanceBefore = subscription.CreditsRemaining;
        subscription.CreditsRemaining += pack.Credits;

        var transaction = new CreditTransaction
        {
            TenantId = tenantId,
            Type = CreditTransactionType.PackPurchase,
            Amount = pack.Credits,
            BalanceBefore = balanceBefore,
            BalanceAfter = subscription.CreditsRemaining,
            Description = $"Purchased {pack.Name}",
            PerformedByUserId = _currentUser.UserId
        };
        _context.CreditTransactions.Add(transaction);

        await _context.SaveChangesAsync(cancellationToken);

        return new CreditTransactionDto(
            transaction.Id, transaction.Type, transaction.Amount,
            transaction.BalanceBefore, transaction.BalanceAfter,
            transaction.Description, transaction.CreatedAt
        );
    }

    public async Task<PagedResult<CreditTransactionDto>> GetCreditHistoryAsync(int page, int pageSize, CancellationToken cancellationToken)
    {
        var tenantId = _currentUser.TenantId
            ?? throw new BusinessRuleException("NO_TENANT", "No tenant context");

        var query = _context.CreditTransactions
            .AsNoTracking()
            .Where(t => t.TenantId == tenantId)
            .OrderByDescending(t => t.CreatedAt);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(t => new CreditTransactionDto(
                t.Id, t.Type, t.Amount,
                t.BalanceBefore, t.BalanceAfter,
                t.Description, t.CreatedAt
            ))
            .ToListAsync(cancellationToken);

        return new PagedResult<CreditTransactionDto> { Items = items, TotalCount = totalCount, Page = page, PageSize = pageSize };
    }

    public async Task<CreditTransactionDto> AdminAdjustCreditsAsync(Guid tenantId, AdminCreditAdjustmentRequest request, CancellationToken cancellationToken)
    {
        var subscription = await _context.TenantSubscriptions
            .FirstOrDefaultAsync(s => s.TenantId == tenantId && s.Status == SubscriptionStatus.Active, cancellationToken)
            ?? throw new NotFoundException("Subscription", tenantId);

        var balanceBefore = subscription.CreditsRemaining;
        subscription.CreditsRemaining += request.Amount;

        var transaction = new CreditTransaction
        {
            TenantId = tenantId,
            Type = CreditTransactionType.AdminAdjustment,
            Amount = request.Amount,
            BalanceBefore = balanceBefore,
            BalanceAfter = subscription.CreditsRemaining,
            Description = request.Reason,
            PerformedByUserId = _currentUser.UserId
        };
        _context.CreditTransactions.Add(transaction);
        await _context.SaveChangesAsync(cancellationToken);

        return new CreditTransactionDto(
            transaction.Id, transaction.Type, transaction.Amount,
            transaction.BalanceBefore, transaction.BalanceAfter,
            transaction.Description, transaction.CreatedAt
        );
    }

    public Task HandleStripeWebhookAsync(string json, string signature, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    private static TenantSubscriptionDto MapSubToDto(TenantSubscription s) => new(
        s.Id, s.TenantId, s.Plan.Name, s.Plan.Tier, s.Status,
        s.IsAnnual, s.CurrentPeriodStart, s.CurrentPeriodEnd,
        s.CreditsRemaining, s.Plan.MonthlyCredits, s.CreatedAt
    );
}
