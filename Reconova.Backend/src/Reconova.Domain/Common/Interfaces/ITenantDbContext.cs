namespace Reconova.Domain.Common.Interfaces;

public interface ITenantDbContext : IUnitOfWork
{
    Guid TenantId { get; }
}
