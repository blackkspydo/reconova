using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Reconova.Application.Common.Interfaces;
using Reconova.Application.Common.Models;
using Reconova.Application.DTOs.Admin;

namespace Reconova.Api.Controllers.V1.Admin;

/// <summary>
/// Super admin endpoints for managing system configuration, change requests, and cache.
/// </summary>
[ApiController]
[Route("api/v1/admin/config")]
[Authorize(Policy = "RequireSuperAdmin")]
public class AdminConfigController : ControllerBase
{
    private readonly ISystemConfigService _configService;

    public AdminConfigController(ISystemConfigService configService)
    {
        _configService = configService;
    }

    /// <summary>
    /// Lists all system configuration entries.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<SystemConfigDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<SystemConfigDto>>>> GetAll(CancellationToken cancellationToken)
    {
        var result = await _configService.GetAllAsync(cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<SystemConfigDto>>.Ok(result));
    }

    /// <summary>
    /// Gets a specific configuration value by key.
    /// </summary>
    [HttpGet("{key}")]
    [ProducesResponseType(typeof(ApiResponse<SystemConfigDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<SystemConfigDto>>> GetByKey(string key, CancellationToken cancellationToken)
    {
        var result = await _configService.GetByKeyAsync(key, cancellationToken);
        return Ok(ApiResponse<SystemConfigDto>.Ok(result));
    }

    /// <summary>
    /// Reveals the decrypted value of a sensitive configuration.
    /// </summary>
    [HttpGet("{key}/reveal")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<object>>> RevealSensitive(string key, CancellationToken cancellationToken)
    {
        var result = await _configService.RevealSensitiveAsync(key, cancellationToken);
        return Ok(ApiResponse<object>.Ok(new { value = result }));
    }

    /// <summary>
    /// Updates a configuration value.
    /// </summary>
    [HttpPut("{key}")]
    [ProducesResponseType(typeof(ApiResponse<SystemConfigDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<SystemConfigDto>>> Update(
        string key, [FromBody] UpdateConfigRequest request, CancellationToken cancellationToken)
    {
        var result = await _configService.UpdateAsync(key, request, cancellationToken);
        return Ok(ApiResponse<SystemConfigDto>.Ok(result));
    }

    /// <summary>
    /// Lists configuration change history with pagination.
    /// </summary>
    [HttpGet("history")]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<ConfigChangeHistoryDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<PagedResult<ConfigChangeHistoryDto>>>> GetHistory(
        [FromQuery] ConfigHistoryListRequest request, CancellationToken cancellationToken)
    {
        var result = await _configService.GetHistoryAsync(request, cancellationToken);
        return Ok(ApiResponse<PagedResult<ConfigChangeHistoryDto>>.Ok(result));
    }

    /// <summary>
    /// Rolls back a configuration to a previous version.
    /// </summary>
    [HttpPost("history/{id:guid}/rollback")]
    [ProducesResponseType(typeof(ApiResponse<ConfigChangeHistoryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<ConfigChangeHistoryDto>>> Rollback(
        Guid id, [FromBody] RollbackConfigRequest request, CancellationToken cancellationToken)
    {
        var result = await _configService.RollbackAsync(id, request, cancellationToken);
        return Ok(ApiResponse<ConfigChangeHistoryDto>.Ok(result));
    }

    /// <summary>
    /// Creates a configuration change request for approval.
    /// </summary>
    [HttpPost("requests")]
    [ProducesResponseType(typeof(ApiResponse<ConfigChangeRequestDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<ConfigChangeRequestDto>>> CreateRequest(
        [FromBody] CreateConfigChangeRequestDto request, CancellationToken cancellationToken)
    {
        var result = await _configService.CreateChangeRequestAsync(request, cancellationToken);
        return StatusCode(201, ApiResponse<ConfigChangeRequestDto>.Ok(result));
    }

    /// <summary>
    /// Lists configuration change requests filtered by status.
    /// </summary>
    [HttpGet("requests")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<ConfigChangeRequestDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ConfigChangeRequestDto>>>> GetRequests(
        [FromQuery] string status = "pending", CancellationToken cancellationToken = default)
    {
        if (status == "pending")
        {
            var result = await _configService.GetPendingRequestsAsync(cancellationToken);
            return Ok(ApiResponse<IReadOnlyList<ConfigChangeRequestDto>>.Ok(result));
        }
        else
        {
            var result = await _configService.GetRecentDecisionsAsync(cancellationToken);
            return Ok(ApiResponse<IReadOnlyList<ConfigChangeRequestDto>>.Ok(result));
        }
    }

    /// <summary>
    /// Approves a pending configuration change request.
    /// </summary>
    [HttpPost("requests/{id:guid}/approve")]
    [ProducesResponseType(typeof(ApiResponse<ConfigChangeRequestDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<ConfigChangeRequestDto>>> Approve(
        Guid id, CancellationToken cancellationToken)
    {
        var result = await _configService.ApproveRequestAsync(id, cancellationToken);
        return Ok(ApiResponse<ConfigChangeRequestDto>.Ok(result));
    }

    /// <summary>
    /// Rejects a pending configuration change request.
    /// </summary>
    [HttpPost("requests/{id:guid}/reject")]
    [ProducesResponseType(typeof(ApiResponse<ConfigChangeRequestDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<ConfigChangeRequestDto>>> Reject(
        Guid id, [FromBody] RejectConfigRequestDto request, CancellationToken cancellationToken)
    {
        var result = await _configService.RejectRequestAsync(id, request, cancellationToken);
        return Ok(ApiResponse<ConfigChangeRequestDto>.Ok(result));
    }

    /// <summary>
    /// Gets the current cache status and statistics.
    /// </summary>
    [HttpGet("cache/status")]
    [ProducesResponseType(typeof(ApiResponse<CacheStatusDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<CacheStatusDto>>> GetCacheStatus(CancellationToken cancellationToken)
    {
        var result = await _configService.GetCacheStatusAsync(cancellationToken);
        return Ok(ApiResponse<CacheStatusDto>.Ok(result));
    }

    /// <summary>
    /// Invalidates all cached configuration values.
    /// </summary>
    [HttpPost("cache/invalidate")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse>> InvalidateCache(CancellationToken cancellationToken)
    {
        await _configService.InvalidateCacheAsync(cancellationToken);
        return Ok(ApiResponse.Ok());
    }
}
