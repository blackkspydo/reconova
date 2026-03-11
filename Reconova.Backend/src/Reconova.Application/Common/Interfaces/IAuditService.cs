using Reconova.Application.Common.Models;
using Reconova.Application.DTOs.Admin;
using Reconova.Domain.Common.Enums;

namespace Reconova.Application.Common.Interfaces;

public interface IAuditService
{
    Task LogAsync(AuditAction action, string entityType, string? entityId = null,
        object? oldValues = null, object? newValues = null, string? description = null,
        CancellationToken cancellationToken = default);

    Task<PagedResult<AuditLogDto>> GetLogsAsync(AuditLogListRequest request, CancellationToken cancellationToken = default);
}
