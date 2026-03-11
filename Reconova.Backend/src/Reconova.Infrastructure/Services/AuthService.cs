using Microsoft.EntityFrameworkCore;
using Reconova.Application.Common.Interfaces;
using Reconova.Application.DTOs.Identity;
using Reconova.Domain.Common.Enums;
using Reconova.Domain.Common.Exceptions;
using Reconova.Domain.Common.Interfaces;
using Reconova.Domain.Entities.Identity;
using Reconova.Infrastructure.Persistence.Control;

namespace Reconova.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly ControlDbContext _context;
    private readonly ITokenService _tokenService;
    private readonly ICurrentUserService _currentUser;

    public AuthService(ControlDbContext context, ITokenService tokenService, ICurrentUserService currentUser)
    {
        _context = context;
        _tokenService = tokenService;
        _currentUser = currentUser;
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, string ipAddress, string userAgent, CancellationToken cancellationToken)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == request.Email.ToLowerInvariant(), cancellationToken)
            ?? throw new BusinessRuleException("INVALID_CREDENTIALS", "Invalid email or password");

        if (user.Status == UserStatus.Locked)
        {
            if (user.LockoutEndAt.HasValue && user.LockoutEndAt > DateTime.UtcNow)
                throw new BusinessRuleException("ACCOUNT_LOCKED", "Account is locked. Please try again later.");
            user.Status = UserStatus.Active;
            user.FailedLoginAttempts = 0;
            user.LockoutEndAt = null;
        }

        if (user.Status == UserStatus.Inactive)
            throw new ForbiddenException("ACCOUNT_INACTIVE", "Account has been deactivated.");

        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            user.FailedLoginAttempts++;
            if (user.FailedLoginAttempts >= 3)
            {
                user.Status = UserStatus.Locked;
                user.LockoutEndAt = DateTime.UtcNow.AddHours(1);
            }
            await _context.SaveChangesAsync(cancellationToken);
            throw new BusinessRuleException("INVALID_CREDENTIALS", "Invalid email or password");
        }

        user.FailedLoginAttempts = 0;
        user.LastLoginAt = DateTime.UtcNow;
        user.LastLoginIp = ipAddress;

        if (user.Status == UserStatus.PasswordExpired)
            throw new BusinessRuleException("PASSWORD_EXPIRED", "Password has expired. Please reset your password.");

        var session = new Session
        {
            UserId = user.Id,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            LastActiveAt = DateTime.UtcNow
        };
        _context.Sessions.Add(session);

        var refreshToken = _tokenService.GenerateRefreshToken();
        var refreshTokenEntity = new RefreshToken
        {
            UserId = user.Id,
            SessionId = session.Id,
            TokenHash = _tokenService.HashToken(refreshToken),
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        };
        _context.RefreshTokens.Add(refreshTokenEntity);

        await _context.SaveChangesAsync(cancellationToken);

        var accessToken = _tokenService.GenerateAccessToken(user, session.Id);

        return new AuthResponse(accessToken, refreshToken, DateTime.UtcNow.AddMinutes(15), MapUserToDto(user));
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken)
    {
        var emailLower = request.Email.ToLowerInvariant();
        if (await _context.Users.AnyAsync(u => u.Email == emailLower, cancellationToken))
            throw new ConflictException("EMAIL_EXISTS", "Email is already registered.");

        var user = new User
        {
            Email = emailLower,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            FirstName = request.FirstName,
            LastName = request.LastName,
            Role = UserRole.TenantOwner,
            Status = UserStatus.PendingVerification,
            LastPasswordChangeAt = DateTime.UtcNow
        };

        _context.Users.Add(user);

        var slug = request.CompanyName?.ToLowerInvariant().Replace(" ", "-") ?? emailLower.Split('@')[0];
        var tenant = new Tenant
        {
            Name = request.CompanyName ?? $"{request.FirstName}'s Workspace",
            Slug = slug,
            OwnerId = user.Id,
            Status = TenantStatus.PendingSetup,
            CompanyName = request.CompanyName
        };

        _context.Tenants.Add(tenant);
        user.TenantId = tenant.Id;

        _context.PasswordHistories.Add(new PasswordHistory
        {
            UserId = user.Id,
            PasswordHash = user.PasswordHash
        });

        await _context.SaveChangesAsync(cancellationToken);

        var accessToken = _tokenService.GenerateAccessToken(user);
        var refreshToken = _tokenService.GenerateRefreshToken();

        return new AuthResponse(accessToken, refreshToken, DateTime.UtcNow.AddMinutes(15), MapUserToDto(user));
    }

    public async Task<AuthResponse> RefreshTokenAsync(RefreshTokenRequest request, string ipAddress, string userAgent, CancellationToken cancellationToken)
    {
        var tokenHash = _tokenService.HashToken(request.RefreshToken);
        var refreshToken = await _context.RefreshTokens
            .Include(r => r.User)
            .Include(r => r.Session)
            .FirstOrDefaultAsync(r => r.TokenHash == tokenHash, cancellationToken)
            ?? throw new BusinessRuleException("INVALID_TOKEN", "Invalid refresh token");

        if (!refreshToken.IsActive)
            throw new BusinessRuleException("TOKEN_EXPIRED", "Refresh token is no longer active");

        refreshToken.IsUsed = true;

        if (refreshToken.Session != null)
            refreshToken.Session.LastActiveAt = DateTime.UtcNow;

        var newRefreshToken = _tokenService.GenerateRefreshToken();
        var newRefreshTokenEntity = new RefreshToken
        {
            UserId = refreshToken.UserId,
            SessionId = refreshToken.SessionId,
            TokenHash = _tokenService.HashToken(newRefreshToken),
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        };
        _context.RefreshTokens.Add(newRefreshTokenEntity);

        await _context.SaveChangesAsync(cancellationToken);

        var accessToken = _tokenService.GenerateAccessToken(refreshToken.User, refreshToken.SessionId);
        return new AuthResponse(accessToken, newRefreshToken, DateTime.UtcNow.AddMinutes(15), MapUserToDto(refreshToken.User));
    }

    public async Task LogoutAsync(CancellationToken cancellationToken)
    {
        var sessionId = _currentUser.SessionId;
        if (!sessionId.HasValue || sessionId == Guid.Empty) return;

        var session = await _context.Sessions.FindAsync(new object[] { sessionId.Value }, cancellationToken);
        if (session != null)
        {
            session.IsRevoked = true;
            session.RevokedAt = DateTime.UtcNow;
        }

        var tokens = await _context.RefreshTokens
            .Where(r => r.SessionId == sessionId && !r.IsUsed && !r.IsRevoked)
            .ToListAsync(cancellationToken);
        foreach (var token in tokens)
            token.IsRevoked = true;

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task LogoutAllSessionsAsync(CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;
        var sessions = await _context.Sessions
            .Where(s => s.UserId == userId && !s.IsRevoked)
            .ToListAsync(cancellationToken);

        foreach (var session in sessions)
        {
            session.IsRevoked = true;
            session.RevokedAt = DateTime.UtcNow;
        }

        var tokens = await _context.RefreshTokens
            .Where(r => r.UserId == userId && !r.IsUsed && !r.IsRevoked)
            .ToListAsync(cancellationToken);
        foreach (var token in tokens)
            token.IsRevoked = true;

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken cancellationToken)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == request.Email.ToLowerInvariant(), cancellationToken);
        if (user == null) return;

        user.PasswordResetToken = Guid.NewGuid().ToString();
        user.PasswordResetTokenExpiresAt = DateTime.UtcNow.AddHours(1);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.PasswordResetToken == request.Token, cancellationToken)
            ?? throw new BusinessRuleException("INVALID_TOKEN", "Invalid or expired reset token");

        if (user.PasswordResetTokenExpiresAt < DateTime.UtcNow)
            throw new BusinessRuleException("TOKEN_EXPIRED", "Reset token has expired");

        var recentPasswords = await _context.PasswordHistories
            .Where(ph => ph.UserId == user.Id)
            .OrderByDescending(ph => ph.CreatedAt)
            .Take(5)
            .ToListAsync(cancellationToken);

        foreach (var history in recentPasswords)
        {
            if (BCrypt.Net.BCrypt.Verify(request.NewPassword, history.PasswordHash))
                throw new BusinessRuleException("PASSWORD_REUSE", "Cannot reuse any of your last 5 passwords");
        }

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        user.PasswordResetToken = null;
        user.PasswordResetTokenExpiresAt = null;
        user.LastPasswordChangeAt = DateTime.UtcNow;
        user.Status = UserStatus.Active;

        _context.PasswordHistories.Add(new PasswordHistory
        {
            UserId = user.Id,
            PasswordHash = user.PasswordHash
        });

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task ChangePasswordAsync(ChangePasswordRequest request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId ?? throw new BusinessRuleException("NOT_AUTHENTICATED", "Not authenticated");
        var user = await _context.Users.FindAsync(new object[] { userId }, cancellationToken)
            ?? throw new NotFoundException("User", userId);

        if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash))
            throw new BusinessRuleException("INVALID_PASSWORD", "Current password is incorrect");

        var recentPasswords = await _context.PasswordHistories
            .Where(ph => ph.UserId == user.Id)
            .OrderByDescending(ph => ph.CreatedAt)
            .Take(5)
            .ToListAsync(cancellationToken);

        foreach (var history in recentPasswords)
        {
            if (BCrypt.Net.BCrypt.Verify(request.NewPassword, history.PasswordHash))
                throw new BusinessRuleException("PASSWORD_REUSE", "Cannot reuse any of your last 5 passwords");
        }

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        user.LastPasswordChangeAt = DateTime.UtcNow;

        _context.PasswordHistories.Add(new PasswordHistory
        {
            UserId = user.Id,
            PasswordHash = user.PasswordHash
        });

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task VerifyEmailAsync(string token, CancellationToken cancellationToken)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.EmailVerificationToken == token, cancellationToken)
            ?? throw new BusinessRuleException("INVALID_TOKEN", "Invalid verification token");

        if (user.EmailVerificationTokenExpiresAt < DateTime.UtcNow)
            throw new BusinessRuleException("TOKEN_EXPIRED", "Verification token has expired");

        user.EmailVerified = true;
        user.EmailVerificationToken = null;
        user.EmailVerificationTokenExpiresAt = null;
        if (user.Status == UserStatus.PendingVerification)
            user.Status = UserStatus.Active;

        await _context.SaveChangesAsync(cancellationToken);
    }

    public Task<Enable2FaResponse> Enable2FaAsync(CancellationToken cancellationToken)
    {
        var secret = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(20));
        return Task.FromResult(new Enable2FaResponse(secret, $"otpauth://totp/Reconova:{_currentUser.Email}?secret={secret}&issuer=Reconova"));
    }

    public async Task Verify2FaAsync(Verify2FaRequest request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId ?? throw new BusinessRuleException("NOT_AUTHENTICATED", "Not authenticated");
        var user = await _context.Users.FindAsync(new object[] { userId }, cancellationToken)
            ?? throw new NotFoundException("User", userId);

        user.TwoFactorEnabled = true;
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task Disable2FaAsync(CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId ?? throw new BusinessRuleException("NOT_AUTHENTICATED", "Not authenticated");
        var user = await _context.Users.FindAsync(new object[] { userId }, cancellationToken)
            ?? throw new NotFoundException("User", userId);

        user.TwoFactorEnabled = false;
        user.TwoFactorSecret = null;
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<SessionDto>> GetActiveSessionsAsync(CancellationToken cancellationToken)
    {
        var currentSessionId = _currentUser.SessionId;
        var sessions = await _context.Sessions
            .Where(s => s.UserId == _currentUser.UserId && !s.IsRevoked && s.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(s => s.LastActiveAt ?? s.CreatedAt)
            .Select(s => new SessionDto(s.Id, s.IpAddress, s.UserAgent, s.CreatedAt, s.ExpiresAt, s.IsImpersonation, s.Id == currentSessionId))
            .ToListAsync(cancellationToken);

        return sessions;
    }

    public async Task RevokeSessionAsync(Guid sessionId, CancellationToken cancellationToken)
    {
        var session = await _context.Sessions
            .FirstOrDefaultAsync(s => s.Id == sessionId && s.UserId == _currentUser.UserId, cancellationToken)
            ?? throw new NotFoundException("Session", sessionId);

        session.IsRevoked = true;
        session.RevokedAt = DateTime.UtcNow;

        var tokens = await _context.RefreshTokens
            .Where(r => r.SessionId == sessionId && !r.IsUsed && !r.IsRevoked)
            .ToListAsync(cancellationToken);
        foreach (var token in tokens)
            token.IsRevoked = true;

        await _context.SaveChangesAsync(cancellationToken);
    }

    private static UserDto MapUserToDto(User user) => new(
        user.Id, user.Email, user.FirstName, user.LastName,
        user.Role, user.Status, user.TenantId,
        user.EmailVerified, user.TwoFactorEnabled,
        user.LastLoginAt, user.CreatedAt
    );
}
