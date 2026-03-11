namespace Reconova.Domain.Common.Enums;

public enum NotificationEventType
{
    ScanCompleted,
    ScanFailed,
    VulnerabilityFound,
    CriticalVulnerabilityFound,
    CveAlertMatched,
    ComplianceViolation,
    CreditLow,
    CreditExhausted,
    SubscriptionExpiring,
    DomainVerified,
    DomainVerificationFailed
}
