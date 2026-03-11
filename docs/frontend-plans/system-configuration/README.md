# System Configuration — Frontend Plan

Scope: Admin system configuration management with grouped config viewing/editing, change history with rollback, critical config approval workflow, sensitive value masking, and cache invalidation.

**Based on:** `docs/plans/business-rules/11-system-configuration.md` (BR-CFG-001 — BR-CFG-009)
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
| BR-CFG-001 | Config Tier Separation | Bootstrap configs shown as read-only info banner | **This plan** |
| BR-CFG-002 | Bootstrap Config Immutability | Info banner: "Infrastructure configs require redeploy" | **This plan** |
| BR-CFG-003 | Config Storage & Update | Config list with inline edit, reason required | **This plan** |
| BR-CFG-004 | Config Value Validation | Client-side type/range/enum validation on edit form | **This plan** |
| BR-CFG-005 | Config Change History | Change history page with filterable log | **This plan** |
| BR-CFG-006 | Config Rollback | [Rollback] action on history entries | **This plan** |
| BR-CFG-007 | Config Caching | Cache status indicator + [Invalidate Cache] button | **This plan** |
| BR-CFG-008 | Critical Config Approval | Approval queue page with request/approve/reject flow | **This plan** |
| BR-CFG-009 | Config Seeding | Backend-only (seed on deploy). No frontend action. | — |

---

## User Roles

| Role | Screens Accessible | Key Actions |
|------|-------------------|-------------|
| **Super Admin** | All `/admin/config/*` screens | View configs, edit values, request critical changes, approve/reject requests, rollback, invalidate cache |

> No tenant roles access these screens. Admin layout enforces `SUPER_ADMIN` role guard. Tenant owners have zero access to system configuration.

---

## Config Update State Machine

```
                    ┌──────────┐
                    │  VIEWING  │ (config row, read-only)
                    └────┬─────┘
                         │ Click [Edit]
                         ▼
                    ┌──────────┐
                    │  EDITING  │ (inline form expanded)
                    └──┬────┬──┘
                       │    │
          is_critical? │    │ !is_critical
                       │    │
                       ▼    ▼
              ┌────────────┐  ┌───────────┐
              │  REQUESTING │  │  SAVING   │
              │  (approval) │  │  (direct) │
              └──────┬─────┘  └─────┬─────┘
                     │              │
                     ▼              ▼
              ┌────────────┐  ┌───────────┐
              │  PENDING   │  │  SAVED    │
              │  APPROVAL  │  │  (done)   │
              └────────────┘  └───────────┘
```

### Config Update Transitions

| Current State | Action | Next State | Conditions |
|--------------|--------|------------|------------|
| VIEWING | Click [Edit] | EDITING | One edit at a time across all categories |
| EDITING | Click [Cancel] | VIEWING | Revert to original value |
| EDITING | Click [Save] | SAVING | Config is NOT critical. Value valid. Reason ≥1 char. |
| EDITING | Click [Request Approval] | REQUESTING | Config IS critical. Value valid. Reason ≥1 char. |
| SAVING | API success | VIEWING | Row shows updated value. Toast success. |
| SAVING | API error | EDITING | Inline error shown. Form stays open. |
| REQUESTING | API success | VIEWING | Toast: "Approval request submitted." Approval badge count increments. |
| REQUESTING | API error | EDITING | Inline error shown. Form stays open. |

---

## Critical Config Approval State Machine

```
         ┌──────────┐
         │   IDLE   │ (no pending request for this config)
         └────┬─────┘
              │ Admin submits request
              ▼
         ┌──────────┐
         │  PENDING  │ (awaiting different admin's action)
         └──┬──┬──┬──┘
            │  │  │
   Approve  │  │  │ Expire (24h)
            │  │  │
            ▼  │  ▼
  ┌──────────┐ │ ┌──────────┐
  │ APPROVED │ │ │ EXPIRED  │
  │(applied) │ │ └──────────┘
  └──────────┘ │
               │ Reject
               ▼
         ┌──────────┐
         │ REJECTED │
         └──────────┘
```

### Approval Transitions

| Current State | Action | Next State | Conditions |
|--------------|--------|------------|------------|
| IDLE | Submit request | PENDING | Config is critical. Valid value. Reason provided. |
| PENDING | Approve | APPROVED | Different admin than requester. Request not expired. |
| PENDING | Reject | REJECTED | Any super admin. Rejection reason required. |
| PENDING | 24h elapsed | EXPIRED | Automatic. Requester notified. |
| APPROVED | — | IDLE | Config value applied. New request can be created. |
| REJECTED | — | IDLE | No change applied. New request can be created. |
| EXPIRED | — | IDLE | No change applied. New request can be created. |

---

## Configuration Categories

| Category | Key Prefix | Config Count | Contains Critical | Contains Sensitive |
|----------|-----------|-------------|:-----------------:|:------------------:|
| Authentication | `auth.*` | 12 | No | No |
| Tenant Management | `tenant.*` | 6 | Yes (2) | No |
| Billing & Credits | `billing.*` | 6 | Yes (3) | No |
| Scanning & Workflows | `scanning.*` | 10 | Yes (1) | No |
| Feature Flags | `feature_flags.*` | 2 | No | No |
| Compliance | `compliance.*` | 5 | No | No |
| CVE Monitoring | `cve.*` | 3 | No | No |
| Integrations | `integrations.*` | 5 | No | No |
| Rate Limiting | `rate_limit.*` | 8 | No | No |
| API Versioning | `versioning.*` | 2 | Yes (2) | No |
| Platform Operations | `admin.*` | 6 | Yes (1) | No |

> **Sensitive configs**: While the `is_sensitive` flag exists in the schema, none of the default-seeded configs are marked sensitive. The UI must still support masking for any config that may be added later with `is_sensitive = true`.

---

## Config Data Types & Edit Controls

| Data Type | Edit Control | Validation | Display |
|-----------|-------------|------------|---------|
| `INTEGER` | Number input | Min/max range, integer only | Numeric value |
| `DECIMAL` | Number input | Min/max range, decimal allowed | Numeric value |
| `BOOLEAN` | Toggle switch | true/false only | "Enabled" / "Disabled" |
| `STRING` | Text input (or dropdown if `allowed_values` set) | Allowed values list | String value |
| `JSON` | Textarea with JSON validation | Valid JSON syntax | Formatted JSON |
| `DURATION` | Number input with unit label | Min/max range | Value + unit |

---

## Screen Navigation Map

```
/admin/config (Super Admin)
  ├── Config List (main page, default landing)
  │     ├── Search bar (filter by key or description)
  │     ├── Collapsible category sections (12 categories)
  │     │     └── Config rows: key, value, type, critical badge, [Edit]
  │     ├── Bootstrap configs info banner
  │     ├── Cache status + [Invalidate Cache] button
  │     ├── Header links: [History] [Approvals (N)]
  │     └── Inline edit form (expands on [Edit] click)
  │
  ├── Change History (/admin/config/history)
  │     ├── Filterable change log table
  │     │     └── Columns: Timestamp, Key, Old→New, Changed By, Reason
  │     ├── Filters: date range, category, key search
  │     ├── [Rollback] action per entry (if not already rolled back)
  │     └── Rollback confirmation modal
  │
  └── Approval Queue (/admin/config/approvals)
        ├── Pending Requests section
        │     └── Cards: config key, current→proposed, requester, reason, expires in
        ├── [Approve] / [Reject] actions per request
        ├── Reject reason modal
        └── Recent Decisions section (approved/rejected/expired)
```

### Screen Summary

| # | Screen | Route | Notes |
|---|--------|-------|-------|
| 1 | Config List | `/admin/config` | Main page with grouped configs + inline edit |
| 2 | Change History | `/admin/config/history` | Filterable log with rollback |
| 3 | Approval Queue | `/admin/config/approvals` | Pending requests + recent decisions |
| 4 | Rollback Confirmation | (modal overlay) | Confirm rollback with reason |
| 5 | Reject Reason | (modal overlay) | Required reason for rejecting request |

---

## Config Row Indicators

| Indicator | Condition | Display |
|-----------|-----------|---------|
| Critical badge | `is_critical = true` | Orange "Critical" badge next to key |
| Sensitive mask | `is_sensitive = true` | Value shows `••••••••` with [Reveal] toggle |
| Restart required | `requires_restart = true` | Yellow "Restart Required" chip next to value |
| Pending approval | Active PENDING request exists | Blue "Pending Approval" badge, [Edit] disabled |
| Recently changed | `updated_at` within last 24h | Subtle highlight on row |

---

## Cross-Reference Index

| Config Category | Referenced By Plan | Example Configs |
|----------------|-------------------|-----------------|
| Authentication | `authentication` | `auth.jwt.access_token_ttl_minutes`, `auth.lockout.max_failed_attempts` |
| Tenant Management | `tenant-management` | `tenant.suspension.grace_period_days` |
| Billing & Credits | `billing-credits` | `billing.credits.starter_monthly`, `billing.credits.pro_monthly` |
| Scanning & Workflows | `scanning-workflows` | `scanning.job.timeout_hours`, `scanning.queue.max_depth` |
| Feature Flags | `feature-flags-access-control` | `feature_flags.cache.ttl_minutes` |
| Compliance | `compliance-engine` | `compliance.framework.default_grace_period_days` |
| CVE Monitoring | `cve-monitoring` | `cve.feed.sync_interval_hours` |
| Integrations | `integrations` | `integrations.webhook.timeout_seconds` |
| Platform Operations | `super-admin-operations` | `admin.impersonation.session_ttl_minutes` |
