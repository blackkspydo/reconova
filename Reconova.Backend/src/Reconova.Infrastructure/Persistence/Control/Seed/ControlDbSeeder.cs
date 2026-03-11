using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Reconova.Domain.Common.Enums;
using Reconova.Domain.Entities.Admin;
using Reconova.Domain.Entities.Billing;
using Reconova.Domain.Entities.FeatureFlags;
using Reconova.Domain.Entities.Identity;

namespace Reconova.Infrastructure.Persistence.Control.Seed;

public class ControlDbSeeder
{
    private readonly ControlDbContext _context;
    private readonly ILogger<ControlDbSeeder> _logger;

    public ControlDbSeeder(ControlDbContext context, ILogger<ControlDbSeeder> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task SeedAsync()
    {
        await _context.Database.MigrateAsync();

        if (!await _context.Users.AnyAsync())
        {
            await SeedSuperAdminAsync();
        }

        if (!await _context.SubscriptionPlans.AnyAsync())
        {
            await SeedSubscriptionPlansAsync();
        }

        if (!await _context.CreditPacks.AnyAsync())
        {
            await SeedCreditPacksAsync();
        }

        if (!await _context.SystemConfigs.AnyAsync())
        {
            await SeedSystemConfigsAsync();
        }

        if (!await _context.FeatureFlags.AnyAsync())
        {
            await SeedFeatureFlagsAsync();
        }

        if (!await _context.ApiVersions.AnyAsync())
        {
            await SeedApiVersionsAsync();
        }

        await _context.SaveChangesAsync();
        _logger.LogInformation("Database seeding completed");
    }

    private async Task SeedSuperAdminAsync()
    {
        var admin = new User
        {
            Email = "admin@reconova.io",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123456"),
            FirstName = "System",
            LastName = "Administrator",
            Role = UserRole.SuperAdmin,
            Status = UserStatus.Active,
            EmailVerified = true
        };
        await _context.Users.AddAsync(admin);
        _logger.LogInformation("Seeded super admin user: admin@reconova.io");
    }

    private async Task SeedSubscriptionPlansAsync()
    {
        var plans = new List<SubscriptionPlan>
        {
            new()
            {
                Name = "Starter",
                Tier = SubscriptionTier.Starter,
                MonthlyPrice = 49m,
                AnnualPrice = 470m,
                MonthlyCredits = 100,
                MaxUsers = 5,
                MaxDomains = 10,
                MaxScansPerDay = 10
            },
            new()
            {
                Name = "Pro",
                Tier = SubscriptionTier.Pro,
                MonthlyPrice = 149m,
                AnnualPrice = 1430m,
                MonthlyCredits = 500,
                MaxUsers = 25,
                MaxDomains = 50,
                MaxScansPerDay = 50
            },
            new()
            {
                Name = "Enterprise",
                Tier = SubscriptionTier.Enterprise,
                MonthlyPrice = 499m,
                AnnualPrice = 4790m,
                MonthlyCredits = 2000,
                MaxUsers = 100,
                MaxDomains = 200,
                MaxScansPerDay = 200
            }
        };
        await _context.SubscriptionPlans.AddRangeAsync(plans);
        _logger.LogInformation("Seeded {Count} subscription plans", plans.Count);
    }

    private async Task SeedCreditPacksAsync()
    {
        var packs = new List<CreditPack>
        {
            new() { Name = "Small Pack", Credits = 50, Price = 19.99m, Description = "50 scan credits" },
            new() { Name = "Medium Pack", Credits = 200, Price = 59.99m, Description = "200 scan credits" },
            new() { Name = "Large Pack", Credits = 500, Price = 119.99m, Description = "500 scan credits" },
            new() { Name = "Enterprise Pack", Credits = 2000, Price = 399.99m, Description = "2000 scan credits" }
        };
        await _context.CreditPacks.AddRangeAsync(packs);
        _logger.LogInformation("Seeded {Count} credit packs", packs.Count);
    }

    private async Task SeedSystemConfigsAsync()
    {
        var configs = new List<SystemConfig>
        {
            // Authentication
            new() { Key = "auth.jwt.access_token_ttl_minutes", Value = "15", DefaultValue = "15", DataType = ConfigDataType.Integer, Description = "JWT access token TTL in minutes", Category = "Authentication", MinValue = "5", MaxValue = "60", Unit = "minutes" },
            new() { Key = "auth.jwt.refresh_token_ttl_days", Value = "7", DefaultValue = "7", DataType = ConfigDataType.Integer, Description = "Refresh token TTL in days", Category = "Authentication", MinValue = "1", MaxValue = "30", Unit = "days" },
            new() { Key = "auth.lockout.max_failed_attempts", Value = "5", DefaultValue = "5", DataType = ConfigDataType.Integer, Description = "Max failed login attempts before lockout", Category = "Authentication", MinValue = "3", MaxValue = "10" },
            new() { Key = "auth.lockout.duration_minutes", Value = "30", DefaultValue = "30", DataType = ConfigDataType.Integer, Description = "Account lockout duration", Category = "Authentication", MinValue = "5", MaxValue = "1440", Unit = "minutes" },
            new() { Key = "auth.password.min_length", Value = "8", DefaultValue = "8", DataType = ConfigDataType.Integer, Description = "Minimum password length", Category = "Authentication", MinValue = "8", MaxValue = "32" },
            new() { Key = "auth.password.require_uppercase", Value = "true", DefaultValue = "true", DataType = ConfigDataType.Boolean, Description = "Require uppercase in passwords", Category = "Authentication" },
            new() { Key = "auth.password.require_lowercase", Value = "true", DefaultValue = "true", DataType = ConfigDataType.Boolean, Description = "Require lowercase in passwords", Category = "Authentication" },
            new() { Key = "auth.password.require_digit", Value = "true", DefaultValue = "true", DataType = ConfigDataType.Boolean, Description = "Require digit in passwords", Category = "Authentication" },
            new() { Key = "auth.password.require_special", Value = "true", DefaultValue = "true", DataType = ConfigDataType.Boolean, Description = "Require special character in passwords", Category = "Authentication" },
            new() { Key = "auth.session.max_concurrent", Value = "5", DefaultValue = "5", DataType = ConfigDataType.Integer, Description = "Max concurrent sessions per user", Category = "Authentication", MinValue = "1", MaxValue = "20" },
            new() { Key = "auth.email_verification.required", Value = "true", DefaultValue = "true", DataType = ConfigDataType.Boolean, Description = "Require email verification", Category = "Authentication" },
            new() { Key = "auth.email_verification.token_ttl_hours", Value = "24", DefaultValue = "24", DataType = ConfigDataType.Integer, Description = "Email verification token TTL", Category = "Authentication", MinValue = "1", MaxValue = "72", Unit = "hours" },

            // Tenant Management
            new() { Key = "tenant.max_users_default", Value = "5", DefaultValue = "5", DataType = ConfigDataType.Integer, Description = "Default max users per tenant", Category = "Tenant Management", MinValue = "1", MaxValue = "1000" },
            new() { Key = "tenant.max_domains_default", Value = "10", DefaultValue = "10", DataType = ConfigDataType.Integer, Description = "Default max domains per tenant", Category = "Tenant Management", MinValue = "1", MaxValue = "500" },
            new() { Key = "tenant.suspension.grace_period_days", Value = "30", DefaultValue = "30", DataType = ConfigDataType.Integer, Description = "Grace period before data deletion after suspension", Category = "Tenant Management", IsCritical = true, MinValue = "7", MaxValue = "90", Unit = "days" },
            new() { Key = "tenant.auto_provision_database", Value = "true", DefaultValue = "true", DataType = ConfigDataType.Boolean, Description = "Auto-provision tenant database on creation", Category = "Tenant Management", IsCritical = true },

            // Billing & Credits
            new() { Key = "billing.credits.starter_monthly", Value = "100", DefaultValue = "100", DataType = ConfigDataType.Integer, Description = "Monthly credit allocation for Starter plan", Category = "Billing & Credits", IsCritical = true, MinValue = "10", MaxValue = "10000" },
            new() { Key = "billing.credits.pro_monthly", Value = "500", DefaultValue = "500", DataType = ConfigDataType.Integer, Description = "Monthly credit allocation for Pro plan", Category = "Billing & Credits", IsCritical = true, MinValue = "10", MaxValue = "10000" },
            new() { Key = "billing.credits.enterprise_monthly", Value = "2000", DefaultValue = "2000", DataType = ConfigDataType.Integer, Description = "Monthly credit allocation for Enterprise plan", Category = "Billing & Credits", IsCritical = true, MinValue = "10", MaxValue = "100000" },
            new() { Key = "billing.credits.rollover_enabled", Value = "false", DefaultValue = "false", DataType = ConfigDataType.Boolean, Description = "Allow unused credits to roll over", Category = "Billing & Credits" },
            new() { Key = "billing.credits.low_threshold_percent", Value = "20", DefaultValue = "20", DataType = ConfigDataType.Integer, Description = "Low credit warning threshold (%)", Category = "Billing & Credits", MinValue = "5", MaxValue = "50" },
            new() { Key = "billing.trial_days", Value = "14", DefaultValue = "14", DataType = ConfigDataType.Integer, Description = "Free trial duration in days", Category = "Billing & Credits", MinValue = "7", MaxValue = "90", Unit = "days" },

            // Scanning & Workflows
            new() { Key = "scanning.job.timeout_hours", Value = "4", DefaultValue = "4", DataType = ConfigDataType.Integer, Description = "Scan job timeout", Category = "Scanning & Workflows", MinValue = "1", MaxValue = "24", Unit = "hours" },
            new() { Key = "scanning.job.max_concurrent_per_tenant", Value = "3", DefaultValue = "3", DataType = ConfigDataType.Integer, Description = "Max concurrent scans per tenant", Category = "Scanning & Workflows", MinValue = "1", MaxValue = "10" },
            new() { Key = "scanning.queue.max_depth", Value = "100", DefaultValue = "100", DataType = ConfigDataType.Integer, Description = "Max scan queue depth", Category = "Scanning & Workflows", IsCritical = true, MinValue = "10", MaxValue = "1000" },
            new() { Key = "scanning.results.retention_days", Value = "365", DefaultValue = "365", DataType = ConfigDataType.Integer, Description = "Scan results retention period", Category = "Scanning & Workflows", MinValue = "30", MaxValue = "1825", Unit = "days" },

            // Rate Limiting
            new() { Key = "rate_limit.api.requests_per_minute", Value = "60", DefaultValue = "60", DataType = ConfigDataType.Integer, Description = "API requests per minute per user", Category = "Rate Limiting", MinValue = "10", MaxValue = "1000" },
            new() { Key = "rate_limit.auth.login_per_minute", Value = "5", DefaultValue = "5", DataType = ConfigDataType.Integer, Description = "Login attempts per minute per IP", Category = "Rate Limiting", MinValue = "3", MaxValue = "20" },
            new() { Key = "rate_limit.scan.create_per_hour", Value = "20", DefaultValue = "20", DataType = ConfigDataType.Integer, Description = "Scan creation rate limit per hour", Category = "Rate Limiting", MinValue = "5", MaxValue = "100" },

            // Compliance
            new() { Key = "compliance.framework.default_grace_period_days", Value = "30", DefaultValue = "30", DataType = ConfigDataType.Integer, Description = "Default grace period for compliance remediation", Category = "Compliance", MinValue = "7", MaxValue = "90", Unit = "days" },
            new() { Key = "compliance.assessment.auto_run", Value = "true", DefaultValue = "true", DataType = ConfigDataType.Boolean, Description = "Auto-run compliance assessment after scans", Category = "Compliance" },

            // CVE Monitoring
            new() { Key = "cve.feed.sync_interval_hours", Value = "6", DefaultValue = "6", DataType = ConfigDataType.Integer, Description = "CVE feed sync interval", Category = "CVE Monitoring", MinValue = "1", MaxValue = "24", Unit = "hours" },
            new() { Key = "cve.alert.auto_match", Value = "true", DefaultValue = "true", DataType = ConfigDataType.Boolean, Description = "Auto-match CVEs to detected technologies", Category = "CVE Monitoring" },

            // Integrations
            new() { Key = "integrations.webhook.timeout_seconds", Value = "30", DefaultValue = "30", DataType = ConfigDataType.Integer, Description = "Webhook delivery timeout", Category = "Integrations", MinValue = "5", MaxValue = "120", Unit = "seconds" },
            new() { Key = "integrations.webhook.max_retries", Value = "3", DefaultValue = "3", DataType = ConfigDataType.Integer, Description = "Max webhook delivery retries", Category = "Integrations", MinValue = "0", MaxValue = "10" },
            new() { Key = "integrations.webhook.retry_delay_seconds", Value = "60", DefaultValue = "60", DataType = ConfigDataType.Integer, Description = "Delay between webhook retries", Category = "Integrations", MinValue = "10", MaxValue = "3600", Unit = "seconds" },

            // Platform Operations
            new() { Key = "admin.impersonation.session_ttl_minutes", Value = "30", DefaultValue = "30", DataType = ConfigDataType.Integer, Description = "Impersonation session TTL", Category = "Platform Operations", IsCritical = true, MinValue = "5", MaxValue = "120", Unit = "minutes" },
            new() { Key = "admin.audit.retention_days", Value = "365", DefaultValue = "365", DataType = ConfigDataType.Integer, Description = "Audit log retention period", Category = "Platform Operations", MinValue = "90", MaxValue = "1825", Unit = "days" },

            // API Versioning
            new() { Key = "versioning.current_api_version", Value = "v1", DefaultValue = "v1", DataType = ConfigDataType.String, Description = "Current API version", Category = "API Versioning", IsCritical = true, AllowedValues = "v1,v2" },
            new() { Key = "versioning.sunset_warning_days", Value = "90", DefaultValue = "90", DataType = ConfigDataType.Integer, Description = "Days before sunset to start warnings", Category = "API Versioning", IsCritical = true, MinValue = "30", MaxValue = "365", Unit = "days" },

            // Feature Flags
            new() { Key = "feature_flags.cache.ttl_minutes", Value = "5", DefaultValue = "5", DataType = ConfigDataType.Integer, Description = "Feature flag cache TTL", Category = "Feature Flags", MinValue = "1", MaxValue = "60", Unit = "minutes" },
            new() { Key = "feature_flags.evaluation.log_enabled", Value = "false", DefaultValue = "false", DataType = ConfigDataType.Boolean, Description = "Log feature flag evaluations", Category = "Feature Flags" },
        };

        await _context.SystemConfigs.AddRangeAsync(configs);
        _logger.LogInformation("Seeded {Count} system configs", configs.Count);
    }

    private async Task SeedFeatureFlagsAsync()
    {
        var flags = new List<FeatureFlag>
        {
            // Subscription flags — scanning
            new() { Key = "subdomain_enumeration", Name = "Subdomain Enumeration", Type = FeatureFlagType.PlanBased, Module = "scanning", DefaultEnabled = true, IsEnabled = true, AllowedPlans = "[\"Starter\",\"Pro\",\"Enterprise\"]" },
            new() { Key = "port_scanning", Name = "Port Scanning", Type = FeatureFlagType.PlanBased, Module = "scanning", DefaultEnabled = true, IsEnabled = true, AllowedPlans = "[\"Starter\",\"Pro\",\"Enterprise\"]" },
            new() { Key = "technology_detection", Name = "Technology Detection", Type = FeatureFlagType.PlanBased, Module = "scanning", DefaultEnabled = true, IsEnabled = true, AllowedPlans = "[\"Starter\",\"Pro\",\"Enterprise\"]" },
            new() { Key = "screenshot_capture", Name = "Screenshot Capture", Type = FeatureFlagType.PlanBased, Module = "scanning", DefaultEnabled = true, IsEnabled = true, AllowedPlans = "[\"Starter\",\"Pro\",\"Enterprise\"]" },
            new() { Key = "vulnerability_scanning", Name = "Vulnerability Scanning", Type = FeatureFlagType.PlanBased, Module = "scanning", DefaultEnabled = false, IsEnabled = true, AllowedPlans = "[\"Pro\",\"Enterprise\"]" },

            // Subscription flags — compliance
            new() { Key = "compliance_checks", Name = "Compliance Checks", Type = FeatureFlagType.PlanBased, Module = "compliance", DefaultEnabled = false, IsEnabled = true, AllowedPlans = "[\"Pro\",\"Enterprise\"]" },
            new() { Key = "compliance_reports", Name = "Compliance Reports", Type = FeatureFlagType.PlanBased, Module = "compliance", DefaultEnabled = false, IsEnabled = true, AllowedPlans = "[\"Pro\",\"Enterprise\"]" },

            // Subscription flags — integrations
            new() { Key = "shodan_integration", Name = "Shodan Integration", Type = FeatureFlagType.PlanBased, Module = "integrations", DefaultEnabled = false, IsEnabled = true, AllowedPlans = "[\"Pro\",\"Enterprise\"]" },
            new() { Key = "securitytrails_integration", Name = "SecurityTrails Integration", Type = FeatureFlagType.PlanBased, Module = "integrations", DefaultEnabled = false, IsEnabled = true, AllowedPlans = "[\"Pro\",\"Enterprise\"]" },
            new() { Key = "censys_integration", Name = "Censys Integration", Type = FeatureFlagType.PlanBased, Module = "integrations", DefaultEnabled = false, IsEnabled = true, AllowedPlans = "[\"Enterprise\"]" },
            new() { Key = "custom_api_connectors", Name = "Custom API Connectors", Type = FeatureFlagType.PlanBased, Module = "integrations", DefaultEnabled = false, IsEnabled = true, AllowedPlans = "[\"Enterprise\"]" },

            // Subscription flags — workflows
            new() { Key = "custom_workflows", Name = "Custom Workflows", Type = FeatureFlagType.PlanBased, Module = "workflows", DefaultEnabled = false, IsEnabled = true, AllowedPlans = "[\"Pro\",\"Enterprise\"]" },
            new() { Key = "scheduled_scans", Name = "Scheduled Scans", Type = FeatureFlagType.PlanBased, Module = "scanning", DefaultEnabled = false, IsEnabled = true, AllowedPlans = "[\"Pro\",\"Enterprise\"]" },

            // Subscription flags — notifications
            new() { Key = "notification_slack", Name = "Slack Notifications", Type = FeatureFlagType.PlanBased, Module = "notifications", DefaultEnabled = false, IsEnabled = true, AllowedPlans = "[\"Pro\",\"Enterprise\"]" },
            new() { Key = "notification_jira", Name = "Jira Integration", Type = FeatureFlagType.PlanBased, Module = "notifications", DefaultEnabled = false, IsEnabled = true, AllowedPlans = "[\"Enterprise\"]" },
            new() { Key = "notification_webhook", Name = "Webhook Notifications", Type = FeatureFlagType.PlanBased, Module = "notifications", DefaultEnabled = false, IsEnabled = true, AllowedPlans = "[\"Pro\",\"Enterprise\"]" },
            new() { Key = "notification_siem", Name = "SIEM/Syslog Integration", Type = FeatureFlagType.PlanBased, Module = "notifications", DefaultEnabled = false, IsEnabled = true, AllowedPlans = "[\"Enterprise\"]" },

            // Subscription flags — CVE
            new() { Key = "cve_monitoring", Name = "CVE Monitoring", Type = FeatureFlagType.PlanBased, Module = "cve", DefaultEnabled = false, IsEnabled = true, AllowedPlans = "[\"Pro\",\"Enterprise\"]" },

            // Operational flags
            new() { Key = "maintenance_mode", Name = "Maintenance Mode", Type = FeatureFlagType.Boolean, Module = "platform", DefaultEnabled = false, IsEnabled = false, Description = "When enabled, blocks scan creation platform-wide" },
            new() { Key = "vuln_scanning_global", Name = "Global Vulnerability Scanning", Type = FeatureFlagType.Boolean, Module = "platform", DefaultEnabled = true, IsEnabled = true, Description = "Emergency disable for vulnerability scanning" },
            new() { Key = "compliance_global", Name = "Global Compliance Engine", Type = FeatureFlagType.Boolean, Module = "platform", DefaultEnabled = true, IsEnabled = true, Description = "Emergency disable for compliance features" },
            new() { Key = "cve_monitoring_global", Name = "Global CVE Monitoring", Type = FeatureFlagType.Boolean, Module = "platform", DefaultEnabled = true, IsEnabled = true, Description = "Enable/disable CVE feed monitoring" },
            new() { Key = "api_global", Name = "Global API Availability", Type = FeatureFlagType.Boolean, Module = "platform", DefaultEnabled = true, IsEnabled = true, Description = "Global API kill switch" },
        };

        await _context.FeatureFlags.AddRangeAsync(flags);
        _logger.LogInformation("Seeded {Count} feature flags", flags.Count);
    }

    private async Task SeedApiVersionsAsync()
    {
        var version = new ApiVersion
        {
            Version = "v1",
            Status = ApiVersionStatus.Current
        };
        await _context.ApiVersions.AddAsync(version);
        _logger.LogInformation("Seeded API version v1");
    }
}
