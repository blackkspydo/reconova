using Reconova.Domain.Common;

namespace Reconova.Domain.Entities.Identity;

public class Session : BaseEntity
{
    public Guid UserId { get; set; }
    public string IpAddress { get; set; } = string.Empty;
    public string UserAgent { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public DateTime? LastActiveAt { get; set; }
    public bool IsRevoked { get; set; }
    public DateTime? RevokedAt { get; set; }
    public bool IsImpersonation { get; set; }
    public Guid? ImpersonatedByUserId { get; set; }

    // Navigation
    public User User { get; set; } = null!;
}
