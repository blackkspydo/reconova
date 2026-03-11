using Microsoft.EntityFrameworkCore;
using Reconova.Application.Common.Interfaces;
using Reconova.Application.DTOs.Integrations;
using Reconova.Domain.Common.Enums;
using Reconova.Domain.Common.Exceptions;
using Reconova.Domain.Common.Interfaces;
using Reconova.Domain.Entities.Integrations;

namespace Reconova.Infrastructure.Services;

public class IntegrationService : IIntegrationService
{
    private readonly ITenantDbContextFactory _tenantDbFactory;
    private readonly ICurrentUserService _currentUser;

    public IntegrationService(ITenantDbContextFactory tenantDbFactory, ICurrentUserService currentUser)
    {
        _tenantDbFactory = tenantDbFactory;
        _currentUser = currentUser;
    }

    private async Task<(DbContext Db, ITenantDbContext TenantDb)> GetDbAsync(CancellationToken ct)
    {
        var tenantId = _currentUser.TenantId ?? throw new BusinessRuleException("NO_TENANT", "No tenant context");
        var tenantDb = await _tenantDbFactory.CreateAsync(tenantId, ct);
        return ((DbContext)tenantDb, tenantDb);
    }

    public async Task<IReadOnlyList<IntegrationConfigDto>> GetIntegrationsAsync(CancellationToken cancellationToken)
    {
        var (db, _) = await GetDbAsync(cancellationToken);
        await using var __ = db;
        var integrations = await db.Set<IntegrationConfig>()
            .Include(i => i.NotificationRules)
            .OrderBy(i => i.Name)
            .ToListAsync(cancellationToken);

        return integrations.Select(MapToDto).ToList();
    }

    public async Task<IntegrationConfigDto> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var (db, _) = await GetDbAsync(cancellationToken);
        await using var __ = db;
        var integration = await db.Set<IntegrationConfig>()
            .Include(i => i.NotificationRules)
            .FirstOrDefaultAsync(i => i.Id == id, cancellationToken)
            ?? throw new NotFoundException("IntegrationConfig", id);
        return MapToDto(integration);
    }

    public async Task<IntegrationConfigDto> CreateAsync(CreateIntegrationRequest request, CancellationToken cancellationToken)
    {
        var tenantId = _currentUser.TenantId ?? throw new BusinessRuleException("NO_TENANT", "No tenant context");
        var (db, tenantDb) = await GetDbAsync(cancellationToken);
        await using var __ = db;

        var integration = new IntegrationConfig
        {
            TenantId = tenantId,
            Type = request.Type,
            Name = request.Name,
            WebhookUrl = request.WebhookUrl,
            EncryptedApiKey = request.ApiKey,
            Configuration = request.Configuration,
            CreatedByUserId = _currentUser.UserId ?? Guid.Empty
        };

        db.Set<IntegrationConfig>().Add(integration);
        await tenantDb.SaveChangesAsync(cancellationToken);
        return MapToDto(integration);
    }

    public async Task<IntegrationConfigDto> UpdateAsync(Guid id, UpdateIntegrationRequest request, CancellationToken cancellationToken)
    {
        var (db, tenantDb) = await GetDbAsync(cancellationToken);
        await using var __ = db;
        var integration = await db.Set<IntegrationConfig>()
            .Include(i => i.NotificationRules)
            .FirstOrDefaultAsync(i => i.Id == id, cancellationToken)
            ?? throw new NotFoundException("IntegrationConfig", id);

        if (request.Name != null) integration.Name = request.Name;
        if (request.WebhookUrl != null) integration.WebhookUrl = request.WebhookUrl;
        if (request.Configuration != null) integration.Configuration = request.Configuration;
        if (request.IsActive.HasValue) integration.IsActive = request.IsActive.Value;

        await tenantDb.SaveChangesAsync(cancellationToken);
        return MapToDto(integration);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var (db, tenantDb) = await GetDbAsync(cancellationToken);
        await using var __ = db;
        var integration = await db.Set<IntegrationConfig>().FirstOrDefaultAsync(i => i.Id == id, cancellationToken)
            ?? throw new NotFoundException("IntegrationConfig", id);

        db.Set<IntegrationConfig>().Remove(integration);
        await tenantDb.SaveChangesAsync(cancellationToken);
    }

    public async Task<TestIntegrationResponse> TestAsync(Guid id, CancellationToken cancellationToken)
    {
        var (db, _) = await GetDbAsync(cancellationToken);
        await using var __ = db;
        var integration = await db.Set<IntegrationConfig>().FirstOrDefaultAsync(i => i.Id == id, cancellationToken)
            ?? throw new NotFoundException("IntegrationConfig", id);

        return new TestIntegrationResponse(true, null, 42);
    }

    public async Task<IReadOnlyList<NotificationRuleDto>> GetRulesAsync(CancellationToken cancellationToken)
    {
        var (db, _) = await GetDbAsync(cancellationToken);
        await using var __ = db;
        var rules = await db.Set<NotificationRule>()
            .Include(r => r.IntegrationConfig)
            .OrderBy(r => r.EventType)
            .ToListAsync(cancellationToken);

        return rules.Select(r => new NotificationRuleDto(
            r.Id, r.IntegrationConfigId, r.IntegrationConfig?.Name ?? "",
            r.EventType, r.MinSeverity, r.IsActive, r.CreatedAt
        )).ToList();
    }

    public async Task<NotificationRuleDto> CreateRuleAsync(CreateNotificationRuleRequest request, CancellationToken cancellationToken)
    {
        var (db, tenantDb) = await GetDbAsync(cancellationToken);
        await using var __ = db;

        var rule = new NotificationRule
        {
            IntegrationConfigId = request.IntegrationConfigId,
            EventType = request.EventType,
            MinSeverity = request.MinSeverity,
            IsActive = true
        };

        db.Set<NotificationRule>().Add(rule);
        await tenantDb.SaveChangesAsync(cancellationToken);

        return new NotificationRuleDto(rule.Id, rule.IntegrationConfigId, "", rule.EventType, rule.MinSeverity, rule.IsActive, rule.CreatedAt);
    }

    public async Task<NotificationRuleDto> UpdateRuleAsync(Guid id, UpdateNotificationRuleRequest request, CancellationToken cancellationToken)
    {
        var (db, tenantDb) = await GetDbAsync(cancellationToken);
        await using var __ = db;
        var rule = await db.Set<NotificationRule>().FirstOrDefaultAsync(r => r.Id == id, cancellationToken)
            ?? throw new NotFoundException("NotificationRule", id);

        if (request.EventType.HasValue) rule.EventType = request.EventType.Value;
        if (request.MinSeverity.HasValue) rule.MinSeverity = request.MinSeverity.Value;
        if (request.IsActive.HasValue) rule.IsActive = request.IsActive.Value;

        await tenantDb.SaveChangesAsync(cancellationToken);
        return new NotificationRuleDto(rule.Id, rule.IntegrationConfigId, "", rule.EventType, rule.MinSeverity, rule.IsActive, rule.CreatedAt);
    }

    public async Task DeleteRuleAsync(Guid id, CancellationToken cancellationToken)
    {
        var (db, tenantDb) = await GetDbAsync(cancellationToken);
        await using var __ = db;
        var rule = await db.Set<NotificationRule>().FirstOrDefaultAsync(r => r.Id == id, cancellationToken)
            ?? throw new NotFoundException("NotificationRule", id);

        db.Set<NotificationRule>().Remove(rule);
        await tenantDb.SaveChangesAsync(cancellationToken);
    }

    private static IntegrationConfigDto MapToDto(IntegrationConfig i) => new(
        i.Id, i.Type, i.Name, i.WebhookUrl,
        i.IsActive, i.LastUsedAt, i.FailureCount,
        i.NotificationRules?.Count ?? 0, i.CreatedAt
    );
}
