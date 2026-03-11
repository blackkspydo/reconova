using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Reconova.Application.Common.Interfaces;
using Reconova.Application.Common.Models;
using Reconova.Application.DTOs.Scanning;

namespace Reconova.Api.Controllers.V1.Scans;

/// <summary>
/// Manages security scans and vulnerability tracking.
/// </summary>
[ApiController]
[Route("api/v1/scans")]
[Authorize(Policy = "RequireTenantMember")]
public class ScansController : ControllerBase
{
    private readonly IScanService _scanService;

    public ScansController(IScanService scanService)
    {
        _scanService = scanService;
    }

    /// <summary>
    /// Initiates a new security scan.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<ScanJobDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<ScanJobDto>>> Create(
        [FromBody] CreateScanRequest request, CancellationToken cancellationToken)
    {
        var result = await _scanService.CreateScanAsync(request, cancellationToken);
        return StatusCode(201, ApiResponse<ScanJobDto>.Ok(result));
    }

    /// <summary>
    /// Lists all scans with pagination and filtering.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<ScanJobDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<PagedResult<ScanJobDto>>>> GetAll(
        [FromQuery] ScanListRequest request, CancellationToken cancellationToken)
    {
        var result = await _scanService.GetScansAsync(request, cancellationToken);
        return Ok(ApiResponse<PagedResult<ScanJobDto>>.Ok(result));
    }

    /// <summary>
    /// Gets details of a specific scan.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<ScanJobDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<ScanJobDto>>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _scanService.GetScanByIdAsync(id, cancellationToken);
        return Ok(ApiResponse<ScanJobDto>.Ok(result));
    }

    /// <summary>
    /// Cancels a running scan.
    /// </summary>
    [HttpPost("{id:guid}/cancel")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse>> Cancel(Guid id, CancellationToken cancellationToken)
    {
        await _scanService.CancelScanAsync(id, cancellationToken);
        return Ok(ApiResponse.Ok());
    }

    /// <summary>
    /// Lists discovered vulnerabilities with pagination.
    /// </summary>
    [HttpGet("vulnerabilities")]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<VulnerabilityDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<PagedResult<VulnerabilityDto>>>> GetVulnerabilities(
        [FromQuery] VulnerabilityListRequest request, CancellationToken cancellationToken)
    {
        var result = await _scanService.GetVulnerabilitiesAsync(request, cancellationToken);
        return Ok(ApiResponse<PagedResult<VulnerabilityDto>>.Ok(result));
    }

    /// <summary>
    /// Gets details of a specific vulnerability.
    /// </summary>
    [HttpGet("vulnerabilities/{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<VulnerabilityDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<VulnerabilityDto>>> GetVulnerability(
        Guid id, CancellationToken cancellationToken)
    {
        var result = await _scanService.GetVulnerabilityByIdAsync(id, cancellationToken);
        return Ok(ApiResponse<VulnerabilityDto>.Ok(result));
    }

    /// <summary>
    /// Marks a vulnerability as resolved.
    /// </summary>
    [HttpPost("vulnerabilities/{id:guid}/resolve")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse>> ResolveVulnerability(
        Guid id, CancellationToken cancellationToken)
    {
        await _scanService.MarkVulnerabilityResolvedAsync(id, cancellationToken);
        return Ok(ApiResponse.Ok());
    }
}
