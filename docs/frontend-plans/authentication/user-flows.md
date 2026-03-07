# User Flows (Authentication & Account Security)

Scope: All auth user journeys — registration, login, 2FA, password management, session handling, and super admin user management.

---

## Preconditions

- All auth pages (`/auth/*`) are public — no JWT required.
- 2FA setup and verify pages require a valid temp token (passed via navigation state).
- All `/(app)/*` pages require an active session (httpOnly cookie with valid JWT).
- All `/(admin)/*` pages require active session with `role === SUPER_ADMIN`.
- Backend sets JWT + refresh token in httpOnly secure cookies on login/register success.

---

## 1. Registration Flow (2-Step Wizard)

```
[/auth/register] → Step 1: Enter email + password + confirm password
    │
    │ Client-side validation (BR-AUTH-001):
    │   • 12–128 chars, uppercase, lowercase, digit, special char
    │   • Passwords match
    │   • Email RFC 5322 format
    │
    ▼ [Next →]
    │
Step 2: Enter organization name
    │   • Required, non-empty
    │
    ▼ [Create Account]
    │ (analytics: auth_register_submit)
    │
    ▼ POST /api/auth/register { email, password, tenantName }
    ├─ 201 Created → redirect to /auth/2fa-setup (temp_token in nav state)
    │                (analytics: auth_register_success)
    ├─ 409 ERR_AUTH_011 → show inline error "Email already registered"
    │                      stay on Step 1, highlight email field
    ├─ 400 ERR_AUTH_001/002/003 → show password validation errors inline
    └─ 5xx → show toast "Something went wrong. Please try again."
```

### Registration State Reset
- Navigating away clears form state.
- Back button (Step 2 → Step 1) preserves entered data.
- No partial registration — account only created on final submit.

---

## 2. Login Flow

```
[/auth/login] → Enter email + password
    │
    ▼ [Log In]
    │ (analytics: auth_login_submit)
    │
    ▼ POST /api/auth/login { email, password }
    │
    ├─ 200 { token, refresh_token }
    │   → cookies set by backend
    │   → redirect to /(app)/dashboard
    │   (analytics: auth_login_success)
    │
    ├─ 200 { requires_2fa: true, temp_token }
    │   → redirect to /auth/2fa-verify (temp_token in nav state)
    │
    ├─ 200 { requires_2fa_setup: true, temp_token }
    │   → redirect to /auth/2fa-setup (temp_token in nav state)
    │
    ├─ 200 { requires_password_change: true, temp_token }
    │   → redirect to /auth/change-password (temp_token in nav state)
    │
    ├─ 401 ERR_AUTH_005 → show "Invalid email or password"
    │   (no distinction between wrong email and wrong password)
    │
    ├─ 423 ERR_AUTH_006 → show "Account locked. Try again in X minutes."
    │   (disable submit button, show countdown if lockout info available)
    │
    ├─ 403 ERR_AUTH_007 → show "Account deactivated. Contact support."
    │   (disable submit button, show support link)
    │
    └─ 429 ERR_AUTH_010 → show "Too many attempts. Try again later."
        (disable submit button for 60 seconds with countdown)
```

### Login Edge Cases
- Authenticated user navigating to `/auth/login` → redirect to dashboard.
- Login page shows "Forgot password?" link → navigates to `/auth/forgot-password`.
- Login page shows "Create account" link → navigates to `/auth/register`.

---

## 3. 2FA Setup Flow (Post-Registration)

```
[/auth/2fa-setup] → temp_token required in nav state
    │
    │ IF no temp_token → redirect to /auth/login
    │
    ▼ GET /api/auth/2fa/setup (Authorization: temp_token)
    │
    ├─ 200 { secret, qr_uri }
    │   → display QR code (generated from qr_uri)
    │   → show manual secret entry option (base32 string)
    │
    ├─ 400 ERR_AUTH_015 → "2FA already enabled" → redirect to /auth/login
    └─ 403 ERR_AUTH_014 → "Session expired" → redirect to /auth/login with toast
    │
    ▼ User scans QR → enters 6-digit code → [Verify & Continue]
    │ (analytics: auth_2fa_setup_verify)
    │
    ▼ POST /api/auth/2fa/verify { totp_code } (Authorization: temp_token)
    │
    ├─ 200 { token, refresh_token }
    │   → cookies set by backend
    │   → redirect to /(app)/dashboard
    │   (analytics: auth_2fa_setup_success)
    │
    ├─ 401 ERR_AUTH_008 → show "Invalid code. Check your authenticator and try again."
    │   → clear code input, keep QR visible, allow retry
    │
    └─ 403 ERR_AUTH_014 → "Session expired" → redirect to /auth/login with toast
```

### 2FA Setup UX Details
- QR code rendered client-side from `qr_uri` (otpauth:// URI).
- "Can't scan?" toggle reveals the Base32 secret for manual entry.
- Code input: 6 individual digit boxes with auto-advance.
- No back button — user must complete 2FA to proceed.
- Temp token has 5-min expiry. If expired mid-setup, redirect to login with message.

---

## 4. 2FA Verify Flow (Login Challenge)

```
[/auth/2fa-verify] → temp_token required in nav state
    │
    │ IF no temp_token → redirect to /auth/login
    │
    ▼ Show: "Enter the 6-digit code from your authenticator app"
    │
    ▼ User enters code → [Verify]
    │ (analytics: auth_2fa_verify_submit)
    │
    ▼ POST /api/auth/2fa/verify { totp_code } (Authorization: temp_token)
    │
    ├─ 200 { token, refresh_token }
    │   → cookies set by backend
    │   → redirect to /(app)/dashboard
    │   (analytics: auth_2fa_verify_success)
    │
    ├─ 401 ERR_AUTH_008 → show "Invalid code"
    │   → clear input, allow retry
    │   → NOTE: does NOT increment failed_login_count (no double-lockout)
    │
    └─ 403 ERR_AUTH_014 → "Session expired" → redirect to /auth/login
```

### 2FA Verify Edge Cases
- Temp token expires after 5 min → user must re-login.
- No "resend code" — TOTP is time-based, user generates from their app.
- Show "Having trouble? Contact support" link.

---

## 5. Change Password Flow

### 5a. Forced Change (PASSWORD_EXPIRED)

```
[/auth/change-password] → temp_token in nav state (purpose: password_change)
    │
    │ IF no temp_token → redirect to /auth/login
    │
    ▼ Show: "Your password has expired. Please set a new password."
    │
    │ Enter: New password + Confirm new password
    │ (BR-AUTH-001 validation: 12+ chars, complexity, not in last 5)
    │
    ▼ [Set New Password]
    │ (analytics: auth_password_change_submit)
    │
    ▼ POST /api/auth/password/change { new_password } (Authorization: temp_token)
    │
    ├─ 200 → success toast "Password changed. Please log in."
    │   → redirect to /auth/login
    │   (analytics: auth_password_change_success)
    │   (Side effect: all sessions deleted — BR-AUTH-009)
    │
    ├─ 400 ERR_AUTH_001/002/003 → show inline validation errors
    ├─ 400 ERR_AUTH_004 → "Password was used recently. Choose a different password."
    └─ 403 ERR_AUTH_014 → session expired → redirect to /auth/login
```

### 5b. Voluntary Change (From Settings)

```
[/(app)/settings] → Security section → [Change Password]
    │
    ▼ Show: Current password + New password + Confirm new password
    │
    ▼ [Update Password]
    │ (analytics: auth_password_change_submit, source: settings)
    │
    ▼ POST /api/auth/password/change { current_password, new_password }
    │
    ├─ 200 → success toast "Password changed. You'll need to log in again."
    │   → clear auth state → redirect to /auth/login
    │   (all sessions deleted — BR-AUTH-009)
    │
    ├─ 401 ERR_AUTH_005 → "Current password is incorrect"
    ├─ 400 ERR_AUTH_001/002/003 → inline validation errors
    └─ 400 ERR_AUTH_004 → "Password was used recently"
```

---

## 6. Forgot Password Flow

```
[/auth/forgot-password] → Enter email
    │
    ▼ [Send Reset Link]
    │ (analytics: auth_forgot_password_submit)
    │
    ▼ POST /api/auth/password/forgot { email }
    │
    ├─ 200 → ALWAYS show "If an account exists, we've sent reset instructions."
    │   (no email enumeration — same message for existing & non-existing)
    │   → show "Back to login" link
    │
    └─ 429 ERR_AUTH_010 → "Too many requests. Try again later."

[User clicks email link with reset token] → /auth/change-password?token=...
    │
    ▼ Same flow as 5a (forced change) but with token from URL query param
```

---

## 7. Token Refresh Flow (Silent / Background)

```
[Any authenticated page] → API client interceptor
    │
    │ Before each request: check if JWT cookie is near expiry
    │ (backend handles refresh via cookie-based flow)
    │
    ▼ POST /api/auth/refresh (cookies sent automatically)
    │
    ├─ 200 → backend sets new cookies → continue original request
    │
    ├─ 401 ERR_AUTH_009 → session expired
    │   → clear auth state
    │   → redirect to /auth/login with toast "Session expired. Please log in."
    │   (analytics: auth_session_expired)
    │
    └─ Network error → retry once → if still fails, show offline banner
```

### Token Refresh Edge Cases
- **30-second grace period:** If two tabs refresh simultaneously, backend handles via `used_at` grace window.
- **Idle timeout (30 min):** Backend rejects refresh if `last_active_at` exceeds 30 min. Frontend shows "Session expired."
- **Password changed on another device:** Refresh fails because sessions were deleted. User redirected to login.

---

## 8. Logout Flow

```
[Any authenticated page] → Header → [Logout]
    │ (analytics: auth_logout)
    │
    ▼ POST /api/auth/logout (cookies sent automatically)
    │
    ├─ 200 → backend clears cookies → clear local auth state → redirect to /auth/login
    └─ Any error → still clear local state → redirect to /auth/login
        (best-effort server logout; client always clears)
```

---

## 9. Account Settings Flow

```
[/(app)/settings]
    │
    ▼ Load user profile: GET /api/auth/me
    │
    ├─ Display: Email (read-only), Role (read-only), Account created date
    │
    ├─ Security Section:
    │   • Password: last changed date + [Change Password] button
    │   • 2FA: status "Enabled" (always, since mandatory)
    │
    └─ [Change Password] → inline expand or scroll to password form
        → see flow 5b above
```

---

## 10. Super Admin: User Management

### 10a. User List

```
[/(admin)/users] → Super Admin only
    │
    ▼ GET /api/admin/users?page=1&pageSize=20
    │
    ├─ Display table: Email | Role | Status | Last Login | Actions
    │
    │ Filters: status (ACTIVE, LOCKED, DEACTIVATED, PENDING_2FA, PASSWORD_EXPIRED)
    │ Search: by email
    │ Sort: by email, status, last login
    │
    └─ Click row → navigate to /(admin)/users/:id
```

### 10b. User Detail + Actions

```
[/(admin)/users/:id]
    │
    ▼ GET /api/admin/users/:id
    │
    ├─ Display: Email, Role, Status, Created, Last Login, 2FA Status
    │
    ├─ Actions (conditional on user status):
    │
    │   IF status == LOCKED:
    │   └─ [Unlock Account]
    │       ▼ POST /api/admin/users/:id/unlock
    │       ├─ 200 → toast "Account unlocked" → refresh user detail
    │       └─ Error → toast error message
    │       (analytics: admin_user_unlock)
    │
    │   IF status == ACTIVE or PENDING_2FA:
    │   └─ [Deactivate Account]
    │       ▼ Confirmation dialog: "Deactivate {email}? They won't be able to log in."
    │       ▼ POST /api/admin/users/:id/deactivate
    │       ├─ 200 → toast "Account deactivated" → refresh
    │       └─ Error → toast error message
    │       (analytics: admin_user_deactivate)
    │
    │   IF status == DEACTIVATED:
    │   └─ [Re-enable Account]
    │       ▼ POST /api/admin/users/:id/enable
    │       ├─ 200 → toast "Account re-enabled" → refresh
    │       └─ Error → toast error message
    │       (analytics: admin_user_enable)
    │
    │   IF two_factor_enabled == true:
    │   └─ [Reset 2FA]
    │       ▼ Confirmation dialog: "Reset 2FA for {email}? They'll need to re-enroll."
    │       ▼ POST /api/admin/users/:id/reset-2fa
    │       ├─ 200 → toast "2FA reset. User must re-enroll on next login." → refresh
    │       └─ Error → toast error message
    │       (analytics: admin_user_reset_2fa)
    │
    └─ [← Back to Users] → navigate to /(admin)/users
```

---

## Analytics Events Summary

| Event | Trigger | Parameters |
|-------|---------|------------|
| `auth_register_submit` | Registration form submitted | `step` (1 or 2) |
| `auth_register_success` | Registration completed | — |
| `auth_login_submit` | Login form submitted | — |
| `auth_login_success` | Login completed (tokens received) | `required_2fa` (boolean) |
| `auth_2fa_setup_verify` | 2FA setup code submitted | — |
| `auth_2fa_setup_success` | 2FA enrollment completed | — |
| `auth_2fa_verify_submit` | 2FA login challenge code submitted | — |
| `auth_2fa_verify_success` | 2FA login challenge passed | — |
| `auth_password_change_submit` | Password change submitted | `source` (expired, settings, forgot) |
| `auth_password_change_success` | Password changed | `source` |
| `auth_forgot_password_submit` | Forgot password email submitted | — |
| `auth_logout` | User clicked logout | — |
| `auth_session_expired` | Token refresh failed | — |
| `admin_user_unlock` | Admin unlocked a user | `target_user_id` |
| `admin_user_deactivate` | Admin deactivated a user | `target_user_id` |
| `admin_user_enable` | Admin re-enabled a user | `target_user_id` |
| `admin_user_reset_2fa` | Admin reset user's 2FA | `target_user_id` |
