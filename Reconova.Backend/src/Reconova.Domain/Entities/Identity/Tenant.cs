using Reconova.Domain.Common;
using Reconova.Domain.Common.Enums;

namespace Reconova.Domain.Entities.Identity;

public class Tenant : BaseEntity, ISoftDeletable
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public TenantStatus Status { get; set; } = TenantStatus.PendingSetup;
    public Guid OwnerId { get; set; }
    public string? CompanyName { get; set; }
    public string? Industry { get; set; }
    public int MaxUsers { get; set; } = 5;
    public int MaxDomains { get; set; } = 10;
    public DateTime? SuspendedAt { get; set; }
    public string? SuspensionReason { get; set; }
    public Guid? SuspendedByUserId { get; set; }
    public DateTime? GracePeriodEndsAt { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }

    // Navigation
    public User Owner { get; set; } = null!;
    public TenantDatabase? Database { get; set; }
    public ICollection<User> Users { get; set; } = new List<User>();
}
