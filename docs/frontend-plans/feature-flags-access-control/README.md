# Feature Flags & Access Control — Frontend Plan

Scope: Feature flag management (super admin), tenant feature visibility, plan-based access gating, operational flag controls, and reusable upgrade/locked-feature UI patterns.

**Based on:** `docs/plans/business-rules/05-feature-flags-access-control.md` (BR-FLAG-001 — BR-FLAG-007)
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
| BR-FLAG-001 | Feature Flag Evaluation (Single) | Feature store: fetch + cache per-feature status on app load |
| BR-FLAG-001B | Bulk Feature Evaluation | `GET /api/features` call on app init → populate feature store |
| BR-FLAG-002 | Operational Flags | Admin: operational flag toggle panel with confirmation modal |
| BR-FLAG-003 | Tenant Feature Overrides | Admin: override CRUD per tenant (create, edit, delete with reason) |
| BR-FLAG-004 | Plan-to-Feature Mapping & Sync | Tenant: Plan & Features page showing tier matrix; downgrade notices |
| BR-FLAG-005 | Caching Strategy | Frontend: feature store refresh on plan change / override update |
| BR-FLAG-006 | API & Scan Enforcement | Module gating: locked indicators, disabled buttons, filtered steps |
| BR-FLAG-007 | Feature Visibility API | Tenant: feature list with enabled/reason/required_tier display |

---

## User Roles

| Role | Screens Accessible | Key Actions |
|------|-------------------|-------------|
| **Tenant Owner** | Plan & Features page, upgrade modal | View feature availability, see required tiers, navigate to billing |
| **Tenant Member** | Module-level gating indicators | See locked features, see upgrade prompts (read-only) |
| **Super Admin** | Feature management, operational flags, override management | Toggle operational flags, create/edit/delete overrides, view audit trail |

---

## Feature Availability State Machine

```
                          ┌───────────────────┐
                          │    AVAILABLE       │
         Plan includes    │  (feature active)  │◄──────────┐
        ─────────────────►│                    │            │
                          └────┬──────────┬────┘            │
                               │          │                 │
                  Operational   │          │ Plan            │ Operational
                  flag off      │          │ downgrade       │ flag on /
                               │          │                 │ Plan upgrade
                               ▼          ▼                 │
                ┌──────────────────┐  ┌───────────────┐     │
                │  DISABLED        │  │   LOCKED      │     │
                │  (operational)   │  │   (plan tier) │─────┘
                │                  │  │               │  Upgrade
                └──────────────────┘  └───────┬───────┘
                  Affects ALL tenants         │
                  No upgrade path             │ User clicks
                  "Temporarily unavailable"   │ upgrade CTA
                                              ▼
                                    ┌───────────────────┐
                                    │  UPGRADE MODAL    │
                                    │  → View Plans     │
                                    │  → /settings/     │
                                    │    billing/plans  │
                                    └───────────────────┘
```

### State Transitions

| Current State | Condition | Next State | UI Effect |
|--------------|-----------|------------|-----------|
| AVAILABLE | Operational flag disabled | DISABLED | Feature hidden/greyed, "Temporarily unavailable" message |
| AVAILABLE | Plan downgrade removes feature | LOCKED | Lock icon, upgrade CTA modal |
| LOCKED | Tenant upgrades plan | AVAILABLE | Feature unlocked, full access |
| LOCKED | Super admin creates override (enabled) | AVAILABLE | Feature unlocked via override |
| AVAILABLE | Super admin creates override (disabled) | LOCKED | Feature locked via override |
| DISABLED | Operational flag re-enabled | (previous state) | Restored to plan-based state |
| Any | Operational flag disabled | DISABLED | Operational always wins |

### Evaluation Precedence (Frontend Display Logic)

```
1. Operational flag OFF?  → DISABLED (no upgrade path shown)
2. Override exists?       → Use override value
3. Plan includes feature? → AVAILABLE / LOCKED
4. No data?              → LOCKED (fail-safe)
```

---

## Screen Navigation Map

```
/settings/features
  └── Plan & Features (Tenant Owner)
        ├── Feature list grouped by module
        │     ├── Scanning features (7)
        │     ├── Compliance features (2)
        │     ├── Integration features (4)
        │     ├── Notification features (4)
        │     └── Monitoring features (1)
        ├── Each feature shows: name, status badge, required tier
        └── [Upgrade Plan] ──► /settings/billing/plans

/admin/features (Super Admin)
  ├── Feature Overview (default tab)
  │     ├── All 18 subscription flags with per-tenant status
  │     └── All 5 operational flags with global toggles
  │
  ├── Operational Flags (/admin/features/operational)
  │     ├── Toggle switches with confirmation modal
  │     └── Current status + last changed info
  │
  └── Tenant Overrides (/admin/features/overrides)
        ├── Tenant search/select
        ├── Override list for selected tenant
        ├── [Add Override] ──► Override creation modal
        ├── [Edit Override] ──► Override edit modal
        └── [Delete Override] ──► Delete confirmation modal

Cross-Module Gating (integrated into other modules)
  ├── Scan Creation ──► steps filtered by feature flags
  ├── Compliance Module ──► locked if compliance_checks disabled
  ├── CVE Monitoring ──► locked if cve_monitoring disabled
  ├── Integration Setup ──► locked per-integration flag
  └── Notification Config ──► locked per-channel flag
```

### Screen Summary

| # | Screen | Route | Access | Notes |
|---|--------|-------|--------|-------|
| 1 | Plan & Features | `/settings/features` | Tenant Owner (view), Members (view) | Feature visibility by module |
| 2 | Upgrade Modal | (overlay) | All authenticated | Shown when locked feature clicked |
| 3 | Admin Feature Overview | `/admin/features` | Super Admin | All flags, status overview |
| 4 | Admin Operational Flags | `/admin/features/operational` | Super Admin | Global toggle controls |
| 5 | Admin Tenant Overrides | `/admin/features/overrides` | Super Admin | Per-tenant override CRUD |

---

## Banners & Global States

| Condition | Banner/Indicator | Actions |
|-----------|-----------------|---------|
| Operational flag disabled | Inline "Temporarily unavailable" badge on affected modules | None (no user action possible) |
| Feature locked by plan | Lock icon + "Requires {tier}" badge | Click → Upgrade modal → [View Plans] |
| Plan downgrade pending | "Some features will be locked on {date}" notice on Plan & Features page | [View Changes] |
| Feature unlocked via override | "Enabled by administrator" subtle badge (admin view only) | None |

---

## Cross-Cutting Gating Patterns

This plan defines **reusable gating components** used across all modules:

| Component | Purpose | Used By |
|-----------|---------|---------|
| `FeatureGate` | Wraps content; shows locked state if feature disabled | All gated modules |
| `UpgradeModal` | Modal with feature info + tier requirement + "View Plans" CTA | Any locked feature interaction |
| `LockedBadge` | Inline lock icon + "Requires {tier}" text | Nav items, buttons, cards |
| `OperationalBanner` | "Temporarily unavailable" notice | Module headers when operational flag off |

See [implementation-guide.md](./implementation-guide.md) for component specs.
