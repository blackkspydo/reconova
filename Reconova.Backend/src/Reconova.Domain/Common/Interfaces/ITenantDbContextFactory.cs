namespace Reconova.Domain.Common.Interfaces;

public interface ITenantDbContextFactory
{
    Task<ITenantDbContext> CreateAsync(Guid tenantId, CancellationToken cancellationToken = default);
}
