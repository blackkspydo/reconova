using Reconova.Domain.Common;

namespace Reconova.Domain.Entities.Cve;

public class TenantCveSetting : BaseEntity
{
    public Guid TenantId { get; set; }
    public string DigestTimeUtc { get; set; } = "09:00"; // HH:MM format
    public bool DigestEnabled { get; set; } = true;
}
