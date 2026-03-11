using Reconova.Domain.Common;

namespace Reconova.Domain.Entities.Compliance;

public class TenantComplianceSelection : BaseEntity
{
    public Guid TenantId { get; set; }
    public Guid FrameworkId { get; set; }
    public DateTime EnabledAt { get; set; }
    public DateTime? DisabledAt { get; set; }
}
