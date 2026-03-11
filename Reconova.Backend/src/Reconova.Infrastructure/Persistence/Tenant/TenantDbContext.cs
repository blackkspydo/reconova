using Microsoft.EntityFrameworkCore;
using Reconova.Domain.Common;
using Reconova.Domain.Common.Interfaces;
using Reconova.Domain.Entities.Admin;
using Reconova.Domain.Entities.Compliance;
using Reconova.Domain.Entities.Cve;
using Reconova.Domain.Entities.Integrations;
using Reconova.Domain.Entities.Scanning;
using Reconova.Domain.Entities.Workflows;

namespace Reconova.Infrastructure.Persistence.Tenant;

public class TenantDbContext : DbContext, ITenantDbContext
{
    public Guid TenantId { get; }

    public TenantDbContext(DbContextOptions<TenantDbContext> options, Guid tenantId)
        : base(options)
    {
        TenantId = tenantId;
    }

    // Scanning
    public DbSet<Domain.Entities.Scanning.Domain> Domains => Set<Domain.Entities.Scanning.Domain>();
    public DbSet<Subdomain> Subdomains => Set<Subdomain>();
    public DbSet<Port> Ports => Set<Port>();
    public DbSet<Technology> Technologies => Set<Technology>();
    public DbSet<ScanJob> ScanJobs => Set<ScanJob>();
    public DbSet<ScanResult> ScanResults => Set<ScanResult>();
    public DbSet<Vulnerability> Vulnerabilities => Set<Vulnerability>();
    public DbSet<ScanSchedule> ScanSchedules => Set<ScanSchedule>();
    public DbSet<Screenshot> Screenshots => Set<Screenshot>();

    // Workflows
    public DbSet<WorkflowTemplate> WorkflowTemplates => Set<WorkflowTemplate>();
    public DbSet<Workflow> Workflows => Set<Workflow>();

    // Compliance
    public DbSet<ComplianceFramework> ComplianceFrameworks => Set<ComplianceFramework>();
    public DbSet<ComplianceControl> ComplianceControls => Set<ComplianceControl>();
    public DbSet<ComplianceAssessment> ComplianceAssessments => Set<ComplianceAssessment>();
    public DbSet<ControlCheckMapping> ControlCheckMappings => Set<ControlCheckMapping>();
    public DbSet<TenantComplianceSelection> TenantComplianceSelections => Set<TenantComplianceSelection>();
    public DbSet<ControlResult> ControlResults => Set<ControlResult>();

    // Integrations
    public DbSet<IntegrationConfig> IntegrationConfigs => Set<IntegrationConfig>();
    public DbSet<NotificationRule> NotificationRules => Set<NotificationRule>();

    // CVE Alerts (tenant-scoped)
    public DbSet<VulnerabilityAlert> VulnerabilityAlerts => Set<VulnerabilityAlert>();
    public DbSet<TenantCveSetting> TenantCveSettings => Set<TenantCveSetting>();

    // Audit (tenant-level)
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(TenantDbContext).Assembly,
            type => type.Namespace?.Contains("Tenant.Configurations") == true);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = DateTime.UtcNow;
            }
        }

        foreach (var entry in ChangeTracker.Entries<ISoftDeletable>())
        {
            if (entry.State == EntityState.Deleted)
            {
                entry.State = EntityState.Modified;
                entry.Entity.IsDeleted = true;
                entry.Entity.DeletedAt = DateTime.UtcNow;
            }
        }

        return await base.SaveChangesAsync(cancellationToken);
    }
}
