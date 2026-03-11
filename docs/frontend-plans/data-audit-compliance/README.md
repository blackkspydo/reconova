# Data, Audit & Platform Compliance — Frontend Plan

Scope: Tenant privacy center with data inventory, data export request/download, data deletion with multi-step confirmation, audit log viewer (tenant-scoped and cross-tenant admin), and admin backup/migration status views.

**Based on:** `docs/plans/business-rules/10-data-audit-platform-compliance.md` (BR-DATA-001 — BR-DATA-023)
**Last updated:** 2026-03-11

---

## Documentation Index

| # | Artifact | Description | Audience |
|---|----------|-------------|----------|
| 1 | [README.md](./README.md) | Overview, state machines, navigation map | All |
| 2 | [user-flows.md](./user-flows.md) | User journey flowcharts, branching logic | Design / Frontend |
| 3 | [screens-wireframes.md](./screens-wireframes.md) | ASCII wireframes for every screen state | Design / Frontend |
| 4 | [implementation-guide.md](./implementation-guide.md) | State management, API integration, components | Frontend devs |
| 5 | [reference.md](./reference.md) | Error handling, validation, security | Frontend devs |

---

## Business Rule Coverage

| BR Code | Rule Name | Frontend Feature | Plan Location |
|---------|-----------|------------------|---------------|
| BR-DATA-001 | Scan Result Retention | Display retention period per tier in data inventory | **This plan** (data inventory) |
| BR-DATA-002 | Audit Log Retention | Display "minimum 1 year" in data inventory | **This plan** (data inventory) |
| BR-DATA-003 | Tenant DB Backups | Admin backup status view | **This plan** (admin) |
| BR-DATA-004 | Credit Transaction Retention | Display "lifetime" in data inventory | **This plan** (data inventory) |
| BR-DATA-005 | Notification History Retention | Display "90 days" in data inventory | **This plan** (data inventory) |
| BR-DATA-006 | Encryption at Rest | Backend-only (no frontend) | — |
| BR-DATA-007 | Encryption in Transit | Backend-only (no frontend) | — |
| BR-DATA-008 | API Key Encryption | Backend-only (no frontend) | — |
| BR-DATA-009 | Password Hashing | Backend-only (no frontend) | — |
| BR-DATA-010 | 2FA Secret Storage | Backend-only (no frontend) | — |
| BR-DATA-011 | Auditable Events | Audit log viewer — event category filters | **This plan** |
| BR-DATA-012 | Audit Log Immutability | Read-only UI — no edit/delete actions | **This plan** |
| BR-DATA-013 | Audit Log Fields | Audit log table columns + detail expansion | **This plan** |
| BR-DATA-014 | Cross-Tenant Isolation | Backend-only (no frontend) | — |
| BR-DATA-015 | Tenant DB Naming | Backend-only (no frontend) | — |
| BR-DATA-016 | Pre-Migration Backup | Admin migration status view | **This plan** (admin) |
| BR-DATA-017 | Data Integrity Verification | Admin migration status — integrity check indicator | **This plan** (admin) |
| BR-DATA-018 | Migration Backup Cleanup | Admin backup status — cleanup status | **This plan** (admin) |
| BR-DATA-019 | Schema Conflict Detection | Admin migration status — conflict alerts | **This plan** (admin) |
| BR-DATA-020 | Data Export | Privacy center — request, status, download | **This plan** |
| BR-DATA-021 | Data Deletion | Privacy center — multi-step deletion modal | **This plan** |
| BR-DATA-022 | Data Inventory | Privacy center — data categories + processors | **This plan** |
| BR-DATA-023 | Purge Strategy | Backend-only (no frontend) | — |

---

## User Roles

| Role | Screens Accessible | Key Actions |
|------|-------------------|-------------|
| **Tenant Owner** | Privacy center, audit log (own tenant) | View data inventory, request export, download export, request deletion, cancel deletion, view audit logs |
| **Tenant Member** | Audit log (own tenant, read-only) | View audit logs only (cannot request export or deletion) |
| **Super Admin** | Cross-tenant audit log, backup status, migration status | Search all audit logs, view backup health, view migration status, trigger integrity checks |

---

## Data Export State Machine

```
         ┌───────────┐
         │   IDLE     │ (no active export)
         └─────┬─────┘
               │ Request export
               ▼
         ┌───────────┐
         │ PROCESSING │ (backend packaging data)
         └─────┬─────┘
               │ Export ready
               ▼
         ┌───────────┐
         │   READY    │ (download available)
         └──┬──────┬──┘
            │      │
  Download  │      │ 24h expires
            ▼      ▼
         ┌───────────┐
         │  EXPIRED   │
         └───────────┘
```

### State Transitions

| Current State | Action | Next State | Conditions | Who |
|--------------|--------|------------|------------|-----|
| IDLE | Request export | PROCESSING | No existing pending/processing export | Tenant Owner / Super Admin |
| PROCESSING | Backend completes | READY | Export file packaged, download URL generated | System |
| READY | Download | EXPIRED | First download OR 24h elapsed | Tenant Owner / Super Admin |
| READY | 24h expires | EXPIRED | Auto-expiry | System |
| EXPIRED | (N/A) | IDLE | User can request new export | — |

---

## Data Deletion State Machine

```
         ┌───────────┐
         │   IDLE     │ (no deletion request)
         └─────┬─────┘
               │ Request deletion (phrase + re-auth)
               ▼
         ┌───────────┐
         │  PENDING   │ (72-hour cooling off)
         └──┬──────┬──┘
            │      │
   Cancel   │      │ 72h expires
            ▼      ▼
         ┌─────┐ ┌───────────┐
         │IDLE │ │ EXECUTING  │
         └─────┘ └─────┬─────┘
                       │ Complete
                       ▼
                 ┌───────────┐
                 │ COMPLETED  │
                 └───────────┘
                 (terminal — account gone)
```

### State Transitions

| Current State | Action | Next State | Conditions | Who |
|--------------|--------|------------|------------|-----|
| IDLE | Request deletion | PENDING | Confirmation phrase matches, re-auth passed | Tenant Owner |
| PENDING | Cancel | IDLE | Within 72-hour window | Tenant Owner / Super Admin |
| PENDING | 72h expires | EXECUTING | Automatic — system processes | System |
| EXECUTING | Deletion completes | COMPLETED | DB dropped, records soft-deleted | System |

### Deletion Multi-Step Modal

| Step | Content | Validation |
|------|---------|------------|
| 1 — Warning + Confirm | Warning text, confirmation phrase input: "DELETE {tenant-slug}" | Phrase must match exactly (case-sensitive) |
| 2 — Re-Authenticate | Password input + 2FA code input | Must pass both |
| 3 — Final Confirmation | 72-hour cooling-off explanation, [Confirm Deletion] button | None — informational |

---

## Audit Log Event Categories

| Category | Event Prefix | Example Events |
|----------|-------------|----------------|
| Authentication | `auth.*` | login, logout, login_failed, 2fa_setup, password_changed |
| Tenant Lifecycle | `tenant.*` | created, updated, suspended, reactivated, deleted |
| Domain Management | `domain.*` | added, verified, removed |
| Scanning | `scan.*` | created, started, completed, failed, cancelled |
| Billing | `billing.*` | plan_changed, credit_purchased, refund_issued |
| Feature Flags | `feature.*` | flag_updated, override_created, operational_toggled |
| Compliance | `compliance.*` | framework_selected, assessment_generated, report_downloaded |
| Integrations | `integration.*` | created, updated, deleted |
| Notifications | `notification.*` | sent, failed |
| Super Admin | `admin.*` | impersonation_started, config_changed, api_key_rotated |
| Config Changes | `config.*` | updated, rolled_back, critical_approved |
| Data Operations | `data.*` | export_requested, deletion_requested, deletion_completed |

---

## Data Inventory Categories

| Category | Description | Retention | Deletable |
|----------|-------------|-----------|:---------:|
| Scan Results | Subdomain, port, technology, vulnerability data | 30d (Starter) / 90d (Pro) / 1yr (Enterprise) | Yes |
| Billing Information | Subscription history, credit transactions, payment refs | Lifetime (credit txns), soft-deleted on account deletion | No |
| Audit Logs | Login history, action history, admin actions | Minimum 1 year | No |
| Account Information | Email, tenant name, domain list | Active account lifetime | Yes |
| Notification History | Delivery logs for integrations | 90 days | Yes |
| Compliance Reports | Assessment results, control scores | Same as scan results per tier | Yes |

### Third-Party Processors

| Processor | Purpose | Data Shared |
|-----------|---------|-------------|
| Stripe | Payment processing | Email, payment method tokens |
| Shodan / SecurityTrails / Censys | Scan data enrichment | Target domains and subdomains (during scans) |

---

## Screen Navigation Map

```
/privacy (Tenant)
  ├── Privacy Center (default)
  │     ├── Data Inventory section
  │     │     ├── Data categories table (category, description, retention, deletable)
  │     │     └── Third-party processors list
  │     ├── Export Data section
  │     │     ├── Current export status (if any)
  │     │     ├── [Request Export] button
  │     │     └── Download link (when ready)
  │     └── Delete Account section
  │           ├── Deletion status (if pending)
  │           ├── [Delete My Account] button → multi-step modal
  │           └── [Cancel Deletion] (if pending)
  │
  └── Audit Log (/privacy/audit-log)
        ├── Filters: date range, category, action search
        ├── Paginated log table
        └── Expandable rows with detail JSON

/admin/audit-logs (Super Admin)
  └── Cross-Tenant Audit Log
        ├── All filters from tenant view + tenant filter
        ├── Impersonation badge on relevant entries
        └── Super admin action highlighting

/admin/data (Super Admin)
  ├── Backups tab (/admin/data/backups)
  │     ├── Backup health summary (last 24h)
  │     ├── Per-tenant backup status table
  │     └── [Trigger Manual Backup] per tenant
  │
  └── Migrations tab (/admin/data/migrations)
        ├── Recent migrations table
        ├── Integrity check status per migration
        ├── Conflict alerts
        └── [Run Integrity Check] per tenant
```

### Screen Summary

| # | Screen | Route | Access | Notes |
|---|--------|-------|--------|-------|
| 1 | Privacy Center | `/privacy` | Tenant Owner (full), Members (view inventory only) | Data inventory + export + deletion |
| 2 | Audit Log | `/privacy/audit-log` | Tenant Owner + Members | Filterable, paginated, read-only |
| 3 | Admin Audit Log | `/admin/audit-logs` | Super Admin | Cross-tenant search |
| 4 | Admin Backups | `/admin/data` (backups tab) | Super Admin | Backup health + manual trigger |
| 5 | Admin Migrations | `/admin/data` (migrations tab) | Super Admin | Status + integrity + conflicts |

---

## Feature Flag Integration

| Flag | Effect |
|------|--------|
| None | Privacy center and audit logs are not feature-gated — available to all plans. Data retention periods vary by tier (displayed dynamically from plan). |

---

## Banners & Global States

| Condition | Banner/Indicator | Actions |
|-----------|-----------------|---------|
| Deletion pending | Warning banner at top of privacy center: "Account deletion scheduled for {date}. You have {hours} hours to cancel." | [Cancel Deletion] CTA |
| Export processing | Info banner in export section: "Your data export is being prepared. We'll notify you when it's ready." | None (wait) |
| Export ready | Success banner in export section: "Your data export is ready. Download within 24 hours." | [Download] CTA |
| Export expired | Info text: "Your previous export has expired. Request a new one." | [Request Export] CTA |
| Backup failure (admin) | Warning row in backup table: tenant highlighted with failure badge | [Retry Backup] per tenant |
| Migration integrity warning (admin) | Warning badge on migration row: "NEEDS_REVIEW" | [View Details] link |
| Schema conflict (admin) | Alert badge on migration row: "CONFLICT — {n} tenants skipped" | [View Conflicts] link |
