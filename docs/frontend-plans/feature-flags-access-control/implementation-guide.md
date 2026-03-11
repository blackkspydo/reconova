# Implementation Guide (Feature Flags & Access Control)

Scope: State management, API integration, component architecture, and build checklist for feature flag visibility, module-level gating, and super admin flag management.

---

## State Management

### Feature Store (Global — Core of Feature Gating)

```typescript
interface Feature {
  name: string;
  module: 'scanning' | 'compliance' | 'integrations' | 'notifications' | 'monitoring' | 'platform';
  description: string;
  enabled: boolean;
  reason: 'plan' | 'operational_disabled' | 'override' | 'default';
  required_tier: string | null;  // e.g., "Pro", "Enterprise" — null if enabled
  current_tier: string;          // e.g., "Starter", "Pro"
}

interface FeatureStore {
  features: Record<string, Feature>;   // keyed by feature name
  isLoaded: boolean;
  isLoading: boolean;
  error: string | null;
  lastFetchedAt: string | null;
}

// Derived helpers
function isEnabled(featureName: string): boolean {
  const feature = featureStore.features[featureName];
  if (!feature) return false;  // fail-safe: unknown features are locked
  return feature.enabled;
}

function getFeature(featureName: string): Feature | null {
  return featureStore.features[featureName] ?? null;
}

function getFeaturesByModule(module: Feature['module']): Feature[] {
  return Object.values(featureStore.features)
    .filter(f => f.module === module);
}

function isOperationallyDisabled(featureName: string): boolean {
  const feature = featureStore.features[featureName];
  return feature?.reason === 'operational_disabled';
}
```

### Feature Flags Page State (Tenant)

```typescript
interface FeatureFlagsPageState {
  featuresByModule: Record<string, Feature[]>;  // grouped by module
  currentTier: string;
  isLoading: boolean;
  error: string | null;
}
```

### Upgrade Modal State

```typescript
interface UpgradeModalState {
  isOpen: boolean;
  feature: Feature | null;  // the locked feature that triggered the modal
  source: string | null;    // where the modal was triggered from (for analytics)
}
```

### Admin Feature Management State

```typescript
interface AdminFeatureState {
  subscriptionFlags: SubscriptionFlagRow[];
  operationalFlags: OperationalFlag[];
  activeTab: 'subscription' | 'operational' | 'overrides';
  moduleFilter: string | null;
  searchQuery: string;
  isLoading: boolean;
  error: string | null;
}

interface SubscriptionFlagRow {
  id: string;
  name: string;
  module: string;
  description: string;
  default_enabled: boolean;
  tier_availability: Record<string, boolean>;  // { "Free": false, "Starter": true, ... }
}

interface OperationalFlag {
  id: string;
  name: string;
  module: string;
  description: string;
  enabled: boolean;
  last_changed_at: string | null;
  last_changed_by: string | null;
}
```

### Admin Tenant Override State

```typescript
interface AdminOverrideState {
  tenantSearchQuery: string;
  tenantSearchResults: TenantSummary[];
  selectedTenant: TenantSummary | null;
  tenantFeatures: TenantFeatureRow[];
  isLoading: boolean;
  error: string | null;
}

interface TenantSummary {
  id: string;
  name: string;
  plan_name: string;
  subscription_status: string;
}

interface TenantFeatureRow {
  feature_id: string;
  feature_name: string;
  module: string;
  plan_enabled: boolean;          // does their plan include this?
  override: TenantOverride | null;
  effective_enabled: boolean;     // final resolved state
}

interface TenantOverride {
  id: string;
  enabled: boolean;
  reason: string;
  overridden_by: string;          // admin user ID
  overridden_by_name: string;     // admin display name
  created_at: string;
  updated_at: string;
}
```

### Override Form State

```typescript
interface OverrideFormState {
  values: {
    enabled: boolean;
    reason: string;
  };
  errors: {
    reason: string | null;
  };
  touched: {
    reason: boolean;
  };
  isSubmitting: boolean;
  isValid: boolean;
}

// Validation
function validateOverrideForm(values: OverrideFormState['values']): Record<string, string | null> {
  return {
    reason: values.reason.length < 10
      ? 'Reason must be at least 10 characters'
      : values.reason.length > 500
        ? 'Reason must be 500 characters or fewer'
        : null,
  };
}
```

### Operational Flag Toggle State

```typescript
interface OperationalToggleState {
  isConfirmModalOpen: boolean;
  targetFlag: OperationalFlag | null;
  targetState: boolean;             // what the admin wants to toggle to
  isSubmitting: boolean;
}
```

---

## API Integration

### Endpoint Table

| Method | Path | Description | Request | Response |
|--------|------|-------------|---------|----------|
| `GET` | `/api/features` | Bulk feature evaluation (tenant) | — | `FeatureListResponse` |
| `GET` | `/api/admin/features` | List all flags (admin) | — | `AdminFeatureListResponse` |
| `GET` | `/api/admin/features/operational` | List operational flags | — | `OperationalFlag[]` |
| `PUT` | `/api/admin/features/operational/{name}` | Toggle operational flag | `{ enabled: boolean }` | `OperationalFlag` |
| `GET` | `/api/admin/tenants/{id}/features` | Get tenant feature state | — | `TenantFeatureRow[]` |
| `POST` | `/api/admin/tenants/{tid}/features/{fid}/override` | Create override | `CreateOverrideRequest` | `TenantOverride` |
| `PUT` | `/api/admin/tenants/{tid}/features/{fid}/override` | Update override | `UpdateOverrideRequest` | `TenantOverride` |
| `DELETE` | `/api/admin/tenants/{tid}/features/{fid}/override` | Delete override | — | `204 No Content` |
| `GET` | `/api/admin/tenants` | Search tenants | `?q=` | `TenantSummary[]` |

### Request Types

```typescript
interface CreateOverrideRequest {
  enabled: boolean;
  reason: string;   // min 10 chars, max 500 chars
}

interface UpdateOverrideRequest {
  enabled: boolean;
  reason: string;   // min 10 chars, max 500 chars
}
```

### Response Types

```typescript
interface FeatureListResponse {
  features: Feature[];  // uses Feature interface from feature store
}

interface AdminFeatureListResponse {
  subscription_flags: SubscriptionFlagRow[];
  operational_flags: OperationalFlag[];
}

interface ApiError {
  error: {
    code: string;        // e.g., "ERR_FLAG_003"
    message: string;
    details?: Record<string, unknown>;
  };
}
```

### Caching & Refresh Strategy

| Data | Cache Strategy | Invalidation |
|------|---------------|--------------|
| Feature store (`GET /api/features`) | In-memory, session lifetime | Plan change, Stripe redirect, token refresh, manual refresh |
| Admin flag list | No cache (admin pages) | After operational toggle |
| Admin operational flags | No cache | After toggle |
| Tenant features (admin) | No cache | After override CRUD |
| Tenant search results | No cache (debounced) | — |

### Feature Store Fetch Pattern

```typescript
// Called once on app init, and on specific triggers
async function fetchFeatures(force: boolean = false): Promise<void> {
  if (featureStore.isLoaded && !force) return;

  featureStore.isLoading = true;
  featureStore.error = null;

  try {
    const response = await api.get<FeatureListResponse>('/api/features');
    const featureMap: Record<string, Feature> = {};
    for (const feature of response.features) {
      featureMap[feature.name] = feature;
    }
    featureStore.features = featureMap;
    featureStore.isLoaded = true;
    featureStore.lastFetchedAt = new Date().toISOString();
  } catch (err) {
    featureStore.error = 'Failed to load features';
    // Fail-safe: all features remain locked (empty map)
  } finally {
    featureStore.isLoading = false;
  }
}

// Refresh triggers
function onPlanChange(): void   { fetchFeatures(true); }
function onStripeReturn(): void { fetchFeatures(true); }
function onTokenRefresh(): void { fetchFeatures(true); }
```

---

## Component Architecture

### Component Tree

```
<App>
├── <FeatureStoreProvider>               # Global context — fetches on mount
│
├── <AppSidebar>
│   ├── <SidebarNavItem feature="compliance_checks" />
│   ├── <SidebarNavItem feature="cve_monitoring" />
│   └── <SidebarNavItem feature="custom_workflows" />
│       └── uses <LockedBadge /> for locked items
│
├── <UpgradeModal />                     # Global overlay — controlled by UpgradeModalState
│
├── <SettingsFeaturesPage>               # /settings/features
│   └── <FeatureModuleGroup />           # × 5 (one per module)
│       └── <FeatureRow />               # × N per module
│           └── <LockedBadge />
│
├── <FeatureGate feature="...">          # Reusable wrapper — used across all modules
│   ├── children (if enabled)
│   ├── <LockedPlaceholder /> (if locked by plan)
│   └── <OperationalBanner /> (if disabled by operational flag)
│
└── <AdminFeaturePages>                  # /admin/features/*
    ├── <AdminFeatureOverview>           # /admin/features (subscription flags tab)
    │   ├── <ModuleFilter />
    │   ├── <SearchInput />
    │   └── <SubscriptionFlagTable />
    │
    ├── <AdminOperationalFlags>          # /admin/features/operational
    │   ├── <OperationalFlagCard />      # × 5
    │   └── <OperationalToggleModal />
    │
    └── <AdminTenantOverrides>           # /admin/features/overrides
        ├── <TenantSearch />
        ├── <TenantFeatureTable />
        │   └── <OverrideStatusCell />
        ├── <CreateOverrideModal />
        ├── <EditOverrideModal />
        └── <DeleteOverrideModal />
```

### Key Component Specifications

#### `<FeatureStoreProvider />`

| Prop | Type | Description |
|------|------|-------------|
| `children` | `ReactNode` | App content |

| Responsibility |
|---------------|
| Fetch `GET /api/features` on mount |
| Provide feature store context to all children |
| Expose `isEnabled()`, `getFeature()`, `getFeaturesByModule()` helpers |
| Re-fetch on plan change events |
| Fail-safe: all locked if fetch fails |

---

#### `<FeatureGate />`

| Prop | Type | Description |
|------|------|-------------|
| `feature` | `string` | Feature flag name to check |
| `children` | `ReactNode` | Content to render when enabled |
| `fallback` | `ReactNode?` | Optional custom fallback (defaults to `<LockedPlaceholder />`) |

| Responsibility |
|---------------|
| Check feature store for `feature` name |
| If enabled → render `children` |
| If locked (plan) → render `<LockedPlaceholder />` with upgrade CTA |
| If disabled (operational) → render `<OperationalBanner />` |
| If feature store not loaded → render nothing (or skeleton) |

---

#### `<UpgradeModal />`

| Prop | Type | Description |
|------|------|-------------|
| — | — | Controlled by global `UpgradeModalState` |

| Responsibility |
|---------------|
| Display feature name, description, required tier, current tier |
| [View Plans] CTA → navigate to `/settings/billing/plans` |
| [See All Features] → navigate to `/settings/features` |
| Close on backdrop click or ✕ button |
| Fire analytics: `locked_feature_clicked`, `upgrade_cta_clicked` |

---

#### `<LockedBadge />`

| Prop | Type | Description |
|------|------|-------------|
| `feature` | `Feature` | Feature data |
| `clickable` | `boolean` | Whether clicking opens upgrade modal (default: true) |

| Responsibility |
|---------------|
| Show `🔒 Requires {tier}` for plan-locked features |
| Show `⚠ Temporarily unavailable` for operational-disabled features |
| Show `✓ Included` for enabled features |
| On click (if locked + clickable) → open `<UpgradeModal />` |

---

#### `<OperationalBanner />`

| Prop | Type | Description |
|------|------|-------------|
| `featureName` | `string` | Feature flag name |
| `message` | `string?` | Optional custom message |

| Responsibility |
|---------------|
| Full-width warning banner at top of module page |
| Default message: "This feature is temporarily unavailable for maintenance." |
| Non-dismissible (persists until operational flag re-enabled) |
| No CTA (not a plan issue — user can't resolve it) |

---

#### `<OperationalFlagCard />`

| Prop | Type | Description |
|------|------|-------------|
| `flag` | `OperationalFlag` | Flag data |
| `onToggle` | `(flag: OperationalFlag, newState: boolean) => void` | Toggle handler |

| Responsibility |
|---------------|
| Display flag name, module, description, current status |
| Toggle switch visual (ON green / OFF red) |
| Show last changed date and by whom |
| On toggle → call `onToggle` (parent opens confirmation modal) |

---

#### `<CreateOverrideModal />` / `<EditOverrideModal />`

| Prop | Type | Description |
|------|------|-------------|
| `tenant` | `TenantSummary` | Target tenant |
| `feature` | `TenantFeatureRow` | Feature being overridden |
| `existingOverride` | `TenantOverride?` | For edit mode — pre-fill values |
| `onSubmit` | `(data: CreateOverrideRequest) => void` | Submit handler |
| `onClose` | `() => void` | Close handler |

| Responsibility |
|---------------|
| Show tenant name, feature name, module, plan status (read-only) |
| Enable/Disable radio toggle |
| Reason textarea with character count and min-length validation |
| Disable submit until form is valid (reason ≥ 10 chars) |
| Show inline validation errors |
| For edit: pre-fill with existing values, show "Current override" info |

---

#### `<DeleteOverrideModal />`

| Prop | Type | Description |
|------|------|-------------|
| `tenant` | `TenantSummary` | Target tenant |
| `feature` | `TenantFeatureRow` | Feature with override |
| `onConfirm` | `() => void` | Delete handler |
| `onClose` | `() => void` | Close handler |

| Responsibility |
|---------------|
| Show current override state and what happens after removal |
| Show "After removal" preview: will have access / will lose access |
| Red/destructive confirm button |

---

### Shared / Reusable Components

| Component | Used By | Notes |
|-----------|---------|-------|
| `<Modal />` | All confirmation/form modals | Standard modal with title, body, actions |
| `<Toast />` | All success/error notifications | Global toast provider |
| `<SearchInput />` | Admin tenant search | Debounced search with results dropdown |
| `<SkeletonLoader />` | All loading states | Configurable rows/shape |
| `<Banner />` | Operational banner, info notices | Type-based styling (info, warning, error) |
| `<EmptyState />` | No tenant selected, no overrides | Icon + message + optional CTA |
| `<TabNav />` | Admin feature management tabs | Tab navigation with active state |
| `<Badge />` | Feature status badges | Variant-based: success, warning, muted |

---

## File Structure

### NEW Files

```
src/
├── pages/
│   ├── settings/
│   │   └── features/
│   │       └── FeaturesPage.tsx
│   └── admin/
│       └── features/
│           ├── AdminFeaturesPage.tsx
│           ├── AdminOperationalFlagsPage.tsx
│           └── AdminTenantOverridesPage.tsx
│
├── components/
│   └── features/
│       ├── FeatureGate.tsx
│       ├── UpgradeModal.tsx
│       ├── LockedBadge.tsx
│       ├── LockedPlaceholder.tsx
│       ├── OperationalBanner.tsx
│       ├── FeatureModuleGroup.tsx
│       ├── FeatureRow.tsx
│       ├── SubscriptionFlagTable.tsx
│       ├── OperationalFlagCard.tsx
│       ├── OperationalToggleModal.tsx
│       ├── TenantFeatureTable.tsx
│       ├── OverrideStatusCell.tsx
│       ├── CreateOverrideModal.tsx
│       ├── EditOverrideModal.tsx
│       └── DeleteOverrideModal.tsx
│
├── providers/
│   └── FeatureStoreProvider.tsx
│
├── hooks/
│   └── features/
│       ├── useFeatureStore.ts
│       ├── useFeatureGate.ts
│       ├── useAdminFeatures.ts
│       ├── useAdminOperationalFlags.ts
│       └── useAdminTenantOverrides.ts
│
├── api/
│   └── features.ts                    # All feature flag API calls
│
└── types/
    └── features.ts                    # All feature flag TypeScript types
```

### EXISTING Files to Modify

| File | Change |
|------|--------|
| `src/App.tsx` (or root) | Wrap with `<FeatureStoreProvider>` |
| `src/components/layout/AppSidebar.tsx` | Add `<LockedBadge />` to gated nav items |
| `src/router.tsx` (or equivalent) | Add `/settings/features` and `/admin/features/*` routes |
| `src/pages/scans/ScanCreationPage.tsx` | Wrap step selector with `<FeatureGate />` per step |
| `src/pages/compliance/*` | Wrap module entry with `<FeatureGate feature="compliance_checks">` |
| `src/pages/cve/*` | Wrap module entry with `<FeatureGate feature="cve_monitoring">` |
| `src/pages/integrations/*` | Wrap per-integration sections with `<FeatureGate>` |
| `src/pages/notifications/*` | Wrap per-channel sections with `<FeatureGate>` |

---

## Build Checklist

Build in this order to ensure dependencies are satisfied:

1. **Types & API layer**
   - [ ] Define all TypeScript types in `types/features.ts`
   - [ ] Implement API client functions in `api/features.ts`

2. **Feature store (global foundation — everything depends on this)**
   - [ ] `<FeatureStoreProvider />` — context provider, fetch on mount
   - [ ] `useFeatureStore` hook — `isEnabled()`, `getFeature()`, `getFeaturesByModule()`
   - [ ] Wrap app root with provider
   - [ ] Wire refresh triggers (plan change, Stripe return, token refresh)

3. **Reusable gating components**
   - [ ] `<FeatureGate />` — wrapper component
   - [ ] `<LockedBadge />` — inline status badge
   - [ ] `<LockedPlaceholder />` — locked content replacement
   - [ ] `<OperationalBanner />` — maintenance notice
   - [ ] `<UpgradeModal />` — global upgrade prompt
   - [ ] `useFeatureGate` hook — convenience hook for components

4. **Sidebar integration**
   - [ ] Add `<LockedBadge />` to gated sidebar nav items
   - [ ] Wire click → `<UpgradeModal />` for locked items

5. **Tenant Plan & Features page**
   - [ ] `<FeatureModuleGroup />` — collapsible module section
   - [ ] `<FeatureRow />` — single feature display
   - [ ] `<FeaturesPage />` — compose grouped feature list
   - [ ] Add route `/settings/features`

6. **Module-level gating integration**
   - [ ] Wrap scan step selector with per-step `<FeatureGate />`
   - [ ] Wrap compliance module entry
   - [ ] Wrap CVE monitoring module entry
   - [ ] Wrap per-integration sections
   - [ ] Wrap per-notification channel sections

7. **Admin: Feature Overview**
   - [ ] `useAdminFeatures` hook
   - [ ] `<SubscriptionFlagTable />` — tier matrix display
   - [ ] `<ModuleFilter />` — dropdown filter
   - [ ] `<AdminFeaturesPage />` — tab layout, subscription tab

8. **Admin: Operational Flags**
   - [ ] `useAdminOperationalFlags` hook
   - [ ] `<OperationalFlagCard />` — toggle card
   - [ ] `<OperationalToggleModal />` — confirmation with impact warning
   - [ ] `<AdminOperationalFlagsPage />`

9. **Admin: Tenant Overrides**
   - [ ] `useAdminTenantOverrides` hook
   - [ ] `<TenantSearch />` (reuse from billing if exists)
   - [ ] `<TenantFeatureTable />` — feature list with override column
   - [ ] `<OverrideStatusCell />` — override badge + actions
   - [ ] `<CreateOverrideModal />`
   - [ ] `<EditOverrideModal />`
   - [ ] `<DeleteOverrideModal />`
   - [ ] `<AdminTenantOverridesPage />`

10. **Routing & navigation**
    - [ ] Add `/settings/features` route
    - [ ] Add `/admin/features`, `/admin/features/operational`, `/admin/features/overrides` routes
    - [ ] Add Settings > Plan & Features nav item
    - [ ] Add Admin > Feature Management nav items (super admin only)
    - [ ] Guard admin routes with role check

---

## Testing Notes

### Scenarios to Cover

| Area | Test Scenarios |
|------|---------------|
| **Feature store init** | Fetch on mount, fail-safe on error (all locked), re-fetch on plan change |
| **FeatureGate** | Enabled → renders children, locked → shows placeholder, operational → shows banner |
| **LockedBadge** | Correct badge per reason (plan/operational/enabled), click opens modal |
| **UpgradeModal** | Correct feature info, [View Plans] navigates, [Close] dismisses, analytics fired |
| **Sidebar gating** | Lock icons on correct items per tier, click opens upgrade modal |
| **Scan step gating** | Locked steps non-selectable, operational steps hidden, credit estimate excludes locked |
| **Plan & Features page** | Grouped by module, correct badges per tier, free tier shows upgrade banner |
| **Feature store refresh** | Refreshes after plan upgrade/downgrade, sidebar updates, locked page redirects |
| **Admin subscription flags** | All 18 flags displayed, module filter works, search filters by name |
| **Admin operational toggle** | Confirmation modal shows impact, toggle updates state, cache invalidation note |
| **Admin override CRUD** | Create with reason validation, edit pre-fills, delete shows impact preview |
| **Admin override errors** | ERR_FLAG_007 (operational), ERR_FLAG_009 (duplicate), ERR_FLAG_006 (reason short) |
| **Role guards** | Non-admin blocked from `/admin/features/*`, tenant members see read-only features page |
| **Post-downgrade** | Newly locked features show lock icons, locked page redirects to dashboard |
| **Operational + override** | Operational disabled wins even when override exists (UI shows "Temporarily unavailable") |
