# Implementation Guide (System Configuration)

Scope: TypeScript interfaces, API integration, component tree, and build checklist for Config List, Change History, and Approval Queue screens.

---

## State Interfaces

### Config State

```typescript
// --- Config Item ---

type ConfigDataType = 'STRING' | 'INTEGER' | 'BOOLEAN' | 'DECIMAL' | 'JSON' | 'DURATION';

interface ConfigItem {
  id: string;
  key: string;
  value: string;                    // Stored as string, parsed by data_type
  data_type: ConfigDataType;
  category: string;
  description: string;
  default_value: string;
  min_value: string | null;
  max_value: string | null;
  allowed_values: string | null;    // Comma-separated if set
  is_sensitive: boolean;
  is_critical: boolean;
  requires_restart: boolean;
  updated_by: string | null;        // User ID
  updated_by_email: string | null;  // For display
  updated_at: string | null;        // ISO 8601
  pending_request: PendingRequestSummary | null;
}

interface PendingRequestSummary {
  id: string;
  new_value: string;
  requested_by_email: string;
  expires_at: string;
}

// --- Config Categories ---

interface ConfigCategory {
  key: string;                      // e.g., "auth", "billing"
  label: string;                    // e.g., "Authentication"
  config_count: number;
  critical_count: number;
}

// --- Cache Status ---

interface CacheStatus {
  healthy: boolean;
  last_invalidated_at: string | null;
  ttl_minutes: number;
}

// --- Config List Page ---

interface ConfigListState {
  configs: ConfigItem[];
  categories: ConfigCategory[];
  cache_status: CacheStatus;
  search_query: string;
  collapsed_categories: Set<string>;   // Category keys
  editing_key: string | null;          // One at a time
  edit_form: ConfigEditFormState | null;
  revealed_keys: Set<string>;          // Sensitive configs currently revealed
  pending_count: number;               // For header badge
  loading: boolean;
  error: string | null;
}

// --- Inline Edit Form ---

interface ConfigEditFormState {
  config_key: string;
  original_value: string;
  new_value: string;
  reason: string;
  is_critical: boolean;
  validation_error: string | null;
  submitting: boolean;
}
```

### Change History State

```typescript
interface ConfigHistoryEntry {
  id: string;
  config_id: string;
  config_key: string;
  config_category: string;
  old_value: string;                // Masked if sensitive
  new_value: string;                // Masked if sensitive
  changed_by: string;               // User ID
  changed_by_email: string;
  reason: string;
  changed_at: string;               // ISO 8601
  rolled_back: boolean;
  is_sensitive: boolean;
}

interface HistoryFilters {
  date_from: string | null;
  date_to: string | null;
  category: string | null;
  search: string;
}

interface HistoryPagination {
  page: number;
  per_page: number;                 // Default 25
  total_entries: number;
  total_pages: number;
}

interface ConfigHistoryState {
  entries: ConfigHistoryEntry[];
  filters: HistoryFilters;
  pagination: HistoryPagination;
  rollback_target: ConfigHistoryEntry | null;  // Entry being rolled back (modal open)
  rollback_reason: string;
  rollback_submitting: boolean;
  loading: boolean;
  error: string | null;
}
```

### Approval Queue State

```typescript
type ChangeRequestStatus = 'PENDING' | 'APPROVED' | 'REJECTED' | 'EXPIRED';

interface ChangeRequest {
  id: string;
  config_id: string;
  config_key: string;
  config_description: string;
  old_value: string;
  new_value: string;
  requested_by: string;             // User ID
  requested_by_email: string;
  reason: string;
  status: ChangeRequestStatus;
  approved_by_email: string | null;
  rejected_reason: string | null;
  requested_at: string;             // ISO 8601
  expires_at: string;
  approved_at: string | null;
  is_own_request: boolean;          // True if current admin is requester
  requires_restart: boolean;
}

interface ApprovalQueueState {
  pending_requests: ChangeRequest[];
  recent_decisions: ChangeRequest[];   // Last 20 approved/rejected/expired
  rejecting_request: ChangeRequest | null;  // Request being rejected (modal open)
  reject_reason: string;
  reject_submitting: boolean;
  approving_request_id: string | null;
  loading: boolean;
  error: string | null;
}
```

---

## API Endpoints

### Config List Endpoints

| # | Method | Endpoint | Request | Response | Use |
|---|--------|----------|---------|----------|-----|
| 1 | `GET` | `/api/admin/config` | — | `{ configs: ConfigItem[], categories: ConfigCategory[], cache_status: CacheStatus, pending_count: number }` | Load all configs |
| 2 | `PUT` | `/api/admin/config/{key}` | `{ value: string, reason: string }` | `{ config: ConfigItem }` | Update non-critical config |
| 3 | `GET` | `/api/admin/config/{key}/reveal` | — | `{ value: string }` | Reveal sensitive config value |
| 4 | `POST` | `/api/admin/config/cache/invalidate` | `{}` | `{ cache_status: CacheStatus }` | Invalidate config cache |

### Change History Endpoints

| # | Method | Endpoint | Request | Response | Use |
|---|--------|----------|---------|----------|-----|
| 5 | `GET` | `/api/admin/config/history` | `?page=&per_page=&date_from=&date_to=&category=&search=` | `{ entries: ConfigHistoryEntry[], pagination: HistoryPagination }` | Load filtered history |
| 6 | `POST` | `/api/admin/config/history/{id}/rollback` | `{ reason: string }` | `{ config: ConfigItem, history_entry: ConfigHistoryEntry }` | Rollback a change |

### Approval Workflow Endpoints

| # | Method | Endpoint | Request | Response | Use |
|---|--------|----------|---------|----------|-----|
| 7 | `POST` | `/api/admin/config/requests` | `{ config_key: string, new_value: string, reason: string }` | `{ request: ChangeRequest }` | Request critical config change |
| 8 | `GET` | `/api/admin/config/requests` | `?status=PENDING` | `{ requests: ChangeRequest[] }` | Load pending requests |
| 9 | `GET` | `/api/admin/config/requests/recent` | — | `{ decisions: ChangeRequest[] }` | Load recent decisions (last 20) |
| 10 | `POST` | `/api/admin/config/requests/{id}/approve` | `{}` | `{ request: ChangeRequest, config: ConfigItem }` | Approve request |
| 11 | `POST` | `/api/admin/config/requests/{id}/reject` | `{ reason: string }` | `{ request: ChangeRequest }` | Reject request |

---

## Request Type Definitions

```typescript
// --- Config Update (non-critical) ---

interface UpdateConfigRequest {
  value: string;
  reason: string;                   // Required, min 1 char
}

// --- Critical Config Change Request ---

interface CreateChangeRequest {
  config_key: string;
  new_value: string;
  reason: string;                   // Required, min 1 char
}

// --- Rollback ---

interface RollbackRequest {
  reason: string;                   // Required, min 1 char
}

// --- Reject ---

interface RejectRequest {
  reason: string;                   // Required, min 1 char
}

// --- History Filters ---

interface HistoryFilterParams {
  page?: number;
  per_page?: number;
  date_from?: string;
  date_to?: string;
  category?: string;
  search?: string;
}
```

---

## Component Tree

```
src/
├── pages/
│   └── admin/
│       ├── ConfigListPage.tsx                # /admin/config — grouped table + inline edit
│       ├── ConfigHistoryPage.tsx             # /admin/config/history
│       └── ConfigApprovalsPage.tsx           # /admin/config/approvals
│
├── components/
│   └── config/
│       ├── ConfigCategorySection.tsx         # Collapsible category with config rows
│       ├── ConfigRow.tsx                     # Single config row (key, value, badges, actions)
│       ├── ConfigInlineEdit.tsx              # Expandable inline edit form
│       ├── ConfigValueInput.tsx              # Type-aware input (number, toggle, dropdown, JSON)
│       ├── ConfigBadges.tsx                  # Critical, Restart Required, Pending badges
│       ├── SensitiveValue.tsx                # Masked value with [Reveal]/[Hide] + auto-hide timer
│       ├── PendingApprovalInline.tsx         # Inline pending status on config row
│       ├── CacheStatusIndicator.tsx          # Cache healthy/unhealthy + [Invalidate] button
│       ├── ConfigSearchBar.tsx              # Search input with debounce + clear
│       ├── BootstrapInfoBanner.tsx           # Info banner about env vars
│       ├── HistoryTable.tsx                  # Change history table with pagination
│       ├── HistoryFilters.tsx                # Date range + category + search filters
│       ├── HistoryEntryRow.tsx               # Single history row with rollback action
│       ├── RollbackModal.tsx                 # Confirmation modal with reason
│       ├── ApprovalRequestCard.tsx           # Pending request card with approve/reject
│       ├── RecentDecisionsTable.tsx          # Table of recent approved/rejected/expired
│       ├── RejectReasonModal.tsx             # Modal for rejection reason
│       └── ExpiryCountdown.tsx              # Live countdown for request expiry
│
├── hooks/
│   ├── useConfigList.ts                     # GET configs + search/filter logic
│   ├── useConfigEdit.ts                     # PUT config + POST request + validation
│   ├── useConfigReveal.ts                   # GET reveal with auto-hide timer
│   ├── useCacheInvalidation.ts              # POST cache invalidate
│   ├── useConfigHistory.ts                  # GET history + filters + pagination
│   ├── useConfigRollback.ts                 # POST rollback
│   ├── useConfigApprovals.ts                # GET pending + recent + approve + reject
│   └── useExpiryCountdown.ts               # Live countdown timer hook
│
└── utils/
    ├── configValidation.ts                  # Type-aware validation (range, enum, JSON, boolean)
    └── configDisplay.ts                     # Format config values for display (boolean→Enabled/Disabled, JSON→formatted)
```

### File Markers

| File | Status | Notes |
|------|--------|-------|
| `ConfigListPage.tsx` | NEW | Main page with grouped sections |
| `ConfigHistoryPage.tsx` | NEW | Filterable history with rollback |
| `ConfigApprovalsPage.tsx` | NEW | Pending + recent decisions |
| `ConfigCategorySection.tsx` | NEW | Collapsible accordion section |
| `ConfigRow.tsx` | NEW | Read-only config row |
| `ConfigInlineEdit.tsx` | NEW | Edit form with Save/Request Approval |
| `ConfigValueInput.tsx` | NEW | Type-aware input control |
| `ConfigBadges.tsx` | NEW | Badge components (Critical, Restart, Pending) |
| `SensitiveValue.tsx` | NEW | Masked value with reveal |
| `PendingApprovalInline.tsx` | NEW | Inline pending status |
| `CacheStatusIndicator.tsx` | NEW | Cache health + invalidate |
| `ConfigSearchBar.tsx` | NEW | Search with debounce |
| `BootstrapInfoBanner.tsx` | NEW | Static info banner |
| `HistoryTable.tsx` | NEW | Paginated history table |
| `HistoryFilters.tsx` | NEW | Filter controls |
| `HistoryEntryRow.tsx` | NEW | Row with rollback action |
| `RollbackModal.tsx` | NEW | Confirmation modal |
| `ApprovalRequestCard.tsx` | NEW | Pending request card |
| `RecentDecisionsTable.tsx` | NEW | Recent decisions table |
| `RejectReasonModal.tsx` | NEW | Rejection reason modal |
| `ExpiryCountdown.tsx` | NEW | Live countdown display |
| `configValidation.ts` | NEW | Validation utility |
| `configDisplay.ts` | NEW | Display formatting utility |

---

## Key Component Specifications

### 1. ConfigCategorySection

```
Props:
  category: ConfigCategory
  configs: ConfigItem[]
  collapsed: boolean
  onToggle: () => void
  editing_key: string | null
  onEditStart: (key: string) => void
  search_query: string

Behavior:
  - Header shows: category label, config count, collapse/expand toggle
  - Click header → toggle collapsed state
  - Renders ConfigRow for each config in category
  - When search active: auto-expand if any config matches, auto-collapse if none
  - Highlight matching text in keys and descriptions during search
```

### 2. ConfigInlineEdit

```
Props:
  config: ConfigItem
  onSave: (value: string, reason: string) => Promise<void>
  onRequestApproval: (value: string, reason: string) => Promise<void>
  onCancel: () => void

Behavior:
  - Shows config key, description, type info, range/allowed values, default
  - If is_critical: info banner + [Request Approval] button instead of [Save]
  - Value input rendered by ConfigValueInput (type-aware)
  - Reason textarea (required, min 1 char)
  - [Reset to Default] pre-fills value with default_value
  - Client-side validation runs on blur and input change
  - [Save]/[Request Approval] disabled until: value valid + reason non-empty + value differs from current
  - Submit button disabled during API call
```

### 3. ConfigValueInput

```
Props:
  data_type: ConfigDataType
  value: string
  min_value: string | null
  max_value: string | null
  allowed_values: string | null
  onChange: (value: string) => void
  error: string | null

Renders:
  INTEGER    → <input type="number" step="1"> with min/max
  DECIMAL    → <input type="number" step="any"> with min/max
  BOOLEAN    → Radio group: Enabled / Disabled
  STRING (with allowed_values) → <select> dropdown
  STRING (without) → <input type="text">
  JSON       → <textarea> with syntax validation indicator
  DURATION   → <input type="number" step="1"> with min/max + unit label

Validation:
  - INTEGER: parseInt, isNaN check, range check
  - DECIMAL: parseFloat, isNaN check, range check
  - BOOLEAN: always valid (toggle)
  - STRING enum: must be in allowed_values list
  - STRING free: non-empty
  - JSON: JSON.parse try/catch
  - DURATION: same as INTEGER
```

### 4. SensitiveValue

```
Props:
  config_key: string
  is_revealed: boolean
  revealed_value: string | null
  onReveal: () => void
  onHide: () => void

Behavior:
  - Default: show "••••••••" + [Reveal] button
  - On [Reveal]: call GET /api/admin/config/{key}/reveal
  - On success: show actual value + [Hide] button + "Auto-hiding in {N}s"
  - Auto-hide after 30 seconds (countdown displayed)
  - Click [Hide] → mask immediately, clear timer
  - Multiple reveals don't stack — clicking [Reveal] again resets timer
```

### 5. ApprovalRequestCard

```
Props:
  request: ChangeRequest
  onApprove: (id: string) => void
  onReject: (id: string) => void
  approving: boolean

Behavior:
  - Shows: config key, current→proposed value, requester email, reason, expiry countdown
  - If is_own_request: hide [Approve]/[Reject], show "Awaiting another admin's approval"
  - If expires_at < 2 hours: warning styling on expiry countdown
  - [Approve]: inline confirmation ("Apply {key} = {value}?") → API call
  - [Reject]: opens RejectReasonModal
  - Both buttons disabled during their respective API calls
  - requires_restart shown as chip if true
```

### 6. RollbackModal

```
Props:
  entry: ConfigHistoryEntry
  isOpen: boolean
  onClose: () => void
  onConfirm: (reason: string) => Promise<void>

Behavior:
  - Shows: config key, current value, value to revert to
  - Shows original change metadata: who, when, reason
  - Reason textarea (required, min 1 char)
  - [Confirm Rollback] disabled until reason entered
  - [Confirm Rollback] disabled during API call
  - On success: close modal, toast success
  - On error: keep modal open, show error
```

---

## Build Checklist

| # | Phase | Tasks | Dependencies |
|---|-------|-------|-------------|
| 1 | Utilities | `configValidation.ts`, `configDisplay.ts` | None |
| 2 | Hooks (config) | `useConfigList`, `useConfigEdit`, `useConfigReveal`, `useCacheInvalidation` | Phase 1 |
| 3 | Hooks (history) | `useConfigHistory`, `useConfigRollback` | None |
| 4 | Hooks (approvals) | `useConfigApprovals`, `useExpiryCountdown` | None |
| 5 | Shared components | `ConfigBadges`, `ConfigValueInput`, `SensitiveValue`, `ExpiryCountdown`, `CacheStatusIndicator`, `BootstrapInfoBanner`, `ConfigSearchBar` | Phase 1 |
| 6 | Config List | `ConfigListPage`, `ConfigCategorySection`, `ConfigRow`, `ConfigInlineEdit`, `PendingApprovalInline` | Phase 2, 5 |
| 7 | Change History | `ConfigHistoryPage`, `HistoryTable`, `HistoryFilters`, `HistoryEntryRow`, `RollbackModal` | Phase 3, 5 |
| 8 | Approval Queue | `ConfigApprovalsPage`, `ApprovalRequestCard`, `RecentDecisionsTable`, `RejectReasonModal` | Phase 4, 5 |
| 9 | Integration | Route registration, admin sidebar nav link, role guards, breadcrumbs | Phase 6, 7, 8 |
| 10 | Testing | Unit tests (validation per data type, countdown timer), component tests (inline edit flow, approval card states, rollback modal), E2E (edit non-critical, request critical, approve, reject, rollback, search, reveal sensitive) | Phase 9 |

---

## Testing Notes

### Critical Paths

| Path | Why Critical |
|------|-------------|
| Non-critical config edit | Must validate by type, require reason, update row on success |
| Critical config request | Must route to approval workflow, not direct update |
| Approval flow | Must prevent self-approval, handle expiry, apply value on approve |
| Rollback | Must revert to correct value, create new history entry, handle already-rolled-back |
| Type-aware validation | Each data type has different rules — INTEGER range, JSON parse, enum match |
| Sensitive reveal + auto-hide | Timer must count down, auto-mask, clear on manual hide |

### Edge Cases

| Case | Expected Behavior |
|------|-------------------|
| Edit config that has pending approval | [Edit] disabled. Show pending status inline. |
| Approve own request | [Approve] hidden. Show "Awaiting another admin." |
| Approve expired request | API returns ERR_CFG_010. Toast error. Refresh list. |
| Rollback already-rolled-back entry | API returns ERR_CFG_012. Toast error. Refresh. [Rollback] button already hidden. |
| Set value to current value | API accepts (for audit trail). Success toast. |
| JSON value with syntax error | Inline error: "Invalid JSON syntax." [Save] disabled. |
| Search with no matches | All categories auto-collapsed. "No configs match '{query}'." |
| Multiple admins editing different configs | No conflict — only one inline edit per admin session. Server handles concurrency. |
| Cache invalidation while editing | No impact on edit form. Cache status refreshes. |
| Config with requires_restart updated | Success toast + additional toast: "Service restart required." |
| Only one super admin in system | Can create critical change requests but nobody can approve. Show info in approval card. |
| Rapid search typing | 300ms debounce prevents excessive re-filtering. |
