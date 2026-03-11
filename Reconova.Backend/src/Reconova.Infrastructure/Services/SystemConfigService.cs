using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Reconova.Application.Common.Interfaces;
using Reconova.Application.Common.Models;
using Reconova.Application.DTOs.Admin;
using Reconova.Domain.Common.Enums;
using Reconova.Domain.Common.Exceptions;
using Reconova.Domain.Common.Interfaces;
using Reconova.Domain.Entities.Admin;
using Reconova.Infrastructure.Persistence.Control;

namespace Reconova.Infrastructure.Services;

public class SystemConfigService : ISystemConfigService
{
    private readonly ControlDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IMemoryCache _cache;

    public SystemConfigService(ControlDbContext context, ICurrentUserService currentUser, IMemoryCache cache)
    {
        _context = context;
        _currentUser = currentUser;
        _cache = cache;
    }

    public async Task<IReadOnlyList<SystemConfigDto>> GetAllAsync(CancellationToken cancellationToken)
    {
        var configs = await _context.SystemConfigs
            .AsNoTracking()
            .OrderBy(c => c.Category).ThenBy(c => c.Key)
            .ToListAsync(cancellationToken);

        var pendingKeys = await _context.ConfigChangeRequests
            .AsNoTracking()
            .Where(r => r.Status == ConfigApprovalStatus.Pending && r.ExpiresAt > DateTime.UtcNow)
            .Select(r => r.ConfigKey)
            .ToListAsync(cancellationToken);

        return configs.Select(c => new SystemConfigDto(
            c.Id, c.Key, c.IsSensitive ? "********" : c.Value,
            c.DefaultValue, c.DataType, c.Description,
            c.Category, c.IsCritical, c.IsSensitive, c.RequiresRestart,
            c.MinValue, c.MaxValue, c.AllowedValues, c.Unit,
            pendingKeys.Contains(c.Key), c.UpdatedAt
        )).ToList();
    }

    public async Task<SystemConfigDto> GetByKeyAsync(string key, CancellationToken cancellationToken)
    {
        var config = await _context.SystemConfigs
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Key == key, cancellationToken)
            ?? throw new NotFoundException("SystemConfig", key);

        var hasPending = await _context.ConfigChangeRequests
            .AnyAsync(r => r.ConfigKey == key && r.Status == ConfigApprovalStatus.Pending && r.ExpiresAt > DateTime.UtcNow, cancellationToken);

        return new SystemConfigDto(
            config.Id, config.Key, config.IsSensitive ? "********" : config.Value,
            config.DefaultValue, config.DataType, config.Description,
            config.Category, config.IsCritical, config.IsSensitive, config.RequiresRestart,
            config.MinValue, config.MaxValue, config.AllowedValues, config.Unit,
            hasPending, config.UpdatedAt
        );
    }

    public async Task<string> RevealSensitiveAsync(string key, CancellationToken cancellationToken)
    {
        var config = await _context.SystemConfigs
            .FirstOrDefaultAsync(c => c.Key == key, cancellationToken)
            ?? throw new NotFoundException("SystemConfig", key);

        if (!config.IsSensitive)
            throw new BusinessRuleException("NOT_SENSITIVE", "Config is not marked as sensitive");

        return config.Value;
    }

    public async Task<SystemConfigDto> UpdateAsync(string key, UpdateConfigRequest request, CancellationToken cancellationToken)
    {
        var config = await _context.SystemConfigs
            .FirstOrDefaultAsync(c => c.Key == key, cancellationToken)
            ?? throw new NotFoundException("SystemConfig", key);

        var oldValue = config.Value;

        if (config.AllowedValues != null && !config.AllowedValues.Split(',').Contains(request.Value))
            throw new BusinessRuleException("INVALID_VALUE", $"Value must be one of: {config.AllowedValues}");

        config.Value = request.Value;
        config.LastModifiedByUserId = _currentUser.UserId;

        _context.ConfigChangeHistories.Add(new ConfigChangeHistory
        {
            ConfigKey = key,
            OldValue = oldValue,
            NewValue = request.Value,
            ChangedByUserId = _currentUser.UserId ?? Guid.Empty,
            Reason = request.Reason
        });

        await _context.SaveChangesAsync(cancellationToken);
        _cache.Remove($"config:{key}");

        var hasPending = await _context.ConfigChangeRequests
            .AnyAsync(r => r.ConfigKey == key && r.Status == ConfigApprovalStatus.Pending && r.ExpiresAt > DateTime.UtcNow, cancellationToken);

        return new SystemConfigDto(
            config.Id, config.Key, config.IsSensitive ? "********" : config.Value,
            config.DefaultValue, config.DataType, config.Description,
            config.Category, config.IsCritical, config.IsSensitive, config.RequiresRestart,
            config.MinValue, config.MaxValue, config.AllowedValues, config.Unit,
            hasPending, config.UpdatedAt
        );
    }

    public async Task<PagedResult<ConfigChangeHistoryDto>> GetHistoryAsync(ConfigHistoryListRequest request, CancellationToken cancellationToken)
    {
        var query = _context.ConfigChangeHistories.AsNoTracking();

        if (!string.IsNullOrEmpty(request.ConfigKey))
            query = query.Where(h => h.ConfigKey == request.ConfigKey);
        if (request.DateFrom.HasValue)
            query = query.Where(h => h.CreatedAt >= request.DateFrom.Value);
        if (request.DateTo.HasValue)
            query = query.Where(h => h.CreatedAt <= request.DateTo.Value);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(h => h.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        var userIds = items.Select(h => h.ChangedByUserId).Distinct().ToList();
        var users = await _context.Users
            .Where(u => userIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.Email, cancellationToken);

        var dtos = items.Select(h => new ConfigChangeHistoryDto(
            h.Id, h.ConfigKey, h.OldValue, h.NewValue,
            h.Reason, h.ChangedByUserId,
            users.GetValueOrDefault(h.ChangedByUserId, ""),
            h.IsRolledBack, h.RolledBackAt, h.CreatedAt
        )).ToList();

        return new PagedResult<ConfigChangeHistoryDto> { Items = dtos, TotalCount = total, Page = request.Page, PageSize = request.PageSize };
    }

    public async Task<ConfigChangeHistoryDto> RollbackAsync(Guid historyId, RollbackConfigRequest request, CancellationToken cancellationToken)
    {
        var history = await _context.ConfigChangeHistories.FindAsync(new object[] { historyId }, cancellationToken)
            ?? throw new NotFoundException("ConfigChangeHistory", historyId);

        var config = await _context.SystemConfigs
            .FirstOrDefaultAsync(c => c.Key == history.ConfigKey, cancellationToken)
            ?? throw new NotFoundException("SystemConfig", history.ConfigKey);

        var currentValue = config.Value;
        config.Value = history.OldValue;
        config.LastModifiedByUserId = _currentUser.UserId;

        var rollbackHistory = new ConfigChangeHistory
        {
            ConfigKey = history.ConfigKey,
            OldValue = currentValue,
            NewValue = history.OldValue,
            ChangedByUserId = _currentUser.UserId ?? Guid.Empty,
            Reason = request.Reason,
            IsRolledBack = false
        };
        _context.ConfigChangeHistories.Add(rollbackHistory);

        // Mark original as rolled back
        history.IsRolledBack = true;
        history.RolledBackAt = DateTime.UtcNow;
        history.RolledBackByUserId = _currentUser.UserId;

        await _context.SaveChangesAsync(cancellationToken);
        _cache.Remove($"config:{history.ConfigKey}");

        var email = await _context.Users.Where(u => u.Id == _currentUser.UserId).Select(u => u.Email).FirstOrDefaultAsync(cancellationToken) ?? "";

        return new ConfigChangeHistoryDto(
            rollbackHistory.Id, rollbackHistory.ConfigKey, rollbackHistory.OldValue,
            rollbackHistory.NewValue, rollbackHistory.Reason, rollbackHistory.ChangedByUserId,
            email, rollbackHistory.IsRolledBack, rollbackHistory.RolledBackAt, rollbackHistory.CreatedAt
        );
    }

    public async Task<ConfigChangeRequestDto> CreateChangeRequestAsync(CreateConfigChangeRequestDto request, CancellationToken cancellationToken)
    {
        var config = await _context.SystemConfigs
            .FirstOrDefaultAsync(c => c.Key == request.ConfigKey, cancellationToken)
            ?? throw new NotFoundException("SystemConfig", request.ConfigKey);

        var changeRequest = new ConfigChangeRequest
        {
            ConfigKey = request.ConfigKey,
            CurrentValue = config.Value,
            ProposedValue = request.ProposedValue,
            Reason = request.Reason,
            RequestedByUserId = _currentUser.UserId ?? Guid.Empty,
            Status = ConfigApprovalStatus.Pending,
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        };

        _context.ConfigChangeRequests.Add(changeRequest);
        await _context.SaveChangesAsync(cancellationToken);

        return await MapChangeRequestToDtoAsync(changeRequest, cancellationToken);
    }

    public async Task<IReadOnlyList<ConfigChangeRequestDto>> GetPendingRequestsAsync(CancellationToken cancellationToken)
    {
        var requests = await _context.ConfigChangeRequests
            .AsNoTracking()
            .Where(r => r.Status == ConfigApprovalStatus.Pending && r.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(cancellationToken);

        return await MapChangeRequestsToDtosAsync(requests, cancellationToken);
    }

    public async Task<IReadOnlyList<ConfigChangeRequestDto>> GetRecentDecisionsAsync(CancellationToken cancellationToken)
    {
        var requests = await _context.ConfigChangeRequests
            .AsNoTracking()
            .Where(r => r.Status != ConfigApprovalStatus.Pending)
            .OrderByDescending(r => r.UpdatedAt)
            .Take(50)
            .ToListAsync(cancellationToken);

        return await MapChangeRequestsToDtosAsync(requests, cancellationToken);
    }

    public async Task<ConfigChangeRequestDto> ApproveRequestAsync(Guid requestId, CancellationToken cancellationToken)
    {
        var request = await _context.ConfigChangeRequests.FindAsync(new object[] { requestId }, cancellationToken)
            ?? throw new NotFoundException("ConfigChangeRequest", requestId);

        if (request.Status != ConfigApprovalStatus.Pending)
            throw new BusinessRuleException("NOT_PENDING", "Request is not pending");

        if (request.RequestedByUserId == _currentUser.UserId)
            throw new BusinessRuleException("SELF_APPROVE", "Cannot approve your own request");

        var config = await _context.SystemConfigs
            .FirstOrDefaultAsync(c => c.Key == request.ConfigKey, cancellationToken)
            ?? throw new NotFoundException("SystemConfig", request.ConfigKey);

        var oldValue = config.Value;
        config.Value = request.ProposedValue;
        config.LastModifiedByUserId = _currentUser.UserId;

        request.Status = ConfigApprovalStatus.Approved;
        request.ReviewedByUserId = _currentUser.UserId;
        request.ReviewedAt = DateTime.UtcNow;

        _context.ConfigChangeHistories.Add(new ConfigChangeHistory
        {
            ConfigKey = request.ConfigKey,
            OldValue = oldValue,
            NewValue = request.ProposedValue,
            ChangedByUserId = _currentUser.UserId ?? Guid.Empty,
            Reason = $"Approved change request: {request.Reason}"
        });

        await _context.SaveChangesAsync(cancellationToken);
        _cache.Remove($"config:{request.ConfigKey}");

        return await MapChangeRequestToDtoAsync(request, cancellationToken);
    }

    public async Task<ConfigChangeRequestDto> RejectRequestAsync(Guid requestId, RejectConfigRequestDto rejectRequest, CancellationToken cancellationToken)
    {
        var request = await _context.ConfigChangeRequests.FindAsync(new object[] { requestId }, cancellationToken)
            ?? throw new NotFoundException("ConfigChangeRequest", requestId);

        if (request.Status != ConfigApprovalStatus.Pending)
            throw new BusinessRuleException("NOT_PENDING", "Request is not pending");

        request.Status = ConfigApprovalStatus.Rejected;
        request.ReviewedByUserId = _currentUser.UserId;
        request.ReviewedAt = DateTime.UtcNow;
        request.ReviewReason = rejectRequest.Reason;

        await _context.SaveChangesAsync(cancellationToken);
        return await MapChangeRequestToDtoAsync(request, cancellationToken);
    }

    public Task<CacheStatusDto> GetCacheStatusAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult(new CacheStatusDto(null, 0));
    }

    public Task InvalidateCacheAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public async Task<T> GetValueAsync<T>(string key, CancellationToken cancellationToken)
    {
        if (_cache.TryGetValue($"config:{key}", out T? cachedValue) && cachedValue != null)
            return cachedValue;

        var config = await _context.SystemConfigs
            .FirstOrDefaultAsync(c => c.Key == key, cancellationToken)
            ?? throw new NotFoundException("SystemConfig", key);

        var value = (T)Convert.ChangeType(config.Value, typeof(T));
        _cache.Set($"config:{key}", value, TimeSpan.FromMinutes(5));
        return value;
    }

    private async Task<ConfigChangeRequestDto> MapChangeRequestToDtoAsync(ConfigChangeRequest r, CancellationToken cancellationToken)
    {
        var requestedByEmail = await _context.Users.Where(u => u.Id == r.RequestedByUserId).Select(u => u.Email).FirstOrDefaultAsync(cancellationToken) ?? "";
        string? reviewedByEmail = null;
        if (r.ReviewedByUserId.HasValue)
            reviewedByEmail = await _context.Users.Where(u => u.Id == r.ReviewedByUserId.Value).Select(u => u.Email).FirstOrDefaultAsync(cancellationToken);

        return new ConfigChangeRequestDto(
            r.Id, r.ConfigKey, r.CurrentValue, r.ProposedValue,
            r.Reason, r.RequestedByUserId, requestedByEmail,
            r.Status, r.ReviewedByUserId, reviewedByEmail,
            r.ReviewedAt, r.ReviewReason, r.ExpiresAt, r.CreatedAt
        );
    }

    private async Task<IReadOnlyList<ConfigChangeRequestDto>> MapChangeRequestsToDtosAsync(
        List<ConfigChangeRequest> requests, CancellationToken cancellationToken)
    {
        var userIds = requests.Select(r => r.RequestedByUserId)
            .Concat(requests.Where(r => r.ReviewedByUserId.HasValue).Select(r => r.ReviewedByUserId!.Value))
            .Distinct()
            .ToList();

        var userEmails = await _context.Users
            .Where(u => userIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.Email, cancellationToken);

        return requests.Select(r => new ConfigChangeRequestDto(
            r.Id, r.ConfigKey, r.CurrentValue, r.ProposedValue,
            r.Reason, r.RequestedByUserId, userEmails.GetValueOrDefault(r.RequestedByUserId, ""),
            r.Status, r.ReviewedByUserId,
            r.ReviewedByUserId.HasValue ? userEmails.GetValueOrDefault(r.ReviewedByUserId.Value) : null,
            r.ReviewedAt, r.ReviewReason, r.ExpiresAt, r.CreatedAt
        )).ToList();
    }
}
