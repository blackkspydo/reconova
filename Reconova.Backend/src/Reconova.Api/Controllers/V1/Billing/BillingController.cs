using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Reconova.Application.Common.Interfaces;
using Reconova.Application.Common.Models;
using Reconova.Application.DTOs.Billing;

namespace Reconova.Api.Controllers.V1.Billing;

/// <summary>
/// Manages subscriptions, billing plans, and credit operations.
/// </summary>
[ApiController]
[Route("api/v1/billing")]
[Authorize]
public class BillingController : ControllerBase
{
    private readonly IBillingService _billingService;

    public BillingController(IBillingService billingService)
    {
        _billingService = billingService;
    }

    /// <summary>
    /// Lists all available subscription plans.
    /// </summary>
    [HttpGet("plans")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<SubscriptionPlanDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<SubscriptionPlanDto>>>> GetPlans(CancellationToken cancellationToken)
    {
        var result = await _billingService.GetPlansAsync(cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<SubscriptionPlanDto>>.Ok(result));
    }

    /// <summary>
    /// Gets the current tenant's subscription details.
    /// </summary>
    [HttpGet("subscription")]
    [Authorize(Policy = "RequireTenantOwner")]
    [ProducesResponseType(typeof(ApiResponse<TenantSubscriptionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<TenantSubscriptionDto>>> GetSubscription(CancellationToken cancellationToken)
    {
        var result = await _billingService.GetCurrentSubscriptionAsync(cancellationToken);
        return Ok(ApiResponse<TenantSubscriptionDto>.Ok(result));
    }

    /// <summary>
    /// Creates a new subscription for the tenant.
    /// </summary>
    [HttpPost("subscription")]
    [Authorize(Policy = "RequireTenantOwner")]
    [ProducesResponseType(typeof(ApiResponse<TenantSubscriptionDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<TenantSubscriptionDto>>> Subscribe(
        [FromBody] CreateSubscriptionRequest request, CancellationToken cancellationToken)
    {
        var result = await _billingService.SubscribeAsync(request, cancellationToken);
        return StatusCode(201, ApiResponse<TenantSubscriptionDto>.Ok(result));
    }

    /// <summary>
    /// Changes the tenant's subscription plan.
    /// </summary>
    [HttpPut("subscription/plan/{planId:guid}")]
    [Authorize(Policy = "RequireTenantOwner")]
    [ProducesResponseType(typeof(ApiResponse<TenantSubscriptionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<TenantSubscriptionDto>>> ChangePlan(
        Guid planId, CancellationToken cancellationToken)
    {
        var result = await _billingService.ChangePlanAsync(planId, cancellationToken);
        return Ok(ApiResponse<TenantSubscriptionDto>.Ok(result));
    }

    /// <summary>
    /// Cancels the tenant's current subscription.
    /// </summary>
    [HttpPost("subscription/cancel")]
    [Authorize(Policy = "RequireTenantOwner")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse>> Cancel(
        [FromBody] CancelSubscriptionRequest request, CancellationToken cancellationToken)
    {
        await _billingService.CancelSubscriptionAsync(request, cancellationToken);
        return Ok(ApiResponse.Ok());
    }

    /// <summary>
    /// Gets the current tenant's credit balance.
    /// </summary>
    [HttpGet("credits")]
    [ProducesResponseType(typeof(ApiResponse<CreditBalanceDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<CreditBalanceDto>>> GetCredits(CancellationToken cancellationToken)
    {
        var result = await _billingService.GetCreditBalanceAsync(cancellationToken);
        return Ok(ApiResponse<CreditBalanceDto>.Ok(result));
    }

    /// <summary>
    /// Lists available credit packs for purchase.
    /// </summary>
    [HttpGet("credits/packs")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<CreditPackDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<CreditPackDto>>>> GetCreditPacks(CancellationToken cancellationToken)
    {
        var result = await _billingService.GetCreditPacksAsync(cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<CreditPackDto>>.Ok(result));
    }

    /// <summary>
    /// Purchases a credit pack.
    /// </summary>
    [HttpPost("credits/purchase")]
    [Authorize(Policy = "RequireTenantOwner")]
    [ProducesResponseType(typeof(ApiResponse<CreditTransactionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<CreditTransactionDto>>> PurchasePack(
        [FromBody] PurchaseCreditPackRequest request, CancellationToken cancellationToken)
    {
        var result = await _billingService.PurchaseCreditPackAsync(request, cancellationToken);
        return Ok(ApiResponse<CreditTransactionDto>.Ok(result));
    }

    /// <summary>
    /// Gets credit transaction history with pagination.
    /// </summary>
    [HttpGet("credits/history")]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<CreditTransactionDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<PagedResult<CreditTransactionDto>>>> GetCreditHistory(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var result = await _billingService.GetCreditHistoryAsync(page, pageSize, cancellationToken);
        return Ok(ApiResponse<PagedResult<CreditTransactionDto>>.Ok(result));
    }

    /// <summary>
    /// Handles incoming Stripe webhook events.
    /// </summary>
    [HttpPost("webhooks/stripe")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> StripeWebhook(CancellationToken cancellationToken)
    {
        var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync(cancellationToken);
        var signature = Request.Headers["Stripe-Signature"].ToString();
        await _billingService.HandleStripeWebhookAsync(json, signature, cancellationToken);
        return Ok();
    }
}
