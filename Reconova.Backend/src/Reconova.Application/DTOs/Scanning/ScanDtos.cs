using Reconova.Domain.Common.Enums;

namespace Reconova.Application.DTOs.Scanning;

public record DomainDto(
    Guid Id,
    string Name,
    DomainVerificationStatus VerificationStatus,
    string? VerificationMethod,
    DateTime? VerifiedAt,
    DateTime? LastScanAt,
    int SubdomainCount,
    DateTime CreatedAt
);

public record CreateDomainRequest(string Name, string VerificationMethod);

public record DomainVerificationDto(
    Guid DomainId,
    string DomainName,
    string VerificationMethod,
    string VerificationToken,
    string Instructions
);

public record SubdomainDto(
    Guid Id,
    Guid DomainId,
    string Name,
    string? IpAddress,
    bool IsAlive,
    int? HttpStatusCode,
    string? WebServer,
    int PortCount,
    int TechnologyCount,
    DateTime? FirstSeenAt,
    DateTime? LastSeenAt
);

public record PortDto(
    Guid Id,
    int PortNumber,
    string Protocol,
    PortState State,
    string? ServiceName,
    string? ServiceVersion,
    string? Banner
);

public record TechnologyDto(
    Guid Id,
    string Name,
    string? Version,
    string? Category,
    DateTime? FirstSeenAt
);

public record ScanJobDto(
    Guid Id,
    Guid DomainId,
    string DomainName,
    ScanType Type,
    ScanStatus Status,
    int CreditCost,
    DateTime? StartedAt,
    DateTime? CompletedAt,
    int? ResultCount,
    int? VulnerabilityCount,
    double? ProgressPercentage,
    DateTime CreatedAt
);

public record CreateScanRequest(
    Guid DomainId,
    ScanType Type,
    string? Configuration
);

public record VulnerabilityDto(
    Guid Id,
    string Title,
    string? Description,
    SeverityLevel Severity,
    string? CveId,
    double? CvssScore,
    string? AffectedComponent,
    string? Remediation,
    bool IsResolved,
    DateTime? FirstSeenAt,
    DateTime? LastSeenAt,
    DateTime CreatedAt
);

public record ScanListRequest(
    int Page = 1,
    int PageSize = 20,
    Guid? DomainId = null,
    ScanType? Type = null,
    ScanStatus? Status = null
);

public record VulnerabilityListRequest(
    int Page = 1,
    int PageSize = 20,
    SeverityLevel? Severity = null,
    bool? IsResolved = null,
    Guid? DomainId = null,
    string? Search = null
);
