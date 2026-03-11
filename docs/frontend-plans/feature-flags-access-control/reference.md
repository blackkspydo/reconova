# Reference (Feature Flags & Access Control)

Scope: Error handling matrix, input validation rules, security considerations, and frontend-to-backend action mapping.

---

## Error Handling Matrix

### Authorization Errors

| Error Code | HTTP | User Message | UI Action |
|-----------|------|-------------|-----------|
| `ERR_FLAG_003` | 403 | "Upgrade to {tier} to access {feature}." | Open `<UpgradeModal />` with feature details and [View Plans] CTA. |
| `ERR_FLAG_004` | 403 | "This feature is temporarily unavailable." | Show `<OperationalBanner />`. No CTA (not user-resolvable). |
| `ERR_FLAG_005` | 403 | "You don't have permission to manage feature flags." | Toast (error). Redirect to dashboard. |

### Feature Flag Errors

| Error Code | HTTP | User Message | UI Action |
|-----------|------|-------------|-----------|
| `ERR_FLAG_001` | 404 | "Feature flag not found." | Toast (error). Refresh feature list. |
| `ERR_FLAG_002` | 400 | "Invalid flag type." | Toast (error). Should not occur in normal UI flow. |
| `ERR_FLAG_011` | 400 | "Feature flag name already exists." | Toast (error). Admin flag management only. |
| `ERR_FLAG_012` | 400 | "Cannot delete flag with active overrides." | Toast (error). Show override count. |

### Override Errors

| Error Code | HTTP | User Message | UI Action |
|-----------|------|-------------|-----------|
| `ERR_FLAG_006` | 400 | "Reason must be at least 10 characters." | Inline error on reason textarea. Keep modal open. |
| `ERR_FLAG_007` | 400 | "Operational flags cannot have tenant overrides." | Toast (error). Close modal. Should not occur (UI filters operational flags from override options). |
| `ERR_FLAG_008` | 404 | "Tenant not found." | Toast (error). Close modal. Clear tenant selection. |
| `ERR_FLAG_009` | 409 | "An override already exists for this feature." | Toast (error). Close create modal. Refresh table (show existing override). |
| `ERR_FLAG_010` | 404 | "Override not found." | Toast (error). Close modal. Refresh table (override was already deleted). |

### Network & System Errors

| Scenario | User Message | UI Action |
|----------|-------------|-----------|
| Network timeout | "Unable to connect. Check your connection and try again." | Toast (error) + [Retry] button. |
| 500 Internal Server Error | "Something went wrong. Please try again." | Toast (error) + [Retry] button. |
| Feature store fetch failure | (no visible message) | All features default to locked (fail-safe). Retry on next navigation. |
| Admin API failure | "Unable to load feature management data." | Error state with [Retry] button. |

### Error Response Parsing

```typescript
function handleFeatureFlagError(error: ApiError): void {
  const { code, message, details } = error.error;

  switch (code) {
    case 'ERR_FLAG_003':
      // Upgrade required — show modal with feature details
      openUpgradeModal({
        featureName: details?.feature as string,
        requiredTier: details?.required_tier as string,
        currentTier: details?.current_tier as string,
      });
      break;

    case 'ERR_FLAG_004':
      // Operational flag disabled — show banner, no action
      showToast('warning', message);
      break;

    case 'ERR_FLAG_006':
      // Inline validation — keep form open
      setFieldError('reason', message);
      break;

    case 'ERR_FLAG_009':
      // Duplicate override — refresh to show existing
      showToast('error', message);
      closeModal();
      refetchTenantFeatures();
      break;

    case 'ERR_FLAG_008':
    case 'ERR_FLAG_010':
      // Stale data — close modal and refresh
      showToast('error', message);
      closeModal();
      refetchTenantFeatures();
      break;

    case 'ERR_FLAG_005':
      // Permission denied — redirect
      showToast('error', message);
      navigateTo('/dashboard');
      break;

    default:
      showToast('error', message);
  }
}
```

---

## Input Validation Rules

### Override Creation / Edit

| Field | Type | Constraints | Error Message |
|-------|------|-------------|---------------|
| `enabled` | boolean | Required. | "Please select Enable or Disable." |
| `reason` | string | Required. Min 10 chars. Max 500 chars. | "Reason must be at least 10 characters." / "Reason must be 500 characters or fewer." |

### Admin Tenant Search

| Field | Type | Constraints | Error Message |
|-------|------|-------------|---------------|
| `query` | string | Min 2 chars to trigger search. Max 100 chars. | "Enter at least 2 characters to search." |

### Admin Operational Flag Toggle

| Field | Type | Constraints | Error Message |
|-------|------|-------------|---------------|
| `enabled` | boolean | Required. Must differ from current state. | N/A (toggle only changes state) |

### Client-Side Validation Timing

| Trigger | Behavior |
|---------|----------|
| Reason textarea blur | Validate length, show inline error if < 10 chars |
| Reason textarea input | Live character count display (`{N} / 500`) |
| Form submit | Validate all fields, focus first invalid field |
| Tenant search input | Debounce 300ms, only search if ≥ 2 chars |
| Toggle click | No validation — opens confirmation modal |

---

## Security Considerations

### Secure Storage

| Data | Storage | Justification |
|------|---------|---------------|
| Feature store (feature map) | Memory (state) | Fetched per session, never persisted to disk |
| Admin flag data | Memory (state) | Fetched per page visit, not cached |
| Override form values | Component state | Discarded on modal close |
| Tenant search results | Component state | Discarded on page leave |

### No Client-Side-Only Gating (Critical)

**The frontend feature store is for UI rendering only.** It determines what to show/hide but does NOT enforce access control. The API is authoritative:

- Backend middleware checks feature flags on every API call (BR-FLAG-006)
- If a user bypasses frontend gating (e.g., direct API call), the backend returns `ERR_FLAG_003` or `ERR_FLAG_004`
- Frontend gating is a UX convenience, not a security boundary
- Never trust client-side feature state for authorization decisions

### Rate Limiting Awareness

| Action | Rate Limit | Frontend Handling |
|--------|-----------|-------------------|
| Feature store fetch (`GET /api/features`) | Soft cache (per session) | Don't re-fetch within same page view unless triggered |
| Operational flag toggle | Backend enforced | Disable toggle during API call, re-enable on response |
| Override CRUD | Backend enforced | Disable submit button during API call |
| Tenant search | Debounce 300ms | Only fire API call after 300ms of no typing |

### Button Debouncing

| Action | Debounce Strategy |
|--------|-------------------|
| Operational flag toggle | Disable toggle on click → open modal → disable [Confirm] during API → re-enable on response |
| Create override | Disable [Create Override] on click → re-enable on API response or error |
| Edit override | Disable [Save Changes] on click → re-enable on API response or error |
| Delete override | Disable [Remove Override] on click → re-enable on API response or error |
| Tenant search | Debounce input 300ms before API call |

### Input Sanitization

| Input | Sanitization |
|-------|-------------|
| Override reason | Trim whitespace. No HTML tags. Max 500 chars. |
| Tenant search query | Trim whitespace. No HTML tags. Max 100 chars. |
| Feature names (display) | Read-only from API. Escape for rendering (XSS prevention). |

---

## Key Actions → Backend Use Cases Mapping

### Tenant Owner Actions

| Frontend Action | Backend Use Case | Endpoint | Triggers |
|----------------|-----------------|----------|----------|
| View feature availability | BR-FLAG-007: Feature Visibility API | `GET /api/features` | App init, plan change, page refresh |
| View Plan & Features page | BR-FLAG-007: Feature Visibility API | `GET /api/features` (cached) | `/settings/features` page load |
| Click locked feature | BR-FLAG-003: ERR_FLAG_003 (frontend-only, no API call) | — | Locked item click → Upgrade Modal |
| Navigate to upgrade | BR-BILL-004 (billing plan) | — | [View Plans] CTA → `/settings/billing/plans` |

### Super Admin Actions

| Frontend Action | Backend Use Case | Endpoint | Triggers |
|----------------|-----------------|----------|----------|
| View all flags | List flags | `GET /api/admin/features` | `/admin/features` page load |
| View operational flags | List operational flags | `GET /api/admin/features/operational` | Operational tab selected |
| Toggle operational flag | BR-FLAG-002: Operational Flag Toggle | `PUT /api/admin/features/operational/{name}` | [Confirm] in toggle modal |
| Search tenants | List tenants | `GET /api/admin/tenants?q=` | Tenant search input (debounced) |
| View tenant features | BR-FLAG-007: Feature Visibility (admin) | `GET /api/admin/tenants/{id}/features` | Tenant selected |
| Create override | BR-FLAG-003: Create Override | `POST /api/admin/tenants/{tid}/features/{fid}/override` | [Create Override] click |
| Edit override | BR-FLAG-003: Update Override | `PUT /api/admin/tenants/{tid}/features/{fid}/override` | [Save Changes] click |
| Delete override | BR-FLAG-003: Delete Override | `DELETE /api/admin/tenants/{tid}/features/{fid}/override` | [Remove Override] click |

### System-Triggered UI Updates

| Backend Event | Frontend Effect | How Applied |
|--------------|----------------|-------------|
| Plan upgrade/downgrade | Feature store refreshes → gating updates | Feature store re-fetch triggered by plan change event |
| Operational flag toggled (by another admin) | Stale feature store until next refresh | Next `GET /api/features` call reflects new state (30-min cache TTL server-side) |
| Override created/updated/deleted | Tenant feature state changes | Admin: immediate table refresh. Tenant: next feature store refresh |
| Tenant suspension/deactivation | All features effectively disabled | Next API call returns auth error → redirect to blocked state |

---

## State Management Notes

### What State to Track

| State | Scope | Persistence |
|-------|-------|-------------|
| Feature store (all features map) | Global (used everywhere) | Memory. Fetch on app init + plan change triggers. |
| Upgrade modal state | Global (can be triggered anywhere) | Component state. Closed = cleared. |
| Features page view | `/settings/features` page | Component state. Derived from feature store. |
| Admin active tab | `/admin/features` page | Component state. Default: `subscription`. |
| Admin module filter | Admin subscription flags tab | Component state. Reset on tab change. |
| Admin search query | Admin subscription flags tab | Component state. Reset on tab change. |
| Admin selected tenant | Admin overrides tab | Component state. Reset on page leave. |
| Admin tenant features | Admin overrides tab | Component state. Refresh on override CRUD. |
| Override form values | Create/edit modal | Component state. Discarded on close. |
| Operational toggle target | Toggle confirmation modal | Component state. Cleared on close. |

### Reset Behavior

| Event | State Reset |
|-------|-------------|
| Plan change (upgrade/downgrade) | Re-fetch feature store. Sidebar gating updates. |
| Stripe checkout return | Re-fetch feature store (plan may have changed). |
| Operational flag toggled (admin) | Re-fetch admin operational flags. Note: tenant feature stores refresh on their next fetch. |
| Override created/updated/deleted (admin) | Re-fetch tenant feature table. Clear form/modal. |
| Tenant search cleared (admin) | Clear tenant features table. Show empty state. |
| Tab change (admin) | Reset filter/search for previous tab. |
| Navigation away from admin features | Clear all admin page-level state. |
| Logout | Clear feature store + all page state. |

### Unsaved Changes Guard

| Page | Guard Behavior |
|------|---------------|
| Override create/edit modal | If form has changes and user clicks outside or ✕: "Discard changes?" confirm dialog. |
| All other feature flag pages | No guard needed (no local edits to lose). |
