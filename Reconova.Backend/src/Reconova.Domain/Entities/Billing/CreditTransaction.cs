using Reconova.Domain.Common;
using Reconova.Domain.Common.Enums;

namespace Reconova.Domain.Entities.Billing;

public class CreditTransaction : BaseEntity
{
    public Guid TenantId { get; set; }
    public CreditTransactionType Type { get; set; }
    public int Amount { get; set; }
    public int BalanceBefore { get; set; }
    public int BalanceAfter { get; set; }
    public string? Description { get; set; }
    public string? ReferenceType { get; set; }
    public Guid? ReferenceId { get; set; }
    public Guid? PerformedByUserId { get; set; }
}
