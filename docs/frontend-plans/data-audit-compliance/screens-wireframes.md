# Screens & Wireframes (Data, Audit & Platform Compliance)

Scope: ASCII wireframes for privacy center, audit log, data export/deletion flows, and admin backup/migration views.

---

## Route Structure

| Route | Screen | Access |
|-------|--------|--------|
| `/privacy` | Privacy Center | Tenant Owner (full), Member (inventory only) |
| `/privacy/audit-log` | Tenant Audit Log | Tenant Owner + Members |
| `/admin/audit-logs` | Admin Cross-Tenant Audit Log | SUPER_ADMIN |
| `/admin/data` | Admin Data Management (tabbed) | SUPER_ADMIN |
| (overlay) | Data Deletion Modal | Tenant Owner |

---

## Screen 1: Privacy Center

### State 1A: Default (No Active Export, No Pending Deletion)

```
┌──────────────────────────────────────────────────────────────────────┐
│ ┌─ App Header ────────────────────────────────────────────────────┐  │
│ │ Reconova                                                    👤  │  │
│ └─────────────────────────────────────────────────────────────────┘  │
├────────────┬─────────────────────────────────────────────────────────┤
│            │                                                         │
│  Sidebar   │  Privacy Center                                         │
│            │                                                         │
│  Dashboard │  ┌─ Data Inventory ─────────────────────────────────┐   │
│  Scans     │  │                                                  │   │
│  Domains   │  │  What data we store about you                    │   │
│  ...       │  │                                                  │   │
│  Settings  │  │  Category          Retention       Deletable     │   │
│  Privacy   │  │  ──────────────── ──────────────── ──────────    │   │
│  ──────    │  │  Scan Results      90 days (Pro)   Yes           │   │
│            │  │  Billing Info      Lifetime         No           │   │
│            │  │  Audit Logs        Min. 1 year      No           │   │
│            │  │  Account Info      Active lifetime   Yes          │   │
│            │  │  Notif. History    90 days           Yes          │   │
│            │  │  Compliance        90 days (Pro)     Yes          │   │
│            │  │                                                  │   │
│            │  │  Third-Party Processors                          │   │
│            │  │  ┌──────────────┬──────────────┬──────────────┐  │   │
│            │  │  │ Stripe       │ Payments     │ Email, tokens│  │   │
│            │  │  │ Shodan et al │ Scan enrich. │ Domains      │  │   │
│            │  │  └──────────────┴──────────────┴──────────────┘  │   │
│            │  │                                                  │   │
│            │  └──────────────────────────────────────────────────┘   │
│            │                                                         │
│            │  ┌─ Export Your Data ────────────────────────────────┐   │
│            │  │                                                  │   │
│            │  │  Download a full copy of your tenant data        │   │
│            │  │  including scans, domains, billing, and more.    │   │
│            │  │                                                  │   │
│            │  │  [Request Export]                                 │   │
│            │  │                                                  │   │
│            │  └──────────────────────────────────────────────────┘   │
│            │                                                         │
│            │  ┌─ Delete Your Account ────────────────────────────┐   │
│            │  │                                                  │   │
│            │  │  Permanently delete your account and all data.   │   │
│            │  │  This action has a 72-hour cooling-off period.   │   │
│            │  │                                                  │   │
│            │  │  [Delete My Account]                             │   │
│            │  │                                                  │   │
│            │  └──────────────────────────────────────────────────┘   │
│            │                                                         │
│            │  [View Audit Log →]                                     │
│            │                                                         │
└────────────┴─────────────────────────────────────────────────────────┘
```

### State 1B: Export Processing

```
│            │  ┌─ Export Your Data ────────────────────────────────┐   │
│            │  │                                                  │   │
│            │  │  ℹ Your data export is being prepared.           │   │
│            │  │  We'll notify you by email when it's ready.      │   │
│            │  │                                                  │   │
│            │  │  Status: Processing ◌                            │   │
│            │  │  Requested: 2026-03-11 10:30 UTC                 │   │
│            │  │                                                  │   │
│            │  └──────────────────────────────────────────────────┘   │
```

### State 1C: Export Ready

```
│            │  ┌─ Export Your Data ────────────────────────────────┐   │
│            │  │                                                  │   │
│            │  │  ✓ Your data export is ready.                    │   │
│            │  │                                                  │   │
│            │  │  File size: 47.2 MB                              │   │
│            │  │  Available for: 22 hours 15 minutes              │   │
│            │  │                                                  │   │
│            │  │  [Download Export]                                │   │
│            │  │                                                  │   │
│            │  │  ⚠ The download link expires after first use     │   │
│            │  │  or after 24 hours, whichever comes first.       │   │
│            │  │                                                  │   │
│            │  └──────────────────────────────────────────────────┘   │
```

### State 1D: Export Ready (Multi-Part)

```
│            │  ┌─ Export Your Data ────────────────────────────────┐   │
│            │  │                                                  │   │
│            │  │  ✓ Your data export is ready.                    │   │
│            │  │                                                  │   │
│            │  │  Total size: 1.3 GB (3 parts)                    │   │
│            │  │  Available for: 22 hours 15 minutes              │   │
│            │  │                                                  │   │
│            │  │  [Download Part 1 (500 MB)]                      │   │
│            │  │  [Download Part 2 (500 MB)]                      │   │
│            │  │  [Download Part 3 (300 MB)]                      │   │
│            │  │                                                  │   │
│            │  └──────────────────────────────────────────────────┘   │
```

### State 1E: Export Expired

```
│            │  ┌─ Export Your Data ────────────────────────────────┐   │
│            │  │                                                  │   │
│            │  │  Your previous export has expired.               │   │
│            │  │  Request a new one to download your data.        │   │
│            │  │                                                  │   │
│            │  │  [Request Export]                                 │   │
│            │  │                                                  │   │
│            │  └──────────────────────────────────────────────────┘   │
```

### State 1F: Deletion Pending

```
│            │                                                         │
│  Sidebar   │  Privacy Center                                         │
│            │                                                         │
│            │  ┌──────────────────────────────────────────────────┐   │
│            │  │ ⚠ Account deletion scheduled for 2026-03-14     │   │
│            │  │ at 10:30 UTC. You have 68h 15m to cancel.       │   │
│            │  │                               [Cancel Deletion]  │   │
│            │  └──────────────────────────────────────────────────┘   │
│            │                                                         │
│            │  ┌─ Data Inventory ─────────────────────────────────┐   │
│            │  │  (... content as State 1A ...)                   │   │
│            │  └──────────────────────────────────────────────────┘   │
│            │                                                         │
│            │  ┌─ Export Your Data ────────────────────────────────┐   │
│            │  │                                                  │   │
│            │  │  Data export unavailable during pending deletion. │   │
│            │  │                                                  │   │
│            │  └──────────────────────────────────────────────────┘   │
│            │                                                         │
│            │  ┌─ Delete Your Account ────────────────────────────┐   │
│            │  │                                                  │   │
│            │  │  Deletion scheduled: 2026-03-14 10:30 UTC        │   │
│            │  │  Time remaining: 68h 15m                         │   │
│            │  │                                                  │   │
│            │  │  [Cancel Deletion]                               │   │
│            │  │                                                  │   │
│            │  └──────────────────────────────────────────────────┘   │
```

### State 1G: Deletion Pending — Less Than 1 Hour

```
│            │  ┌──────────────────────────────────────────────────┐   │
│            │  │ 🔴 Account deletion in 45 minutes. This cannot   │   │
│            │  │ be undone once executed.      [Cancel Deletion]   │   │
│            │  └──────────────────────────────────────────────────┘   │
│            │    ▲ red background, urgent styling                     │
```

### State 1H: Loading

```
│            │  Privacy Center                                         │
│            │                                                         │
│            │  ┌──────────────────────────────────────────────────┐   │
│            │  │ ░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░ │   │
│            │  │ ░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░ │   │
│            │  └──────────────────────────────────────────────────┘   │
│            │  ┌──────────────────────────────────────────────────┐   │
│            │  │ ░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░ │   │
│            │  └──────────────────────────────────────────────────┘   │
│            │  ┌──────────────────────────────────────────────────┐   │
│            │  │ ░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░ │   │
│            │  └──────────────────────────────────────────────────┘   │
```

### State 1I: Tenant Member View

```
│            │  Privacy Center                                         │
│            │                                                         │
│            │  ┌─ Data Inventory ─────────────────────────────────┐   │
│            │  │  (... full inventory visible ...)                │   │
│            │  └──────────────────────────────────────────────────┘   │
│            │                                                         │
│            │  (Export and Delete sections not shown for members)      │
│            │                                                         │
│            │  [View Audit Log →]                                     │
```

### State 1J: Suspended Tenant

```
│            │  Privacy Center                                         │
│            │                                                         │
│            │  ┌──────────────────────────────────────────────────┐   │
│            │  │ ⚠ Your account is suspended. Some features are   │   │
│            │  │ unavailable. Contact support for assistance.     │   │
│            │  └──────────────────────────────────────────────────┘   │
│            │                                                         │
│            │  ┌─ Data Inventory ─────────────────────────────────┐   │
│            │  │  (... visible, read-only ...)                    │   │
│            │  └──────────────────────────────────────────────────┘   │
│            │                                                         │
│            │  ┌─ Export Your Data ────────────────────────────────┐   │
│            │  │  Unavailable while account is suspended.          │   │
│            │  └──────────────────────────────────────────────────┘   │
│            │                                                         │
│            │  ┌─ Delete Your Account ────────────────────────────┐   │
│            │  │  Unavailable while account is suspended.          │   │
│            │  └──────────────────────────────────────────────────┘   │
│            │                                                         │
│            │  [View Audit Log →]                                     │
```

---

## Screen 2: Data Deletion Modal

### State 2A: Step 1 — Warning + Confirmation Phrase

```
┌──────────────────────────────────────────────────────────────┐
│                                                              │
│  Delete Your Account                                  ✕     │
│                                                              │
│  ○───●───○  Step 1 of 3: Confirm Intent                     │
│                                                              │
│  ┌────────────────────────────────────────────────────────┐  │
│  │ ⚠ Warning — This action is permanent                  │  │
│  │                                                        │  │
│  │ Deleting your account will:                            │  │
│  │ • Permanently delete all scan data and results         │  │
│  │ • Cancel your active subscription                      │  │
│  │ • Remove all integrations and configurations           │  │
│  │ • Soft-delete billing records for compliance           │  │
│  │                                                        │  │
│  │ Audit logs and credit transactions are retained        │  │
│  │ per legal requirements.                                │  │
│  └────────────────────────────────────────────────────────┘  │
│                                                              │
│  Type "DELETE acme-corp" to confirm:                         │
│  ┌────────────────────────────────────────────────────────┐  │
│  │                                                        │  │
│  └────────────────────────────────────────────────────────┘  │
│                                                              │
│                               [Cancel]  [Next →]            │
│                                                              │
└──────────────────────────────────────────────────────────────┘
```

### State 2B: Step 1 — Validation Error

```
│  Type "DELETE acme-corp" to confirm:                         │
│  ┌────────────────────────────────────────────────────────┐  │
│  │ delete acme-corp                                      │  │
│  └────────────────────────────────────────────────────────┘  │
│  ⚠ Phrase must match exactly: DELETE acme-corp               │
│    (case-sensitive)                                          │
│                                                              │
│                               [Cancel]  [Next →]            │
│                                        ▲ disabled            │
```

### State 2C: Step 2 — Re-Authentication

```
┌──────────────────────────────────────────────────────────────┐
│                                                              │
│  Delete Your Account                                  ✕     │
│                                                              │
│  ●───○───○  Step 2 of 3: Verify Identity                    │
│                                                              │
│  Re-enter your credentials to continue.                      │
│                                                              │
│  Password                                                    │
│  ┌────────────────────────────────────────────────────────┐  │
│  │ ••••••••                                              │  │
│  └────────────────────────────────────────────────────────┘  │
│                                                              │
│  2FA Code                                                    │
│  ┌────────────┐                                              │
│  │            │                                              │
│  └────────────┘                                              │
│                                                              │
│                            [← Back]  [Verify →]             │
│                                                              │
└──────────────────────────────────────────────────────────────┘
```

### State 2D: Step 2 — Auth Error

```
│  Password                                                    │
│  ┌────────────────────────────────────────────────────────┐  │
│  │ ••••••••                                              │  │
│  └────────────────────────────────────────────────────────┘  │
│  ⚠ Incorrect password.                                      │
│                                                              │
│  2FA Code                                                    │
│  ┌────────────┐                                              │
│  │ 123456     │                                              │
│  └────────────┘                                              │
│  ⚠ Invalid 2FA code.                                        │
```

### State 2E: Step 3 — Final Confirmation

```
┌──────────────────────────────────────────────────────────────┐
│                                                              │
│  Delete Your Account                                  ✕     │
│                                                              │
│  ●───●───○  Step 3 of 3: Final Confirmation                 │
│                                                              │
│  Your account will be scheduled for deletion.                │
│                                                              │
│  ┌────────────────────────────────────────────────────────┐  │
│  │ • Deletion executes in 72 hours                       │  │
│  │ • You can cancel anytime within 72 hours              │  │
│  │ • You'll receive an email confirmation                │  │
│  │ • After 72 hours, deletion is irreversible            │  │
│  └────────────────────────────────────────────────────────┘  │
│                                                              │
│                       [← Back]  [Confirm Deletion]          │
│                                  ▲ red button                │
│                                                              │
└──────────────────────────────────────────────────────────────┘
```

### State 2F: Submitting

```
│                       [← Back]  [Deleting... ◌]             │
│                                  ▲ disabled, spinner         │
```

---

## Screen 3: Tenant Audit Log

### State 3A: Default (With Entries)

```
┌──────────────────────────────────────────────────────────────────────┐
│ ┌─ App Header ────────────────────────────────────────────────────┐  │
│ │ Reconova                                                    👤  │  │
│ └─────────────────────────────────────────────────────────────────┘  │
├────────────┬─────────────────────────────────────────────────────────┤
│            │                                                         │
│  Sidebar   │  ← Privacy Center  /  Audit Log                        │
│            │                                                         │
│  Privacy   │  ┌─ Filters ────────────────────────────────────────┐   │
│  ──────    │  │                                                  │   │
│            │  │  Date Range: [2026-03-04] to [2026-03-11]        │   │
│            │  │  Category: [All Categories ▼]                    │   │
│            │  │  Search: [________________________] 🔍           │   │
│            │  │                                                  │   │
│            │  └──────────────────────────────────────────────────┘   │
│            │                                                         │
│            │  Showing 127 entries                                     │
│            │                                                         │
│            │  ┌─ Log Table ──────────────────────────────────────┐   │
│            │  │                                                  │   │
│            │  │ Timestamp          Action          User     IP   │   │
│            │  │ ──────────────── ──────────────── ──────── ───── │   │
│            │  │ Mar 11 14:32     scan.completed   john@..  .100 │   │
│            │  │ Mar 11 14:30     scan.started     john@..  .100 │   │
│            │  │ Mar 11 12:15     auth.login       jane@..  .205 │   │
│            │  │ Mar 11 10:00     billing.credit   john@..  .100 │   │
│            │  │                  _purchased                      │   │
│            │  │ Mar 10 22:45     domain.added     john@..  .100 │   │
│            │  │ Mar 10 18:30     integration      john@..  .100 │   │
│            │  │                  .created                        │   │
│            │  │ Mar 10 16:20     scan.created     jane@..  .205 │   │
│            │  │ ...                                              │   │
│            │  │                                                  │   │
│            │  │ [← Previous]   Page 1 of 3   [Next →]           │   │
│            │  │                                                  │   │
│            │  └──────────────────────────────────────────────────┘   │
│            │                                                         │
└────────────┴─────────────────────────────────────────────────────────┘
```

### State 3B: Entry Expanded

```
│            │  │ Mar 11 14:32     scan.completed   john@..  .100 ▼│   │
│            │  │ ┌────────────────────────────────────────────┐   │   │
│            │  │ │ Action: scan.completed                     │   │   │
│            │  │ │ Timestamp: 2026-03-11 14:32:05 UTC         │   │   │
│            │  │ │ User: john@acme.com                        │   │   │
│            │  │ │ IP: 192.168.1.100                          │   │   │
│            │  │ │ User Agent: Mozilla/5.0 (Mac...)           │   │   │
│            │  │ │ Resource: scan_job / job_abc123             │   │   │
│            │  │ │                                            │   │   │
│            │  │ │ Details:                                   │   │   │
│            │  │ │ {                                          │   │   │
│            │  │ │   "scan_type": "full",                     │   │   │
│            │  │ │   "domains_scanned": 5,                    │   │   │
│            │  │ │   "duration_seconds": 272,                 │   │   │
│            │  │ │   "credits_consumed": 15                   │   │   │
│            │  │ │ }                                          │   │   │
│            │  │ │                              [Copy JSON]   │   │   │
│            │  │ └────────────────────────────────────────────┘   │   │
│            │  │ Mar 11 14:30     scan.started     john@..  .100 │   │
```

### State 3C: Entry With Impersonation Badge

```
│            │  │ Mar 11 14:32     scan.completed   john@..  .100 │   │
│            │  │                  🔷 Via impersonation            │   │
│            │  │ ┌────────────────────────────────────────────┐   │   │
│            │  │ │ ...                                        │   │   │
│            │  │ │ Performed during impersonation by          │   │   │
│            │  │ │ admin@reconova.io                          │   │   │
│            │  │ └────────────────────────────────────────────┘   │   │
```

### State 3D: Empty (No Results)

```
│            │  Showing 0 entries                                       │
│            │                                                         │
│            │  ┌──────────────────────────────────────────────────┐   │
│            │  │                                                  │   │
│            │  │   No audit log entries match your filters.       │   │
│            │  │   Try adjusting the date range or category.      │   │
│            │  │                                                  │   │
│            │  └──────────────────────────────────────────────────┘   │
```

### State 3E: Loading

```
│            │  ┌─ Filters ────────────────────────────────────────┐   │
│            │  │  (... filters visible ...)                       │   │
│            │  └──────────────────────────────────────────────────┘   │
│            │                                                         │
│            │  ┌──────────────────────────────────────────────────┐   │
│            │  │ ░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░ │   │
│            │  │ ░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░ │   │
│            │  │ ░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░ │   │
│            │  │ ░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░ │   │
│            │  └──────────────────────────────────────────────────┘   │
```

---

## Screen 4: Admin Cross-Tenant Audit Log

### State 4A: Default

```
┌──────────────────────────────────────────────────────────────────────┐
│ ┌─ Admin Header ──────────────────────────────────────────────────┐  │
│ │ Reconova Admin                    🔔 ·  ⚙ Maintenance: Off  👤 │  │
│ └─────────────────────────────────────────────────────────────────┘  │
├────────────┬─────────────────────────────────────────────────────────┤
│            │                                                         │
│  Admin     │  Audit Logs                                [Export CSV] │
│  Sidebar   │                                                         │
│            │  ┌─ Filters ────────────────────────────────────────┐   │
│            │  │                                                  │   │
│            │  │  Tenant: [All Tenants        ▼]                  │   │
│            │  │  User:   [_________________]                     │   │
│            │  │  Date:   [2026-03-04] to [2026-03-11]            │   │
│            │  │  Category: [All Categories ▼]                    │   │
│            │  │  Search: [________________________] 🔍           │   │
│            │  │  □ Super admin actions only                      │   │
│            │  │                                                  │   │
│            │  └──────────────────────────────────────────────────┘   │
│            │                                                         │
│            │  Showing 2,451 entries                                   │
│            │                                                         │
│            │  ┌─ Log Table ──────────────────────────────────────┐   │
│            │  │                                                  │   │
│            │  │ Timestamp     Tenant    Action       User    IP  │   │
│            │  │ ──────────── ──────── ──────────── ─────── ───── │   │
│            │  │ Mar 11 14:32 Acme     scan.compl.  john@.. .100 │   │
│            │  │ Mar 11 14:30 Acme     scan.started john@.. .100 │   │
│            │  │ Mar 11 14:25 —        admin.config admin@  .50  │   │
│            │  │              ▲ platform action, highlighted bg    │   │
│            │  │ Mar 11 14:20 Beta Co  auth.login   sara@.. .77  │   │
│            │  │ Mar 11 14:15 [Del.]   billing.ref  —       .50  │   │
│            │  │              Gamma    und_issued                  │   │
│            │  │              ▲ deleted tenant shown as [Del.]     │   │
│            │  │ ...                                              │   │
│            │  │                                                  │   │
│            │  │ [← Previous]  Page 1 of 50  [Next →]            │   │
│            │  │                                                  │   │
│            │  └──────────────────────────────────────────────────┘   │
│            │                                                         │
└────────────┴─────────────────────────────────────────────────────────┘
```

### State 4B: Filtered by Super Admin Actions

```
│            │  ┌─ Filters ────────────────────────────────────────┐   │
│            │  │  ...                                             │   │
│            │  │  ☑ Super admin actions only                      │   │
│            │  └──────────────────────────────────────────────────┘   │
│            │                                                         │
│            │  Showing 89 entries                                      │
│            │                                                         │
│            │  │ Timestamp     Tenant    Action           User    │   │
│            │  │ ──────────── ──────── ────────────────── ─────── │   │
│            │  │ Mar 11 14:25 —        admin.config_chng  admin@  │   │
│            │  │ Mar 11 12:00 Acme     admin.imperson..   admin@  │   │
│            │  │              ▲ all rows have highlighted bg       │   │
│            │  │ Mar 10 18:30 —        admin.api_key_rot  admin@  │   │
```

### State 4C: Loading / Empty / Error

```
(Same patterns as tenant audit log States 3D, 3E)
```

---

## Screen 5: Admin Data — Backups Tab

### State 5A: Default

```
┌──────────────────────────────────────────────────────────────────────┐
│ ┌─ Admin Header ──────────────────────────────────────────────────┐  │
│ │ Reconova Admin                    🔔 ·  ⚙ Maintenance: Off  👤 │  │
│ └─────────────────────────────────────────────────────────────────┘  │
├────────────┬─────────────────────────────────────────────────────────┤
│            │                                                         │
│  Admin     │  Data Management                                        │
│  Sidebar   │                                                         │
│            │  [Backups]  [Migrations]                                 │
│            │   ▲ active tab                                          │
│            │                                                         │
│            │  ┌──────────┐ ┌──────────┐ ┌──────────┐ ┌──────────┐  │
│            │  │ Backed Up│ │ Failed   │ │ Last Run │ │ Storage  │  │
│            │  │ (24h)    │ │          │ │          │ │ Used     │  │
│            │  │  1,245   │ │  🔴 2    │ │ 03:00    │ │  234 GB  │  │
│            │  │          │ │          │ │ UTC      │ │          │  │
│            │  └──────────┘ └──────────┘ └──────────┘ └──────────┘  │
│            │                                                         │
│            │  ┌─ Backup Status ──────────────────────────────────┐   │
│            │  │                                                  │   │
│            │  │ Tenant          Last Backup     Status   Size    │   │
│            │  │ ──────────────── ──────────────── ─────── ────── │   │
│            │  │ Acme Corp       Mar 11 03:00    ● OK     120MB  │   │
│            │  │ Beta Co         Mar 11 03:02    ● OK      89MB  │   │
│            │  │ 🔴 Gamma Inc    Mar 11 03:05   ● FAILED   —    │   │
│            │  │   Error: Connection timeout                      │   │
│            │  │                              [Retry Backup]      │   │
│            │  │ 🔴 Delta LLC    Mar 11 03:08   ● FAILED   —    │   │
│            │  │   Error: Disk space insufficient                 │   │
│            │  │                              [Retry Backup]      │   │
│            │  │ Epsilon Ltd     Mar 11 03:10    ● OK      45MB  │   │
│            │  │ ...                                              │   │
│            │  │                                                  │   │
│            │  │ [← Previous]   Page 1 of 25   [Next →]          │   │
│            │  │                                                  │   │
│            │  └──────────────────────────────────────────────────┘   │
│            │                                                         │
└────────────┴─────────────────────────────────────────────────────────┘
```

### State 5B: All Backups Healthy

```
│            │  ┌──────────┐ ┌──────────┐ ┌──────────┐ ┌──────────┐  │
│            │  │ Backed Up│ │ Failed   │ │ Last Run │ │ Storage  │  │
│            │  │ (24h)    │ │          │ │          │ │ Used     │  │
│            │  │  1,247   │ │  ● 0     │ │ 03:00    │ │  234 GB  │  │
│            │  │          │ │          │ │ UTC      │ │          │  │
│            │  └──────────┘ └──────────┘ └──────────┘ └──────────┘  │
│            │                                                         │
│            │  (all rows show ● OK status, no retry buttons)          │
```

### State 5C: Loading

```
│            │  ┌──────────┐ ┌──────────┐ ┌──────────┐ ┌──────────┐  │
│            │  │ ░░░░░░░░ │ │ ░░░░░░░░ │ │ ░░░░░░░░ │ │ ░░░░░░░░ │  │
│            │  └──────────┘ └──────────┘ └──────────┘ └──────────┘  │
│            │                                                         │
│            │  ┌──────────────────────────────────────────────────┐   │
│            │  │ ░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░ │   │
│            │  │ ░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░ │   │
│            │  └──────────────────────────────────────────────────┘   │
```

---

## Screen 6: Admin Data — Migrations Tab

### State 6A: Default

```
│            │  Data Management                                        │
│            │                                                         │
│            │  [Backups]  [Migrations]                                 │
│            │              ▲ active tab                                │
│            │                                                         │
│            │  ┌─ Recent Migrations ──────────────────────────────┐   │
│            │  │                                                  │   │
│            │  │ Migration ID         Type   Applied   Tenants    │   │
│            │  │                             At        Status     │   │
│            │  │ ──────────────────── ────── ──────── ──────────  │   │
│            │  │ 2026_03_10_add_col   BASE   Mar 10   ● COMPLETE │   │
│            │  │   Integrity: ✓ Passed           1247/1247       │   │
│            │  │                                                  │   │
│            │  │ 2026_03_08_new_idx   BASE   Mar 08   🟡PARTIAL  │   │
│            │  │   Integrity: ✓ Passed           1244/1247       │   │
│            │  │   ⚠ 3 tenants skipped (conflicts)               │   │
│            │  │                                                  │   │
│            │  │ 2026_03_05_update    BASE   Mar 05   ● COMPLETE │   │
│            │  │   Integrity: ✓ Passed           1247/1247       │   │
│            │  │                                                  │   │
│            │  │ 2026_03_01_tenant    TENANT Mar 01   ● COMPLETE │   │
│            │  │   Tenant: Acme Corp                              │   │
│            │  │   Integrity: — Not checked                       │   │
│            │  │                    [Run Integrity Check]         │   │
│            │  │                                                  │   │
│            │  │ [← Previous]   Page 1 of 8   [Next →]           │   │
│            │  │                                                  │   │
│            │  └──────────────────────────────────────────────────┘   │
```

### State 6B: Migration Row Expanded (With Conflicts)

```
│            │  │ 2026_03_08_new_idx   BASE   Mar 08   🟡PARTIAL  │   │
│            │  │ ┌────────────────────────────────────────────┐   │   │
│            │  │ │ Description: Add index on scan_results     │   │   │
│            │  │ │ Tables affected: scan_results              │   │   │
│            │  │ │ Applied to: 1244 of 1247 tenants           │   │   │
│            │  │ │                                            │   │   │
│            │  │ │ Conflicts (3 tenants skipped):             │   │   │
│            │  │ │ ┌────────┬────────────────┬───────┬──────┐│   │   │
│            │  │ │ │ Tenant │ Conflict Migr. │ Table │ Col  ││   │   │
│            │  │ │ ├────────┼────────────────┼───────┼──────┤│   │   │
│            │  │ │ │ Acme   │ tm_001_custom  │ s_res │ meta ││   │   │
│            │  │ │ │ Beta   │ tm_003_addon   │ s_res │ meta ││   │   │
│            │  │ │ │ Gamma  │ tm_002_extend  │ s_res │ opts ││   │   │
│            │  │ │ └────────┴────────────────┴───────┴──────┘│   │   │
│            │  │ │                                            │   │   │
│            │  │ │ These tenants require manual resolution.   │   │   │
│            │  │ └────────────────────────────────────────────┘   │   │
```

### State 6C: Migration With Integrity Issues

```
│            │  │ 2026_03_10_add_col   BASE   Mar 10   🟠REVIEW   │   │
│            │  │   Integrity: ⚠ Issues found                      │   │
│            │  │ ┌────────────────────────────────────────────┐   │   │
│            │  │ │ Integrity Check Results:                    │   │   │
│            │  │ │ ✓ Foreign key constraints: OK               │   │   │
│            │  │ │ ⚠ Index validation: 2 invalid indexes       │   │   │
│            │  │ │ ✓ CHECK constraints: OK                     │   │   │
│            │  │ │                                            │   │   │
│            │  │ │ Tenants affected: Acme Corp, Delta LLC     │   │   │
│            │  │ │ Manual review recommended.                 │   │   │
│            │  │ └────────────────────────────────────────────┘   │   │
```

### State 6D: Loading

```
(Same skeleton pattern as other tabs)
```

---

## Screen 7: Export Request Confirmation

```
┌──────────────────────────────────────────────────────────────┐
│                                                              │
│  Request Data Export                                  ✕     │
│                                                              │
│  Request a full export of your tenant data?                  │
│                                                              │
│  The export will include:                                    │
│  • Scan results, domains, and subdomains                     │
│  • Billing history and credit transactions                   │
│  • Compliance reports                                        │
│  • Integration configurations                                │
│  • Audit logs (tenant-scoped)                                │
│                                                              │
│  You'll be notified by email when the export is ready.       │
│  The download link expires after 24 hours.                   │
│                                                              │
│                         [Cancel]  [Request Export]            │
│                                                              │
└──────────────────────────────────────────────────────────────┘
```

---

## Screen 8: Cancel Deletion Confirmation

```
┌──────────────────────────────────────────────────────────────┐
│                                                              │
│  Cancel Account Deletion?                             ✕     │
│                                                              │
│  Cancel your account deletion request?                       │
│  Your account will remain active and no data will be         │
│  deleted.                                                    │
│                                                              │
│              [Keep Deletion]  [Cancel Deletion]              │
│                                                              │
└──────────────────────────────────────────────────────────────┘
```

---

## Conditional Rendering Rules

| Condition | Effect |
|-----------|--------|
| `role === TENANT_MEMBER` | Hide export + deletion sections. Show inventory + audit log link. |
| `role === TENANT_OWNER` | Show all sections. |
| `tenant.status === 'SUSPENDED'` | Show suspended banner. Disable export + deletion buttons. |
| `export.status === 'PROCESSING'` | Show processing indicator. Hide request button. |
| `export.status === 'READY'` | Show download button + expiry countdown. |
| `deletion.status === 'PENDING'` | Show deletion countdown banner. Disable export. Show cancel button. |
| `deletion.countdown < 1 hour` | Red urgent banner styling. |
| `audit_log.impersonated_by !== null` | Show impersonation badge on log entry. |
| `audit_log.is_super_admin === true` | Highlight row in admin view. |
| `tenant.deleted_at !== null` (admin) | Show "[Deleted]" prefix on tenant name. |
| `migration.status === 'PARTIAL'` | Show yellow badge + conflict count. |
| `migration.integrity === 'ISSUES'` | Show orange NEEDS_REVIEW badge. |
| `backup.status === 'FAILED'` | Highlight row red. Show error + [Retry]. |
