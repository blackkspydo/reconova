using Reconova.Application.Common.Models;
using Reconova.Application.DTOs.Workflows;

namespace Reconova.Application.Common.Interfaces;

public interface IWorkflowService
{
    Task<IReadOnlyList<WorkflowTemplateDto>> GetTemplatesAsync(CancellationToken cancellationToken = default);
    Task<WorkflowTemplateDto> GetTemplateByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<WorkflowTemplateDto> CreateTemplateAsync(CreateWorkflowTemplateRequest request, CancellationToken cancellationToken = default);
    Task<WorkflowTemplateDto> UpdateTemplateAsync(Guid id, UpdateWorkflowTemplateRequest request, CancellationToken cancellationToken = default);
    Task DeleteTemplateAsync(Guid id, CancellationToken cancellationToken = default);
    Task<WorkflowDto> ExecuteWorkflowAsync(ExecuteWorkflowRequest request, CancellationToken cancellationToken = default);
    Task<WorkflowDto> GetWorkflowByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PagedResult<WorkflowDto>> GetWorkflowsAsync(int page, int pageSize, CancellationToken cancellationToken = default);
    Task CancelWorkflowAsync(Guid id, CancellationToken cancellationToken = default);
}
