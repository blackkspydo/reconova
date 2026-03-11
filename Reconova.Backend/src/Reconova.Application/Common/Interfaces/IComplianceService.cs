using Reconova.Application.Common.Models;
using Reconova.Application.DTOs.Compliance;

namespace Reconova.Application.Common.Interfaces;

public interface IComplianceService
{
    Task<IReadOnlyList<ComplianceFrameworkDto>> GetFrameworksAsync(CancellationToken cancellationToken = default);
    Task<ComplianceFrameworkDto> GetFrameworkByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ComplianceControlDto>> GetControlsAsync(Guid frameworkId, CancellationToken cancellationToken = default);
    Task<ComplianceAssessmentDto> RunAssessmentAsync(RunAssessmentRequest request, CancellationToken cancellationToken = default);
    Task<ComplianceAssessmentDto> GetAssessmentByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PagedResult<ComplianceAssessmentDto>> GetAssessmentsAsync(AssessmentListRequest request, CancellationToken cancellationToken = default);
}
