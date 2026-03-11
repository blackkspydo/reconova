using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Reconova.Application.Common.Interfaces;
using Reconova.Application.Common.Models;
using Reconova.Application.DTOs.Billing;
using Reconova.Application.DTOs.Tenancy;

namespace Reconova.Api.Controllers.V1.Admin;

/// <summary>
/// Super admin endpoints for managing all tenants across the platform.
/// </summary>
[ApiController]
[Route("api/v1/admin/tenants")]
[Authorize(Policy = "RequireSuperAdmin")]
public class AdminTenantsController : ControllerBase
{
    private readonly ITenantService _tenantService;
    private readonly IBillingService _billingService;

    public AdminTenantsController(ITenantService tenantService, IBillingService billingService)
    {
        _tenantService = tenantService;
        _billingService = billingService;
    }

    /// <summary>
    /// Lists all tenants with pagination.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<TenantDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<PagedResult<TenantDto>>>> GetAll(
        [FromQuery] TenantListRequest request, CancellationToken cancellationToken)
    {
        var result = await _tenantService.GetTenantsAsync(request, cancellationToken);
        return Ok(ApiResponse<PagedResult<TenantDto>>.Ok(result));
    }

    /// <summary>
    /// Gets detailed information about a specific tenant.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<TenantDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<TenantDetailDto>>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _tenantService.GetByIdAsync(id, cancellationToken);
        return Ok(ApiResponse<TenantDetailDto>.Ok(result));
    }

    /// <summary>
    /// Creates a new tenant.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<TenantDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<TenantDto>>> Create(
        [FromBody] CreateTenantRequest request, CancellationToken cancellationToken)
    {
        var result = await _tenantService.CreateAsync(request, cancellationToken);
        return StatusCode(201, ApiResponse<TenantDto>.Ok(result));
    }

    /// <summary>
    /// Updates tenant information.
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<TenantDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<TenantDto>>> Update(
        Guid id, [FromBody] UpdateTenantRequest request, CancellationToken cancellationToken)
    {
        var result = await _tenantService.UpdateAsync(id, request, cancellationToken);
        return Ok(ApiResponse<TenantDto>.Ok(result));
    }

    /// <summary>
    /// Suspends a tenant with a reason.
    /// </summary>
    [HttpPost("{id:guid}/suspend")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse>> Suspend(
        Guid id, [FromBody] SuspendTenantRequest request, CancellationToken cancellationToken)
    {
        await _tenantService.SuspendAsync(id, request, cancellationToken);
        return Ok(ApiResponse.Ok());
    }

    /// <summary>
    /// Reactivates a suspended tenant.
    /// </summary>
    [HttpPost("{id:guid}/reactivate")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse>> Reactivate(Guid id, CancellationToken cancellationToken)
    {
        await _tenantService.ReactivateAsync(id, cancellationToken);
        return Ok(ApiResponse.Ok());
    }

    /// <summary>
    /// Permanently deletes a tenant.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse>> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _tenantService.DeleteAsync(id, cancellationToken);
        return Ok(ApiResponse.Ok());
    }

    /// <summary>
    /// Adjusts a tenant's credit balance.
    /// </summary>
    [HttpPost("{id:guid}/credits/adjust")]
    [ProducesResponseType(typeof(ApiResponse<CreditTransactionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<CreditTransactionDto>>> AdjustCredits(
        Guid id, [FromBody] AdminCreditAdjustmentRequest request, CancellationToken cancellationToken)
    {
        var result = await _billingService.AdminAdjustCreditsAsync(id, request, cancellationToken);
        return Ok(ApiResponse<CreditTransactionDto>.Ok(result));
    }
}
