# Tenant Management — Frontend Plan

Scope: Full tenant lifecycle including dashboard, domain management, tenant settings, billing overview, provisioning status, free-tier enforcement, and super admin tenant operations (list, detail, suspend, reactivate, delete, impersonate).

## Documentation Index

| File | Description | Audience |
|------|-------------|----------|
| [user-flows.md](./user-flows.md) | User journey flowcharts + branching logic | Product, QA, Frontend |
| [screens-wireframes.md](./screens-wireframes.md) | ASCII wireframes for all 13 screens | UI/UX, Frontend |
| [implementation-guide.md](./implementation-guide.md) | State management, API integration, components | Frontend devs |
| [reference.md](./reference.md) | Error codes, validation rules, security | Frontend devs |

---

## Business Rule Coverage

| BR Rule | Name | Frontend Feature |
|---------|------|-----------------|
| BR-TNT-001 | Tenant Creation & Provisioning | Registration flow (covered in auth plan) + provisioning status page |
| BR-TNT-002 | Slug Generation | Registration form tenant name validation (inline slug preview) |
| BR-TNT-003 | One Tenant per User | No frontend impact — enforced by backend. No UI for joining/switching tenants |
| BR-TNT-004 | Tenant Context Resolution | API client tenant header injection (middleware) |
| BR-TNT-005 | Provisioning Retry | Provisioning status page — polling + error state |
| BR-TNT-006 | Tenant Suspension | Admin tenant detail -> suspend action; Suspended status page for tenant users |
| BR-TNT-007 | Tenant Reactivation | Admin tenant detail -> reactivate action |
| BR-TNT-008 | Tenant Deletion Request | Settings -> delete account modal; Admin -> approve/deny deletion |
| BR-TNT-009 | Data Retention & Cleanup | No direct frontend (backend job). Deactivated status page shown to users |
| BR-TNT-010 | Tenant Status States | Status badges throughout UI; blocked-state pages |
| BR-TNT-011 | Subscription Expiry Downgrade | Free-tier banner + disabled action buttons; billing overview |
| BR-TNT-012 | Free Tier Access Control | Global upgrade banner; disabled CTAs on domains, scans, compliance, schedules, integrations |
| BR-TNT-013 | Domain Ownership Verification | [POST-MVP] Domain verification flow — placeholder in domain list |
| BR-TNT-014 | Tenant Impersonation | Admin tenant detail -> impersonate (new tab); impersonation banner component |

---

## User Roles

| Role | Tenant-Owner Screens | Admin Screens |
|------|---------------------|---------------|
| `TENANT_OWNER` | Dashboard, Domains, Settings, Billing, Billing Plans, Provisioning Status | — |
| `SUPER_ADMIN` | — | Tenant List, Tenant Detail (suspend/reactivate/delete/impersonate) |
| `SUPER_ADMIN` (impersonating) | Dashboard, Domains, Settings, Billing (read-only with impersonation banner) | — |

---

## Tenant State Machine

```
                    ┌──────────────────────────────────────────────────────────────┐
                    │                    TENANT STATE MACHINE                      │
                    └──────────────────────────────────────────────────────────────┘

     ┌───────────────┐
     │  (user signs  │
     │    up)        │
     └───────┬───────┘
             │ Registration complete
             ▼
     ┌───────────────┐         ┌───────────────┐
     │ PROVISIONING  │────────►│    ACTIVE      │
     │               │  DB     │                │
     │ (polling page)│ ready   │ (full access)  │
     └───────┬───────┘         └──┬──────┬──────┘
             │                    │      │
             │ Provisioning       │      │ Super admin
             │ fails (3 retries)  │      │ suspends
             ▼                    │      ▼
     ┌───────────────┐            │  ┌───────────────┐
     │    FAILED     │            │  │  SUSPENDED     │
     │               │            │  │                │
     │ (error page)  │            │  │ (blocked page) │
     └───────────────┘            │  └───────┬───────┘
                                  │          │
                     Super admin  │          │ Super admin
                     approves     │          │ reactivates
                     deletion     │          │
                                  ▼          ▼
                           ┌───────────────┐
                           │  DEACTIVATED  │
                           │  (terminal)   │
                           │ (blocked page)│
                           └───────────────┘
```

## State Transitions Table

| Current State | Action | Next State | Trigger | Frontend Impact |
|--------------|--------|------------|---------|-----------------|
| _(new)_ | Registration complete | `PROVISIONING` | Self-service | Redirect to provisioning status page |
| `PROVISIONING` | DB clone + migrations succeed | `ACTIVE` | System | Auto-redirect to dashboard (polling detects) |
| `PROVISIONING` | Max retries exhausted | `PROVISIONING` (DB: `FAILED`) | System | Polling page shows error state with support CTA |
| `ACTIVE` | Super admin suspends | `SUSPENDED` | Super Admin | User's session invalidated -> login shows suspended message |
| `SUSPENDED` | Super admin reactivates | `ACTIVE` | Super Admin | User can log in again |
| `ACTIVE` / `SUSPENDED` | Deletion approved | `DEACTIVATED` | Super Admin | Sessions invalidated -> login shows deactivated message |

---

## Screen Navigation Map

```
                                    ┌──────────────┐
                                    │     ROOT     │
                                    │   /          │
                                    └──────┬───────┘
                          ┌────────────────┼────────────────────┐
                          │                │                    │
                          ▼                ▼                    ▼
                   ┌────────────┐   ┌────────────┐      ┌─────────────┐
                   │PROVISIONING│   │  APP SHELL  │      │ ADMIN SHELL │
                   │/auth/      │   │ /(app)/     │      │ /(admin)/   │
                   │provisioning│   └──────┬──────┘      └──────┬──────┘
                   └────────────┘          │                    │
                                    ┌──────┼──────┬──────┐      │
                                    │      │      │      │      │
                                    ▼      ▼      ▼      ▼      ▼
                             ┌────────┐┌────────┐┌────────┐┌────────────┐
                             │DASHBRD ││DOMAINS ││SETTINGS││TENANT LIST │
                             │/(app)/ ││/(app)/ ││/(app)/ ││/(admin)/   │
                             │dashbrd ││domains ││settings││admin/      │
                             └────────┘└───┬────┘└────────┘│tenants     │
                                           │               └──────┬─────┘
                                           ▼                      │
                                    ┌────────────┐                ▼
                                    │DOMAIN DTL  │         ┌────────────┐
                                    │/(app)/     │         │TENANT DTL  │
                                    │domains/[id]│         │/(admin)/   │
                                    └────────────┘         │admin/      │
                                                           │tenants/[id]│
                             ┌────────┐┌────────┐          └────────────┘
                             │BILLING ││BILLING │
                             │/(app)/ ││/(app)/ │
                             │billing ││billing/│
                             └────────┘│plans   │
                                       └────────┘
```

---

## Screens Summary

| # | Screen | Route | Access | Notes |
|---|--------|-------|--------|-------|
| 1 | Provisioning Status | `/auth/provisioning` | Temp token | Polling page after registration |
| 2 | Dashboard | `/(app)/dashboard` | Authenticated | Overview cards, recent activity |
| 3 | Domains List | `/(app)/domains` | Authenticated | Domain list + inline add form |
| 4 | Domain Detail | `/(app)/domains/[id]` | Authenticated | Domain info, scan history |
| 5 | Tenant Settings | `/(app)/settings` | Authenticated | Tenant name, delete account |
| 6 | Billing Overview | `/(app)/billing` | Authenticated | Current plan, credits, usage |
| 7 | Plan Comparison | `/(app)/billing/plans` | Authenticated | Plan features comparison + upgrade |
| 8 | Suspended Page | `/auth/suspended` | Public | Shown when suspended tenant user tries to log in |
| 9 | Deactivated Page | `/auth/deactivated` | Public | Shown when deactivated tenant user tries to log in |
| 10 | Provisioning Failed | (state of #1) | Temp token | Error state of provisioning page |
| 11 | Admin Tenant List | `/(admin)/admin/tenants` | Super Admin | Paginated, searchable tenant list |
| 12 | Admin Tenant Detail | `/(admin)/admin/tenants/[id]` | Super Admin | View/suspend/reactivate/delete/impersonate |
| 13 | Impersonation Banner | (global component) | Super Admin (impersonating) | Persistent banner during impersonation |

---

**Document Version:** 1.0
**Last Updated:** March 2026
**Based On:** `docs/plans/business-rules/02-tenant-management.md`
