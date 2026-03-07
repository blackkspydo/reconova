# Screens & Wireframes (Authentication & Account Security)

Scope: All auth screens — public auth pages, authenticated settings, and super admin user management.

---

## Route Structure

### Public Routes (`/auth/*`)
| Route | Screen | Guard |
|-------|--------|-------|
| `/auth/register` | Registration Wizard | Redirect to dashboard if authenticated |
| `/auth/login` | Login | Redirect to dashboard if authenticated |
| `/auth/2fa-setup` | 2FA Enrollment | Requires temp_token (purpose: 2fa_setup) |
| `/auth/2fa-verify` | 2FA Login Challenge | Requires temp_token (purpose: 2fa_verify) |
| `/auth/change-password` | Change Password | Requires temp_token or authenticated |
| `/auth/forgot-password` | Forgot Password | Redirect to dashboard if authenticated |

### Authenticated Routes (`/(app)/*`)
| Route | Screen | Guard |
|-------|--------|-------|
| `/(app)/settings` | Account Settings | Authenticated (any role) |

### Admin Routes (`/(admin)/*`)
| Route | Screen | Guard |
|-------|--------|-------|
| `/(admin)/users` | User Management List | Authenticated + SUPER_ADMIN |
| `/(admin)/users/:id` | User Detail | Authenticated + SUPER_ADMIN |

---

## Auth Layout

All `/auth/*` pages share a centered card layout:

```
┌──────────────────────────────────────────────────────────────────────┐
│                                                                      │
│                         ┌─────────────┐                              │
│                         │  RECONOVA   │                              │
│                         │    logo     │                              │
│                         └─────────────┘                              │
│                                                                      │
│                    ┌────────────────────────┐                        │
│                    │                        │                        │
│                    │    Auth Card Content   │                        │
│                    │    (varies per page)   │                        │
│                    │                        │                        │
│                    └────────────────────────┘                        │
│                                                                      │
│                    © 2026 Reconova. All rights reserved.             │
│                                                                      │
└──────────────────────────────────────────────────────────────────────┘
```

---

## Screen 1: Registration Wizard

### Step 1 — Credentials

```
┌────────────────────────────────────────────┐
│            Create your account             │
│            ●───────○                       │
│            Step 1 of 2                     │
├────────────────────────────────────────────┤
│                                            │
│  Email                                     │
│  ┌──────────────────────────────────────┐  │
│  │ you@company.com                      │  │
│  └──────────────────────────────────────┘  │
│                                            │
│  Password                                  │
│  ┌──────────────────────────────────────┐  │
│  │ ••••••••••••           👁            │  │
│  └──────────────────────────────────────┘  │
│  Strength: ████░░ Good                     │
│  ✓ 12+ characters  ✓ Uppercase             │
│  ✓ Lowercase       ✓ Number                │
│  ✓ Special char                            │
│                                            │
│  Confirm Password                          │
│  ┌──────────────────────────────────────┐  │
│  │ ••••••••••••           👁            │  │
│  └──────────────────────────────────────┘  │
│                                            │
│  ┌──────────────────────────────────────┐  │
│  │             Next →                   │  │
│  └──────────────────────────────────────┘  │
│                                            │
│  Already have an account? Log in           │
│                                            │
└────────────────────────────────────────────┘
```

**Password strength indicator states:**
- `░░░░░░` None — no input
- `██░░░░` Weak — < 12 chars or missing categories
- `████░░` Good — meets all requirements
- `██████` Strong — 16+ chars with all categories

**Validation (real-time):**
- Email: RFC 5322 format, shown on blur
- Password: checklist updates as user types (BR-AUTH-001)
- Confirm: "Passwords don't match" shown on blur
- Next button disabled until all validations pass

### Step 1 — Error State (email taken)

```
┌────────────────────────────────────────────┐
│            Create your account             │
│            ●───────○                       │
├────────────────────────────────────────────┤
│                                            │
│  Email                                     │
│  ┌──────────────────────────────────────┐  │
│  │ taken@company.com                    │  │
│  └──────────────────────────────────────┘  │
│  ⚠ Email already registered. Log in?      │
│                                            │
│  ...                                       │
└────────────────────────────────────────────┘
```

### Step 2 — Organization

```
┌────────────────────────────────────────────┐
│         Set up your organization           │
│            ●───────●                       │
│            Step 2 of 2                     │
├────────────────────────────────────────────┤
│                                            │
│  Organization Name                         │
│  ┌──────────────────────────────────────┐  │
│  │ Acme Corp                            │  │
│  └──────────────────────────────────────┘  │
│  This will be your tenant name in          │
│  Reconova.                                 │
│                                            │
│  ┌─────────────┐ ┌────────────────────┐   │
│  │   ← Back    │ │  Create Account    │   │
│  └─────────────┘ └────────────────────┘   │
│                                            │
└────────────────────────────────────────────┘
```

### Step 2 — Loading State

```
┌────────────────────────────────────────────┐
│         Set up your organization           │
│            ●───────●                       │
├────────────────────────────────────────────┤
│                                            │
│  Organization Name                         │
│  ┌──────────────────────────────────────┐  │
│  │ Acme Corp                            │  │
│  └──────────────────────────────────────┘  │
│                                            │
│  ┌─────────────┐ ┌────────────────────┐   │
│  │   ← Back    │ │  ⟳ Creating...     │   │
│  └─────────────┘ └────────────────────┘   │
│                                            │
└────────────────────────────────────────────┘
```

---

## Screen 2: Login

### Default State

```
┌────────────────────────────────────────────┐
│              Welcome back                  │
├────────────────────────────────────────────┤
│                                            │
│  Email                                     │
│  ┌──────────────────────────────────────┐  │
│  │ you@company.com                      │  │
│  └──────────────────────────────────────┘  │
│                                            │
│  Password                                  │
│  ┌──────────────────────────────────────┐  │
│  │ ••••••••••••           👁            │  │
│  └──────────────────────────────────────┘  │
│                          Forgot password?  │
│                                            │
│  ┌──────────────────────────────────────┐  │
│  │              Log In                  │  │
│  └──────────────────────────────────────┘  │
│                                            │
│  Don't have an account? Register           │
│                                            │
└────────────────────────────────────────────┘
```

### Error State — Invalid Credentials

```
┌────────────────────────────────────────────┐
│              Welcome back                  │
├────────────────────────────────────────────┤
│                                            │
│  ┌──────────────────────────────────────┐  │
│  │ ⚠ Invalid email or password          │  │
│  └──────────────────────────────────────┘  │
│                                            │
│  Email                                     │
│  ┌──────────────────────────────────────┐  │
│  │ user@company.com                     │  │
│  └──────────────────────────────────────┘  │
│  ...                                       │
└────────────────────────────────────────────┘
```

### Error State — Account Locked

```
┌────────────────────────────────────────────┐
│              Welcome back                  │
├────────────────────────────────────────────┤
│                                            │
│  ┌──────────────────────────────────────┐  │
│  │ 🔒 Account locked. Try again in     │  │
│  │    47 minutes.                       │  │
│  └──────────────────────────────────────┘  │
│                                            │
│  Email / Password fields (disabled)        │
│                                            │
│  ┌──────────────────────────────────────┐  │
│  │         Log In (disabled)            │  │
│  └──────────────────────────────────────┘  │
│                                            │
└────────────────────────────────────────────┘
```

### Error State — Account Deactivated

```
┌────────────────────────────────────────────┐
│              Welcome back                  │
├────────────────────────────────────────────┤
│                                            │
│  ┌──────────────────────────────────────┐  │
│  │ ⛔ Account deactivated.              │  │
│  │    Contact support for assistance.   │  │
│  └──────────────────────────────────────┘  │
│                                            │
│  Email / Password fields (disabled)        │
│                                            │
│  ┌──────────────────────────────────────┐  │
│  │         Log In (disabled)            │  │
│  └──────────────────────────────────────┘  │
│                                            │
└────────────────────────────────────────────┘
```

### Error State — Rate Limited

```
┌────────────────────────────────────────────┐
│              Welcome back                  │
├────────────────────────────────────────────┤
│                                            │
│  ┌──────────────────────────────────────┐  │
│  │ ⏳ Too many attempts. Try again in   │  │
│  │    58 seconds.                       │  │
│  └──────────────────────────────────────┘  │
│                                            │
│  Email / Password fields (disabled)        │
│                                            │
│  ┌──────────────────────────────────────┐  │
│  │      Log In (disabled — countdown)   │  │
│  └──────────────────────────────────────┘  │
│                                            │
└────────────────────────────────────────────┘
```

---

## Screen 3: 2FA Setup

### Default State

```
┌────────────────────────────────────────────┐
│     Set up two-factor authentication       │
├────────────────────────────────────────────┤
│                                            │
│  Two-factor authentication adds an extra   │
│  layer of security to your account.        │
│                                            │
│  1. Install an authenticator app           │
│     (Google Authenticator, Authy, etc.)    │
│                                            │
│  2. Scan this QR code                      │
│     ┌──────────────────┐                   │
│     │                  │                   │
│     │    ██ ██ ██ ██   │                   │
│     │    ██    ██ ██   │                   │
│     │    ██ ██ ██ ██   │                   │
│     │                  │                   │
│     └──────────────────┘                   │
│     Can't scan? [Show manual key]          │
│                                            │
│  3. Enter the 6-digit code                 │
│     ┌──┐ ┌──┐ ┌──┐  ┌──┐ ┌──┐ ┌──┐      │
│     │  │ │  │ │  │  │  │ │  │ │  │      │
│     └──┘ └──┘ └──┘  └──┘ └──┘ └──┘      │
│                                            │
│  ┌──────────────────────────────────────┐  │
│  │         Verify & Continue            │  │
│  └──────────────────────────────────────┘  │
│                                            │
└────────────────────────────────────────────┘
```

### Manual Key Expanded

```
│     Can't scan? [Hide manual key]          │
│     ┌──────────────────────────────────┐   │
│     │ JBSW Y3DP EHPK 3PXP             │   │
│     │                       [Copy]     │   │
│     └──────────────────────────────────┘   │
```

### Error State — Invalid Code

```
│  3. Enter the 6-digit code                 │
│     ┌──┐ ┌──┐ ┌──┐  ┌──┐ ┌──┐ ┌──┐      │
│     │  │ │  │ │  │  │  │ │  │ │  │      │
│     └──┘ └──┘ └──┘  └──┘ └──┘ └──┘      │
│     ⚠ Invalid code. Check your app and    │
│       try again.                           │
```

### Loading State (fetching QR)

```
┌────────────────────────────────────────────┐
│     Set up two-factor authentication       │
├────────────────────────────────────────────┤
│                                            │
│            ⟳ Loading...                    │
│                                            │
└────────────────────────────────────────────┘
```

---

## Screen 4: 2FA Verify (Login Challenge)

### Default State

```
┌────────────────────────────────────────────┐
│        Two-factor authentication           │
├────────────────────────────────────────────┤
│                                            │
│  Enter the 6-digit code from your          │
│  authenticator app.                        │
│                                            │
│     ┌──┐ ┌──┐ ┌──┐  ┌──┐ ┌──┐ ┌──┐      │
│     │  │ │  │ │  │  │  │ │  │ │  │      │
│     └──┘ └──┘ └──┘  └──┘ └──┘ └──┘      │
│                                            │
│  ┌──────────────────────────────────────┐  │
│  │              Verify                  │  │
│  └──────────────────────────────────────┘  │
│                                            │
│  Having trouble? Contact support           │
│                                            │
└────────────────────────────────────────────┘
```

### Error State — Invalid Code

```
│     ┌──┐ ┌──┐ ┌──┐  ┌──┐ ┌──┐ ┌──┐      │
│     │  │ │  │ │  │  │  │ │  │ │  │      │
│     └──┘ └──┘ └──┘  └──┘ └──┘ └──┘      │
│     ⚠ Invalid code. Try again.            │
```

### Error State — Session Expired

```
┌────────────────────────────────────────────┐
│        Two-factor authentication           │
├────────────────────────────────────────────┤
│                                            │
│  ┌──────────────────────────────────────┐  │
│  │ ⏳ Session expired. Please log in    │  │
│  │    again.                            │  │
│  └──────────────────────────────────────┘  │
│                                            │
│  ┌──────────────────────────────────────┐  │
│  │           Back to Login              │  │
│  └──────────────────────────────────────┘  │
│                                            │
└────────────────────────────────────────────┘
```

---

## Screen 5: Change Password

### Forced Change (PASSWORD_EXPIRED)

```
┌────────────────────────────────────────────┐
│          Change your password              │
├────────────────────────────────────────────┤
│                                            │
│  ┌──────────────────────────────────────┐  │
│  │ ℹ Your password has expired.         │  │
│  │   Please set a new password to       │  │
│  │   continue.                          │  │
│  └──────────────────────────────────────┘  │
│                                            │
│  New Password                              │
│  ┌──────────────────────────────────────┐  │
│  │ ••••••••••••           👁            │  │
│  └──────────────────────────────────────┘  │
│  Strength: ████░░ Good                     │
│  ✓ 12+ characters  ✓ Uppercase             │
│  ✓ Lowercase       ✓ Number                │
│  ✓ Special char                            │
│                                            │
│  Confirm New Password                      │
│  ┌──────────────────────────────────────┐  │
│  │ ••••••••••••           👁            │  │
│  └──────────────────────────────────────┘  │
│                                            │
│  ┌──────────────────────────────────────┐  │
│  │          Set New Password            │  │
│  └──────────────────────────────────────┘  │
│                                            │
└────────────────────────────────────────────┘
```

### Error State — Password Reuse

```
│  New Password                              │
│  ┌──────────────────────────────────────┐  │
│  │ ••••••••••••                         │  │
│  └──────────────────────────────────────┘  │
│  ⚠ Password was used recently. Choose a   │
│    different password.                     │
```

---

## Screen 6: Forgot Password

### Default State

```
┌────────────────────────────────────────────┐
│           Forgot your password?            │
├────────────────────────────────────────────┤
│                                            │
│  Enter your email and we'll send you       │
│  instructions to reset your password.      │
│                                            │
│  Email                                     │
│  ┌──────────────────────────────────────┐  │
│  │ you@company.com                      │  │
│  └──────────────────────────────────────┘  │
│                                            │
│  ┌──────────────────────────────────────┐  │
│  │          Send Reset Link             │  │
│  └──────────────────────────────────────┘  │
│                                            │
│  ← Back to Login                           │
│                                            │
└────────────────────────────────────────────┘
```

### Success State

```
┌────────────────────────────────────────────┐
│           Check your email                 │
├────────────────────────────────────────────┤
│                                            │
│  ┌──────────────────────────────────────┐  │
│  │ ✓ If an account exists for that      │  │
│  │   email, we've sent reset            │  │
│  │   instructions.                      │  │
│  └──────────────────────────────────────┘  │
│                                            │
│  ← Back to Login                           │
│                                            │
└────────────────────────────────────────────┘
```

---

## Screen 7: Account Settings

```
┌──────────────────────────────────────────────────────────────────────┐
│  ┌──────────┐                                                        │
│  │ Sidebar  │  Settings                                              │
│  │          │  ────────────────────────────────────────────────────── │
│  │ Dashboard│                                                        │
│  │ Domains  │  Account                                               │
│  │ Scans    │  ┌────────────────────────────────────────────────────┐│
│  │ ...      │  │ Email        you@company.com                      ││
│  │          │  │ Role         Tenant Owner                          ││
│  │ Settings │  │ Member since March 2026                            ││
│  │          │  └────────────────────────────────────────────────────┘│
│  │          │                                                        │
│  │          │  Security                                              │
│  │          │  ┌────────────────────────────────────────────────────┐│
│  │          │  │ Password                                           ││
│  │          │  │ Last changed: 45 days ago    [Change Password]     ││
│  │          │  │                                                    ││
│  │          │  │ Two-Factor Authentication                          ││
│  │          │  │ Status: ✓ Enabled                                  ││
│  │          │  └────────────────────────────────────────────────────┘│
│  │          │                                                        │
│  └──────────┘                                                        │
└──────────────────────────────────────────────────────────────────────┘
```

### Change Password Expanded (inline)

```
│  │ Password                                           ││
│  │ Last changed: 45 days ago                          ││
│  │                                                    ││
│  │ Current Password                                   ││
│  │ ┌──────────────────────────────────────┐           ││
│  │ │ ••••••••••••           👁            │           ││
│  │ └──────────────────────────────────────┘           ││
│  │                                                    ││
│  │ New Password                                       ││
│  │ ┌──────────────────────────────────────┐           ││
│  │ │ ••••••••••••           👁            │           ││
│  │ └──────────────────────────────────────┘           ││
│  │ Strength: ████░░ Good                              ││
│  │                                                    ││
│  │ Confirm New Password                               ││
│  │ ┌──────────────────────────────────────┐           ││
│  │ │ ••••••••••••           👁            │           ││
│  │ └──────────────────────────────────────┘           ││
│  │                                                    ││
│  │ [Cancel]                [Update Password]          ││
```

---

## Screen 8: Admin — User List

```
┌──────────────────────────────────────────────────────────────────────┐
│  ┌──────────┐                                                        │
│  │ Sidebar  │  User Management                                       │
│  │          │  ────────────────────────────────────────────────────── │
│  │ Admin    │                                                        │
│  │  Users   │  ┌──────────────────────────────┐ ┌──────────────────┐ │
│  │  Tenants │  │ 🔍 Search by email...        │ │ Status: All  ▾  │ │
│  │  ...     │  └──────────────────────────────┘ └──────────────────┘ │
│  │          │                                                        │
│  │          │  ┌────────────────────────────────────────────────────┐│
│  │          │  │ Email            │ Role    │ Status │ Last Login   ││
│  │          │  ├──────────────────┼─────────┼────────┼──────────────┤│
│  │          │  │ alice@acme.com   │ OWNER   │ ACTIVE │ 2h ago       ││
│  │          │  │ bob@widgets.io   │ OWNER   │ LOCKED │ 3d ago       ││
│  │          │  │ carol@big.co     │ OWNER   │ DEACT. │ 30d ago      ││
│  │          │  │ dave@startup.dev │ OWNER   │ P_2FA  │ Never        ││
│  │          │  └────────────────────────────────────────────────────┘│
│  │          │                                                        │
│  │          │  ← Prev  Page 1 of 5  Next →                          │
│  └──────────┘                                                        │
└──────────────────────────────────────────────────────────────────────┘
```

### Loading State

```
│  │          │  ┌────────────────────────────────────────────────────┐│
│  │          │  │ Email            │ Role    │ Status │ Last Login   ││
│  │          │  ├──────────────────┼─────────┼────────┼──────────────┤│
│  │          │  │ ░░░░░░░░░░░░░░░ │ ░░░░░░  │ ░░░░░  │ ░░░░░░░░░░  ││
│  │          │  │ ░░░░░░░░░░░░░░░ │ ░░░░░░  │ ░░░░░  │ ░░░░░░░░░░  ││
│  │          │  │ ░░░░░░░░░░░░░░░ │ ░░░░░░  │ ░░░░░  │ ░░░░░░░░░░  ││
│  │          │  └────────────────────────────────────────────────────┘│
```

### Empty State

```
│  │          │  ┌────────────────────────────────────────────────────┐│
│  │          │  │                                                    ││
│  │          │  │          No users match your filters.              ││
│  │          │  │          Try adjusting your search.                ││
│  │          │  │                                                    ││
│  │          │  └────────────────────────────────────────────────────┘│
```

---

## Screen 9: Admin — User Detail

```
┌──────────────────────────────────────────────────────────────────────┐
│  ┌──────────┐                                                        │
│  │ Sidebar  │  ← Back to Users                                       │
│  │          │                                                        │
│  │          │  User Detail                                           │
│  │          │  ────────────────────────────────────────────────────── │
│  │          │                                                        │
│  │          │  Account Information                                   │
│  │          │  ┌────────────────────────────────────────────────────┐│
│  │          │  │ Email          alice@acme.com                      ││
│  │          │  │ Role           TENANT_OWNER                        ││
│  │          │  │ Status         ● ACTIVE                            ││
│  │          │  │ 2FA            ✓ Enabled                           ││
│  │          │  │ Created        2026-03-01                          ││
│  │          │  │ Last Login     2026-03-08 14:30 UTC                ││
│  │          │  │ Last Login IP  192.168.1.100                       ││
│  │          │  └────────────────────────────────────────────────────┘│
│  │          │                                                        │
│  │          │  Actions                                               │
│  │          │  ┌────────────────────────────────────────────────────┐│
│  │          │  │                                                    ││
│  │          │  │  [Deactivate Account]     [Reset 2FA]             ││
│  │          │  │                                                    ││
│  │          │  └────────────────────────────────────────────────────┘│
│  │          │                                                        │
│  └──────────┘                                                        │
└──────────────────────────────────────────────────────────────────────┘
```

### Conditional Actions by Status

| User Status | Available Actions |
|-------------|-------------------|
| `ACTIVE` | [Deactivate Account], [Reset 2FA] (if 2FA enabled) |
| `LOCKED` | [Unlock Account], [Deactivate Account] |
| `PASSWORD_EXPIRED` | [Deactivate Account], [Reset 2FA] (if 2FA enabled) |
| `PENDING_2FA` | [Deactivate Account] |
| `DEACTIVATED` | [Re-enable Account] |

### Status Badge Colors

| Status | Color | Label |
|--------|-------|-------|
| `ACTIVE` | Green | ● Active |
| `LOCKED` | Orange | ● Locked |
| `PASSWORD_EXPIRED` | Yellow | ● Password Expired |
| `PENDING_2FA` | Blue | ● Pending 2FA |
| `DEACTIVATED` | Red | ● Deactivated |

### Confirmation Dialog (Destructive Actions)

```
┌────────────────────────────────────────────┐
│  Deactivate account?                       │
├────────────────────────────────────────────┤
│                                            │
│  alice@acme.com will no longer be able     │
│  to log in. Their data will be preserved.  │
│                                            │
│  ┌─────────────┐ ┌────────────────────┐   │
│  │   Cancel     │ │   Deactivate       │   │
│  └─────────────┘ └────────────────────┘   │
│                                            │
└────────────────────────────────────────────┘
```

---

## Screen 10: Forgot Password

See Screen 6 above (under Auth Layout section).

---

## Shared Components

| Component | Used In | Description |
|-----------|---------|-------------|
| `PasswordInput` | Register, Change Password | Password field with show/hide toggle, strength meter, checklist |
| `OTPInput` | 2FA Setup, 2FA Verify | 6-digit code input with auto-advance between boxes |
| `AuthCard` | All /auth/* screens | Centered card with logo header and footer |
| `AlertBanner` | Login, 2FA, Change Password | Inline warning/error/info banner (colored by severity) |
| `ConfirmDialog` | Admin User Detail | Modal dialog for destructive action confirmation |
| `StatusBadge` | Admin User List, User Detail | Colored pill badge showing account status |
| `DataTable` | Admin User List | Sortable, filterable table with pagination |
| `PasswordStrength` | Register, Change Password | Visual strength indicator + checklist |
