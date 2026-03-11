using Reconova.Domain.Common;

namespace Reconova.Domain.Entities.Identity;

public class PasswordHistory : BaseEntity
{
    public Guid UserId { get; set; }
    public string PasswordHash { get; set; } = string.Empty;

    // Navigation
    public User User { get; set; } = null!;
}
