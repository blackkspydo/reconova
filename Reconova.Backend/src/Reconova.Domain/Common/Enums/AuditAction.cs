namespace Reconova.Domain.Common.Enums;

public enum AuditAction
{
    Create,
    Read,
    Update,
    Delete,
    Login,
    Logout,
    FailedLogin,
    PasswordChange,
    PasswordReset,
    RoleChange,
    Export,
    Import,
    Approve,
    Reject,
    Suspend,
    Reactivate,
    Impersonate
}
