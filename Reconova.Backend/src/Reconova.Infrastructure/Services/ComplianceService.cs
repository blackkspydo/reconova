using Microsoft.EntityFrameworkCore;
using Reconova.Application.Common.Interfaces;
using Reconova.Application.Common.Models;
using Reconova.Application.DTOs.Compliance;
using Reconova.Domain.Common.Enums;
using Reconova.Domain.Common.Exceptions;
using Reconova.Domain.Common.Interfaces;
using Reconova.Domain.Entities.Compliance;

namespace Reconova.Infrastructure.Services;

public class ComplianceService : IComplianceService
{
    private readonly ITenantDbContextFactory _tenantDbFactory;
    private readonly ICurrentUserService _currentUser;

    public ComplianceService(ITenantDbContextFactory tenantDbFactory, ICurrentUserService currentUser)
    {
        _tenantDbFactory = tenantDbFactory;
        _currentUser = currentUser;
    }

    private async Task<(DbContext Db, ITenantDbContext TenantDb)> GetDbAsync(CancellationToken ct)
    {
        var tenantId = _currentUser.TenantId ?? throw new BusinessRuleException("NO_TENANT", "No tenant context");
        var tenantDb = await _tenantDbFactory.CreateAsync(tenantId, ct);
        return ((DbContext)tenantDb, tenantDb);
    }

    public async Task<IReadOnlyList<ComplianceFrameworkDto>> GetFrameworksAsync(CancellationToken cancellationToken)
    {
        var (db, _) = await GetDbAsync(cancellationToken);
        await using var __ = db;
        var frameworks = await db.Set<ComplianceFramework>()
            .Where(f => f.Status == FrameworkStatus.Active)
            .OrderBy(f => f.Name)
            .ToListAsync(cancellationToken);

        return frameworks.Select(f => new ComplianceFrameworkDto(
            f.Id, f.Name, f.Code, f.Description, f.Version,
            f.Status, f.IsSystemFramework, f.ControlCount, f.PublishedAt, f.CreatedAt
        )).ToList();
    }

    public async Task<ComplianceFrameworkDto> GetFrameworkByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var (db, _) = await GetDbAsync(cancellationToken);
        await using var __ = db;
        var f = await db.Set<ComplianceFramework>()
            .Include(f => f.Controls)
            .FirstOrDefaultAsync(f => f.Id == id, cancellationToken)
            ?? throw new NotFoundException("ComplianceFramework", id);

        return new ComplianceFrameworkDto(
            f.Id, f.Name, f.Code, f.Description, f.Version,
            f.Status, f.IsSystemFramework, f.ControlCount, f.PublishedAt, f.CreatedAt
        );
    }

    public async Task<IReadOnlyList<ComplianceControlDto>> GetControlsAsync(Guid frameworkId, CancellationToken cancellationToken)
    {
        var (db, _) = await GetDbAsync(cancellationToken);
        await using var __ = db;
        var controls = await db.Set<ComplianceControl>()
            .Where(c => c.FrameworkId == frameworkId)
            .OrderBy(c => c.SortOrder)
            .ToListAsync(cancellationToken);

        return controls.Select(c => new ComplianceControlDto(
            c.Id, c.ControlId, c.Title, c.Description,
            c.Category, c.IsAutomatable, c.SortOrder
        )).ToList();
    }

    public async Task<ComplianceAssessmentDto> RunAssessmentAsync(RunAssessmentRequest request, CancellationToken cancellationToken)
    {
        var tenantId = _currentUser.TenantId ?? throw new BusinessRuleException("NO_TENANT", "No tenant context");
        var (db, tenantDb) = await GetDbAsync(cancellationToken);
        await using var __ = db;

        var framework = await db.Set<ComplianceFramework>()
            .Include(f => f.Controls)
            .FirstOrDefaultAsync(f => f.Id == request.FrameworkId, cancellationToken)
            ?? throw new NotFoundException("ComplianceFramework", request.FrameworkId);

        var domain = await db.Set<Domain.Entities.Scanning.Domain>()
            .FirstOrDefaultAsync(d => d.Id == request.DomainId, cancellationToken)
            ?? throw new NotFoundException("Domain", request.DomainId);

        var assessment = new ComplianceAssessment
        {
            TenantId = tenantId,
            FrameworkId = framework.Id,
            DomainId = request.DomainId,
            OverallStatus = ComplianceStatus.InProgress,
            TotalControls = framework.Controls.Count,
            AssessedAt = DateTime.UtcNow,
            AssessedByUserId = _currentUser.UserId
        };

        db.Set<ComplianceAssessment>().Add(assessment);

        foreach (var control in framework.Controls)
        {
            db.Set<ControlResult>().Add(new ControlResult
            {
                AssessmentId = assessment.Id,
                ControlId = control.Id,
                Status = ControlResultStatus.NotAssessed
            });
        }

        await tenantDb.SaveChangesAsync(cancellationToken);

        return new ComplianceAssessmentDto(assessment.Id, assessment.FrameworkId, framework.Name,
            assessment.DomainId, domain.Name, assessment.OverallStatus, assessment.ComplianceScore,
            assessment.TotalControls, assessment.PassedControls, assessment.FailedControls,
            assessment.NotAssessedControls, assessment.AssessedAt, assessment.CreatedAt);
    }

    public async Task<ComplianceAssessmentDto> GetAssessmentByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var (db, _) = await GetDbAsync(cancellationToken);
        await using var __ = db;
        var assessment = await db.Set<ComplianceAssessment>()
            .Include(a => a.Framework)
            .Include(a => a.Domain)
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken)
            ?? throw new NotFoundException("ComplianceAssessment", id);

        return MapAssessmentToDto(assessment);
    }

    public async Task<PagedResult<ComplianceAssessmentDto>> GetAssessmentsAsync(AssessmentListRequest request, CancellationToken cancellationToken)
    {
        var (db, _) = await GetDbAsync(cancellationToken);
        await using var __ = db;
        var query = db.Set<ComplianceAssessment>().Include(a => a.Framework).Include(a => a.Domain).AsQueryable();

        if (request.FrameworkId.HasValue)
            query = query.Where(a => a.FrameworkId == request.FrameworkId.Value);
        if (request.DomainId.HasValue)
            query = query.Where(a => a.DomainId == request.DomainId.Value);
        if (request.Status.HasValue)
            query = query.Where(a => a.OverallStatus == request.Status.Value);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(a => a.AssessedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<ComplianceAssessmentDto> { Items = items.Select(MapAssessmentToDto).ToList(), TotalCount = total, Page = request.Page, PageSize = request.PageSize };
    }

    private static ComplianceAssessmentDto MapAssessmentToDto(ComplianceAssessment a) => new(
        a.Id, a.FrameworkId, a.Framework?.Name ?? "", a.DomainId, a.Domain?.Name ?? "",
        a.OverallStatus, a.ComplianceScore,
        a.TotalControls, a.PassedControls, a.FailedControls, a.NotAssessedControls,
        a.AssessedAt, a.CreatedAt
    );
}
