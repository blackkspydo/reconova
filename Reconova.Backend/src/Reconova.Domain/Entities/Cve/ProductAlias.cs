using Reconova.Domain.Common;

namespace Reconova.Domain.Entities.Cve;

public class ProductAlias : BaseEntity
{
    public string CanonicalName { get; set; } = string.Empty;
    public string Alias { get; set; } = string.Empty;
    public Guid CreatedByUserId { get; set; }
}
