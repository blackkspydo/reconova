using System.Security.Claims;
using Reconova.Domain.Common.Enums;

namespace Reconova.Api.Extensions;

public static class AuthorizationExtensions
{
    public static IServiceCollection AddAuthorizationPolicies(this IServiceCollection services)
    {
        services.AddAuthorizationBuilder()
            .AddPolicy("RequireSuperAdmin", policy =>
                policy.RequireAssertion(context =>
                    context.User.HasClaim(ClaimTypes.Role, UserRole.SuperAdmin.ToString())))

            .AddPolicy("RequireTenantOwner", policy =>
                policy.RequireAssertion(context =>
                {
                    var role = context.User.FindFirst(ClaimTypes.Role)?.Value;
                    return role == UserRole.SuperAdmin.ToString() ||
                           role == UserRole.TenantOwner.ToString();
                }))

            .AddPolicy("RequireTenantAdmin", policy =>
                policy.RequireAssertion(context =>
                {
                    var role = context.User.FindFirst(ClaimTypes.Role)?.Value;
                    return role == UserRole.SuperAdmin.ToString() ||
                           role == UserRole.TenantOwner.ToString() ||
                           role == UserRole.TenantAdmin.ToString();
                }))

            .AddPolicy("RequireTenantMember", policy =>
                policy.RequireAssertion(context =>
                    context.User.Identity?.IsAuthenticated == true));

        return services;
    }
}
