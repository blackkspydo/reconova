using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Reconova.Application.Common.Interfaces;
using Reconova.Application.Common.Models;
using Reconova.Application.DTOs.Scanning;

namespace Reconova.Api.Controllers.V1.Domains;

/// <summary>
/// Manages domain assets including registration, verification, and subdomain discovery.
/// </summary>
[ApiController]
[Route("api/v1/domains")]
[Authorize(Policy = "RequireTenantMember")]
public class DomainsController : ControllerBase
{
    private readonly IScanService _scanService;

    public DomainsController(IScanService scanService)
    {
        _scanService = scanService;
    }

    /// <summary>
    /// Lists all domains for the current tenant.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<DomainDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<DomainDto>>>> GetAll(CancellationToken cancellationToken)
    {
        var result = await _scanService.GetDomainsAsync(cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<DomainDto>>.Ok(result));
    }

    /// <summary>
    /// Gets a specific domain by ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<DomainDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<DomainDto>>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _scanService.GetDomainByIdAsync(id, cancellationToken);
        return Ok(ApiResponse<DomainDto>.Ok(result));
    }

    /// <summary>
    /// Registers a new domain for scanning.
    /// </summary>
    [HttpPost]
    [Authorize(Policy = "RequireTenantAdmin")]
    [ProducesResponseType(typeof(ApiResponse<DomainDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<DomainDto>>> Create(
        [FromBody] CreateDomainRequest request, CancellationToken cancellationToken)
    {
        var result = await _scanService.CreateDomainAsync(request, cancellationToken);
        return StatusCode(201, ApiResponse<DomainDto>.Ok(result));
    }

    /// <summary>
    /// Removes a domain from the tenant.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "RequireTenantAdmin")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse>> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _scanService.DeleteDomainAsync(id, cancellationToken);
        return Ok(ApiResponse.Ok());
    }

    /// <summary>
    /// Initiates the domain ownership verification process.
    /// </summary>
    [HttpPost("{id:guid}/verify/initiate")]
    [Authorize(Policy = "RequireTenantAdmin")]
    [ProducesResponseType(typeof(ApiResponse<DomainVerificationDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<DomainVerificationDto>>> InitiateVerification(
        Guid id, CancellationToken cancellationToken)
    {
        var result = await _scanService.InitiateVerificationAsync(id, cancellationToken);
        return Ok(ApiResponse<DomainVerificationDto>.Ok(result));
    }

    /// <summary>
    /// Completes domain ownership verification.
    /// </summary>
    [HttpPost("{id:guid}/verify")]
    [Authorize(Policy = "RequireTenantAdmin")]
    [ProducesResponseType(typeof(ApiResponse<DomainDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<DomainDto>>> Verify(Guid id, CancellationToken cancellationToken)
    {
        var result = await _scanService.VerifyDomainAsync(id, cancellationToken);
        return Ok(ApiResponse<DomainDto>.Ok(result));
    }

    /// <summary>
    /// Lists discovered subdomains for a domain with pagination.
    /// </summary>
    [HttpGet("{id:guid}/subdomains")]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<SubdomainDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<PagedResult<SubdomainDto>>>> GetSubdomains(
        Guid id, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var result = await _scanService.GetSubdomainsAsync(id, page, pageSize, cancellationToken);
        return Ok(ApiResponse<PagedResult<SubdomainDto>>.Ok(result));
    }
}
