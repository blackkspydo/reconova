using Reconova.Domain.Common.Enums;

namespace Reconova.Application.DTOs.Cve;

public record CveEntryDto(
    Guid Id,
    string CveId,
    string? Description,
    SeverityLevel Severity,
    double? CvssV3Score,
    string? CvssV3Vector,
    DateTime? PublishedDate,
    string? Source
);

public record CveSearchRequest(
    string? Query = null,
    SeverityLevel? Severity = null,
    DateTime? PublishedAfter = null,
    DateTime? PublishedBefore = null,
    int Page = 1,
    int PageSize = 20
);

public record VulnerabilityAlertDto(
    Guid Id,
    string CveId,
    SeverityLevel Severity,
    string MatchedProduct,
    string? MatchedVersion,
    bool IsAcknowledged,
    bool IsResolved,
    DateTime CreatedAt
);

public record AcknowledgeAlertRequest(Guid AlertId);
