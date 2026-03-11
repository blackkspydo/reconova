# Reference (Super Admin & Operations)

Scope: Error handling matrix, input validation rules, security considerations, and frontend-to-backend action mapping.

---

## Error Handling Matrix

### Authorization Errors

| Error Code | HTTP | User Message | UI Action |
|-----------|------|-------------|-----------|
| `ERR_ADM_001` | 403 | "Super admin access required." | Toast (error). Redirect to /dashboard. |

### Impersonation Errors

| Error Code | HTTP | User Message | UI Action |
|-----------|------|-------------|-----------|
| `ERR_ADM_002` | 400 | "Cannot impersonate deactivated tenant." | Toast (error). Keep tenant detail open. Disable [Impersonate] button. |
| `ERR_ADM_003` | 400 | "Impersonation reason required (minimum 10 characters)." | Inline error on reason field. Keep modal open. |
| `ERR_ADM_004` | 409 | "Impersonation session already active. End current session first." | Toast (error). Show current session info in toast. |

### Credit Adjustment Errors

| Error Code | HTTP | User Message | UI Action |
|-----------|------|-------------|-----------|
| `ERR_ADM_005` | 400 | "Cannot deduct more credits than available balance." | Inline error on amount field. Keep form open. |

### Tenant Suspension Errors

| Error Code | HTTP | User Message | UI Action |
|-----------|------|-------------|-----------|
| `ERR_ADM_006` | 400 | "Tenant already suspended." | Toast (warning). Refresh tenant status badge. |
| `ERR_ADM_007` | 400 | "Cannot suspend deactivated tenant." | Toast (error). Refresh tenant status badge. |

### API Key Errors

| Error Code | HTTP | User Message | UI Action |
|-----------|------|-------------|-----------|
| `ERR_ADM_008` | 404 | "API key not found." | Toast (error). Refresh key list. |
| `ERR_ADM_010` | 400 | "No active API keys available for provider {name}." | Toast (error). Highlight provider in API key pool health. |

### Identity Errors

| Error Code | HTTP | User Message | UI Action |
|-----------|------|-------------|-----------|
| `ERR_ADM_009` | 400 | "Cannot delete last super admin. Platform requires at least one." | Toast (error). Disable delete action. |

### Network & System Errors

| Scenario | User Message | UI Action |
|----------|-------------|-----------|
| Network timeout | "Unable to connect. Check your connection and try again." | Toast (error) + [Retry] button. |
| 500 Internal Server Error | "Something went wrong. Please try again." | Toast (error) + [Retry] button. |
| Monitoring section fetch failure | "Failed to load {section} metrics." | Inline error on section + [Retry]. Other sections unaffected. |
| Impersonation token expired (401) | "Impersonation session expired." | Toast (warning). Clear state. Redirect to admin panel. |

### Error Response Parsing

```typescript
function handleAdminError(error: ApiError): void {
  const { code, message } = error.error;

  switch (code) {
    case 'ERR_ADM_001':
      // Not a super admin
      showToast('error', message);
      navigateTo('/dashboard');
      break;

    case 'ERR_ADM_002':
      // Deactivated tenant impersonation
      showToast('error', message);
      setButtonDisabled('impersonate', true);
      break;

    case 'ERR_ADM_003':
      // Reason too short
      setFieldError('reason', message);
      break;

    case 'ERR_ADM_004':
      // Already impersonating
      showToast('error', message);
      break;

    case 'ERR_ADM_005':
      // Insufficient credits for deduction
      setFieldError('amount', message);
      break;

    case 'ERR_ADM_006':
      // Already suspended
      showToast('warning', message);
      refetchTenant();
      break;

    case 'ERR_ADM_007':
      // Deactivated tenant
      showToast('error', message);
      refetchTenant();
      break;

    case 'ERR_ADM_008':
      // API key not found
      showToast('error', message);
      refetchApiKeys();
      break;

    case 'ERR_ADM_009':
      // Last super admin
      showToast('error', message);
      setButtonDisabled('delete', true);
      break;

    case 'ERR_ADM_010':
      // No API keys for provider
      showToast('error', message);
      break;

    default:
      showToast('error', message);
  }
}
```

---

## Input Validation Rules

### Maintenance Mode — Enable

| Field | Type | Constraints | Error Message |
|-------|------|-------------|---------------|
| `reason` | string | Required. Min 10 chars. Max 500 chars. | "Reason is required." / "Reason must be at least 10 characters." / "Reason must be 500 characters or fewer." |
| `estimated_duration_minutes` | integer | Required. Min 1. Max 1440 (24 hours). | "Estimated duration is required." / "Duration must be at least 1 minute." / "Duration cannot exceed 24 hours." |

### Alert Threshold — Edit

| Field | Type | Constraints | Error Message |
|-------|------|-------------|---------------|
| `threshold` | number | Required. Numeric. Within alert-specific min/max range. | "Threshold must be a number." / "Threshold must be between {min} and {max}." |

### Alert Threshold Ranges

| Alert Type | Min | Max | Unit |
|-----------|-----|-----|------|
| Queue depth % | 1 | 100 | percent |
| Zero active workers | 1 | 100 | count (minimum workers required) |
| API key pool min | 1 | 50 | keys |
| DB pool capacity % | 1 | 100 | percent |
| Credit anomaly multiplier | 1.5 | 100 | times daily average |
| Worker stale timeout | 1 | 60 | minutes |
| API key quota exhausted | 1 | 50 | keys per provider |
| DB backup failure | 1 | 10 | consecutive failures |

### Impersonation — Start (Cross-reference: tenant-management plan)

| Field | Type | Constraints | Error Message |
|-------|------|-------------|---------------|
| `reason` | string | Required. Min 10 chars. Max 500 chars. | "Reason is required." / "Reason must be at least 10 characters." |
| `target_tenant_id` | string | Required. Must exist. Must not be DEACTIVATED. | "Tenant not found." / "Cannot impersonate deactivated tenant." |

### Client-Side Validation Timing

| Trigger | Behavior |
|---------|----------|
| Maintenance reason blur | Validate ≥ 10 chars |
| Maintenance reason input | Live character count (`{N} / 500`) |
| Duration blur | Validate numeric, ≥ 1, ≤ 1440 |
| Alert threshold blur | Validate numeric, within range |
| Alert threshold input | Immediate format validation (numeric only) |

---

## Security Considerations

### Secure Storage

| Data | Storage | Justification |
|------|---------|---------------|
| Dashboard summary | Memory (state) | Re-fetched per page visit |
| Monitoring metrics | Memory (state) | Auto-refreshed at intervals. Never persisted. |
| Active alerts | Memory (state) | Fetched per page load + auto-refresh |
| Alert rules | Memory (state) | Fetched per alerts page visit |
| Maintenance status | Memory (state) | Fetched with dashboard + header |
| Impersonation state | JWT claims + memory | Read from token. Timer in component state. |
| Impersonation reason | Memory (state) | Displayed in indicator. Cleared on session end. |
| Admin session ID | JWT claim | Used for audit trail grouping. Not stored separately. |

### API Authorization

- All `/api/admin/*` endpoints require `SUPER_ADMIN` role
- Role checked server-side on every request — frontend guard is UX convenience
- Impersonation JWT is separate from admin JWT — scoped to target tenant
- Impersonation JWT includes `impersonated_by` claim for audit trail
- Admin cannot access admin panel endpoints while impersonating (uses impersonation JWT)
- Monitoring endpoints query read replicas / cached aggregates — never primary production DB

### Rate Limiting Awareness

| Action | Rate Limit | Frontend Handling |
|--------|-----------|-------------------|
| Dashboard summary fetch | No specific limit | Standard fetch |
| Monitoring metric fetch | Backend enforced per endpoint | Auto-refresh respects interval; no parallel fetches for same section |
| Alert threshold update | Backend enforced | Disable [Save] during API call |
| Alert rule toggle | Backend enforced | Disable toggle during API call |
| Maintenance enable/disable | Backend enforced | Disable button during API call |
| Impersonation start | 10 per hour per admin | Disable [Impersonate] during call; show cooldown if 429 |
| Impersonation end | No specific limit | Disable [End Session] during API call |
| Alert badge refresh | Once per 60 seconds | Client-side interval, no manual trigger |

### Button Debouncing

| Action | Debounce Strategy |
|--------|-------------------|
| Enable maintenance | Disable [Enable Maintenance] → re-enable on response or error |
| Disable maintenance | Disable [Disable Maintenance] → re-enable on response |
| Save alert threshold | Disable [Save] → re-enable on response or error |
| Toggle alert rule | Disable toggle → re-enable on response (optimistic) |
| End impersonation | Disable [End Session] → re-enable on response |
| Metric section retry | Disable [Retry] → re-enable on response |
| Summary card click | No debounce (navigation only) |

### Input Sanitization

| Input | Sanitization |
|-------|-------------|
| Maintenance reason | Trim whitespace. No HTML tags. Max 500 chars. |
| Duration minutes | Parse as integer. Reject non-numeric. Range 1–1440. |
| Alert threshold | Parse as number. Reject non-numeric. Validate per-alert range. |
| Impersonation reason | Trim whitespace. No HTML tags. Max 500 chars. |

---

## Key Actions → Backend Use Cases Mapping

### Super Admin Actions (This Plan)

| Frontend Action | Backend Use Case | Endpoint | Triggers |
|----------------|-----------------|----------|----------|
| View admin dashboard | Load summary + maintenance status | `GET /api/admin/dashboard/summary` | `/admin` page load |
| View monitoring | Load all metric sections | `GET /api/admin/monitoring/*` (5 endpoints) | `/admin/monitoring` page load |
| Drill into metric | (Client-side — data already loaded) | — | Metric card click |
| Collapse/expand section | (Client-side — toggle visibility) | — | Section header click |
| View active alerts (badge) | Load recent alerts | `GET /api/admin/monitoring/alerts/active` | Badge click or auto-refresh |
| View alert management | Load alerts + rules | `GET /api/admin/monitoring/alerts/*` (2 endpoints) | `/admin/monitoring/alerts` page load |
| Edit alert threshold | Update alert rule | `PUT /api/admin/monitoring/alerts/rules/{id}` | [Save] in inline edit |
| Toggle alert rule | Enable/disable alert | `PUT /api/admin/monitoring/alerts/rules/{id}` | Toggle click |
| Enable maintenance | BR-ADM-010A: Enable maintenance | `POST /api/admin/maintenance/enable` | [Enable Maintenance] submit |
| Disable maintenance | BR-ADM-010B: Disable maintenance | `POST /api/admin/maintenance/disable` | [Disable Maintenance] confirmed |
| End impersonation | BR-ADM-003B: End session | `POST /api/admin/impersonation/end` | [End Session] confirmed |
| Request browser notifications | (Client-side — Notification API) | — | [Enable] click on prompt |

### Cross-Referenced Actions (Other Plans)

| Frontend Action | Backend Use Case | Plan | Route |
|----------------|-----------------|------|-------|
| Start impersonation | BR-ADM-003A | tenant-management | `/admin/tenants/:id` |
| Adjust credits | BR-ADM-004A | billing-credits | `/admin/billing/credits` |
| Suspend tenant | BR-ADM-006A | tenant-management | `/admin/tenants/:id` |
| Reactivate tenant | BR-ADM-006B | tenant-management | `/admin/tenants/:id` |
| Manage API keys | BR-ADM-007A/B/C | integrations | `/admin/integrations` |
| Manage frameworks | BR-ADM-008A/B | compliance-engine | `/admin/compliance` |
| Update scan pricing | BR-ADM-009A | billing-credits | `/admin/billing/pricing` |
| Toggle feature overrides | BR-ADM-005 | feature-flags-access-control | `/admin/features/overrides` |

### System-Triggered UI Updates

| Backend Event | Frontend Effect | How Applied |
|--------------|----------------|-------------|
| Maintenance enabled | Maintenance badge appears in admin header. Tenant-facing maintenance banner. | Next page load or dashboard summary refresh |
| Maintenance disabled | Badge removed. Tenant banners cleared. | Next page load or dashboard summary refresh |
| Alert threshold breached | Metric card highlighted. Alert badge count incremented. | Auto-refresh cycle on monitoring page |
| Alert resolved (value drops below threshold) | Highlight removed. Badge count decremented. | Auto-refresh cycle |
| Impersonation session expires | Indicator fades. Toast warning. Redirect to admin. | Client-side timer reaches 0 |
| New critical alert fires | Browser notification (if permission granted) | Auto-refresh detects new alert |
| Worker becomes stale | Worker metrics section shows stale count increase | 30-second auto-refresh |
| API key quota exhausted | System health shows provider health change | 5-minute auto-refresh |

---

## State Management Notes

### What State to Track

| State | Scope | Persistence |
|-------|-------|-------------|
| Dashboard summary | Dashboard page | Memory. Fetch on page load. |
| Maintenance mode status | Admin header (global) | Memory. Fetch with dashboard or on mount. |
| Active alerts count | Admin header (global) | Memory. Auto-refresh every 60 seconds. |
| Recent alerts (dropdown) | Admin header | Memory. Fetch on badge click. |
| Tenant metrics | Monitoring page | Memory. Auto-refresh every 5 min. |
| Scan metrics | Monitoring page | Memory. Auto-refresh every 30 sec. |
| Worker metrics | Monitoring page | Memory. Auto-refresh every 30 sec. |
| Credit metrics | Monitoring page | Memory. Auto-refresh every 60 min. |
| System health metrics | Monitoring page | Memory. Auto-refresh every 1 min. |
| Expanded metric | Monitoring page | Component state. One per section. |
| Collapsed sections | Monitoring page | Component state. Default: all expanded. |
| Browser notification permission | Monitoring page | Browser API (persisted by browser). |
| Active alerts list | Alerts page | Memory. Fetch on page load. |
| Alert rules | Alerts page | Memory. Fetch on page load. |
| Editing rule ID | Alerts page | Component state. One at a time. |
| Threshold edit form | Alerts page | Component state. Cleared on save/cancel. |
| Maintenance form | Modal | Component state. Cleared on submit/cancel. |
| Impersonation session | Global (app root) | JWT claims + component state. Timer client-side. |
| Impersonation indicator expanded | Global | Component state. Default: collapsed. |

### Reset Behavior

| Event | State Reset |
|-------|-------------|
| Dashboard loaded | Fetch fresh summary |
| Monitoring page entered | Start all auto-refresh timers |
| Monitoring page left | Stop all auto-refresh timers |
| Tab becomes hidden | Pause auto-refresh |
| Tab becomes visible | Resume auto-refresh, fetch immediately |
| Alert threshold updated | Re-evaluate active alerts against new threshold |
| Alert rule toggled | Dismiss active alert if rule disabled |
| Maintenance enabled | Update header badge, refresh dashboard |
| Maintenance disabled | Update header badge, refresh dashboard |
| Impersonation started | Set global impersonation state, start timer |
| Impersonation ended | Clear global state, redirect to admin |
| Impersonation expired | Clear global state, toast, redirect |
| Navigation away from alerts page | Clear editing state |
| Logout | Clear all admin state, stop all timers |

### Unsaved Changes Guard

| Page | Guard Behavior |
|------|---------------|
| Maintenance enable modal | If form has values and user clicks outside or ✕: "Discard changes?" dialog |
| Alert threshold inline edit | If value changed and user clicks another edit or navigates: auto-cancel (revert to original) |
| All other admin pages | No guard needed |
