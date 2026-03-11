using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Reconova.Application.Common.Interfaces;
using Reconova.Application.Common.Models;
using Reconova.Application.DTOs.Admin;

namespace Reconova.Api.Controllers.V1.Admin;

/// <summary>
/// Super admin endpoints for viewing platform audit logs.
/// </summary>
[ApiController]
[Route("api/v1/admin/audit")]
[Authorize(Policy = "RequireSuperAdmin")]
public class AdminAuditController : ControllerBase
{
    private readonly IAuditService _auditService;

    public AdminAuditController(IAuditService auditService)
    {
        _auditService = auditService;
    }

    /// <summary>
    /// Retrieves audit logs with pagination and filtering.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<AuditLogDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<PagedResult<AuditLogDto>>>> GetLogs(
        [FromQuery] AuditLogListRequest request, CancellationToken cancellationToken)
    {
        var result = await _auditService.GetLogsAsync(request, cancellationToken);
        return Ok(ApiResponse<PagedResult<AuditLogDto>>.Ok(result));
    }
}
