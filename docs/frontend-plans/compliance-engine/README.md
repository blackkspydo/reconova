# Compliance Engine — Frontend Plan

Scope: Compliance framework selection, automated assessment viewing, per-control drill-down, report generation (PDF/HTML), historical trends, framework requests (tenant), and full framework/control/mapping management with tier access and request review (super admin).

**Based on:** `docs/plans/business-rules/06-compliance-engine.md` (BR-COMP-001 — BR-COMP-011)
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
| BR-COMP-001 | Tier Access Configuration | Admin: per-framework plan access toggles |
| BR-COMP-002A | Create Framework | Admin: framework creation form |
| BR-COMP-002B | Publish Framework | Admin: publish action with validation checks |
| BR-COMP-002C | Deprecate Framework | Admin: deprecate action with confirmation |
| BR-COMP-002D | Delete Draft Framework | Admin: delete draft with optional reason |
| BR-COMP-002E | Reactivate Framework | Admin: reactivate deprecated framework |
| BR-COMP-002F | Add Control-Check Mapping | Admin: mapping editor with pass condition config |
| BR-COMP-003 | Assessment Execution | Tenant: auto-generated assessments appear after scan completes |
| BR-COMP-005 | Control Result Evaluation | Tenant: per-control status display (PASS/FAIL/PARTIAL/NOT_ASSESSED) |
| BR-COMP-008 | Report Generation | Tenant: on-demand PDF/HTML download (Pro+ gated) |
| BR-COMP-010 | Historical Trends | Tenant: trend chart per framework (≥3 assessments required) |
| BR-COMP-011A | Submit Framework Request | Tenant: request form on compliance dashboard |
| BR-COMP-011B | Review Framework Request | Admin: request review with status transitions + admin notes |

---

## User Roles

| Role | Screens Accessible | Key Actions |
|------|-------------------|-------------|
| **Tenant Owner** | Compliance dashboard, assessment detail, report download, framework requests | Select/deselect frameworks, view assessments, download reports, submit requests |
| **Tenant Member** | Compliance dashboard (read-only), assessment detail | View assessments and scores (cannot select frameworks or submit requests) |
| **Super Admin** | Framework management, control editor, mapping editor, tier access, request review | Full CRUD on frameworks/controls/mappings, configure plan access, review requests |

---

## Framework Lifecycle State Machine

```
                    ┌───────────────┐
   Create           │     DRAFT     │◄─────────────────┐
  ─────────────────►│               │                   │
                    └──┬────────┬───┘                   │
                       │        │                       │
            Publish    │        │ Delete                │
            (valid)    │        │ (admin)               │
                       ▼        ▼                       │
              ┌──────────────┐  (removed)               │
              │   ACTIVE     │                          │
              │              │◄──────────┐              │
              └──────┬───────┘           │              │
                     │              Reactivate          │
              Deprecate│                 │              │
                     │                   │              │
                     ▼                   │              │
              ┌──────────────┐           │              │
              │ DEPRECATED   │───────────┘              │
              │              │                          │
              └──────────────┘                          │
                                                        │
    Public Preview (DRAFT + is_public=true):            │
      Shown as "Coming Soon" to tenants                 │
      Name + description only, not selectable           │
```

### State Transitions

| Current State | Action | Next State | Conditions | Who |
|--------------|--------|------------|------------|-----|
| — | Create | DRAFT | Super admin only | Super Admin |
| DRAFT | Publish | ACTIVE | ≥1 control with ≥1 mapping, ≥1 plan tier enabled | Super Admin |
| DRAFT | Delete | (removed) | If is_public=true, reason required (≥10 chars) | Super Admin |
| ACTIVE | Deprecate | DEPRECATED | Publishes changelog entry | Super Admin |
| DEPRECATED | Reactivate | ACTIVE | Publishes changelog entry | Super Admin |

### Public Preview Rules

| Status | is_public | Tenant Visibility |
|--------|-----------|-------------------|
| DRAFT | false | Hidden |
| DRAFT | true | "Coming Soon" — name + description only, not selectable |
| ACTIVE | N/A | Fully visible (tier-gated) |
| DEPRECATED | N/A | Visible only to tenants with existing selections |

---

## Control Result Status Model

```
┌───────────────────────────────────────────────────┐
│              Control Evaluation                    │
│                                                   │
│  For each control's check mappings:               │
│    ├─ Has scan data? → Evaluate pass condition    │
│    └─ No scan data? → NOT_ASSESSED                │
│                                                   │
│  Precedence: FAIL > PARTIAL > PASS > NOT_ASSESSED │
│                                                   │
│  ┌─────────┐  All pass, all data                 │
│  │  PASS   │  ───────────────────►  Green badge   │
│  └─────────┘                                      │
│  ┌─────────┐  ≥1 mapping fails                   │
│  │  FAIL   │  ───────────────────►  Red badge     │
│  └─────────┘                                      │
│  ┌─────────┐  All pass, some missing data         │
│  │ PARTIAL │  ───────────────────►  Yellow badge   │
│  └─────────┘                                      │
│  ┌──────────────┐  No data for any mapping        │
│  │ NOT_ASSESSED │  ──────────────►  Grey badge     │
│  └──────────────┘                                  │
└───────────────────────────────────────────────────┘
```

---

## Assessment Score Model

```
┌──────────────────────────────────────────────┐
│           Overall Compliance Score           │
│                                              │
│  score = (passing / assessed) × 100          │
│                                              │
│  passing_controls:  PASS count               │
│  assessed_controls: PASS + FAIL + PARTIAL    │
│  total_controls:    all controls             │
│                                              │
│  NOT_ASSESSED excluded from score calc       │
│  If assessed = 0 → score = 0.00             │
│                                              │
│  Score color:                                │
│    90-100  Green   "Excellent"               │
│    70-89   Yellow  "Good"                    │
│    50-69   Orange  "Needs Improvement"       │
│    0-49    Red     "Critical"                │
│                                              │
└──────────────────────────────────────────────┘
```

---

## Framework Request Lifecycle

```
   Submit           ┌──────────────┐
  ─────────────────►│  SUBMITTED   │
                    └──────┬───────┘
                           │
                    Review │ (admin_notes required)
                           ▼
                    ┌──────────────┐
                    │   REVIEWED   │
                    └──┬───────┬───┘
                       │       │
              Accept   │       │ Reject
                       ▼       ▼
              ┌──────────┐  ┌──────────┐
              │ ACCEPTED │  │ REJECTED │
              └──────────┘  └──────────┘
              (terminal)    (terminal)
```

Note: SUBMITTED can also transition directly to ACCEPTED or REJECTED.

---

## Screen Navigation Map

```
/compliance (Tenant)
  ├── Compliance Dashboard (default)
  │     ├── Framework Selection section
  │     │     ├── Available frameworks (ACTIVE, plan-accessible)
  │     │     ├── "Coming Soon" frameworks (DRAFT, is_public=true)
  │     │     └── [Select] / [Deselect] toggles
  │     │
  │     ├── Assessment Scores section
  │     │     ├── Per-framework score card with trend sparkline
  │     │     └── Click card ──► /compliance/frameworks/{id}
  │     │
  │     ├── Framework Requests section
  │     │     ├── Existing requests with status badges
  │     │     └── [Request a Framework] ──► Request submission modal
  │     │
  │     └── [Download Report] per framework ──► PDF/HTML download
  │
  ├── Framework Detail (/compliance/frameworks/{id})
  │     ├── Framework info + latest score
  │     ├── Assessment history list
  │     ├── Trend chart (if ≥3 assessments)
  │     └── Click assessment ──► /compliance/assessments/{id}
  │
  └── Assessment Detail (/compliance/assessments/{id})
        ├── Score summary + metadata
        ├── Controls grouped by category
        │     ├── Status filter: [All] [Pass] [Fail] [Partial] [Not Assessed]
        │     └── Expandable control rows → evidence + recommendations
        └── [Download Report] ──► PDF/HTML download

/admin/compliance (Super Admin)
  ├── Framework Management (default tab)
  │     ├── Framework list with status badges
  │     ├── [Create Framework] ──► Creation form
  │     ├── Click framework ──► /admin/compliance/frameworks/{id}
  │     └── Actions: Publish, Deprecate, Reactivate, Delete
  │
  ├── Framework Detail (/admin/compliance/frameworks/{id})
  │     ├── Framework metadata (editable for DRAFT)
  │     ├── Controls tab ──► Control list with CRUD
  │     │     └── Click control ──► Control detail with mappings
  │     ├── Tier Access tab ──► Plan toggle matrix
  │     └── Actions: Publish, Deprecate, etc.
  │
  └── Framework Requests (/admin/compliance/requests)
        ├── Request list with status filters
        └── Click request ──► Review modal (notes + status transition)
```

### Screen Summary

| # | Screen | Route | Access | Notes |
|---|--------|-------|--------|-------|
| 1 | Compliance Dashboard | `/compliance` | Tenant Owner (edit), Members (view) | Framework selection + scores + requests |
| 2 | Framework Detail (tenant) | `/compliance/frameworks/{id}` | Tenant Owner / Members | Assessment history + trend chart |
| 3 | Assessment Detail | `/compliance/assessments/{id}` | Tenant Owner / Members | Per-control results + report download |
| 4 | Admin Framework List | `/admin/compliance` | Super Admin | CRUD + lifecycle actions |
| 5 | Admin Framework Detail | `/admin/compliance/frameworks/{id}` | Super Admin | Controls + mappings + tier access |
| 6 | Admin Framework Requests | `/admin/compliance/requests` | Super Admin | Review + status transitions |

---

## Feature Flag Integration

| Flag | Effect |
|------|--------|
| `compliance_checks` | Entire compliance module gated. If disabled: `<FeatureGate>` shows locked placeholder. No assessments generated. |
| `compliance_reports` | Report download button gated. If disabled: button shows `<LockedBadge>` "Requires Pro". Assessments still viewable. |

See [Feature Flags & Access Control plan](../feature-flags-access-control/README.md) for gating component specs.

---

## Banners & Global States

| Condition | Banner/Indicator | Actions |
|-----------|-----------------|---------|
| compliance_checks disabled (plan) | `<FeatureGate>` locked placeholder on `/compliance` | [Upgrade Plan] CTA |
| compliance_checks disabled (operational) | `<OperationalBanner>` on `/compliance` | None (maintenance) |
| compliance_reports disabled | Report download buttons show lock badge | [Upgrade to Pro] tooltip |
| No frameworks selected | Empty state on assessment scores section | "Select a framework to get started" |
| Framework deprecated (with existing data) | "Deprecated" badge on framework card | Assessment data still viewable |
| Pending downgrade affecting compliance | Info banner on dashboard | "Compliance features will be locked on {date}" |
