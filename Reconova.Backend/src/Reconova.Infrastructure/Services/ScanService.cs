using Microsoft.EntityFrameworkCore;
using Reconova.Application.Common.Interfaces;
using Reconova.Application.Common.Models;
using Reconova.Application.DTOs.Scanning;
using Reconova.Domain.Common.Enums;
using Reconova.Domain.Common.Exceptions;
using Reconova.Domain.Common.Interfaces;
using Reconova.Domain.Entities.Scanning;
using Reconova.Infrastructure.Persistence.Control;

namespace Reconova.Infrastructure.Services;

public class ScanService : IScanService
{
    private readonly ITenantDbContextFactory _tenantDbFactory;
    private readonly ControlDbContext _controlDb;
    private readonly ICurrentUserService _currentUser;

    public ScanService(ITenantDbContextFactory tenantDbFactory, ControlDbContext controlDb, ICurrentUserService currentUser)
    {
        _tenantDbFactory = tenantDbFactory;
        _controlDb = controlDb;
        _currentUser = currentUser;
    }

    private async Task<ITenantDbContext> GetTenantDbAsync(CancellationToken ct)
    {
        var tenantId = _currentUser.TenantId ?? throw new BusinessRuleException("NO_TENANT", "No tenant context");
        return await _tenantDbFactory.CreateAsync(tenantId, ct);
    }

    // --- Domains ---
    public async Task<IReadOnlyList<DomainDto>> GetDomainsAsync(CancellationToken cancellationToken)
    {
        var tenantDb = await GetTenantDbAsync(cancellationToken);
        await using var _ = (IAsyncDisposable)tenantDb;
        var db = (DbContext)tenantDb;
        var domains = await db.Set<Domain.Entities.Scanning.Domain>()
            .Include(d => d.Subdomains)
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync(cancellationToken);

        return domains.Select(d => new DomainDto(
            d.Id, d.Name, d.VerificationStatus, d.VerificationMethod,
            d.VerifiedAt, d.LastScanAt, d.Subdomains.Count, d.CreatedAt
        )).ToList();
    }

    public async Task<DomainDto> GetDomainByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var tenantDb = await GetTenantDbAsync(cancellationToken);
        await using var _ = (IAsyncDisposable)tenantDb;
        var db = (DbContext)tenantDb;
        var domain = await db.Set<Domain.Entities.Scanning.Domain>()
            .Include(d => d.Subdomains)
            .FirstOrDefaultAsync(d => d.Id == id, cancellationToken)
            ?? throw new NotFoundException("Domain", id);

        return new DomainDto(
            domain.Id, domain.Name, domain.VerificationStatus, domain.VerificationMethod,
            domain.VerifiedAt, domain.LastScanAt, domain.Subdomains.Count, domain.CreatedAt
        );
    }

    public async Task<DomainDto> CreateDomainAsync(CreateDomainRequest request, CancellationToken cancellationToken)
    {
        var tenantId = _currentUser.TenantId ?? throw new BusinessRuleException("NO_TENANT", "No tenant context");
        var tenantDb = await GetTenantDbAsync(cancellationToken);
        await using var _ = (IAsyncDisposable)tenantDb;
        var db = (DbContext)tenantDb;

        if (await db.Set<Domain.Entities.Scanning.Domain>().AnyAsync(d => d.Name == request.Name.ToLowerInvariant(), cancellationToken))
            throw new ConflictException("DOMAIN_EXISTS", "Domain already exists");

        var domain = new Domain.Entities.Scanning.Domain
        {
            TenantId = tenantId,
            Name = request.Name.ToLowerInvariant(),
            VerificationStatus = DomainVerificationStatus.Pending,
            VerificationMethod = request.VerificationMethod,
            VerificationToken = Guid.NewGuid().ToString()
        };

        db.Set<Domain.Entities.Scanning.Domain>().Add(domain);
        await tenantDb.SaveChangesAsync(cancellationToken);

        return new DomainDto(domain.Id, domain.Name, domain.VerificationStatus, domain.VerificationMethod,
            domain.VerifiedAt, domain.LastScanAt, 0, domain.CreatedAt);
    }

    public async Task DeleteDomainAsync(Guid id, CancellationToken cancellationToken)
    {
        var tenantDb = await GetTenantDbAsync(cancellationToken);
        await using var _ = (IAsyncDisposable)tenantDb;
        var db = (DbContext)tenantDb;
        var domain = await db.Set<Domain.Entities.Scanning.Domain>()
            .FirstOrDefaultAsync(d => d.Id == id, cancellationToken)
            ?? throw new NotFoundException("Domain", id);

        db.Set<Domain.Entities.Scanning.Domain>().Remove(domain);
        await tenantDb.SaveChangesAsync(cancellationToken);
    }

    public async Task<DomainVerificationDto> InitiateVerificationAsync(Guid domainId, CancellationToken cancellationToken)
    {
        var tenantDb = await GetTenantDbAsync(cancellationToken);
        await using var _ = (IAsyncDisposable)tenantDb;
        var db = (DbContext)tenantDb;
        var domain = await db.Set<Domain.Entities.Scanning.Domain>()
            .FirstOrDefaultAsync(d => d.Id == domainId, cancellationToken)
            ?? throw new NotFoundException("Domain", domainId);

        domain.VerificationToken = Guid.NewGuid().ToString();
        await tenantDb.SaveChangesAsync(cancellationToken);

        return new DomainVerificationDto(domain.Id, domain.Name, domain.VerificationMethod ?? "dns",
            domain.VerificationToken,
            $"Add a TXT record with value: reconova-verify={domain.VerificationToken}");
    }

    public async Task<DomainDto> VerifyDomainAsync(Guid domainId, CancellationToken cancellationToken)
    {
        var tenantDb = await GetTenantDbAsync(cancellationToken);
        await using var _ = (IAsyncDisposable)tenantDb;
        var db = (DbContext)tenantDb;
        var domain = await db.Set<Domain.Entities.Scanning.Domain>()
            .Include(d => d.Subdomains)
            .FirstOrDefaultAsync(d => d.Id == domainId, cancellationToken)
            ?? throw new NotFoundException("Domain", domainId);

        domain.VerificationStatus = DomainVerificationStatus.Verified;
        domain.VerifiedAt = DateTime.UtcNow;
        await tenantDb.SaveChangesAsync(cancellationToken);

        return new DomainDto(domain.Id, domain.Name, domain.VerificationStatus, domain.VerificationMethod,
            domain.VerifiedAt, domain.LastScanAt, domain.Subdomains.Count, domain.CreatedAt);
    }

    // --- Subdomains ---
    public async Task<PagedResult<SubdomainDto>> GetSubdomainsAsync(Guid domainId, int page, int pageSize, CancellationToken cancellationToken)
    {
        var tenantDb = await GetTenantDbAsync(cancellationToken);
        await using var _ = (IAsyncDisposable)tenantDb;
        var db = (DbContext)tenantDb;
        var query = db.Set<Subdomain>()
            .Include(s => s.Ports)
            .Include(s => s.Technologies)
            .Where(s => s.DomainId == domainId);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(s => s.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(s => new SubdomainDto(s.Id, s.DomainId, s.Name, s.IpAddress, s.IsAlive, s.HttpStatusCode, s.WebServer, s.Ports.Count, s.Technologies.Count, s.FirstSeenAt, s.LastSeenAt))
            .ToListAsync(cancellationToken);

        return new PagedResult<SubdomainDto> { Items = items, TotalCount = total, Page = page, PageSize = pageSize };
    }

    public async Task<SubdomainDto> GetSubdomainDetailAsync(Guid subdomainId, CancellationToken cancellationToken)
    {
        var tenantDb = await GetTenantDbAsync(cancellationToken);
        await using var _ = (IAsyncDisposable)tenantDb;
        var db = (DbContext)tenantDb;
        var s = await db.Set<Subdomain>()
            .Include(s => s.Ports)
            .Include(s => s.Technologies)
            .FirstOrDefaultAsync(s => s.Id == subdomainId, cancellationToken)
            ?? throw new NotFoundException("Subdomain", subdomainId);
        return new SubdomainDto(s.Id, s.DomainId, s.Name, s.IpAddress, s.IsAlive, s.HttpStatusCode, s.WebServer, s.Ports.Count, s.Technologies.Count, s.FirstSeenAt, s.LastSeenAt);
    }

    public async Task<IReadOnlyList<PortDto>> GetSubdomainPortsAsync(Guid subdomainId, CancellationToken cancellationToken)
    {
        var tenantDb = await GetTenantDbAsync(cancellationToken);
        await using var _ = (IAsyncDisposable)tenantDb;
        var db = (DbContext)tenantDb;
        return await db.Set<Port>()
            .Where(p => p.SubdomainId == subdomainId)
            .Select(p => new PortDto(p.Id, p.PortNumber, p.Protocol, p.State, p.ServiceName, p.ServiceVersion, p.Banner))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<TechnologyDto>> GetSubdomainTechnologiesAsync(Guid subdomainId, CancellationToken cancellationToken)
    {
        var tenantDb = await GetTenantDbAsync(cancellationToken);
        await using var _ = (IAsyncDisposable)tenantDb;
        var db = (DbContext)tenantDb;
        return await db.Set<Technology>()
            .Where(t => t.SubdomainId == subdomainId)
            .Select(t => new TechnologyDto(t.Id, t.Name, t.Version, t.Category, t.FirstSeenAt))
            .ToListAsync(cancellationToken);
    }

    // --- Scan Jobs ---
    public async Task<ScanJobDto> CreateScanAsync(CreateScanRequest request, CancellationToken cancellationToken)
    {
        var tenantId = _currentUser.TenantId ?? throw new BusinessRuleException("NO_TENANT", "No tenant context");
        var tenantDb = await GetTenantDbAsync(cancellationToken);
        await using var _ = (IAsyncDisposable)tenantDb;
        var db = (DbContext)tenantDb;

        var domain = await db.Set<Domain.Entities.Scanning.Domain>()
            .FirstOrDefaultAsync(d => d.Id == request.DomainId, cancellationToken)
            ?? throw new NotFoundException("Domain", request.DomainId);

        // Determine credit cost based on scan type
        var creditCost = request.Type switch
        {
            ScanType.SubdomainEnumeration => 1,
            ScanType.PortScan => 3,
            ScanType.TechnologyDetection => 2,
            ScanType.VulnerabilityScan => 5,
            ScanType.FullRecon => 10,
            _ => 5
        };

        // Check credits
        var subscription = await _controlDb.TenantSubscriptions
            .FirstOrDefaultAsync(s => s.TenantId == tenantId && s.Status == SubscriptionStatus.Active, cancellationToken)
            ?? throw new BusinessRuleException("NO_SUBSCRIPTION", "No active subscription");

        if (subscription.CreditsRemaining < creditCost)
            throw new BusinessRuleException("INSUFFICIENT_CREDITS", "Insufficient credits");

        var scanJob = new ScanJob
        {
            TenantId = tenantId,
            DomainId = request.DomainId,
            Type = request.Type,
            CreditCost = creditCost,
            TotalCredits = creditCost,
            Configuration = request.Configuration,
            InitiatedByUserId = _currentUser.UserId ?? Guid.Empty
        };

        db.Set<ScanJob>().Add(scanJob);

        // Deduct credits
        var balanceBefore = subscription.CreditsRemaining;
        subscription.CreditsRemaining -= creditCost;
        subscription.CreditsUsedThisPeriod += creditCost;

        _controlDb.CreditTransactions.Add(new Domain.Entities.Billing.CreditTransaction
        {
            TenantId = tenantId,
            Type = CreditTransactionType.ScanDeduction,
            Amount = -creditCost,
            BalanceBefore = balanceBefore,
            BalanceAfter = subscription.CreditsRemaining,
            Description = $"Scan: {domain.Name} ({request.Type})",
            ReferenceType = "ScanJob",
            ReferenceId = scanJob.Id,
            PerformedByUserId = _currentUser.UserId ?? Guid.Empty
        });

        await tenantDb.SaveChangesAsync(cancellationToken);
        await _controlDb.SaveChangesAsync(cancellationToken);

        return new ScanJobDto(scanJob.Id, scanJob.DomainId, domain.Name, scanJob.Type, scanJob.Status,
            scanJob.CreditCost, scanJob.StartedAt, scanJob.CompletedAt,
            scanJob.ResultCount, scanJob.VulnerabilityCount, scanJob.ProgressPercentage, scanJob.CreatedAt);
    }

    public async Task<ScanJobDto> GetScanByIdAsync(Guid scanId, CancellationToken cancellationToken)
    {
        var tenantDb = await GetTenantDbAsync(cancellationToken);
        await using var _ = (IAsyncDisposable)tenantDb;
        var db = (DbContext)tenantDb;
        var scan = await db.Set<ScanJob>()
            .Include(s => s.Domain)
            .FirstOrDefaultAsync(s => s.Id == scanId, cancellationToken)
            ?? throw new NotFoundException("ScanJob", scanId);

        return MapScanJobToDto(scan);
    }

    public async Task<PagedResult<ScanJobDto>> GetScansAsync(ScanListRequest request, CancellationToken cancellationToken)
    {
        var tenantDb = await GetTenantDbAsync(cancellationToken);
        await using var _ = (IAsyncDisposable)tenantDb;
        var db = (DbContext)tenantDb;
        var query = db.Set<ScanJob>().Include(s => s.Domain).AsQueryable();

        if (request.DomainId.HasValue)
            query = query.Where(s => s.DomainId == request.DomainId.Value);
        if (request.Type.HasValue)
            query = query.Where(s => s.Type == request.Type.Value);
        if (request.Status.HasValue)
            query = query.Where(s => s.Status == request.Status.Value);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(s => s.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<ScanJobDto> { Items = items.Select(MapScanJobToDto).ToList(), TotalCount = total, Page = request.Page, PageSize = request.PageSize };
    }

    public async Task CancelScanAsync(Guid scanId, CancellationToken cancellationToken)
    {
        var tenantDb = await GetTenantDbAsync(cancellationToken);
        await using var _ = (IAsyncDisposable)tenantDb;
        var db = (DbContext)tenantDb;
        var scan = await db.Set<ScanJob>().FirstOrDefaultAsync(s => s.Id == scanId, cancellationToken)
            ?? throw new NotFoundException("ScanJob", scanId);

        if (scan.Status != ScanStatus.Queued && scan.Status != ScanStatus.Running)
            throw new BusinessRuleException("INVALID_STATUS", "Can only cancel queued or running scans");

        scan.Status = ScanStatus.Cancelled;
        scan.CompletedAt = DateTime.UtcNow;
        scan.CancelledByUserId = _currentUser.UserId;

        await tenantDb.SaveChangesAsync(cancellationToken);
    }

    // --- Vulnerabilities ---
    public async Task<PagedResult<VulnerabilityDto>> GetVulnerabilitiesAsync(VulnerabilityListRequest request, CancellationToken cancellationToken)
    {
        var tenantDb = await GetTenantDbAsync(cancellationToken);
        await using var _ = (IAsyncDisposable)tenantDb;
        var db = (DbContext)tenantDb;
        var query = db.Set<Vulnerability>().AsQueryable();

        if (request.Severity.HasValue)
            query = query.Where(v => v.Severity == request.Severity.Value);
        if (request.IsResolved.HasValue)
            query = query.Where(v => v.IsResolved == request.IsResolved.Value);
        if (request.DomainId.HasValue)
            query = query.Where(v => v.ScanJob.DomainId == request.DomainId.Value);
        if (!string.IsNullOrEmpty(request.Search))
            query = query.Where(v => v.Title.Contains(request.Search) || (v.CveId != null && v.CveId.Contains(request.Search)));

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(v => v.CvssScore ?? 0)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(v => new VulnerabilityDto(
                v.Id, v.Title, v.Description, v.Severity,
                v.CveId, v.CvssScore, v.AffectedComponent,
                v.Remediation, v.IsResolved, v.FirstSeenAt, v.LastSeenAt, v.CreatedAt
            ))
            .ToListAsync(cancellationToken);

        return new PagedResult<VulnerabilityDto> { Items = items, TotalCount = total, Page = request.Page, PageSize = request.PageSize };
    }

    public async Task<VulnerabilityDto> GetVulnerabilityByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var tenantDb = await GetTenantDbAsync(cancellationToken);
        await using var _ = (IAsyncDisposable)tenantDb;
        var db = (DbContext)tenantDb;
        var v = await db.Set<Vulnerability>().FirstOrDefaultAsync(v => v.Id == id, cancellationToken)
            ?? throw new NotFoundException("Vulnerability", id);

        return new VulnerabilityDto(
            v.Id, v.Title, v.Description, v.Severity,
            v.CveId, v.CvssScore, v.AffectedComponent,
            v.Remediation, v.IsResolved, v.FirstSeenAt, v.LastSeenAt, v.CreatedAt
        );
    }

    public async Task MarkVulnerabilityResolvedAsync(Guid id, CancellationToken cancellationToken)
    {
        var tenantDb = await GetTenantDbAsync(cancellationToken);
        await using var _ = (IAsyncDisposable)tenantDb;
        var db = (DbContext)tenantDb;
        var vuln = await db.Set<Vulnerability>().FirstOrDefaultAsync(v => v.Id == id, cancellationToken)
            ?? throw new NotFoundException("Vulnerability", id);

        vuln.IsResolved = true;
        vuln.ResolvedAt = DateTime.UtcNow;
        await tenantDb.SaveChangesAsync(cancellationToken);
    }

    private static ScanJobDto MapScanJobToDto(ScanJob s) => new(
        s.Id, s.DomainId, s.Domain?.Name ?? "", s.Type, s.Status,
        s.CreditCost, s.StartedAt, s.CompletedAt,
        s.ResultCount, s.VulnerabilityCount, s.ProgressPercentage, s.CreatedAt
    );
}
