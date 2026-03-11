using Microsoft.EntityFrameworkCore;
using Reconova.Domain.Common;
using Reconova.Domain.Common.Interfaces;
using Reconova.Domain.Entities.Admin;
using Reconova.Domain.Entities.Billing;
using Reconova.Domain.Entities.Compliance;
using Reconova.Domain.Entities.Cve;
using Reconova.Domain.Entities.FeatureFlags;
using Reconova.Domain.Entities.Identity;
using Reconova.Domain.Entities.Integrations;

namespace Reconova.Infrastructure.Persistence.Control;

public class ControlDbContext : DbContext, IUnitOfWork
{
    public ControlDbContext(DbContextOptions<ControlDbContext> options) : base(options) { }

    // Identity
    public DbSet<User> Users => Set<User>();
    public DbSet<Session> Sessions => Set<Session>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Domain.Entities.Identity.Tenant> Tenants => Set<Domain.Entities.Identity.Tenant>();
    public DbSet<TenantDatabase> TenantDatabases => Set<TenantDatabase>();
    public DbSet<PasswordHistory> PasswordHistories => Set<PasswordHistory>();

    // Billing
    public DbSet<SubscriptionPlan> SubscriptionPlans => Set<SubscriptionPlan>();
    public DbSet<TenantSubscription> TenantSubscriptions => Set<TenantSubscription>();
    public DbSet<CreditTransaction> CreditTransactions => Set<CreditTransaction>();
    public DbSet<CreditPack> CreditPacks => Set<CreditPack>();
    public DbSet<ScanStepPricing> ScanStepPricings => Set<ScanStepPricing>();

    // Feature Flags
    public DbSet<FeatureFlag> FeatureFlags => Set<FeatureFlag>();
    public DbSet<PlanFeature> PlanFeatures => Set<PlanFeature>();
    public DbSet<TenantFeatureOverride> TenantFeatureOverrides => Set<TenantFeatureOverride>();

    // CVE (global)
    public DbSet<CveEntry> CveEntries => Set<CveEntry>();
    public DbSet<CveFeedSource> CveFeedSources => Set<CveFeedSource>();
    public DbSet<ProductAlias> ProductAliases => Set<ProductAlias>();

    // Compliance (control-level)
    public DbSet<PlanComplianceAccess> PlanComplianceAccesses => Set<PlanComplianceAccess>();

    // Integrations (platform-level)
    public DbSet<PlatformApiKey> PlatformApiKeys => Set<PlatformApiKey>();
    public DbSet<ApiUsageTracking> ApiUsageTrackings => Set<ApiUsageTracking>();

    // Admin
    public DbSet<SystemConfig> SystemConfigs => Set<SystemConfig>();
    public DbSet<ConfigChangeHistory> ConfigChangeHistories => Set<ConfigChangeHistory>();
    public DbSet<ConfigChangeRequest> ConfigChangeRequests => Set<ConfigChangeRequest>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<ApiVersion> ApiVersions => Set<ApiVersion>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(ControlDbContext).Assembly,
            type => type.Namespace?.Contains("Control.Configurations") == true);
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
