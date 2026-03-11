using Microsoft.EntityFrameworkCore;
using Reconova.Application.Common.Interfaces;
using Reconova.Application.Common.Models;
using Reconova.Application.DTOs.Tenancy;
using Reconova.Domain.Common.Enums;
using Reconova.Domain.Common.Exceptions;
using Reconova.Domain.Common.Interfaces;
using Reconova.Domain.Entities.Identity;
using Reconova.Infrastructure.Persistence.Control;

namespace Reconova.Infrastructure.Services;

public class TenantService : ITenantService
{
    private readonly ControlDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public TenantService(ControlDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<TenantDto> GetCurrentTenantAsync(CancellationToken cancellationToken)
    {
        var tenantId = _currentUser.TenantId
            ?? throw new BusinessRuleException("NO_TENANT", "No tenant context");

        var tenant = await _context.Tenants
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == tenantId, cancellationToken)
            ?? throw new NotFoundException("Tenant", tenantId);

        var userCount = await _context.Users.CountAsync(u => u.TenantId == tenantId, cancellationToken);
        var domainCount = 0; // Domains are in tenant DB

        return MapToDto(tenant, userCount, domainCount);
    }

    public async Task<TenantDetailDto> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var tenant = await _context.Tenants
            .AsNoTracking()
            .Include(t => t.Owner)
            .Include(t => t.Database)
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken)
            ?? throw new NotFoundException("Tenant", id);

        var subscription = await _context.TenantSubscriptions
            .AsNoTracking()
            .Include(s => s.Plan)
            .FirstOrDefaultAsync(s => s.TenantId == id && s.Status == SubscriptionStatus.Active, cancellationToken);

        var userCount = await _context.Users.CountAsync(u => u.TenantId == id, cancellationToken);

        SubscriptionSummaryDto? subSummary = null;
        if (subscription != null)
        {
            subSummary = new SubscriptionSummaryDto(
                subscription.Plan.Name, subscription.Plan.Tier,
                subscription.Status, subscription.CreditsRemaining,
                subscription.Plan.MonthlyCredits, subscription.CurrentPeriodEnd
            );
        }

        return new TenantDetailDto(
            tenant.Id, tenant.Name, tenant.Slug, tenant.Status,
            tenant.OwnerId, tenant.Owner.Email,
            tenant.CompanyName, tenant.Industry, tenant.MaxUsers, tenant.MaxDomains,
            userCount, 0, tenant.Database?.IsProvisioned ?? false,
            subSummary, tenant.SuspendedAt, tenant.SuspensionReason, tenant.CreatedAt
        );
    }

    public async Task<PagedResult<TenantDto>> GetTenantsAsync(TenantListRequest request, CancellationToken cancellationToken)
    {
        var query = _context.Tenants.AsNoTracking();

        if (!string.IsNullOrEmpty(request.Search))
            query = query.Where(t => t.Name.Contains(request.Search) || t.Slug.Contains(request.Search));

        if (request.Status.HasValue)
            query = query.Where(t => t.Status == request.Status.Value);

        var totalCount = await query.CountAsync(cancellationToken);
        var tenants = await query
            .OrderByDescending(t => t.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        var tenantIds = tenants.Select(t => t.Id).ToList();
        var userCounts = await _context.Users
            .Where(u => tenantIds.Contains(u.TenantId ?? Guid.Empty))
            .GroupBy(u => u.TenantId)
            .Select(g => new { TenantId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.TenantId ?? Guid.Empty, x => x.Count, cancellationToken);

        var dtos = tenants.Select(t => MapToDto(t, userCounts.GetValueOrDefault(t.Id, 0), 0)).ToList();

        return new PagedResult<TenantDto> { Items = dtos, TotalCount = totalCount, Page = request.Page, PageSize = request.PageSize };
    }

    public async Task<TenantDto> CreateAsync(CreateTenantRequest request, CancellationToken cancellationToken)
    {
        var slug = request.Name.ToLowerInvariant().Replace(" ", "-");
        if (await _context.Tenants.AnyAsync(t => t.Slug == slug, cancellationToken))
            throw new ConflictException("SLUG_TAKEN", "Tenant slug is already taken");

        var reservedSlugs = new[] { "admin", "api", "app", "www", "mail", "support", "help", "status", "docs" };
        if (reservedSlugs.Contains(slug))
            throw new BusinessRuleException("RESERVED_SLUG", "This slug is reserved");

        var tenant = new Tenant
        {
            Name = request.Name,
            Slug = slug,
            OwnerId = request.OwnerId,
            CompanyName = request.CompanyName,
            Industry = request.Industry,
            Status = TenantStatus.PendingSetup
        };

        _context.Tenants.Add(tenant);
        await _context.SaveChangesAsync(cancellationToken);

        return MapToDto(tenant, 0, 0);
    }

    public async Task<TenantDto> UpdateAsync(Guid id, UpdateTenantRequest request, CancellationToken cancellationToken)
    {
        var tenant = await _context.Tenants
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken)
            ?? throw new NotFoundException("Tenant", id);

        if (request.Name != null) tenant.Name = request.Name;
        if (request.CompanyName != null) tenant.CompanyName = request.CompanyName;
        if (request.Industry != null) tenant.Industry = request.Industry;
        if (request.MaxUsers.HasValue) tenant.MaxUsers = request.MaxUsers.Value;
        if (request.MaxDomains.HasValue) tenant.MaxDomains = request.MaxDomains.Value;

        await _context.SaveChangesAsync(cancellationToken);
        var userCount = await _context.Users.CountAsync(u => u.TenantId == id, cancellationToken);
        return MapToDto(tenant, userCount, 0);
    }

    public async Task SuspendAsync(Guid id, SuspendTenantRequest request, CancellationToken cancellationToken)
    {
        var tenant = await _context.Tenants
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken)
            ?? throw new NotFoundException("Tenant", id);

        if (tenant.Status == TenantStatus.Suspended)
            throw new BusinessRuleException("ALREADY_SUSPENDED", "Tenant is already suspended");

        tenant.Status = TenantStatus.Suspended;
        tenant.SuspendedAt = DateTime.UtcNow;
        tenant.SuspensionReason = request.Reason;
        tenant.SuspendedByUserId = _currentUser.UserId;
        tenant.GracePeriodEndsAt = DateTime.UtcNow.AddDays(30);

        var sessions = await _context.Sessions
            .Include(s => s.User)
            .Where(s => s.User.TenantId == id && !s.IsRevoked)
            .ToListAsync(cancellationToken);
        foreach (var session in sessions)
        {
            session.IsRevoked = true;
            session.RevokedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task ReactivateAsync(Guid id, CancellationToken cancellationToken)
    {
        var tenant = await _context.Tenants
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken)
            ?? throw new NotFoundException("Tenant", id);

        if (tenant.Status != TenantStatus.Suspended)
            throw new BusinessRuleException("NOT_SUSPENDED", "Tenant is not suspended");

        tenant.Status = TenantStatus.Active;
        tenant.SuspendedAt = null;
        tenant.SuspensionReason = null;
        tenant.SuspendedByUserId = null;
        tenant.GracePeriodEndsAt = null;

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var tenant = await _context.Tenants
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken)
            ?? throw new NotFoundException("Tenant", id);

        _context.Tenants.Remove(tenant);
        await _context.SaveChangesAsync(cancellationToken);
    }

    private static TenantDto MapToDto(Tenant t, int userCount, int domainCount) => new(
        t.Id, t.Name, t.Slug, t.Status, t.OwnerId,
        t.CompanyName, t.Industry, t.MaxUsers, t.MaxDomains,
        userCount, domainCount, t.CreatedAt
    );
}
