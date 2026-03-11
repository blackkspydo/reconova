using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Reconova.Domain.Common.Interfaces;
using Reconova.Infrastructure.Persistence.Control;

namespace Reconova.Infrastructure.Persistence.Tenant;

public class TenantDbContextFactory : ITenantDbContextFactory
{
    private readonly ControlDbContext _controlDb;
    private readonly IMemoryCache _cache;

    public TenantDbContextFactory(ControlDbContext controlDb, IMemoryCache cache)
    {
        _controlDb = controlDb;
        _cache = cache;
    }

    public async Task<ITenantDbContext> CreateAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var connectionString = await GetConnectionStringAsync(tenantId, cancellationToken);

        var optionsBuilder = new DbContextOptionsBuilder<TenantDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        return new TenantDbContext(optionsBuilder.Options, tenantId);
    }

    private async Task<string> GetConnectionStringAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        var cacheKey = $"tenant_conn_{tenantId}";

        if (_cache.TryGetValue<string>(cacheKey, out var cached) && cached is not null)
            return cached;

        var tenantDb = await _controlDb.TenantDatabases
            .FirstOrDefaultAsync(t => t.TenantId == tenantId && t.IsProvisioned, cancellationToken)
            ?? throw new InvalidOperationException($"No provisioned database found for tenant {tenantId}");

        var connectionString = $"Host={tenantDb.ServerHost};Port={tenantDb.ServerPort};Database={tenantDb.DatabaseName};Username={tenantDb.Username};Password={tenantDb.EncryptedPassword}";

        _cache.Set(cacheKey, connectionString, TimeSpan.FromMinutes(30));

        return connectionString;
    }
}
