# Reconova - Business Rules Document

**Date:** 2026-03-01
**Status:** Approved
**Scope:** System behavior rules + operational policies
**Reference:** `docs/plans/2026-03-01-reconova-prd-design.md`

---

## 1. Authentication & Account Security

| Rule ID | Rule | Details |
|---------|------|---------|
| BR-AUTH-001 | Password minimum length | 12 characters minimum |
| BR-AUTH-002 | Password complexity | Must contain: 1 uppercase, 1 lowercase, 1 number, 1 special character |
| BR-AUTH-003 | Password history | Cannot reuse last 5 passwords |
| BR-AUTH-004 | Password rotation | Forced password change every 90 days |
| BR-AUTH-005 | 2FA mandatory | All accounts must enroll TOTP 2FA before accessing any feature. No bypass. |
| BR-AUTH-006 | Login lockout | Account locks after 3 consecutive failed attempts. 1-hour lockout period. |
| BR-AUTH-007 | IP rate limiting | Max 10 login attempts per IP per 15 minutes, regardless of account |
| BR-AUTH-008 | Session timeout | Inactive sessions expire after 30 minutes |
| BR-AUTH-009 | Session invalidation on password change | All existing sessions are invalidated when password is changed |
| BR-AUTH-010 | Concurrent session limit | Max 3 concurrent sessions per user. Oldest session terminated on new login. |
| BR-AUTH-011 | New device verification | Login from unrecognized device/IP requires email verification code |
| BR-AUTH-012 | JWT access token expiry | 15 minutes |
| BR-AUTH-013 | JWT refresh token expiry | 7 days, single-use with rotation |
| BR-AUTH-014 | Super admin registration | Super admins cannot self-register. Must be seeded directly in the database. |
| BR-AUTH-015 | Super admin 2FA | Super admins follow all the same auth rules plus all actions are double-logged in audit |

---

## 2. Tenant Management

| Rule ID | Rule | Details |
|---------|------|---------|
| BR-TNT-001 | Self-service tenant creation | Any user can sign up and create a tenant. No approval needed. |
| BR-TNT-002 | Tenant slug uniqueness | Tenant slug must be globally unique. Generated from name: lowercase, alphanumeric + hyphens only. |
| BR-TNT-003 | One tenant per user (V1) | Each user belongs to exactly one tenant. A user cannot be part of multiple tenants. |
| BR-TNT-004 | Tenant database isolation | Each tenant gets a dedicated PostgreSQL database cloned from the template DB. No shared tables. |
| BR-TNT-005 | Tenant provisioning states | Tenant goes through: `provisioning` → `active`. If provisioning fails, remains in `provisioning` for retry. |
| BR-TNT-006 | Tenant suspension | Super admin can suspend a tenant immediately. All access is blocked. Running scans are cancelled. Scheduled scans are disabled. Data is preserved. No refunds issued automatically. |
| BR-TNT-007 | Tenant reactivation | Only super admin can reactivate a suspended tenant. Reactivation restores access. Scheduled scans must be manually re-enabled by tenant. |
| BR-TNT-008 | Tenant deletion | Admin-only. Tenants cannot self-delete. Super admin processes deletion requests. Tenant database is dropped. Control DB records are soft-deleted. |
| BR-TNT-009 | Tenant data retention on deletion | After deletion is initiated, tenant DB is backed up, then dropped after 30 days. Control DB records (audit logs, billing history) are retained for 1 year minimum. |
| BR-TNT-010 | Tenant status values | `provisioning`, `active`, `suspended`, `deactivated` |
| BR-TNT-011 | Subscription expiry behavior | When subscription expires or payment fails, tenant is downgraded to free tier. Read-only access to existing results. No new scans. |
| BR-TNT-012 | Free tier capabilities | View existing scan results only. No new scans. No new domains. No compliance reports. No scheduled scans. No integrations. |
| BR-TNT-013 | Domain ownership verification | DNS TXT record verification. **Deferred from MVP** — documented as a rule, to be implemented post-MVP. Until then, scans work without verification. |
| BR-TNT-014 | Tenant impersonation | Super admin can impersonate any tenant user. Creates an explicit audit trail entry. Impersonation sessions are time-limited (1 hour max). |

---

## 3. Billing & Credits

### 3.1 Subscription Rules

| Rule ID | Rule | Details |
|---------|------|---------|
| BR-BILL-001 | Subscription tiers | Starter, Pro, Enterprise. Each with defined credits, domain limits, and feature access. |
| BR-BILL-002 | No free trial | No time-limited trial. Free tier (read-only after expiry/no subscription) is the entry point. Must subscribe to scan. |
| BR-BILL-003 | Billing frequency | Monthly or annual billing via Stripe. Annual billing gets a discount (configured per plan). |
| BR-BILL-004 | Plan upgrade | Immediate effect. Pro-rated billing. New features and credit allotment become available instantly. Existing credits carry over. |
| BR-BILL-005 | Plan downgrade | Takes effect at end of current billing period. Access to higher-tier features continues until period ends. Credits do not carry over to lower plan. |
| BR-BILL-006 | Subscription cancellation | Tenant retains access until end of billing period. After that, downgraded to free tier (BR-TNT-011). |
| BR-BILL-007 | Payment failure | Stripe handles retries (3 attempts over ~2 weeks). If all fail, subscription is cancelled. Tenant downgraded to free tier. |

### 3.2 Credit Rules

| Rule ID | Rule | Details |
|---------|------|---------|
| BR-BILL-008 | Credit consumption model | Per-step per-domain. Each workflow step costs X credits per domain scanned. Rates vary by subscription tier. |
| BR-BILL-009 | Credit pricing configurability | Super admin sets credit costs per check type per tier. Changes take effect for new scans only, not in-progress scans. |
| BR-BILL-010 | Credit allotment reset | Credits reset to plan allotment at the start of each billing period. Unused credits do NOT roll over. |
| BR-BILL-011 | Credit balance check | Before a scan starts, system must verify sufficient credits. If insufficient, scan is rejected with a clear error showing required vs. available credits. |
| BR-BILL-012 | Credit deduction timing | Credits are deducted at scan job creation (before execution), not after. This prevents over-consumption. |
| BR-BILL-013 | Credit refund on failure | If a scan step fails, credits for that step and all subsequent unexecuted steps are refunded. Completed steps are not refunded. |
| BR-BILL-014 | Credit transaction logging | Every credit change (allotment, consumption, purchase, refund) is logged in `credit_transactions` with timestamp, type, amount, and scan job reference. |
| BR-BILL-015 | Credit pack purchase | Tenants can purchase additional credit packs as one-time Stripe payments. Credits are added immediately and do not expire until the end of the billing period. |
| BR-BILL-016 | Manual credit adjustment | Super admin can manually adjust tenant credits (add or deduct). Must provide a reason. Logged as credit transaction with type `refund` (add) or `consumption` (deduct). |
| BR-BILL-017 | Scheduled scan credit reservation | When scheduling a recurring scan, credits for the next execution are reserved (held). If insufficient credits at scheduling time, the schedule creation is rejected. |
| BR-BILL-018 | Scheduled scan credit release | If a scheduled scan is cancelled or the schedule is deleted, reserved credits are released back to the balance. |

---

## 4. Scanning & Workflows

### 4.1 Domain Rules

| Rule ID | Rule | Details |
|---------|------|---------|
| BR-SCAN-001 | Domain limit per tier | Starter: 3, Pro: 20, Enterprise: unlimited. Exceeding the limit blocks new domain additions. |
| BR-SCAN-002 | Domain format validation | Must be a valid domain name (e.g., `example.com`). No IP addresses. No URLs with paths. No subdomains (the scan discovers those). |
| BR-SCAN-003 | Domain uniqueness per tenant | A tenant cannot add the same domain twice. Duplicate check is case-insensitive. |
| BR-SCAN-004 | Domain deletion with active scans | Cannot delete a domain that has a running scan. Must wait for scan to complete or cancel it first. |

### 4.2 Scan Execution Rules

| Rule ID | Rule | Details |
|---------|------|---------|
| BR-SCAN-005 | Concurrent scan limits | Tier-based: Starter: 1 concurrent scan total. Pro: 3 concurrent scans. Enterprise: configurable by super admin. |
| BR-SCAN-006 | One active scan per domain | Only 1 scan can run against a specific domain at a time, even within the tier's concurrent limit. |
| BR-SCAN-007 | Scan job states | `queued` → `running` → `completed` / `failed` / `partial` / `cancelled` |
| BR-SCAN-008 | Scan cancellation | Tenant can cancel a queued or running scan. Running scan stops after current step completes. Credits for unexecuted steps are refunded per BR-BILL-013. |
| BR-SCAN-009 | Scan timeout | Individual scan steps timeout after 30 minutes. Entire scan job times out after 4 hours. On timeout, mark as `partial`, refund remaining steps. |
| BR-SCAN-010 | Scan step retry | If a step fails, retry up to 2 times automatically. After 3rd failure, mark step as failed, continue to next step if possible, refund failed step credits. |
| BR-SCAN-011 | Feature flag enforcement | Before executing each step, verify the check type is enabled for the tenant's plan (feature flag check). Skip steps that are not enabled. Refund credits for skipped steps. |
| BR-SCAN-012 | Results persistence | Scan results are written to the tenant DB after each step completes. Even if subsequent steps fail, earlier results are preserved. |
| BR-SCAN-013 | Scan results immutability | Scan results are append-only. Results from a completed scan cannot be modified or deleted by the tenant. Only super admin can purge data. |

### 4.3 Workflow Rules

| Rule ID | Rule | Details |
|---------|------|---------|
| BR-SCAN-014 | System workflow templates | 5 predefined templates (Quick Recon, Full Scan, Web App Scan, Compliance Check, Continuous Monitor). Cannot be modified or deleted by tenants. |
| BR-SCAN-015 | Custom workflow creation | Pro+ tiers only. Tenants can duplicate a system template and customize steps. Must have at least 1 step. |
| BR-SCAN-016 | Workflow step ordering | Steps execute sequentially in defined order. Output of one step feeds as input to the next (e.g., subdomain enum results feed port scanning). |
| BR-SCAN-017 | Custom workflow limits | Max 20 custom workflows per tenant. Max 15 steps per workflow. |

### 4.4 Scheduled Scan Rules

| Rule ID | Rule | Details |
|---------|------|---------|
| BR-SCAN-018 | Scheduling availability | Pro+ tiers only. Starter tier cannot schedule scans. |
| BR-SCAN-019 | Minimum schedule interval | Minimum 24 hours between scheduled executions. No sub-daily scheduling. |
| BR-SCAN-020 | Schedule limit per tenant | Max 10 active schedules per tenant. |
| BR-SCAN-021 | Schedule auto-disable on suspension | All schedules are disabled when tenant is suspended. Not automatically re-enabled on reactivation. |

---

## 5. Feature Flags & Access Control

| Rule ID | Rule | Details |
|---------|------|---------|
| BR-FLAG-001 | Feature flag evaluation order | 1. Operational flag (global kill switch) → 2. Plan feature → 3. Tenant override. If step 1 is disabled, stop. Otherwise proceed through chain. |
| BR-FLAG-002 | Operational flag precedence | An operational flag set to `disabled` blocks ALL tenants regardless of plan or override. Used for maintenance, outages, or retiring features. |
| BR-FLAG-003 | Tenant override precedence | A super admin tenant override always overrides the plan default. Can enable features not in the plan (e.g., beta access) or disable features that are in the plan. |
| BR-FLAG-004 | Plan change flag sync | When a tenant changes plans, feature flags are recalculated from the new plan. Existing tenant overrides remain intact (overrides survive plan changes). |
| BR-FLAG-005 | Feature flag caching | Feature flag results are cached in Redis per tenant. Cache is invalidated on: plan change, tenant override change, operational flag change. TTL: 30 minutes as fallback. |
| BR-FLAG-006 | Feature gating enforcement points | Feature flags are checked at: API endpoint level (before processing), scan step execution (before running), UI navigation (hide/show modules). |
| BR-FLAG-007 | Blocked feature response | When a feature is blocked by flag, API returns HTTP 403 with message indicating whether it's a plan limitation ("Upgrade to Pro to access vulnerability scanning") or an operational disable ("Feature temporarily unavailable"). |

---

## 6. Compliance Engine

| Rule ID | Rule | Details |
|---------|------|---------|
| BR-COMP-001 | Framework availability by tier | Compliance frameworks are tier-gated via `plan_compliance_access`. Only frameworks enabled for the tenant's tier are visible and selectable. |
| BR-COMP-002 | Framework management | Only super admin can create, modify, or delete compliance frameworks. Tenants can only select from available frameworks. |
| BR-COMP-003 | Assessment trigger | Compliance assessment runs automatically after a scan completes, mapping results against the tenant's selected frameworks. No manual trigger needed. |
| BR-COMP-004 | Assessment scope | Assessment evaluates only the scan results from the triggering scan job. It does not aggregate results across multiple scans. |
| BR-COMP-005 | Control result statuses | `pass` — all check criteria met. `fail` — one or more criteria not met. `partial` — some criteria met, some not assessed. `not_assessed` — no scan data available for this control. |
| BR-COMP-006 | Compliance score calculation | Overall score = (number of passing controls / total assessed controls) x 100. Controls with `not_assessed` status are excluded from the denominator. |
| BR-COMP-007 | Minimum security recommendations | Each compliance control has a `min_security_recommendations_json` field defining baseline requirements. These are displayed even when a control passes, as best-practice guidance. |
| BR-COMP-008 | Report generation | Compliance reports (PDF/HTML) can be generated on demand for any completed assessment. Reports include: executive summary, per-control results, evidence, remediation, and historical trend. |
| BR-COMP-009 | Report availability | Compliance reports are only available on Pro+ tiers. Starter tier cannot generate or view compliance reports. |
| BR-COMP-010 | Historical trend | System tracks compliance scores over time per framework per domain. Minimum 3 assessments needed before trend is displayed. |
| BR-COMP-011 | Framework requests | Tenants can submit a request for a new compliance framework or additional compliance features. Requests are logged and reviewed by super admin. No SLA on fulfillment — treated as feature requests. |

---

## 7. CVE Monitoring

| Rule ID | Rule | Details |
|---------|------|---------|
| BR-CVE-001 | Feed ingestion frequency | CVE feeds (NVD, MITRE) are synced every 1 hour. Super admin can trigger manual sync. |
| BR-CVE-002 | Severity threshold for alerting | Only critical and high severity CVEs trigger automatic tenant alerts. Medium and low are stored but do not generate alerts. |
| BR-CVE-003 | Tech stack matching | CVE matching compares `affected_products_json` against tenant's `technologies` table (tech_name + version). Match must include version range overlap. |
| BR-CVE-004 | Alert creation | When a match is found, a `vulnerability_alert` is created in the affected tenant's DB with status `new`. |
| BR-CVE-005 | Alert notification | New critical CVE alerts trigger immediate notification via tenant's configured channels. High CVE alerts batch into a daily digest. |
| BR-CVE-006 | Alert lifecycle | `new` → `acknowledged` → `resolved` or `false_positive`. Only tenant owner can change alert status. |
| BR-CVE-007 | Alert auto-resolution | If a subsequent scan shows the affected technology has been updated to a non-vulnerable version, the alert is auto-resolved with status `resolved` and a note indicating auto-resolution. |
| BR-CVE-008 | CVE monitoring scope | CVE monitoring only applies to tenants with at least one completed scan that includes technology detection results. Tenants with no scan data receive no alerts. |

---

## 8. Integrations

### 8.1 Platform API Keys (Data Sources)

| Rule ID | Rule | Details |
|---------|------|---------|
| BR-INT-001 | Platform-managed keys only | Tenants do not provide their own API keys for data sources (Shodan, Censys, etc.). All keys are managed by super admin in a shared pool. |
| BR-INT-002 | API key rotation | Super admin is responsible for rotating API keys. System tracks usage count and monthly quota per key. Alert super admin when a key reaches 80% of its quota. |
| BR-INT-003 | Rate limiting per tenant | API calls to third-party sources are rate-limited per tenant to prevent one tenant from exhausting the shared pool. Limits configurable by super admin per provider. |
| BR-INT-004 | API usage tracking | Every API call to a third-party source is tracked with: tenant ID, scan job ID, provider, call count, timestamp. Enables cost attribution. |
| BR-INT-005 | Provider availability by tier | Shodan, SecurityTrails, VirusTotal: Pro+. Censys, Custom connectors: Enterprise only. Nuclei templates: all tiers. |
| BR-INT-006 | API key exhaustion behavior | If all keys for a provider are exhausted or rate-limited, the scan step using that provider is skipped. Results are marked as incomplete. Credits for that step are refunded. |

### 8.2 Notification Integrations (Tenant-Configured)

| Rule ID | Rule | Details |
|---------|------|---------|
| BR-INT-007 | Notification config ownership | Tenants configure their own notification integrations (Slack webhook URL, Jira API token, email addresses, webhook endpoints). |
| BR-INT-008 | Notification retry | Failed notification delivery is retried 3 times with exponential backoff (1 min, 5 min, 15 min). After 3 failures, notification is marked as `failed` in history. |
| BR-INT-009 | Notification event types | Scan complete, scan failed, CVE alert (critical), CVE alert (high, daily digest), credit low (< 20% remaining), compliance report ready, compliance score change. |
| BR-INT-010 | Notification channel limits | All notification channels (Email, Slack, Jira, Webhooks, SIEM) are available to all tiers. Limit on number of active integrations: Starter: 2, Pro: 5, Enterprise: unlimited. |
| BR-INT-011 | Notification history retention | Notification history is retained for 90 days, then auto-purged. |
| BR-INT-012 | Webhook security | Outbound webhooks include an HMAC signature header for verification. Tenants receive a webhook secret when configuring. |

---

## 9. Super Admin & Operations

| Rule ID | Rule | Details |
|---------|------|---------|
| BR-ADM-001 | Super admin creation | Super admins are seeded directly in the database. No self-registration or invitation flow. |
| BR-ADM-002 | Super admin audit trail | All super admin actions are logged with `is_super_admin = true` flag. Includes: action, target tenant, target resource, IP address, timestamp. |
| BR-ADM-003 | Tenant impersonation rules | Impersonation creates a separate audit trail entry. Impersonation sessions are limited to 1 hour. All actions during impersonation are logged under the super admin's ID with an impersonation flag. |
| BR-ADM-004 | Credit adjustment rules | Super admin can add or deduct credits from any tenant. Must provide a reason. Logged as credit transaction with type `refund` (add) or `consumption` (deduct). |
| BR-ADM-005 | Feature override rules | Super admin can override any feature flag per tenant. Must provide a reason. Override persists across plan changes. |
| BR-ADM-006 | Tenant suspension criteria | Super admin may suspend a tenant for: payment fraud, abuse of platform resources, violation of terms of service, security incident. Reason must be documented. |
| BR-ADM-007 | Platform API key management | Super admin adds, rotates, and deactivates platform API keys. Deactivated keys are never deleted, only marked inactive for audit purposes. |
| BR-ADM-008 | Compliance framework lifecycle | Super admin creates frameworks as `draft` → `active`. Active frameworks can be used by tenants. Deprecated frameworks are hidden from new selections but existing assessments remain valid. |
| BR-ADM-009 | Scan step pricing changes | Pricing changes take effect for new scans only. In-progress scans use the pricing at the time of job creation. |
| BR-ADM-010 | System maintenance mode | Super admin can enable maintenance mode (operational flag). All tenant scans are paused. Running scans complete but no new scans are accepted. Dashboard shows maintenance notice. |

---

## 10. Data, Audit & Platform Compliance

### 10.1 Data Retention

| Rule ID | Rule | Details |
|---------|------|---------|
| BR-DATA-001 | Scan result retention by tier | Starter: 30 days. Pro: 90 days. Enterprise: 1 year. After retention period, scan results are auto-purged from tenant DB. |
| BR-DATA-002 | Audit log retention | Minimum 1 year for all audit logs (platform compliance requirement). Not configurable per tenant. |
| BR-DATA-003 | Tenant DB backups | Daily encrypted backups of each tenant database. Retained for 30 days. |
| BR-DATA-004 | Credit transaction retention | Credit transaction history is retained for the lifetime of the tenant account. Never auto-purged. |
| BR-DATA-005 | Notification history retention | 90 days, then auto-purged (BR-INT-011). |

### 10.2 Security & Encryption

| Rule ID | Rule | Details |
|---------|------|---------|
| BR-DATA-006 | Encryption at rest | All PostgreSQL databases encrypted at rest (TDE or disk-level encryption). |
| BR-DATA-007 | Encryption in transit | TLS 1.2+ required for all connections: API, database, Redis, third-party APIs. |
| BR-DATA-008 | API key encryption | Platform API keys stored encrypted with AES-256. Decrypted only in memory during use. |
| BR-DATA-009 | Password hashing | BCrypt with minimum cost factor of 12. |
| BR-DATA-010 | 2FA secret storage | TOTP secrets stored encrypted, not in plaintext. |

### 10.3 Audit Logging

| Rule ID | Rule | Details |
|---------|------|---------|
| BR-DATA-011 | Auditable events | Authentication (login, logout, 2FA, failed attempts), CRUD on all resources (domains, scans, workflows), billing events (plan change, credit purchase), super admin actions, feature flag changes, compliance report generation. |
| BR-DATA-012 | Audit log immutability | Audit logs are append-only. Cannot be modified or deleted by any user including super admin. |
| BR-DATA-013 | Audit log fields | Every log entry includes: tenant_id, user_id, action, resource_type, resource_id, details_json, ip_address, user_agent, timestamp, is_super_admin. |

### 10.4 Data Isolation

| Rule ID | Rule | Details |
|---------|------|---------|
| BR-DATA-014 | Cross-tenant data isolation | No API endpoint or query can access data from a different tenant's database. Tenant resolution middleware enforces this at the request level. |
| BR-DATA-015 | Tenant DB naming convention | `tenant_{slug}` where slug is the sanitized tenant name. |

### 10.5 Migration Rules

| Rule ID | Rule | Details |
|---------|------|---------|
| BR-DATA-016 | Base migration propagation | When a base migration is added, it must be applied to all active tenant databases. Failed applications are flagged for manual review. Tenant status is not affected. |
| BR-DATA-017 | Tenant-specific migration isolation | Tenant-specific migrations are applied only to the requesting tenant's DB. Up and down scripts are stored in the control DB for rollback capability. |
| BR-DATA-018 | Migration rollback scope | Rolling back a tenant-specific migration affects only that tenant. Other tenants are completely unaffected. |
| BR-DATA-019 | Migration conflict detection | Before applying a base migration to a tenant with custom migrations, check for schema conflicts. If conflict detected, flag for manual review instead of auto-applying. |

---

## Quick Reference: Rule Count by Domain

| Domain | Rules | Range |
|--------|-------|-------|
| Authentication & Security | 15 | BR-AUTH-001 to BR-AUTH-015 |
| Tenant Management | 14 | BR-TNT-001 to BR-TNT-014 |
| Billing & Credits | 18 | BR-BILL-001 to BR-BILL-018 |
| Scanning & Workflows | 21 | BR-SCAN-001 to BR-SCAN-021 |
| Feature Flags | 7 | BR-FLAG-001 to BR-FLAG-007 |
| Compliance Engine | 11 | BR-COMP-001 to BR-COMP-011 |
| CVE Monitoring | 8 | BR-CVE-001 to BR-CVE-008 |
| Integrations | 12 | BR-INT-001 to BR-INT-012 |
| Super Admin & Operations | 10 | BR-ADM-001 to BR-ADM-010 |
| Data, Audit & Compliance | 19 | BR-DATA-001 to BR-DATA-019 |
| **Total** | **135** | |
