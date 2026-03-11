using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Reconova.Application.Common.Interfaces;
using Reconova.Domain.Common.Interfaces;
using Reconova.Infrastructure.Identity;
using Reconova.Infrastructure.Persistence.Control;
using Reconova.Infrastructure.Persistence.Tenant;
using Reconova.Infrastructure.Services;

namespace Reconova.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Control Database
        services.AddDbContext<ControlDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("ControlDb"),
                npgsql => npgsql.MigrationsAssembly(typeof(ControlDbContext).Assembly.FullName)));

        // Tenant Database Factory
        services.AddScoped<ITenantDbContextFactory, TenantDbContextFactory>();
        services.AddMemoryCache();

        // Identity
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<ITokenService, TokenService>();

        // Unit of Work
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<ControlDbContext>());

        // Generic Repository
        services.AddScoped(typeof(IRepository<>), typeof(Persistence.Control.Repositories.ControlRepository<>));

        // Application Services
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<ITenantService, TenantService>();
        services.AddScoped<IBillingService, BillingService>();
        services.AddScoped<IScanService, ScanService>();
        services.AddScoped<IWorkflowService, WorkflowService>();
        services.AddScoped<IComplianceService, ComplianceService>();
        services.AddScoped<ICveService, CveService>();
        services.AddScoped<IIntegrationService, IntegrationService>();
        services.AddScoped<IFeatureFlagService, FeatureFlagService>();
        services.AddScoped<ISystemConfigService, SystemConfigService>();
        services.AddScoped<IAuditService, AuditService>();

        return services;
    }
}
