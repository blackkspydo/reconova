# Screens & Wireframes (System Configuration)

Scope: ASCII wireframes for every screen state across Config List, Change History, and Approval Queue.

---

## Route Structure

| Route | Screen | Auth | Role |
|-------|--------|------|------|
| `/admin/config` | Config List | Required | `SUPER_ADMIN` |
| `/admin/config/history` | Change History | Required | `SUPER_ADMIN` |
| `/admin/config/approvals` | Approval Queue | Required | `SUPER_ADMIN` |

---

## Screen 1: Config List

### State 1A: Default (All Categories Expanded)

```
┌──────────────────────────────────────────────────────────────────────┐
│ Admin > System Configuration                                         │
├──────────────────────────────────────────────────────────────────────┤
│                                                                      │
│  [Search configs...]                    [History] [Approvals (2)]    │
│                                                                      │
│  Cache: ● Healthy (TTL 5m)                    [Invalidate Cache]     │
│                                                                      │
│  ┌─────────────────────────────────────────────────────────────────┐ │
│  │ ℹ Bootstrap configs (DB connection, Redis, encryption key,      │ │
│  │   app environment) are set via environment variables and        │ │
│  │   require a redeploy to change.                                 │ │
│  └─────────────────────────────────────────────────────────────────┘ │
│                                                                      │
│  ▼ Authentication (12 configs)                                       │
│  ┌─────────────────────────────────────────────────────────────────┐ │
│  │ Key                              Value    Type     Actions      │ │
│  │ auth.jwt.access_token_ttl_min…   15       INTEGER  [Edit]       │ │
│  │ auth.jwt.refresh_token_ttl_days  7        INTEGER  [Edit]       │ │
│  │ auth.password.min_length         12       INTEGER  [Edit]       │ │
│  │ auth.password.require_uppercase  Enabled  BOOLEAN  [Edit]       │ │
│  │ auth.password.require_number     Enabled  BOOLEAN  [Edit]       │ │
│  │ auth.password.require_special    Enabled  BOOLEAN  [Edit]       │ │
│  │ auth.lockout.max_failed_att…     5        INTEGER  [Edit]       │ │
│  │ auth.lockout.duration_minutes    30       INTEGER  [Edit]       │ │
│  │ auth.2fa.code_validity_seconds   30       INTEGER  [Edit]       │ │
│  │ auth.2fa.recovery_codes_count    10       INTEGER  [Edit]       │ │
│  │ auth.session.max_concurrent      5        INTEGER  [Edit]       │ │
│  │ auth.password_reset.token_ttl…   60       INTEGER  [Edit]       │ │
│  └─────────────────────────────────────────────────────────────────┘ │
│                                                                      │
│  ▼ Tenant Management (6 configs)                                     │
│  ┌─────────────────────────────────────────────────────────────────┐ │
│  │ Key                              Value    Type     Actions      │ │
│  │ tenant.slug.min_length           3        INTEGER  [Edit]       │ │
│  │ tenant.slug.max_length           50       INTEGER  [Edit]       │ │
│  │ tenant.provisioning.timeout_s…   120      INTEGER  [Edit]       │ │
│  │ tenant.domain.verification_t…    72       INTEGER  [Edit]       │ │
│  │ tenant.suspension.grace_peri…    30       INTEGER  [Edit]  🔶   │ │
│  │ tenant.deactivation.data_ret…    90       INTEGER  [Edit]  🔶   │ │
│  └─────────────────────────────────────────────────────────────────┘ │
│                                                                      │
│  ▶ Billing & Credits (6 configs)                                     │
│  ▶ Scanning & Workflows (10 configs)                                 │
│  ▶ Feature Flags (2 configs)                                         │
│  ▶ Compliance (5 configs)                                            │
│  ▶ CVE Monitoring (3 configs)                                        │
│  ▶ Integrations (5 configs)                                          │
│  ▶ Rate Limiting (8 configs)                                         │
│  ▶ API Versioning (2 configs)                                        │
│  ▶ Platform Operations (6 configs)                                   │
│                                                                      │
│  Legend: 🔶 Critical (requires approval)                              │
│                                                                      │
└──────────────────────────────────────────────────────────────────────┘
```

### State 1B: Inline Edit — Non-Critical Config

```
┌──────────────────────────────────────────────────────────────────────┐
│  ▼ Authentication (12 configs)                                       │
│  ┌─────────────────────────────────────────────────────────────────┐ │
│  │ auth.jwt.access_token_ttl_min…   15       INTEGER  [Edit]       │ │
│  │                                                                  │ │
│  │ ┌─────────────────────────────────────────────────────────────┐ │ │
│  │ │ auth.password.min_length                                    │ │ │
│  │ │ "Minimum password length"                                   │ │ │
│  │ │                                                             │ │ │
│  │ │ Type: INTEGER    Range: 8 – 32    Default: 12              │ │ │
│  │ │ Current value: 12                                           │ │ │
│  │ │                                                             │ │ │
│  │ │ New value: [ 16            ]                                │ │ │
│  │ │                                                             │ │ │
│  │ │ Reason: [ Increasing minimum for security compliance    ]   │ │ │
│  │ │                                                             │ │ │
│  │ │          [Reset to Default]     [Cancel]  [Save]           │ │ │
│  │ └─────────────────────────────────────────────────────────────┘ │ │
│  │                                                                  │ │
│  │ auth.password.require_uppercase  Enabled  BOOLEAN  [Edit]       │ │
│  └─────────────────────────────────────────────────────────────────┘ │
└──────────────────────────────────────────────────────────────────────┘
```

### State 1C: Inline Edit — Critical Config

```
┌──────────────────────────────────────────────────────────────────────┐
│  ▼ Billing & Credits (6 configs)                                     │
│  ┌─────────────────────────────────────────────────────────────────┐ │
│  │ billing.credits.starter_monthly  100  INTEGER  [Edit]  🔶       │ │
│  │                                                                  │ │
│  │ ┌─────────────────────────────────────────────────────────────┐ │ │
│  │ │ billing.credits.pro_monthly                            🔶   │ │ │
│  │ │ "Monthly credit allotment for Pro tier"                     │ │ │
│  │ │                                                             │ │ │
│  │ │ ┌───────────────────────────────────────────────────────┐  │ │ │
│  │ │ │ ℹ This is a critical configuration. Changes require   │  │ │ │
│  │ │ │   approval from another super admin.                   │  │ │ │
│  │ │ └───────────────────────────────────────────────────────┘  │ │ │
│  │ │                                                             │ │ │
│  │ │ Type: INTEGER    Range: 10 – 50000    Default: 500         │ │ │
│  │ │ Current value: 500                                          │ │ │
│  │ │                                                             │ │ │
│  │ │ New value: [ 750           ]                                │ │ │
│  │ │                                                             │ │ │
│  │ │ Reason: [ Adjusting Pro allotment for Q2 pricing update ]  │ │ │
│  │ │                                                             │ │ │
│  │ │     [Reset to Default]     [Cancel]  [Request Approval]    │ │ │
│  │ └─────────────────────────────────────────────────────────────┘ │ │
│  │                                                                  │ │
│  │ billing.credits.max_pack_pur…    10   INTEGER  [Edit]           │ │
│  └─────────────────────────────────────────────────────────────────┘ │
└──────────────────────────────────────────────────────────────────────┘
```

### State 1D: Inline Edit — Validation Error

```
┌─────────────────────────────────────────────────────────────┐
│ auth.lockout.max_failed_attempts                             │
│ "Failed login attempts before lockout"                       │
│                                                              │
│ Type: INTEGER    Range: 3 – 10    Default: 5                │
│ Current value: 5                                             │
│                                                              │
│ New value: [ 25           ]                                  │
│ ⚠ Value out of range. Must be between 3 and 10.             │
│                                                              │
│ Reason: [                                                 ]  │
│ ⚠ Reason is required.                                        │
│                                                              │
│          [Reset to Default]     [Cancel]  [Save] (disabled)  │
└─────────────────────────────────────────────────────────────┘
```

### State 1E: Inline Edit — Boolean Toggle

```
┌─────────────────────────────────────────────────────────────┐
│ auth.password.require_uppercase                              │
│ "Require uppercase in password"                              │
│                                                              │
│ Type: BOOLEAN    Default: Enabled                            │
│ Current value: Enabled                                       │
│                                                              │
│ New value: ( ○ Enabled  ● Disabled )                         │
│                                                              │
│ Reason: [ Relaxing for internal testing environment       ]  │
│                                                              │
│          [Reset to Default]     [Cancel]  [Save]             │
└─────────────────────────────────────────────────────────────┘
```

### State 1F: Inline Edit — String Enum Dropdown

```
┌─────────────────────────────────────────────────────────────┐
│ cve.alert.severity_threshold                                 │
│ "Minimum severity for auto-alerting tenants"                 │
│                                                              │
│ Type: STRING    Allowed: HIGH, CRITICAL    Default: HIGH     │
│ Current value: HIGH                                          │
│                                                              │
│ New value: [ CRITICAL      ▼ ]                               │
│                                                              │
│ Reason: [ Only alert on critical CVEs to reduce noise    ]   │
│                                                              │
│          [Reset to Default]     [Cancel]  [Save]             │
└─────────────────────────────────────────────────────────────┘
```

### State 1G: Inline Edit — JSON Textarea

```
┌─────────────────────────────────────────────────────────────┐
│ billing.subscription.dunning_retry_days                      │
│ "Payment retry schedule (days after failure)"                │
│                                                              │
│ Type: JSON    Default: [1,3,5,7]                             │
│ Current value: [1,3,5,7]                                     │
│                                                              │
│ New value:                                                   │
│ ┌─────────────────────────────────────────────────────────┐ │
│ │ [1, 3, 5, 7, 14]                                        │ │
│ │                                                          │ │
│ └─────────────────────────────────────────────────────────┘ │
│ ✓ Valid JSON                                                 │
│                                                              │
│ Reason: [ Adding 14-day final retry before suspension    ]   │
│                                                              │
│          [Reset to Default]     [Cancel]  [Save]             │
└─────────────────────────────────────────────────────────────┘
```

### State 1H: Config with Pending Approval

```
┌──────────────────────────────────────────────────────────────────────┐
│  ▼ Billing & Credits (6 configs)                                     │
│  ┌─────────────────────────────────────────────────────────────────┐ │
│  │ billing.credits.starter_monthly  100  INTEGER  [Edit]  🔶       │ │
│  │ billing.credits.pro_monthly      500  INTEGER  (disabled) 🔶    │ │
│  │   ┌──────────────────────────────────────────┐                   │ │
│  │   │ 🔵 Pending Approval: 500 → 750           │                   │ │
│  │   │ Requested by admin@example.com            │                   │ │
│  │   │ Expires in 18h 32m   [View in Approvals]  │                   │ │
│  │   └──────────────────────────────────────────┘                   │ │
│  │ billing.credits.max_pack_pur…    10   INTEGER  [Edit]           │ │
│  └─────────────────────────────────────────────────────────────────┘ │
└──────────────────────────────────────────────────────────────────────┘
```

### State 1I: Sensitive Config (Masked)

```
┌──────────────────────────────────────────────────────────────────────┐
│  │ some.sensitive.config            ••••••••  STRING  [Reveal] [Edit]│
│  │ some.other.config                value123  STRING  [Edit]         │
└──────────────────────────────────────────────────────────────────────┘
```

### State 1J: Sensitive Config (Revealed — 30s Timer)

```
┌──────────────────────────────────────────────────────────────────────┐
│  │ some.sensitive.config            s3cr3t!   STRING  [Hide] [Edit] │
│  │   Auto-hiding in 24s                                              │
└──────────────────────────────────────────────────────────────────────┘
```

### State 1K: Search Active

```
┌──────────────────────────────────────────────────────────────────────┐
│ Admin > System Configuration                                         │
├──────────────────────────────────────────────────────────────────────┤
│                                                                      │
│  [password___________] [✕]              [History] [Approvals (2)]    │
│                                                                      │
│  Showing 5 configs matching "password"                               │
│                                                                      │
│  ▼ Authentication (4 matches)                                        │
│  ┌─────────────────────────────────────────────────────────────────┐ │
│  │ auth.**password**.min_length         12       INTEGER  [Edit]    │ │
│  │ auth.**password**.require_uppercase  Enabled  BOOLEAN  [Edit]    │ │
│  │ auth.**password**.require_number     Enabled  BOOLEAN  [Edit]    │ │
│  │ auth.**password**.require_special    Enabled  BOOLEAN  [Edit]    │ │
│  └─────────────────────────────────────────────────────────────────┘ │
│                                                                      │
│  ▼ Authentication (1 match)                                          │
│  ┌─────────────────────────────────────────────────────────────────┐ │
│  │ auth.**password**_reset.token_ttl…  60       INTEGER  [Edit]    │ │
│  └─────────────────────────────────────────────────────────────────┘ │
│                                                                      │
│  (Tenant, Billing, Scanning… collapsed — no matches)                 │
│                                                                      │
└──────────────────────────────────────────────────────────────────────┘
```

### State 1L: Loading

```
┌──────────────────────────────────────────────────────────────────────┐
│ Admin > System Configuration                                         │
├──────────────────────────────────────────────────────────────────────┤
│                                                                      │
│  [Search configs...]                    [History] [Approvals]        │
│                                                                      │
│  ┌─────────────────────────────────────────────────────────────────┐ │
│  │ ░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░ │ │
│  │ ░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░ │ │
│  │ ░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░ │ │
│  │ ░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░ │ │
│  │ ░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░ │ │
│  └─────────────────────────────────────────────────────────────────┘ │
│                                                                      │
└──────────────────────────────────────────────────────────────────────┘
```

### State 1M: Error

```
┌──────────────────────────────────────────────────────────────────────┐
│ Admin > System Configuration                                         │
├──────────────────────────────────────────────────────────────────────┤
│                                                                      │
│  ┌─────────────────────────────────────────────────────────────────┐ │
│  │                                                                  │ │
│  │         ⚠ Failed to load system configuration.                   │ │
│  │                                                                  │ │
│  │                       [Retry]                                    │ │
│  │                                                                  │ │
│  └─────────────────────────────────────────────────────────────────┘ │
│                                                                      │
└──────────────────────────────────────────────────────────────────────┘
```

---

## Screen 2: Change History

### State 2A: Default

```
┌──────────────────────────────────────────────────────────────────────┐
│ Admin > System Configuration > Change History                        │
├──────────────────────────────────────────────────────────────────────┤
│                                                                      │
│  From: [2026-02-01] To: [2026-03-11]  Category: [All ▼]             │
│  Search: [config key...]                           [Clear Filters]   │
│                                                                      │
│  ┌─────────────────────────────────────────────────────────────────┐ │
│  │ Timestamp          Key                    Old → New              │ │
│  │                    Changed By             Reason       Actions   │ │
│  ├─────────────────────────────────────────────────────────────────┤ │
│  │ 2026-03-11 14:22   auth.lockout.duration  30 → 60               │ │
│  │                    admin@recon.io         "Increase lockout      │ │
│  │                                           for brute force        │ │
│  │                                           protection"  [Rollback]│ │
│  ├─────────────────────────────────────────────────────────────────┤ │
│  │ 2026-03-10 09:15   billing.credits.pro…   500 → 750             │ │
│  │                    ops@recon.io           "Q2 pricing            │ │
│  │                                           adjustment"  [Rollback]│ │
│  ├─────────────────────────────────────────────────────────────────┤ │
│  │ 2026-03-09 16:40   scanning.job.timeout   4 → 6                 │ │
│  │                    admin@recon.io         "Rollback: Revert      │ │
│  │                                           timeout change"        │ │
│  │                                                     Rolled back  │ │
│  ├─────────────────────────────────────────────────────────────────┤ │
│  │ 2026-03-09 11:00   scanning.job.timeout   6 → 4       ~~struck~~│ │
│  │                    admin@recon.io         "Reduce timeout"       │ │
│  │                                                     Rolled back  │ │
│  └─────────────────────────────────────────────────────────────────┘ │
│                                                                      │
│  Showing 1-4 of 47                          [← Prev] [1] [2] [Next →]│
│                                                                      │
└──────────────────────────────────────────────────────────────────────┘
```

### State 2B: Sensitive Values Masked

```
┌─────────────────────────────────────────────────────────────────┐
│ 2026-03-08 10:30   some.sensitive.key   •••••••• → ••••••••     │
│                    admin@recon.io       "Rotate API secret"     │
│                                                      [Rollback] │
└─────────────────────────────────────────────────────────────────┘
```

### State 2C: Empty (No Results)

```
┌──────────────────────────────────────────────────────────────────────┐
│ Admin > System Configuration > Change History                        │
├──────────────────────────────────────────────────────────────────────┤
│                                                                      │
│  From: [2026-03-11] To: [2026-03-11]  Category: [CVE ▼]             │
│  Search: [timeout...]                              [Clear Filters]   │
│                                                                      │
│  ┌─────────────────────────────────────────────────────────────────┐ │
│  │                                                                  │ │
│  │         No changes match your filters.                           │ │
│  │                                                                  │ │
│  │                    [Clear Filters]                                │ │
│  │                                                                  │ │
│  └─────────────────────────────────────────────────────────────────┘ │
│                                                                      │
└──────────────────────────────────────────────────────────────────────┘
```

### State 2D: Loading

```
┌──────────────────────────────────────────────────────────────────────┐
│ Admin > System Configuration > Change History                        │
├──────────────────────────────────────────────────────────────────────┤
│                                                                      │
│  From: [         ] To: [         ]  Category: [All ▼]                │
│  Search: [                  ]                      [Clear Filters]   │
│                                                                      │
│  ┌─────────────────────────────────────────────────────────────────┐ │
│  │ ░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░ │ │
│  │ ░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░ │ │
│  │ ░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░ │ │
│  └─────────────────────────────────────────────────────────────────┘ │
│                                                                      │
└──────────────────────────────────────────────────────────────────────┘
```

---

## Screen 3: Approval Queue

### State 3A: Default (With Pending Requests)

```
┌──────────────────────────────────────────────────────────────────────┐
│ Admin > System Configuration > Approvals                             │
├──────────────────────────────────────────────────────────────────────┤
│                                                                      │
│  Pending Requests (2)                                                │
│                                                                      │
│  ┌─────────────────────────────────────────────────────────────────┐ │
│  │ billing.credits.pro_monthly                                 🔶  │ │
│  │                                                                  │ │
│  │ Current: 500  →  Proposed: 750                                   │ │
│  │ Requested by: ops@recon.io                                       │ │
│  │ Reason: "Adjusting Pro allotment for Q2 pricing update"          │ │
│  │ Requested: 2026-03-11 10:30    Expires in: 18h 32m              │ │
│  │                                                                  │ │
│  │                                    [Reject]  [Approve]           │ │
│  └─────────────────────────────────────────────────────────────────┘ │
│                                                                      │
│  ┌─────────────────────────────────────────────────────────────────┐ │
│  │ versioning.api.min_deprecation_months                       🔶  │ │
│  │                                                                  │ │
│  │ Current: 6  →  Proposed: 12                                      │ │
│  │ Requested by: admin@recon.io         ← (You requested this)     │ │
│  │ Reason: "Extending deprecation window per customer feedback"     │ │
│  │ Requested: 2026-03-10 16:00    Expires in: 2h 00m               │ │
│  │                                                                  │ │
│  │                       (Awaiting another admin's approval)        │ │
│  └─────────────────────────────────────────────────────────────────┘ │
│                                                                      │
│  ─────────────────────────────────────────────────────────────────   │
│                                                                      │
│  Recent Decisions                                                    │
│                                                                      │
│  ┌─────────────────────────────────────────────────────────────────┐ │
│  │ Config Key                  Decision    Decided By   Timestamp  │ │
│  ├─────────────────────────────────────────────────────────────────┤ │
│  │ scanning.queue.max_depth    ● Approved  admin@…     Mar 9 14:00│ │
│  │ billing.credits.starter…    ● Rejected  ops@…       Mar 8 11:30│ │
│  │ tenant.suspension.grace…    ○ Expired   —           Mar 7 10:00│ │
│  └─────────────────────────────────────────────────────────────────┘ │
│                                                                      │
└──────────────────────────────────────────────────────────────────────┘
```

### State 3B: No Pending Requests

```
┌──────────────────────────────────────────────────────────────────────┐
│ Admin > System Configuration > Approvals                             │
├──────────────────────────────────────────────────────────────────────┤
│                                                                      │
│  Pending Requests (0)                                                │
│                                                                      │
│  ┌─────────────────────────────────────────────────────────────────┐ │
│  │                                                                  │ │
│  │         No pending approval requests.                            │ │
│  │         Critical config changes will appear here                 │ │
│  │         when another admin submits a request.                    │ │
│  │                                                                  │ │
│  └─────────────────────────────────────────────────────────────────┘ │
│                                                                      │
│  ─────────────────────────────────────────────────────────────────   │
│                                                                      │
│  Recent Decisions                                                    │
│  ...                                                                 │
│                                                                      │
└──────────────────────────────────────────────────────────────────────┘
```

### State 3C: Request Expiring Soon (<2 hours)

```
┌─────────────────────────────────────────────────────────────────┐
│ versioning.api.min_deprecation_months                       🔶  │
│                                                                  │
│ Current: 6  →  Proposed: 12                                      │
│ Requested by: admin@recon.io                                     │
│ Reason: "Extending deprecation window per customer feedback"     │
│ Requested: 2026-03-10 16:00                                      │
│                                                                  │
│ ⚠ Expires in: 1h 12m                                             │
│                                                                  │
│                                    [Reject]  [Approve]           │
└─────────────────────────────────────────────────────────────────┘
```

### State 3D: Loading

```
┌──────────────────────────────────────────────────────────────────────┐
│ Admin > System Configuration > Approvals                             │
├──────────────────────────────────────────────────────────────────────┤
│                                                                      │
│  Pending Requests                                                    │
│  ┌─────────────────────────────────────────────────────────────────┐ │
│  │ ░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░ │ │
│  │ ░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░ │ │
│  └─────────────────────────────────────────────────────────────────┘ │
│                                                                      │
└──────────────────────────────────────────────────────────────────────┘
```

---

## Screen 4: Rollback Confirmation Modal

```
┌─────────────────────────────────────────────────────────────┐
│ Rollback Configuration Change                                │
├─────────────────────────────────────────────────────────────┤
│                                                              │
│  Config: auth.lockout.duration_minutes                       │
│  Current value: 60                                           │
│  Revert to: 30                                               │
│                                                              │
│  Original change by admin@recon.io on 2026-03-11 14:22      │
│  Reason: "Increase lockout for brute force protection"       │
│                                                              │
│  Rollback reason: [                                       ]  │
│                                                              │
│                      [Cancel]  [Confirm Rollback]            │
│                                                              │
└─────────────────────────────────────────────────────────────┘
```

---

## Screen 5: Reject Reason Modal

```
┌─────────────────────────────────────────────────────────────┐
│ Reject Configuration Change                                  │
├─────────────────────────────────────────────────────────────┤
│                                                              │
│  Config: billing.credits.starter_monthly                     │
│  Proposed change: 100 → 50                                   │
│  Requested by: ops@recon.io                                  │
│                                                              │
│  Rejection reason: [                                      ]  │
│                                                              │
│                         [Cancel]  [Reject]                   │
│                                                              │
└─────────────────────────────────────────────────────────────┘
```

---

## Conditional Rendering Rules

| Condition | Effect |
|-----------|--------|
| `is_critical = true` | Show orange "Critical" badge. Edit form shows [Request Approval] instead of [Save]. |
| `is_sensitive = true` | Mask value as "••••••••". Show [Reveal] button. |
| `requires_restart = true` | Show yellow "Restart Required" chip on config row. |
| Active PENDING request for config | Disable [Edit]. Show pending approval inline status. |
| Current admin = request requester | Hide [Approve] / [Reject] on own request. Show "Awaiting another admin's approval". |
| Request expires in < 2 hours | Show warning color on expiry timer. |
| `history.rolled_back = true` | Show strikethrough on old→new values. Show "Rolled back" badge. Hide [Rollback] button. |
| `allowed_values` is set | Render dropdown instead of text input for STRING type. |
| Search active | Auto-expand matching categories. Auto-collapse non-matching. Show match count. |
