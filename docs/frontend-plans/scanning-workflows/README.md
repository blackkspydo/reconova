# Scanning & Workflows — Frontend Plan

Scope: Domain management, scan job creation and monitoring, workflow templates, custom workflows, scan results viewing, scan scheduling, and super admin concurrent scan limit configuration.

**Based on:** `docs/plans/business-rules/04-scanning-workflows.md` (BR-SCAN-001 — BR-SCAN-021)
**Last updated:** 2026-03-10

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

| BR Code | Rule Name | Frontend Feature |
|---------|-----------|------------------|
| BR-SCAN-001 | Add Domain | Domain list: add domain form with validation |
| BR-SCAN-002 | Domain Validation | Inline validation (no IPs, no URLs, no subdomains) |
| BR-SCAN-003 | Domain Uniqueness | Duplicate detection with error message |
| BR-SCAN-004 | Delete Domain | Delete button with active-scan guard |
| BR-SCAN-005 | Create Scan Job | Scan creation form: domain + workflow + credit estimate |
| BR-SCAN-006 | One-Active-Scan-Per-Domain | Error message when domain has active scan |
| BR-SCAN-007 | Scan Job States | Scan list status badges, progress pipeline |
| BR-SCAN-008 | Cancel Scan | Cancel button with refund preview |
| BR-SCAN-009 | Scan Timeouts | Timeout status display, stale scan indicators |
| BR-SCAN-010 | Step Retry | Retry indicator in step progress pipeline |
| BR-SCAN-011 | Feature Flag Enforcement | Filtered steps indicator, upgrade prompts |
| BR-SCAN-012 | Results Persistence | Tabbed results view per check type |
| BR-SCAN-013 | Results Immutability | Read-only results display (no edit/delete) |
| BR-SCAN-014 | System Workflow Templates | Workflow template cards (read-only) |
| BR-SCAN-015 | Create Custom Workflow | Custom workflow builder form (Pro+) |
| BR-SCAN-016 | Step Execution | Step progress pipeline with status per step |
| BR-SCAN-017 | Scheduled Scans Gating | Feature gate with upgrade prompt (Pro+) |
| BR-SCAN-018 | Create Scan Schedule | Schedule creation form with cron builder |
| BR-SCAN-019 | Schedule Min Interval | Cron validation (min 24h) |
| BR-SCAN-020 | Schedule Limit | Schedule count indicator, limit warning |
| BR-SCAN-021 | Schedule Auto-Disable | Disabled schedule indicators, re-enable flow |

---

## User Roles

| Role | Screens Accessible | Key Actions |
|------|-------------------|-------------|
| **Tenant Owner** | All domain, scan, workflow, schedule screens | Full CRUD on domains, scans, workflows, schedules |
| **Tenant Member** | All screens (read-only for mutations) | View domains, scans, results |
| **Super Admin** | Concurrent scan limit config | Configure per-tenant scan limits |

---

## Scan Job State Machine

```
               ┌──────────────┐
  Scan created │    QUEUED    │
 ─────────────►│              │
               └──────┬───────┘
                      │ Worker picks up
                      ▼
               ┌──────────────┐
               │   RUNNING    │
               └──┬──┬──┬──┬─┘
                  │  │  │  │
        All steps │  │  │  │ Cancelled /
        succeed   │  │  │  │ 4h timeout
                  ▼  │  │  ▼
         ┌──────────┐│  │┌──────────────┐
         │COMPLETED ││  ││  CANCELLED   │
         └──────────┘│  │└──────────────┘
                     │  │
        Some steps   │  │ All steps fail /
        fail, some   │  │ root step fails
        succeed      │  │
                     ▼  ▼
              ┌──────────┐┌──────────┐
              │ PARTIAL  ││  FAILED  │
              └──────────┘└──────────┘
```

### State Transitions

| Current State | Action | Next State | Trigger | Who |
|--------------|--------|------------|---------|-----|
| (new) | Create scan | QUEUED | Tenant owner starts scan | Tenant Owner |
| QUEUED | Worker picks up | RUNNING | Background worker | System |
| QUEUED | Cancel | CANCELLED | Tenant cancels, full credit refund | Tenant Owner |
| QUEUED | Queue timeout (1h) | FAILED | No worker pickup, full refund | System |
| RUNNING | All steps succeed | COMPLETED | Step execution | System |
| RUNNING | Some steps fail | PARTIAL | Mixed results, partial refund | System |
| RUNNING | All/root step fails | FAILED | No usable results, refund unexecuted | System |
| RUNNING | Cancel | CANCELLED | Tenant cancels, current step completes, refund rest | Tenant Owner |
| RUNNING | 4-hour timeout | CANCELLED | Hard timeout, refund unexecuted | System |
| RUNNING | Stale (30min no progress) | FAILED | Health check, refund unexecuted | System |

**Terminal states:** COMPLETED, PARTIAL, FAILED, CANCELLED

---

## Step Execution Pipeline

```
┌────────────┐   ┌────────────┐   ┌────────────┐   ┌────────────┐   ┌────────────┐
│ subdomain  │──►│ port_scan  │──►│ tech_detect │──►│ screenshot │──►│ vuln_scan  │
│   _enum    │   │            │   │            │   │            │   │            │
│  (root)    │   │ depends on │   │ depends on │   │ depends on │   │ depends on │
│            │   │ subdomain  │   │ subdomain  │   │ subdomain  │   │ subdomain  │
└────────────┘   └────────────┘   └────────────┘   └────────────┘   └────────────┘
```

### Step Dependencies

| Step | Depends On | If Dependency Failed |
|------|-----------|---------------------|
| `subdomain_enum` | None (root) | N/A |
| `port_scan` | `subdomain_enum` | Skipped + refunded |
| `tech_detect` | `subdomain_enum` | Skipped + refunded |
| `screenshot` | `subdomain_enum` | Skipped + refunded |
| `vuln_scan` | `subdomain_enum` | Skipped + refunded |
| `compliance_check` | `vuln_scan` | Skipped + refunded |
| `shodan_lookup` | `subdomain_enum` | Skipped + refunded |
| `securitytrails_lookup` | None (root domain) | N/A |
| `censys_lookup` | `subdomain_enum` | Skipped + refunded |
| `custom_connector` | Configurable | Depends on config |

### Step Status Values

| Status | Meaning | Visual |
|--------|---------|--------|
| PENDING | Not yet started | Grey circle |
| RUNNING | Currently executing | Blue spinner |
| RETRYING | Failed, retrying (attempt 2 or 3) | Orange spinner |
| COMPLETED | Succeeded | Green check |
| FAILED | All attempts exhausted | Red X |
| SKIPPED | Dependency failed or feature-gated | Grey dash |
| CANCELLED | Scan cancelled before this step | Grey X |

---

## Concurrent Scan Limits

| Tier | Max Concurrent | One-Per-Domain |
|------|---------------|----------------|
| Starter | 1 | Yes |
| Pro | 3 | Yes |
| Enterprise | 10 (configurable) | Yes |

---

## Screen Navigation Map

```
/domains
  ├── Domain List (default)
  │     ├── [Add Domain] ──► Add domain modal
  │     ├── [Domain Name] ──► /domains/{id}
  │     └── [Delete] ──► Delete confirmation modal
  │
  └── Domain Details (/domains/{id})
        ├── Overview tab: subdomains, ports, technologies
        ├── Scan History tab: scans run against this domain
        └── [Start Scan] ──► /scans/new?domain={id}

/scans
  ├── Scan Jobs (default, /scans)
  │     ├── [New Scan] ──► /scans/new
  │     ├── [Scan Row] ──► /scans/{id}
  │     └── [Cancel] ──► Cancel confirmation modal
  │
  ├── Scan Details (/scans/{id})
  │     ├── Step progress pipeline
  │     ├── Results tabs: Subdomains | Ports | Technologies | Vulns | Screenshots | ...
  │     └── [Cancel Scan] (if QUEUED/RUNNING)
  │
  ├── New Scan (/scans/new)
  │     ├── Domain picker
  │     ├── Workflow picker
  │     ├── Credit cost estimate
  │     └── [Start Scan]
  │
  ├── Workflows (/scans/workflows)
  │     ├── System templates (read-only cards)
  │     ├── Custom workflows list [Pro+]
  │     ├── [Create Workflow] ──► /scans/workflows/new
  │     └── [Workflow] ──► /scans/workflows/{id}
  │
  └── Schedules (/scans/schedules) [Pro+]
        ├── Schedule list with status
        ├── [Create Schedule] ──► /scans/schedules/new
        ├── [Enable/Disable] toggle
        └── [Delete] ──► Delete confirmation modal

/admin/scans (Super Admin)
  └── Concurrent Limits (/admin/scans/limits)
        └── Per-tenant limit configuration (Enterprise tenants)
```

### Screen Summary

| # | Screen | Route | Access | Notes |
|---|--------|-------|--------|-------|
| 1 | Domain List | `/domains` | All members | Add, delete, view domains |
| 2 | Domain Details | `/domains/{id}` | All members | Subdomains, ports, tech, scan history |
| 3 | Scan Jobs List | `/scans` | All members | List scans with status, filter, cancel |
| 4 | Scan Details | `/scans/{id}` | All members | Step pipeline + tabbed results |
| 5 | New Scan | `/scans/new` | Tenant Owner | Domain + workflow + credit estimate |
| 6 | Workflow List | `/scans/workflows` | All members | System + custom workflows |
| 7 | Custom Workflow Builder | `/scans/workflows/new` | Tenant Owner (Pro+) | Step picker, ordering |
| 8 | Workflow Details | `/scans/workflows/{id}` | All members | Steps, description |
| 9 | Schedule List | `/scans/schedules` | Tenant Owner (Pro+) | Manage schedules |
| 10 | New Schedule | `/scans/schedules/new` | Tenant Owner (Pro+) | Domain + workflow + cron |
| 11 | Admin Concurrent Limits | `/admin/scans/limits` | Super Admin | Enterprise per-tenant config |

---

## Feature Gating Summary

| Feature | Tiers | Gating Behavior |
|---------|-------|-----------------|
| Add domains | Starter+ | Free tier: disabled with upgrade CTA |
| Basic scans (subdomain, port, tech, screenshot) | Starter+ | Free tier: disabled |
| Vuln scan, compliance | Pro+ | Starter: steps filtered, badge shown |
| Custom workflows | Pro+ | Starter: hidden or upgrade prompt |
| Scheduled scans | Pro+ | Starter: hidden or upgrade prompt |
| Shodan/SecurityTrails | Pro+ | Starter: steps filtered |
| Censys, custom connectors | Enterprise | Pro: steps filtered |
