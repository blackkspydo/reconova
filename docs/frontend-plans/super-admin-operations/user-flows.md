# User Flows (Super Admin & Operations)

Scope: User journey flowcharts for admin dashboard, monitoring, alert management, maintenance mode, and impersonation indicator.

---

## Preconditions

| Condition | Requirement |
|-----------|-------------|
| Authentication | User must be logged in with valid session |
| Role | `SUPER_ADMIN` role required for all flows |
| 2FA | Must be enrolled. First-login redirect to 2FA setup if not enrolled (BR-ADM-001) |
| Admin layout | All flows occur within the `/(admin)/` layout shell |

---

## Flow 1: Admin Dashboard Load

```
[Super Admin] → navigate to /admin
    │ (analytics: admin_dashboard_viewed)
    ▼
Route guard checks role
    ├─ NOT SUPER_ADMIN → redirect to /dashboard (tenant context)
    └─ SUPER_ADMIN → continue
         │
         ▼
    Fetch dashboard summary data
    GET /api/admin/dashboard/summary
         │
         ├─ Loading → show skeleton cards (4 summary cards + quick links)
         │
         ├─ Success → render dashboard
         │    │
         │    ├─ Summary cards:
         │    │   ├─ Total Active Tenants (count + trend arrow)
         │    │   ├─ Active Scans (count + queue depth)
         │    │   ├─ Credits Consumed (30d total + trend)
         │    │   └─ System Health (status dot: green/yellow/red)
         │    │
         │    ├─ Check maintenance mode status
         │    │   ├─ ACTIVE → show maintenance badge in header (orange)
         │    │   └─ INACTIVE → show toggle button in header
         │    │
         │    ├─ Check active alerts count
         │    │   ├─ > 0 → show notification badge with count in header
         │    │   └─ 0 → no badge
         │    │
         │    └─ Quick links grid → all admin sections
         │
         └─ Error → error state with [Retry] button
              (analytics: admin_dashboard_load_error, { error_code })
```

---

## Flow 2: Navigate to Monitoring Detail

```
[Super Admin] → click "View Monitoring" link on dashboard
    │         OR navigate to /admin/monitoring
    │ (analytics: admin_monitoring_viewed)
    ▼
Fetch all metric categories in parallel:
    ├─ GET /api/admin/monitoring/tenants
    ├─ GET /api/admin/monitoring/scans
    ├─ GET /api/admin/monitoring/workers
    ├─ GET /api/admin/monitoring/credits
    └─ GET /api/admin/monitoring/system-health
         │
         ├─ Loading → skeleton per section (independent loading)
         │
         ├─ Success → render metric sections
         │    │
         │    ├─ Each section renders independently as its data arrives
         │    ├─ Auto-refresh timers start per section:
         │    │   ├─ Scans + Workers: every 30 seconds
         │    │   ├─ System Health: every 1 minute
         │    │   ├─ Tenants: every 5 minutes
         │    │   └─ Credits: every 60 minutes
         │    │
         │    ├─ Metric cards show value + sparkline trend (where applicable)
         │    │
         │    └─ Alert-triggering metrics highlighted:
         │         ├─ CRITICAL threshold breached → red card border + pulse
         │         └─ WARNING threshold breached → yellow card border
         │
         └─ Partial error → failed section shows error + [Retry] for that section
              Other sections render normally
              (analytics: admin_monitoring_section_error, { section, error_code })
```

---

## Flow 3: Monitoring Auto-Refresh Cycle

```
[Monitoring page loaded] → auto-refresh timer fires
    │
    ▼
Check if page is visible (document.visibilityState)
    ├─ Hidden → skip refresh, wait for next interval
    └─ Visible → continue
         │
         ▼
    Fetch updated metrics for this section
         │
         ├─ Success → update metric values in place (no full re-render)
         │    │
         │    ├─ Compare new values to alert thresholds
         │    │   ├─ Newly breached → flash highlight + update alert badge count
         │    │   └─ Recovered → remove highlight
         │    │
         │    └─ Update "Last updated: {time}" indicator per section
         │
         └─ Error → show stale data indicator on section
              "Data may be stale. Last updated: {time}."
              Continue auto-refresh (next interval may succeed)
```

---

## Flow 4: Drill Into Metric Detail

```
[Super Admin] → click on a metric card
    │ (analytics: admin_metric_drilled, { metric_name, section })
    ▼
Expand metric card to show detail view
    │
    ├─ Tenant metrics drill-down:
    │   ├─ "Active Tenants" → breakdown by plan tier (bar chart)
    │   ├─ "New Tenants" → 7d vs 30d comparison
    │   └─ "Churn" → list of recently cancelled subscriptions
    │
    ├─ Scan metrics drill-down:
    │   ├─ "Active Scans" → list of running scan jobs (tenant, domain, progress)
    │   ├─ "Queue Depth" → queue items by priority
    │   └─ "Failure Rate" → recent failed scans with error categories
    │
    ├─ Worker metrics drill-down:
    │   ├─ "Active Workers" → worker list with current job, uptime
    │   └─ "Stale Workers" → stale worker list with last heartbeat time
    │
    ├─ Credit metrics drill-down:
    │   ├─ "Credits Consumed" → top 10 tenants by consumption
    │   └─ "Revenue" → MRR breakdown by plan tier
    │
    └─ System health drill-down:
        ├─ "API Latency" → p50/p95/p99 values with mini chart
        ├─ "DB Pool" → current/max connections
        └─ "API Key Pool" → per-provider key status summary
    │
    ▼
Click again or click another card → collapse detail
    (analytics: admin_metric_collapsed, { metric_name })
```

---

## Flow 5: Enable Maintenance Mode

```
[Super Admin] → click maintenance toggle in admin header
    │ (analytics: admin_maintenance_toggle_clicked)
    ▼
Current state = INACTIVE?
    ├─ No (already ACTIVE) → go to Flow 6 (Disable)
    └─ Yes → open Enable Maintenance Modal
         │
         ▼
    ┌─────────────────────────────────────┐
    │ Enable Maintenance Mode             │
    │                                     │
    │ Reason: [________________]          │
    │ (min 10 characters)                 │
    │                                     │
    │ Estimated Duration: [__] minutes    │
    │                                     │
    │ ⚠ Warning: New scan creation will   │
    │ be blocked. Running scans will      │
    │ complete normally.                  │
    │                                     │
    │ [Cancel]  [Enable Maintenance]      │
    └─────────────────────────────────────┘
         │
         ├─ Cancel → close modal, no change
         │
         └─ Submit
              │
              ▼
         Validate fields
              ├─ Reason < 10 chars → inline error: "Reason must be at least 10 characters."
              ├─ Duration empty or < 1 → inline error: "Estimated duration is required."
              └─ Valid → continue
                   │
                   ▼
              POST /api/admin/maintenance/enable
              { reason, estimated_duration_minutes }
                   │
                   ├─ Success →
                   │    ├─ Close modal
                   │    ├─ Toast (success): "Maintenance mode enabled."
                   │    ├─ Header toggle shows ACTIVE state (orange badge)
                   │    ├─ All tenant sessions receive maintenance banner
                   │    │   (analytics: admin_maintenance_enabled, { reason, duration })
                   │    └─ Dashboard summary card updates
                   │
                   └─ Error →
                        ├─ ERR_ADM_001 → Toast (error): "Super admin access required." Redirect.
                        └─ Network error → Toast (error) + [Retry]. Keep modal open.
```

---

## Flow 6: Disable Maintenance Mode

```
[Super Admin] → click maintenance toggle (currently ACTIVE)
    │ (analytics: admin_maintenance_disable_clicked)
    ▼
Show confirmation dialog:
    "Disable maintenance mode and resume normal operations?"
    [Cancel] [Disable Maintenance]
    │
    ├─ Cancel → close dialog
    └─ Confirm →
         │
         ▼
    POST /api/admin/maintenance/disable
         │
         ├─ Success →
         │    ├─ Close dialog
         │    ├─ Toast (success): "Maintenance mode disabled. Normal operations resumed."
         │    ├─ Header toggle shows INACTIVE state
         │    ├─ Tenant maintenance banners removed
         │    │   (analytics: admin_maintenance_disabled, { actual_duration_minutes })
         │    └─ Dashboard summary updates
         │
         └─ Error →
              Toast (error): "Failed to disable maintenance mode." + [Retry]
```

---

## Flow 7: View Active Alerts (Header Badge)

```
[Super Admin] → sees alert notification badge in admin header
    │ (badge shows count of active alerts)
    ▼
Click badge → dropdown opens
    │ (analytics: admin_alerts_dropdown_opened)
    ▼
Dropdown shows up to 5 most recent active alerts:
    ┌────────────────────────────────────────┐
    │ 🔴 Zero active workers          2m ago │
    │ 🟡 Queue depth at 85%          15m ago │
    │ 🟡 Worker stale: worker-03     22m ago │
    │                                        │
    │ [View All Alerts]                      │
    └────────────────────────────────────────┘
    │
    ├─ Click individual alert → navigate to /admin/monitoring
    │   with that metric section highlighted
    │   (analytics: admin_alert_clicked, { alert_type, level })
    │
    └─ Click "View All Alerts" → navigate to /admin/monitoring/alerts
         (analytics: admin_alerts_page_navigated)
```

---

## Flow 8: Alert Management Page Load

```
[Super Admin] → navigate to /admin/monitoring/alerts
    │ (analytics: admin_alerts_page_viewed)
    ▼
Fetch in parallel:
    ├─ GET /api/admin/monitoring/alerts/active
    └─ GET /api/admin/monitoring/alerts/rules
         │
         ├─ Loading → skeleton for both panels
         │
         ├─ Success → render two panels:
         │    │
         │    ├─ Active Alerts Panel (top)
         │    │   ├─ Currently firing alerts sorted by level (CRITICAL first)
         │    │   ├─ Each row: level badge, condition description, triggered at, duration
         │    │   ├─ Click row → navigate to /admin/monitoring with section highlighted
         │    │   └─ Empty state: "No active alerts. All systems healthy." ✓
         │    │
         │    └─ Alert Rules Table (bottom)
         │        ├─ All configurable alert rules
         │        ├─ Columns: Condition, Current Value, Threshold, Level, Enabled
         │        ├─ Each row has: edit threshold button, enable/disable toggle
         │        └─ No create/delete — rules are system-defined, only thresholds editable
         │
         └─ Error → error state with [Retry]
```

---

## Flow 9: Edit Alert Threshold

```
[Super Admin] → click edit icon on alert rule row
    │ (analytics: admin_alert_threshold_edit_started, { alert_type })
    ▼
Row enters inline edit mode:
    ├─ Threshold field becomes editable input
    ├─ [Save] [Cancel] buttons appear
    │
    ├─ Cancel → revert to original value, exit edit mode
    │
    └─ Save →
         │
         ▼
    Validate threshold
         ├─ Empty or non-numeric → inline error: "Threshold must be a number."
         ├─ Out of range → inline error: "Threshold must be between {min} and {max}."
         └─ Valid → continue
              │
              ▼
         PUT /api/admin/monitoring/alerts/rules/{id}
         { threshold, enabled }
              │
              ├─ Success →
              │    ├─ Exit edit mode
              │    ├─ Update row with new threshold
              │    ├─ Toast (success): "Alert threshold updated."
              │    │   (analytics: admin_alert_threshold_updated, { alert_type, old_threshold, new_threshold })
              │    └─ Re-evaluate active alerts against new threshold
              │
              └─ Error →
                   Toast (error): "Failed to update threshold." + keep edit mode open
```

---

## Flow 10: Toggle Alert Rule Enabled/Disabled

```
[Super Admin] → click enable/disable toggle on alert rule
    │ (analytics: admin_alert_rule_toggled, { alert_type, enabled })
    ▼
PUT /api/admin/monitoring/alerts/rules/{id}
{ enabled: !currentValue }
    │
    ├─ Success →
    │    ├─ Update toggle state
    │    ├─ If disabled → any active alert for this rule is dismissed
    │    ├─ Toast (success): "Alert rule {enabled ? 'enabled' : 'disabled'}."
    │    └─ Update header alert badge count
    │
    └─ Error →
         ├─ Revert toggle (optimistic rollback)
         └─ Toast (error): "Failed to update alert rule."
```

---

## Flow 11: Impersonation Indicator — Display

```
[Any page load] → check for active impersonation session
    │
    ▼
Read JWT claims for `impersonated_by` field
    ├─ NOT present → no indicator shown
    └─ Present → show floating impersonation indicator
         │
         ▼
    Floating pill (bottom-right corner):
    ┌───────────────────────────────┐
    │ 👤 Acme Corp  ⏱ 42:15        │
    └───────────────────────────────┘
         │
         ├─ Timer counts down from session TTL
         │   ├─ > 10 min remaining → normal styling
         │   ├─ ≤ 10 min remaining → yellow warning
         │   └─ ≤ 2 min remaining → red pulse
         │
         └─ Auto-refresh: timer updates every second (client-side countdown)
```

---

## Flow 12: Impersonation Indicator — Expand

```
[Super Admin] → click floating impersonation pill
    │ (analytics: admin_impersonation_indicator_expanded)
    ▼
Pill expands to detail panel:
    ┌────────────────────────────────────┐
    │ Impersonating: Acme Corp           │
    │ Tenant ID: tnt_abc123              │
    │ Time remaining: 42:15              │
    │ Reason: "Investigating scan fail"  │
    │                                    │
    │ ⚠ Restricted: Password changes,   │
    │   2FA, tenant deletion, billing    │
    │                                    │
    │ [End Session]                      │
    └────────────────────────────────────┘
    │
    ├─ Click outside → collapse back to pill
    │
    └─ Click [End Session] → Flow 13
```

---

## Flow 13: End Impersonation Session

```
[Super Admin] → click [End Session] in impersonation indicator
    │ (analytics: admin_impersonation_end_clicked)
    ▼
Show confirmation:
    "End impersonation session for Acme Corp?"
    [Cancel] [End Session]
    │
    ├─ Cancel → close confirmation, keep indicator
    │
    └─ Confirm →
         │
         ▼
    POST /api/admin/impersonation/end
         │
         ├─ Success →
         │    ├─ Remove floating indicator
         │    ├─ Clear impersonation JWT
         │    ├─ Redirect to /admin/tenants/{id} (the impersonated tenant)
         │    ├─ Toast (success): "Impersonation session ended."
         │    │   (analytics: admin_impersonation_ended, { tenant_id, duration_minutes })
         │    └─ Restore admin context (admin header, sidebar)
         │
         └─ Error →
              Toast (error): "Failed to end session. Try again."
              Keep indicator visible
```

---

## Flow 14: Impersonation Session Auto-Expiry

```
[Timer reaches 0:00]
    │ (analytics: admin_impersonation_expired, { tenant_id, session_duration })
    ▼
Show expiry notification:
    Toast (warning): "Impersonation session expired."
    │
    ▼
Remove floating indicator
    │
    ▼
Next API call returns 401 (expired token)
    │
    ▼
Auth interceptor detects impersonation expiry
    ├─ Clear impersonation state
    ├─ Redirect to /admin/tenants/{id}
    └─ Toast (info): "Session expired. Returned to admin context."
```

---

## Flow 15: Impersonation Restricted Action Attempt

```
[Super Admin in impersonation] → attempts restricted action
    │ (e.g., change password, modify 2FA, delete tenant, change billing)
    ▼
Frontend check: is action in restricted list?
    ├─ No → allow action to proceed normally
    └─ Yes → block action
         │
         ▼
    Action button/link is disabled with tooltip:
    "Not available during impersonation."
         │
         ├─ If user somehow submits (edge case) →
         │   API returns 403
         │   Toast (error): "This action is not allowed during impersonation."
         │   (analytics: admin_impersonation_restricted_action, { action, tenant_id })
         │
         └─ No navigation or modal opens for restricted actions
```

---

## Flow 16: Admin Dashboard — Quick Link Navigation

```
[Super Admin] → click quick link card on dashboard
    │ (analytics: admin_quicklink_clicked, { target })
    ▼
Navigate to target admin section:
    ├─ "Tenants" → /admin/tenants
    ├─ "Users" → /admin/users
    ├─ "Feature Flags" → /admin/features
    ├─ "Integrations" → /admin/integrations
    ├─ "Compliance" → /admin/compliance
    ├─ "Billing" → /admin/billing/credits
    ├─ "CVE Monitoring" → /admin/cve
    ├─ "Scan Config" → /admin/scans/limits
    └─ "Monitoring" → /admin/monitoring
```

---

## Flow 17: Admin First Login — 2FA Enrollment

```
[Super Admin] → login successful, no 2FA enrolled
    │ (analytics: admin_first_login_2fa_required)
    ▼
Redirect to /auth/2fa/setup (forced)
    │
    ├─ Cannot navigate to any /admin/* route until 2FA enrolled
    ├─ Cannot skip or defer enrollment
    │
    ▼
Standard 2FA enrollment flow (see authentication plan)
    │
    ├─ Success → redirect to /admin (dashboard)
    │   (analytics: admin_2fa_enrolled)
    │
    └─ Abandon → logout (cannot proceed without 2FA)
```

---

## Flow 18: Monitoring Page — Section Collapse/Expand

```
[Super Admin] → click section header on monitoring page
    │ (analytics: admin_monitoring_section_toggled, { section, expanded })
    ▼
Toggle section visibility
    ├─ Expanded → collapse (hide metric cards, keep header)
    └─ Collapsed → expand (show metric cards, resume auto-refresh)
         │
         ▼
    Section expansion state persisted in component state
    (not persisted across page loads — all sections default expanded)
```

---

## Flow 19: Dashboard Summary — System Health Click

```
[Super Admin] → click System Health summary card on dashboard
    │ (card shows aggregate status: green/yellow/red)
    │ (analytics: admin_system_health_clicked)
    ▼
Navigate to /admin/monitoring
    │
    ▼
Auto-scroll to System Health section
    Highlight section header briefly (flash)
```

---

## Flow 20: Alert Fires While on Monitoring Page

```
[Auto-refresh returns data with threshold breach]
    │
    ▼
Compare metric value to configured threshold
    ├─ Previously OK, now breached →
    │    ├─ Highlight metric card (yellow/red border based on level)
    │    ├─ Increment header alert badge count
    │    ├─ Show inline alert indicator on the card
    │    │   (analytics: admin_alert_fired, { alert_type, level, value, threshold })
    │    └─ If CRITICAL → browser notification (if permissions granted)
    │
    ├─ Previously breached, still breached → maintain highlight (no re-alert)
    │
    └─ Previously breached, now OK →
         ├─ Remove highlight from card
         ├─ Decrement header alert badge count
         └─ (analytics: admin_alert_resolved, { alert_type, value })
```

---

## Flow 21: Browser Notification Permission

```
[Super Admin] → first visit to /admin/monitoring
    │
    ▼
Check browser notification permission
    ├─ Already granted → no action
    ├─ Already denied → no action (respect user choice)
    └─ Not yet asked →
         Show inline prompt at top of monitoring page:
         "Enable browser notifications for critical alerts?"
         [Enable] [Not Now]
              │
              ├─ "Enable" → request Notification API permission
              │    ├─ Granted → Toast (success): "Critical alerts will trigger browser notifications."
              │    └─ Denied → no further prompts
              │         (analytics: admin_notifications_permission, { granted: true/false })
              │
              └─ "Not Now" → dismiss prompt, don't ask again this session
```
