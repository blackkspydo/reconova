using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Reconova.Application.Common.Interfaces;
using Reconova.Application.Common.Models;
using Reconova.Application.DTOs.Compliance;

namespace Reconova.Api.Controllers.V1.Compliance;

/// <summary>
/// Manages compliance frameworks, controls, and assessments.
/// </summary>
[ApiController]
[Route("api/v1/compliance")]
[Authorize(Policy = "RequireTenantMember")]
public class ComplianceController : ControllerBase
{
    private readonly IComplianceService _complianceService;

    public ComplianceController(IComplianceService complianceService)
    {
        _complianceService = complianceService;
    }

    /// <summary>
    /// Lists all available compliance frameworks.
    /// </summary>
    [HttpGet("frameworks")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<ComplianceFrameworkDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ComplianceFrameworkDto>>>> GetFrameworks(CancellationToken cancellationToken)
    {
        var result = await _complianceService.GetFrameworksAsync(cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<ComplianceFrameworkDto>>.Ok(result));
    }

    /// <summary>
    /// Gets a specific compliance framework by ID.
    /// </summary>
    [HttpGet("frameworks/{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<ComplianceFrameworkDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<ComplianceFrameworkDto>>> GetFramework(Guid id, CancellationToken cancellationToken)
    {
        var result = await _complianceService.GetFrameworkByIdAsync(id, cancellationToken);
        return Ok(ApiResponse<ComplianceFrameworkDto>.Ok(result));
    }

    /// <summary>
    /// Lists controls for a specific compliance framework.
    /// </summary>
    [HttpGet("frameworks/{id:guid}/controls")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<ComplianceControlDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ComplianceControlDto>>>> GetControls(Guid id, CancellationToken cancellationToken)
    {
        var result = await _complianceService.GetControlsAsync(id, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<ComplianceControlDto>>.Ok(result));
    }

    /// <summary>
    /// Runs a new compliance assessment against a framework.
    /// </summary>
    [HttpPost("assessments")]
    [ProducesResponseType(typeof(ApiResponse<ComplianceAssessmentDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<ComplianceAssessmentDto>>> RunAssessment(
        [FromBody] RunAssessmentRequest request, CancellationToken cancellationToken)
    {
        var result = await _complianceService.RunAssessmentAsync(request, cancellationToken);
        return StatusCode(201, ApiResponse<ComplianceAssessmentDto>.Ok(result));
    }

    /// <summary>
    /// Lists compliance assessments with pagination.
    /// </summary>
    [HttpGet("assessments")]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<ComplianceAssessmentDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<PagedResult<ComplianceAssessmentDto>>>> GetAssessments(
        [FromQuery] AssessmentListRequest request, CancellationToken cancellationToken)
    {
        var result = await _complianceService.GetAssessmentsAsync(request, cancellationToken);
        return Ok(ApiResponse<PagedResult<ComplianceAssessmentDto>>.Ok(result));
    }

    /// <summary>
    /// Gets details of a specific compliance assessment.
    /// </summary>
    [HttpGet("assessments/{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<ComplianceAssessmentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<ComplianceAssessmentDto>>> GetAssessment(Guid id, CancellationToken cancellationToken)
    {
        var result = await _complianceService.GetAssessmentByIdAsync(id, cancellationToken);
        return Ok(ApiResponse<ComplianceAssessmentDto>.Ok(result));
    }
}
