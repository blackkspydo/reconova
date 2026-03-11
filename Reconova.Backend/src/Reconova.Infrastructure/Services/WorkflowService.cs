using Microsoft.EntityFrameworkCore;
using Reconova.Application.Common.Interfaces;
using Reconova.Application.Common.Models;
using Reconova.Application.DTOs.Workflows;
using Reconova.Domain.Common.Enums;
using Reconova.Domain.Common.Exceptions;
using Reconova.Domain.Common.Interfaces;
using Reconova.Domain.Entities.Workflows;

namespace Reconova.Infrastructure.Services;

public class WorkflowService : IWorkflowService
{
    private readonly ITenantDbContextFactory _tenantDbFactory;
    private readonly ICurrentUserService _currentUser;

    public WorkflowService(ITenantDbContextFactory tenantDbFactory, ICurrentUserService currentUser)
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

    public async Task<IReadOnlyList<WorkflowTemplateDto>> GetTemplatesAsync(CancellationToken cancellationToken)
    {
        var (db, _) = await GetDbAsync(cancellationToken);
        await using var __ = db;
        var templates = await db.Set<WorkflowTemplate>()
            .Where(t => t.IsActive)
            .OrderBy(t => t.Name)
            .ToListAsync(cancellationToken);

        return templates.Select(MapTemplateToDto).ToList();
    }

    public async Task<WorkflowTemplateDto> GetTemplateByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var (db, _) = await GetDbAsync(cancellationToken);
        await using var __ = db;
        var template = await db.Set<WorkflowTemplate>().FirstOrDefaultAsync(t => t.Id == id, cancellationToken)
            ?? throw new NotFoundException("WorkflowTemplate", id);
        return MapTemplateToDto(template);
    }

    public async Task<WorkflowTemplateDto> CreateTemplateAsync(CreateWorkflowTemplateRequest request, CancellationToken cancellationToken)
    {
        var tenantId = _currentUser.TenantId ?? throw new BusinessRuleException("NO_TENANT", "No tenant context");
        var (db, tenantDb) = await GetDbAsync(cancellationToken);
        await using var __ = db;

        var template = new WorkflowTemplate
        {
            TenantId = tenantId,
            Name = request.Name,
            Description = request.Description,
            Steps = request.Steps,
            CreatedByUserId = _currentUser.UserId ?? Guid.Empty,
            IsSystemTemplate = false
        };

        db.Set<WorkflowTemplate>().Add(template);
        await tenantDb.SaveChangesAsync(cancellationToken);
        return MapTemplateToDto(template);
    }

    public async Task<WorkflowTemplateDto> UpdateTemplateAsync(Guid id, UpdateWorkflowTemplateRequest request, CancellationToken cancellationToken)
    {
        var (db, tenantDb) = await GetDbAsync(cancellationToken);
        await using var __ = db;
        var template = await db.Set<WorkflowTemplate>().FirstOrDefaultAsync(t => t.Id == id, cancellationToken)
            ?? throw new NotFoundException("WorkflowTemplate", id);

        if (template.IsSystemTemplate)
            throw new ForbiddenException("SYSTEM_TEMPLATE", "Cannot modify system templates");

        if (request.Name != null) template.Name = request.Name;
        if (request.Description != null) template.Description = request.Description;
        if (request.Steps != null) template.Steps = request.Steps;
        if (request.IsActive.HasValue) template.IsActive = request.IsActive.Value;

        template.Version++;
        await tenantDb.SaveChangesAsync(cancellationToken);
        return MapTemplateToDto(template);
    }

    public async Task DeleteTemplateAsync(Guid id, CancellationToken cancellationToken)
    {
        var (db, tenantDb) = await GetDbAsync(cancellationToken);
        await using var __ = db;
        var template = await db.Set<WorkflowTemplate>().FirstOrDefaultAsync(t => t.Id == id, cancellationToken)
            ?? throw new NotFoundException("WorkflowTemplate", id);

        if (template.IsSystemTemplate)
            throw new ForbiddenException("SYSTEM_TEMPLATE", "Cannot delete system templates");

        template.IsActive = false;
        await tenantDb.SaveChangesAsync(cancellationToken);
    }

    public async Task<WorkflowDto> ExecuteWorkflowAsync(ExecuteWorkflowRequest request, CancellationToken cancellationToken)
    {
        var tenantId = _currentUser.TenantId ?? throw new BusinessRuleException("NO_TENANT", "No tenant context");
        var (db, tenantDb) = await GetDbAsync(cancellationToken);
        await using var __ = db;

        var template = await db.Set<WorkflowTemplate>().FirstOrDefaultAsync(t => t.Id == request.TemplateId, cancellationToken)
            ?? throw new NotFoundException("WorkflowTemplate", request.TemplateId);

        var domain = await db.Set<Domain.Entities.Scanning.Domain>().FirstOrDefaultAsync(d => d.Id == request.DomainId, cancellationToken)
            ?? throw new NotFoundException("Domain", request.DomainId);

        var workflow = new Workflow
        {
            TenantId = tenantId,
            TemplateId = template.Id,
            DomainId = request.DomainId,
            Status = WorkflowStatus.Active,
            TotalSteps = 0,
            InitiatedByUserId = _currentUser.UserId ?? Guid.Empty,
            StartedAt = DateTime.UtcNow
        };

        db.Set<Workflow>().Add(workflow);
        await tenantDb.SaveChangesAsync(cancellationToken);

        return new WorkflowDto(workflow.Id, workflow.TemplateId, template.Name, workflow.DomainId, domain.Name,
            workflow.Status, workflow.CurrentStep, workflow.TotalSteps,
            workflow.StartedAt, workflow.CompletedAt, workflow.ErrorMessage, workflow.CreatedAt);
    }

    public async Task<WorkflowDto> GetWorkflowByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var (db, _) = await GetDbAsync(cancellationToken);
        await using var __ = db;
        var workflow = await db.Set<Workflow>()
            .Include(w => w.Template)
            .Include(w => w.Domain)
            .FirstOrDefaultAsync(w => w.Id == id, cancellationToken)
            ?? throw new NotFoundException("Workflow", id);
        return MapWorkflowToDto(workflow);
    }

    public async Task<PagedResult<WorkflowDto>> GetWorkflowsAsync(int page, int pageSize, CancellationToken cancellationToken)
    {
        var (db, _) = await GetDbAsync(cancellationToken);
        await using var __ = db;
        var query = db.Set<Workflow>().Include(w => w.Template).Include(w => w.Domain).OrderByDescending(w => w.CreatedAt);

        var total = await query.CountAsync(cancellationToken);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);

        return new PagedResult<WorkflowDto> { Items = items.Select(MapWorkflowToDto).ToList(), TotalCount = total, Page = page, PageSize = pageSize };
    }

    public async Task CancelWorkflowAsync(Guid id, CancellationToken cancellationToken)
    {
        var (db, tenantDb) = await GetDbAsync(cancellationToken);
        await using var __ = db;
        var workflow = await db.Set<Workflow>().FirstOrDefaultAsync(w => w.Id == id, cancellationToken)
            ?? throw new NotFoundException("Workflow", id);

        if (workflow.Status != WorkflowStatus.Active && workflow.Status != WorkflowStatus.Running)
            throw new BusinessRuleException("INVALID_STATUS", "Can only cancel active or running workflows");

        workflow.Status = WorkflowStatus.Cancelled;
        workflow.CompletedAt = DateTime.UtcNow;
        await tenantDb.SaveChangesAsync(cancellationToken);
    }

    private static WorkflowTemplateDto MapTemplateToDto(WorkflowTemplate t) => new(
        t.Id, t.Name, t.Description, t.Steps, t.IsActive, t.IsSystemTemplate, t.Version, t.TotalCreditCost, t.CreatedAt
    );

    private static WorkflowDto MapWorkflowToDto(Workflow w) => new(
        w.Id, w.TemplateId, w.Template?.Name ?? "", w.DomainId, w.Domain?.Name ?? "",
        w.Status, w.CurrentStep, w.TotalSteps,
        w.StartedAt, w.CompletedAt, w.ErrorMessage, w.CreatedAt
    );
}
