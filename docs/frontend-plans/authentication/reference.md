# Reference (Authentication & Account Security)

Scope: Error handling matrix, input validation rules, security considerations, and backend use case mapping for all auth features.

---

## 1. Error Handling Matrix

### 1.1 Registration Errors

| Error Code | HTTP | User Message | UI Action |
|------------|------|-------------|-----------|
| `ERR_AUTH_001` | 400 | Password must be at least 12 characters | Inline error on password field |
| `ERR_AUTH_002` | 400 | Password must not exceed 128 characters | Inline error on password field |
| `ERR_AUTH_003` | 400 | Password must contain uppercase, lowercase, digit, and special character | Inline error on password field + highlight missing checklist items |
| `ERR_AUTH_011` | 409 | Email already registered | Inline error on email field + "Log in?" link |
| `422` (validation) | 422 | Field-level errors from `details` object | Map each field error to its input |

### 1.2 Login Errors

| Error Code | HTTP | User Message | UI Action |
|------------|------|-------------|-----------|
| `ERR_AUTH_005` | 401 | Invalid email or password | Alert banner above form. Clear password field. Keep email. |
| `ERR_AUTH_006` | 423 | Account locked. Try again in X minutes. | Alert banner. Disable form + submit button. Show countdown if time available. |
| `ERR_AUTH_007` | 403 | Account deactivated. Contact support. | Alert banner. Disable form + submit button. Show support link. |
| `ERR_AUTH_010` | 429 | Too many login attempts. Try again later. | Alert banner. Disable submit for 60 seconds with countdown. |

### 1.3 2FA Errors

| Error Code | HTTP | User Message | UI Action |
|------------|------|-------------|-----------|
| `ERR_AUTH_008` | 401 | Invalid verification code | Inline error below OTP input. Clear input. Focus first digit. |
| `ERR_AUTH_014` | 403 | Session expired. Please log in again. | Alert banner. Show "Back to Login" button. Disable code input. |
| `ERR_AUTH_015` | 400 | Two-factor authentication is already enabled | Redirect to `/auth/login` with toast. |
| `ERR_AUTH_016` | 400 | Two-factor authentication is not enabled | Redirect to `/auth/login` with toast. Unexpected state — shouldn't happen in normal flow. |

### 1.4 Password Change Errors

| Error Code | HTTP | User Message | UI Action |
|------------|------|-------------|-----------|
| `ERR_AUTH_001` | 400 | Password must be at least 12 characters | Inline error on new password field |
| `ERR_AUTH_002` | 400 | Password must not exceed 128 characters | Inline error on new password field |
| `ERR_AUTH_003` | 400 | Password must contain uppercase, lowercase, digit, and special character | Inline error + highlight missing checklist items |
| `ERR_AUTH_004` | 400 | Password was used recently. Choose a different password. | Inline error on new password field |
| `ERR_AUTH_005` | 401 | Current password is incorrect | Inline error on current password field (voluntary change only) |
| `ERR_AUTH_014` | 403 | Session expired. Please log in again. | Alert banner. Redirect to `/auth/login`. (forced change only) |

### 1.5 Admin Action Errors

| Error Code | HTTP | User Message | UI Action |
|------------|------|-------------|-----------|
| `ERR_AUTH_017` | 404 | User not found | Toast error. Navigate back to user list. |
| `403` | 403 | You don't have permission to perform this action | Toast error. No state change. |
| `409` | 409 | Action conflicts with current user state | Toast error. Refresh user detail to get latest state. |

### 1.6 Session / Token Errors

| Error Code | HTTP | User Message | UI Action |
|------------|------|-------------|-----------|
| `ERR_AUTH_009` | 401 | Session expired. Please log in again. | Clear auth state. Redirect to `/auth/login`. Toast notification. |
| `ERR_AUTH_013` | 403 | Password change required before continuing | Redirect to `/auth/change-password` (shouldn't happen if login flow handled correctly). |

### 1.7 Network / Generic Errors

| Condition | User Message | UI Action |
|-----------|-------------|-----------|
| Network error (no response) | Unable to connect. Check your internet connection. | Toast error. Keep form state. Enable retry. |
| 500 Internal Server Error | Something went wrong. Please try again. | Toast error. Keep form state. Enable retry. |
| 503 Service Unavailable | Service temporarily unavailable. Please try again shortly. | Toast error. Keep form state. Enable retry. |
| Unknown error code | Something went wrong. Please try again. | Toast error. Log error to console in dev mode. |

---

## 2. Input Validation Rules

### 2.1 Client-Side Validation

All client-side validation runs **before** API calls to provide instant feedback. Server is still the authority — client validation is a UX convenience.

| Field | Type | Constraints | Validation Timing | Error Message |
|-------|------|-------------|-------------------|---------------|
| Email | string | Required. Max 255 chars. RFC 5322 format. | On blur + on submit | "Enter a valid email address" |
| Password | string | Required. 12–128 chars. Must contain: uppercase, lowercase, digit, special char (OWASP set). | On input (real-time checklist) | Checklist items turn red/green |
| Confirm Password | string | Required. Must match password. | On blur + on submit | "Passwords don't match" |
| Organization Name | string | Required. Min 1 char. Max 100 chars. Trimmed. | On blur + on submit | "Organization name is required" |
| TOTP Code | string | Required. Exactly 6 digits. | On complete (auto-submit) | "Invalid code" |
| Current Password | string | Required (voluntary change only). | On submit | "Current password is required" |

### 2.2 Password Complexity Rules (BR-AUTH-001)

```typescript
// OWASP special characters set
const SPECIAL_CHARS = /[!@#$%^&*()\-_=+\[\]{};:'",./<>?\\|`~]/;

function validatePassword(password: string): ValidationResult {
  return {
    minLength: password.length >= 12,
    maxLength: password.length <= 128,
    hasUppercase: /[A-Z]/.test(password),
    hasLowercase: /[a-z]/.test(password),
    hasDigit: /[0-9]/.test(password),
    hasSpecial: SPECIAL_CHARS.test(password),
  };
}
```

### 2.3 Password Strength Meter Levels

| Level | Criteria | Visual |
|-------|----------|--------|
| None | Empty input | `░░░░░░` (grey) |
| Weak | < 12 chars OR missing 2+ categories | `██░░░░` (red) |
| Good | 12+ chars AND all categories met | `████░░` (yellow) |
| Strong | 16+ chars AND all categories met | `██████` (green) |

---

## 3. Security Considerations

### 3.1 Token Storage

| Data | Storage | Rationale |
|------|---------|-----------|
| JWT access token | httpOnly secure cookie (backend-set) | Not accessible to JavaScript. XSS-proof. |
| Refresh token | httpOnly secure cookie (backend-set) | Not accessible to JavaScript. XSS-proof. |
| Temp token | SvelteKit navigation state (`goto()` state param) | In-memory only. Cleared on navigation. Never in URL (except forgot-password reset link). |
| User object | Svelte store (in-memory) | Fetched from `/api/auth/me`. Lost on page refresh → re-fetched. |
| 2FA secret (Base32) | Displayed only. Never stored client-side. | Shown once during setup. User copies to authenticator app. |

### 3.2 What NOT to Store

- Never store passwords (even temporarily) in any client storage.
- Never store JWT or refresh tokens in localStorage, sessionStorage, or cookies set by JavaScript.
- Never store TOTP secrets after the setup page is left.
- Never log tokens or passwords to console (even in dev mode — use redacted placeholders).

### 3.3 Cookie Configuration (Backend Requirements)

The frontend relies on the backend setting cookies with these attributes:

| Attribute | Value | Purpose |
|-----------|-------|---------|
| `HttpOnly` | `true` | Prevents JavaScript access |
| `Secure` | `true` | HTTPS only |
| `SameSite` | `Strict` | Prevents CSRF via cross-site requests |
| `Path` | `/api` | Cookies only sent to API endpoints |
| `Max-Age` | 900 (JWT) / 604800 (refresh) | 15 min / 7 days |

### 3.4 CORS & Credentials

- All `fetch()` calls must include `credentials: 'include'` to send httpOnly cookies.
- Backend CORS must whitelist the frontend origin exactly (no wildcards with credentials).
- SvelteKit provides built-in CSRF protection for form submissions.

### 3.5 Rate Limiting Awareness

| Endpoint | Limit | Frontend Behavior |
|----------|-------|-------------------|
| `POST /api/auth/login` | 10 per 15 min per IP | On 429: disable submit, show countdown (60s default). |
| `POST /api/auth/password/forgot` | (rate limited) | On 429: show "Too many requests" toast. |
| `POST /api/auth/2fa/verify` | (no explicit limit per BR) | TOTP failures do NOT increment login lockout counter. |

**Debouncing:**
- Email availability check (if added): debounce 500ms.
- Search in admin user list: debounce 300ms.

### 3.6 Input Sanitization

- All user input trimmed before submission (`email.trim()`, `tenantName.trim()`).
- Password is NOT trimmed (spaces are valid characters).
- Email is lowercased before submission (backend also normalizes, but match for consistency).
- No HTML/script content expected in any auth field — backend rejects any HTML tags in text fields.

---

## 4. Frontend Actions → Backend Use Cases

| Frontend Action | Business Rule | Backend Endpoint | Notes |
|-----------------|--------------|-----------------|-------|
| Submit registration | BR-AUTH-001 (password), BR-TNT (tenant creation) | `POST /api/auth/register` | Creates user + tenant atomically |
| Submit login | BR-AUTH-005 | `POST /api/auth/login` | Multi-branch response handling |
| Fetch 2FA setup | BR-AUTH-2FA-SETUP (Step 1) | `GET /api/auth/2fa/setup` | Requires temp token |
| Verify 2FA code (setup) | BR-AUTH-2FA-SETUP (Step 2) | `POST /api/auth/2fa/verify` | Sets status ACTIVE, creates session |
| Verify 2FA code (login) | BR-AUTH-2FA-LOGIN | `POST /api/auth/2fa/verify` | Same endpoint, different temp token purpose |
| Change password (forced) | BR-AUTH-004, BR-AUTH-009 | `POST /api/auth/password/change` | Temp token auth. All sessions deleted. |
| Change password (voluntary) | BR-AUTH-001, BR-AUTH-009 | `POST /api/auth/password/change` | Cookie auth + current password. All sessions deleted. |
| Forgot password | (implied by password change flow) | `POST /api/auth/password/forgot` | Always returns success (no email enumeration) |
| Refresh token | BR-AUTH-013 | `POST /api/auth/refresh` | Silent. Cookies auto-sent. Single-use rotation. |
| Logout | BR-AUTH-009 (session deletion) | `POST /api/auth/logout` | Clears cookies + local state |
| Get current user | (session validation) | `GET /api/auth/me` | Used by auth guard on app load |
| Admin: list users | BR-AUTH-015 (audit) | `GET /api/admin/users` | Super admin only. Paginated. |
| Admin: view user | BR-AUTH-015 (audit) | `GET /api/admin/users/:id` | Super admin only. |
| Admin: deactivate | BR-AUTH state transition | `POST /api/admin/users/:id/deactivate` | Confirmation required. |
| Admin: re-enable | BR-AUTH state transition | `POST /api/admin/users/:id/enable` | — |
| Admin: unlock | BR-AUTH state transition | `POST /api/admin/users/:id/unlock` | — |
| Admin: reset 2FA | BR-AUTH-2FA-RESET | `POST /api/admin/users/:id/reset-2fa` | Confirmation required. Sets PENDING_2FA. |

---

## 5. State Management Notes

### 5.1 Auth State Lifecycle

```
Page load → GET /api/auth/me
  ├─ 200 → set user in store → render app
  ├─ 401 → user is null → redirect to /auth/login (if on protected route)
  └─ Network error → show offline banner → retry on reconnect
```

### 5.2 State Reset Triggers

| Event | Reset Action |
|-------|-------------|
| Successful logout | Clear entire auth store. Redirect to `/auth/login`. |
| 401 on token refresh | Clear entire auth store. Redirect to `/auth/login`. Toast "Session expired." |
| Password changed (any variant) | Clear entire auth store. Redirect to `/auth/login`. Toast "Password changed." |
| Navigate away from `/auth/*` page | Clear form state (passwords, OTP codes). Keep temp token until consumed. |
| Temp token expires (ERR_AUTH_014) | Clear temp token. Redirect to `/auth/login`. Toast "Session expired." |

### 5.3 Optimistic Updates

No optimistic updates for auth actions. All state changes wait for server confirmation because:
- Auth state is security-critical.
- Incorrect optimistic state could grant unauthorized access.
- Auth API calls are fast (no need for perceived speed).

Admin actions (deactivate, unlock, etc.) also wait for server confirmation — refresh user detail after success.

---

## 6. Accessibility Notes

| Component | Requirement |
|-----------|------------|
| Password toggle (show/hide) | `aria-label="Show password"` / `"Hide password"`. Toggle via button, not checkbox. |
| OTP input | `aria-label="Digit N of 6"` on each box. Group wrapped in `role="group"` with `aria-label="Verification code"`. |
| Alert banners | `role="alert"` for error banners (announced by screen readers). `role="status"` for info banners. |
| Form errors | `aria-describedby` linking input to its error message element. `aria-invalid="true"` on invalid fields. |
| Loading states | Submit buttons show `aria-busy="true"` and `aria-disabled="true"` while submitting. |
| Confirmation dialogs | Focus trapped inside modal. `role="alertdialog"`. ESC key closes. |
| Status badges | Color is never the only indicator — always includes text label (e.g., "● Active" not just a green dot). |
