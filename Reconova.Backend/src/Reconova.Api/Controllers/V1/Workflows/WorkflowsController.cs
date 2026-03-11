using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Reconova.Application.Common.Interfaces;
using Reconova.Application.Common.Models;
using Reconova.Application.DTOs.Workflows;

namespace Reconova.Api.Controllers.V1.Workflows;

/// <summary>
/// Manages workflow templates and workflow execution.
/// </summary>
[ApiController]
[Route("api/v1/workflows")]
[Authorize(Policy = "RequireTenantMember")]
public class WorkflowsController : ControllerBase
{
    private readonly IWorkflowService _workflowService;

    public WorkflowsController(IWorkflowService workflowService)
    {
        _workflowService = workflowService;
    }

    /// <summary>
    /// Lists all workflow templates.
    /// </summary>
    [HttpGet("templates")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<WorkflowTemplateDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<WorkflowTemplateDto>>>> GetTemplates(CancellationToken cancellationToken)
    {
        var result = await _workflowService.GetTemplatesAsync(cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<WorkflowTemplateDto>>.Ok(result));
    }

    /// <summary>
    /// Gets a specific workflow template by ID.
    /// </summary>
    [HttpGet("templates/{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<WorkflowTemplateDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<WorkflowTemplateDto>>> GetTemplate(Guid id, CancellationToken cancellationToken)
    {
        var result = await _workflowService.GetTemplateByIdAsync(id, cancellationToken);
        return Ok(ApiResponse<WorkflowTemplateDto>.Ok(result));
    }

    /// <summary>
    /// Creates a new workflow template.
    /// </summary>
    [HttpPost("templates")]
    [Authorize(Policy = "RequireTenantAdmin")]
    [ProducesResponseType(typeof(ApiResponse<WorkflowTemplateDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<WorkflowTemplateDto>>> CreateTemplate(
        [FromBody] CreateWorkflowTemplateRequest request, CancellationToken cancellationToken)
    {
        var result = await _workflowService.CreateTemplateAsync(request, cancellationToken);
        return StatusCode(201, ApiResponse<WorkflowTemplateDto>.Ok(result));
    }

    /// <summary>
    /// Updates an existing workflow template.
    /// </summary>
    [HttpPut("templates/{id:guid}")]
    [Authorize(Policy = "RequireTenantAdmin")]
    [ProducesResponseType(typeof(ApiResponse<WorkflowTemplateDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<WorkflowTemplateDto>>> UpdateTemplate(
        Guid id, [FromBody] UpdateWorkflowTemplateRequest request, CancellationToken cancellationToken)
    {
        var result = await _workflowService.UpdateTemplateAsync(id, request, cancellationToken);
        return Ok(ApiResponse<WorkflowTemplateDto>.Ok(result));
    }

    /// <summary>
    /// Deletes a workflow template.
    /// </summary>
    [HttpDelete("templates/{id:guid}")]
    [Authorize(Policy = "RequireTenantAdmin")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse>> DeleteTemplate(Guid id, CancellationToken cancellationToken)
    {
        await _workflowService.DeleteTemplateAsync(id, cancellationToken);
        return Ok(ApiResponse.Ok());
    }

    /// <summary>
    /// Executes a workflow from a template.
    /// </summary>
    [HttpPost("execute")]
    [ProducesResponseType(typeof(ApiResponse<WorkflowDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<WorkflowDto>>> Execute(
        [FromBody] ExecuteWorkflowRequest request, CancellationToken cancellationToken)
    {
        var result = await _workflowService.ExecuteWorkflowAsync(request, cancellationToken);
        return StatusCode(201, ApiResponse<WorkflowDto>.Ok(result));
    }

    /// <summary>
    /// Lists executed workflows with pagination.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<WorkflowDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<PagedResult<WorkflowDto>>>> GetWorkflows(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var result = await _workflowService.GetWorkflowsAsync(page, pageSize, cancellationToken);
        return Ok(ApiResponse<PagedResult<WorkflowDto>>.Ok(result));
    }

    /// <summary>
    /// Gets details of a specific workflow execution.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<WorkflowDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<WorkflowDto>>> GetWorkflow(Guid id, CancellationToken cancellationToken)
    {
        var result = await _workflowService.GetWorkflowByIdAsync(id, cancellationToken);
        return Ok(ApiResponse<WorkflowDto>.Ok(result));
    }

    /// <summary>
    /// Cancels a running workflow.
    /// </summary>
    [HttpPost("{id:guid}/cancel")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse>> Cancel(Guid id, CancellationToken cancellationToken)
    {
        await _workflowService.CancelWorkflowAsync(id, cancellationToken);
        return Ok(ApiResponse.Ok());
    }
}
