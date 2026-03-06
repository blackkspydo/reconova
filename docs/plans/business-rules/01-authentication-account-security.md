# 1. Authentication & Account Security

> Covers: BR-AUTH-001 through BR-AUTH-015 from the original business rules.
> Error response JSON schema is defined in `00-index.md` Section 12 reference.

### 1.1 User Account States

| Status | Meaning |
|--------|---------|
| `PENDING_2FA` | User registered, email confirmed, but 2FA not yet enrolled. Cannot access any feature. |
| `ACTIVE` | Fully authenticated with 2FA. Full access per role and plan. |
| `LOCKED` | Locked due to failed login attempts. Automatically unlocks after lockout period. |
| `PASSWORD_EXPIRED` | 90-day password rotation triggered. Must change password before accessing any feature. |
| `DEACTIVATED` | Account disabled by super admin. Cannot log in. Data preserved. |

### 1.2 User Account State Transitions

| From | To | Trigger | Who |
|------|----|---------|-----|
| _(new)_ | `PENDING_2FA` | User completes registration | Self-service |
| `PENDING_2FA` | `ACTIVE` | User enrolls and verifies TOTP 2FA | Self-service |
| `ACTIVE` | `LOCKED` | 3 consecutive failed login attempts | System |
| `LOCKED` | `ACTIVE` | Lockout period (1 hour) expires | System |
| `LOCKED` | `ACTIVE` | Super admin manually unlocks | Super admin |
| `ACTIVE` | `PASSWORD_EXPIRED` | 90 days since last password change | System |
| `PASSWORD_EXPIRED` | `ACTIVE` | User sets a new valid password | Self-service |
| `ACTIVE` | `DEACTIVATED` | Super admin disables account | Super admin |
| `DEACTIVATED` | `ACTIVE` | Super admin re-enables account | Super admin |
| `PENDING_2FA` | `DEACTIVATED` | Super admin disables account | Super admin |

```
                     ┌──────────────┐
       register      │ PENDING_2FA  │
      ──────────────►│              │
                     └──────┬───────┘
                            │ enroll 2FA
                     ┌──────▼───────┐
              ┌─────►│    ACTIVE    │◄─────────┐
              │      └──┬───┬───┬──┘           │
              │         │   │   │              │
         unlock/     3  │   │   │ 90 days   re-enable
         expire    fails│   │   │              │
              │         │   │   │              │
         ┌────┴───┐     │   │  ┌▼──────────┐  │
         │ LOCKED │◄────┘   │  │ PASSWORD   │  │
         └────────┘         │  │ _EXPIRED   │  │
                            │  └────────────┘  │
                     ┌──────▼───────┐          │
                     │ DEACTIVATED  ├──────────┘
                     └──────────────┘
```

### 1.3 Field Constraints

**Users Table (Control DB)**

| Field | Type | Constraints |
|-------|------|------------|
| `id` | UUID | System-generated. Immutable. |
| `tenant_id` | UUID | Foreign key to `tenants`. Nullable for super admins. Immutable after creation. |
| `email` | string | Required. Max 255 chars. Must match RFC 5322 format. Unique globally. Stored lowercase-normalized. |
| `password_hash` | string | BCrypt hash with cost factor 12. Never stored as plaintext. |
| `two_factor_secret` | string | 20-byte Base32-encoded TOTP secret. Encrypted at rest with AES-256. Nullable (null until enrollment). |
| `two_factor_enabled` | boolean | Default false. Set true after successful TOTP verification during enrollment. |
| `role` | string | CHECK (`TENANT_OWNER`, `SUPER_ADMIN`). Default: `TENANT_OWNER`. Immutable after creation except by super admin. |
| `status` | string | CHECK (`PENDING_2FA`, `ACTIVE`, `LOCKED`, `PASSWORD_EXPIRED`, `DEACTIVATED`). |
| `created_at` | datetime | UTC. Set on creation. Immutable. |
| `password_changed_at` | datetime | UTC. Updated on every password change. Used for 90-day rotation check. |
| `failed_login_count` | integer | 0–3. Reset to 0 on successful login. Incremented on failed attempt. At 3, account locks. |
| `locked_until` | datetime | Nullable. Set to `now + 1 hour` when locked. Null when unlocked. |
| `last_login_at` | datetime | UTC. Updated on every successful login. |
| `last_login_ip` | string | IPv4 or IPv6. Stored on successful login. For audit purposes. |

**Sessions Table (Control DB)**

| Field | Type | Constraints |
|-------|------|------------|
| `id` | UUID | System-generated. |
| `user_id` | UUID | Foreign key to `users`. |
| `refresh_token_hash` | string | SHA-256 hash of the refresh token. Never store plaintext. |
| `is_used` | boolean | Default false. Set true when refresh token is consumed. Single-use enforcement. |
| `used_at` | datetime | Nullable. Set when `is_used` becomes true. Used for 30-second grace period. |
| `ip_address` | string | IPv4 or IPv6. Captured at session creation. |
| `user_agent` | string | Max 500 chars. Captured at session creation. |
| `created_at` | datetime | UTC. Immutable. |
| `last_active_at` | datetime | UTC. Updated on each token refresh. Used for 30-min idle timeout. |
| `expires_at` | datetime | UTC. Set to `created_at + 7 days`. Absolute session expiry. |

**Password History Table (Control DB)**

| Field | Type | Constraints |
|-------|------|------------|
| `id` | UUID | System-generated. |
| `user_id` | UUID | Foreign key to `users`. |
| `password_hash` | string | BCrypt hash of the previous password. |
| `created_at` | datetime | UTC. When this password was set. |

Only the last 5 entries per user are relevant for the reuse check. Older entries may be retained for audit but are not checked.

### 1.4 Password Rules

**OWASP special characters set:**

```
! @ # $ % ^ & * ( ) - _ = + [ ] { } ; : ' " , . < > ? / \ | ` ~
```

**Validation algorithm:**

```
BR-AUTH-001: Password Requirements
──────────────────────────────────
Input:  candidate_password, user_id

1. IF length(candidate_password) < 12 → REJECT "ERR_AUTH_001"
2. IF length(candidate_password) > 128 → REJECT "ERR_AUTH_002"
3. IF NOT contains_uppercase(candidate_password) → REJECT "ERR_AUTH_003"
4. IF NOT contains_lowercase(candidate_password) → REJECT "ERR_AUTH_003"
5. IF NOT contains_digit(candidate_password) → REJECT "ERR_AUTH_003"
6. IF NOT contains_special(candidate_password, OWASP_SET) → REJECT "ERR_AUTH_003"
7. LOAD last 5 password_hashes for user_id (ordered by created_at DESC)
8. FOR EACH old_hash:
     IF bcrypt_verify(candidate_password, old_hash) → REJECT "ERR_AUTH_004"
9. ACCEPT
```

**Password rotation enforcement:**

```
BR-AUTH-004: Password Rotation Enforcement
───────────────────────────────────────────
Input: user (from JWT or login)

1. IF user.status == "ACTIVE":
   IF (now() - user.password_changed_at) > 90 days:
     SET user.status = "PASSWORD_EXPIRED"
     AUDIT_LOG("auth.password_expired", user.id)
2. Caller handles PASSWORD_EXPIRED status per context:
   - Login flow: returns requires_password_change with temp token
   - API middleware: returns ERR_AUTH_013
```

**Password change side effects:**

- New hash stored in `users.password_hash`
- Old hash added to `password_history`
- `users.password_changed_at` = now()
- If user was `PASSWORD_EXPIRED`, status transitions to `ACTIVE`
- All existing sessions for this user are deleted (BR-AUTH-009)
- All refresh tokens become invalid (consequence of session deletion)
- `AUDIT_LOG("auth.password_changed", user.id)`

### 1.5 Login Flow

```
BR-AUTH-005: Login Flow
───────────────────────
Input:  email, password, totp_code (optional), ip_address, user_agent

1. RATE CHECK: Count login attempts from ip_address in last 15 min
   IF count >= 10 → REJECT "ERR_AUTH_010" (IP rate limited)
   Storage: Redis key "login_attempts:{ip_address}" with 15-min TTL

2. LOOKUP user by email (case-insensitive)
   IF NOT found → REJECT "ERR_AUTH_005" (invalid credentials)
   NOTE: Same error for missing user and wrong password to prevent enumeration

3. IF user.status == "LOCKED":
   IF user.locked_until > now() → REJECT "ERR_AUTH_006"
   ELSE → SET user.status = "ACTIVE", user.failed_login_count = 0

4. IF user.status == "DEACTIVATED" → REJECT "ERR_AUTH_007"

5. IF NOT bcrypt_verify(password, user.password_hash):
   INCREMENT user.failed_login_count
   IF user.failed_login_count >= 3:
     SET user.status = "LOCKED"
     SET user.locked_until = now() + 1 hour
     AUDIT_LOG("auth.account_locked", user.id)
   REJECT "ERR_AUTH_005"

6. RESET user.failed_login_count = 0

7. IF user.status == "PASSWORD_EXPIRED":
   GENERATE temp_token (HMAC-signed, purpose: "password_change", expiry: 5 min)
   RETURN { requires_password_change: true, temp_token }

8. IF user.status == "PENDING_2FA":
   GENERATE temp_token (HMAC-signed, purpose: "2fa_setup", expiry: 5 min)
   RETURN { requires_2fa_setup: true, temp_token }

9. IF user.two_factor_enabled:
   IF totp_code is null:
     GENERATE temp_token (HMAC-signed, purpose: "2fa_verify", expiry: 5 min)
     RETURN { requires_2fa: true, temp_token }
   IF NOT verify_totp(user.two_factor_secret, totp_code):
     REJECT "ERR_AUTH_008"

10. [POST-MVP] DEVICE CHECK:
    IF ip_address NOT IN user.known_ips (last 30 days):
      SEND verification email
      RETURN { requires_device_verification: true }

11. SESSION MANAGEMENT:
    COUNT active sessions for user (WHERE expires_at > now() AND is_used = false)
    IF count >= 3 → DELETE oldest session (by created_at ASC)

12. CREATE session record:
    {
      user_id,
      refresh_token_hash: sha256(refresh_token),
      is_used: false,
      ip_address,
      user_agent,
      created_at: now(),
      last_active_at: now(),
      expires_at: now() + 7 days
    }

13. GENERATE jwt_token (15 min expiry) with claims:
    { user_id, tenant_id, role, session_id }
    GENERATE refresh_token (cryptographically random, 256-bit, base64url-encoded)

14. UPDATE user.last_login_at = now()
    UPDATE user.last_login_ip = ip_address
    AUDIT_LOG("auth.login", user.id, { ip: ip_address, user_agent })

15. RETURN { token, refresh_token, expires_in: 900 }
```

### 1.6 Token Management

**Three token types in the system:**

| Token | Format | Storage | Lifetime | Purpose |
|-------|--------|---------|----------|---------|
| Access token (JWT) | Signed JWT (HMAC-SHA256) | Client-side only. Never stored server-side. | 15 minutes | Authenticates API requests. Claims: `user_id`, `tenant_id`, `role`, `session_id`. |
| Refresh token | Cryptographically random (256-bit, base64url-encoded) | SHA-256 hash in `sessions.refresh_token_hash` | 7 days | Obtains new access token. Single-use with rotation. |
| Temp token | HMAC-SHA256 signed payload | Not stored. Verified by signature. | 5 minutes | Allows access to a single restricted endpoint (password change, 2FA setup, 2FA verify). |

**Token refresh algorithm:**

```
BR-AUTH-013: Token Refresh
──────────────────────────
Input: refresh_token

1. COMPUTE hash = sha256(refresh_token)

2. LOOKUP session WHERE refresh_token_hash = hash
   IF NOT found → REJECT "ERR_AUTH_009"

3. IF session.is_used == true:
   // Potential token theft — same refresh token presented twice
   IF session.used_at + 30 seconds > now():
     // Grace period for network retries — return same token pair
     RETURN cached_response
   ELSE:
     // Outside grace period — likely theft. Invalidate entire session.
     DELETE session
     AUDIT_LOG("auth.refresh_token_reuse", session.user_id, { suspicious: true })
     REJECT "ERR_AUTH_009"

4. IF session.expires_at <= now() → DELETE session, REJECT "ERR_AUTH_009"

5. IF (now() - session.last_active_at) > 30 minutes:
   // Idle timeout exceeded
   DELETE session
   REJECT "ERR_AUTH_009"

6. LOAD user by session.user_id
   IF user.status NOT IN ("ACTIVE") → DELETE session, REJECT "ERR_AUTH_007"

7. MARK session.is_used = true, session.used_at = now()

8. CREATE new session record:
   {
     user_id: session.user_id,
     refresh_token_hash: sha256(new_refresh_token),
     is_used: false,
     ip_address: session.ip_address,
     user_agent: session.user_agent,
     created_at: now(),
     last_active_at: now(),
     expires_at: session.expires_at  // preserves original 7-day window
   }

9. DELETE old session record

10. GENERATE new jwt_token (15 min) with same claims
    GENERATE new refresh_token (random)

11. RETURN { token, refresh_token, expires_in: 900 }
```

**Temp token format:**

```
BR-AUTH-TEMP: Temp Token Format
────────────────────────────────
Payload: base64url(user_id + ":" + purpose + ":" + expires_unix)
Signature: HMAC-SHA256(payload, server_secret)
Token: payload + "." + signature

Purposes: "password_change", "2fa_setup", "2fa_verify"

Validation:
1. Split token into payload + signature
2. Verify HMAC-SHA256(payload, server_secret) == signature
3. Decode payload → extract user_id, purpose, expires_unix
4. IF expires_unix < now() → REJECT "ERR_AUTH_014"
5. IF purpose != expected_purpose for this endpoint → REJECT "ERR_AUTH_014"
6. RETURN user_id
```

### 1.7 Session Management

**Session lifecycle:**

| Event | Behavior |
|-------|----------|
| Login success | New session created. If user already has 3 sessions, oldest is deleted. |
| Token refresh | `last_active_at` updated on new session. Old session deleted. Idle timeout checked (30 min since `last_active_at`). |
| Idle timeout | If `last_active_at + 30 min < now()` at refresh time, session is deleted and refresh rejected. |
| Absolute expiry | If `expires_at <= now()`, session is deleted. 7-day hard limit regardless of activity. |
| Password change | All sessions for the user are deleted. User must re-authenticate on all devices. |
| Account locked | Existing sessions remain but refresh will fail (user.status check in refresh flow). Sessions expire naturally. |
| Account deactivated | Same as locked — refresh fails on status check. Sessions expire naturally. |
| User logout | Specific session is deleted by session_id. |

**Session cleanup background job:**

```
BR-AUTH-CLEANUP: Session Garbage Collection
────────────────────────────────────────────
Runs: Every 1 hour (background job)

1. DELETE FROM sessions WHERE expires_at <= now()
2. DELETE FROM sessions WHERE last_active_at + 30 minutes < now()
3. LOG count of cleaned sessions
```

Ensures stale sessions don't accumulate even if users never explicitly refresh or logout.

**Concurrent session counting:**

Only sessions where `expires_at > now()` are counted toward the limit of 3. Stale/expired sessions don't block new logins.

### 1.8 Two-Factor Authentication

**2FA Enrollment Flow:**

```
BR-AUTH-2FA-SETUP: TOTP Enrollment
───────────────────────────────────
Input: temp_token (purpose: "2fa_setup"), totp_code

STEP 1 — Generate Secret (GET /api/auth/2fa/setup):
1. VALIDATE temp_token (purpose must be "2fa_setup")
2. LOAD user by temp_token.user_id
3. IF user.two_factor_enabled == true → REJECT "ERR_AUTH_015"
4. GENERATE 20-byte random secret
5. ENCODE as Base32
6. ENCRYPT with AES-256 and store in user.two_factor_secret
7. BUILD otpauth URI:
   otpauth://totp/Reconova:{user.email}?secret={base32}&issuer=Reconova&digits=6&period=30
8. RETURN { secret: base32, qr_uri: otpauth_uri }

STEP 2 — Verify & Activate (POST /api/auth/2fa/verify):
1. VALIDATE temp_token (purpose must be "2fa_setup")
2. LOAD user by temp_token.user_id
3. DECRYPT user.two_factor_secret
4. VERIFY totp_code against secret (window: +/- 1 step = 30 sec each direction)
   IF NOT valid → REJECT "ERR_AUTH_008"
5. SET user.two_factor_enabled = true
6. SET user.status = "ACTIVE"
7. AUDIT_LOG("auth.2fa_enrolled", user.id)
8. PROCEED to session creation (same as login flow steps 11–15)
9. RETURN { token, refresh_token, expires_in: 900 }
```

**2FA Verification on Login:**

```
BR-AUTH-2FA-LOGIN: TOTP Verification at Login
──────────────────────────────────────────────
Input: temp_token (purpose: "2fa_verify"), totp_code

1. VALIDATE temp_token (purpose must be "2fa_verify")
2. LOAD user by temp_token.user_id
3. IF user.two_factor_enabled == false → REJECT "ERR_AUTH_016"
4. DECRYPT user.two_factor_secret
5. VERIFY totp_code (window: +/- 1 step)
   IF NOT valid → REJECT "ERR_AUTH_008"
   NOTE: Failed TOTP does NOT increment failed_login_count
         (password was already verified; this prevents double-lockout)
6. PROCEED to session creation (login flow steps 11–15)
7. RETURN { token, refresh_token, expires_in: 900 }
```

**2FA Reset by Super Admin:**

```
BR-AUTH-2FA-RESET: Admin 2FA Reset
───────────────────────────────────
Input: super_admin_id, target_user_id

1. VERIFY super_admin has "users:reset_2fa" permission
2. LOAD target user
   IF NOT found → REJECT "ERR_AUTH_017"
3. SET user.two_factor_secret = null
4. SET user.two_factor_enabled = false
5. SET user.status = "PENDING_2FA"
6. DELETE all sessions for target user
7. AUDIT_LOG("auth.2fa_reset", target_user.id, { reset_by: super_admin_id })
8. RETURN success
```

On next login, the user will be prompted to re-enroll 2FA (login flow step 8).

**TOTP Configuration:**

| Parameter | Value | Notes |
|-----------|-------|-------|
| Algorithm | SHA-1 | Standard for TOTP (RFC 6238). Widest authenticator app support. |
| Digits | 6 | Standard. |
| Period | 30 seconds | Standard. |
| Window | +/- 1 step | Accepts codes from 30 sec before to 30 sec after current step. Handles clock skew. |
| Secret length | 20 bytes (160 bits) | RFC 4226 recommended minimum. |
| Issuer | `Reconova` | Displayed in authenticator apps. |
| Label | `Reconova:{email}` | Identifies the account in authenticator apps. |

### 1.9 Permissions Matrix

| Action | `TENANT_OWNER` | `SUPER_ADMIN` |
|--------|:-:|:-:|
| Register account | Yes | No (seeded) |
| Login | Yes | Yes |
| Change own password | Yes | Yes |
| Enroll 2FA (own account) | Yes | Yes |
| Reset own 2FA | No (contact support) | No (contact another super admin) |
| View own sessions | Yes | Yes |
| Logout (own session) | Yes | Yes |
| Logout all own sessions | Yes | Yes |
| Unlock any account | No | Yes |
| Deactivate any account | No | Yes |
| Re-enable any account | No | Yes |
| Reset any user's 2FA | No | Yes |
| View audit logs (own tenant) | Yes | Yes |
| View audit logs (all tenants) | No | Yes |

**Super admin audit rule (BR-AUTH-015):**

All actions performed by a super admin are logged with `is_super_admin = true` in the audit entry. This applies to every action in the system, not just auth actions. During impersonation, both `is_super_admin` and `is_impersonation` flags are set, with the super admin's `user_id` recorded in `impersonated_by`.

### 1.10 Edge Cases

| Scenario | Behavior |
|----------|----------|
| User registers with email that exists but is `DEACTIVATED` | Reject registration. Email is permanently claimed. Return `ERR_AUTH_011`. |
| User registers with email that exists and is `ACTIVE` | Reject registration. Return `ERR_AUTH_011`. Same error — no status leakage. |
| Password change while multiple sessions are active | All sessions deleted immediately. User must re-authenticate on all devices. |
| 2FA secret is compromised | User contacts support. Super admin resets 2FA via `BR-AUTH-2FA-RESET`, forcing re-enrollment on next login. |
| User attempts login during `PASSWORD_EXPIRED` state | Password verified first. If correct, returns `requires_password_change` with temp token. Temp token valid only for password change endpoint. |
| User attempts login during `PENDING_2FA` state | Password verified first. If correct, returns `requires_2fa_setup` with temp token. Temp token valid only for 2FA setup endpoint. |
| Refresh token used after password change | Rejected. All sessions were deleted on password change, so session lookup fails. Returns `ERR_AUTH_009`. |
| Refresh token used after account deactivation | Session lookup succeeds but user status check fails at step 6 of refresh flow. Returns `ERR_AUTH_007`. |
| Clock skew on TOTP verification | Codes accepted within +/- 1 step window (30 seconds each direction). Beyond that, rejected. |
| Two login attempts with correct password arrive simultaneously | Both proceed through password verification. Both attempt session creation. Both succeed — if this pushes session count over 3, the oldest sessions are cleaned up. No race condition on `failed_login_count` since both succeed. |
| Two login attempts with wrong password arrive simultaneously | Both increment `failed_login_count`. Possible to jump from 1 to 3 (skipping 2) due to race. Account locks. This is acceptable — errs on the side of security. |
| Lockout period expires during a login attempt | Step 3 of login flow checks `locked_until`. If expired, auto-unlocks and continues. No separate unlock job needed. |
| Super admin deactivates a user who is currently `LOCKED` | Status changes to `DEACTIVATED`. `locked_until` becomes irrelevant. When/if re-enabled by admin, status goes to `ACTIVE` with `failed_login_count` reset to 0. |
| Temp token reused after first use | Temp tokens are stateless — no server-side tracking of usage. However, the 5-minute expiry limits the reuse window. For password change: after first use, password hash changes so the old context is invalidated. For 2FA setup: after enrollment, `two_factor_enabled` becomes true, blocking repeat setup (`ERR_AUTH_015`). For 2FA verify: after verification, a session is created; repeat verify just creates another session (harmless, session limit enforces cleanup). |
| User with `PASSWORD_EXPIRED` also has 2FA enabled | Password change takes priority. Login flow hits step 7 before step 9. User must change password first (via temp token), then re-login, then provide TOTP code. |
| IP rate limit hit by legitimate user behind shared NAT | All users behind that IP are blocked for the 15-min window. This is a known trade-off — protects against brute force at the cost of occasional false positives on shared IPs. No override mechanism in V1. |
| Super admin tries to deactivate another super admin | Allowed. Super admins can deactivate any account, including other super admins. Requires `users:deactivate` permission. |
| Deleted/expired session referenced by session_id in JWT | JWT is still valid (signature checks pass) but any API call that checks session liveness will fail. Token refresh will fail. Access token works for its remaining lifetime since JWTs are stateless. |

### 1.11 Error Codes

Error response JSON schema is defined in Section 12.

| Code | HTTP | Message | Cause |
|------|------|---------|-------|
| `ERR_AUTH_001` | 400 | Password must be at least 12 characters | Password too short |
| `ERR_AUTH_002` | 400 | Password must not exceed 128 characters | Password too long |
| `ERR_AUTH_003` | 400 | Password must contain uppercase, lowercase, digit, and special character | Missing complexity requirement |
| `ERR_AUTH_004` | 400 | Password was used recently. Choose a different password. | Matches one of last 5 password hashes |
| `ERR_AUTH_005` | 401 | Invalid email or password | Wrong credentials or user not found (same message to prevent enumeration) |
| `ERR_AUTH_006` | 423 | Account is locked. Try again later. | 3+ failed login attempts, lockout period active |
| `ERR_AUTH_007` | 403 | Account is deactivated. Contact support. | Account disabled by super admin |
| `ERR_AUTH_008` | 401 | Invalid verification code | Wrong TOTP code |
| `ERR_AUTH_009` | 401 | Session expired. Please log in again. | Refresh token expired, used, invalid, or session idle timeout exceeded |
| `ERR_AUTH_010` | 429 | Too many login attempts. Try again later. | IP rate limit exceeded (10 per 15 min) |
| `ERR_AUTH_011` | 409 | Email already registered | Duplicate registration attempt |
| `ERR_AUTH_012` | 403 | Device verification required | Login from unrecognized IP/device (post-MVP, reserved) |
| `ERR_AUTH_013` | 403 | Password change required before continuing | API request while status is `PASSWORD_EXPIRED` |
| `ERR_AUTH_014` | 403 | Invalid or expired temporary token | Temp token signature invalid, expired, or wrong purpose |
| `ERR_AUTH_015` | 400 | Two-factor authentication is already enabled | Attempted 2FA setup when already enrolled |
| `ERR_AUTH_016` | 400 | Two-factor authentication is not enabled | Attempted 2FA verify on account without 2FA |
| `ERR_AUTH_017` | 404 | User not found | Admin action targeting non-existent user |

---
