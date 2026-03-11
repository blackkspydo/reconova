# Billing & Credits — Frontend Plan

Scope: Subscription management, credit system, credit pack purchases, transaction history, and super admin billing operations (credit adjustments, scan pricing configuration).

**Based on:** `docs/plans/business-rules/03-billing-credits.md` (BR-BILL-001 — BR-BILL-018)
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
| BR-BILL-001 | Subscription Plan Tiers | Plan comparison page, feature matrix display |
| BR-BILL-002 | Plan Feature Comparison | Plan comparison table with tier columns |
| BR-BILL-003 | Create Subscription | Plan selection → Stripe checkout flow |
| BR-BILL-004 | Plan Upgrade | Upgrade button → confirmation modal → Stripe checkout |
| BR-BILL-005 | Plan Downgrade | Downgrade button → scheduled change notice → pending indicator |
| BR-BILL-006 | Subscription Cancellation | Cancel button → confirmation modal → period-end notice |
| BR-BILL-007 | Payment Failure Flow | Past-due banner, payment update link |
| BR-BILL-008 | Credit System | Credit balance header widget, billing overview |
| BR-BILL-009 | Scan Pricing Config | Admin: scan step pricing table (CRUD) |
| BR-BILL-010 | Credit Allotment & Reset | Billing overview period display, reset notice |
| BR-BILL-011 | Credit Balance Check | Scan creation: cost estimate + insufficient credits modal |
| BR-BILL-012 | Credit Deduction | Post-scan balance update |
| BR-BILL-013 | Credit Refunds | Transaction history: refund entries |
| BR-BILL-014 | Credit Balance Calculation | Credit balance widget (allotment + purchased breakdown) |
| BR-BILL-015 | Credit Pack Purchase | Credit pack cards → Stripe checkout |
| BR-BILL-016 | Manual Credit Adjustment | Admin: credit adjustment form |
| BR-BILL-017 | Scheduled Scan Credits | Scan scheduling: credit estimate warning |
| BR-BILL-018 | Schedule Cancellation | Handled in scanning plan (cross-ref) |

---

## User Roles

| Role | Screens Accessible | Key Actions |
|------|-------------------|-------------|
| **Tenant Owner** | Billing overview, plan comparison, credit history, credit packs | Subscribe, upgrade, downgrade, cancel, purchase credits |
| **Tenant Member** | Billing overview (read-only) | View plan and credit balance |
| **Super Admin** | Admin credit adjustment, scan pricing config | Adjust credits, configure pricing |

---

## Subscription State Machine

```
                    ┌──────────────┐
   Subscribe        │    ACTIVE    │◄─────────────────┐
  ─────────────────►│              │                   │
                    └──┬───────┬───┘                   │
                       │       │                       │
          Payment      │       │ Cancel             Payment
          fails        │       │                    succeeds
                       ▼       ▼                       │
              ┌────────────┐  ┌──────────────┐         │
              │  PAST_DUE  │  │  CANCELLED   │         │
              │            │  │              │         │
              └──────┬─────┘  └──────┬───────┘         │
                     │               │                 │
           All retries               │ Period ends     │
           exhausted  │              │                 │
                      ▼              ▼                 │
              ┌──────────────────────────┐             │
              │         EXPIRED          │             │
              │  (downgraded to free)    │─────────────┘
              └──────────────────────────┘  Re-subscribe
```

### State Transitions

| Current State | Action | Next State | Trigger | Who |
|--------------|--------|------------|---------|-----|
| (none) | Subscribe | ACTIVE | Tenant owner selects plan | Tenant Owner |
| ACTIVE | Payment fails | PAST_DUE | Stripe webhook `invoice.payment_failed` | System |
| ACTIVE | Cancel | CANCELLED | Tenant owner clicks Cancel | Tenant Owner |
| PAST_DUE | Payment succeeds | ACTIVE | Stripe webhook `invoice.payment_succeeded` | System |
| PAST_DUE | All retries fail | EXPIRED | Stripe webhook `customer.subscription.deleted` | System |
| CANCELLED | Period ends | EXPIRED | Stripe webhook / background job | System |
| CANCELLED | Upgrade | ACTIVE | Tenant owner selects higher plan | Tenant Owner |
| EXPIRED | Re-subscribe | ACTIVE | Tenant owner selects plan | Tenant Owner |

---

## Credit Balance Model

```
┌─────────────────────────────────────────┐
│           Total Available Credits        │
│                                         │
│  ┌─────────────────┐ ┌────────────────┐ │
│  │   Allotment     │ │   Purchased    │ │
│  │   (resets each  │ │   (survives    │ │
│  │    period)      │ │    reset)      │ │
│  └─────────────────┘ └────────────────┘ │
│                                         │
│  Consumption order: Allotment first,    │
│  then Purchased                         │
└─────────────────────────────────────────┘
```

### Credit Transaction Types

| Type | Amount | Survives Reset | Source |
|------|--------|---------------|--------|
| ALLOTMENT | + (plan credits) | No — reset each period | Period start |
| CONSUMPTION | − (scan cost) | N/A | Scan creation |
| PURCHASE | + (pack credits) | Yes | Credit pack checkout |
| REFUND | + (failed step credits) | Yes | Scan step failure |
| ADJUSTMENT | ± (admin amount) | Yes | Super admin |

---

## Screen Navigation Map

```
Global Header
  └── [Credit Balance Widget] ──► /settings/billing

/settings/billing
  ├── Billing Overview (default tab)
  │     ├── Current plan summary
  │     ├── Credit balance breakdown
  │     ├── [Change Plan] ──► /settings/billing/plans
  │     ├── [Purchase Credits] ──► /settings/billing/credit-packs
  │     ├── [Cancel Subscription] ──► Cancel confirmation modal
  │     └── [Manage Payment Method] ──► Stripe Customer Portal (external)
  │
  ├── Transaction History (/settings/billing/transactions)
  │     └── Paginated table with type filters
  │
  ├── Plan Comparison (/settings/billing/plans)
  │     ├── Plan cards with feature comparison
  │     ├── Billing interval toggle (Monthly/Annual)
  │     └── [Select Plan] ──► Upgrade/Downgrade confirmation modal ──► Stripe Checkout (external)
  │
  └── Credit Packs (/settings/billing/credit-packs)
        ├── Available pack cards
        └── [Purchase] ──► Stripe Checkout (external)

/admin/billing (Super Admin)
  ├── Credit Adjustment (/admin/billing/credits)
  │     └── Tenant search + adjustment form
  │
  └── Scan Pricing Config (/admin/billing/pricing)
        └── Pricing matrix table (check type × tier)
```

### Screen Summary

| # | Screen | Route | Access | Notes |
|---|--------|-------|--------|-------|
| 1 | Credit Balance Widget | Global header | All authenticated | Compact display, links to billing |
| 2 | Billing Overview | `/settings/billing` | Tenant Owner (edit), Members (view) | Default billing landing |
| 3 | Plan Comparison | `/settings/billing/plans` | Tenant Owner | Feature matrix, upgrade/downgrade |
| 4 | Transaction History | `/settings/billing/transactions` | Tenant Owner | Paginated, filterable |
| 5 | Credit Packs | `/settings/billing/credit-packs` | Tenant Owner | Purchase additional credits |
| 6 | Admin Credit Adjustment | `/admin/billing/credits` | Super Admin | Adjust any tenant's credits |
| 7 | Admin Scan Pricing | `/admin/billing/pricing` | Super Admin | Configure per-tier pricing |

---

## Banners & Global States

| Condition | Banner | Actions |
|-----------|--------|---------|
| Free tier (no subscription) | "Upgrade to start scanning" | [View Plans] CTA |
| PAST_DUE | "Payment failed — update your payment method" | [Update Payment] → Stripe Portal |
| CANCELLED | "Your plan cancels on {date}" | [Reactivate] (upgrade flow) |
| Pending downgrade | "Downgrading to {plan} on {date}" | [Cancel Downgrade] |
| Low credits (<20% remaining) | "Running low on credits" | [Purchase Credits] CTA |
