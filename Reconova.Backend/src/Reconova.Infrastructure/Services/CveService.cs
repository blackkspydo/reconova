using Microsoft.EntityFrameworkCore;
using Reconova.Application.Common.Interfaces;
using Reconova.Application.Common.Models;
using Reconova.Application.DTOs.Cve;
using Reconova.Domain.Common.Enums;
using Reconova.Domain.Common.Exceptions;
using Reconova.Domain.Common.Interfaces;
using Reconova.Domain.Entities.Cve;
using Reconova.Infrastructure.Persistence.Control;

namespace Reconova.Infrastructure.Services;

public class CveService : ICveService
{
    private readonly ControlDbContext _controlDb;
    private readonly ITenantDbContextFactory _tenantDbFactory;
    private readonly ICurrentUserService _currentUser;

    public CveService(ControlDbContext controlDb, ITenantDbContextFactory tenantDbFactory, ICurrentUserService currentUser)
    {
        _controlDb = controlDb;
        _tenantDbFactory = tenantDbFactory;
        _currentUser = currentUser;
    }

    public async Task<PagedResult<CveEntryDto>> SearchAsync(CveSearchRequest request, CancellationToken cancellationToken)
    {
        var query = _controlDb.CveEntries.AsQueryable();

        if (!string.IsNullOrEmpty(request.Query))
            query = query.Where(c => c.CveId.Contains(request.Query) || (c.Description != null && c.Description.Contains(request.Query)));

        if (request.Severity.HasValue)
            query = query.Where(c => c.Severity == request.Severity.Value);

        if (request.PublishedAfter.HasValue)
            query = query.Where(c => c.PublishedDate >= request.PublishedAfter.Value);

        if (request.PublishedBefore.HasValue)
            query = query.Where(c => c.PublishedDate <= request.PublishedBefore.Value);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(c => c.PublishedDate)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(c => new CveEntryDto(
                c.Id, c.CveId, c.Description, c.Severity,
                c.CvssV3Score, c.CvssV3Vector, c.PublishedDate, c.Source
            ))
            .ToListAsync(cancellationToken);

        return new PagedResult<CveEntryDto> { Items = items, TotalCount = total, Page = request.Page, PageSize = request.PageSize };
    }

    public async Task<CveEntryDto> GetByIdAsync(string cveId, CancellationToken cancellationToken)
    {
        var entry = await _controlDb.CveEntries
            .FirstOrDefaultAsync(c => c.CveId == cveId, cancellationToken)
            ?? throw new NotFoundException("CveEntry", cveId);

        return new CveEntryDto(
            entry.Id, entry.CveId, entry.Description, entry.Severity,
            entry.CvssV3Score, entry.CvssV3Vector, entry.PublishedDate, entry.Source
        );
    }

    public async Task<PagedResult<VulnerabilityAlertDto>> GetAlertsAsync(int page, int pageSize, CancellationToken cancellationToken)
    {
        var tenantId = _currentUser.TenantId ?? throw new BusinessRuleException("NO_TENANT", "No tenant context");
        var tenantContext = await _tenantDbFactory.CreateAsync(tenantId, cancellationToken);
        await using var _ = (IAsyncDisposable)tenantContext;
        var tenantDb = (DbContext)tenantContext;

        var query = tenantDb.Set<VulnerabilityAlert>()
            .Where(a => a.TenantId == tenantId)
            .OrderByDescending(a => a.CreatedAt);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new VulnerabilityAlertDto(
                a.Id, a.CveId, a.Severity, a.MatchedProduct, a.MatchedVersion,
                a.IsAcknowledged, a.IsResolved, a.CreatedAt
            ))
            .ToListAsync(cancellationToken);

        return new PagedResult<VulnerabilityAlertDto> { Items = items, TotalCount = total, Page = page, PageSize = pageSize };
    }

    public async Task AcknowledgeAlertAsync(Guid alertId, CancellationToken cancellationToken)
    {
        var tenantId = _currentUser.TenantId ?? throw new BusinessRuleException("NO_TENANT", "No tenant context");
        var tenantDb = await _tenantDbFactory.CreateAsync(tenantId, cancellationToken);
        await using var _ = (IAsyncDisposable)tenantDb;
        var db = (DbContext)tenantDb;

        var alert = await db.Set<VulnerabilityAlert>().FirstOrDefaultAsync(a => a.Id == alertId, cancellationToken)
            ?? throw new NotFoundException("VulnerabilityAlert", alertId);

        alert.IsAcknowledged = true;
        alert.AcknowledgedAt = DateTime.UtcNow;
        alert.AcknowledgedByUserId = _currentUser.UserId;

        await tenantDb.SaveChangesAsync(cancellationToken);
    }

    public async Task ResolveAlertAsync(Guid alertId, CancellationToken cancellationToken)
    {
        var tenantId = _currentUser.TenantId ?? throw new BusinessRuleException("NO_TENANT", "No tenant context");
        var tenantDb = await _tenantDbFactory.CreateAsync(tenantId, cancellationToken);
        await using var _ = (IAsyncDisposable)tenantDb;
        var db = (DbContext)tenantDb;

        var alert = await db.Set<VulnerabilityAlert>().FirstOrDefaultAsync(a => a.Id == alertId, cancellationToken)
            ?? throw new NotFoundException("VulnerabilityAlert", alertId);

        alert.IsResolved = true;
        alert.ResolvedAt = DateTime.UtcNow;

        await tenantDb.SaveChangesAsync(cancellationToken);
    }
}
