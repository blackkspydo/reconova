using Reconova.Domain.Common.Enums;

namespace Reconova.Application.DTOs.Compliance;

public record ComplianceFrameworkDto(
    Guid Id,
    string Name,
    string Code,
    string? Description,
    string Version,
    FrameworkStatus Status,
    bool IsSystemFramework,
    int ControlCount,
    DateTime? PublishedAt,
    DateTime CreatedAt
);

public record ComplianceControlDto(
    Guid Id,
    string ControlId,
    string Title,
    string? Description,
    string? Category,
    bool IsAutomatable,
    int SortOrder
);

public record ComplianceAssessmentDto(
    Guid Id,
    Guid FrameworkId,
    string FrameworkName,
    Guid DomainId,
    string DomainName,
    ComplianceStatus OverallStatus,
    double ComplianceScore,
    int TotalControls,
    int PassedControls,
    int FailedControls,
    int NotAssessedControls,
    DateTime? AssessedAt,
    DateTime CreatedAt
);

public record RunAssessmentRequest(
    Guid FrameworkId,
    Guid DomainId
);

public record AssessmentListRequest(
    int Page = 1,
    int PageSize = 20,
    Guid? FrameworkId = null,
    Guid? DomainId = null,
    ComplianceStatus? Status = null
);
