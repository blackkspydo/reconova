# Reconova - Product Requirements Document & Design

**Date:** 2026-03-01
**Status:** Approved
**Type:** B2B SaaS Multi-Tenant Reconnaissance & Compliance Platform

## 1. Product Overview

Reconova is a B2B SaaS platform where businesses register, add their target domains, run full reconnaissance and vulnerability scans, and receive compliance readiness reports (SOC 2, NIST). The platform acts as a mediator between tenants and third-party security APIs, abstracting away the complexity of managing multiple tools and API keys.

**Core value proposition:** A business signs up, adds domains, runs scans, and gets actionable compliance reports - without needing to manage security tooling or API integrations.

## 2. Tech Stack

- **Backend:** .NET (ASP.NET Core)
- **Frontend:** SvelteKit
- **Database:** PostgreSQL (database-per-tenant isolation)
- **Cache/Queue:** Redis
- **Payments:** Stripe
- **Recon Tools:** Hybrid (external tools + custom modules)

## 3. Architecture

**Modular Monolith with Scan Worker Separation:**

```
SvelteKit Frontend
        |
.NET API (single process, modular)
├── Control Plane Module (auth, tenants, billing, feature flags, super admin)
├── Tenant Module (domains, scans, results, workflows, compliance, integrations)
        |
Redis Queue
        |
Scan Worker(s) (separate process, horizontally scalable)
├── Internal Modules (subdomain enum, port scan, HTTP probe, etc.)
├── External Tool Adapters (subfinder, amass, nmap, httpx, nuclei)
└── API Integrations (Shodan, SecurityTrails, Censys, VirusTotal, Nuclei templates)
```

Key decisions:
- Single .NET solution with clear module boundaries, not separate deployables initially
- Scan Workers are separate processes to avoid blocking the API
- Workers scale horizontally based on queue depth
- Can evolve into microservices later if needed

## 4. Multi-Tenancy & Database Architecture

### 4.1 Isolation Strategy

**Database-per-tenant.** Each tenant gets their own PostgreSQL database cloned from a template. Strongest isolation for a security product with compliance requirements.

### 4.2 Control Database

Single database for platform management:

```sql
-- Tenant Management
tenants              (id, name, slug, status, plan_id, created_at)
tenant_databases     (tenant_id, connection_string, status, template_version)

-- Migration Tracking
base_migrations      (id, version, name, script_hash, created_at)
tenant_migrations    (id, tenant_id, version, name, type, script_hash,
                      applied_at, rolled_back_at, applied_by, status)
                     -- type: 'base' | 'tenant_specific'
                     -- status: 'applied' | 'rolled_back' | 'failed'
migration_scripts    (id, migration_id, up_script, down_script, checksum)

-- Auth (simplified for V1)
users                (id, tenant_id, email, password_hash, 2fa_secret,
                      2fa_enabled, role, created_at)
                     -- role: 'tenant_owner' | 'super_admin'
super_admins         (id, user_id, permissions)
                     -- seeded directly in DB, no self-registration

-- Billing
subscription_plans   (id, name, stripe_price_id, monthly_credits,
                      max_domains, price_monthly, price_annual,
                      features_json, status)
tenant_subscriptions (tenant_id, plan_id, stripe_subscription_id,
                      stripe_customer_id, status, current_period_start,
                      current_period_end, credits_remaining,
                      credits_used_this_period)
credit_transactions  (id, tenant_id, amount, type, scan_job_id,
                      description, created_at)
                     -- type: 'allotment' | 'consumption' | 'purchase' | 'refund'
credit_packs         (id, name, stripe_price_id, credits, price)
scan_step_pricing    (id, check_type, tier_id, credits_per_domain,
                      description, updated_by, updated_at)

-- Feature Flags
feature_flags        (id, name, type, module, default_enabled, description)
                     -- type: 'subscription' | 'operational'
plan_features        (plan_id, feature_id, enabled)
tenant_feature_overrides (tenant_id, feature_id, enabled, overridden_by, reason)

-- Compliance Frameworks (shared, managed by super admin)
compliance_frameworks    (id, name, version, region, description,
                          is_template, created_by, status)
compliance_controls      (id, framework_id, control_id, title, description,
                          category, min_security_recommendations_json)
plan_compliance_access   (plan_id, framework_id, enabled)
control_check_mappings   (id, control_id, check_type, severity_threshold,
                          pass_condition_json, recommendation_json)

-- CVE Monitoring
cve_database           (id, cve_id, severity, affected_products_json,
                        published_at, description, remediation)
cve_feed_sources       (id, name, url, last_synced_at, enabled)

-- Platform API Keys (super admin managed, shared pool)
platform_api_keys     (id, provider, api_key_encrypted, rate_limit,
                       usage_count, monthly_quota, status, added_by)
api_usage_tracking    (id, api_key_id, tenant_id, scan_job_id,
                       provider, calls_made, timestamp)

-- Audit
audit_logs           (id, tenant_id, user_id, action, resource_type,
                      resource_id, details_json, ip_address, user_agent,
                      timestamp, is_super_admin)
```

### 4.3 Template Database

Schema blueprint cloned for each new tenant:

```sql
-- Domains & Discovery
domains              (id, domain, status, added_by, verified_at)
subdomains           (id, domain_id, subdomain, source, first_seen, last_seen)
ports                (id, subdomain_id, port, protocol, service, banner)
technologies         (id, subdomain_id, tech_name, version, category)
screenshots          (id, subdomain_id, url, storage_path, taken_at)

-- Scanning
scan_jobs            (id, domain_id, workflow_id, status, started_at, completed_at)
scan_results         (id, scan_job_id, check_type, target, data_json, severity)
vulnerabilities      (id, scan_result_id, cve, severity, description, remediation)
scan_schedules       (id, domain_id, workflow_id, cron_expression, enabled)

-- Workflows
workflows            (id, name, template_id, steps_json, created_by)
workflow_templates   (id, name, description, steps_json, is_system)

-- Compliance (per-tenant results)
tenant_compliance_selections (id, framework_id, enabled_at)
compliance_assessments       (id, scan_job_id, framework_id, generated_at, overall_score)
control_results              (id, assessment_id, control_id, status,
                               evidence_json, findings_count, recommendations)

-- CVE Alerts
vulnerability_alerts  (id, domain_id, cve_id, affected_tech,
                       severity, status, detected_at, acknowledged_at)
                      -- status: 'new' | 'acknowledged' | 'resolved' | 'false_positive'

-- Notifications
integration_configs   (id, type, provider, config_json, enabled, created_at)
notification_rules    (id, integration_id, event_type, severity_filter, enabled)
notification_history  (id, rule_id, event_type, payload_json, status, sent_at, error)
```

### 4.4 Provisioning Flow

1. User signs up via self-service, creates account in `control_db.users`
2. User creates tenant, inserts into `control_db.tenants`
3. System clones `template_db` to `tenant_{slug}_db`
4. Records `template_version` in `tenant_databases`
5. Applies all `base_migrations` up to current version
6. Records each migration in `tenant_migrations` (type: `base`)
7. Tenant is live

### 4.5 Migration Strategy

**Base migrations:** Applied to all tenants when the template evolves. Tracked per-tenant in `tenant_migrations`.

**Tenant-specific migrations:** Applied only to one tenant's DB. Up + down scripts stored in `migration_scripts`. Rollback affects only that tenant.

**Base template upgrade:** New base migration added, system iterates all tenants, applies migration, records in `tenant_migrations`. If conflict with tenant-specific migration detected, flags for manual review.

### 4.6 Tenant Resolution

Request arrives with tenant context (subdomain, header, or JWT claim). Middleware resolves tenant from control DB (cached in Redis). EF Core DbContext created with tenant's connection string. All queries within request scope target correct tenant DB.

## 5. Authentication

### V1 (Current)

- Email/password signup with mandatory 2FA (TOTP authenticator app)
- JWT-based sessions with refresh token rotation
- JWT claims: `user_id`, `tenant_id`, `role`
- 2FA enrollment required on first login - no access without it
- Single user per tenant (tenant_owner role)
- Super admins seeded directly in DB, not self-registerable

### Future

- Multi-user per tenant with role + permission model
- Roles: tenant_admin, manager, analyst, viewer
- Module-level permissions (e.g., `vulnerability_scanning:execute`)
- Custom roles created by tenant admins
- SSO for enterprise tenants

## 6. Scan Engine & Workflow System

### 6.1 Scan Pipeline

```
1. Tenant adds target domain
2. Tenant selects workflow (template or custom)
3. System validates credits, creates scan_job, deducts credits
4. Scan job pushed to Redis queue
5. Scan Worker picks up job
6. Worker executes workflow steps in sequence:
   - Subdomain enumeration (subfinder, amass, API sources)
   - DNS resolution & filtering
   - Port scanning (nmap/masscan)
   - HTTP probing (httpx)
   - Technology detection
   - Screenshot capture
   - Vulnerability scanning (nuclei + API enrichment)
   - Content discovery
7. Results written to tenant DB after each step
8. Notifications sent if configured
9. Compliance mapping runs against results
10. Scan job marked complete
```

### 6.2 Scan Worker Architecture

```
Scan Worker(s)
├── Internal Modules (built-in .NET modules)
│   ├── Subdomain Enumeration
│   ├── Port Scanning
│   ├── HTTP Probing
│   └── Content Discovery
├── External Tool Adapters
│   ├── subfinder, amass
│   ├── nmap, masscan
│   ├── httpx, aquatone
│   └── nuclei (with template management)
└── API Integrations (platform-managed keys)
    ├── Shodan
    ├── SecurityTrails / Censys
    ├── VirusTotal
    ├── Nuclei template feeds
    └── Custom connectors (Enterprise tier)
```

### 6.3 Workflow Templates

| Template | Steps | Use Case |
|----------|-------|----------|
| Quick Recon | Subdomain enum, DNS, HTTP probe | Fast attack surface overview |
| Full Scan | All steps | Comprehensive assessment |
| Web App Scan | HTTP probe, tech detect, vuln scan, content discovery | Web application focus |
| Compliance Check | Vuln scan + compliance mapping | SOC/NIST readiness |
| Continuous Monitor | Subdomain enum, HTTP probe (scheduled) | Ongoing monitoring |

Tenants can duplicate templates, add/remove/reorder steps, configure per-step parameters, and save as custom workflows.

### 6.4 Credit System

- Credits consumed **per-step per-domain**
- Rates configurable by super admin per check type per subscription tier
- Higher tiers get discounted rates
- Example: Pro tier, 3 domains, Full Scan = subdomain(1x3) + port(2x3) + vuln(3x3) = 18 credits
- Credits reset monthly with plan allotment
- Overage requires credit pack purchase or plan upgrade
- All credit changes logged in `credit_transactions`

## 7. Feature Flags & Subscription Tiers

### 7.1 Tiers

| Feature | Starter | Pro | Enterprise |
|---------|---------|-----|------------|
| Domains | 3 | 20 | Unlimited |
| Monthly credits | 100 | 500 | Custom |
| Subdomain enum | Yes | Yes | Yes |
| Port scanning | Yes | Yes | Yes |
| Vuln scanning | No | Yes | Yes |
| Compliance reports | No | Yes | Yes |
| Shodan integration | No | Yes | Yes |
| Custom API connectors | No | No | Yes |
| Custom workflows | No | Yes | Yes |
| Scheduled scans | No | Yes | Yes |
| Notifications | Email | Slack + Email | All |
| Data retention | 30 days | 90 days | 1 year |

### 7.2 Flag Types

**Subscription flags:** Tied to plan, auto-toggled on plan change. Super admin can override per-tenant.

**Operational flags:** Platform-level toggles (maintenance mode, beta features, global API disable). Super admin controlled.

### 7.3 Evaluation Logic

1. Check operational flags (globally disabled blocks everyone)
2. Check `plan_features` for tenant's current plan
3. Check `tenant_feature_overrides` (super admin override wins)
4. Cache result in Redis, invalidate on plan change or override

## 8. Compliance Engine

### 8.1 Framework Management

- Super admin creates/manages compliance frameworks (SOC 2, NIST, etc.)
- Frameworks created from templates or custom-built
- Each framework is tier-gated via `plan_compliance_access`
- Tenants select which frameworks to assess against (from available on their tier)

### 8.2 Compliance Mapping

Scan results are mapped to compliance controls via `control_check_mappings`:

| Scan Finding | Maps To | Result |
|-------------|---------|--------|
| Open port 22, weak SSH | NIST PR.AC-3 | Fail |
| No critical CVEs | SOC 2 CC7.1 | Pass |
| TLS properly configured | NIST PR.DS-2 | Pass |
| Outdated jQuery with XSS | SOC 2 CC6.1 | Fail |

### 8.3 Reports

- PDF/HTML compliance readiness reports
- Per-control status with evidence from scans
- Overall compliance score per framework
- Remediation recommendations for failing controls
- Historical trend (pass rate over time)
- Minimum security recommendations per compliance control (e.g., EU SOC audit baselines)

### 8.4 CVE Monitoring

Background process running continuously:

1. Ingest CVE feeds (NVD, MITRE, vendor advisories)
2. For each new critical/high CVE, check all tenants' detected tech stacks
3. If match found: create alert, send immediate notification, flag domain at risk
4. Tenants manage alerts: acknowledge, resolve, or mark false positive

## 9. Integrations

### 9.1 Data Source Integrations (Platform-Managed)

Reconova acts as a mediator. Super admin manages shared API key pools. Tenants consume data through scans without managing their own API keys. Cost is absorbed into scan credit pricing.

| Provider | Purpose | Tier |
|----------|---------|------|
| Shodan | Passive recon, open ports, services | Pro+ |
| SecurityTrails | DNS history, subdomain discovery | Pro+ |
| Censys | Certificate transparency, hosts | Enterprise |
| VirusTotal | URL/domain reputation, malware | Pro+ |
| Nuclei Templates | Vuln scanning templates | All |
| Custom Connectors | User-defined REST APIs | Enterprise |

### 9.2 Notification Integrations (Tenant-Configured)

| Integration | Triggers |
|-------------|----------|
| Email | Scan complete, CVE alert, credit low, compliance report |
| Slack | Scan complete, critical CVE alert, compliance change |
| Jira | Auto-create tickets for critical/high findings |
| Webhooks | All events (tenant configures endpoint) |
| SIEM (Syslog/CEF) | Security events for enterprise SOC |

## 10. Super Admin Control Plane

**Tenant management:** View/create/suspend/delete tenants, impersonate users, view tenant data, override feature flags, adjust credits.

**Platform configuration:** Manage plans and pricing, configure scan credit costs per tier, manage compliance frameworks, manage workflow templates, configure CVE feeds, set operational flags, manage shared API keys.

**Monitoring:** Active scans across tenants, system health (queue depth, DB connections), credit consumption analytics, tenant growth/churn, cross-tenant audit log viewer.

**Auth:** Seeded in DB, mandatory 2FA, all actions audit-logged with super_admin flag.


**Changelog/Roadmap page:** Tenant-facing dashboard page showing platform updates, compliance framework changes, feature releases, and roadmap items. Super admin publishes entries manually or they are auto-generated by system events (e.g., framework published, framework deprecated, public draft removed). Provides transparency and communication with tenants about platform evolution. Business rules to be defined in §9 (Super Admin & Operations).

## 11. Platform Compliance & Audit

**Data security:**
- All databases encrypted at rest
- TLS 1.2+ for all connections
- Tenant data isolation via database-per-tenant
- API keys encrypted AES-256
- No cross-tenant data leakage by architecture

**Audit logging:**
- All auth events, CRUD operations, billing events, super admin actions, feature flag changes, compliance report generation
- Logs include user, action, resource, IP, timestamp

**Data retention:**
- Audit logs: minimum 1 year
- Scan results: per-tier (30d / 90d / 1y)
- Tenant DB backups: daily, encrypted

**Access controls:**
- 2FA mandatory for all users
- Super admin actions double-logged
- Impersonation creates visible audit trail
- All actions tied to individual users
