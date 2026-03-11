using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Reconova.Application.Common.Interfaces;
using Reconova.Application.Common.Models;
using Reconova.Application.DTOs.FeatureFlags;
using Reconova.Domain.Common.Exceptions;
using Reconova.Domain.Common.Interfaces;

namespace Reconova.Api.Controllers.V1.FeatureFlags;

/// <summary>
/// Manages feature flags, evaluations, and tenant-level overrides.
/// </summary>
[ApiController]
[Route("api/v1/feature-flags")]
[Authorize]
public class FeatureFlagsController : ControllerBase
{
    private readonly IFeatureFlagService _featureFlagService;
    private readonly ICurrentUserService _currentUser;

    public FeatureFlagsController(IFeatureFlagService featureFlagService, ICurrentUserService currentUser)
    {
        _featureFlagService = featureFlagService;
        _currentUser = currentUser;
    }

    /// <summary>
    /// Evaluates all feature flags for the current tenant.
    /// </summary>
    [HttpGet("evaluate")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<FeatureFlagEvaluationDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<FeatureFlagEvaluationDto>>>> EvaluateAll(CancellationToken cancellationToken)
    {
        var tenantId = _currentUser.TenantId ?? throw new BusinessRuleException("NO_TENANT", "No tenant context");
        var result = await _featureFlagService.EvaluateAllAsync(tenantId, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<FeatureFlagEvaluationDto>>.Ok(result));
    }

    /// <summary>
    /// Evaluates a specific feature flag by key for the current tenant.
    /// </summary>
    [HttpGet("evaluate/{key}")]
    [ProducesResponseType(typeof(ApiResponse<FeatureFlagEvaluationDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<FeatureFlagEvaluationDto>>> Evaluate(string key, CancellationToken cancellationToken)
    {
        var tenantId = _currentUser.TenantId ?? throw new BusinessRuleException("NO_TENANT", "No tenant context");
        var result = await _featureFlagService.EvaluateAsync(key, tenantId, cancellationToken);
        return Ok(ApiResponse<FeatureFlagEvaluationDto>.Ok(result));
    }

    /// <summary>
    /// Lists all feature flags (super admin only).
    /// </summary>
    [HttpGet]
    [Authorize(Policy = "RequireSuperAdmin")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<FeatureFlagDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<FeatureFlagDto>>>> GetAll(CancellationToken cancellationToken)
    {
        var result = await _featureFlagService.GetAllFlagsAsync(cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<FeatureFlagDto>>.Ok(result));
    }

    /// <summary>
    /// Creates a new feature flag.
    /// </summary>
    [HttpPost]
    [Authorize(Policy = "RequireSuperAdmin")]
    [ProducesResponseType(typeof(ApiResponse<FeatureFlagDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<FeatureFlagDto>>> Create(
        [FromBody] CreateFeatureFlagRequest request, CancellationToken cancellationToken)
    {
        var result = await _featureFlagService.CreateAsync(request, cancellationToken);
        return StatusCode(201, ApiResponse<FeatureFlagDto>.Ok(result));
    }

    /// <summary>
    /// Updates an existing feature flag.
    /// </summary>
    [HttpPut("{id:guid}")]
    [Authorize(Policy = "RequireSuperAdmin")]
    [ProducesResponseType(typeof(ApiResponse<FeatureFlagDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<FeatureFlagDto>>> Update(
        Guid id, [FromBody] UpdateFeatureFlagRequest request, CancellationToken cancellationToken)
    {
        var result = await _featureFlagService.UpdateAsync(id, request, cancellationToken);
        return Ok(ApiResponse<FeatureFlagDto>.Ok(result));
    }

    /// <summary>
    /// Deletes a feature flag.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "RequireSuperAdmin")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse>> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _featureFlagService.DeleteAsync(id, cancellationToken);
        return Ok(ApiResponse.Ok());
    }

    /// <summary>
    /// Lists feature flag overrides for a specific tenant.
    /// </summary>
    [HttpGet("overrides/{tenantId:guid}")]
    [Authorize(Policy = "RequireSuperAdmin")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<TenantFeatureOverrideDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<TenantFeatureOverrideDto>>>> GetOverrides(
        Guid tenantId, CancellationToken cancellationToken)
    {
        var result = await _featureFlagService.GetOverridesAsync(tenantId, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<TenantFeatureOverrideDto>>.Ok(result));
    }

    /// <summary>
    /// Sets a feature flag override for a specific tenant.
    /// </summary>
    [HttpPost("overrides/{tenantId:guid}")]
    [Authorize(Policy = "RequireSuperAdmin")]
    [ProducesResponseType(typeof(ApiResponse<TenantFeatureOverrideDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<TenantFeatureOverrideDto>>> SetOverride(
        Guid tenantId, [FromBody] SetTenantOverrideRequest request, CancellationToken cancellationToken)
    {
        var result = await _featureFlagService.SetOverrideAsync(tenantId, request, cancellationToken);
        return Ok(ApiResponse<TenantFeatureOverrideDto>.Ok(result));
    }

    /// <summary>
    /// Removes a feature flag override for a specific tenant.
    /// </summary>
    [HttpDelete("overrides/{tenantId:guid}/{featureFlagId:guid}")]
    [Authorize(Policy = "RequireSuperAdmin")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse>> RemoveOverride(
        Guid tenantId, Guid featureFlagId, CancellationToken cancellationToken)
    {
        await _featureFlagService.RemoveOverrideAsync(tenantId, featureFlagId, cancellationToken);
        return Ok(ApiResponse.Ok());
    }
}
