using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Reconova.Application.Common.Interfaces;
using Reconova.Application.Common.Models;
using Reconova.Application.DTOs.Identity;

namespace Reconova.Api.Controllers.V1.Auth;

/// <summary>
/// Handles authentication operations including login, registration, password management,
/// two-factor authentication, and session management.
/// </summary>
[ApiController]
[Route("api/v1/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    /// <summary>
    /// Authenticates a user and returns access and refresh tokens.
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<AuthResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<AuthResponse>>> Login(
        [FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var userAgent = Request.Headers.UserAgent.ToString();
        var result = await _authService.LoginAsync(request, ipAddress, userAgent, cancellationToken);
        return Ok(ApiResponse<AuthResponse>.Ok(result));
    }

    /// <summary>
    /// Registers a new user account and returns access and refresh tokens.
    /// </summary>
    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<AuthResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<AuthResponse>>> Register(
        [FromBody] RegisterRequest request, CancellationToken cancellationToken)
    {
        var result = await _authService.RegisterAsync(request, cancellationToken);
        return StatusCode(201, ApiResponse<AuthResponse>.Ok(result));
    }

    /// <summary>
    /// Refreshes an expired access token using a valid refresh token.
    /// </summary>
    [HttpPost("refresh")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<AuthResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<AuthResponse>>> RefreshToken(
        [FromBody] RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var userAgent = Request.Headers.UserAgent.ToString();
        var result = await _authService.RefreshTokenAsync(request, ipAddress, userAgent, cancellationToken);
        return Ok(ApiResponse<AuthResponse>.Ok(result));
    }

    /// <summary>
    /// Logs out the current user by invalidating the active session.
    /// </summary>
    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse>> Logout(CancellationToken cancellationToken)
    {
        await _authService.LogoutAsync(cancellationToken);
        return Ok(ApiResponse.Ok());
    }

    /// <summary>
    /// Logs out the current user from all active sessions.
    /// </summary>
    [HttpPost("logout-all")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse>> LogoutAll(CancellationToken cancellationToken)
    {
        await _authService.LogoutAllSessionsAsync(cancellationToken);
        return Ok(ApiResponse.Ok());
    }

    /// <summary>
    /// Initiates the password reset flow by sending a reset link to the user's email.
    /// </summary>
    [HttpPost("forgot-password")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse>> ForgotPassword(
        [FromBody] ForgotPasswordRequest request, CancellationToken cancellationToken)
    {
        await _authService.ForgotPasswordAsync(request, cancellationToken);
        return Ok(ApiResponse.Ok());
    }

    /// <summary>
    /// Resets the user's password using a valid reset token.
    /// </summary>
    [HttpPost("reset-password")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse>> ResetPassword(
        [FromBody] ResetPasswordRequest request, CancellationToken cancellationToken)
    {
        await _authService.ResetPasswordAsync(request, cancellationToken);
        return Ok(ApiResponse.Ok());
    }

    /// <summary>
    /// Changes the password for the currently authenticated user.
    /// </summary>
    [HttpPost("change-password")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse>> ChangePassword(
        [FromBody] ChangePasswordRequest request, CancellationToken cancellationToken)
    {
        await _authService.ChangePasswordAsync(request, cancellationToken);
        return Ok(ApiResponse.Ok());
    }

    /// <summary>
    /// Verifies a user's email address using the provided verification token.
    /// </summary>
    [HttpGet("verify-email")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse>> VerifyEmail(
        [FromQuery] string token, CancellationToken cancellationToken)
    {
        await _authService.VerifyEmailAsync(token, cancellationToken);
        return Ok(ApiResponse.Ok());
    }

    /// <summary>
    /// Enables two-factor authentication and returns the setup details.
    /// </summary>
    [HttpPost("2fa/enable")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<Enable2FaResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<Enable2FaResponse>>> Enable2Fa(CancellationToken cancellationToken)
    {
        var result = await _authService.Enable2FaAsync(cancellationToken);
        return Ok(ApiResponse<Enable2FaResponse>.Ok(result));
    }

    /// <summary>
    /// Verifies a two-factor authentication code to complete 2FA setup.
    /// </summary>
    [HttpPost("2fa/verify")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse>> Verify2Fa(
        [FromBody] Verify2FaRequest request, CancellationToken cancellationToken)
    {
        await _authService.Verify2FaAsync(request, cancellationToken);
        return Ok(ApiResponse.Ok());
    }

    /// <summary>
    /// Disables two-factor authentication for the current user.
    /// </summary>
    [HttpPost("2fa/disable")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse>> Disable2Fa(CancellationToken cancellationToken)
    {
        await _authService.Disable2FaAsync(cancellationToken);
        return Ok(ApiResponse.Ok());
    }

    /// <summary>
    /// Retrieves all active sessions for the current user.
    /// </summary>
    [HttpGet("sessions")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<SessionDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<SessionDto>>>> GetSessions(CancellationToken cancellationToken)
    {
        var result = await _authService.GetActiveSessionsAsync(cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<SessionDto>>.Ok(result));
    }

    /// <summary>
    /// Revokes a specific session by its identifier.
    /// </summary>
    [HttpDelete("sessions/{sessionId:guid}")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse>> RevokeSession(Guid sessionId, CancellationToken cancellationToken)
    {
        await _authService.RevokeSessionAsync(sessionId, cancellationToken);
        return Ok(ApiResponse.Ok());
    }
}
