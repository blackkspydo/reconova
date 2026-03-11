# User Flows (System Configuration)

Scope: Step-by-step user journeys for config viewing, editing, approval workflows, change history, rollback, and cache management.

---

## Preconditions

| Condition | Requirement |
|-----------|-------------|
| Authentication | User must be logged in with valid session |
| Role | User must have `SUPER_ADMIN` role |
| Route guard | `/admin/config/*` routes enforce role check — redirect to `/dashboard` if not super admin |

---

## Flow 1: View Config List

```
[Admin navigates to /admin/config]
    │ (page_view: admin_config_list)
    ▼
Load all configs grouped by category
    ├─ Success → Render grouped table
    │     ├─ 12 collapsible category sections
    │     ├─ All sections expanded by default
    │     ├─ Each row: key, description (tooltip), value, type, badges
    │     ├─ Cache status indicator in header
    │     └─ Header links: [History] [Approvals (N)]
    └─ Failure → Error state
          └─ "Failed to load system configuration." + [Retry]
```

---

## Flow 2: Search Configs

```
[Admin types in search bar]
    │ (config_search: { query })
    ▼
Filter configs client-side (300ms debounce)
    ├─ Match found → Show matching rows
    │     ├─ Search matches against key and description
    │     ├─ Auto-expand categories containing matches
    │     ├─ Auto-collapse categories with no matches
    │     └─ Highlight matched text
    └─ No match → Empty state
          └─ "No configs match '{query}'." + [Clear Search]
```

---

## Flow 3: Collapse/Expand Category

```
[Admin clicks category header]
    │ (config_category_toggle: { category, expanded })
    ▼
Toggle section visibility
    ├─ Was expanded → Collapse (hide rows)
    └─ Was collapsed → Expand (show rows)

Note: Collapse state is component-local. Reset on page reload.
Search overrides collapse state (matching categories auto-expand).
```

---

## Flow 4: Edit Non-Critical Config

```
[Admin clicks [Edit] on a non-critical config row]
    │ (config_edit_start: { key, is_critical: false })
    ▼
Expand inline edit form
    ├─ Close any other open edit form first
    ├─ Show: current value, edit control (type-aware), reason textarea
    ├─ Pre-fill value with current value
    └─ Focus on value input
    │
    ▼
[Admin modifies value]
    │
    ▼
Client-side validation
    ├─ INTEGER: Must be integer, within min/max range
    ├─ DECIMAL: Must be number, within min/max range
    ├─ BOOLEAN: Toggle (always valid)
    ├─ STRING with allowed_values: Must be in list
    ├─ STRING without allowed_values: Non-empty
    ├─ JSON: Must be valid JSON syntax
    ├─ DURATION: Must be integer, within min/max range
    │
    ├─ Valid → Enable [Save]
    └─ Invalid → Show inline error, disable [Save]
    │
    ▼
[Admin enters reason and clicks [Save]]
    │ (config_update: { key, new_value, is_critical: false })
    ▼
PUT /api/admin/config/{key}
    ├─ Success → Collapse form
    │     ├─ Update row with new value
    │     ├─ Toast: "Configuration updated."
    │     ├─ If requires_restart: Toast: "Service restart required for changes to take effect."
    │     └─ Highlight row briefly (recently changed)
    ├─ ERR_CFG_002 → Inline error: "Invalid value. Expected {type}."
    ├─ ERR_CFG_003 → Inline error: "Value out of range. Must be between {min} and {max}."
    ├─ ERR_CFG_004 → Inline error: "Invalid value. Must be one of: {values}."
    └─ Other error → Toast (error) + keep form open
```

---

## Flow 5: Edit Critical Config (Request Approval)

```
[Admin clicks [Edit] on a critical config row]
    │ (config_edit_start: { key, is_critical: true })
    ▼
Expand inline edit form
    ├─ Same as Flow 4 but [Save] button replaced with [Request Approval]
    ├─ Info banner: "This is a critical configuration. Changes require approval from another admin."
    └─ If PENDING request exists: form disabled, show "Pending approval" status
    │
    ▼
[Admin modifies value and enters reason]
    │
    ▼
Client-side validation (same as Flow 4)
    │
    ▼
[Admin clicks [Request Approval]]
    │ (config_approval_request: { key, new_value })
    ▼
POST /api/admin/config/requests
    ├─ Success → Collapse form
    │     ├─ Toast: "Approval request submitted. Another admin must approve."
    │     ├─ Row shows "Pending Approval" badge
    │     ├─ [Edit] disabled for this config
    │     └─ Approval badge count in header increments
    ├─ ERR_CFG_006 → Toast: "Critical config requires approval workflow." (shouldn't happen — UI already routes here)
    ├─ ERR_CFG_009 → Toast: "A pending request already exists for this config."
    │     └─ Show link to approvals page
    └─ Validation errors → Same as Flow 4
```

---

## Flow 6: Cancel Edit

```
[Admin clicks [Cancel] on inline edit form]
    │ (config_edit_cancel: { key })
    ▼
Check for unsaved changes
    ├─ No changes → Collapse form immediately
    └─ Has changes → Collapse form immediately (no guard — lightweight edit)
          └─ Value reverts to original
```

---

## Flow 7: View Change History

```
[Admin clicks [History] link or navigates to /admin/config/history]
    │ (page_view: admin_config_history)
    ▼
Load change history (paginated, most recent first)
    ├─ Success → Render history table
    │     ├─ Columns: Timestamp, Config Key, Old Value, New Value, Changed By, Reason, Actions
    │     ├─ Sensitive values masked as "••••••••"
    │     ├─ Rolled-back entries show strikethrough + "Rolled back" badge
    │     ├─ [Rollback] button on non-rolled-back entries
    │     └─ Pagination: 25 per page
    └─ Failure → Error state + [Retry]
```

---

## Flow 8: Filter Change History

```
[Admin adjusts history filters]
    │ (config_history_filter: { date_from, date_to, category, search })
    ▼
Apply filters
    ├─ Date range: date pickers (From / To)
    ├─ Category: dropdown with "All Categories" default
    ├─ Search: text input (matches key), 300ms debounce
    ├─ Filters apply on change (no submit button)
    ├─ Reset page to 1 on filter change
    └─ URL query params synced for shareability
    │
    ▼
GET /api/admin/config/history?...
    ├─ Results → Update table
    └─ No results → "No changes match your filters." + [Clear Filters]
```

---

## Flow 9: Rollback Config Change

```
[Admin clicks [Rollback] on a history entry]
    │ (config_rollback_start: { history_id, key })
    ▼
Open rollback confirmation modal
    ├─ Show: config key, current value, value to revert to
    ├─ Reason textarea (required)
    └─ [Cancel] [Confirm Rollback]
    │
    ▼
[Admin enters reason and clicks [Confirm Rollback]]
    │ (config_rollback_confirm: { history_id, key })
    ▼
POST /api/admin/config/history/{id}/rollback
    ├─ Success → Close modal
    │     ├─ Toast: "Configuration rolled back successfully."
    │     ├─ History entry shows "Rolled back" badge
    │     ├─ New history entry appears at top (the rollback itself)
    │     ├─ If requires_restart: Toast: "Service restart required."
    │     └─ Refresh history table
    ├─ ERR_CFG_011 → Toast: "History record not found." + close modal + refresh
    ├─ ERR_CFG_012 → Toast: "This change was already rolled back." + close modal + refresh
    └─ Other error → Toast (error) + keep modal open
```

---

## Flow 10: View Approval Queue

```
[Admin clicks [Approvals (N)] link or navigates to /admin/config/approvals]
    │ (page_view: admin_config_approvals)
    ▼
Load pending requests + recent decisions
    ├─ Success → Render two sections
    │     ├─ Pending Requests (top)
    │     │     ├─ Cards: config key, current→proposed value, requester, reason, time remaining
    │     │     ├─ [Approve] [Reject] per card
    │     │     ├─ Cannot approve own request (buttons hidden/disabled)
    │     │     └─ Empty: "No pending approval requests."
    │     └─ Recent Decisions (bottom)
    │           ├─ Table: config key, decision, decided by, timestamp
    │           ├─ Status badges: green (Approved), red (Rejected), gray (Expired)
    │           └─ Last 20 decisions shown
    └─ Failure → Error state + [Retry]
```

---

## Flow 11: Approve Critical Config Change

```
[Admin clicks [Approve] on a pending request]
    │ (config_approval_approve_start: { request_id, key })
    ▼
Check: Is current admin the requester?
    ├─ Yes → Button should already be disabled. If reached: Toast: "Cannot approve your own request."
    └─ No → Proceed
    │
    ▼
Inline confirmation: "Apply {key} = {new_value}?"
    │
    ▼
[Admin confirms]
    │ (config_approval_approve: { request_id, key })
    ▼
POST /api/admin/config/requests/{id}/approve
    ├─ Success
    │     ├─ Toast: "Configuration change approved and applied."
    │     ├─ Request moves to Recent Decisions as "Approved"
    │     ├─ Pending count decrements
    │     ├─ If requires_restart: Toast: "Service restart required."
    │     └─ Config list shows new value (on next visit)
    ├─ ERR_CFG_007 → Toast: "Cannot approve your own request."
    ├─ ERR_CFG_009 → Toast: "Request already processed." + refresh
    ├─ ERR_CFG_010 → Toast: "Request has expired." + refresh
    └─ Other error → Toast (error)
```

---

## Flow 12: Reject Critical Config Change

```
[Admin clicks [Reject] on a pending request]
    │ (config_approval_reject_start: { request_id, key })
    ▼
Open reject reason modal
    ├─ Show: config key, proposed change, requester
    ├─ Reason textarea (required, min 1 char)
    └─ [Cancel] [Reject]
    │
    ▼
[Admin enters reason and clicks [Reject]]
    │ (config_approval_reject: { request_id, key })
    ▼
POST /api/admin/config/requests/{id}/reject
    ├─ Success → Close modal
    │     ├─ Toast: "Configuration change request rejected."
    │     ├─ Request moves to Recent Decisions as "Rejected"
    │     └─ Pending count decrements
    ├─ ERR_CFG_008 → Toast: "Request not found." + close modal + refresh
    ├─ ERR_CFG_009 → Toast: "Request already processed." + close modal + refresh
    └─ Other error → Toast (error) + keep modal open
```

---

## Flow 13: Reveal Sensitive Config Value

```
[Admin clicks [Reveal] on a sensitive config]
    │ (config_sensitive_reveal: { key })
    ▼
GET /api/admin/config/{key}/reveal
    ├─ Success → Show actual value for 30 seconds
    │     ├─ Replace "••••••••" with real value
    │     ├─ [Reveal] becomes [Hide]
    │     ├─ Auto-hide after 30 seconds
    │     └─ Click [Hide] to mask immediately
    └─ Failure → Toast: "Unable to reveal value."
```

---

## Flow 14: Invalidate Config Cache

```
[Admin clicks [Invalidate Cache] in config page header]
    │ (config_cache_invalidate: {})
    ▼
Confirmation: "Invalidate all config cache entries?"
    │
    ▼
[Admin confirms]
    │
    ▼
POST /api/admin/config/cache/invalidate
    ├─ Success
    │     ├─ Toast: "Config cache invalidated. Values will reload from database."
    │     └─ Cache status indicator refreshes
    └─ Failure → Toast (error)
```

---

## Flow 15: Reset Config to Default

```
[Admin clicks [Reset to Default] in inline edit form]
    │ (config_reset_default: { key })
    ▼
Pre-fill value field with config.default_value
    ├─ Admin still must enter reason and click [Save] / [Request Approval]
    └─ Does NOT auto-save — just populates the edit form
```

---

## Flow 16: View Config with Pending Approval

```
[Admin views config list with a config that has PENDING request]
    │
    ▼
Config row shows "Pending Approval" badge
    ├─ [Edit] button disabled for this config
    ├─ Tooltip: "Change pending approval. View in Approvals."
    └─ Click badge → Navigate to /admin/config/approvals
```

---

## Flow 17: Approval Request Expiry

```
[PENDING request reaches 24-hour expiry]
    │
    ▼
Backend sets status = EXPIRED
    │
    ▼
On next page load or refresh:
    ├─ Config row: "Pending Approval" badge removed, [Edit] re-enabled
    ├─ Approvals page: Request moves to Recent Decisions as "Expired"
    └─ Pending count decrements

Note: No real-time push. Detected on next API fetch.
```

---

## Flow 18: History Pagination

```
[Admin clicks page control on history table]
    │ (config_history_page: { page })
    ▼
GET /api/admin/config/history?page={N}&...
    ├─ Success → Update table rows
    │     └─ "Showing {start}-{end} of {total}" updates
    └─ Failure → Toast (error). Keep current page.
```
