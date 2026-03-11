using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Reconova.Application.Common.Interfaces;
using Reconova.Application.Common.Models;
using Reconova.Application.DTOs.Integrations;

namespace Reconova.Api.Controllers.V1.Integrations;

/// <summary>
/// Manages third-party integrations and notification rules.
/// </summary>
[ApiController]
[Route("api/v1/integrations")]
[Authorize(Policy = "RequireTenantAdmin")]
public class IntegrationsController : ControllerBase
{
    private readonly IIntegrationService _integrationService;

    public IntegrationsController(IIntegrationService integrationService)
    {
        _integrationService = integrationService;
    }

    /// <summary>
    /// Lists all configured integrations.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<IntegrationConfigDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<IntegrationConfigDto>>>> GetAll(CancellationToken cancellationToken)
    {
        var result = await _integrationService.GetIntegrationsAsync(cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<IntegrationConfigDto>>.Ok(result));
    }

    /// <summary>
    /// Gets a specific integration configuration.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<IntegrationConfigDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<IntegrationConfigDto>>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _integrationService.GetByIdAsync(id, cancellationToken);
        return Ok(ApiResponse<IntegrationConfigDto>.Ok(result));
    }

    /// <summary>
    /// Creates a new integration configuration.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<IntegrationConfigDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<IntegrationConfigDto>>> Create(
        [FromBody] CreateIntegrationRequest request, CancellationToken cancellationToken)
    {
        var result = await _integrationService.CreateAsync(request, cancellationToken);
        return StatusCode(201, ApiResponse<IntegrationConfigDto>.Ok(result));
    }

    /// <summary>
    /// Updates an existing integration configuration.
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<IntegrationConfigDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<IntegrationConfigDto>>> Update(
        Guid id, [FromBody] UpdateIntegrationRequest request, CancellationToken cancellationToken)
    {
        var result = await _integrationService.UpdateAsync(id, request, cancellationToken);
        return Ok(ApiResponse<IntegrationConfigDto>.Ok(result));
    }

    /// <summary>
    /// Removes an integration configuration.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse>> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _integrationService.DeleteAsync(id, cancellationToken);
        return Ok(ApiResponse.Ok());
    }

    /// <summary>
    /// Tests an integration connection.
    /// </summary>
    [HttpPost("{id:guid}/test")]
    [ProducesResponseType(typeof(ApiResponse<TestIntegrationResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<TestIntegrationResponse>>> Test(Guid id, CancellationToken cancellationToken)
    {
        var result = await _integrationService.TestAsync(id, cancellationToken);
        return Ok(ApiResponse<TestIntegrationResponse>.Ok(result));
    }

    /// <summary>
    /// Lists all notification rules.
    /// </summary>
    [HttpGet("rules")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<NotificationRuleDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<NotificationRuleDto>>>> GetRules(CancellationToken cancellationToken)
    {
        var result = await _integrationService.GetRulesAsync(cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<NotificationRuleDto>>.Ok(result));
    }

    /// <summary>
    /// Creates a new notification rule.
    /// </summary>
    [HttpPost("rules")]
    [ProducesResponseType(typeof(ApiResponse<NotificationRuleDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<NotificationRuleDto>>> CreateRule(
        [FromBody] CreateNotificationRuleRequest request, CancellationToken cancellationToken)
    {
        var result = await _integrationService.CreateRuleAsync(request, cancellationToken);
        return StatusCode(201, ApiResponse<NotificationRuleDto>.Ok(result));
    }

    /// <summary>
    /// Updates an existing notification rule.
    /// </summary>
    [HttpPut("rules/{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<NotificationRuleDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<NotificationRuleDto>>> UpdateRule(
        Guid id, [FromBody] UpdateNotificationRuleRequest request, CancellationToken cancellationToken)
    {
        var result = await _integrationService.UpdateRuleAsync(id, request, cancellationToken);
        return Ok(ApiResponse<NotificationRuleDto>.Ok(result));
    }

    /// <summary>
    /// Removes a notification rule.
    /// </summary>
    [HttpDelete("rules/{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse>> DeleteRule(Guid id, CancellationToken cancellationToken)
    {
        await _integrationService.DeleteRuleAsync(id, cancellationToken);
        return Ok(ApiResponse.Ok());
    }
}
