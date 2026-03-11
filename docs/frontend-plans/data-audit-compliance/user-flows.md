# User Flows (Data, Audit & Platform Compliance)

Scope: User journey flowcharts for privacy center, data export, data deletion, data inventory, audit log viewer, and admin backup/migration views.

---

## Preconditions

| Condition | Requirement |
|-----------|-------------|
| Authentication | User must be logged in with valid session |
| Role (tenant) | TENANT_OWNER for export/deletion actions. TENANT_MEMBER for read-only audit log. |
| Role (admin) | SUPER_ADMIN for cross-tenant audit, backup, migration views |
| Tenant status | Tenant must be ACTIVE (suspended tenants can still view audit log but cannot request export/deletion) |

---

## Flow 1: Privacy Center Page Load

```
[Tenant Owner] → navigate to /privacy
    │ (analytics: privacy_center_viewed)
    ▼
Fetch in parallel:
    ├─ GET /api/data-inventory
    ├─ GET /api/data/export/status
    └─ GET /api/data/deletion/status
         │
         ├─ Loading → skeleton for all three sections
         │
         ├─ Success → render privacy center
         │    │
         │    ├─ Data Inventory section (always visible)
         │    │   ├─ Data categories table with retention per tier
         │    │   └─ Third-party processors list
         │    │
         │    ├─ Export Data section
         │    │   ├─ No active export → show [Request Export] button
         │    │   ├─ PROCESSING → show progress indicator + info banner
         │    │   ├─ READY → show [Download] button + expiry countdown
         │    │   └─ EXPIRED → show "Export expired" text + [Request Export]
         │    │
         │    └─ Delete Account section
         │        ├─ No pending deletion → show [Delete My Account] button
         │        ├─ PENDING → show warning banner with countdown + [Cancel Deletion]
         │        └─ TENANT_MEMBER role → section hidden (no delete permission)
         │
         └─ Error → error state with [Retry]
              (analytics: privacy_center_load_error, { error_code })
```

---

## Flow 2: View Data Inventory

```
[Tenant Owner or Member] → scrolls to Data Inventory section
    │ (analytics: data_inventory_viewed)
    ▼
Data inventory displays:
    │
    ├─ Data Categories table:
    │   ├─ Category name
    │   ├─ Description
    │   ├─ Retention period (dynamically shows tenant's tier retention)
    │   ├─ Location (Tenant DB / Control DB)
    │   └─ Deletable (Yes/No badge)
    │
    └─ Third-Party Processors:
        ├─ Processor name
        ├─ Purpose
        └─ Data shared
    │
    ▼
Static content — no further API calls
Read-only for all roles
```

---

## Flow 3: Request Data Export

```
[Tenant Owner] → click [Request Export]
    │ (analytics: data_export_requested)
    ▼
Show confirmation dialog:
    "Request a full export of your tenant data?"
    "The export will include scan results, domains, billing history,
     compliance reports, and audit logs."
    "You'll be notified when the export is ready to download."
    [Cancel] [Request Export]
    │
    ├─ Cancel → close dialog
    │
    └─ Confirm →
         │
         ▼
    POST /api/data/export
         │
         ├─ Success →
         │    ├─ Close dialog
         │    ├─ Toast (success): "Data export requested. We'll notify you when it's ready."
         │    ├─ Export section updates to PROCESSING state
         │    │   (analytics: data_export_submitted)
         │    └─ Show progress indicator
         │
         ├─ ERR_DATA_001 →
         │    Toast (error): "An export is already in progress."
         │    Close dialog
         │
         └─ Error →
              Toast (error): "Failed to request export." + [Retry]
```

---

## Flow 4: Export Processing → Ready Notification

```
[Export processing in background]
    │
    ▼
User visits /privacy (or is already on page)
    │
    ▼
GET /api/data/export/status returns READY
    │
    ├─ Export section updates:
    │   ├─ Success banner: "Your data export is ready."
    │   ├─ [Download Export] button
    │   ├─ File size display: "{size} MB"
    │   ├─ Expiry countdown: "Available for {hours} more hours"
    │   └─ If multi-part (>1GB): "Export split into {n} parts"
    │
    └─ Also: email notification sent by backend
         (analytics: data_export_ready_viewed)
```

---

## Flow 5: Download Data Export

```
[Tenant Owner] → click [Download Export]
    │ (analytics: data_export_downloaded)
    ▼
GET /api/data/export/download
    │
    ├─ Single file → browser downloads ZIP file
    │
    ├─ Multi-part → browser downloads manifest with links
    │   Each link → individual ZIP part download
    │
    ├─ Success →
    │    ├─ Toast (success): "Download started."
    │    ├─ Export transitions to EXPIRED after first download
    │    └─ Section updates to show "Export downloaded. Request a new one if needed."
    │
    ├─ ERR_DATA_002 →
    │    Toast (error): "Export has expired. Request a new one."
    │    Update section to EXPIRED state
    │
    └─ Error →
         Toast (error): "Download failed. Try again."
```

---

## Flow 6: Request Data Deletion — Multi-Step Modal

```
[Tenant Owner] → click [Delete My Account]
    │ (analytics: data_deletion_modal_opened)
    ▼
Open multi-step modal — Step 1: Warning + Confirmation Phrase
    │
    ┌─────────────────────────────────────────────────┐
    │ Delete Your Account                       ✕     │
    │                                                 │
    │ Step 1 of 3                                     │
    │                                                 │
    │ ⚠ This action is permanent and irreversible.   │
    │                                                 │
    │ Deleting your account will:                     │
    │ • Permanently delete all scan data              │
    │ • Cancel your subscription                      │
    │ • Remove all integrations and configurations    │
    │ • Soft-delete billing records for compliance    │
    │                                                 │
    │ Type "DELETE {tenant-slug}" to confirm:          │
    │ [_________________________________]             │
    │                                                 │
    │                        [Cancel]  [Next →]       │
    └─────────────────────────────────────────────────┘
    │
    ├─ Cancel → close modal
    │
    ├─ Phrase doesn't match → inline error:
    │   "Phrase must match exactly: DELETE {tenant-slug}"
    │
    └─ Phrase matches → advance to Step 2
         │ (analytics: data_deletion_phrase_confirmed)
         ▼
    Step 2: Re-Authentication
    │
    ┌─────────────────────────────────────────────────┐
    │ Delete Your Account                       ✕     │
    │                                                 │
    │ Step 2 of 3                                     │
    │                                                 │
    │ Re-enter your credentials to continue.          │
    │                                                 │
    │ Password                                        │
    │ [_________________________________]             │
    │                                                 │
    │ 2FA Code                                        │
    │ [______]                                        │
    │                                                 │
    │                     [← Back]  [Verify →]        │
    └─────────────────────────────────────────────────┘
    │
    ├─ Back → return to Step 1 (phrase preserved)
    │
    ├─ Auth fails →
    │   ├─ Wrong password → inline error: "Incorrect password."
    │   ├─ Wrong 2FA → inline error: "Invalid 2FA code."
    │   └─ ERR_DATA_005 → Toast (error): "Re-authentication failed."
    │
    └─ Auth passes → advance to Step 3
         │ (analytics: data_deletion_reauth_passed)
         ▼
    Step 3: Final Confirmation
    │
    ┌─────────────────────────────────────────────────┐
    │ Delete Your Account                       ✕     │
    │                                                 │
    │ Step 3 of 3                                     │
    │                                                 │
    │ Your account will be scheduled for deletion.    │
    │                                                 │
    │ • Deletion executes in 72 hours                 │
    │ • You can cancel anytime within 72 hours        │
    │ • You'll receive an email confirmation          │
    │ • After 72 hours, deletion is irreversible      │
    │                                                 │
    │                 [← Back]  [Confirm Deletion]    │
    └─────────────────────────────────────────────────┘
    │
    ├─ Back → return to Step 2 (auth preserved)
    │
    └─ Confirm →
         │
         ▼
    POST /api/data/deletion
    { confirmation_phrase, password, totp_code }
         │
         ├─ Success →
         │    ├─ Close modal
         │    ├─ Toast (warning): "Account deletion scheduled."
         │    ├─ Privacy center shows PENDING deletion banner
         │    ├─ Countdown to deletion visible
         │    │   (analytics: data_deletion_confirmed, { scheduled_at })
         │    └─ Email notification sent by backend
         │
         ├─ ERR_DATA_003 →
         │    Toast (error): "A deletion request is already pending."
         │    Close modal
         │
         ├─ ERR_DATA_004 →
         │    Return to Step 1, show inline error on phrase field
         │
         ├─ ERR_DATA_005 →
         │    Return to Step 2, show inline error
         │
         └─ Error →
              Toast (error): "Failed to submit deletion request."
              Keep modal open at current step
```

---

## Flow 7: Cancel Data Deletion

```
[Tenant Owner] → click [Cancel Deletion] on privacy center
    │ (analytics: data_deletion_cancel_clicked)
    ▼
Show confirmation dialog:
    "Cancel your account deletion request?"
    "Your account will remain active and no data will be deleted."
    [Keep Deletion]  [Cancel Deletion]
    │
    ├─ "Keep Deletion" → close dialog, deletion stays pending
    │
    └─ "Cancel Deletion" →
         │
         ▼
    POST /api/data/deletion/cancel
         │
         ├─ Success →
         │    ├─ Close dialog
         │    ├─ Toast (success): "Deletion request cancelled. Your account is safe."
         │    ├─ Remove pending deletion banner
         │    ├─ Restore [Delete My Account] button
         │    │   (analytics: data_deletion_cancelled)
         │    └─ Email notification sent by backend
         │
         ├─ ERR_DATA_007 →
         │    Toast (error): "Cannot cancel. Deletion is already processing."
         │    Refresh deletion status
         │
         └─ Error →
              Toast (error): "Failed to cancel deletion." + [Retry]
```

---

## Flow 8: Deletion Pending — Countdown Display

```
[Tenant Owner visits /privacy with pending deletion]
    │
    ▼
GET /api/data/deletion/status returns PENDING
    │
    ▼
Show warning banner:
    ┌──────────────────────────────────────────────────┐
    │ ⚠ Account deletion scheduled for {date} at      │
    │ {time} UTC. You have {hours}h {min}m to cancel.  │
    │                                [Cancel Deletion] │
    └──────────────────────────────────────────────────┘
    │
    ├─ Countdown updates every minute (client-side)
    ├─ When < 1 hour remaining → banner turns red
    ├─ Export section disabled (cannot export during pending deletion)
    └─ [Delete My Account] button replaced by countdown
```

---

## Flow 9: Tenant Audit Log Load

```
[Tenant Owner or Member] → navigate to /privacy/audit-log
    │ (analytics: audit_log_viewed, { role })
    ▼
Initialize default filters:
    ├─ Date range: last 7 days
    ├─ Category: All
    ├─ Search: empty
    ├─ Page: 1, per_page: 50
    │
    ▼
GET /api/audit-logs?date_from=&date_to=&category=&q=&page=1&per_page=50
    │
    ├─ Loading → skeleton table rows
    │
    ├─ Success → render audit log table
    │    │
    │    ├─ Filter bar:
    │    │   ├─ Date range picker (from / to)
    │    │   ├─ Category dropdown (All, Authentication, Scanning, Billing, etc.)
    │    │   ├─ Search input (searches action name + details)
    │    │   └─ [Apply Filters] button (or auto-apply on change)
    │    │
    │    ├─ Log table:
    │    │   ├─ Columns: Timestamp, Action, User, Resource, IP Address
    │    │   ├─ Each row expandable → shows full details_json
    │    │   └─ Impersonation badge if impersonated_by is set
    │    │
    │    ├─ Pagination: [Previous] Page {n} of {total} [Next]
    │    │
    │    └─ Empty state: "No audit log entries match your filters."
    │
    └─ Error → error state with [Retry]
         (analytics: audit_log_load_error)
```

---

## Flow 10: Audit Log — Apply Filters

```
[User] → changes filter value (date, category, or search)
    │ (analytics: audit_log_filtered, { filter_type, value })
    ▼
Debounce search input (300ms), immediate for date/category
    │
    ▼
Reset pagination to page 1
    │
    ▼
GET /api/audit-logs?{updated_filters}&page=1&per_page=50
    │
    ├─ Loading → show loading indicator on table (keep filters visible)
    │
    ├─ Success → update table rows
    │    └─ Update result count: "Showing {count} entries"
    │
    └─ Error → Toast (error): "Failed to load audit logs." Keep previous results.
```

---

## Flow 11: Audit Log — Expand Entry Detail

```
[User] → click on audit log row
    │ (analytics: audit_log_entry_expanded, { action })
    ▼
Expand row to show detail panel:
    ┌────────────────────────────────────────────────────────┐
    │ Action: scan.completed                                 │
    │ Timestamp: 2026-03-10 14:32:05 UTC                     │
    │ User: john@acme.com                                    │
    │ IP Address: 192.168.1.100                              │
    │ User Agent: Mozilla/5.0 ...                            │
    │ Resource: scan_job / job_abc123                         │
    │                                                        │
    │ Details:                                               │
    │ {                                                      │
    │   "scan_type": "full",                                 │
    │   "domains_scanned": 5,                                │
    │   "duration_seconds": 272,                             │
    │   "credits_consumed": 15                               │
    │ }                                                      │
    │                                                        │
    │ [Copy JSON]                                            │
    └────────────────────────────────────────────────────────┘
    │
    ├─ One row expanded at a time (click another → collapse this)
    ├─ [Copy JSON] copies details_json to clipboard
    └─ If impersonated_by set → show "Action performed during impersonation by {admin_email}"
```

---

## Flow 12: Audit Log — Pagination

```
[User] → click [Next] or [Previous]
    │ (analytics: audit_log_paginated, { page, direction })
    ▼
GET /api/audit-logs?{current_filters}&page={new_page}&per_page=50
    │
    ├─ Loading → spinner on table body
    │
    ├─ Success → update table rows, update page indicator
    │    └─ Scroll to top of table
    │
    └─ Error → Toast (error). Keep current page data.
```

---

## Flow 13: Admin Cross-Tenant Audit Log

```
[Super Admin] → navigate to /admin/audit-logs
    │ (analytics: admin_audit_log_viewed)
    ▼
Initialize default filters:
    ├─ Date range: last 7 days
    ├─ Tenant: All tenants
    ├─ Category: All
    ├─ User: All
    ├─ Super admin only: Off
    ├─ Search: empty
    │
    ▼
GET /api/admin/audit-logs?{filters}&page=1&per_page=50
    │
    ├─ Loading → skeleton table
    │
    ├─ Success → render cross-tenant audit log
    │    │
    │    ├─ Filter bar (extends tenant version):
    │    │   ├─ Tenant search/select (searchable dropdown)
    │    │   ├─ User search (email)
    │    │   ├─ Super admin actions toggle
    │    │   ├─ Date range picker
    │    │   ├─ Category dropdown
    │    │   └─ Search input
    │    │
    │    ├─ Log table (extends tenant version):
    │    │   ├─ Additional column: Tenant Name
    │    │   ├─ is_super_admin rows highlighted (subtle background)
    │    │   ├─ impersonated_by entries show badge: "Via impersonation"
    │    │   └─ Deleted tenant entries show: "[Deleted] {tenant_name}"
    │    │
    │    └─ Pagination
    │
    └─ Error → error state with [Retry]
```

---

## Flow 14: Admin Backup Status Load

```
[Super Admin] → navigate to /admin/data (backups tab)
    │ (analytics: admin_backups_viewed)
    ▼
Fetch in parallel:
    ├─ GET /api/admin/data/backups/summary
    └─ GET /api/admin/data/backups?page=1&per_page=50
         │
         ├─ Loading → skeleton cards + table
         │
         ├─ Success → render backup view
         │    │
         │    ├─ Summary cards:
         │    │   ├─ Total Tenants Backed Up (last 24h)
         │    │   ├─ Failed Backups (count, red if > 0)
         │    │   ├─ Last Backup Run (timestamp)
         │    │   └─ Backup Storage Used
         │    │
         │    ├─ Backup table:
         │    │   ├─ Columns: Tenant, Last Backup, Status, Size, Actions
         │    │   ├─ Status badges: SUCCESS (green), FAILED (red), IN_PROGRESS (yellow), NEVER (gray)
         │    │   ├─ Failed rows highlighted with error message
         │    │   └─ Per-row action: [Retry Backup]
         │    │
         │    └─ Pagination
         │
         └─ Error → error state with [Retry]
```

---

## Flow 15: Admin Trigger Manual Backup

```
[Super Admin] → click [Retry Backup] on failed tenant row
    │ (analytics: admin_backup_retry, { tenant_id })
    ▼
Show confirmation:
    "Trigger manual backup for {tenant_name}?"
    [Cancel] [Trigger Backup]
    │
    ├─ Cancel → close
    │
    └─ Confirm →
         │
         ▼
    POST /api/admin/data/backups/{tenant_id}/trigger
         │
         ├─ Success →
         │    ├─ Toast (success): "Backup triggered for {tenant_name}."
         │    ├─ Row status updates to IN_PROGRESS
         │    │   (analytics: admin_backup_triggered, { tenant_id })
         │    └─ Refresh table to show updated status
         │
         └─ Error →
              Toast (error): "Failed to trigger backup."
```

---

## Flow 16: Admin Migration Status Load

```
[Super Admin] → click Migrations tab on /admin/data
    │ (analytics: admin_migrations_viewed)
    ▼
GET /api/admin/data/migrations?page=1&per_page=50
    │
    ├─ Loading → skeleton table
    │
    ├─ Success → render migration table
    │    │
    │    ├─ Columns: Migration ID, Type, Applied At, Tenants Applied, Status, Integrity
    │    │
    │    ├─ Type badges: BASE (blue), TENANT_SPECIFIC (purple)
    │    │
    │    ├─ Status badges:
    │    │   ├─ COMPLETED (green) — all tenants applied successfully
    │    │   ├─ PARTIAL (yellow) — some tenants skipped (conflicts)
    │    │   ├─ FAILED (red) — migration failed for one or more tenants
    │    │   └─ NEEDS_REVIEW (orange) — integrity check flagged issues
    │    │
    │    ├─ Integrity column:
    │    │   ├─ ✓ Passed (green)
    │    │   ├─ ⚠ Issues (orange) — click to see details
    │    │   └─ — Not checked (gray)
    │    │
    │    ├─ Expandable row → migration details:
    │    │   ├─ Description
    │    │   ├─ Tables affected
    │    │   ├─ Per-tenant status list (if PARTIAL or FAILED)
    │    │   └─ Conflict details (if any)
    │    │
    │    └─ Per-row action: [Run Integrity Check] (if not already checked)
    │
    └─ Error → error state with [Retry]
```

---

## Flow 17: Admin Run Integrity Check

```
[Super Admin] → click [Run Integrity Check] on migration row
    │ (analytics: admin_integrity_check_triggered, { migration_id })
    ▼
Show confirmation:
    "Run data integrity check for migration {id}?"
    "This verifies foreign keys, indexes, and constraints for all applied tenants."
    [Cancel] [Run Check]
    │
    ├─ Cancel → close
    │
    └─ Confirm →
         │
         ▼
    POST /api/admin/data/migrations/{id}/integrity-check
         │
         ├─ Success →
         │    ├─ Toast (success): "Integrity check started."
         │    ├─ Integrity column updates to "Running..."
         │    │   (analytics: admin_integrity_check_started, { migration_id })
         │    └─ Poll or refresh to see result
         │
         └─ Error →
              Toast (error): "Failed to start integrity check."
```

---

## Flow 18: Admin View Migration Conflicts

```
[Super Admin] → expand PARTIAL migration row
    │ (analytics: admin_migration_conflicts_viewed, { migration_id })
    ▼
Show conflict details:
    ┌──────────────────────────────────────────────────────┐
    │ Migration: 2026_03_10_add_column_xyz                 │
    │ Status: PARTIAL — 3 tenants skipped                  │
    │                                                      │
    │ Conflicts:                                           │
    │ ┌────────┬──────────────────┬──────────┬───────────┐ │
    │ │ Tenant │ Conflicting Migr │ Table    │ Column    │ │
    │ ├────────┼──────────────────┼──────────┼───────────┤ │
    │ │ Acme   │ tm_001_custom    │ domains  │ metadata  │ │
    │ │ Beta   │ tm_003_addon     │ domains  │ metadata  │ │
    │ │ Gamma  │ tm_002_extend    │ scans    │ options   │ │
    │ └────────┴──────────────────┴──────────┴───────────┘ │
    │                                                      │
    │ These tenants require manual resolution by the       │
    │ infrastructure team.                                 │
    └──────────────────────────────────────────────────────┘
```

---

## Flow 19: Privacy Center — Suspended Tenant

```
[Tenant Owner of suspended tenant] → navigate to /privacy
    │
    ▼
Page loads with restrictions:
    ├─ Data Inventory → visible (read-only, no change)
    ├─ Export Data → disabled
    │   "Data export is unavailable while your account is suspended."
    ├─ Delete Account → disabled
    │   "Account deletion is unavailable while your account is suspended."
    └─ Audit Log link → accessible (read-only)
         (analytics: privacy_center_viewed_suspended)
```

---

## Flow 20: Privacy Center — Tenant Member (Limited Access)

```
[Tenant Member] → navigate to /privacy
    │ (analytics: privacy_center_viewed, { role: 'MEMBER' })
    ▼
Page loads with role restrictions:
    ├─ Data Inventory → visible (read-only)
    ├─ Export Data → hidden (TENANT_OWNER only)
    ├─ Delete Account → hidden (TENANT_OWNER only)
    └─ Audit Log link → visible (can view logs, read-only)
```

---

## Flow 21: Admin Audit Log — Export to CSV

```
[Super Admin] → click [Export CSV] on admin audit log
    │ (analytics: admin_audit_log_exported, { filters })
    ▼
Apply current filters to export request
    │
    ▼
POST /api/admin/audit-logs/export
{ date_from, date_to, tenant_id, category, q }
    │
    ├─ Success → browser downloads CSV file
    │   Toast (success): "Audit log exported ({count} entries)."
    │
    ├─ Too many entries (>100k) →
    │   Toast (warning): "Export limited to 100,000 entries. Apply filters to narrow results."
    │   Download available (capped)
    │
    └─ Error →
         Toast (error): "Failed to export audit logs."
```

---

## Flow 22: Admin Data Page — Tab Navigation

```
[Super Admin] → click tab on /admin/data
    │ (analytics: admin_data_tab_changed, { tab })
    ▼
Switch tab content:
    ├─ "Backups" → load backup summary + table (Flow 14)
    └─ "Migrations" → load migration table (Flow 16)
    │
    ▼
Previous tab data kept cached
New tab data fetched if not already loaded
```
