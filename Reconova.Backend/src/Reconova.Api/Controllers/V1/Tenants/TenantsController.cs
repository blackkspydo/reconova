using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Reconova.Application.Common.Interfaces;
using Reconova.Application.Common.Models;
using Reconova.Application.DTOs.Tenancy;

namespace Reconova.Api.Controllers.V1.Tenants;

/// <summary>
/// Manages the current tenant's settings and information.
/// </summary>
[ApiController]
[Route("api/v1/tenants")]
[Authorize]
public class TenantsController : ControllerBase
{
    private readonly ITenantService _tenantService;

    public TenantsController(ITenantService tenantService)
    {
        _tenantService = tenantService;
    }

    /// <summary>
    /// Gets the current tenant's details.
    /// </summary>
    [HttpGet("current")]
    [ProducesResponseType(typeof(ApiResponse<TenantDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<TenantDto>>> GetCurrent(CancellationToken cancellationToken)
    {
        var result = await _tenantService.GetCurrentTenantAsync(cancellationToken);
        return Ok(ApiResponse<TenantDto>.Ok(result));
    }

    /// <summary>
    /// Updates the current tenant's settings.
    /// </summary>
    [HttpPut("current")]
    [Authorize(Policy = "RequireTenantOwner")]
    [ProducesResponseType(typeof(ApiResponse<TenantDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<TenantDto>>> UpdateCurrent(
        [FromBody] UpdateTenantRequest request, CancellationToken cancellationToken)
    {
        var current = await _tenantService.GetCurrentTenantAsync(cancellationToken);
        var result = await _tenantService.UpdateAsync(current.Id, request, cancellationToken);
        return Ok(ApiResponse<TenantDto>.Ok(result));
    }
}
