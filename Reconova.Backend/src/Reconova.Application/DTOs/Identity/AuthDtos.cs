namespace Reconova.Application.DTOs.Identity;

public record LoginRequest(string Email, string Password, string? TwoFactorCode = null);

public record RegisterRequest(
    string Email,
    string Password,
    string FirstName,
    string LastName,
    string? TenantName = null,
    string? CompanyName = null
);

public record AuthResponse(
    string AccessToken,
    string RefreshToken,
    DateTime ExpiresAt,
    UserDto User
);

public record RefreshTokenRequest(string RefreshToken);

public record ForgotPasswordRequest(string Email);

public record ResetPasswordRequest(string Token, string NewPassword);

public record ChangePasswordRequest(string CurrentPassword, string NewPassword);

public record Enable2FaResponse(string Secret, string QrCodeUri);

public record Verify2FaRequest(string Code);
