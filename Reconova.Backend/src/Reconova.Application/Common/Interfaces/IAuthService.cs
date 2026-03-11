using Reconova.Application.DTOs.Identity;

namespace Reconova.Application.Common.Interfaces;

public interface IAuthService
{
    Task<AuthResponse> LoginAsync(LoginRequest request, string ipAddress, string userAgent, CancellationToken cancellationToken = default);
    Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);
    Task<AuthResponse> RefreshTokenAsync(RefreshTokenRequest request, string ipAddress, string userAgent, CancellationToken cancellationToken = default);
    Task LogoutAsync(CancellationToken cancellationToken = default);
    Task LogoutAllSessionsAsync(CancellationToken cancellationToken = default);
    Task ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken cancellationToken = default);
    Task ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default);
    Task ChangePasswordAsync(ChangePasswordRequest request, CancellationToken cancellationToken = default);
    Task VerifyEmailAsync(string token, CancellationToken cancellationToken = default);
    Task<Enable2FaResponse> Enable2FaAsync(CancellationToken cancellationToken = default);
    Task Verify2FaAsync(Verify2FaRequest request, CancellationToken cancellationToken = default);
    Task Disable2FaAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SessionDto>> GetActiveSessionsAsync(CancellationToken cancellationToken = default);
    Task RevokeSessionAsync(Guid sessionId, CancellationToken cancellationToken = default);
}
