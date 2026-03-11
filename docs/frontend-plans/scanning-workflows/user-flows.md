# User Flows (Scanning & Workflows)

Scope: User journey flowcharts for domain management, scan operations, workflow management, scan scheduling, and results viewing.

---

## Preconditions

| Flow Group | Auth State | Role | Data Requirements |
|-----------|------------|------|-------------------|
| Domain management | Authenticated | Tenant Owner | Active tenant, Starter+ plan |
| Scan operations | Authenticated | Tenant Owner | Active tenant, ≥1 domain, Starter+ plan |
| View results | Authenticated | Any tenant member | Active tenant |
| Custom workflows | Authenticated | Tenant Owner | Pro+ plan |
| Scan schedules | Authenticated | Tenant Owner | Pro+ plan |
| Admin scan config | Authenticated | Super Admin | Enterprise tenant exists |

---

## Flow 1: Add Domain

**Entry points:**
- Clicks [Add Domain] on domain list page
- Clicks [Add Domain] on empty state

```
[Domain List Page] → clicks [Add Domain]
    │ (analytics: domain_add_initiated)
    ▼
[Add Domain Modal]
    │ Input: [domain.com]
    │ Hint: "Enter a bare domain (e.g., example.com)"
    │ Domain count: "3 / 20 domains used"
    │ CTA: [Add Domain]  [Cancel]
    │
    ├─ User types domain → client-side validation on blur:
    │   ├─ Contains protocol (http://) → inline error: "Enter domain without protocol"
    │   ├─ Contains path (/page) → inline error: "Enter domain without path"
    │   ├─ Is subdomain (api.example.com) → inline error: "Enter root domain only (e.g., example.com)"
    │   ├─ Is IP address → inline error: "IP addresses are not supported"
    │   ├─ Invalid characters → inline error: "Invalid domain format"
    │   └─ Valid → clear error, enable [Add Domain]
    │
    └─ Clicks [Add Domain]
        │ (analytics: domain_add_submitted, {domain})
        ▼
    API: POST /api/domains {domain}
        │
        ├─ Success (201)
        │   ▼
        │ Toast: "Domain added: {domain}"
        │ Domain list refreshes, new domain appears
        │ Modal closes
        │ (analytics: domain_added, {domain_id})
        │
        ├─ ERR_SCAN_001 (invalid format)
        │   └─► Inline error on domain field
        │
        ├─ ERR_SCAN_002 (subdomain)
        │   └─► Inline error: "Enter root domain only"
        │
        ├─ ERR_SCAN_003 (duplicate)
        │   └─► Inline error: "This domain already exists"
        │
        └─ ERR_SCAN_004 (limit reached)
            └─► Inline error: "Domain limit reached ({N}/{max})"
                Show [Upgrade Plan] link
```

---

## Flow 2: Delete Domain

```
[Domain List] → clicks [Delete] on a domain
    │ (analytics: domain_delete_initiated, {domain_id})
    ▼
[Delete Domain Confirmation Modal]
    │ "Delete {domain.com}?"
    │ "This will permanently remove:"
    │   • All subdomains, ports, technologies, screenshots
    │   • All scan history for this domain
    │   • Associated scan schedules will be disabled
    │ "Active scans must be cancelled first."
    │ CTA: [Cancel]  [Delete Domain] (destructive/red)
    │
    ├─ Clicks [Cancel] → modal closes
    │
    └─ Clicks [Delete Domain]
        │ (analytics: domain_delete_confirmed, {domain_id})
        ▼
    API: DELETE /api/domains/{id}
        │
        ├─ Success (200)
        │   ▼
        │ Toast: "Domain deleted: {domain}"
        │ Domain removed from list
        │ (analytics: domain_deleted, {domain_id})
        │
        ├─ ERR_SCAN_005 (not found)
        │   └─► Toast (error): "Domain not found." Refresh list.
        │
        └─ ERR_SCAN_006 (active scans)
            └─► Toast (error): "Cannot delete — active scans exist. Cancel them first."
                Highlight active scans link
```

---

## Flow 3: Create Scan (New Scan)

**Entry points:**
- Clicks [New Scan] on scan list
- Clicks [Start Scan] on domain details page (domain pre-selected)

```
[User] → clicks [New Scan]
    │ (analytics: scan_creation_started)
    ▼
[New Scan Page]
    │
    │ Step 1: Select Domain
    │ ┌─────────────────────────────────┐
    │ │ [Select Domain ▼]              │
    │ │ Dropdown lists tenant's domains │
    │ └─────────────────────────────────┘
    │ IF arrived from domain details → domain pre-selected
    │ IF no domains → "No domains added yet" + [Add Domain]
    │
    │ Step 2: Select Workflow
    │ ┌─────────────────────────────────────────┐
    │ │ System Templates:                        │
    │ │  ○ Quick Recon (3 steps)                 │
    │ │  ● Full Scan (5 steps)                   │
    │ │  ○ Web App Scan (4 steps)                │
    │ │  ○ Compliance Check (4 steps)            │
    │ │  ○ Continuous Monitor (2 steps)           │
    │ │                                          │
    │ │ Custom Workflows: (Pro+ only)            │
    │ │  ○ My Custom Workflow (3 steps)           │
    │ └─────────────────────────────────────────┘
    │
    │ Step 3: Review & Confirm
    │ ┌─────────────────────────────────────────┐
    │ │ Domain: example.com                      │
    │ │ Workflow: Full Scan                      │
    │ │                                          │
    │ │ Steps:                                   │
    │ │  1. subdomain_enum     1 credit          │
    │ │  2. port_scan          2 credits         │
    │ │  3. tech_detect        1 credit          │
    │ │  4. screenshot         1 credit          │
    │ │  5. vuln_scan          3 credits         │
    │ │  ──────────────────────────────          │
    │ │  Total: 8 credits                        │
    │ │  Available: 390 credits                  │
    │ │                                          │
    │ │  ⚠ 2 steps unavailable on your plan:     │
    │ │    vuln_scan, compliance_check            │
    │ │    [Upgrade to Pro]                       │
    │ │  (shown if steps filtered by feature flag)│
    │ └─────────────────────────────────────────┘
    │
    │ CTA: [Start Scan — 8 credits]  [Cancel]
    │
    ├─ Domain or workflow not selected → [Start Scan] disabled
    │
    └─ Clicks [Start Scan]
        │ (analytics: scan_submitted, {domain_id, workflow_id, estimated_credits})
        ▼
    API: POST /api/scans {domain_id, workflow_id}
        │
        ├─ Success (201)
        │   ▼
        │ Redirect to /scans/{id} (scan details)
        │ Toast: "Scan started on {domain}"
        │ (analytics: scan_created, {scan_id, domain_id, workflow_id, credits_deducted})
        │
        ├─ ERR_SCAN_013 (active scan on domain)
        │   └─► Toast (error): "A scan is already running on this domain."
        │       Show link to active scan
        │
        ├─ ERR_SCAN_014 (concurrent limit)
        │   └─► Toast (error): "Concurrent scan limit reached. Wait for a scan to complete or cancel one."
        │       Show link to scan list
        │
        ├─ ERR_SCAN_015 (no steps available)
        │   └─► Toast (error): "No scan steps available on your plan."
        │       Show [Upgrade] link
        │
        ├─ ERR_BILL_007 (insufficient credits)
        │   └─► Show <InsufficientCreditsModal /> (from billing plan)
        │
        ├─ ERR_SCAN_005 (domain not found)
        │   └─► Toast (error): "Domain not found." Redirect to domain list.
        │
        └─ ERR_SCAN_012 (workflow not found)
            └─► Toast (error): "Workflow not found." Refresh workflow list.
```

---

## Flow 4: View Scan Progress (Running Scan)

```
[Scan Details Page] (/scans/{id}) — scan status: RUNNING
    │
    ▼
[Step Progress Pipeline] (polls every 10 seconds)
    │
    │ ┌──────────┐   ┌──────────┐   ┌──────────┐   ┌──────────┐   ┌──────────┐
    │ │subdomain │──►│port_scan │──►│tech_det  │──►│screenshot│──►│vuln_scan │
    │ │  ✓ Done  │   │ ● Run... │   │ ○ Pending│   │ ○ Pending│   │ ○ Pending│
    │ └──────────┘   └──────────┘   └──────────┘   └──────────┘   └──────────┘
    │
    │ Current: port_scan (step 2 of 5)
    │ Started: 2 min ago
    │ [Cancel Scan]
    │
    │ Polling behavior:
    │   GET /api/scans/{id} every 10 seconds while status is QUEUED or RUNNING
    │   Stop polling when terminal state reached
    │
    ├─ Step completes → pipeline updates, next step starts
    │
    ├─ Step retrying:
    │   Pipeline shows: "⟳ Retrying (attempt 2/3)"
    │   (analytics: scan_step_retry_observed, {scan_id, step, attempt})
    │
    ├─ Step fails after retries:
    │   Pipeline shows: "✗ Failed" on that step
    │   Dependent steps show: "— Skipped"
    │   Independent steps continue
    │
    ├─ All steps complete → status changes:
    │   ├─ COMPLETED → green badge, all steps ✓
    │   ├─ PARTIAL → yellow badge, mix of ✓ and ✗
    │   └─ FAILED → red badge, all steps ✗
    │   Polling stops. Results tabs become active.
    │   (analytics: scan_completed_observed, {scan_id, status, duration})
    │
    └─ Scan cancelled/timed out → CANCELLED badge
        Pipeline shows completed steps + "Cancelled" on remaining
        Polling stops.
```

---

## Flow 5: Cancel Scan

**Preconditions:** Scan status is QUEUED or RUNNING

```
[Scan Details] → clicks [Cancel Scan]
    │ (analytics: scan_cancel_initiated, {scan_id, current_status})
    ▼
[Cancel Confirmation Modal]
    │ IF QUEUED:
    │   "Cancel this scan?"
    │   "No steps have started. All {X} credits will be refunded."
    │
    │ IF RUNNING:
    │   "Cancel this scan?"
    │   "The current step ({step_name}) will complete."
    │   "Remaining steps will be cancelled and credits refunded."
    │   "Completed results will be preserved."
    │
    │ CTA: [Keep Scanning]  [Cancel Scan]
    │
    ├─ Clicks [Keep Scanning] → modal closes
    │
    └─ Clicks [Cancel Scan]
        │ (analytics: scan_cancelled, {scan_id, status_before})
        ▼
    API: DELETE /api/scans/{id}
        │
        ├─ Success (200)
        │   ▼
        │ Toast: "Scan cancelled. Credits refunded."
        │ Scan status → CANCELLED
        │ Pipeline updates to show cancelled steps
        │
        └─ ERR_SCAN_016 (cannot cancel in current state)
            └─► Toast (error): "This scan can no longer be cancelled."
                Refresh scan details (may have completed)
```

---

## Flow 6: View Scan Results

**Preconditions:** Scan status is COMPLETED, PARTIAL, or FAILED (may have some results)

```
[Scan Details Page] → scan in terminal state
    │ (analytics: scan_results_viewed, {scan_id, status})
    ▼
[Results Section — Tabbed by Check Type]
    │
    │ Tabs shown only for steps that produced results:
    │ [Subdomains (42)] [Ports (156)] [Technologies (28)] [Vulns (7)] [Screenshots (12)]
    │
    ├─ Subdomains Tab (default if present):
    │   Table: Subdomain | Source | First Seen | Last Seen
    │   Sortable by each column
    │   Search/filter within results
    │   (analytics: results_tab_viewed, {scan_id, tab: "subdomains"})
    │
    ├─ Ports Tab:
    │   Table: Subdomain | Port | Protocol | Service | Banner
    │   Group by subdomain option
    │   (analytics: results_tab_viewed, {scan_id, tab: "ports"})
    │
    ├─ Technologies Tab:
    │   Table: Subdomain | Technology | Version | Category
    │   Filter by category
    │   (analytics: results_tab_viewed, {scan_id, tab: "technologies"})
    │
    ├─ Vulnerabilities Tab:
    │   Table: Subdomain | CVE | Severity | Description | Remediation
    │   Severity badges: CRITICAL (red), HIGH (orange), MEDIUM (yellow), LOW (blue), INFO (grey)
    │   Sort by severity (default: highest first)
    │   Filter by severity
    │   (analytics: results_tab_viewed, {scan_id, tab: "vulnerabilities"})
    │
    └─ Screenshots Tab:
        Grid of screenshot thumbnails
        Click → full-size lightbox
        Shows URL under each thumbnail
        (analytics: results_tab_viewed, {scan_id, tab: "screenshots"})

[PARTIAL scan results]
    │ Banner: "This scan completed partially. Some steps failed."
    │ Failed steps listed with reasons
    │ Only tabs for completed steps are shown

[FAILED scan results]
    │ Banner: "This scan failed. No results were produced."
    │ Show failure reason
    │ No result tabs (or empty)
```

---

## Flow 7: View Domain Details

```
[Domain List] → clicks on domain name
    │ (analytics: domain_details_viewed, {domain_id})
    ▼
[Domain Details Page] (/domains/{id})
    │
    │ Header: example.com
    │ Status: ACTIVE
    │ Added: Mar 1, 2026 by user@tenant.com
    │ [Start Scan]  [Delete Domain]
    │
    ├── Overview Tab (default):
    │   │
    │   │ Subdomains ({count})
    │   │ Table: Subdomain | Source | First Seen | Last Seen
    │   │ (aggregated from all scans, latest data)
    │   │
    │   │ Open Ports ({count})
    │   │ Table: Subdomain | Port | Protocol | Service
    │   │
    │   │ Technologies ({count})
    │   │ Table: Subdomain | Technology | Version | Category
    │   │
    │   ├─ Empty state (no scans run yet):
    │   │   "No scan data yet. Run your first scan."
    │   │   [Start Scan] CTA
    │   │
    │   └─ Loading → skeleton tables
    │
    └── Scan History Tab:
        │
        │ Table: Scan ID | Workflow | Status | Started | Duration | Credits
        │ Sorted by date descending
        │ Status badges: COMPLETED (green), PARTIAL (yellow), FAILED (red), CANCELLED (grey)
        │ Click row → /scans/{id}
        │
        ├─ Empty state: "No scans run against this domain yet."
        │
        └─ (analytics: domain_scan_history_viewed, {domain_id})
```

---

## Flow 8: Workflow List & Custom Workflow Creation

```
[Scans nav] → clicks [Workflows]
    │ (analytics: workflows_viewed)
    ▼
[Workflows Page]
    │
    │ System Templates:
    │ ┌─ Quick Recon ─────────────────────────┐
    │ │ subdomain_enum → tech_detect → screenshot │
    │ │ Fast attack surface overview            │
    │ │ [Use Template]                          │
    │ └─────────────────────────────────────────┘
    │ ... (5 system templates displayed as cards)
    │
    │ Custom Workflows (Pro+ only):
    │ IF Starter tier:
    │   "Custom workflows require Pro or Enterprise." [Upgrade]
    │ IF Pro+:
    │   List of custom workflows + [Create Workflow]
    │   Each: Name | Steps | Created | [Edit] [Delete]
    │   Count: "3 / 20 custom workflows"
    │
    └─ Clicks [Create Workflow]
        │ (analytics: workflow_create_initiated)
        ▼
    [Custom Workflow Builder] (/scans/workflows/new)
        │
        │ Name: [My Custom Workflow]
        │
        │ Available Steps:
        │ ┌──────────────────────────────────┐
        │ │ ☑ subdomain_enum                  │
        │ │ ☑ port_scan                       │
        │ │ ☐ tech_detect                     │
        │ │ ☐ screenshot                      │
        │ │ ☑ vuln_scan                       │
        │ │ ☐ compliance_check                │
        │ │ 🔒 censys_lookup (Enterprise only)│
        │ └──────────────────────────────────┘
        │
        │ Selected Steps (drag to reorder):
        │ 1. subdomain_enum
        │ 2. port_scan
        │ 3. vuln_scan
        │
        │ Step count: 3 / 15 max
        │ CTA: [Create Workflow]  [Cancel]
        │
        ├─ No steps selected → [Create] disabled
        ├─ >15 steps → error: "Maximum 15 steps"
        │
        └─ Clicks [Create Workflow]
            │ (analytics: workflow_created, {name, step_count})
            ▼
        API: POST /api/workflows {name, steps_json}
            │
            ├─ Success (201)
            │   ▼
            │ Toast: "Workflow created: {name}"
            │ Redirect to workflows list
            │
            ├─ ERR_SCAN_007 (Pro+ required)
            │   └─► Toast (error): "Custom workflows require Pro or Enterprise."
            │
            ├─ ERR_SCAN_008 (no steps)
            │   └─► Inline error: "Add at least one step."
            │
            ├─ ERR_SCAN_009 (>15 steps)
            │   └─► Inline error: "Maximum 15 steps allowed."
            │
            ├─ ERR_SCAN_010 (unknown step type)
            │   └─► Inline error on the unknown step
            │
            └─ ERR_SCAN_011 (workflow limit)
                └─► Toast (error): "Custom workflow limit reached (20)."
```

### Delete Custom Workflow

```
[Workflows Page] → clicks [Delete] on custom workflow
    │ (analytics: workflow_delete_initiated, {workflow_id})
    ▼
[Confirmation Modal]
    │ "Delete workflow '{name}'?"
    │ "Schedules using this workflow will be disabled."
    │ CTA: [Cancel]  [Delete Workflow] (destructive)
    │
    └─ Clicks [Delete Workflow]
        │ (analytics: workflow_deleted, {workflow_id})
        ▼
    API: DELETE /api/workflows/{id}
        │
        ├─ Success → Toast: "Workflow deleted." List refreshes.
        │
        └─ Error (active scans reference it)
            └─► Toast (error): "Cannot delete — active scans use this workflow."
```

---

## Flow 9: Create Scan Schedule (Pro+)

```
[Scans nav] → clicks [Schedules]
    │ (analytics: schedules_viewed)
    ▼
[Schedules Page]
    │ IF Starter/Free tier:
    │   "Scheduled scans require Pro or Enterprise." [Upgrade]
    │   Return
    │
    │ Schedule list:
    │ Domain | Workflow | Cron | Next Run | Status | [Enable/Disable] [Delete]
    │ Count: "4 / 10 active schedules"
    │
    └─ Clicks [Create Schedule]
        │ (analytics: schedule_create_initiated)
        ▼
    [New Schedule Page] (/scans/schedules/new)
        │
        │ Domain: [Select Domain ▼]
        │ Workflow: [Select Workflow ▼]
        │
        │ Schedule:
        │ ┌─ Frequency Presets ──────────────────┐
        │ │  ○ Daily (at midnight)                │
        │ │  ○ Weekly (Monday at midnight)         │
        │ │  ○ Monthly (1st at midnight)           │
        │ │  ○ Custom cron expression              │
        │ └──────────────────────────────────────┘
        │
        │ IF Custom:
        │   Cron: [0 0 * * 1]
        │   Preview: "Every Monday at 00:00 UTC"
        │   Validation: min 24-hour interval
        │
        │ Credit Estimate:
        │   "Estimated cost per run: {X} credits"
        │   "Note: Credits checked at execution time, not reserved."
        │
        │ CTA: [Create Schedule]  [Cancel]
        │
        └─ Clicks [Create Schedule]
            │ (analytics: schedule_created, {domain_id, workflow_id, cron})
            ▼
        API: POST /api/scans/schedules {domain_id, workflow_id, cron_expression}
            │
            ├─ Success (201)
            │   ▼
            │ Toast: "Schedule created. Next run: {date}"
            │ Redirect to schedules list
            │
            ├─ ERR_SCAN_017 (Pro+ required)
            │   └─► Toast (error): "Scheduled scans require Pro or Enterprise."
            │
            ├─ ERR_SCAN_018 (invalid cron)
            │   └─► Inline error: "Invalid cron expression."
            │
            ├─ ERR_SCAN_019 (min interval)
            │   └─► Inline error: "Minimum interval is 24 hours."
            │
            └─ ERR_SCAN_020 (schedule limit)
                └─► Toast (error): "Schedule limit reached (10 active)."
```

### Enable/Disable Schedule

```
[Schedules Page] → clicks toggle on a schedule
    │
    ├─ Enabling:
    │   │ (analytics: schedule_enabled, {schedule_id})
    │   ▼
    │ API: POST /api/scans/schedules/{id}/enable
    │   ├─ Success → toggle on, toast: "Schedule enabled."
    │   └─ ERR_SCAN_020 (limit) → toast: "Active schedule limit reached."
    │
    └─ Disabling:
        │ (analytics: schedule_disabled, {schedule_id})
        ▼
    API: POST /api/scans/schedules/{id}/disable
        └─ Success → toggle off, toast: "Schedule disabled."
```

### Auto-Disabled Schedules

```
[Schedules Page] — schedules auto-disabled by system
    │
    │ Schedule row shows: "Disabled — {reason}"
    │ Reasons:
    │   • "Plan downgraded" (plan no longer supports scheduled scans)
    │   • "Domain deleted" (associated domain was removed)
    │   • "Subscription expired" (tenant lost subscription)
    │
    │ IF plan supports scheduling:
    │   [Re-enable] toggle available
    │ ELSE:
    │   Toggle disabled, tooltip: "Upgrade to Pro to re-enable"
```

---

## Flow 10: Scan List (All Scans)

```
[User navigates to /scans]
    │ (analytics: scan_list_viewed)
    ▼
[Scan List Page]
    │ Filter bar: [All Statuses ▼] [All Domains ▼] [Date Range ▼]
    │
    │ Table: Domain | Workflow | Status | Started | Duration | Credits | Actions
    │ Sorted by date descending
    │ Pagination: 25 per page
    │
    │ Status badges with colors:
    │   QUEUED (blue), RUNNING (blue pulse), COMPLETED (green),
    │   PARTIAL (yellow), FAILED (red), CANCELLED (grey)
    │
    │ Running scans appear at top with [Cancel] action
    │
    ├─ Click scan row → /scans/{id}
    │
    ├─ Click [Cancel] on running scan → cancel modal (Flow 5)
    │
    ├─ Empty state: "No scans yet. Start your first scan."
    │   [New Scan] CTA
    │
    └─ Loading → skeleton table rows
```

---

## Flow 11: Domain List

```
[User navigates to /domains]
    │ (analytics: domain_list_viewed)
    ▼
[Domain List Page]
    │ Header: "Domains" + domain count badge "{used}/{max}"
    │ [Add Domain] button
    │
    │ Table: Domain | Status | Added | Last Scanned | Actions
    │ Actions: [View] [Start Scan] [Delete]
    │
    │ Click domain name → /domains/{id}
    │ Click [Start Scan] → /scans/new?domain={id}
    │ Click [Delete] → delete confirmation (Flow 2)
    │
    ├─ IF Free tier:
    │   Banner: "Upgrade to add domains and start scanning." [Upgrade]
    │   [Add Domain] disabled
    │
    ├─ IF at domain limit:
    │   [Add Domain] disabled
    │   Tooltip: "Domain limit reached ({N}/{max}). Upgrade for more."
    │
    ├─ Empty state (no domains, has plan):
    │   "No domains added yet. Add your first domain to start scanning."
    │   [Add Domain] CTA
    │
    └─ Loading → skeleton table rows
```

---

## Flow 12: Super Admin — Concurrent Scan Limits

**Preconditions:** Super Admin role

```
[Admin Panel] → navigates to Scans > Concurrent Limits
    │ (analytics: admin_scan_limits_viewed)
    ▼
[Concurrent Limits Page]
    │ Info: "Override concurrent scan limits for Enterprise tenants."
    │ Default limits: Starter: 1, Pro: 3, Enterprise: 10
    │
    │ ┌─── Enterprise Tenant Overrides ───────────────────────┐
    │ │                                                        │
    │ │ Search: [                                    🔍]      │
    │ │                                                        │
    │ │ Tenant           Current Limit    [Override]           │
    │ │ ──────────────────────────────────────────────         │
    │ │ Acme Corp        10 (default)     [Set Custom]        │
    │ │ MegaCo           25 (custom)      [Edit] [Reset]      │
    │ │                                                        │
    │ └────────────────────────────────────────────────────────┘
    │
    └─ Clicks [Set Custom] or [Edit]
        ▼
    [Inline Edit / Modal]
        │ Concurrent limit: [25]
        │ Min: 1, Max: 100
        │ CTA: [Save]  [Cancel]
        │
        └─ Clicks [Save]
            │ (analytics: admin_scan_limit_updated, {tenant_id, new_limit})
            ▼
        API: PUT /api/admin/tenants/{id}/scan-limit {max_concurrent_scans}
            │
            ├─ Success → toast: "Concurrent limit set to {N} for {tenant}."
            │
            └─ Error → toast with error message
```
