using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Reconova.Application.Common.Interfaces;
using Reconova.Application.Common.Models;
using Reconova.Application.DTOs.Admin;
using Reconova.Domain.Common.Enums;
using Reconova.Domain.Common.Interfaces;
using Reconova.Domain.Entities.Admin;
using Reconova.Infrastructure.Persistence.Control;

namespace Reconova.Infrastructure.Services;

public class AuditService : IAuditService
{
    private readonly ControlDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public AuditService(ControlDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task LogAsync(AuditAction action, string entityType, string? entityId = null,
        object? oldValues = null, object? newValues = null, string? description = null,
        CancellationToken cancellationToken = default)
    {
        var log = new AuditLog
        {
            TenantId = _currentUser.TenantId,
            UserId = _currentUser.UserId == Guid.Empty ? null : _currentUser.UserId,
            UserEmail = _currentUser.Email,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            OldValues = oldValues != null ? JsonSerializer.Serialize(oldValues) : null,
            NewValues = newValues != null ? JsonSerializer.Serialize(newValues) : null,
            Description = description,
            IsAdminAction = _currentUser.Role == UserRole.SuperAdmin
        };

        _context.AuditLogs.Add(log);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<PagedResult<AuditLogDto>> GetLogsAsync(AuditLogListRequest request, CancellationToken cancellationToken)
    {
        var query = _context.AuditLogs.AsNoTracking();

        if (request.TenantId.HasValue)
            query = query.Where(a => a.TenantId == request.TenantId.Value);
        if (request.UserId.HasValue)
            query = query.Where(a => a.UserId == request.UserId.Value);
        if (request.Action.HasValue)
            query = query.Where(a => a.Action == request.Action.Value);
        if (!string.IsNullOrEmpty(request.EntityType))
            query = query.Where(a => a.EntityType == request.EntityType);
        if (request.DateFrom.HasValue)
            query = query.Where(a => a.CreatedAt >= request.DateFrom.Value);
        if (request.DateTo.HasValue)
            query = query.Where(a => a.CreatedAt <= request.DateTo.Value);
        if (!string.IsNullOrEmpty(request.Search))
            query = query.Where(a => (a.Description != null && a.Description.Contains(request.Search)) || (a.EntityId != null && a.EntityId.Contains(request.Search)));

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(a => a.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(a => new AuditLogDto(
                a.Id, a.TenantId, a.UserId, a.UserEmail,
                a.Action, a.EntityType, a.EntityId,
                a.IpAddress, a.Description, a.IsAdminAction, a.CreatedAt
            ))
            .ToListAsync(cancellationToken);

        return new PagedResult<AuditLogDto> { Items = items, TotalCount = total, Page = request.Page, PageSize = request.PageSize };
    }
}
