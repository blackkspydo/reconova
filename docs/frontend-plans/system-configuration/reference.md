# Reference (System Configuration)

Scope: Error handling matrix, input validation rules, security considerations, and frontend-to-backend action mapping.

---

## Error Handling Matrix

### Authorization Errors

| Error Code | HTTP | User Message | UI Action |
|-----------|------|-------------|-----------|
| `ERR_CFG_005` | 403 | "Insufficient permissions to modify system configuration." | Toast (error). Redirect to /dashboard. |

### Validation Errors

| Error Code | HTTP | User Message | UI Action |
|-----------|------|-------------|-----------|
| `ERR_CFG_001` | 404 | "Configuration key not found." | Toast (error). Refresh config list. |
| `ERR_CFG_002` | 400 | "Invalid configuration value. Expected {data_type}." | Inline error on value input. Keep form open. |
| `ERR_CFG_003` | 400 | "Value out of range. Must be between {min} and {max}." | Inline error on value input. Keep form open. |
| `ERR_CFG_004` | 400 | "Invalid value. Must be one of: {allowed_values}." | Inline error on value input. Keep form open. |

### Critical Config Errors

| Error Code | HTTP | User Message | UI Action |
|-----------|------|-------------|-----------|
| `ERR_CFG_006` | 403 | "Critical configuration change requires approval workflow." | Toast (warning). Switch button to [Request Approval]. Refresh config. |
| `ERR_CFG_007` | 403 | "Cannot approve your own configuration change request." | Toast (error). Hide [Approve] button. |

### Change Request Errors

| Error Code | HTTP | User Message | UI Action |
|-----------|------|-------------|-----------|
| `ERR_CFG_008` | 404 | "Configuration change request not found." | Toast (error). Refresh approval queue. |
| `ERR_CFG_009` | 409 | "Configuration change request already processed." | Toast (warning). Refresh approval queue. |
| `ERR_CFG_010` | 400 | "Configuration change request has expired." | Toast (warning). Move request to Recent Decisions. Refresh. |

### Rollback Errors

| Error Code | HTTP | User Message | UI Action |
|-----------|------|-------------|-----------|
| `ERR_CFG_011` | 404 | "Configuration history record not found." | Toast (error). Close modal. Refresh history. |
| `ERR_CFG_012` | 409 | "Configuration change already rolled back." | Toast (warning). Close modal. Refresh history. Hide [Rollback]. |

### Network & System Errors

| Scenario | User Message | UI Action |
|----------|-------------|-----------|
| Network timeout | "Unable to connect. Check your connection and try again." | Toast (error) + [Retry] button. |
| 500 Internal Server Error | "Something went wrong. Please try again." | Toast (error) + [Retry] button. |
| Config list fetch failure | "Failed to load system configuration." | Full page error state + [Retry]. |
| History fetch failure | "Failed to load change history." | Full page error state + [Retry]. |
| Approvals fetch failure | "Failed to load approval queue." | Full page error state + [Retry]. |
| Cache invalidation failure | "Failed to invalidate cache." | Toast (error). Re-enable [Invalidate Cache]. |
| Sensitive reveal failure | "Unable to reveal value." | Toast (error). Keep value masked. |

### Error Response Parsing

```typescript
function handleConfigError(error: ApiError): void {
  const { code, message } = error.error;

  switch (code) {
    case 'ERR_CFG_001':
      // Config key not found
      showToast('error', message);
      refetchConfigs();
      break;

    case 'ERR_CFG_002':
    case 'ERR_CFG_003':
    case 'ERR_CFG_004':
      // Validation errors
      setFieldError('value', message);
      break;

    case 'ERR_CFG_005':
      // Not super admin
      showToast('error', message);
      navigateTo('/dashboard');
      break;

    case 'ERR_CFG_006':
      // Critical config — shouldn't happen if UI routes correctly
      showToast('warning', message);
      refetchConfigs();
      break;

    case 'ERR_CFG_007':
      // Self-approval
      showToast('error', message);
      setButtonHidden('approve', true);
      break;

    case 'ERR_CFG_008':
      // Request not found
      showToast('error', message);
      refetchApprovals();
      break;

    case 'ERR_CFG_009':
      // Already processed
      showToast('warning', message);
      refetchApprovals();
      break;

    case 'ERR_CFG_010':
      // Expired
      showToast('warning', message);
      refetchApprovals();
      break;

    case 'ERR_CFG_011':
      // History not found
      showToast('error', message);
      closeRollbackModal();
      refetchHistory();
      break;

    case 'ERR_CFG_012':
      // Already rolled back
      showToast('warning', message);
      closeRollbackModal();
      refetchHistory();
      break;

    default:
      showToast('error', message);
  }
}
```

---

## Input Validation Rules

### Config Value — By Data Type

| Data Type | Constraints | Error Message |
|-----------|-------------|---------------|
| `INTEGER` | Required. Must parse as integer (no decimals). If min_value set: value ≥ min. If max_value set: value ≤ max. | "Value must be a whole number." / "Value out of range. Must be between {min} and {max}." |
| `DECIMAL` | Required. Must parse as number. If min_value set: value ≥ min. If max_value set: value ≤ max. | "Value must be a number." / "Value out of range. Must be between {min} and {max}." |
| `BOOLEAN` | Required. Must be `"true"` or `"false"`. | (Toggle — always valid from UI) |
| `STRING` (with allowed_values) | Required. Must be in comma-separated allowed_values list. | "Invalid value. Must be one of: {values}." |
| `STRING` (without allowed_values) | Required. Non-empty after trim. | "Value is required." |
| `JSON` | Required. Must be valid JSON (JSON.parse succeeds). | "Invalid JSON syntax." |
| `DURATION` | Required. Must parse as integer. If min_value set: value ≥ min. If max_value set: value ≤ max. | "Value must be a whole number." / "Value out of range. Must be between {min} and {max}." |

### Config Update — Reason

| Field | Type | Constraints | Error Message |
|-------|------|-------------|---------------|
| `reason` | string | Required. Min 1 char after trim. Max 500 chars. | "Reason is required." / "Reason must be 500 characters or fewer." |

### Rollback — Reason

| Field | Type | Constraints | Error Message |
|-------|------|-------------|---------------|
| `reason` | string | Required. Min 1 char after trim. Max 500 chars. | "Reason is required." / "Reason must be 500 characters or fewer." |

### Reject — Reason

| Field | Type | Constraints | Error Message |
|-------|------|-------------|---------------|
| `reason` | string | Required. Min 1 char after trim. Max 500 chars. | "Rejection reason is required." / "Reason must be 500 characters or fewer." |

### History Filters — Date Range

| Field | Type | Constraints | Error Message |
|-------|------|-------------|---------------|
| `date_from` | date | Optional. If set, must be ≤ `date_to`. Must not be in the future. | "Start date must be before end date." |
| `date_to` | date | Optional. If set, must be ≥ `date_from`. Must not be in the future. | "End date cannot be in the future." |

### History Filters — Search

| Field | Type | Constraints | Error Message |
|-------|------|-------------|---------------|
| `search` | string | Optional. Max 200 chars. | "Search term must be 200 characters or fewer." |

### Client-Side Validation Timing

| Trigger | Behavior |
|---------|----------|
| Value input change | Immediate type validation (numeric check, JSON parse) |
| Value input blur | Full validation (range check, enum check) |
| Reason input blur | Validate non-empty |
| Reason input change | Live character count (`{N} / 500`) |
| JSON textarea input | Validate JSON on each change with debounce (500ms) |
| Date picker change | Validate range consistency immediately |
| Search input | 300ms debounce before filtering |

---

## Security Considerations

### Secure Storage

| Data | Storage | Justification |
|------|---------|---------------|
| Config list | Memory (state) | Fetched on page load. Contains business-critical values. |
| Sensitive config values (masked) | Memory (state) | Always masked in list response. |
| Sensitive config values (revealed) | Component state | Auto-cleared after 30 seconds. Never persisted. |
| Change history | Memory (state) | Fetched per filter/page change. Sensitive values masked. |
| Approval requests | Memory (state) | Fetched on page load. |
| Edit form values | Component state | Cleared on cancel/submit. |
| Rollback/reject reasons | Component state | Cleared on modal close. |

### API Authorization

- All `/api/admin/config/*` endpoints require `SUPER_ADMIN` role
- Role checked server-side on every request — frontend guard is UX convenience
- Sensitive config reveal endpoint (`GET /api/admin/config/{key}/reveal`) logged in audit trail
- Approval endpoint validates approver ≠ requester server-side
- Config update endpoint rejects critical configs (must use approval workflow)
- History rollback endpoint validates entry exists and is not already rolled back

### Rate Limiting Awareness

| Action | Rate Limit | Frontend Handling |
|--------|-----------|-------------------|
| Load config list | No specific limit | Standard fetch |
| Update config | Backend enforced | Disable [Save] during API call |
| Request approval | Backend enforced | Disable [Request Approval] during API call |
| Approve request | Backend enforced | Disable [Approve] during API call |
| Reject request | Backend enforced | Disable [Reject] during API call |
| Rollback | Backend enforced | Disable [Confirm Rollback] during API call |
| Reveal sensitive | Backend enforced | Disable [Reveal] during API call |
| Invalidate cache | Backend enforced | Disable [Invalidate Cache] during API call |
| History page fetch | Backend enforced | Standard pagination fetch |

### Button Debouncing

| Action | Debounce Strategy |
|--------|-------------------|
| Save config | Disable [Save] → re-enable on response or error |
| Request approval | Disable [Request Approval] → re-enable on response or error |
| Approve | Disable [Approve] → re-enable on response or error |
| Reject | Disable [Reject] → re-enable on response or error |
| Confirm rollback | Disable [Confirm Rollback] → re-enable on response or error |
| Reveal sensitive | Disable [Reveal] → re-enable on response |
| Invalidate cache | Disable [Invalidate Cache] → re-enable on response or error |
| Search input | 300ms debounce before filtering |
| Category toggle | Instant (client-side only) |

### Input Sanitization

| Input | Sanitization |
|-------|-------------|
| Config value (STRING) | Trim whitespace. No HTML tags. Server validates further. |
| Config value (JSON) | Parsed with JSON.parse. No additional sanitization. |
| Config value (numeric) | Parsed as number. Reject non-numeric input. |
| Reason fields | Trim whitespace. No HTML tags. Max 500 chars. |
| Search term | Trim whitespace. Escape for URL query param. Max 200 chars. |
| Date values | Validate as ISO date format. Reject invalid dates. |

---

## Key Actions → Backend Use Cases Mapping

### Config Management Actions

| Frontend Action | Backend Use Case | Endpoint | Triggers |
|----------------|-----------------|----------|----------|
| View config list | Load all configs | `GET /api/admin/config` | `/admin/config` page load |
| Search configs | (Client-side filter) | — | Search input change |
| Collapse/expand category | (Client-side toggle) | — | Category header click |
| Edit non-critical config | BR-CFG-003A: Update config | `PUT /api/admin/config/{key}` | [Save] click |
| Request critical config change | BR-CFG-008A: Request change | `POST /api/admin/config/requests` | [Request Approval] click |
| Reset to default | (Client-side — pre-fill form) | — | [Reset to Default] click |
| Reveal sensitive value | Load unmasked value | `GET /api/admin/config/{key}/reveal` | [Reveal] click |
| Hide sensitive value | (Client-side — re-mask) | — | [Hide] click or 30s timer |
| Invalidate cache | BR-CFG-007: Cache invalidation | `POST /api/admin/config/cache/invalidate` | [Invalidate Cache] confirmed |

### History Actions

| Frontend Action | Backend Use Case | Endpoint | Triggers |
|----------------|-----------------|----------|----------|
| View change history | BR-CFG-005: Load history | `GET /api/admin/config/history` | `/admin/config/history` page load |
| Filter history | Filter + re-fetch | `GET /api/admin/config/history?...` | Filter change |
| Rollback change | BR-CFG-006A: Rollback | `POST /api/admin/config/history/{id}/rollback` | [Confirm Rollback] click |

### Approval Actions

| Frontend Action | Backend Use Case | Endpoint | Triggers |
|----------------|-----------------|----------|----------|
| View approvals | Load pending + recent | `GET /api/admin/config/requests` (x2) | `/admin/config/approvals` page load |
| Approve request | BR-CFG-008B: Approve | `POST /api/admin/config/requests/{id}/approve` | [Approve] confirmed |
| Reject request | BR-CFG-008C: Reject | `POST /api/admin/config/requests/{id}/reject` | [Reject] with reason |

### System-Triggered UI Updates

| Backend Event | Frontend Effect | How Applied |
|--------------|----------------|-------------|
| Config updated (by another admin) | Stale data until page refresh | Next GET /api/admin/config fetch |
| Critical request approved | Config value changes, request moves to decisions | Next page load or refresh |
| Critical request rejected | Requester sees rejection in decisions | Next page load or refresh |
| Critical request expired (24h) | Request moves to decisions, config edit re-enabled | Next page load or refresh |
| Cache invalidated | Cache status refreshes | Response from POST call |
| Config requiring restart updated | "Restart Required" chip remains until restart | Persistent on config row |

---

## State Management Notes

### What State to Track

| State | Scope | Persistence |
|-------|-------|-------------|
| Config list | Config list page | Memory. Fetch on page load. |
| Categories + counts | Config list page | Memory. Derived from config list. |
| Cache status | Config list page | Memory. Fetched with config list. |
| Search query | Config list page | Component state. Cleared on navigation. |
| Collapsed categories | Config list page | Component state. Default: all expanded. |
| Editing config key | Config list page | Component state. One at a time. |
| Edit form values | Config list page | Component state. Cleared on save/cancel. |
| Revealed sensitive keys | Config list page | Component state. Auto-clear after 30s each. |
| Pending approval count | Config list page header | Memory. Fetched with config list. |
| History entries | History page | Memory. Fetch on filter/page change. |
| History filters | History page | URL query params + component state. |
| Rollback modal state | History page | Component state. Cleared on close. |
| Pending requests | Approvals page | Memory. Fetch on page load. |
| Recent decisions | Approvals page | Memory. Fetch on page load. |
| Reject modal state | Approvals page | Component state. Cleared on close. |

### Reset Behavior

| Event | State Reset |
|-------|-------------|
| Config list loaded | Fetch fresh config list, cache status, pending count |
| Config updated | Update single config in list. Clear edit form. |
| Approval requested | Update config row (add pending badge). Clear edit form. Increment pending count. |
| Search cleared | Reset collapsed categories to default (all expanded). |
| History page loaded | Fetch first page with default filters. |
| Filter changed | Reset page to 1. Fetch with new filters. |
| Rollback confirmed | Close modal. Refresh history table. |
| Approvals page loaded | Fetch pending requests + recent decisions. |
| Request approved | Move from pending to recent. Decrement pending count. |
| Request rejected | Move from pending to recent. Decrement pending count. Clear reject modal. |
| Sensitive value revealed | Start 30s timer. On expiry: re-mask, remove from revealed set. |
| Navigation away | Clear edit form, search query, revealed keys, modal state. |
| Logout | Clear all state. |
