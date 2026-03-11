# Reference (Tenant Management)

Scope: Error handling matrix, input validation rules, security considerations, and backend use case mappings for tenant management.

---

## Error Handling Matrix

### Tenant Status Errors

| Error Code | HTTP | User Message | UI Action |
|-----------|------|-------------|-----------|
| `ERR_TNT_003` | 401 | Tenant context could not be resolved | Clear auth state -> redirect to `/auth/login` |
| `ERR_TNT_005` | 403 | Account is suspended. Contact support. | Clear auth state -> redirect to `/auth/suspended` |
| `ERR_TNT_006` | 403 | Account has been deactivated. | Clear auth state -> redirect to `/auth/deactivated` |
| `ERR_TNT_007` | 403 | Account is being set up. Please wait. | Redirect to `/auth/provisioning` |

### Tenant Creation Errors

| Error Code | HTTP | User Message | UI Action |
|-----------|------|-------------|-----------|
| `ERR_TNT_001` | 409 | Tenant name is unavailable | Inline error on registration form: "This organization name is taken" |
| `ERR_TNT_002` | 400 | Tenant name is invalid | Inline error on registration form: "Enter a valid organization name" |

### Free Tier Enforcement Errors

| Error Code | HTTP | User Message | UI Action |
|-----------|------|-------------|-----------|
| `ERR_TNT_008` | 403 | Upgrade required to run scans | Toast: "Upgrade your plan to run scans" (should not normally occur -- button disabled) |
| `ERR_TNT_009` | 403 | Upgrade required to add domains | Toast: "Upgrade your plan to add domains" (should not normally occur -- form disabled) |
| `ERR_TNT_010` | 403 | Upgrade required to generate compliance reports | Toast: "Upgrade your plan for compliance reports" |
| `ERR_TNT_011` | 403 | Upgrade required to manage scan schedules | Toast: "Upgrade your plan to schedule scans" |
| `ERR_TNT_012` | 403 | Upgrade required to manage integrations | Toast: "Upgrade your plan for integrations" |

### State Transition Errors

| Error Code | HTTP | User Message | UI Action |
|-----------|------|-------------|-----------|
| `ERR_TNT_013` | 409 | Tenant is not in a valid state for this action | Toast: "This action is not available for this tenant's current status" + refresh tenant detail |
| `ERR_TNT_014` | 409 | Tenant is not suspended | Toast: "Tenant is not in a suspended state" + refresh tenant detail |
| `ERR_TNT_015` | 409 | Tenant cannot be deleted in its current state | Toast: "Account cannot be deleted in its current state" |
| `ERR_TNT_016` | 409 | Deletion request already pending | Toast: "A deletion request is already pending" + show pending state in UI |
| `ERR_TNT_017` | 400 | No deletion request pending for this tenant | Toast: "No pending deletion request found" + refresh tenant detail |

### Impersonation Errors

| Error Code | HTTP | User Message | UI Action |
|-----------|------|-------------|-----------|
| `ERR_TNT_018` | 403 | Insufficient permissions for impersonation | Toast: "You do not have permission to impersonate" |
| `ERR_TNT_019` | 400 | Cannot impersonate users in inactive tenants | Toast: "Can only impersonate users in active tenants" |

### Domain Errors

| Error Code | HTTP | User Message | UI Action |
|-----------|------|-------------|-----------|
| Domain duplicate | 409 | Domain already exists | Inline error: "This domain already exists" |
| Max domains | 403 | Domain limit reached | Inline error: "Domain limit reached ({count}/{max}). Upgrade for more." |

### Network / Generic Errors

| Condition | User Message | UI Action |
|-----------|-------------|-----------|
| Network timeout | Failed to connect. Please check your internet connection. | Toast + retry button |
| 500 Internal Server Error | Something went wrong. Please try again. | Toast + retry button |
| 429 Too Many Requests | Too many requests. Please wait a moment. | Toast, auto-retry after `Retry-After` header |

---

## Input Validation Rules

### Tenant Name (Organization Name)

| Field | Type | Constraints | Error Message |
|-------|------|------------|---------------|
| `name` | string | Required | "Organization name is required" |
| `name` | string | Min 1 char (after trim) | "Organization name is required" |
| `name` | string | Max 200 chars | "Maximum 200 characters" |

### Domain Name

| Field | Type | Constraints | Error Message |
|-------|------|------------|---------------|
| `domain` | string | Required | "Domain name is required" |
| `domain` | string | Valid domain format (regex) | "Enter a valid domain (e.g., example.com)" |
| `domain` | string | No protocol prefix (strip `https://`) | Auto-strip, no error |
| `domain` | string | No path/query (strip everything after domain) | Auto-strip, no error |

**Domain validation regex:**
```typescript
const DOMAIN_REGEX = /^(?:[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?\.)+[a-zA-Z]{2,}$/;
```

### Suspend Reason

| Field | Type | Constraints | Error Message |
|-------|------|------------|---------------|
| `reason` | string | Required | "Reason is required" |
| `reason` | string | Min 1 char (after trim) | "Reason is required" |

### Delete Account Confirmation

| Field | Type | Constraints | Error Message |
|-------|------|------------|---------------|
| `confirmSlug` | string | Must match tenant slug exactly | Button disabled (no error text -- visual feedback only) |

---

## Security Considerations

### Token & Session Storage

| Data | Storage | Reason |
|------|---------|--------|
| JWT access token | Memory (variable) | Short-lived, never persisted |
| Refresh token | httpOnly cookie | Secure, not accessible via JS |
| Impersonation tokens | sessionStorage | Tab-scoped, cleared on tab close |
| Tenant context | Derived from JWT | Not separately stored |

### Impersonation Security

| Rule | Implementation |
|------|---------------|
| Impersonation tokens are tab-scoped | Stored in `sessionStorage`, not `localStorage` |
| Impersonation banner cannot be hidden | Rendered outside of page content, no dismiss button |
| Impersonation session is hard-limited | 1 hour max, `expires_at` checked server-side on every refresh |
| Delete account blocked during impersonation | Frontend hides button; backend also rejects |
| All impersonation actions audited | JWT `is_impersonation` claim logged server-side |

### Rate Limiting Awareness

| Action | Debounce/Throttle |
|--------|------------------|
| Admin tenant search | 300ms debounce |
| Provisioning status polling | 3 second interval |
| Dashboard refresh | No auto-refresh; manual refresh button |
| Domain add form submit | Disable button during request |
| Tenant name save | Disable button during request |

### Input Sanitization

| Input | Sanitization |
|-------|-------------|
| Tenant name | Trim whitespace. No HTML tags (display via textContent, not innerHTML). |
| Domain name | Trim whitespace. Strip protocol. Strip path/query. Lowercase. |
| Suspend reason | Trim whitespace. No HTML tags. |
| Search query (admin) | Trim whitespace. No SQL-like patterns needed (server handles). |

---

## Frontend Action -> Backend Use Case Mapping

| Frontend Action | Backend Use Case | Endpoint |
|----------------|-----------------|----------|
| View provisioning status | Get tenant status | `GET /api/tenants/me/status` |
| View dashboard | Get dashboard summary | `GET /api/dashboard/summary` |
| View domains | List domains | `GET /api/domains` |
| Add domain | Create domain | `POST /api/domains` |
| Delete domain | Delete domain | `DELETE /api/domains/{id}` |
| View domain detail | Get domain + scan history | `GET /api/domains/{id}` |
| Update tenant name | Update tenant | `PATCH /api/tenants/me` |
| Request account deletion | Create deletion request | `POST /api/tenants/me/deletion-request` |
| View billing | Get subscription + credits + history | `GET /api/billing/subscription`, `/credits`, `/history` |
| View plans | List available plans | `GET /api/billing/plans` |
| Upgrade plan | Create Stripe checkout session | `POST /api/billing/checkout` |
| Purchase credits | Create Stripe checkout session | `POST /api/billing/credits/purchase` |
| Admin: List tenants | List all tenants | `GET /api/admin/tenants` |
| Admin: View tenant | Get tenant detail | `GET /api/admin/tenants/{id}` |
| Admin: Suspend tenant | Suspend tenant | `POST /api/admin/tenants/{id}/suspend` |
| Admin: Reactivate tenant | Reactivate tenant | `POST /api/admin/tenants/{id}/reactivate` |
| Admin: Approve deletion | Process deletion | `POST /api/admin/tenants/{id}/deletion` |
| Admin: Deny deletion | Process deletion | `POST /api/admin/tenants/{id}/deletion` |
| Admin: Impersonate | Start impersonation | `POST /api/admin/tenants/{id}/impersonate` |
| Admin: Retry provisioning | Retry provisioning | `POST /api/admin/tenants/{id}/retry-provisioning` |
| Admin: Delete failed tenant | Delete tenant | `DELETE /api/admin/tenants/{id}` |
| End impersonation | End session | `DELETE /api/auth/session` |

---

## State Management Notes

### State Reset Triggers

| Event | State to Reset |
|-------|---------------|
| Logout | All stores reset to initial state |
| Session expired | All stores reset -> redirect to login |
| Tenant suspended (mid-session) | All stores reset -> redirect to `/auth/suspended` |
| Impersonation started | Load fresh tenant/dashboard/domain stores with impersonated tenant data |
| Impersonation ended | Clear impersonation store -> close tab |
| Plan upgrade completed (Stripe return) | Reload subscription, credits, tenant stores |
| Domain added/deleted | Reload domains list + dashboard summary (domains count) |
| Tenant name updated | Update tenant store + navbar display |

### State Persistence

| Store | Persistence | Reason |
|-------|------------|--------|
| Tenant | None (fetched on load) | Always fresh from API |
| Dashboard | None (fetched on load) | Data changes frequently |
| Domains | None (fetched on load) | List can change |
| Billing | None (fetched on load) | Credits/subscription can change |
| Impersonation | sessionStorage (tab-scoped) | Survives page refresh within tab |
| Admin Tenants | None (fetched on load) | Always fresh |
| Search/filter state | URL query params | Shareable, survives refresh |

---

## Accessibility Requirements

| Element | Requirement |
|---------|------------|
| Free tier banner | `role="alert"`, includes upgrade link with `aria-label` |
| Impersonation banner | `role="alert"`, `aria-live="polite"` for countdown |
| Status badges | Color + text label (not color-only) |
| Disabled buttons (free tier) | `aria-disabled="true"`, `title` explaining why disabled |
| Confirmation modals | Focus trap, `aria-modal="true"`, ESC to close |
| Form errors | `aria-describedby` linking input to error message |
| Data tables | `<table>` with proper `<thead>`/`<tbody>`, sortable columns with `aria-sort` |
| Pagination | `nav` with `aria-label="Pagination"` |
| Provisioning steps | `aria-live="polite"` for step updates |
