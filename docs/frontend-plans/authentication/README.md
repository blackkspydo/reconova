# Authentication & Account Security — Frontend Plan

Scope: All self-service auth flows (registration, login, 2FA, password management, settings) and super admin user management for the Reconova platform.

## Documentation Index

| File | Description | Audience |
|------|-------------|----------|
| [user-flows.md](./user-flows.md) | User journey flowcharts + branching logic | Product, QA, Frontend |
| [screens-wireframes.md](./screens-wireframes.md) | ASCII wireframes for all 10 screens | UI/UX, Frontend |
| [implementation-guide.md](./implementation-guide.md) | State management, API integration, components | Frontend devs |
| [reference.md](./reference.md) | Error codes, validation rules, security | Frontend devs |

---

## Business Rule Coverage

| BR Rule | Name | Frontend Feature |
|---------|------|-----------------|
| BR-AUTH-001 | Password Requirements | Registration form + change password validation |
| BR-AUTH-004 | Password Rotation | Login flow → expired password redirect |
| BR-AUTH-005 | Login Flow | Login page with multi-branch handling |
| BR-AUTH-009 | Session Deletion on Password Change | Change password → auto-logout all sessions |
| BR-AUTH-013 | Token Refresh | API client interceptor (silent refresh) |
| BR-AUTH-015 | Super Admin Audit | Admin panel audit visibility |
| BR-AUTH-2FA-SETUP | TOTP Enrollment | 2FA setup page (QR + manual + verify) |
| BR-AUTH-2FA-LOGIN | TOTP Verify at Login | 2FA verify page |
| BR-AUTH-2FA-RESET | Admin 2FA Reset | Admin user detail → reset 2FA action |
| BR-AUTH-TEMP | Temp Token Handling | Passed via redirect state for 2FA/password flows |
| BR-AUTH-CLEANUP | Session GC | No frontend impact (backend-only) |

---

## User Roles

| Role | Auth Screens | Admin Screens |
|------|-------------|---------------|
| `TENANT_OWNER` | Register, Login, 2FA Setup, 2FA Verify, Change Password, Settings | — |
| `SUPER_ADMIN` | Login, 2FA Verify, Change Password, Settings | User List, User Detail (deactivate/unlock/reset 2FA) |

---

## Account State Machine

```
                    ┌─────────────────────────────────────────────────────────────┐
                    │                   ACCOUNT STATE MACHINE                     │
                    └─────────────────────────────────────────────────────────────┘

     ┌───────────────┐
     │   (new user)  │
     └───────┬───────┘
             │ Registration complete
             ▼
     ┌───────────────┐         ┌───────────────┐
     │  PENDING_2FA  │────────►│    ACTIVE      │
     │               │  2FA    │                │
     │ (must enroll) │ enrolled│ (full access)  │
     └───────┬───────┘         └──┬──────┬──────┘
             │                    │      │
             │ Super admin        │      │ 3 failed logins
             │ deactivates        │      ▼
             │              ┌─────┘  ┌───────────────┐
             │              │        │    LOCKED      │
             ▼              │        │ (1hr cooldown) │
     ┌───────────────┐      │        └───────┬───────┘
     │  DEACTIVATED  │◄─────┤               │ Auto-unlock after 1hr
     │               │      │               │ or super admin unlock
     │ (no access)   │      │               ▼
     └───────────────┘      │        ┌───────────────┐
             ▲              │        │    ACTIVE      │
             │              │        └───────────────┘
             │              │
             │              │ 90 days since password change
             │              ▼
             │       ┌───────────────────┐
             │       │ PASSWORD_EXPIRED  │
             │       │ (must change pwd) │
             └───────┴───────────────────┘
```

## State Transitions Table

| Current State | Action | Next State | Trigger | Who |
|--------------|--------|------------|---------|-----|
| _(new)_ | Registration complete | `PENDING_2FA` | Self-service registration | User |
| `PENDING_2FA` | 2FA enrolled & verified | `ACTIVE` | TOTP verification on setup page | User |
| `PENDING_2FA` | Deactivated | `DEACTIVATED` | Super admin action | Super Admin |
| `ACTIVE` | 3 failed login attempts | `LOCKED` | System auto-lock | System |
| `LOCKED` | Lockout expires (1hr) | `ACTIVE` | Auto-unlock on next login attempt | System |
| `LOCKED` | Manual unlock | `ACTIVE` | Super admin action | Super Admin |
| `ACTIVE` | 90 days since password change | `PASSWORD_EXPIRED` | System check on login | System |
| `PASSWORD_EXPIRED` | New password set | `ACTIVE` | User changes password | User |
| `ACTIVE` | Deactivated | `DEACTIVATED` | Super admin action | Super Admin |
| `DEACTIVATED` | Re-enabled | `ACTIVE` | Super admin action | Super Admin |

---

## Screen Navigation Map

```
                                    ┌──────────────┐
                                    │     ROOT     │
                                    │   /          │
                                    └──────┬───────┘
                                           │
                         ┌─────────────────┼─────────────────┐
                         │                 │                 │
                         ▼                 ▼                 ▼
                  ┌────────────┐    ┌────────────┐    ┌────────────┐
                  │   LOGIN    │◄──►│  REGISTER  │    │ DASHBOARD  │
                  │ /auth/login│    │/auth/       │    │ /(app)/    │
                  └─────┬──────┘    │ register   │    │ dashboard  │
                        │           └─────┬──────┘    └──────┬─────┘
       ┌────────────────┼─────────────────┤                  │
       │                │                 │                  │
       ▼                ▼                 ▼                  ▼
┌────────────┐   ┌────────────┐   ┌────────────┐     ┌────────────┐
│  CHANGE    │   │  2FA       │   │  2FA       │     │  SETTINGS  │
│  PASSWORD  │   │  VERIFY    │   │  SETUP     │     │ /(app)/    │
│ /auth/     │   │ /auth/     │   │ /auth/     │     │ settings   │
│ change-pwd │   │ 2fa-verify │   │ 2fa-setup  │     └──────┬─────┘
└────────────┘   └─────┬──────┘   └─────┬──────┘            │
                       │                 │                   │
                       ▼                 ▼              (super admin only)
                  ┌────────────┐   ┌────────────┐     ┌────────────┐
                  │ DASHBOARD  │   │ DASHBOARD  │     │ USER MGMT  │
                  │ (success)  │   │ (success)  │     │ /(admin)/  │
                  └────────────┘   └────────────┘     │ users      │
                                                      └──────┬─────┘
                                                             │
                                                      ┌────────────┐
                                                      │ USER DETAIL│
                                                      │ /(admin)/  │
                                                      │ users/:id  │
                                                      └────────────┘
```

---

## Screens Summary

| # | Screen | Route | Access |
|---|--------|-------|--------|
| 1 | Registration (2-step wizard) | `/auth/register` | Public |
| 2 | Login | `/auth/login` | Public |
| 3 | 2FA Setup | `/auth/2fa-setup` | Temp token |
| 4 | 2FA Verify | `/auth/2fa-verify` | Temp token |
| 5 | Change Password | `/auth/change-password` | Temp token or authenticated |
| 6 | Account Settings | `/(app)/settings` | Authenticated |
| 7 | Dashboard (landing) | `/(app)/dashboard` | Authenticated |
| 8 | Admin User List | `/(admin)/users` | Super Admin |
| 9 | Admin User Detail | `/(admin)/users/:id` | Super Admin |
| 10 | Forgot Password | `/auth/forgot-password` | Public |

**Note:** Forgot password is implied by business rules (password change endpoint with temp token) but not explicitly documented. Included for completeness.

---

**Document Version:** 1.0
**Last Updated:** March 2026
**Based On:** `docs/plans/business-rules/01-authentication-account-security.md`
