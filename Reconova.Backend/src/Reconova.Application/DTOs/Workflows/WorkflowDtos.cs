using Reconova.Domain.Common.Enums;

namespace Reconova.Application.DTOs.Workflows;

public record WorkflowTemplateDto(
    Guid Id,
    string Name,
    string? Description,
    string Steps,
    bool IsActive,
    bool IsSystemTemplate,
    int Version,
    int TotalCreditCost,
    DateTime CreatedAt
);

public record CreateWorkflowTemplateRequest(
    string Name,
    string? Description,
    string Steps // JSON array
);

public record UpdateWorkflowTemplateRequest(
    string? Name,
    string? Description,
    string? Steps,
    bool? IsActive
);

public record WorkflowDto(
    Guid Id,
    Guid TemplateId,
    string TemplateName,
    Guid DomainId,
    string DomainName,
    WorkflowStatus Status,
    int CurrentStep,
    int TotalSteps,
    DateTime? StartedAt,
    DateTime? CompletedAt,
    string? ErrorMessage,
    DateTime CreatedAt
);

public record ExecuteWorkflowRequest(
    Guid TemplateId,
    Guid DomainId
);
