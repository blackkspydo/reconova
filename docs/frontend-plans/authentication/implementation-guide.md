# Implementation Guide (Authentication & Account Security)

Scope: State management, API integration, component architecture, and build plan for all auth features in the SvelteKit frontend.

---

## State Management Strategy

### Auth Store

**File:** `frontend/src/lib/stores/auth.ts`

```typescript
interface User {
  id: string;
  email: string;
  tenantId: string;
  role: 'TENANT_OWNER' | 'SUPER_ADMIN';
  twoFactorEnabled: boolean;
  status: 'PENDING_2FA' | 'ACTIVE' | 'LOCKED' | 'PASSWORD_EXPIRED' | 'DEACTIVATED';
  lastLoginAt: string | null;
  passwordChangedAt: string | null;
  createdAt: string;
}

interface AuthState {
  user: User | null;
  isAuthenticated: boolean;
  isLoading: boolean;
  error: string | null;
}
```

**Key behaviors:**
- Auth state derived from `/api/auth/me` endpoint on app load (not from JWT parsing).
- Backend is single source of truth — frontend never decodes JWT.
- `isAuthenticated` set to `true` when `/api/auth/me` succeeds (cookie-based, no token in store).
- On 401 from any API call → clear auth state → redirect to `/auth/login`.

### Temp Token State

**File:** `frontend/src/lib/stores/auth.ts` (or SvelteKit page state)

```typescript
interface TempTokenState {
  tempToken: string | null;
  purpose: 'password_change' | '2fa_setup' | '2fa_verify' | null;
}
```

**Key behaviors:**
- Temp token passed via SvelteKit `goto()` state (not URL params, except forgot-password reset link).
- Cleared on navigation away from the target page.
- 5-min expiry enforced by backend; frontend shows "Session expired" on ERR_AUTH_014.

### Form State Pattern

Used by all auth forms (register, login, change password):

```typescript
interface FormState<T> {
  values: T;
  errors: Partial<Record<keyof T, string>>;
  touched: Partial<Record<keyof T, boolean>>;
  isSubmitting: boolean;
  submitError: string | null;
}
```

### Registration Wizard State

```typescript
interface RegisterWizardState {
  currentStep: 1 | 2;
  step1: FormState<{ email: string; password: string; confirmPassword: string }>;
  step2: FormState<{ tenantName: string }>;
}
```

**Key behaviors:**
- Step 1 data preserved when navigating to Step 2 and back.
- All data cleared on navigation away from `/auth/register`.
- Submission only on Step 2 — sends all fields in single API call.

---

## API Integration

### Auth Endpoints

| Method | Path | Description | Auth | Request | Response |
|--------|------|-------------|------|---------|----------|
| POST | `/api/auth/register` | Create account + tenant | None | `RegisterRequest` | `RegisterResponse` |
| POST | `/api/auth/login` | Authenticate | None | `LoginRequest` | `LoginResponse` |
| GET | `/api/auth/2fa/setup` | Get TOTP secret + QR | Temp token | — | `TwoFactorSetupResponse` |
| POST | `/api/auth/2fa/verify` | Verify TOTP code | Temp token | `TwoFactorVerifyRequest` | `AuthTokenResponse` |
| POST | `/api/auth/password/change` | Change password | Temp token or cookie | `ChangePasswordRequest` | `SuccessResponse` |
| POST | `/api/auth/password/forgot` | Request reset email | None | `ForgotPasswordRequest` | `SuccessResponse` |
| POST | `/api/auth/refresh` | Refresh tokens | Cookie | — | `AuthTokenResponse` |
| POST | `/api/auth/logout` | End session | Cookie | — | `SuccessResponse` |
| GET | `/api/auth/me` | Get current user | Cookie | — | `User` |

### Admin Endpoints

| Method | Path | Description | Auth | Request | Response |
|--------|------|-------------|------|---------|----------|
| GET | `/api/admin/users` | List users (paginated) | Cookie + SUPER_ADMIN | Query params | `PaginatedUsers` |
| GET | `/api/admin/users/:id` | Get user detail | Cookie + SUPER_ADMIN | — | `AdminUserDetail` |
| POST | `/api/admin/users/:id/deactivate` | Deactivate user | Cookie + SUPER_ADMIN | — | `SuccessResponse` |
| POST | `/api/admin/users/:id/enable` | Re-enable user | Cookie + SUPER_ADMIN | — | `SuccessResponse` |
| POST | `/api/admin/users/:id/unlock` | Unlock locked user | Cookie + SUPER_ADMIN | — | `SuccessResponse` |
| POST | `/api/admin/users/:id/reset-2fa` | Reset user's 2FA | Cookie + SUPER_ADMIN | — | `SuccessResponse` |

### Request/Response Types

```typescript
// --- Registration ---
interface RegisterRequest {
  email: string;
  password: string;
  tenantName: string;
}

interface RegisterResponse {
  userId: string;
  tenantId: string;
  requires2faSetup: true;
  tempToken: string;  // purpose: 2fa_setup
}

// --- Login ---
interface LoginRequest {
  email: string;
  password: string;
  totpCode?: string;
}

interface LoginResponse {
  // Cookies set by backend (httpOnly)
  // Body indicates next step:
  requiresTwoFactor?: boolean;
  requires2faSetup?: boolean;
  requiresPasswordChange?: boolean;
  tempToken?: string;  // only when redirect needed
}

// --- 2FA ---
interface TwoFactorSetupResponse {
  secret: string;   // Base32 for manual entry
  qrUri: string;    // otpauth:// URI for QR generation
}

interface TwoFactorVerifyRequest {
  totpCode: string;
}

// --- Password ---
interface ChangePasswordRequest {
  currentPassword?: string;  // only for voluntary change from settings
  newPassword: string;
}

interface ForgotPasswordRequest {
  email: string;
}

// --- Admin ---
interface AdminUserDetail {
  id: string;
  email: string;
  role: 'TENANT_OWNER' | 'SUPER_ADMIN';
  status: 'PENDING_2FA' | 'ACTIVE' | 'LOCKED' | 'PASSWORD_EXPIRED' | 'DEACTIVATED';
  twoFactorEnabled: boolean;
  createdAt: string;
  lastLoginAt: string | null;
  lastLoginIp: string | null;
  tenantId: string;
  tenantName: string;
}

interface PaginatedUsers {
  users: AdminUserDetail[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

interface GetUsersParams {
  page?: number;
  pageSize?: number;
  status?: string;
  search?: string;
  sortBy?: string;
  sortDesc?: boolean;
}

// --- Shared ---
interface SuccessResponse {
  success: true;
}

interface AuthTokenResponse {
  // Cookies set by backend
  // Body may be empty or confirm success
  success: true;
}

interface ApiError {
  code: string;       // e.g. ERR_AUTH_005
  status: number;     // HTTP status
  message: string;    // User-facing message
  requestId: string;
  timestamp: string;
  details?: Record<string, string>;  // Field-level validation errors
}
```

### API Client

**File:** `frontend/src/lib/api.ts`

```typescript
// Singleton HTTP client wrapping fetch
// - All requests include credentials: 'include' (sends httpOnly cookies)
// - No Authorization header needed (cookies are automatic)
// - Response interceptor: on 401 → attempt refresh → if still 401 → redirect to login
// - Request interceptor: content-type: application/json
// - Error responses parsed into ApiError type
```

**Key patterns:**
- `credentials: 'include'` on every fetch call (required for httpOnly cookies).
- On 401: attempt `POST /api/auth/refresh` once. If refresh also fails, clear auth state and redirect.
- Retry queue: if multiple requests fail with 401 simultaneously, only one refresh attempt; others wait and retry.
- All errors thrown as `ApiError` objects for consistent handling.

---

## Component Architecture

### Component Tree

```
frontend/src/lib/components/
├── auth/
│   ├── LoginForm.svelte              # Email + password form
│   ├── RegisterWizard.svelte         # 2-step wizard container
│   │   ├── RegisterStep1.svelte      # Credentials step
│   │   └── RegisterStep2.svelte      # Organization step
│   ├── TwoFactorSetup.svelte         # QR code + manual key + verify input
│   ├── TwoFactorVerify.svelte        # 6-digit code entry
│   ├── ChangePasswordForm.svelte     # New password + confirm (forced or voluntary)
│   └── ForgotPasswordForm.svelte     # Email input for reset
│
├── admin/
│   ├── UserTable.svelte              # Paginated user list with filters
│   ├── UserDetail.svelte             # User info card + action buttons
│   ├── UserActions.svelte            # Conditional action buttons by status
│   └── UserStatusBadge.svelte        # Colored status pill
│
├── shared/
│   ├── PasswordInput.svelte          # Password field with toggle + strength
│   ├── PasswordStrength.svelte       # Strength meter + checklist
│   ├── OTPInput.svelte               # 6-digit input with auto-advance
│   ├── AuthCard.svelte               # Centered card layout for auth pages
│   ├── AlertBanner.svelte            # Info/warning/error inline banner
│   ├── ConfirmDialog.svelte          # Confirmation modal for destructive actions
│   ├── DataTable.svelte              # Generic sortable/filterable/paginated table
│   └── StatusBadge.svelte            # Generic colored pill badge
│
└── layout/
    ├── Sidebar.svelte                # App sidebar navigation
    └── Header.svelte                 # Top header with user info + logout
```

### Key Component Specifications

**PasswordInput.svelte**
- Props: `value`, `placeholder`, `error`, `onInput`
- Features: show/hide toggle, password strength indicator (via PasswordStrength child)
- Validation: real-time strength check against BR-AUTH-001 rules

**OTPInput.svelte**
- Props: `length` (default 6), `onComplete`, `error`
- Features: individual digit boxes, auto-advance on input, auto-submit on last digit, paste support (full 6-digit paste), backspace goes to previous box
- State: array of digit values, focused index

**RegisterWizard.svelte**
- Props: none (self-contained)
- State: `currentStep`, `step1Data`, `step2Data`, `isSubmitting`, `error`
- Behavior: validates Step 1 before allowing Next, submits all data on Step 2 confirm

**TwoFactorSetup.svelte**
- Props: `tempToken`
- State: `qrUri`, `secret`, `showManualKey`, `code`, `isVerifying`, `error`
- Behavior: fetches setup data on mount, renders QR from URI client-side, verifies code via API

**UserActions.svelte**
- Props: `user: AdminUserDetail`, `onAction: (action) => void`
- Behavior: conditionally renders buttons based on `user.status` and `user.twoFactorEnabled`
- All destructive actions (deactivate, reset 2FA) trigger ConfirmDialog first

---

## Route Guards (SvelteKit Layouts)

### Auth Layout (`/auth/+layout.svelte`)
- Public pages — no auth required.
- On mount: check if user is authenticated via `/api/auth/me`.
- IF authenticated → redirect to `/(app)/dashboard`.

### App Layout (`/(app)/+layout.svelte`)
- Protected pages — requires authentication.
- On mount: call `/api/auth/me`.
- IF 401 → redirect to `/auth/login`.
- Renders sidebar + header + content slot.

### Admin Layout (`/(admin)/admin/+layout.svelte`)
- Super admin pages — requires authentication + `SUPER_ADMIN` role.
- On mount: check auth state.
- IF not authenticated → redirect to `/auth/login`.
- IF role !== `SUPER_ADMIN` → redirect to `/(app)/dashboard` with toast "Access denied."
- Renders admin sidebar + content slot.

---

## File Structure

```
frontend/src/
├── routes/
│   ├── +page.svelte                          # Root redirect → login or dashboard
│   ├── auth/
│   │   ├── +layout.svelte                    # Public layout (redirect if authed)
│   │   ├── register/+page.svelte             # USES: RegisterWizard
│   │   ├── login/+page.svelte                # USES: LoginForm
│   │   ├── 2fa-setup/+page.svelte            # USES: TwoFactorSetup
│   │   ├── 2fa-verify/+page.svelte           # USES: TwoFactorVerify
│   │   ├── change-password/+page.svelte      # USES: ChangePasswordForm
│   │   └── forgot-password/+page.svelte      # USES: ForgotPasswordForm
│   ├── (app)/
│   │   ├── +layout.svelte                    # Auth guard + sidebar + header
│   │   ├── dashboard/+page.svelte            # Landing page (stub for now)
│   │   └── settings/+page.svelte             # USES: ChangePasswordForm (voluntary)
│   └── (admin)/
│       └── admin/
│           ├── +layout.svelte                # Super admin guard
│           └── users/
│               ├── +page.svelte              # USES: UserTable
│               └── [id]/+page.svelte         # USES: UserDetail, UserActions
├── lib/
│   ├── api.ts                                # API client singleton
│   ├── stores/
│   │   └── auth.ts                           # Auth state store
│   ├── components/
│   │   ├── auth/                             # Auth-specific components
│   │   ├── admin/                            # Admin-specific components
│   │   ├── shared/                           # Reusable components
│   │   └── layout/                           # Layout components
│   ├── types/
│   │   └── auth.ts                           # All TypeScript interfaces above
│   └── utils/
│       ├── validation.ts                     # Password validation (BR-AUTH-001), email validation
│       └── qr.ts                             # QR code generation from otpauth:// URI
├── app.html
└── app.css                                   # Tailwind imports
```

---

## Build Checklist

Implementation order (each step builds on the previous):

1. **Project setup** — Initialize SvelteKit, Tailwind, TypeScript config
2. **Types** — Create `types/auth.ts` with all interfaces
3. **API client** — `api.ts` with fetch wrapper, cookie handling, 401 interceptor
4. **Auth store** — `stores/auth.ts` with user state + loading/error
5. **Validation utils** — `utils/validation.ts` (password rules from BR-AUTH-001, email format)
6. **Shared components** — AuthCard, AlertBanner, PasswordInput, PasswordStrength, OTPInput, ConfirmDialog
7. **Auth layout** — `/auth/+layout.svelte` with redirect logic
8. **Login page** — LoginForm + all error states + redirect branching
9. **Registration page** — RegisterWizard (2-step) + validation + API
10. **2FA Setup page** — QR display + manual key + verify + QR generation util
11. **2FA Verify page** — OTP input + verify API
12. **Change Password page** — Forced + voluntary variants
13. **Forgot Password page** — Email form + success state
14. **App layout** — `(app)/+layout.svelte` with auth guard, sidebar, header
15. **Settings page** — Account info + security section + inline password change
16. **Token refresh** — Background refresh logic in API client
17. **Admin layout** — `(admin)/admin/+layout.svelte` with role guard
18. **Admin User List** — UserTable with pagination, filters, search
19. **Admin User Detail** — UserDetail + UserActions with confirmation dialogs

---

## Testing Notes

### Unit Tests
- Password validation (`utils/validation.ts`): all BR-AUTH-001 rules, edge cases (exactly 12 chars, 128 chars, missing each category)
- OTPInput component: auto-advance, paste handling, backspace, auto-submit
- Auth store: state transitions, clearing on logout
- API client: 401 retry logic, refresh queueing

### Integration Tests
- Registration wizard: Step 1 → Step 2 → submit → redirect to 2FA setup
- Login: success, 2FA required, password expired, locked, deactivated, rate limited
- 2FA setup: QR display → code entry → verify → redirect to dashboard
- Change password: current password verification, reuse rejection, session clear
- Admin user actions: deactivate, unlock, re-enable, reset 2FA (with confirmation)

### E2E Tests
- Full registration flow: register → 2FA setup → dashboard
- Full login flow (with 2FA): login → 2FA verify → dashboard
- Password expiry flow: login → forced change → re-login → 2FA → dashboard
- Admin flow: login as super admin → user list → user detail → deactivate → re-enable
- Session expiry: idle for 30+ min → next action → redirect to login
