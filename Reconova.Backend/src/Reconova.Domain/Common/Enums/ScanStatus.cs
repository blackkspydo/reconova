namespace Reconova.Domain.Common.Enums;

public enum ScanStatus
{
    Queued,
    Running,
    Completed,
    Partial,
    Failed,
    Cancelled,
    TimedOut
}
