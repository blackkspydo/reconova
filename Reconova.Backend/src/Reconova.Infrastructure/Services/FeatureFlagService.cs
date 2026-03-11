using Microsoft.EntityFrameworkCore;
using Reconova.Application.Common.Interfaces;
using Reconova.Application.DTOs.FeatureFlags;
using Reconova.Domain.Common.Enums;
using Reconova.Domain.Common.Exceptions;
using Reconova.Domain.Common.Interfaces;
using Reconova.Domain.Entities.FeatureFlags;
using Reconova.Infrastructure.Persistence.Control;

namespace Reconova.Infrastructure.Services;

public class FeatureFlagService : IFeatureFlagService
{
    private readonly ControlDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public FeatureFlagService(ControlDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<FeatureFlagDto>> GetAllFlagsAsync(CancellationToken cancellationToken)
    {
        var flags = await _context.FeatureFlags
            .AsNoTracking()
            .OrderBy(f => f.Key)
            .ToListAsync(cancellationToken);

        return flags.Select(MapToDto).ToList();
    }

    public async Task<FeatureFlagDto> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var flag = await _context.FeatureFlags.FindAsync(new object[] { id }, cancellationToken)
            ?? throw new NotFoundException("FeatureFlag", id);
        return MapToDto(flag);
    }

    public async Task<FeatureFlagDto> CreateAsync(CreateFeatureFlagRequest request, CancellationToken cancellationToken)
    {
        if (await _context.FeatureFlags.AnyAsync(f => f.Key == request.Key, cancellationToken))
            throw new ConflictException("KEY_EXISTS", "Feature flag key already exists");

        var flag = new FeatureFlag
        {
            Key = request.Key,
            Name = request.Name,
            Description = request.Description,
            Type = request.Type,
            IsEnabled = bool.TryParse(request.DefaultValue, out var defVal) && defVal,
            DefaultEnabled = bool.TryParse(request.DefaultValue, out var defEnabled) && defEnabled,
            RolloutPercentage = request.RolloutPercentage
        };

        _context.FeatureFlags.Add(flag);
        await _context.SaveChangesAsync(cancellationToken);
        return MapToDto(flag);
    }

    public async Task<FeatureFlagDto> UpdateAsync(Guid id, UpdateFeatureFlagRequest request, CancellationToken cancellationToken)
    {
        var flag = await _context.FeatureFlags.FindAsync(new object[] { id }, cancellationToken)
            ?? throw new NotFoundException("FeatureFlag", id);

        if (request.Name != null) flag.Name = request.Name;
        if (request.Description != null) flag.Description = request.Description;
        if (request.IsEnabled.HasValue) flag.IsEnabled = request.IsEnabled.Value;
        if (request.DefaultValue != null && bool.TryParse(request.DefaultValue, out var dv)) flag.DefaultEnabled = dv;
        if (request.RolloutPercentage.HasValue) flag.RolloutPercentage = request.RolloutPercentage;

        await _context.SaveChangesAsync(cancellationToken);
        return MapToDto(flag);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var flag = await _context.FeatureFlags.FindAsync(new object[] { id }, cancellationToken)
            ?? throw new NotFoundException("FeatureFlag", id);
        _context.FeatureFlags.Remove(flag);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<FeatureFlagEvaluationDto> EvaluateAsync(string key, Guid tenantId, CancellationToken cancellationToken)
    {
        var flag = await _context.FeatureFlags.AsNoTracking().FirstOrDefaultAsync(f => f.Key == key, cancellationToken)
            ?? throw new NotFoundException("FeatureFlag", key);

        // Check tenant override
        var tenantOverride = await _context.TenantFeatureOverrides
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.TenantId == tenantId && o.FeatureFlagId == flag.Id, cancellationToken);

        if (tenantOverride != null && (tenantOverride.ExpiresAt == null || tenantOverride.ExpiresAt > DateTime.UtcNow))
            return new FeatureFlagEvaluationDto(flag.Key, tenantOverride.IsEnabled, tenantOverride.Value);

        // Check plan-based flag
        if (flag.Type == FeatureFlagType.PlanBased)
        {
            var subscription = await _context.TenantSubscriptions
                .AsNoTracking()
                .Include(s => s.Plan)
                .FirstOrDefaultAsync(s => s.TenantId == tenantId && s.Status == SubscriptionStatus.Active, cancellationToken);

            if (subscription != null && flag.AllowedPlans != null)
            {
                var allowed = flag.AllowedPlans.Contains(subscription.Plan.Tier.ToString());
                return new FeatureFlagEvaluationDto(flag.Key, allowed && flag.IsEnabled, null);
            }

            return new FeatureFlagEvaluationDto(flag.Key, false, null);
        }

        return new FeatureFlagEvaluationDto(flag.Key, flag.IsEnabled, null);
    }

    public async Task<IReadOnlyList<FeatureFlagEvaluationDto>> EvaluateAllAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        var flags = await _context.FeatureFlags.AsNoTracking().ToListAsync(cancellationToken);

        var overrides = await _context.TenantFeatureOverrides
            .AsNoTracking()
            .Where(o => o.TenantId == tenantId && (o.ExpiresAt == null || o.ExpiresAt > DateTime.UtcNow))
            .ToDictionaryAsync(o => o.FeatureFlagId, cancellationToken);

        var subscription = await _context.TenantSubscriptions
            .AsNoTracking()
            .Include(s => s.Plan)
            .FirstOrDefaultAsync(s => s.TenantId == tenantId && s.Status == SubscriptionStatus.Active, cancellationToken);

        var results = new List<FeatureFlagEvaluationDto>();
        foreach (var flag in flags)
        {
            if (overrides.TryGetValue(flag.Id, out var ov))
            {
                results.Add(new FeatureFlagEvaluationDto(flag.Key, ov.IsEnabled, ov.Value));
                continue;
            }

            if (flag.Type == FeatureFlagType.PlanBased)
            {
                if (subscription != null && flag.AllowedPlans != null)
                {
                    var allowed = flag.AllowedPlans.Contains(subscription.Plan.Tier.ToString());
                    results.Add(new FeatureFlagEvaluationDto(flag.Key, allowed && flag.IsEnabled, null));
                }
                else
                {
                    results.Add(new FeatureFlagEvaluationDto(flag.Key, false, null));
                }
                continue;
            }

            results.Add(new FeatureFlagEvaluationDto(flag.Key, flag.IsEnabled, null));
        }

        return results;
    }

    public async Task<IReadOnlyList<TenantFeatureOverrideDto>> GetOverridesAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        var tenant = await _context.Tenants.FindAsync(new object[] { tenantId }, cancellationToken)
            ?? throw new NotFoundException("Tenant", tenantId);

        var overrides = await _context.TenantFeatureOverrides
            .AsNoTracking()
            .Include(o => o.FeatureFlag)
            .Where(o => o.TenantId == tenantId)
            .ToListAsync(cancellationToken);

        return overrides.Select(o => new TenantFeatureOverrideDto(
            o.Id, o.TenantId, tenant.Name, o.FeatureFlag.Key, o.IsEnabled, o.Value, o.Reason, o.ExpiresAt, o.CreatedAt
        )).ToList();
    }

    public async Task<TenantFeatureOverrideDto> SetOverrideAsync(Guid tenantId, SetTenantOverrideRequest request, CancellationToken cancellationToken)
    {
        var tenant = await _context.Tenants.FindAsync(new object[] { tenantId }, cancellationToken)
            ?? throw new NotFoundException("Tenant", tenantId);

        var flag = await _context.FeatureFlags.FirstOrDefaultAsync(f => f.Id == request.FeatureFlagId, cancellationToken)
            ?? throw new NotFoundException("FeatureFlag", request.FeatureFlagId);

        var existing = await _context.TenantFeatureOverrides
            .FirstOrDefaultAsync(o => o.TenantId == tenantId && o.FeatureFlagId == flag.Id, cancellationToken);

        if (existing != null)
        {
            existing.IsEnabled = request.IsEnabled;
            existing.Value = request.Value;
            existing.ExpiresAt = request.ExpiresAt;
            existing.Reason = request.Reason;
        }
        else
        {
            existing = new TenantFeatureOverride
            {
                TenantId = tenantId,
                FeatureFlagId = flag.Id,
                IsEnabled = request.IsEnabled,
                Value = request.Value,
                ExpiresAt = request.ExpiresAt,
                Reason = request.Reason,
                SetByUserId = _currentUser.UserId ?? Guid.Empty
            };
            _context.TenantFeatureOverrides.Add(existing);
        }

        await _context.SaveChangesAsync(cancellationToken);

        return new TenantFeatureOverrideDto(existing.Id, existing.TenantId, tenant.Name, flag.Key, existing.IsEnabled, existing.Value, existing.Reason, existing.ExpiresAt, existing.CreatedAt);
    }

    public async Task RemoveOverrideAsync(Guid tenantId, Guid featureFlagId, CancellationToken cancellationToken)
    {
        var overrideEntity = await _context.TenantFeatureOverrides
            .FirstOrDefaultAsync(o => o.TenantId == tenantId && o.FeatureFlagId == featureFlagId, cancellationToken)
            ?? throw new NotFoundException("TenantFeatureOverride", featureFlagId);

        _context.TenantFeatureOverrides.Remove(overrideEntity);
        await _context.SaveChangesAsync(cancellationToken);
    }

    private static FeatureFlagDto MapToDto(FeatureFlag f) => new(
        f.Id, f.Key, f.Name, f.Description, f.Type,
        f.IsEnabled, f.DefaultEnabled.ToString().ToLowerInvariant(), f.RolloutPercentage, f.CreatedAt
    );
}
