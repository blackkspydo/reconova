using Reconova.Domain.Common;

namespace Reconova.Domain.Entities.Billing;

public class CreditPack : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public int Credits { get; set; }
    public decimal Price { get; set; }
    public string? StripePriceId { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Description { get; set; }
}
