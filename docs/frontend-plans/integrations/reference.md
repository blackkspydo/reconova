# Reference (Integrations)

Scope: Error handling matrix, input validation rules, security considerations, and frontend-to-backend action mapping.

---

## Error Handling Matrix

### Authorization Errors

| Error Code | HTTP | User Message | UI Action |
|-----------|------|-------------|-----------|
| `ERR_INT_001` | 403 | "You don't have permission to manage integrations." | Toast (error). Redirect to dashboard. |
| `ERR_INT_002` | 403 | "This integration does not belong to your tenant." | Toast (error). Refresh integration list. |
| `ERR_INT_015` | 403 | "You don't have permission to manage platform API keys." | Toast (error). Redirect to admin dashboard. |

### Integration CRUD Errors

| Error Code | HTTP | User Message | UI Action |
|-----------|------|-------------|-----------|
| `ERR_INT_003` | 400 | "Integration limit reached for this channel on your plan." | Toast (error). Show [Upgrade Plan] link. Close modal. |
| `ERR_INT_004` | 404 | "Integration not found." | Toast (error). Close detail. Refresh list. |
| `ERR_INT_005` | 409 | "An integration with this name already exists." | Inline error on name field. Keep form open. |
| `ERR_INT_006` | 400 | "Invalid configuration for this channel type." | Toast (error). Highlight invalid fields. Keep form open. |

### Notification Rule Errors

| Error Code | HTTP | User Message | UI Action |
|-----------|------|-------------|-----------|
| `ERR_INT_007` | 400 | "A rule for this event type already exists on this integration." | Inline error on event_type dropdown. Keep form open. |
| `ERR_INT_008` | 404 | "Notification rule not found." | Toast (error). Refresh rules list. |

### Delivery & Test Errors

| Error Code | HTTP | User Message | UI Action |
|-----------|------|-------------|-----------|
| `ERR_INT_009` | 400 | "Integration must be enabled before testing." | Toast (warning). No test triggered. |
| `ERR_INT_010` | 502 | "Test delivery failed: {provider_error}" | Toast (error). Show error detail in test result area. |
| `ERR_INT_011` | 503 | "Notification delivery is temporarily unavailable." | Toast (error). Show operational banner. |

### API Key Errors (Admin)

| Error Code | HTTP | User Message | UI Action |
|-----------|------|-------------|-----------|
| `ERR_INT_012` | 409 | "An API key with this name already exists for this provider." | Inline error on key_name field. Keep modal open. |
| `ERR_INT_013` | 400 | "Invalid API key format." | Inline error on api_key field. Keep modal open. |
| `ERR_INT_014` | 404 | "API key not found." | Toast (error). Refresh key list. |
| `ERR_INT_016` | 400 | "Cannot delete an active API key. Disable it first." | Toast (error). Close delete dialog. |

### Network & System Errors

| Scenario | User Message | UI Action |
|----------|-------------|-----------|
| Network timeout | "Unable to connect. Check your connection and try again." | Toast (error) + [Retry] button. |
| 500 Internal Server Error | "Something went wrong. Please try again." | Toast (error) + [Retry] button. |
| Rate limited (429) | "Too many requests. Please wait a moment." | Toast (warning). Auto-retry after backoff. |

### Error Response Parsing

```typescript
function handleIntegrationError(error: ApiError): void {
  const { code, message, details } = error.error;

  switch (code) {
    case 'ERR_INT_005':
      // Duplicate integration name — inline field error
      setFieldError('name', message);
      break;

    case 'ERR_INT_007':
      // Duplicate rule event type — inline error
      setFieldError('event_type', message);
      break;

    case 'ERR_INT_012':
      // Duplicate API key name — inline error
      setFieldError('key_name', message);
      break;

    case 'ERR_INT_013':
      // Invalid API key format — inline error
      setFieldError('api_key', message);
      break;

    case 'ERR_INT_006':
      // Invalid config — highlight fields from details
      if (details?.fields) {
        details.fields.forEach((f: { field: string; message: string }) =>
          setFieldError(f.field, f.message)
        );
      } else {
        showToast('error', message);
      }
      break;

    case 'ERR_INT_003':
      // Tier limit reached
      showToast('error', message);
      showUpgradeLink('/settings/billing/plans');
      closeModal();
      break;

    case 'ERR_INT_009':
      // Integration disabled — warning, no action
      showToast('warning', message);
      break;

    case 'ERR_INT_010':
      // Test delivery failed — show provider error
      showTestResult({ success: false, message });
      break;

    case 'ERR_INT_011':
      // Service unavailable — operational banner
      showToast('error', message);
      showOperationalBanner();
      break;

    case 'ERR_INT_016':
      // Cannot delete active key
      showToast('error', message);
      closeModal();
      break;

    case 'ERR_INT_004':
    case 'ERR_INT_008':
    case 'ERR_INT_014':
      // Stale data — close detail/modal, refresh
      showToast('error', message);
      closeModal();
      refetchData();
      break;

    case 'ERR_INT_001':
    case 'ERR_INT_002':
      // Permission denied — tenant
      showToast('error', message);
      navigateTo('/dashboard');
      break;

    case 'ERR_INT_015':
      // Permission denied — admin
      showToast('error', message);
      navigateTo('/admin');
      break;

    default:
      showToast('error', message);
  }
}
```

---

## Input Validation Rules

### Integration Name

| Field | Type | Constraints | Error Message |
|-------|------|-------------|---------------|
| `name` | string | Required. Max 100 chars. Unique per tenant. | "Name is required." / "Name must be under 100 characters." / "An integration with this name already exists." |

### Email Configuration

| Field | Type | Constraints | Error Message |
|-------|------|-------------|---------------|
| `recipients` | string[] | Required. Min 1, max 10. Each must be valid email. | "At least one recipient is required." / "Maximum 10 recipients allowed." / "Invalid email address: {value}." |
| `reply_to` | string | Optional. Valid email if provided. | "Invalid email address." |

### Slack Configuration

| Field | Type | Constraints | Error Message |
|-------|------|-------------|---------------|
| `webhook_url` | string | Required. Must match `https://hooks.slack.com/services/*` pattern. | "Webhook URL is required." / "Must be a valid Slack webhook URL." |
| `mention_on_critical` | boolean | Optional. Default false. | — |

### Jira Configuration

| Field | Type | Constraints | Error Message |
|-------|------|-------------|---------------|
| `instance_url` | string | Required. Valid HTTPS URL. | "Instance URL is required." / "Must be a valid HTTPS URL." |
| `project_key` | string | Required. 2–10 uppercase alphanumeric chars. | "Project key is required." / "Project key must be 2–10 uppercase letters/numbers." |
| `issue_type` | string | Required. One of: Bug, Task, Story. | "Issue type is required." |
| `api_token` | string | Required on create. Max 500 chars. | "API token is required." |
| `assignee_email` | string | Optional. Valid email if provided. | "Invalid email address." |

### Webhook Configuration

| Field | Type | Constraints | Error Message |
|-------|------|-------------|---------------|
| `url` | string | Required. Valid HTTPS URL. Max 500 chars. | "URL is required." / "Must be a valid HTTPS URL." |
| `headers` | Record | Optional. Max 10 entries. Key max 100 chars, value max 500 chars. | "Maximum 10 custom headers allowed." / "Header name must be under 100 characters." |

### SIEM Configuration

| Field | Type | Constraints | Error Message |
|-------|------|-------------|---------------|
| `endpoint_url` | string | Required. Valid HTTPS URL. Max 500 chars. | "Endpoint URL is required." / "Must be a valid HTTPS URL." |
| `format` | string | Required. One of: CEF, LEEF, JSON. | "Format is required." |
| `api_key` | string | Optional. Max 500 chars. | — |
| `include_raw_cve` | boolean | Optional. Default false. | — |

### Notification Rule

| Field | Type | Constraints | Error Message |
|-------|------|-------------|---------------|
| `event_type` | enum | Required. One of 7 event types. Unique per integration. | "Event type is required." / "A rule for this event type already exists." |
| `severity_filter` | enum[] | Required for vulnerability/compliance events. At least one selected. | "Select at least one severity level." |
| `enabled` | boolean | Required. Default true. | — |

### API Key (Admin)

| Field | Type | Constraints | Error Message |
|-------|------|-------------|---------------|
| `provider` | string | Required. Known provider name. | "Provider is required." |
| `key_name` | string | Required. Max 100 chars. Unique per provider. | "Key name is required." / "Key name must be under 100 characters." |
| `api_key` | string | Required. Max 500 chars. | "API key is required." |
| `quota_limit` | integer | Required. Min 1. | "Quota limit is required." / "Quota limit must be at least 1." |
| `rate_limit_rpm` | integer | Required. Min 1, max 10000. | "Rate limit is required." / "Rate limit must be between 1 and 10,000 requests per minute." |

### Provider Config (Admin)

| Field | Type | Constraints | Error Message |
|-------|------|-------------|---------------|
| `default_rate_limit_rpm` | integer | Required. Min 1, max 10000. | "Rate limit must be between 1 and 10,000." |
| `circuit_breaker_threshold` | integer | Required. Min 1, max 100. | "Threshold must be between 1 and 100 consecutive failures." |

### Client-Side Validation Timing

| Trigger | Behavior |
|---------|----------|
| Integration name blur | Validate required + max length |
| Email recipient add | Validate email format before adding to list |
| URL fields blur | Validate URL format (HTTPS required for Jira/Webhook/SIEM) |
| Jira project key blur | Validate uppercase alphanumeric pattern |
| Rule event type select | Check uniqueness against existing rules for this integration |
| API key form submit | Validate all fields, focus first invalid |
| Provider config inline edit blur | Validate range constraints |
| Slack webhook URL paste | Validate pattern match immediately |

---

## Security Considerations

### Secure Storage

| Data | Storage | Justification |
|------|---------|---------------|
| Integration list | Memory (state) | Re-fetched per page visit |
| Integration config | Memory (state) | Fetched per detail expansion |
| Notification rules | Memory (state) | Fetched per rules tab |
| Delivery history | Memory (state) | Paginated, not cached |
| API tokens / secrets | Never stored client-side | API returns `has_*: boolean` only. Secrets never sent to frontend. |
| Webhook secret (revealed) | Component state | Auto-cleared after 30 seconds. Not persisted. |
| Admin API keys | Never stored client-side | API returns metadata only. Key values never sent to frontend. |
| Admin usage data | Memory (state) | Re-fetched per dashboard load |

### API Authorization

- All `/api/integrations/*` endpoints require authentication
- Integration CRUD requires TENANT_OWNER role
- Notification rule management requires TENANT_OWNER role
- TENANT_MEMBER can view integrations (read-only, no status changes)
- All `/api/admin/integrations/*` endpoints require SUPER_ADMIN role
- Frontend role checks are UX convenience; API is authoritative
- Webhook secrets are returned only via dedicated reveal endpoint, rate-limited to 3 reveals per minute

### Rate Limiting Awareness

| Action | Rate Limit | Frontend Handling |
|--------|-----------|-------------------|
| Integration CRUD | Backend enforced | Disable submit during API call |
| Test connection | 5 per minute per integration | Disable [Test] during call, show cooldown if 429 |
| Notification rule CRUD | Backend enforced | Disable submit during API call |
| Webhook secret reveal | 3 per minute | Disable [Reveal] during call, show cooldown if 429 |
| Webhook secret regenerate | 1 per 5 minutes | Disable [Regenerate] during call, show cooldown if 429 |
| Admin key CRUD | Backend enforced | Disable submit during API call |
| Admin usage dashboard | Backend enforced | Disable time range buttons during fetch |

### Button Debouncing

| Action | Debounce Strategy |
|--------|-------------------|
| Create integration | Disable [Create Integration] → re-enable on response or error |
| Save integration config | Disable [Save] → re-enable on response |
| Delete integration | Disable [Delete] → re-enable on response |
| Test connection | Disable [Test Connection] → show spinner → re-enable on response |
| Enable/disable toggle | Disable toggle → re-enable on response |
| Add notification rule | Disable [Add Rule] → re-enable on response |
| Delete notification rule | Disable [Remove] → re-enable on response |
| Reveal webhook secret | Disable [Reveal] → show secret → auto-hide after 30s |
| Regenerate webhook secret | Disable [Regenerate] → show new secret → re-enable after display |
| Add API key | Disable [Add API Key] → re-enable on response |
| Rotate API key | Disable [Rotate] → re-enable on response |
| Disable/enable API key | Disable toggle → re-enable on response |
| Reset quota | Disable [Reset Quota] → re-enable on response |

### Input Sanitization

| Input | Sanitization |
|-------|-------------|
| Integration name | Trim whitespace. No HTML tags. Max 100 chars. |
| Email recipients | Trim whitespace. Validate email regex. Max 10 entries. |
| Slack webhook URL | Trim whitespace. Validate Slack webhook pattern. |
| Jira instance URL | Trim whitespace. Validate HTTPS URL. |
| Jira project key | Trim whitespace. Uppercase. Validate alphanumeric pattern. |
| Webhook URL | Trim whitespace. Validate HTTPS URL. Max 500 chars. |
| Webhook headers | Trim keys and values. No HTML tags. Max 10 pairs. |
| SIEM endpoint URL | Trim whitespace. Validate HTTPS URL. Max 500 chars. |
| API key name (admin) | Trim whitespace. No HTML tags. Max 100 chars. |
| API key value (admin) | No trimming (keys may have leading/trailing chars). Max 500 chars. |
| Quota/rate limit numbers | Parse as integer. Reject non-numeric. Validate range. |

---

## Key Actions → Backend Use Cases Mapping

### Tenant Owner Actions

| Frontend Action | Backend Use Case | Endpoint | Triggers |
|----------------|-----------------|----------|----------|
| View integrations | Load list + tier limits | `GET /api/integrations` | `/settings/integrations` page load |
| Add integration | BR-INT-006: Create integration | `POST /api/integrations` | [Create Integration] submit |
| Edit integration | BR-INT-006: Update integration | `PUT /api/integrations/{id}` | [Save] in config tab |
| Delete integration | BR-INT-006: Remove integration | `DELETE /api/integrations/{id}` | [Delete] confirmed |
| Enable/disable integration | BR-INT-006: Toggle integration | `PUT /api/integrations/{id}/toggle` | Toggle click |
| Test connection | BR-INT-009: Test delivery | `POST /api/integrations/{id}/test` | [Test Connection] click |
| View notification rules | Load rules for integration | `GET /api/integrations/{id}/rules` | Rules tab click |
| Add notification rule | BR-INT-007: Create rule | `POST /api/integrations/{id}/rules` | [Add Rule] submit |
| Edit notification rule | BR-INT-007: Update rule | `PUT /api/integrations/{id}/rules/{ruleId}` | Rule toggle or severity change |
| Delete notification rule | BR-INT-007: Remove rule | `DELETE /api/integrations/{id}/rules/{ruleId}` | [Remove] confirmed |
| View delivery history | Load history for integration | `GET /api/integrations/{id}/history` | History tab click |
| Reveal webhook secret | BR-INT-010: Fetch secret | `GET /api/integrations/{id}/webhook-secret` | [Reveal] click |
| Regenerate webhook secret | BR-INT-010: Regenerate secret | `POST /api/integrations/{id}/regenerate-secret` | [Regenerate] confirmed |

### Super Admin Actions

| Frontend Action | Backend Use Case | Endpoint | Triggers |
|----------------|-----------------|----------|----------|
| View API keys | List platform API keys | `GET /api/admin/integrations/keys` | API Keys tab load |
| Add API key | BR-INT-001: Register key | `POST /api/admin/integrations/keys` | [Add API Key] submit |
| Rotate API key | BR-INT-001: Rotate key | `POST /api/admin/integrations/keys/{id}/rotate` | [Rotate] submit |
| Disable/enable key | BR-INT-001: Toggle key | `PUT /api/admin/integrations/keys/{id}/toggle` | Toggle click |
| Delete API key | BR-INT-001: Remove key | `DELETE /api/admin/integrations/keys/{id}` | [Delete] confirmed |
| Reset key quota | BR-INT-004: Reset quota | `POST /api/admin/integrations/keys/{id}/reset-quota` | [Reset Quota] confirmed |
| View usage dashboard | BR-INT-004: Load usage | `GET /api/admin/integrations/usage` | Usage tab load |
| Change usage time range | BR-INT-004: Filter usage | `GET /api/admin/integrations/usage?range=` | Time range button click |
| View provider config | BR-INT-003: Load providers | `GET /api/admin/integrations/providers` | Provider Config tab load |
| Edit provider config | BR-INT-003: Update provider | `PUT /api/admin/integrations/providers/{id}` | [Save] inline edit |

### System-Triggered UI Updates

| Backend Event | Frontend Effect | How Applied |
|--------------|----------------|-------------|
| Notification delivered | History entry added with DELIVERED status | Next history tab load |
| Notification failed (all retries) | History entry shows FAILED, integration status → ERROR | Next list load shows error badge |
| Integration auto-disabled (tier downgrade) | Integration shows DISABLED status + downgrade banner | Feature store refresh on plan change |
| API key quota exhausted | Key status → QUOTA_EXCEEDED, quota bar red | Next admin page load |
| API key rate limited | Key status → RATE_LIMITED (auto-recovers) | Next admin page load |
| Provider circuit breaker tripped | Provider shows disabled state in config | Next admin page load |

---

## State Management Notes

### What State to Track

| State | Scope | Persistence |
|-------|-------|-------------|
| Integration list | Integrations page | Memory. Fetch on page load. |
| Tier limits | Integrations page | Memory. Fetch with list. |
| Expanded integration ID | Integrations page | Component state. One at a time. |
| Active detail tab | Integration detail | Component state. Default: config. |
| Integration form (add/edit) | Modal / detail | Component state. Cleared on submit/cancel. |
| Notification rules | Rules tab per integration | Memory. Fetch on tab open. |
| Rule form | Rules tab | Component state. Cleared on submit/cancel. |
| Delivery history | History tab per integration | Memory. Paginated, fetch on tab open. |
| Webhook secret (revealed) | Component state | Auto-cleared after 30 seconds. |
| Admin active tab | Admin page | Component state. Default: keys. |
| Admin API keys | Keys tab | Memory. Refresh on CRUD. |
| Admin key form | Modal | Component state. Cleared on submit/cancel. |
| Admin usage data | Usage tab | Memory. Refresh on time range change. |
| Admin usage time range | Usage tab | Component state. Default: 24h. |
| Admin provider configs | Provider tab | Memory. Refresh on edit. |
| Admin provider form | Inline edit | Component state. Cleared on save/cancel. |

### Reset Behavior

| Event | State Reset |
|-------|-------------|
| Integration created/edited/deleted | Refresh integration list + limits |
| Integration toggled | Optimistic update, rollback on error |
| Test connection completed | Update last_test_at, last_test_success in list |
| Rule created/edited/deleted | Refresh rules for that integration |
| History tab opened | Fetch page 1 |
| Webhook secret revealed | Set 30-second auto-clear timer |
| Webhook secret regenerated | Show new secret, update has_secret flag |
| API key created/rotated/deleted | Refresh key list |
| API key toggled/quota reset | Refresh key list |
| Usage time range changed | Fetch new usage data |
| Provider config saved | Refresh provider list |
| Tab change (admin) | Keep previous tab data cached, fetch if stale |
| Navigation away from integrations | Clear expanded state, form state |
| Navigation away from admin | Clear all admin page-level state |
| Logout | Clear all integration state |

### Unsaved Changes Guard

| Page | Guard Behavior |
|------|---------------|
| Integration config edit tab | If form is dirty and user collapses row or navigates: "Discard changes?" dialog |
| Add integration modal | If form has values and user clicks outside or ✕: "Discard changes?" dialog |
| Admin API key modal | If form has values and user closes: "Discard changes?" dialog |
| Admin provider inline edit | If field changed and user clicks away: auto-save or "Discard?" |
| All other integration pages | No guard needed |
