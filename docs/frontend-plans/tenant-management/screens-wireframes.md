# Screens & Wireframes (Tenant Management)

Scope: ASCII wireframes for all 13 screens covering provisioning, dashboard, domains, settings, billing, blocked-state pages, and admin tenant management.

---

## Route Structure

### Public Routes

| Route | Screen | Guard |
|-------|--------|-------|
| `/auth/provisioning` | Provisioning Status | Temp token required |
| `/auth/suspended` | Suspended Page | None (informational) |
| `/auth/deactivated` | Deactivated Page | None (informational) |

### Authenticated Routes (Tenant Owner)

| Route | Screen | Guard |
|-------|--------|-------|
| `/(app)/dashboard` | Dashboard | Authenticated + tenant ACTIVE |
| `/(app)/domains` | Domains List | Authenticated + tenant ACTIVE |
| `/(app)/domains/[id]` | Domain Detail | Authenticated + tenant ACTIVE |
| `/(app)/settings` | Tenant Settings | Authenticated + tenant ACTIVE |
| `/(app)/billing` | Billing Overview | Authenticated + tenant ACTIVE |
| `/(app)/billing/plans` | Plan Comparison | Authenticated + tenant ACTIVE |

### Admin Routes (Super Admin)

| Route | Screen | Guard |
|-------|--------|-------|
| `/(admin)/admin/tenants` | Tenant List | Authenticated + SUPER_ADMIN |
| `/(admin)/admin/tenants/[id]` | Tenant Detail | Authenticated + SUPER_ADMIN |

### Global Components

| Component | Context | Condition |
|-----------|---------|-----------|
| Free Tier Banner | All `/(app)/` routes | `plan === 'free'` |
| Impersonation Banner | All `/(app)/` routes | `jwt.is_impersonation === true` |

---

## App Shell Layout

All authenticated `/(app)/` routes share this shell:

```
┌──────────────────────────────────────────────────────────────────┐
│ Reconova             Dashboard  Domains  Scans  Billing  Settings│
│                                                    [john@acme] v│
├──────────────────────────────────────────────────────────────────┤
│ (optional) Free plan -- Upgrade to unlock all features     [->] │
├──────────────────────────────────────────────────────────────────┤
│                                                                  │
│                        PAGE CONTENT                              │
│                                                                  │
└──────────────────────────────────────────────────────────────────┘
```

Admin `/(admin)/` routes share a separate shell:

```
┌──────────────────────────────────────────────────────────────────┐
│ Reconova Admin       Tenants  Plans  Features  Audit  Monitoring │
│                                                   [admin@rn] v  │
├──────────────────────────────────────────────────────────────────┤
│                                                                  │
│                        PAGE CONTENT                              │
│                                                                  │
└──────────────────────────────────────────────────────────────────┘
```

---

## Screen 1: Provisioning Status (`/auth/provisioning`)

### Default State (Polling)

```
┌──────────────────────────────────────────────────────────────────┐
│                        Reconova                                  │
├──────────────────────────────────────────────────────────────────┤
│                                                                  │
│                  Setting up your workspace...                    │
│                                                                  │
│                  [done]  Account created                         │
│                  [done]  2FA configured                          │
│                  [....]  Preparing your database...              │
│                  [    ]  Applying configurations...              │
│                                                                  │
│                  This usually takes a few seconds.               │
│                                                                  │
└──────────────────────────────────────────────────────────────────┘
```

### Slow Provisioning State (> 60s)

```
┌──────────────────────────────────────────────────────────────────┐
│                        Reconova                                  │
├──────────────────────────────────────────────────────────────────┤
│                                                                  │
│                  Setting up your workspace...                    │
│                                                                  │
│                  [done]  Account created                         │
│                  [done]  2FA configured                          │
│                  [....]  Preparing your database...              │
│                  [    ]  Applying configurations...              │
│                                                                  │
│                  This is taking longer than usual.               │
│                  Please wait -- we're still working on it.       │
│                                                                  │
└──────────────────────────────────────────────────────────────────┘
```

### Error State (Provisioning Failed)

```
┌──────────────────────────────────────────────────────────────────┐
│                        Reconova                                  │
├──────────────────────────────────────────────────────────────────┤
│                                                                  │
│                  Something went wrong                            │
│                                                                  │
│                  We couldn't finish setting up your workspace.   │
│                  Our team has been notified and will resolve      │
│                  this shortly.                                    │
│                                                                  │
│                  [Contact Support]                                │
│                                                                  │
└──────────────────────────────────────────────────────────────────┘
```

---

## Screen 2: Dashboard (`/(app)/dashboard`)

### Default State

```
┌──────────────────────────────────────────────────────────────────┐
│ Reconova             Dashboard  Domains  Scans  Billing  Settings│
├──────────────────────────────────────────────────────────────────┤
│                                                                  │
│  Dashboard                                                       │
│                                                                  │
│  ┌──────────────┐ ┌──────────────┐ ┌──────────────┐ ┌──────────┐│
│  │  Domains     │ │  Scans       │ │  Credits     │ │Compliance││
│  │              │ │              │ │              │ │          ││
│  │    3 / 5     │ │     12       │ │    450       │ │   78%    ││
│  │              │ │  completed   │ │  remaining   │ │  score   ││
│  │  [View All]  │ │  [View All]  │ │  [View All]  │ │  [View]  ││
│  └──────────────┘ └──────────────┘ └──────────────┘ └──────────┘│
│                                                                  │
│  Recent Activity                                                 │
│  ┌──────────────────────────────────────────────────────────────┐│
│  │ Mar 8  Scan completed: acme.com -- 3 vulnerabilities found  ││
│  │ Mar 7  Scan completed: example.org -- Clean                 ││
│  │ Mar 7  Domain added: example.org                            ││
│  │ Mar 5  Scan completed: acme.com -- 1 vulnerability found    ││
│  │ Mar 3  Plan upgraded to Professional                        ││
│  └──────────────────────────────────────────────────────────────┘│
│                                                                  │
│  Quick Actions                                                   │
│  [New Scan]  [Add Domain]  [View Reports]                        │
│                                                                  │
└──────────────────────────────────────────────────────────────────┘
```

### Loading State

```
┌──────────────────────────────────────────────────────────────────┐
│ Reconova             Dashboard  Domains  Scans  Billing  Settings│
├──────────────────────────────────────────────────────────────────┤
│                                                                  │
│  Dashboard                                                       │
│                                                                  │
│  ┌──────────────┐ ┌──────────────┐ ┌──────────────┐ ┌──────────┐│
│  │  ░░░░░░░░░░  │ │  ░░░░░░░░░░  │ │  ░░░░░░░░░░  │ │░░░░░░░░ ││
│  │  ░░░░░░░░    │ │  ░░░░░░░░    │ │  ░░░░░░░░    │ │░░░░░░   ││
│  │  ░░░░░       │ │  ░░░░░       │ │  ░░░░░       │ │░░░░░    ││
│  └──────────────┘ └──────────────┘ └──────────────┘ └──────────┘│
│                                                                  │
│  ░░░░░░░░░░░░░░░░░                                              │
│  ┌──────────────────────────────────────────────────────────────┐│
│  │  ░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░   ││
│  │  ░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░   ││
│  │  ░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░   ││
│  └──────────────────────────────────────────────────────────────┘│
│                                                                  │
└──────────────────────────────────────────────────────────────────┘
```

### Empty State (New Tenant)

```
┌──────────────────────────────────────────────────────────────────┐
│ Reconova             Dashboard  Domains  Scans  Billing  Settings│
├──────────────────────────────────────────────────────────────────┤
│                                                                  │
│  Dashboard                                                       │
│                                                                  │
│  ┌──────────────┐ ┌──────────────┐ ┌──────────────┐ ┌──────────┐│
│  │  Domains     │ │  Scans       │ │  Credits     │ │Compliance││
│  │              │ │              │ │              │ │          ││
│  │    0 / 5     │ │      0       │ │   1000       │ │   --     ││
│  │              │ │  no scans    │ │  remaining   │ │  n/a     ││
│  │  [Add First] │ │  [Run Scan]  │ │              │ │          ││
│  └──────────────┘ └──────────────┘ └──────────────┘ └──────────┘│
│                                                                  │
│  ┌──────────────────────────────────────────────────────────────┐│
│  │                                                              ││
│  │            Welcome to Reconova!                              ││
│  │                                                              ││
│  │   Get started by adding your first domain,                   ││
│  │   then run a security scan.                                  ││
│  │                                                              ││
│  │          [Add Your First Domain]                              ││
│  │                                                              ││
│  └──────────────────────────────────────────────────────────────┘│
│                                                                  │
└──────────────────────────────────────────────────────────────────┘
```

### Free Tier State

```
┌──────────────────────────────────────────────────────────────────┐
│ Reconova             Dashboard  Domains  Scans  Billing  Settings│
├──────────────────────────────────────────────────────────────────┤
│ Free plan -- Upgrade to unlock all features            [Upgrade] │
├──────────────────────────────────────────────────────────────────┤
│                                                                  │
│  Dashboard                                                       │
│                                                                  │
│  ┌──────────────┐ ┌──────────────┐ ┌──────────────┐ ┌──────────┐│
│  │  Domains     │ │  Scans       │ │  Credits     │ │Compliance││
│  │    2 / 0     │ │      5       │ │      0       │ │   65%    ││
│  │  (view only) │ │  (view only) │ │  (no credits)│ │(view only)│
│  └──────────────┘ └──────────────┘ └──────────────┘ └──────────┘│
│                                                                  │
│  Quick Actions                                                   │
│  [New Scan]  [Add Domain]  [View Reports]  <- all disabled/gray  │
│                                                                  │
└──────────────────────────────────────────────────────────────────┘
```

### Error State

```
┌──────────────────────────────────────────────────────────────────┐
│ Reconova             Dashboard  Domains  Scans  Billing  Settings│
├──────────────────────────────────────────────────────────────────┤
│                                                                  │
│  Dashboard                                                       │
│                                                                  │
│  ┌──────────────────────────────────────────────────────────────┐│
│  │                                                              ││
│  │   Failed to load dashboard data.                             ││
│  │                                                              ││
│  │   [Retry]                                                    ││
│  │                                                              ││
│  └──────────────────────────────────────────────────────────────┘│
│                                                                  │
└──────────────────────────────────────────────────────────────────┘
```

---

## Screen 3: Domains List (`/(app)/domains`)

### Default State

```
┌──────────────────────────────────────────────────────────────────┐
│ Reconova             Dashboard  Domains  Scans  Billing  Settings│
├──────────────────────────────────────────────────────────────────┤
│                                                                  │
│  Domains (3/5)                                                   │
│                                                                  │
│  ┌──────────────────────────────────────────────────────────────┐│
│  │ Add domain: [example.com                       ]  [Add]     ││
│  └──────────────────────────────────────────────────────────────┘│
│                                                                  │
│  ┌──────────────────────────────────────────────────────────────┐│
│  │ Domain           │ Added      │ Last Scanned │              ││
│  ├──────────────────┼────────────┼──────────────┼──────────────┤│
│  │ acme.com         │ Mar 1      │ Mar 8        │   [Delete]   ││
│  │ example.org      │ Mar 3      │ Mar 7        │   [Delete]   ││
│  │ test-site.io     │ Mar 5      │ Never        │   [Delete]   ││
│  └──────────────────────────────────────────────────────────────┘│
│                                                                  │
└──────────────────────────────────────────────────────────────────┘
```

### Empty State

```
┌──────────────────────────────────────────────────────────────────┐
│ Reconova             Dashboard  Domains  Scans  Billing  Settings│
├──────────────────────────────────────────────────────────────────┤
│                                                                  │
│  Domains (0/5)                                                   │
│                                                                  │
│  ┌──────────────────────────────────────────────────────────────┐│
│  │ Add domain: [example.com                       ]  [Add]     ││
│  └──────────────────────────────────────────────────────────────┘│
│                                                                  │
│  ┌──────────────────────────────────────────────────────────────┐│
│  │                                                              ││
│  │              No domains yet.                                 ││
│  │   Add your first domain to start scanning.                   ││
│  │                                                              ││
│  └──────────────────────────────────────────────────────────────┘│
│                                                                  │
└──────────────────────────────────────────────────────────────────┘
```

### Free Tier State

```
┌──────────────────────────────────────────────────────────────────┐
│ Reconova             Dashboard  Domains  Scans  Billing  Settings│
├──────────────────────────────────────────────────────────────────┤
│ Free plan -- Upgrade to unlock all features            [Upgrade] │
├──────────────────────────────────────────────────────────────────┤
│                                                                  │
│  Domains (2)                                                     │
│                                                                  │
│  ┌──────────────────────────────────────────────────────────────┐│
│  │ Add domain: [                          ] [Add] <- disabled  ││
│  └──────────────────────────────────────────────────────────────┘│
│                                                                  │
│  ┌──────────────────────────────────────────────────────────────┐│
│  │ Domain           │ Added      │ Last Scanned │              ││
│  ├──────────────────┼────────────┼──────────────┼──────────────┤│
│  │ acme.com         │ Mar 1      │ Mar 5        │              ││
│  │ example.org      │ Mar 3      │ Never        │              ││
│  └──────────────────────────────────────────────────────────────┘│
│                                                                  │
└──────────────────────────────────────────────────────────────────┘
```

### Validation Error State

```
  ┌──────────────────────────────────────────────────────────────┐
  │ Add domain: [not a domain!                      ]  [Add]     │
  │             ! Enter a valid domain (e.g., example.com)       │
  └──────────────────────────────────────────────────────────────┘
```

### Max Domains Reached

```
  ┌──────────────────────────────────────────────────────────────┐
  │ Add domain: [newsite.com                        ]  [Add]     │
  │             ! Domain limit reached (5/5). Upgrade for more.  │
  └──────────────────────────────────────────────────────────────┘
```

### Delete Confirmation

```
  ╔════════════════════════════════════════╗
  ║  Delete Domain                         ║
  ║                                        ║
  ║  Delete acme.com?                      ║
  ║  Scan history for this domain will     ║
  ║  also be removed.                      ║
  ║                                        ║
  ║  [Cancel]  [Delete]                    ║
  ╚════════════════════════════════════════╝
```

---

## Screen 4: Domain Detail (`/(app)/domains/[id]`)

### Default State

```
┌──────────────────────────────────────────────────────────────────┐
│ Reconova             Dashboard  Domains  Scans  Billing  Settings│
├──────────────────────────────────────────────────────────────────┤
│                                                                  │
│  <- Back to Domains                                              │
│                                                                  │
│  acme.com                                                        │
│  Added: March 1, 2026                                            │
│  Verification: Not required (MVP)  [POST-MVP: Verified]         │
│                                                                  │
│  [Run Scan]  [Delete Domain]                                     │
│                                                                  │
│  Scan History                                                    │
│  ┌──────────────────────────────────────────────────────────────┐│
│  │ Date     │ Type        │ Status    │ Findings │             ││
│  ├──────────┼─────────────┼───────────┼──────────┼─────────────┤│
│  │ Mar 8    │ Full Scan   │ Completed │ 3 vulns  │ [View]      ││
│  │ Mar 5    │ Quick Scan  │ Completed │ 1 vuln   │ [View]      ││
│  │ Mar 1    │ Full Scan   │ Completed │ 0        │ [View]      ││
│  └──────────────────────────────────────────────────────────────┘│
│                                                                  │
└──────────────────────────────────────────────────────────────────┘
```

---

## Screen 5: Tenant Settings (`/(app)/settings`)

### Default State

```
┌──────────────────────────────────────────────────────────────────┐
│ Reconova             Dashboard  Domains  Scans  Billing  Settings│
├──────────────────────────────────────────────────────────────────┤
│                                                                  │
│  Settings                                                        │
│                                                                  │
│  -- Organization ------------------------------------------------│
│                                                                  │
│  Organization Name                                               │
│  [Acme Corp                                          ]  [Save]   │
│                                                                  │
│  Slug: acme-corp                                                 │
│  Created: March 1, 2026                                          │
│                                                                  │
│  -- Account -----------------------------------------------------│
│                                                                  │
│  Email: john@acme.com                                            │
│  Role: Tenant Owner                                              │
│  2FA: Enabled                                                    │
│                                                                  │
│  [Change Password]                                               │
│                                                                  │
│  -- Danger Zone -------------------------------------------------│
│  ┌──────────────────────────────────────────────────────────────┐│
│  │  Delete your account and all associated data.                ││
│  │  This action is irreversible after admin approval.           ││
│  │                                              [Delete Account]││
│  └──────────────────────────────────────────────────────────────┘│
│                                                                  │
└──────────────────────────────────────────────────────────────────┘
```

### Deletion Pending State

```
│  -- Danger Zone -------------------------------------------------│
│  ┌──────────────────────────────────────────────────────────────┐│
│  │  Deletion request submitted on March 7, 2026.                ││
│  │  Awaiting admin approval.                                    ││
│  │                                                              ││
│  │  [Delete Account] <- disabled                                ││
│  └──────────────────────────────────────────────────────────────┘│
```

### Delete Account Modal

```
  ╔════════════════════════════════════════════╗
  ║  Delete Account                            ║
  ║                                            ║
  ║  This action cannot be undone.             ║
  ║  Your data will be retained for 30 days,   ║
  ║  then permanently deleted.                 ║
  ║                                            ║
  ║  Type "acme-corp" to confirm:              ║
  ║  ┌──────────────────────────────────────┐  ║
  ║  │                                      │  ║
  ║  └──────────────────────────────────────┘  ║
  ║                                            ║
  ║  [Cancel]  [Request Deletion] <- disabled  ║
  ║            until slug matches              ║
  ╚════════════════════════════════════════════╝
```

### Impersonation State

```
│  -- Danger Zone -------------------------------------------------│
│  ┌──────────────────────────────────────────────────────────────┐│
│  │  You are viewing this page as an impersonated user.          ││
│  │  Account deletion is not available during impersonation.     ││
│  └──────────────────────────────────────────────────────────────┘│
```

---

## Screen 6: Billing Overview (`/(app)/billing`)

### Default State (Paid Plan)

```
┌──────────────────────────────────────────────────────────────────┐
│ Reconova             Dashboard  Domains  Scans  Billing  Settings│
├──────────────────────────────────────────────────────────────────┤
│                                                                  │
│  Billing                                                         │
│                                                                  │
│  -- Current Plan ------------------------------------------------│
│                                                                  │
│  Plan: Professional ($99/mo)                    [Change Plan]    │
│  Status: Active                                                  │
│  Next billing: April 1, 2026                                     │
│                                                                  │
│  -- Credit Balance ----------------------------------------------│
│                                                                  │
│  ┌──────────────────────────────────────────────┐                │
│  │ ████████████████████░░░░░░░░░░  450 / 1000   │                │
│  └──────────────────────────────────────────────┘                │
│  Credits used this period: 550                                   │
│  [Purchase Additional Credits]                                   │
│                                                                  │
│  -- Usage This Period -------------------------------------------│
│                                                                  │
│  Full Scan:     ████████████████  320 credits                    │
│  Quick Scan:    ██████            120 credits                    │
│  Compliance:    ████               80 credits                    │
│  Other:         █                  30 credits                    │
│                                                                  │
│  -- Billing History ---------------------------------------------│
│  ┌──────────────────────────────────────────────────────────────┐│
│  │ Date     │ Description          │ Amount  │ Status │Invoice ││
│  ├──────────┼──────────────────────┼─────────┼────────┼────────┤│
│  │ Mar 1    │ Professional Plan    │ $99.00  │ Paid   │ [DL]   ││
│  │ Feb 15   │ 500 Credits          │ $49.00  │ Paid   │ [DL]   ││
│  │ Feb 1    │ Professional Plan    │ $99.00  │ Paid   │ [DL]   ││
│  └──────────────────────────────────────────────────────────────┘│
│  [Load More]                                                     │
│                                                                  │
└──────────────────────────────────────────────────────────────────┘
```

### Free Tier State

```
┌──────────────────────────────────────────────────────────────────┐
│ Reconova             Dashboard  Domains  Scans  Billing  Settings│
├──────────────────────────────────────────────────────────────────┤
│ Free plan -- Upgrade to unlock all features            [Upgrade] │
├──────────────────────────────────────────────────────────────────┤
│                                                                  │
│  Billing                                                         │
│                                                                  │
│  -- Current Plan ------------------------------------------------│
│                                                                  │
│  Plan: Free                                       [Upgrade]      │
│  Status: Active                                                  │
│  No billing -- upgrade to access all features.                   │
│                                                                  │
│  -- Credit Balance ----------------------------------------------│
│                                                                  │
│  Credits: 0 (Free plan does not include credits)                 │
│                                                                  │
│  -- Billing History ---------------------------------------------│
│  ┌──────────────────────────────────────────────────────────────┐│
│  │ Date     │ Description          │ Amount  │ Status │Invoice ││
│  ├──────────┼──────────────────────┼─────────┼────────┼────────┤│
│  │ Mar 1    │ Downgraded to Free   │ --      │ --     │        ││
│  │ Feb 1    │ Professional Plan    │ $99.00  │ Paid   │ [DL]   ││
│  └──────────────────────────────────────────────────────────────┘│
│                                                                  │
└──────────────────────────────────────────────────────────────────┘
```

---

## Screen 7: Plan Comparison (`/(app)/billing/plans`)

### Default State

```
┌──────────────────────────────────────────────────────────────────┐
│ Reconova             Dashboard  Domains  Scans  Billing  Settings│
├──────────────────────────────────────────────────────────────────┤
│                                                                  │
│  <- Back to Billing                                              │
│                                                                  │
│  Choose Your Plan                                                │
│                                                                  │
│  ┌──────────────────┐ ┌──────────────────┐ ┌──────────────────┐ │
│  │      Free        │ │   Professional   │ │   Enterprise     │ │
│  │                  │ │                  │ │                  │ │
│  │     $0/mo        │ │    $99/mo        │ │   $299/mo        │ │
│  │                  │ │                  │ │                  │ │
│  │ + View results   │ │ + 1,000 credits  │ │ + 5,000 credits  │ │
│  │ - Run scans      │ │ + 5 domains      │ │ + 25 domains     │ │
│  │ - Add domains    │ │ + All scan types │ │ + All scan types │ │
│  │ - Reports        │ │ + Compliance     │ │ + Compliance     │ │
│  │ - Integrations   │ │ + Integrations   │ │ + Integrations   │ │
│  │ - Scheduling     │ │ + Scheduling     │ │ + Priority       │ │
│  │                  │ │                  │ │   support        │ │
│  │   [Current]      │ │   [Upgrade]      │ │   [Upgrade]      │ │
│  └──────────────────┘ └──────────────────┘ └──────────────────┘ │
│                                                                  │
└──────────────────────────────────────────────────────────────────┘
```

---

## Screen 8: Suspended Page (`/auth/suspended`)

```
┌──────────────────────────────────────────────────────────────────┐
│                        Reconova                                  │
├──────────────────────────────────────────────────────────────────┤
│                                                                  │
│                  Account Suspended                               │
│                                                                  │
│                  Your account has been suspended.                │
│                  Contact support for more information.           │
│                                                                  │
│                  [Contact Support]   [Back to Login]             │
│                                                                  │
└──────────────────────────────────────────────────────────────────┘
```

---

## Screen 9: Deactivated Page (`/auth/deactivated`)

```
┌──────────────────────────────────────────────────────────────────┐
│                        Reconova                                  │
├──────────────────────────────────────────────────────────────────┤
│                                                                  │
│                  Account Deactivated                             │
│                                                                  │
│                  Your account has been permanently               │
│                  deactivated. Data will be retained              │
│                  for 30 days from the deletion date.             │
│                                                                  │
│                  [Contact Support]                                │
│                                                                  │
└──────────────────────────────────────────────────────────────────┘
```

---

## Screen 11: Admin Tenant List (`/(admin)/admin/tenants`)

### Default State

```
┌──────────────────────────────────────────────────────────────────┐
│ Reconova Admin       Tenants  Plans  Features  Audit  Monitoring │
├──────────────────────────────────────────────────────────────────┤
│                                                                  │
│  Tenants                                                         │
│                                                                  │
│  Search: [Search by name or slug...                  ]           │
│  Filter: [All]  [ACTIVE]  [SUSPENDED]  [PROVISIONING]           │
│                                                     [DEACTIVATED]│
│                                                                  │
│  ┌──────────────────────────────────────────────────────────────┐│
│  │ Tenant        │ Slug       │ Status  │ Plan │ Created│Active ││
│  ├───────────────┼────────────┼─────────┼──────┼────────┼───────┤│
│  │ Acme Corp     │ acme-corp  │ ACTIVE  │ Pro  │ Mar 1  │ Mar 8 ││
│  │ Beta Inc      │ beta-inc   │SUSPENDED│ Free │ Feb 15 │ Feb 28││
│  │ Gamma LLC     │ gamma-llc  │ ACTIVE  │ Ent  │ Jan 10 │ Mar 7 ││
│  │ Delta Co      │ delta-co   │DEACTIVE │ Pro  │ Dec 1  │ Jan 15││
│  │ Epsilon       │ epsilon    │PROVISION│  --  │ Mar 8  │  --   ││
│  └──────────────────────────────────────────────────────────────┘│
│                                                                  │
│  [<- Prev]  Page 1 of 5  [Next ->]           Showing 5 of 42   │
│                                                                  │
└──────────────────────────────────────────────────────────────────┘
```

### Empty Search Results

```
│  ┌──────────────────────────────────────────────────────────────┐│
│  │                                                              ││
│  │        No tenants found matching "xyz-corp"                  ││
│  │                                                              ││
│  └──────────────────────────────────────────────────────────────┘│
```

---

## Screen 12: Admin Tenant Detail (`/(admin)/admin/tenants/[id]`)

### Active Tenant

```
┌──────────────────────────────────────────────────────────────────┐
│ Reconova Admin       Tenants  Plans  Features  Audit  Monitoring │
├──────────────────────────────────────────────────────────────────┤
│                                                                  │
│  <- Back to Tenants                                              │
│                                                                  │
│  Acme Corp                                     Status: [ACTIVE] │
│                                                                  │
│  -- Tenant Info -------------------------------------------------│
│                                                                  │
│  Slug: acme-corp                                                 │
│  Plan: Professional ($99/mo)                                     │
│  Created: March 1, 2026                                          │
│  Last Active: March 8, 2026                                      │
│  Owner: john@acme.com                                            │
│  Domains: 3 / 5                                                  │
│  Credits: 450 remaining                                          │
│                                                                  │
│  -- Actions -----------------------------------------------------│
│                                                                  │
│  [Suspend]  [Impersonate]                                        │
│                                                                  │
│  -- Audit Log (recent) -----------------------------------------│
│  ┌──────────────────────────────────────────────────────────────┐│
│  │ Mar 8  john@acme.com  scan.completed  acme.com              ││
│  │ Mar 7  john@acme.com  domain.added    example.org           ││
│  │ Mar 1  system         tenant.provisioned                    ││
│  └──────────────────────────────────────────────────────────────┘│
│  [View Full Audit Log]                                           │
│                                                                  │
└──────────────────────────────────────────────────────────────────┘
```

### Active Tenant with Deletion Pending

```
│  -- Pending Actions ---------------------------------------------│
│  ┌──────────────────────────────────────────────────────────────┐│
│  │ Deletion requested on March 7, 2026 by john@acme.com        ││
│  │                                                              ││
│  │ [Approve Deletion]  [Deny Deletion]                          ││
│  └──────────────────────────────────────────────────────────────┘│
│                                                                  │
│  -- Actions -----------------------------------------------------│
│  [Suspend]  [Impersonate]                                        │
```

### Suspended Tenant

```
│  Acme Corp                                  Status: [SUSPENDED] │
│                                                                  │
│  ...tenant info...                                               │
│                                                                  │
│  -- Actions -----------------------------------------------------│
│                                                                  │
│  [Reactivate]                                                    │
```

### Provisioning Failed Tenant

```
│  Epsilon                               Status: [PROVISIONING]   │
│                                        DB Status: FAILED         │
│                                                                  │
│  ...tenant info...                                               │
│                                                                  │
│  -- Actions -----------------------------------------------------│
│                                                                  │
│  [Retry Provisioning]  [Delete Tenant]                           │
```

### Deactivated Tenant

```
│  Delta Co                              Status: [DEACTIVATED]    │
│                                                                  │
│  ...tenant info...                                               │
│                                                                  │
│  Deactivated: January 15, 2026                                   │
│  Data retained until: February 14, 2026                          │
│                                                                  │
│  -- Actions -----------------------------------------------------│
│  No actions available for deactivated tenants.                   │
```

### Suspend Confirmation Modal

```
  ╔════════════════════════════════════════════════╗
  ║  Suspend Acme Corp?                            ║
  ║                                                ║
  ║  This will immediately:                        ║
  ║  - Log out all users                           ║
  ║  - Cancel running scans                        ║
  ║  - Disable scheduled scans                     ║
  ║                                                ║
  ║  Reason (required):                            ║
  ║  ┌────────────────────────────────────────┐    ║
  ║  │                                        │    ║
  ║  └────────────────────────────────────────┘    ║
  ║                                                ║
  ║  [Cancel]  [Suspend Tenant]                    ║
  ╚════════════════════════════════════════════════╝
```

### Impersonation Confirmation Modal

```
  ╔════════════════════════════════════════════════╗
  ║  Impersonate john@acme.com?                    ║
  ║                                                ║
  ║  You will view the platform as this user       ║
  ║  for up to 1 hour. All actions will be logged. ║
  ║                                                ║
  ║  A new tab will open with the tenant's view.   ║
  ║                                                ║
  ║  [Cancel]  [Start Impersonation]               ║
  ╚════════════════════════════════════════════════╝
```

---

## Screen 13: Impersonation Banner (Global Component)

### Banner (persistent, top of all pages during impersonation)

```
┌──────────────────────────────────────────────────────────────────┐
│ Viewing as: john@acme.com (Acme Corp)                            │
│ Session expires in: 58:32                    [End Impersonation] │
├──────────────────────────────────────────────────────────────────┤
│ Reconova             Dashboard  Domains  Scans  Billing  Settings│
├──────────────────────────────────────────────────────────────────┤
│                        PAGE CONTENT                              │
└──────────────────────────────────────────────────────────────────┘
```

### Session Expired State

```
┌──────────────────────────────────────────────────────────────────┐
│                        Reconova                                  │
├──────────────────────────────────────────────────────────────────┤
│                                                                  │
│           Impersonation Session Expired                          │
│                                                                  │
│           Your impersonation session has ended.                  │
│           You can close this tab.                                │
│                                                                  │
│           [Close Tab]                                             │
│                                                                  │
└──────────────────────────────────────────────────────────────────┘
```

---

## Conditional Rendering Rules Summary

| Condition | Effect |
|-----------|--------|
| `plan === 'free'` | Show upgrade banner on all app pages; disable write-action buttons |
| `jwt.is_impersonation === true` | Show impersonation banner; hide delete account in settings |
| `tenant.deletion_requested_at !== null` | Show pending deletion banner on settings; disable delete button |
| `tenant.status === 'SUSPENDED'` | Redirect to `/auth/suspended` |
| `tenant.status === 'DEACTIVATED'` | Redirect to `/auth/deactivated` |
| `tenant.status === 'PROVISIONING'` | Redirect to `/auth/provisioning` |
| `role === 'SUPER_ADMIN'` | Access to `/(admin)/` routes |
