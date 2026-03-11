# Implementation Guide (Billing & Credits)

Scope: State management, API integration, component architecture, and build checklist for all billing & credits screens.

---

## State Management

### Subscription State

```typescript
interface SubscriptionPlan {
  id: string;
  name: string;
  monthly_credits: number;
  max_domains: number;
  price_monthly: number;
  price_annual: number;
  features_json: Record<string, boolean>;
  status: 'ACTIVE' | 'DEPRECATED' | 'ARCHIVED';
}

interface TenantSubscription {
  id: string;
  tenant_id: string;
  plan_id: string;
  plan: SubscriptionPlan;
  stripe_subscription_id: string | null;
  stripe_customer_id: string | null;
  status: 'ACTIVE' | 'PAST_DUE' | 'CANCELLED' | 'EXPIRED';
  billing_interval: 'MONTHLY' | 'ANNUAL';
  current_period_start: string;
  current_period_end: string | null;
  credits_remaining: number;
  credits_used_this_period: number;
  pending_plan_id: string | null;
  pending_plan: SubscriptionPlan | null;
}
```

### Credit State

```typescript
interface CreditBalance {
  allotment_remaining: number;
  allotment_total: number;
  purchased_balance: number;
  total_available: number;
  used_this_period: number;
  resets_at: string | null;
}

interface CreditTransaction {
  id: string;
  tenant_id: string;
  amount: number;
  type: 'ALLOTMENT' | 'CONSUMPTION' | 'PURCHASE' | 'REFUND' | 'ADJUSTMENT';
  scan_job_id: string | null;
  description: string | null;
  created_by: string | null;
  created_at: string;
}

interface CreditPack {
  id: string;
  name: string;
  credits: number;
  price: number;
  status: 'ACTIVE' | 'ARCHIVED';
}
```

### Credit Estimate State (Scan Context)

```typescript
interface CreditEstimate {
  estimated_cost: number;
  available_credits: number;
  sufficient: boolean;
  shortfall: number;
  breakdown: CreditEstimateStep[];
}

interface CreditEstimateStep {
  check_type: string;
  credits_per_domain: number;
  domain_count: number;
  subtotal: number;
}
```

### Scan Pricing State (Admin)

```typescript
interface ScanStepPricing {
  id: string;
  check_type: string;
  tier_id: string;
  credits_per_domain: number;
  description: string | null;
  updated_by: string | null;
  updated_at: string;
}

interface PricingMatrixCell {
  check_type: string;
  tier_id: string;
  tier_name: string;
  credits_per_domain: number;
  available: boolean; // false = N/A for this tier
  modified: boolean;  // local edit, unsaved
}
```

### Billing Page State

```typescript
interface BillingPageState {
  subscription: TenantSubscription | null;
  creditBalance: CreditBalance | null;
  plans: SubscriptionPlan[];
  isLoading: boolean;
  error: string | null;
}

interface PlanComparisonState {
  plans: SubscriptionPlan[];
  currentPlanId: string | null;
  selectedInterval: 'MONTHLY' | 'ANNUAL';
  isLoading: boolean;
}

interface TransactionHistoryState {
  transactions: CreditTransaction[];
  totalCount: number;
  currentPage: number;
  pageSize: number;
  typeFilter: CreditTransaction['type'] | 'ALL';
  dateFilter: 'THIS_PERIOD' | 'LAST_PERIOD' | 'LAST_90_DAYS' | 'ALL_TIME';
  isLoading: boolean;
}

interface CreditPacksState {
  packs: CreditPack[];
  isLoading: boolean;
  purchaseInProgress: string | null; // pack ID being purchased
}
```

### Admin State

```typescript
interface AdminCreditAdjustmentState {
  tenantSearchQuery: string;
  tenantSearchResults: TenantSummary[];
  selectedTenant: TenantSummary | null;
  tenantCreditBalance: CreditBalance | null;
  form: {
    amount: number | null;
    reason: string;
  };
  isSubmitting: boolean;
  errors: Record<string, string>;
}

interface TenantSummary {
  id: string;
  name: string;
  plan_name: string;
  subscription_status: string;
}

interface AdminPricingState {
  matrix: PricingMatrixCell[];
  plans: SubscriptionPlan[];
  hasUnsavedChanges: boolean;
  isLoading: boolean;
  isSaving: boolean;
}
```

### Form State Pattern

```typescript
interface BillingFormState<T> {
  values: T;
  errors: Record<keyof T, string | null>;
  touched: Record<keyof T, boolean>;
  isSubmitting: boolean;
  isValid: boolean;
}
```

### Global Billing State (Header Widget + Banners)

```typescript
interface GlobalBillingState {
  creditBalance: CreditBalance | null;
  subscriptionStatus: TenantSubscription['status'] | null;
  pendingPlanName: string | null;
  periodEndDate: string | null;
  hasSubscription: boolean;
  isLoading: boolean;
}

type BillingBannerType = 'PAST_DUE' | 'FREE_TIER' | 'CANCELLED' | 'LOW_CREDITS' | null;

// Derived: highest priority banner to show
function getActiveBanner(state: GlobalBillingState): BillingBannerType {
  if (state.subscriptionStatus === 'PAST_DUE') return 'PAST_DUE';
  if (!state.hasSubscription) return 'FREE_TIER';
  if (state.subscriptionStatus === 'CANCELLED') return 'CANCELLED';
  if (state.creditBalance &&
      state.creditBalance.allotment_remaining < state.creditBalance.allotment_total * 0.2)
    return 'LOW_CREDITS';
  return null;
}
```

---

## API Integration

### Endpoint Table

| Method | Path | Description | Request | Response |
|--------|------|-------------|---------|----------|
| `GET` | `/api/billing/plans` | List available plans | — | `SubscriptionPlan[]` |
| `GET` | `/api/billing/subscription` | Get current subscription | — | `TenantSubscription \| null` |
| `GET` | `/api/billing/credits` | Get credit balance | — | `CreditBalance` |
| `GET` | `/api/billing/credits/transactions` | List transactions | `?type=&date=&page=&size=` | `PaginatedResponse<CreditTransaction>` |
| `POST` | `/api/billing/checkout` | Create Stripe checkout (subscribe/upgrade) | `CheckoutRequest` | `{ checkout_url: string }` |
| `POST` | `/api/billing/downgrade` | Schedule plan downgrade | `{ plan_id: string }` | `TenantSubscription` |
| `DELETE` | `/api/billing/pending-downgrade` | Cancel pending downgrade | — | `TenantSubscription` |
| `POST` | `/api/billing/cancel` | Cancel subscription | — | `TenantSubscription` |
| `GET` | `/api/billing/portal` | Get Stripe portal URL | — | `{ portal_url: string }` |
| `GET` | `/api/billing/credit-packs` | List available credit packs | — | `CreditPack[]` |
| `POST` | `/api/billing/credits/purchase` | Initiate credit pack purchase | `{ credit_pack_id: string }` | `{ checkout_url: string }` |
| `POST` | `/api/billing/credits/estimate` | Estimate scan cost | `CreditEstimateRequest` | `CreditEstimate` |
| `PUT` | `/api/admin/tenants/{id}/credits` | Adjust tenant credits | `CreditAdjustmentRequest` | `CreditTransaction` |
| `GET` | `/api/admin/billing/pricing` | Get pricing matrix | — | `ScanStepPricing[]` |
| `PUT` | `/api/admin/billing/pricing` | Update pricing | `PricingUpdateRequest` | `ScanStepPricing[]` |
| `GET` | `/api/admin/tenants` | Search tenants | `?q=` | `TenantSummary[]` |

### Request Types

```typescript
interface CheckoutRequest {
  plan_id: string;
  billing_interval: 'MONTHLY' | 'ANNUAL';
}

interface CreditEstimateRequest {
  workflow_steps: string[];
  domain_count: number;
}

interface CreditAdjustmentRequest {
  amount: number;
  reason: string;
}

interface PricingUpdateRequest {
  updates: {
    check_type: string;
    tier_id: string;
    credits_per_domain: number;
  }[];
}
```

### Response Types

```typescript
interface PaginatedResponse<T> {
  data: T[];
  total: number;
  page: number;
  page_size: number;
  total_pages: number;
}

interface ApiError {
  error: {
    code: string;        // e.g., "ERR_BILL_007"
    message: string;
    details?: Record<string, unknown>; // e.g., { required: 42, available: 28, shortfall: 14 }
  };
}
```

### Stripe Redirect Pattern

```typescript
// Subscribe / Upgrade / Credit Pack Purchase
async function initiateStripeCheckout(endpoint: string, body: unknown): Promise<void> {
  const response = await api.post(endpoint, body);
  // Redirect to Stripe-hosted checkout
  window.location.href = response.checkout_url;
  // Stripe redirects back to:
  //   Success: /settings/billing?status=success
  //   Cancel:  /settings/billing?status=cancelled
}

// Payment method management
async function openStripePortal(): Promise<void> {
  const response = await api.get('/api/billing/portal');
  // Open Stripe portal in same tab
  window.location.href = response.portal_url;
}
```

### Return URL Handling

```typescript
// On billing page mount, check for Stripe redirect status
function handleStripeReturn(searchParams: URLSearchParams): void {
  const status = searchParams.get('status');

  switch (status) {
    case 'success':
      showToast('success', 'Subscription activated! Credits are ready to use.');
      break;
    case 'upgraded':
      showToast('success', 'Plan upgraded! Credits have been reset.');
      break;
    case 'credits_purchased':
      showToast('success', 'Credits added to your balance!');
      break;
    case 'cancelled':
      showToast('info', 'Checkout was not completed. You can try again anytime.');
      break;
  }

  // Clean URL params after handling
  removeSearchParams(['status']);
  // Refresh billing data
  refetchBillingData();
}
```

### Caching & Refresh Strategy

| Data | Cache Strategy | Invalidation |
|------|---------------|--------------|
| Plans list | Cache for session | Rarely changes |
| Subscription | Cache, refresh on page visit | After Stripe redirect, plan change |
| Credit balance | Cache, refresh on navigation | After scan, purchase, Stripe redirect |
| Transactions | No cache (paginated) | — |
| Credit packs | Cache for session | Rarely changes |
| Admin pricing | No cache | After save |

---

## Component Architecture

### Component Tree

```
<App>
├── <GlobalHeader>
│   └── <CreditBalanceWidget />           # Always visible
│
├── <BillingBanner />                      # Conditional global banner
│
├── <SettingsBillingPage>                   # /settings/billing
│   ├── <BillingOverview>
│   │   ├── <CurrentPlanCard />
│   │   ├── <CreditBalanceCard />
│   │   └── <QuickActionsCard />
│   │
│   ├── <PlanComparisonPage>               # /settings/billing/plans
│   │   ├── <BillingIntervalToggle />
│   │   ├── <PlanCard />                   # × 3 (Starter, Pro, Enterprise)
│   │   ├── <FeatureComparisonTable />
│   │   ├── <UpgradeConfirmationModal />
│   │   └── <DowngradeConfirmationModal />
│   │
│   ├── <TransactionHistoryPage>           # /settings/billing/transactions
│   │   ├── <TransactionFilters />
│   │   ├── <TransactionTable />
│   │   └── <Pagination />
│   │
│   └── <CreditPacksPage>                  # /settings/billing/credit-packs
│       ├── <CreditPackCard />             # × N
│       └── <PurchaseConfirmationModal />
│
├── <CancellationModal />                  # Triggered from billing overview
│
├── <InsufficientCreditsModal />           # Triggered from scan creation
│
└── <AdminBillingPages>                    # /admin/billing/*
    ├── <AdminCreditAdjustment>            # /admin/billing/credits
    │   ├── <TenantSearch />
    │   ├── <TenantCreditSummary />
    │   ├── <CreditAdjustmentForm />
    │   └── <DeductionConfirmationModal />
    │
    └── <AdminScanPricing>                 # /admin/billing/pricing
        ├── <PricingMatrix />
        ├── <PricingCell />                # Inline editable
        └── <PricingSaveConfirmationModal />
```

### Key Component Specifications

#### `<CreditBalanceWidget />`

| Prop | Type | Description |
|------|------|-------------|
| — | — | Fetches from global billing state |

| Responsibility |
|---------------|
| Display compact credit count in header |
| Color-code based on remaining % (green >50%, yellow 20-50%, red <20%) |
| Show [Upgrade] button for free tier users |
| Navigate to `/settings/billing` on click |
| Show dash `—` on loading/error |

---

#### `<BillingBanner />`

| Prop | Type | Description |
|------|------|-------------|
| — | — | Derives banner type from global billing state |

| Responsibility |
|---------------|
| Show highest-priority banner (past due > free tier > cancelled > low credits) |
| Render appropriate CTA button per banner type |
| Dismissible for low credits only (others persist) |

---

#### `<CurrentPlanCard />`

| Prop | Type | Description |
|------|------|-------------|
| `subscription` | `TenantSubscription \| null` | Current subscription |
| `onChangePlan` | `() => void` | Navigate to plan comparison |
| `onCancel` | `() => void` | Open cancellation modal |
| `onCancelDowngrade` | `() => void` | Cancel pending downgrade |

| Responsibility |
|---------------|
| Display plan name, price, billing interval |
| Show next billing/cancellation date |
| Show pending downgrade notice if applicable |
| Disable actions based on subscription status |
| Show free tier CTA when no subscription |

---

#### `<CreditBalanceCard />`

| Prop | Type | Description |
|------|------|-------------|
| `balance` | `CreditBalance` | Credit balance breakdown |
| `onPurchase` | `() => void` | Navigate to credit packs |
| `onViewHistory` | `() => void` | Navigate to transactions |

| Responsibility |
|---------------|
| Display progress bar (allotment used vs total) |
| Show allotment + purchased breakdown |
| Show total available |
| Show used this period and reset date |

---

#### `<PlanCard />`

| Prop | Type | Description |
|------|------|-------------|
| `plan` | `SubscriptionPlan` | Plan details |
| `currentPlanId` | `string \| null` | Currently active plan |
| `interval` | `'MONTHLY' \| 'ANNUAL'` | Selected billing interval |
| `subscriptionStatus` | `string \| null` | Current subscription status |
| `onSelect` | `(planId: string, action: 'upgrade' \| 'downgrade' \| 'subscribe') => void` | Plan selection handler |

| Responsibility |
|---------------|
| Display plan name, price, key features |
| Show "Current Plan" badge if active |
| Show appropriate CTA: [Select], [Upgrade], [Downgrade], [Contact Sales] |
| Show annual savings when annual interval selected |

---

#### `<PricingMatrix />`

| Prop | Type | Description |
|------|------|-------------|
| `matrix` | `PricingMatrixCell[]` | Full pricing matrix |
| `plans` | `SubscriptionPlan[]` | Plan tiers for column headers |
| `onCellChange` | `(checkType: string, tierId: string, value: number) => void` | Cell edit handler |
| `onSave` | `(changes: PricingUpdateRequest) => void` | Save handler |
| `onDiscard` | `() => void` | Discard changes |
| `hasUnsavedChanges` | `boolean` | Show save/discard buttons |

| Responsibility |
|---------------|
| Render check types as rows, plan tiers as columns |
| Inline-editable number inputs for available cells |
| Show N/A for unavailable tier/check combinations |
| Track modified cells visually |
| Show [Save Changes] and [Discard] when changes exist |

---

### Shared / Reusable Components

| Component | Used By | Notes |
|-----------|---------|-------|
| `<Modal />` | All confirmation modals | Standard modal with title, body, actions |
| `<Toast />` | All success/error notifications | Global toast provider |
| `<Pagination />` | Transaction history | Page controls with count |
| `<SkeletonLoader />` | All loading states | Configurable rows/shape |
| `<SearchInput />` | Admin tenant search | Debounced search with results dropdown |
| `<Banner />` | All global banners | Type-based styling (info, warning, error) |
| `<ProgressBar />` | Credit balance display | Configurable fill color thresholds |
| `<EmptyState />` | Transaction history, credit packs | Icon + message + optional CTA |

---

## File Structure

### NEW Files

```
src/
├── pages/
│   ├── settings/
│   │   └── billing/
│   │       ├── BillingOverviewPage.tsx
│   │       ├── PlanComparisonPage.tsx
│   │       ├── TransactionHistoryPage.tsx
│   │       └── CreditPacksPage.tsx
│   └── admin/
│       └── billing/
│           ├── AdminCreditAdjustmentPage.tsx
│           └── AdminScanPricingPage.tsx
│
├── components/
│   └── billing/
│       ├── CreditBalanceWidget.tsx
│       ├── BillingBanner.tsx
│       ├── CurrentPlanCard.tsx
│       ├── CreditBalanceCard.tsx
│       ├── QuickActionsCard.tsx
│       ├── PlanCard.tsx
│       ├── FeatureComparisonTable.tsx
│       ├── BillingIntervalToggle.tsx
│       ├── TransactionFilters.tsx
│       ├── TransactionTable.tsx
│       ├── CreditPackCard.tsx
│       ├── PricingMatrix.tsx
│       ├── PricingCell.tsx
│       ├── TenantSearch.tsx
│       ├── TenantCreditSummary.tsx
│       ├── CreditAdjustmentForm.tsx
│       ├── UpgradeConfirmationModal.tsx
│       ├── DowngradeConfirmationModal.tsx
│       ├── CancellationModal.tsx
│       ├── PurchaseConfirmationModal.tsx
│       ├── DeductionConfirmationModal.tsx
│       ├── PricingSaveConfirmationModal.tsx
│       └── InsufficientCreditsModal.tsx
│
├── hooks/
│   └── billing/
│       ├── useSubscription.ts
│       ├── useCreditBalance.ts
│       ├── usePlans.ts
│       ├── useTransactionHistory.ts
│       ├── useCreditPacks.ts
│       ├── useCreditEstimate.ts
│       ├── useAdminCreditAdjustment.ts
│       └── useAdminPricing.ts
│
├── api/
│   └── billing.ts                      # All billing API calls
│
└── types/
    └── billing.ts                      # All billing TypeScript types
```

### EXISTING Files to Modify

| File | Change |
|------|--------|
| `src/components/layout/GlobalHeader.tsx` | Add `<CreditBalanceWidget />` |
| `src/components/layout/AppLayout.tsx` | Add `<BillingBanner />` above main content |
| `src/router.tsx` (or equivalent) | Add billing routes |
| `src/pages/scans/ScanCreationPage.tsx` | Integrate `<InsufficientCreditsModal />` and cost estimate |

---

## Build Checklist

Build in this order to ensure dependencies are satisfied:

1. **Types & API layer**
   - [ ] Define all TypeScript types in `types/billing.ts`
   - [ ] Implement API client functions in `api/billing.ts`

2. **Global components (needed by all pages)**
   - [ ] `<CreditBalanceWidget />` — header credit display
   - [ ] `<BillingBanner />` — global status banners
   - [ ] Integrate both into app layout

3. **Billing Overview page**
   - [ ] `useSubscription` and `useCreditBalance` hooks
   - [ ] `<CurrentPlanCard />`
   - [ ] `<CreditBalanceCard />`
   - [ ] `<QuickActionsCard />`
   - [ ] `<BillingOverviewPage />` — compose cards, handle Stripe return URLs
   - [ ] `<CancellationModal />`

4. **Plan Comparison page**
   - [ ] `usePlans` hook
   - [ ] `<BillingIntervalToggle />`
   - [ ] `<PlanCard />` (×3)
   - [ ] `<FeatureComparisonTable />`
   - [ ] `<UpgradeConfirmationModal />`
   - [ ] `<DowngradeConfirmationModal />`
   - [ ] `<PlanComparisonPage />` — compose all, handle Stripe checkout redirect

5. **Transaction History page**
   - [ ] `useTransactionHistory` hook (pagination + filters)
   - [ ] `<TransactionFilters />`
   - [ ] `<TransactionTable />`
   - [ ] `<TransactionHistoryPage />`

6. **Credit Packs page**
   - [ ] `useCreditPacks` hook
   - [ ] `<CreditPackCard />`
   - [ ] `<PurchaseConfirmationModal />`
   - [ ] `<CreditPacksPage />`

7. **Scan integration**
   - [ ] `useCreditEstimate` hook
   - [ ] `<InsufficientCreditsModal />`
   - [ ] Integrate cost estimate into scan creation flow

8. **Admin: Credit Adjustment**
   - [ ] `useAdminCreditAdjustment` hook
   - [ ] `<TenantSearch />`
   - [ ] `<TenantCreditSummary />`
   - [ ] `<CreditAdjustmentForm />`
   - [ ] `<DeductionConfirmationModal />`
   - [ ] `<AdminCreditAdjustmentPage />`

9. **Admin: Scan Pricing**
   - [ ] `useAdminPricing` hook
   - [ ] `<PricingCell />`
   - [ ] `<PricingMatrix />`
   - [ ] `<PricingSaveConfirmationModal />`
   - [ ] `<AdminScanPricingPage />`

10. **Routing & navigation**
    - [ ] Add all billing routes
    - [ ] Add Settings > Billing nav item
    - [ ] Add Admin > Billing nav items (super admin only)
    - [ ] Guard admin routes with role check

---

## Testing Notes

### Scenarios to Cover

| Area | Test Scenarios |
|------|---------------|
| **Subscription lifecycle** | Free → subscribe → upgrade → downgrade → cancel → expire → re-subscribe |
| **Credit widget** | Green/yellow/red thresholds, free tier display, loading/error states |
| **Banners** | Priority ordering, correct banner per state, dismissibility |
| **Plan comparison** | Monthly/annual toggle, current plan badge, correct CTA per plan relation |
| **Upgrade modal** | Pro-ration display, pending downgrade cancellation notice |
| **Downgrade modal** | Domain limit warning, feature diff, effective date |
| **Cancellation** | Period end date display, post-cancel state |
| **Past due** | Disabled actions, payment update link, banner |
| **Credit packs** | No subscription gate, past due gate, Stripe redirect |
| **Transaction history** | Pagination, type filter, date filter, empty state, filter-no-results |
| **Stripe redirects** | Success/cancelled/upgraded/credits_purchased status handling |
| **Admin adjustment** | Tenant search, positive/negative amounts, below-zero guard, reason required |
| **Admin pricing** | Inline edit, N/A cells non-editable, unsaved changes tracking, save confirmation |
| **Insufficient credits** | Shortfall calculation display, navigation to credit packs |
| **Role guards** | Non-owner sees read-only, non-admin blocked from admin routes |
