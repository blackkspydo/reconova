# Super Admin & Operations — Frontend Plan

Scope: Admin dashboard home with summary metrics, monitoring detail page with auto-refreshing platform health data, alert threshold management, maintenance mode control, and global impersonation session indicator.

**Based on:** `docs/plans/business-rules/09-super-admin-operations.md` (BR-ADM-001 — BR-ADM-012)
**Last updated:** 2026-03-11

---

## Documentation Index

| # | Artifact | Description | Audience |
|---|----------|-------------|----------|
| 1 | [README.md](./README.md) | Overview, cross-references, navigation map | All |
| 2 | [user-flows.md](./user-flows.md) | User journey flowcharts, branching logic | Design / Frontend |
| 3 | [screens-wireframes.md](./screens-wireframes.md) | ASCII wireframes for every screen state | Design / Frontend |
| 4 | [implementation-guide.md](./implementation-guide.md) | State management, API integration, components | Frontend devs |
| 5 | [reference.md](./reference.md) | Error handling, validation, security | Frontend devs |

---

## Business Rule Coverage

| BR Code | Rule Name | Frontend Feature | Plan Location |
|---------|-----------|------------------|---------------|
| BR-ADM-001 | Super Admin Identity | Admin session awareness, 2FA enforcement on first login | **This plan** (identity context) |
| BR-ADM-002 | Super Admin Audit Trail | Admin session ID tracking, audit badge in header | **This plan** (identity context) |
| BR-ADM-003 | Tenant Impersonation | Floating impersonation indicator (global component) | **This plan** (indicator) + `tenant-management` (start flow) |
| BR-ADM-004 | Credit Adjustments | Cross-reference only | `billing-credits` (`/admin/billing/credits`) |
| BR-ADM-005 | Feature Overrides | Cross-reference only | `feature-flags-access-control` (`/admin/features/overrides`) |
| BR-ADM-006 | Tenant Suspension | Cross-reference only | `tenant-management` (`/admin/tenants/:id`) |
| BR-ADM-007 | Platform API Key Management | Cross-reference only | `integrations` (`/admin/integrations` API Keys tab) |
| BR-ADM-008 | Compliance Framework Management | Cross-reference only | `compliance-engine` (`/admin/compliance`) |
| BR-ADM-009 | Scan Step Pricing | Cross-reference only | `billing-credits` (`/admin/billing/pricing`) |
| BR-ADM-010 | System Maintenance Mode | Maintenance toggle in dashboard header + confirmation modal | **This plan** |
| BR-ADM-011 | Monitoring Metrics | Admin dashboard home + monitoring detail page | **This plan** |
| BR-ADM-012 | Monitoring Alerts | Dedicated alerts management page + header notification badge | **This plan** |

---

## User Roles

| Role | Screens Accessible | Key Actions |
|------|-------------------|-------------|
| **Super Admin** | All `/admin/*` screens | View dashboard, monitor platform health, configure alert thresholds, toggle maintenance mode, manage impersonation sessions |

> No tenant roles access these screens. Admin layout enforces `SUPER_ADMIN` role guard.

---

## Maintenance Mode State Machine

```
         ┌──────────┐
         │ INACTIVE  │ (normal operations)
         └────┬──────┘
              │ Enable (reason + duration)
              ▼
         ┌──────────┐
         │  ACTIVE   │ (scans blocked, banner shown)
         └────┬──────┘
              │ Disable (manual)
              ▼
         ┌──────────┐
         │ INACTIVE  │
         └──────────┘
```

### State Transitions

| Current State | Action | Next State | Conditions | Who |
|--------------|--------|------------|------------|-----|
| INACTIVE | Enable maintenance | ACTIVE | Reason required (≥10 chars). Estimated duration required. | Super Admin |
| ACTIVE | Disable maintenance | INACTIVE | No conditions. | Super Admin |

### Effects During Active Maintenance

| System Area | Behavior |
|-------------|----------|
| New scan creation | Rejected with ERR_SYS_002 |
| Running scans | Complete normally (not interrupted) |
| API read operations | Continue normally |
| Scheduled scans | Will not trigger |
| Frontend (all tenants) | Maintenance banner with estimated duration |
| Admin panel | Maintenance badge visible, toggle available |

---

## Impersonation Session Model

```
         ┌──────────┐
         │   IDLE    │ (no active impersonation)
         └────┬──────┘
              │ Start (tenant + reason)
              ▼
         ┌──────────┐
         │  ACTIVE   │ (floating indicator visible)
         └──┬────┬───┘
            │    │
   End      │    │ Expire (TTL reached)
            ▼    ▼
         ┌──────────┐
         │   IDLE    │
         └──────────┘
```

### Session Constraints

| Constraint | Rule |
|-----------|------|
| Session TTL | Default 60 minutes (configurable) |
| Concurrent sessions | One per admin at a time |
| Extension | Cannot extend. Must end and start new session. |
| Auto-expiry | Actions after expiry rejected with 401 |

### Restricted Actions During Impersonation

| Action | Allowed | Reason |
|--------|:-------:|--------|
| View tenant data (scans, domains, results) | Yes | Primary purpose |
| Create/cancel scans | Yes | Debugging and support |
| View billing info | Yes | Support use case |
| Create/modify integrations | Yes | Support and debugging |
| Change tenant owner password | No | Security |
| Change/disable 2FA settings | No | Security |
| Delete the tenant | No | Destructive — admin panel only |
| Change billing/payment methods | No | Financial — tenant owner only |

---

## Monitoring Alert Levels

| Level | Color | Urgency | Examples |
|-------|-------|---------|---------|
| CRITICAL | Red | Immediate attention | Zero active workers, all API keys exhausted for provider |
| WARNING | Yellow | Investigate soon | Queue depth >80%, stale workers, DB pool >80%, credit anomaly |

### Configurable Alert Thresholds

| Alert | Default Threshold | Configurable Field |
|-------|-------------------|-------------------|
| Queue depth | 80% of max_depth | `queue_depth_pct` |
| Worker stale timeout | `scanning.worker.stale_threshold_minutes` | `worker_stale_minutes` |
| API key pool minimum | `integrations.api_key.pool_min_keys` | `api_key_pool_min` |
| DB connection pool | 80% capacity | `db_pool_pct` |
| Credit consumption anomaly | 5x daily average | `credit_anomaly_multiplier` |

---

## Metric Refresh Rates

| Category | Metrics | Refresh Interval |
|----------|---------|-----------------|
| Scan metrics | Active scans, queue depth, worker status | 30 seconds |
| System health | API error rate, latency, DB pool, Redis | 1 minute |
| Tenant metrics | Active tenants, by plan, new, suspended, churn | 5 minutes |
| Credit metrics | Consumption, revenue, purchases | Hourly |

---

## Screen Navigation Map

```
/admin (Super Admin)
  ├── Admin Dashboard (home, default landing)
  │     ├── Summary cards: tenants, scans, credits, system health
  │     ├── Maintenance mode toggle (header)
  │     ├── Active alerts notification badge (header)
  │     ├── Quick links to all admin sections
  │     └── Link → /admin/monitoring
  │
  ├── Monitoring Detail (/admin/monitoring)
  │     ├── Tenant Metrics section
  │     │     └── Active tenants, by plan, new (7d/30d), suspended, churn
  │     ├── Scan Metrics section
  │     │     └── Active scans, queue depth, completed (24h), failure rate, avg duration
  │     ├── Worker Metrics section
  │     │     └── Active workers, stale workers, utilization
  │     ├── Credit & Revenue section
  │     │     └── Credits consumed (30d), by tier, MRR, pack purchases
  │     ├── System Health section
  │     │     └── API error rate, latency (p50/p95/p99), DB pool, Redis, API key pool
  │     └── Link → /admin/monitoring/alerts
  │
  ├── Alert Management (/admin/monitoring/alerts)
  │     ├── Active Alerts panel
  │     │     └── Currently firing alerts with level, condition, timestamp
  │     └── Alert Rules table
  │           └── Configurable thresholds: condition, threshold, level, enabled toggle
  │
  └── (Other admin sections — cross-referenced)
        ├── /admin/tenants          → tenant-management plan
        ├── /admin/users            → authentication plan
        ├── /admin/features/*       → feature-flags-access-control plan
        ├── /admin/integrations     → integrations plan
        ├── /admin/compliance       → compliance-engine plan
        ├── /admin/billing/*        → billing-credits plan
        ├── /admin/cve              → cve-monitoring plan
        └── /admin/scans/limits     → scanning-workflows plan

Global Component:
  └── ImpersonationIndicator (floating pill, bottom-right)
        ├── Collapsed: tenant name + timer
        ├── Expanded: tenant name, time remaining, restricted actions note, [End Session]
        └── Visible on ALL pages during active impersonation
```

### Screen Summary

| # | Screen | Route | Notes |
|---|--------|-------|-------|
| 1 | Admin Dashboard | `/admin` | Landing page with summary cards + quick links |
| 2 | Monitoring Detail | `/admin/monitoring` | Auto-refreshing platform metrics by category |
| 3 | Alert Management | `/admin/monitoring/alerts` | Active alerts + threshold configuration |
| 4 | Maintenance Mode Modal | (overlay on any `/admin/*`) | Confirmation with reason + duration |
| 5 | Impersonation Indicator | (global floating component) | Floating pill on all pages during impersonation |

---

## Feature Flag Integration

| Flag | Effect |
|------|--------|
| `maintenance_mode` | Operational flag. When DISABLED (maintenance active): tenant-facing maintenance banner, scan creation blocked. Admin dashboard shows active maintenance indicator. |

> No subscription flags gate the admin panel — it is role-gated (`SUPER_ADMIN` only).

---

## Cross-Reference Index

| Admin Feature | Plan | Route |
|--------------|------|-------|
| Tenant list/detail/suspend/impersonate start | `tenant-management` | `/admin/tenants`, `/admin/tenants/:id` |
| User list/detail/deactivate/unlock | `authentication` | `/admin/users`, `/admin/users/:id` |
| Feature flag toggles + overrides | `feature-flags-access-control` | `/admin/features/*` |
| API key pool + usage + providers | `integrations` | `/admin/integrations` |
| Compliance framework CRUD + publish | `compliance-engine` | `/admin/compliance` |
| Credit adjustments | `billing-credits` | `/admin/billing/credits` |
| Scan step pricing | `billing-credits` | `/admin/billing/pricing` |
| CVE feed sources + aliases + database | `cve-monitoring` | `/admin/cve` |
| Concurrent scan limits | `scanning-workflows` | `/admin/scans/limits` |
