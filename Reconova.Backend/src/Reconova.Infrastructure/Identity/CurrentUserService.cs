using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Reconova.Domain.Common.Enums;
using Reconova.Domain.Common.Interfaces;

namespace Reconova.Infrastructure.Identity;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

    public Guid? UserId
    {
        get
        {
            var claim = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return claim != null && Guid.TryParse(claim, out var id) ? id : null;
        }
    }

    public Guid? TenantId
    {
        get
        {
            var claim = User?.FindFirst("tenant_id")?.Value;
            return claim != null && Guid.TryParse(claim, out var id) ? id : null;
        }
    }

    public UserRole? Role
    {
        get
        {
            var claim = User?.FindFirst(ClaimTypes.Role)?.Value;
            return claim != null && Enum.TryParse<UserRole>(claim, out var role) ? role : null;
        }
    }

    public Guid? SessionId
    {
        get
        {
            var claim = User?.FindFirst("session_id")?.Value;
            return claim != null && Guid.TryParse(claim, out var id) ? id : null;
        }
    }

    public string? Email => User?.FindFirst(ClaimTypes.Email)?.Value;

    public bool IsAuthenticated => User?.Identity?.IsAuthenticated == true;

    public bool IsSuperAdmin => Role == UserRole.SuperAdmin;
}
