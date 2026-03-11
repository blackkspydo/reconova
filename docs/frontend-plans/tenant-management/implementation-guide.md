# Implementation Guide (Tenant Management)

Scope: State management, API integration, component architecture, and build checklist for the tenant management frontend.

---

## State Management Strategy

### Tenant State Model

```typescript
interface TenantState {
  tenant: Tenant | null;
  subscription: TenantSubscription | null;
  isLoading: boolean;
  error: string | null;
}

interface Tenant {
  id: string;
  name: string;
  slug: string;
  status: 'PROVISIONING' | 'ACTIVE' | 'SUSPENDED' | 'DEACTIVATED';
  plan_id: string | null;
  deletion_requested_at: string | null;
  deletion_requested_by: string | null;
  created_at: string;
}

interface TenantSubscription {
  id: string;
  tenant_id: string;
  plan_id: string;
  plan_name: string;
  plan_price: number;
  status: 'ACTIVE' | 'EXPIRED' | 'CANCELLED';
  current_period_start: string;
  current_period_end: string | null;
  credits_remaining: number;
  credits_used_this_period: number;
  monthly_credits: number;
}
```

### Dashboard State Model

```typescript
interface DashboardState {
  summary: DashboardSummary | null;
  isLoading: boolean;
  error: string | null;
}

interface DashboardSummary {
  domains: { count: number; limit: number };
  scans: { completed: number; running: number; failed: number };
  credits: { remaining: number; total: number; used_this_period: number };
  compliance: { score: number | null };
  recent_activity: ActivityItem[];
}

interface ActivityItem {
  id: string;
  type: 'scan_completed' | 'domain_added' | 'domain_deleted' | 'plan_changed' | 'credits_purchased';
  description: string;
  timestamp: string;
  metadata: Record<string, unknown>;
}
```

### Domain State Model

```typescript
interface DomainsState {
  domains: Domain[];
  isLoading: boolean;
  isAdding: boolean;
  error: string | null;
  addError: string | null;
}

interface Domain {
  id: string;
  name: string;
  created_at: string;
  last_scanned_at: string | null;
  scan_count: number;
}

interface DomainDetail extends Domain {
  scan_history: ScanSummary[];
}

interface ScanSummary {
  id: string;
  type: string;
  status: string;
  findings_count: number;
  completed_at: string;
}
```

### Billing State Model

```typescript
interface BillingState {
  subscription: TenantSubscription | null;
  credits: CreditBalance | null;
  usage: UsageBreakdown[];
  history: BillingHistoryItem[];
  plans: SubscriptionPlan[];
  isLoading: boolean;
  error: string | null;
  historyPage: number;
  historyHasMore: boolean;
}

interface CreditBalance {
  remaining: number;
  total: number;
  used_this_period: number;
}

interface UsageBreakdown {
  category: string;
  credits_used: number;
}

interface BillingHistoryItem {
  id: string;
  date: string;
  description: string;
  amount: number | null;
  status: 'paid' | 'pending' | 'failed' | 'refunded';
  invoice_url: string | null;
}

interface SubscriptionPlan {
  id: string;
  name: string;
  price: number;
  monthly_credits: number;
  max_domains: number;
  features: string[];
  is_current: boolean;
}
```

### Admin Tenant State Model

```typescript
interface AdminTenantsState {
  tenants: AdminTenantListItem[];
  isLoading: boolean;
  error: string | null;
  page: number;
  totalPages: number;
  totalCount: number;
  search: string;
  statusFilter: TenantStatusFilter;
}

type TenantStatusFilter = 'ALL' | 'ACTIVE' | 'SUSPENDED' | 'PROVISIONING' | 'DEACTIVATED';

interface AdminTenantListItem {
  id: string;
  name: string;
  slug: string;
  status: 'PROVISIONING' | 'ACTIVE' | 'SUSPENDED' | 'DEACTIVATED';
  plan_name: string | null;
  owner_email: string;
  created_at: string;
  last_active_at: string | null;
  deletion_requested_at: string | null;
}

interface AdminTenantDetail extends AdminTenantListItem {
  domains_count: number;
  domains_limit: number;
  credits_remaining: number;
  db_status: 'PROVISIONING' | 'ACTIVE' | 'FAILED';
  recent_audit_logs: AuditLogEntry[];
}

interface AuditLogEntry {
  id: string;
  action: string;
  actor_email: string;
  details: string;
  timestamp: string;
}
```

### Impersonation State Model

```typescript
interface ImpersonationState {
  isImpersonating: boolean;
  targetUser: { email: string; tenant_name: string } | null;
  expiresAt: string | null;  // ISO timestamp
  remainingSeconds: number;
}
```

### Form State Pattern

```typescript
interface FormState<T> {
  values: T;
  errors: Partial<Record<keyof T, string>>;
  touched: Partial<Record<keyof T, boolean>>;
  isSubmitting: boolean;
  isValid: boolean;
}

// Tenant name form
type TenantNameForm = FormState<{ name: string }>;

// Add domain form
type AddDomainForm = FormState<{ domain: string }>;

// Suspend tenant form
type SuspendTenantForm = FormState<{ reason: string }>;

// Delete account form
type DeleteAccountForm = FormState<{ confirmSlug: string }>;
```

---

## API Integration Patterns

### Tenant-Owner Endpoints

| Method | Path | Description | Auth | Request | Response |
|--------|------|-------------|------|---------|----------|
| GET | `/api/tenants/me` | Get current tenant | JWT | -- | `Tenant` |
| GET | `/api/tenants/me/status` | Get tenant provisioning status | Temp/JWT | -- | `{ status, db_status }` |
| PATCH | `/api/tenants/me` | Update tenant name | JWT | `{ name }` | `Tenant` |
| POST | `/api/tenants/me/deletion-request` | Request account deletion | JWT | -- | `{ message }` |
| GET | `/api/dashboard/summary` | Dashboard summary data | JWT | -- | `DashboardSummary` |
| GET | `/api/domains` | List domains | JWT | `?page&limit` | `{ data: Domain[], total }` |
| GET | `/api/domains/{id}` | Get domain detail | JWT | -- | `DomainDetail` |
| POST | `/api/domains` | Add domain | JWT | `{ name }` | `Domain` |
| DELETE | `/api/domains/{id}` | Delete domain | JWT | -- | `204` |
| GET | `/api/billing/subscription` | Current subscription | JWT | -- | `TenantSubscription` |
| GET | `/api/billing/credits` | Credit balance | JWT | -- | `CreditBalance` |
| GET | `/api/billing/usage` | Usage breakdown | JWT | -- | `UsageBreakdown[]` |
| GET | `/api/billing/history` | Billing history | JWT | `?page&limit` | `{ data: BillingHistoryItem[], total }` |
| GET | `/api/billing/plans` | Available plans | JWT | -- | `SubscriptionPlan[]` |
| POST | `/api/billing/checkout` | Create Stripe checkout | JWT | `{ plan_id }` | `{ checkout_url }` |
| POST | `/api/billing/credits/purchase` | Purchase credits | JWT | `{ amount }` | `{ checkout_url }` |

### Admin Endpoints

| Method | Path | Description | Auth | Request | Response |
|--------|------|-------------|------|---------|----------|
| GET | `/api/admin/tenants` | List tenants | JWT (admin) | `?page&limit&search&status` | `{ data: AdminTenantListItem[], total }` |
| GET | `/api/admin/tenants/{id}` | Tenant detail | JWT (admin) | -- | `AdminTenantDetail` |
| POST | `/api/admin/tenants/{id}/suspend` | Suspend tenant | JWT (admin) | `{ reason }` | `Tenant` |
| POST | `/api/admin/tenants/{id}/reactivate` | Reactivate tenant | JWT (admin) | -- | `Tenant` |
| POST | `/api/admin/tenants/{id}/deletion` | Approve/deny deletion | JWT (admin) | `{ action: 'approve' \| 'deny' }` | `Tenant` |
| POST | `/api/admin/tenants/{id}/impersonate` | Start impersonation | JWT (admin) | -- | `{ token, refresh_token, expires_in }` |
| POST | `/api/admin/tenants/{id}/retry-provisioning` | Retry failed provisioning | JWT (admin) | -- | `Tenant` |
| DELETE | `/api/admin/tenants/{id}` | Delete failed tenant | JWT (admin) | -- | `204` |

### Error Response Format

```typescript
interface ApiError {
  error: {
    code: string;       // e.g., "ERR_TNT_001"
    message: string;    // User-facing message
    details?: Record<string, unknown>;
  };
}
```

---

## Component Architecture

### Component Tree

```
App
├── AuthLayout
│   ├── ProvisioningStatusPage
│   ├── SuspendedPage
│   └── DeactivatedPage
│
├── AppLayout (authenticated shell)
│   ├── AppNavbar
│   ├── FreeTierBanner (conditional)
│   ├── ImpersonationBanner (conditional)
│   │
│   ├── DashboardPage
│   │   ├── StatCard (x4: domains, scans, credits, compliance)
│   │   ├── RecentActivityFeed
│   │   │   └── ActivityItem
│   │   └── QuickActions
│   │
│   ├── DomainsListPage
│   │   ├── AddDomainForm (inline)
│   │   ├── DomainTable
│   │   │   └── DomainRow
│   │   └── DeleteDomainDialog
│   │
│   ├── DomainDetailPage
│   │   └── ScanHistoryTable
│   │
│   ├── SettingsPage
│   │   ├── OrganizationSection
│   │   │   └── TenantNameForm
│   │   ├── AccountSection
│   │   ├── DangerZoneSection
│   │   └── DeleteAccountDialog
│   │
│   ├── BillingPage
│   │   ├── CurrentPlanCard
│   │   ├── CreditBalanceBar
│   │   ├── UsageChart
│   │   └── BillingHistoryTable
│   │
│   └── PlanComparisonPage
│       └── PlanCard (xN)
│
└── AdminLayout (admin shell)
    ├── AdminNavbar
    │
    ├── AdminTenantListPage
    │   ├── TenantSearchBar
    │   ├── StatusFilterTabs
    │   ├── TenantTable
    │   │   └── TenantRow
    │   └── Pagination
    │
    └── AdminTenantDetailPage
        ├── TenantInfoSection
        ├── PendingActionsSection (conditional)
        ├── TenantActionsSection
        │   ├── SuspendDialog
        │   ├── ReactivateDialog
        │   ├── DeletionApprovalDialog
        │   └── ImpersonateDialog
        └── TenantAuditLogPreview
```

### Key Component Specifications

**FreeTierBanner**
- Props: `planName: string`, `onUpgradeClick: () => void`
- Renders: Warning banner with upgrade CTA
- Condition: Only shown when `plan === 'free'`
- Dismissable: No (always visible on free tier)

**ImpersonationBanner**
- Props: `targetEmail: string`, `tenantName: string`, `expiresAt: string`, `onEnd: () => void`
- Renders: Warning banner with countdown timer and end button
- Condition: Only shown when `jwt.is_impersonation === true`
- Timer: Updates every second, shows mm:ss remaining
- Dismissable: No (always visible during impersonation)

**StatCard**
- Props: `title: string`, `value: string | number`, `subtitle: string`, `linkTo: string`, `linkLabel: string`, `isLoading: boolean`
- Renders: Metric card with clickable link
- Loading: Skeleton shimmer

**AddDomainForm**
- Props: `maxDomains: number`, `currentCount: number`, `isDisabled: boolean`, `onSubmit: (domain: string) => void`
- Renders: Inline text input + Add button
- Validation: Domain format regex, max domains check
- States: default, submitting, error, disabled (free tier)

**DeleteAccountDialog**
- Props: `tenantSlug: string`, `onConfirm: () => void`, `onCancel: () => void`
- Renders: Modal with slug confirmation input
- Submit enabled: Only when input matches `tenantSlug` exactly

### Shared/Reusable Components

| Component | Used By | Source |
|-----------|---------|--------|
| `Toast` | All pages | Shared UI library |
| `ConfirmDialog` | Domain delete, suspend, reactivate, impersonate | Shared UI library |
| `Pagination` | Admin tenant list, billing history | Shared UI library |
| `StatusBadge` | Admin tenant list/detail, domain list | Shared UI library |
| `SkeletonLoader` | Dashboard, domains, billing, admin | Shared UI library |
| `EmptyState` | Domains, activity feed, billing history | Shared UI library |

---

## File Structure

### NEW Files

```
src/routes/
├── auth/
│   ├── provisioning/+page.svelte         # Provisioning status
│   ├── suspended/+page.svelte            # Suspended page
│   └── deactivated/+page.svelte          # Deactivated page
│
├── (app)/
│   ├── dashboard/+page.svelte            # Dashboard
│   ├── dashboard/+page.ts                # Dashboard data loader
│   ├── domains/+page.svelte              # Domains list
│   ├── domains/+page.ts                  # Domains data loader
│   ├── domains/[id]/+page.svelte         # Domain detail
│   ├── domains/[id]/+page.ts             # Domain detail loader
│   ├── settings/+page.svelte             # Tenant settings
│   ├── settings/+page.ts                 # Settings data loader
│   ├── billing/+page.svelte              # Billing overview
│   ├── billing/+page.ts                  # Billing data loader
│   └── billing/plans/+page.svelte        # Plan comparison
│       └── billing/plans/+page.ts        # Plans data loader
│
└── (admin)/admin/
    ├── tenants/+page.svelte              # Admin tenant list
    ├── tenants/+page.ts                  # Admin tenants loader
    ├── tenants/[id]/+page.svelte         # Admin tenant detail
    └── tenants/[id]/+page.ts             # Admin tenant detail loader

src/lib/
├── components/
│   ├── banners/
│   │   ├── FreeTierBanner.svelte
│   │   └── ImpersonationBanner.svelte
│   ├── dashboard/
│   │   ├── StatCard.svelte
│   │   ├── RecentActivityFeed.svelte
│   │   └── QuickActions.svelte
│   ├── domains/
│   │   ├── AddDomainForm.svelte
│   │   ├── DomainTable.svelte
│   │   └── DeleteDomainDialog.svelte
│   ├── settings/
│   │   ├── TenantNameForm.svelte
│   │   └── DeleteAccountDialog.svelte
│   ├── billing/
│   │   ├── CurrentPlanCard.svelte
│   │   ├── CreditBalanceBar.svelte
│   │   ├── UsageChart.svelte
│   │   ├── BillingHistoryTable.svelte
│   │   └── PlanCard.svelte
│   └── admin/
│       ├── TenantTable.svelte
│       ├── TenantSearchBar.svelte
│       ├── StatusFilterTabs.svelte
│       ├── TenantInfoSection.svelte
│       ├── TenantActionsSection.svelte
│       ├── SuspendDialog.svelte
│       ├── ImpersonateDialog.svelte
│       └── TenantAuditLogPreview.svelte
│
├── stores/
│   ├── tenant.ts                         # Tenant state store
│   ├── dashboard.ts                      # Dashboard data store
│   ├── domains.ts                        # Domains state store
│   ├── billing.ts                        # Billing state store
│   ├── impersonation.ts                  # Impersonation state store
│   └── admin-tenants.ts                  # Admin tenants store
│
└── api/
    ├── tenant.ts                         # Tenant API client
    ├── dashboard.ts                      # Dashboard API client
    ├── domains.ts                        # Domains API client
    ├── billing.ts                        # Billing API client
    └── admin-tenants.ts                  # Admin tenants API client
```

### EXISTING Files to Modify

| File | Modification |
|------|-------------|
| `src/routes/(app)/+layout.svelte` | Add `FreeTierBanner` and `ImpersonationBanner` components |
| `src/lib/api/client.ts` | Add tenant status error interceptor (ERR_TNT_005/006/007 -> redirect) |
| `src/lib/stores/auth.ts` | Add impersonation state detection from JWT claims |

---

## Build Checklist

1. **API client layer** -- Create API modules for tenant, dashboard, domains, billing, admin-tenants
2. **State stores** -- Create Svelte stores for each domain (tenant, dashboard, domains, billing, impersonation, admin-tenants)
3. **Shared components** -- Build FreeTierBanner, ImpersonationBanner, StatusBadge
4. **App layout update** -- Integrate banners into `(app)/+layout.svelte`
5. **Provisioning page** -- Build polling status page with step indicators
6. **Blocked state pages** -- Build suspended and deactivated pages
7. **API error interceptor** -- Add tenant status error handling to API client
8. **Dashboard** -- Build dashboard with stat cards, activity feed, quick actions
9. **Domains list** -- Build domains page with inline add form, table, delete dialog
10. **Domain detail** -- Build detail page with scan history
11. **Settings** -- Build settings page with tenant name form, delete account flow
12. **Billing overview** -- Build billing page with plan display, credits, usage, history
13. **Plan comparison** -- Build plan comparison page with upgrade flow (Stripe redirect)
14. **Admin tenant list** -- Build admin list with search, filter, pagination
15. **Admin tenant detail** -- Build detail page with conditional action buttons and dialogs
16. **Impersonation flow** -- Build impersonate action (new tab), banner with countdown, session end

---

## Testing Notes

| Scenario | What to Verify |
|----------|---------------|
| Provisioning polling | Polls every 3s, transitions to dashboard on ACTIVE, shows error on FAILED |
| Free tier enforcement | Banner visible, write-action buttons disabled, upgrade links work |
| Impersonation banner | Countdown accurate, end button works, banner cannot be dismissed |
| Tenant name update | Validates length, updates display after save, handles errors |
| Delete account flow | Slug must match exactly, request submits, pending state shown |
| Domain add | Validates format, handles duplicate/max limit errors, updates count |
| Domain delete | Confirmation required, removes from list, updates count |
| Billing checkout | Stripe redirect works, success/cancel states handled on return |
| Admin tenant search | Debounced, filters combine with search, pagination resets on filter change |
| Admin suspend | Confirmation with reason required, status updates, action buttons refresh |
| Admin impersonation | New tab opens, impersonation tokens stored in sessionStorage, countdown works |
| Mid-session suspension | API interceptor catches ERR_TNT_005, clears state, redirects to suspended page |
| Plan comparison | Current plan marked, upgrade redirects to Stripe, correct features displayed |
