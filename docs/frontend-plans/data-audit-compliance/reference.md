# Reference (Data, Audit & Platform Compliance)

Scope: Error handling matrix, input validation rules, security considerations, and frontend-to-backend action mapping.

---

## Error Handling Matrix

### Data Export Errors

| Error Code | HTTP | User Message | UI Action |
|-----------|------|-------------|-----------|
| `ERR_DATA_001` | 409 | "An export is already in progress. Please wait for it to complete." | Toast (warning). Show current export status. Stop polling retry. |
| `ERR_DATA_002` | 429 | "Export limit reached. You can request one export per 24 hours." | Toast (warning). Disable [Request Export]. Show next-available time. |

### Data Deletion Errors

| Error Code | HTTP | User Message | UI Action |
|-----------|------|-------------|-----------|
| `ERR_DATA_003` | 400 | "Confirmation phrase does not match. Please type DELETE MY DATA exactly." | Inline error on phrase field. Keep modal on step 1. |
| `ERR_DATA_004` | 401 | "Password incorrect. Please try again." | Inline error on password field. Keep modal on step 2. Clear password. |
| `ERR_DATA_005` | 409 | "A deletion request is already pending." | Toast (warning). Close modal. Show pending deletion status. |
| `ERR_DATA_006` | 400 | "Cooling-off period has expired. Deletion is being processed." | Toast (info). Disable [Cancel Deletion]. Refresh deletion status. |
| `ERR_DATA_007` | 403 | "Only the account owner can request data deletion." | Toast (error). Hide deletion section. |

### Audit Log Errors

| Error Code | HTTP | User Message | UI Action |
|-----------|------|-------------|-----------|
| `ERR_DATA_008` | 400 | "Invalid date range. Start date must be before end date." | Inline error on date fields. Keep current results. |
| `ERR_DATA_009` | 413 | "Export too large. Please narrow your date range or filters." | Toast (warning). Keep filters open. Suggest narrower range. |

### Admin Data Management Errors

| Error Code | HTTP | User Message | UI Action |
|-----------|------|-------------|-----------|
| `ERR_DATA_010` | 409 | "A backup is already in progress." | Toast (warning). Disable [Trigger Manual Backup]. Show in-progress backup. |

### Network & System Errors

| Scenario | User Message | UI Action |
|----------|-------------|-----------|
| Network timeout | "Unable to connect. Check your connection and try again." | Toast (error) + [Retry] button. |
| 500 Internal Server Error | "Something went wrong. Please try again." | Toast (error) + [Retry] button. |
| Export download 410 Gone | "Download link expired. Refreshing..." | Auto re-fetch export status. If still READY, refresh download URLs. |
| Export polling failure | "Unable to check export status." | Inline error on export section + [Retry]. Other sections unaffected. |
| CSV export timeout | "Export is taking longer than expected. Try narrowing your filters." | Toast (warning). Re-enable [Export CSV]. |

### Error Response Parsing

```typescript
function handleDataError(error: ApiError): void {
  const { code, message } = error.error;

  switch (code) {
    case 'ERR_DATA_001':
      // Export already in progress
      showToast('warning', message);
      refetchExportStatus();
      break;

    case 'ERR_DATA_002':
      // Export rate limited
      showToast('warning', message);
      setButtonDisabled('requestExport', true);
      break;

    case 'ERR_DATA_003':
      // Wrong confirmation phrase
      setFieldError('confirmation_phrase', message);
      break;

    case 'ERR_DATA_004':
      // Wrong password
      setFieldError('password', message);
      clearField('password');
      break;

    case 'ERR_DATA_005':
      // Deletion already pending
      showToast('warning', message);
      closeDeletionModal();
      refetchDeletionStatus();
      break;

    case 'ERR_DATA_006':
      // Cooling-off expired
      showToast('info', message);
      setButtonDisabled('cancelDeletion', true);
      refetchDeletionStatus();
      break;

    case 'ERR_DATA_007':
      // Not account owner
      showToast('error', message);
      hideDeletionSection();
      break;

    case 'ERR_DATA_008':
      // Invalid date range
      setFieldError('date_range', message);
      break;

    case 'ERR_DATA_009':
      // CSV export too large
      showToast('warning', message);
      break;

    case 'ERR_DATA_010':
      // Backup already in progress
      showToast('warning', message);
      setButtonDisabled('triggerBackup', true);
      refetchBackupStatus();
      break;

    default:
      showToast('error', message);
  }
}
```

---

## Input Validation Rules

### Data Deletion — Confirmation Phrase

| Field | Type | Constraints | Error Message |
|-------|------|-------------|---------------|
| `confirmation_phrase` | string | Required. Must exactly equal `"DELETE MY DATA"` (case-sensitive). | "Please type DELETE MY DATA to confirm." |

### Data Deletion — Re-Authentication

| Field | Type | Constraints | Error Message |
|-------|------|-------------|---------------|
| `password` | string | Required. Min 1 char. Validated server-side. | "Password is required." / "Password incorrect. Please try again." |

### Audit Log Filters — Date Range

| Field | Type | Constraints | Error Message |
|-------|------|-------------|---------------|
| `date_from` | date | Optional. If set, must be ≤ `date_to`. Must not be in the future. | "Start date must be before end date." / "Start date cannot be in the future." |
| `date_to` | date | Optional. If set, must be ≥ `date_from`. Must not be in the future. | "End date must be before start date." / "End date cannot be in the future." |

### Audit Log Filters — Search

| Field | Type | Constraints | Error Message |
|-------|------|-------------|---------------|
| `search` | string | Optional. Max 200 chars. No special regex characters. | "Search term must be 200 characters or fewer." |

### Admin Audit Log — Additional Filters

| Field | Type | Constraints | Error Message |
|-------|------|-------------|---------------|
| `tenant_id` | string | Optional. Must be valid UUID if set. | "Invalid tenant selected." |
| `user_id` | string | Optional. Must be valid UUID if set. | "Invalid user selected." |

### Client-Side Validation Timing

| Trigger | Behavior |
|---------|----------|
| Confirmation phrase input | Live comparison against "DELETE MY DATA". Show checkmark when matched. |
| Password blur | Validate not empty |
| Date picker change | Validate range consistency immediately |
| Search input | 300ms debounce before applying filter |
| Category dropdown change | Apply immediately (no validation needed) |

---

## Security Considerations

### Secure Storage

| Data | Storage | Justification |
|------|---------|---------------|
| Data inventory | Memory (state) | Static data, re-fetched per page visit |
| Export status | Memory (state) | Polled during PROCESSING, fetched on page load |
| Export download URLs | Memory (state) | Pre-signed, short-lived. Never persisted to disk. |
| Deletion status | Memory (state) | Fetched per page visit |
| Deletion confirmation phrase | Component state | Cleared on modal close. Never sent to analytics. |
| Re-auth password | Component state | Cleared immediately after API call. Never logged. |
| Audit log entries | Memory (state) | Fetched per filter/page change. May contain PII — not cached. |
| Audit log details JSON | Memory (state) | Displayed in expandable panel. Not persisted. |
| Admin backup data | Memory (state) | Fetched per tab visit |
| Admin migration data | Memory (state) | Fetched per tab visit |
| CSV export file | Browser download | Handled by browser download manager. Not stored in app state. |

### API Authorization

- All `/api/privacy/*` endpoints require authenticated tenant user
- Data deletion (`POST /api/privacy/deletion`) requires `OWNER` role — members cannot delete
- Data export available to all authenticated tenant members
- Audit log (`/api/privacy/audit-log`) scoped to current tenant automatically (server-side)
- All `/api/admin/*` endpoints require `SUPER_ADMIN` role
- Admin audit log returns cross-tenant data — no client-side tenant filtering for security
- CSV export enforced server-side: same filters applied, no client-side data assembly
- Pre-signed download URLs scoped to requesting user, expire in 15 minutes

### Rate Limiting Awareness

| Action | Rate Limit | Frontend Handling |
|--------|-----------|-------------------|
| Request data export | 1 per 24 hours | Disable button after request. Show next-available time on 429. |
| Download export file | No specific limit | Standard download |
| Request data deletion | Backend enforced | Disable after submission. Show pending status. |
| Cancel deletion | Backend enforced | Disable during API call |
| Audit log page fetch | Backend enforced | Standard pagination fetch |
| Admin CSV export | Backend enforced | Disable [Export CSV] during download |
| Trigger manual backup | Backend enforced | Disable during API call + while in_progress |
| Run integrity check | Backend enforced | Disable during check execution |

### Button Debouncing

| Action | Debounce Strategy |
|--------|-------------------|
| Request export | Disable [Request Export] → re-enable on response or error |
| Download file | Disable [Download] for clicked file → re-enable on download start |
| Deletion modal [Next] (step 1) | Disable → re-enable (instant, client-side check) |
| Deletion modal [Verify] (step 2) | Disable → re-enable on response or error |
| Deletion modal [Confirm] (step 3) | Disable → re-enable on response or error |
| Cancel deletion | Disable [Cancel Deletion] → re-enable on response |
| Audit log filter change | 300ms debounce on search; immediate for dropdowns/dates |
| Export CSV | Disable [Export CSV] → re-enable on download complete or error |
| Trigger backup | Disable [Trigger] → re-enable on response |
| Run integrity check | Disable [Run Check] → re-enable on response |

### Input Sanitization

| Input | Sanitization |
|-------|-------------|
| Confirmation phrase | Exact match comparison only. No trimming (spaces matter). |
| Password | Sent as-is to server. Never logged client-side. Cleared from state after call. |
| Search term | Trim whitespace. Escape for URL query param. No regex. Max 200 chars. |
| Date values | Validate as ISO date format. Reject invalid dates. |

---

## Key Actions → Backend Use Cases Mapping

### Tenant Privacy Actions

| Frontend Action | Backend Use Case | Endpoint | Triggers |
|----------------|-----------------|----------|----------|
| View privacy center | Load inventory + export + deletion status | 3 parallel GETs | `/privacy` page load |
| View data inventory | Load categories + processors | `GET /api/privacy/data-inventory` | Privacy center mount |
| Request data export | BR-DATA-020: Data portability | `POST /api/privacy/export` | [Request Export] click |
| Check export status | Poll export progress | `GET /api/privacy/export` | 10-second polling during PROCESSING |
| Download export file | Download pre-signed file | `GET /api/privacy/export/{id}/files/{fileId}/download` | [Download] click |
| Request data deletion | BR-DATA-022: Right to erasure | `POST /api/privacy/deletion` | Deletion modal step 3 [Confirm] |
| Cancel deletion | BR-DATA-022: Cancel within cooling-off | `POST /api/privacy/deletion/{id}/cancel` | [Cancel Deletion] confirmed |
| View tenant audit log | BR-DATA-013: Audit logging | `GET /api/privacy/audit-log` | `/privacy/audit-log` page load |
| Filter audit log | Filter + re-fetch | `GET /api/privacy/audit-log?...` | Filter change |
| Load filter categories | Populate dropdown | `GET /api/privacy/audit-log/categories` | Audit log page mount |
| Expand audit entry | (Client-side — toggle detail) | — | Row click |
| Copy JSON | (Client-side — clipboard) | — | [Copy JSON] click |

### Admin Data Actions

| Frontend Action | Backend Use Case | Endpoint | Triggers |
|----------------|-----------------|----------|----------|
| View admin audit logs | BR-DATA-013: Cross-tenant audit | `GET /api/admin/audit-logs` | `/admin/audit-logs` page load |
| Filter admin audit logs | Filter + re-fetch | `GET /api/admin/audit-logs?...` | Filter change |
| Export CSV | Export filtered log | `GET /api/admin/audit-logs/export?...` | [Export CSV] click |
| View backups | BR-DATA-016: Backup status | `GET /api/admin/data/backups` | `/admin/data` Backups tab |
| Trigger manual backup | BR-DATA-016: Manual backup | `POST /api/admin/data/backups/trigger` | [Trigger Manual Backup] click |
| View migrations | BR-DATA-017: Migration status | `GET /api/admin/data/migrations` | `/admin/data` Migrations tab |
| Run integrity check | BR-DATA-017: Data integrity | `POST /api/admin/data/migrations/integrity-check` | [Run Integrity Check] click |

### System-Triggered UI Updates

| Backend Event | Frontend Effect | How Applied |
|--------------|----------------|-------------|
| Export processing complete | Status changes to READY, files available | Polling detects status change |
| Export expires (24h) | Status changes to EXPIRED, downloads disabled | Next status fetch or page revisit |
| Deletion cooling-off ends | Countdown reaches 0, cancel disabled | Client-side timer or next page visit |
| Deletion processing complete | Status changes to COMPLETED, session ended | Server-side (user logged out) |
| New audit log entry created | Visible on next fetch/filter | Not real-time; visible on page load/filter |
| Backup completes | Status changes from in_progress | Auto-refresh detects change |
| Integrity check finds issues | Results displayed with warnings/errors | Response from POST call |

---

## State Management Notes

### What State to Track

| State | Scope | Persistence |
|-------|-------|-------------|
| Data inventory | Privacy center | Memory. Fetch on page load. |
| Export status | Privacy center | Memory. Poll during PROCESSING. Fetch on page load. |
| Deletion status | Privacy center | Memory. Fetch on page load. |
| Deletion form | Modal | Component state. Cleared on close. |
| Audit log entries | Audit log page | Memory. Fetch on filter/page change. |
| Audit log filters | Audit log page | URL query params + component state. |
| Expanded entry ID | Audit log page | Component state. Cleared on navigation. |
| Admin audit filters | Admin audit log page | URL query params + component state. |
| CSV export progress | Admin audit log page | Component state (boolean). |
| Backup status | Admin data page | Memory. Fetch on tab load. Auto-refresh during in_progress. |
| Migration status | Admin data page | Memory. Fetch on tab load. |
| Integrity check results | Admin data page | Memory. Stored until tab change. |
| Active tab | Admin data page | URL hash or component state. |

### Reset Behavior

| Event | State Reset |
|-------|-------------|
| Privacy center loaded | Fetch fresh inventory, export, deletion status (3 parallel calls) |
| Export requested | Clear error, set requesting=true, start polling on success |
| Export polling stopped | Clear polling timer (page unmount or status terminal) |
| Deletion modal opened | Initialize form: step 1, empty fields |
| Deletion modal closed | Clear entire form state |
| Deletion confirmed | Close modal, refetch deletion status |
| Audit log page loaded | Fetch categories + first page with default filters |
| Filter changed | Reset page to 1, fetch with new filters |
| Audit entry expanded | Collapse previously expanded entry |
| Admin data tab changed | Fetch data for new tab if not already loaded |
| Backup triggered | Optimistic: add in_progress row, start auto-refresh |
| Integrity check started | Clear previous results, show loading |
| Logout | Clear all state |
