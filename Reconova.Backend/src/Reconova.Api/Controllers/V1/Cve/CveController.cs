using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Reconova.Application.Common.Interfaces;
using Reconova.Application.Common.Models;
using Reconova.Application.DTOs.Cve;

namespace Reconova.Api.Controllers.V1.Cve;

/// <summary>
/// Provides CVE database search and vulnerability alert management.
/// </summary>
[ApiController]
[Route("api/v1/cve")]
[Authorize(Policy = "RequireTenantMember")]
public class CveController : ControllerBase
{
    private readonly ICveService _cveService;

    public CveController(ICveService cveService)
    {
        _cveService = cveService;
    }

    /// <summary>
    /// Searches the CVE database with filters and pagination.
    /// </summary>
    [HttpGet("search")]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<CveEntryDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<PagedResult<CveEntryDto>>>> Search(
        [FromQuery] CveSearchRequest request, CancellationToken cancellationToken)
    {
        var result = await _cveService.SearchAsync(request, cancellationToken);
        return Ok(ApiResponse<PagedResult<CveEntryDto>>.Ok(result));
    }

    /// <summary>
    /// Gets details of a specific CVE entry by its CVE ID.
    /// </summary>
    [HttpGet("{cveId:regex(^CVE-\\d{{4}}-\\d{{4,}}$)}")]
    [ProducesResponseType(typeof(ApiResponse<CveEntryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<CveEntryDto>>> GetById(string cveId, CancellationToken cancellationToken)
    {
        var result = await _cveService.GetByIdAsync(cveId, cancellationToken);
        return Ok(ApiResponse<CveEntryDto>.Ok(result));
    }

    /// <summary>
    /// Lists vulnerability alerts for the current tenant with pagination.
    /// </summary>
    [HttpGet("alerts")]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<VulnerabilityAlertDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<PagedResult<VulnerabilityAlertDto>>>> GetAlerts(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var result = await _cveService.GetAlertsAsync(page, pageSize, cancellationToken);
        return Ok(ApiResponse<PagedResult<VulnerabilityAlertDto>>.Ok(result));
    }

    /// <summary>
    /// Acknowledges a vulnerability alert.
    /// </summary>
    [HttpPost("alerts/{id:guid}/acknowledge")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse>> Acknowledge(Guid id, CancellationToken cancellationToken)
    {
        await _cveService.AcknowledgeAlertAsync(id, cancellationToken);
        return Ok(ApiResponse.Ok());
    }

    /// <summary>
    /// Resolves a vulnerability alert.
    /// </summary>
    [HttpPost("alerts/{id:guid}/resolve")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse>> Resolve(Guid id, CancellationToken cancellationToken)
    {
        await _cveService.ResolveAlertAsync(id, cancellationToken);
        return Ok(ApiResponse.Ok());
    }
}
