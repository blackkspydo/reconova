# Implementation Guide (Data, Audit & Platform Compliance)

Scope: TypeScript interfaces, API integration, component tree, and build checklist for Privacy Center, Audit Log, and Admin Data Management screens.

---

## State Interfaces

### Privacy Center State

```typescript
// --- Data Inventory ---

interface DataCategory {
  category: string;           // e.g., "Scan Results", "User Profiles"
  description: string;
  data_types: string[];       // e.g., ["Vulnerability findings", "Severity scores"]
  retention_period: string;   // e.g., "As long as account is active"
  legal_basis: string;        // e.g., "Contract performance"
  deletable: boolean;
}

interface ThirdPartyProcessor {
  name: string;
  purpose: string;
  data_shared: string[];
  privacy_url: string;
}

interface DataInventoryState {
  categories: DataCategory[];
  third_party_processors: ThirdPartyProcessor[];
  loading: boolean;
  error: string | null;
}

// --- Data Export ---

type ExportStatus = 'IDLE' | 'PROCESSING' | 'READY' | 'EXPIRED';

interface ExportFile {
  file_id: string;
  filename: string;
  size_bytes: number;
  download_url: string;       // Pre-signed, short-lived
}

interface ExportRequest {
  id: string;
  status: ExportStatus;
  requested_at: string;       // ISO 8601
  completed_at: string | null;
  expires_at: string | null;  // 24 hours after completion
  file_count: number;         // 1 for <1GB, multiple for >1GB
  files: ExportFile[];
  total_size_bytes: number;
}

interface DataExportState {
  current_export: ExportRequest | null;
  requesting: boolean;
  downloading_file_id: string | null;
  error: string | null;
}

// --- Data Deletion ---

type DeletionStatus = 'NONE' | 'PENDING' | 'PROCESSING' | 'COMPLETED';

interface DeletionRequest {
  id: string;
  status: DeletionStatus;
  requested_at: string;
  cooling_off_ends_at: string;  // requested_at + 72 hours
  processed_at: string | null;
  can_cancel: boolean;          // true if within cooling-off window
}

interface DeletionFormState {
  step: 1 | 2 | 3;
  confirmation_phrase: string;     // Must type "DELETE MY DATA"
  phrase_valid: boolean;
  re_auth_password: string;
  re_auth_error: string | null;
  submitting: boolean;
}

interface DataDeletionState {
  current_request: DeletionRequest | null;
  form: DeletionFormState | null;  // null when modal closed
  cancelling: boolean;
  error: string | null;
}

// --- Combined Privacy Center ---

interface PrivacyCenterState {
  inventory: DataInventoryState;
  export: DataExportState;
  deletion: DataDeletionState;
}
```

### Audit Log State

```typescript
interface AuditLogEntry {
  id: string;
  timestamp: string;            // ISO 8601
  category: string;             // e.g., "authentication", "scanning", "billing"
  action: string;               // e.g., "user.login", "scan.created"
  actor_email: string;
  actor_role: string;
  ip_address: string;
  resource_type: string;
  resource_id: string;
  result: 'success' | 'failure';
  is_impersonated: boolean;
  impersonated_by: string | null;  // Admin email if impersonated
  details_json: Record<string, unknown>;
}

interface AuditLogFilters {
  date_from: string | null;     // ISO date
  date_to: string | null;
  category: string | null;      // null = all categories
  search: string;               // Free text search on action/actor/resource
}

interface AuditLogPagination {
  page: number;
  per_page: number;             // Default 25
  total_entries: number;
  total_pages: number;
}

interface TenantAuditLogState {
  entries: AuditLogEntry[];
  filters: AuditLogFilters;
  pagination: AuditLogPagination;
  expanded_entry_id: string | null;  // One at a time
  loading: boolean;
  error: string | null;
}

// Admin extends tenant with additional filters
interface AdminAuditLogFilters extends AuditLogFilters {
  tenant_id: string | null;
  user_id: string | null;
  super_admin_only: boolean;
}

interface AdminAuditLogState {
  entries: AuditLogEntry[];
  filters: AdminAuditLogFilters;
  pagination: AuditLogPagination;
  expanded_entry_id: string | null;
  exporting_csv: boolean;
  loading: boolean;
  error: string | null;
}
```

### Admin Data Management State

```typescript
// --- Backups ---

interface BackupRecord {
  id: string;
  type: 'automated' | 'manual';
  status: 'success' | 'failed' | 'in_progress';
  started_at: string;
  completed_at: string | null;
  size_bytes: number | null;
  error_message: string | null;
}

interface BackupSchedule {
  frequency: string;            // e.g., "Daily at 02:00 UTC"
  retention_days: number;
  last_successful: string | null;
  next_scheduled: string;
}

interface AdminBackupState {
  schedule: BackupSchedule | null;
  recent_backups: BackupRecord[];
  triggering_manual: boolean;
  loading: boolean;
  error: string | null;
}

// --- Migrations ---

interface MigrationRecord {
  id: string;
  version: string;
  name: string;
  status: 'applied' | 'pending' | 'failed' | 'rolled_back';
  applied_at: string | null;
  duration_ms: number | null;
}

interface IntegrityCheck {
  table: string;
  status: 'ok' | 'warning' | 'error';
  message: string;
  checked_at: string;
}

interface MigrationConflict {
  migration_id: string;
  conflict_type: string;
  description: string;
  resolution: string | null;
}

interface AdminMigrationState {
  migrations: MigrationRecord[];
  integrity_checks: IntegrityCheck[];
  conflicts: MigrationConflict[];
  expanded_migration_id: string | null;
  running_integrity_check: boolean;
  loading: boolean;
  error: string | null;
}

// --- Combined Admin Data ---

interface AdminDataState {
  active_tab: 'backups' | 'migrations';
  backups: AdminBackupState;
  migrations: AdminMigrationState;
}
```

---

## API Endpoints

### Tenant Endpoints — Privacy Center

| # | Method | Endpoint | Request | Response | Use |
|---|--------|----------|---------|----------|-----|
| 1 | `GET` | `/api/privacy/data-inventory` | — | `{ categories: DataCategory[], third_party_processors: ThirdPartyProcessor[] }` | Load data inventory |
| 2 | `GET` | `/api/privacy/export` | — | `{ export: ExportRequest \| null }` | Check current export status |
| 3 | `POST` | `/api/privacy/export` | `{}` | `{ export: ExportRequest }` | Request new data export |
| 4 | `GET` | `/api/privacy/export/{id}/files/{fileId}/download` | — | Binary (redirect to pre-signed URL) | Download export file |
| 5 | `GET` | `/api/privacy/deletion` | — | `{ deletion: DeletionRequest \| null }` | Check current deletion status |
| 6 | `POST` | `/api/privacy/deletion` | `{ confirmation_phrase: string, password: string }` | `{ deletion: DeletionRequest }` | Request account deletion |
| 7 | `POST` | `/api/privacy/deletion/{id}/cancel` | `{}` | `{ deletion: null }` | Cancel deletion within cooling-off |

### Tenant Endpoints — Audit Log

| # | Method | Endpoint | Request | Response | Use |
|---|--------|----------|---------|----------|-----|
| 8 | `GET` | `/api/privacy/audit-log` | `?page=&per_page=&date_from=&date_to=&category=&search=` | `{ entries: AuditLogEntry[], pagination: AuditLogPagination }` | Load filtered audit log |
| 9 | `GET` | `/api/privacy/audit-log/categories` | — | `{ categories: { key: string, label: string }[] }` | Load filter dropdown options |

### Admin Endpoints — Cross-Tenant Audit Log

| # | Method | Endpoint | Request | Response | Use |
|---|--------|----------|---------|----------|-----|
| 10 | `GET` | `/api/admin/audit-logs` | `?page=&per_page=&date_from=&date_to=&category=&search=&tenant_id=&user_id=&super_admin_only=` | `{ entries: AuditLogEntry[], pagination: AuditLogPagination }` | Load cross-tenant audit log |
| 11 | `GET` | `/api/admin/audit-logs/export` | `?date_from=&date_to=&category=&tenant_id=&user_id=&super_admin_only=` | Binary (CSV) | Export filtered audit log as CSV |

### Admin Endpoints — Data Management

| # | Method | Endpoint | Request | Response | Use |
|---|--------|----------|---------|----------|-----|
| 12 | `GET` | `/api/admin/data/backups` | — | `{ schedule: BackupSchedule, recent_backups: BackupRecord[] }` | Load backup status |
| 13 | `POST` | `/api/admin/data/backups/trigger` | `{}` | `{ backup: BackupRecord }` | Trigger manual backup |
| 14 | `GET` | `/api/admin/data/migrations` | — | `{ migrations: MigrationRecord[], conflicts: MigrationConflict[] }` | Load migration status |
| 15 | `POST` | `/api/admin/data/migrations/integrity-check` | `{}` | `{ checks: IntegrityCheck[] }` | Run integrity check |

---

## Request Type Definitions

```typescript
// --- Data Export ---

// POST /api/privacy/export — no body required
type RequestDataExport = Record<string, never>;

// --- Data Deletion ---

interface RequestDataDeletion {
  confirmation_phrase: string;  // Must equal "DELETE MY DATA"
  password: string;             // Re-authentication
}

// --- Backup ---

// POST /api/admin/data/backups/trigger — no body required
type TriggerManualBackup = Record<string, never>;

// --- Integrity Check ---

// POST /api/admin/data/migrations/integrity-check — no body required
type RunIntegrityCheck = Record<string, never>;

// --- Audit Log CSV Export ---

interface AuditLogExportParams {
  date_from?: string;
  date_to?: string;
  category?: string;
  tenant_id?: string;
  user_id?: string;
  super_admin_only?: boolean;
}
```

---

## Component Tree

```
src/
├── pages/
│   ├── privacy/
│   │   ├── PrivacyCenterPage.tsx            # /privacy — layout with 3 sections
│   │   └── AuditLogPage.tsx                 # /privacy/audit-log
│   └── admin/
│       ├── AuditLogsPage.tsx                # /admin/audit-logs
│       └── DataManagementPage.tsx           # /admin/data (tabbed)
│
├── components/
│   ├── privacy/
│   │   ├── DataInventorySection.tsx         # Category table + third-party list
│   │   ├── DataExportSection.tsx            # Export status + request/download
│   │   ├── DataDeletionSection.tsx          # Deletion status + request button
│   │   ├── DeletionModal.tsx                # 3-step deletion confirmation
│   │   ├── ExportStatusBadge.tsx            # PROCESSING/READY/EXPIRED badge
│   │   ├── DeletionCountdown.tsx            # Hours remaining in cooling-off
│   │   └── AuditLogTable.tsx                # Shared table (tenant + admin)
│   │
│   ├── audit/
│   │   ├── AuditLogFilters.tsx              # Date range + category + search
│   │   ├── AdminAuditLogFilters.tsx         # Extends with tenant/user/super-admin
│   │   ├── AuditLogEntryRow.tsx             # Row with expand toggle
│   │   ├── AuditLogEntryDetail.tsx          # Expanded detail with JSON
│   │   ├── ImpersonationBadge.tsx           # "Via impersonation" indicator
│   │   └── CsvExportButton.tsx              # Admin CSV export trigger
│   │
│   └── admin/
│       ├── data/
│       │   ├── BackupStatusTab.tsx          # Schedule + recent backups
│       │   ├── BackupRecordRow.tsx          # Individual backup with status
│       │   ├── MigrationStatusTab.tsx       # Migration list + integrity
│       │   ├── MigrationRecordRow.tsx       # Individual migration with expand
│       │   ├── IntegrityCheckResults.tsx    # Table of check results
│       │   └── MigrationConflictCard.tsx    # Conflict detail display
│       └── ...existing admin components
│
├── hooks/
│   ├── useDataInventory.ts                  # GET data inventory
│   ├── useDataExport.ts                     # Export CRUD + polling
│   ├── useDataDeletion.ts                   # Deletion CRUD + cancel
│   ├── useAuditLog.ts                       # Paginated + filtered audit log
│   ├── useAdminAuditLog.ts                  # Extends useAuditLog with admin filters
│   ├── useAdminBackups.ts                   # Backup status + trigger
│   └── useAdminMigrations.ts               # Migrations + integrity check
│
└── utils/
    ├── formatBytes.ts                       # Human-readable file sizes
    └── countdown.ts                         # Cooling-off period countdown logic
```

### File Markers

| File | Status | Notes |
|------|--------|-------|
| `PrivacyCenterPage.tsx` | NEW | Three-section layout |
| `AuditLogPage.tsx` | NEW | Tenant audit log with filters |
| `admin/AuditLogsPage.tsx` | NEW | Cross-tenant audit log |
| `admin/DataManagementPage.tsx` | NEW | Tabbed: Backups, Migrations |
| `DataInventorySection.tsx` | NEW | Read-only category table |
| `DataExportSection.tsx` | NEW | Export state machine UI |
| `DataDeletionSection.tsx` | NEW | Deletion status + trigger |
| `DeletionModal.tsx` | NEW | 3-step confirmation flow |
| `ExportStatusBadge.tsx` | NEW | Status color mapping |
| `DeletionCountdown.tsx` | NEW | Time-remaining display |
| `AuditLogTable.tsx` | NEW | Shared between tenant/admin |
| `AuditLogFilters.tsx` | NEW | Base filter bar |
| `AdminAuditLogFilters.tsx` | NEW | Extended admin filters |
| `AuditLogEntryRow.tsx` | NEW | Expandable row |
| `AuditLogEntryDetail.tsx` | NEW | JSON detail view |
| `ImpersonationBadge.tsx` | NEW | Small indicator badge |
| `CsvExportButton.tsx` | NEW | Download trigger with loading |
| `BackupStatusTab.tsx` | NEW | Schedule + history |
| `BackupRecordRow.tsx` | NEW | Status-colored row |
| `MigrationStatusTab.tsx` | NEW | Migration list |
| `MigrationRecordRow.tsx` | NEW | Expandable migration detail |
| `IntegrityCheckResults.tsx` | NEW | Check result table |
| `MigrationConflictCard.tsx` | NEW | Conflict detail |
| `formatBytes.ts` | NEW | Utility |
| `countdown.ts` | NEW | Utility |

---

## Key Component Specifications

### 1. DataExportSection

```
Props: none (uses useDataExport hook)

States:
  IDLE        → Show [Request Export] button
  PROCESSING  → Show spinner + "Preparing your data..." + progress estimate
  READY       → Show file list with [Download] per file + expiry countdown
  EXPIRED     → Show "Export expired" message + [Request New Export] button

Behavior:
  - On [Request Export]: POST /api/privacy/export → poll status every 10 seconds
  - On PROCESSING: poll GET /api/privacy/export until status changes
  - On [Download]: open pre-signed URL in new tab (browser handles download)
  - Multi-part: show each file as separate row with filename + size
  - Expiry countdown: "Expires in {hours}h {minutes}m" — update every minute
  - Stop polling when page unmounted
```

### 2. DeletionModal

```
Props:
  isOpen: boolean
  onClose: () => void
  onComplete: () => void

Steps:
  Step 1 — Warning + Phrase
    - Red warning banner: consequences list
    - Text input: type "DELETE MY DATA" exactly
    - [Next] disabled until phrase matches
    - [Cancel] closes modal

  Step 2 — Re-Authentication
    - Password input field
    - [Verify] submits password check
    - On error: inline error "Incorrect password"
    - On success: advance to step 3
    - [Back] returns to step 1

  Step 3 — Final Confirmation
    - Summary: "This action will start a 72-hour countdown..."
    - [Confirm Deletion] → POST /api/privacy/deletion
    - On success: close modal, show deletion pending state
    - [Cancel] closes modal

Behavior:
  - Step indicator shows 1/2/3 progress
  - Cannot skip steps
  - Modal not dismissible by clicking outside (requires explicit cancel)
  - Form state cleared on close
```

### 3. AuditLogTable

```
Props:
  entries: AuditLogEntry[]
  pagination: AuditLogPagination
  expanded_entry_id: string | null
  onExpand: (id: string | null) => void
  onPageChange: (page: number) => void
  loading: boolean

Behavior:
  - Columns: Timestamp, Category, Action, Actor, Result, (expand toggle)
  - Click row → expand inline detail panel
  - Only one expanded at a time (click another collapses previous)
  - Impersonation badge shown next to actor if is_impersonated
  - Detail panel shows: IP address, resource type/ID, details JSON
  - JSON displayed in <pre> with [Copy JSON] button
  - Pagination: "Showing {start}-{end} of {total}" + page controls
  - Loading: skeleton rows matching column structure
```

### 4. AuditLogFilters

```
Props:
  filters: AuditLogFilters
  categories: { key: string; label: string }[]
  onChange: (filters: AuditLogFilters) => void

Behavior:
  - Date range: two date pickers (From / To)
  - Category: dropdown with "All Categories" default
  - Search: text input with 300ms debounce
  - Filters apply on change (no submit button)
  - [Clear Filters] resets all to defaults
  - URL query params synced with filter state for shareability
```

### 5. BackupStatusTab

```
Props: none (uses useAdminBackups hook)

Sections:
  Schedule Info:
    - Frequency, retention period, last successful, next scheduled
    - [Trigger Manual Backup] button

  Recent Backups Table:
    - Columns: Type, Status, Started, Duration, Size
    - Status badges: green (success), red (failed), blue (in_progress)
    - Failed rows show error message in tooltip
    - Most recent at top

Behavior:
  - [Trigger Manual Backup]: POST → add in_progress row at top
  - Disable trigger button while backup in_progress
  - Auto-refresh every 30 seconds while in_progress backup exists
  - Stop auto-refresh when no in_progress backups
```

### 6. MigrationStatusTab

```
Props: none (uses useAdminMigrations hook)

Sections:
  Migration History:
    - Table: Version, Name, Status, Applied At, Duration
    - Status badges: green (applied), yellow (pending), red (failed), gray (rolled_back)
    - Click row → expand to show details

  Integrity Checks:
    - [Run Integrity Check] button
    - Results table: Table, Status, Message, Checked At
    - Status: green (ok), yellow (warning), red (error)

  Conflicts (if any):
    - Warning banner with count
    - Conflict cards: migration ID, conflict type, description, resolution

Behavior:
  - [Run Integrity Check]: POST → show loading → display results
  - Disable button during check
  - Conflicts section only visible when conflicts.length > 0
```

---

## Build Checklist

| # | Phase | Tasks | Dependencies |
|---|-------|-------|-------------|
| 1 | Utilities | `formatBytes.ts`, `countdown.ts` | None |
| 2 | Hooks (tenant) | `useDataInventory`, `useDataExport` (with polling), `useDataDeletion`, `useAuditLog` | Phase 1 |
| 3 | Privacy Center | `PrivacyCenterPage`, `DataInventorySection`, `DataExportSection`, `ExportStatusBadge`, `DataDeletionSection`, `DeletionModal`, `DeletionCountdown` | Phase 2 |
| 4 | Audit Log (shared) | `AuditLogTable`, `AuditLogFilters`, `AuditLogEntryRow`, `AuditLogEntryDetail`, `ImpersonationBadge` | None |
| 5 | Tenant Audit Log | `AuditLogPage` wiring filters + table + pagination | Phase 4 |
| 6 | Hooks (admin) | `useAdminAuditLog`, `useAdminBackups`, `useAdminMigrations` | None |
| 7 | Admin Audit Log | `AuditLogsPage`, `AdminAuditLogFilters`, `CsvExportButton` | Phase 4, 6 |
| 8 | Admin Data | `DataManagementPage`, `BackupStatusTab`, `BackupRecordRow`, `MigrationStatusTab`, `MigrationRecordRow`, `IntegrityCheckResults`, `MigrationConflictCard` | Phase 6 |
| 9 | Integration | Route registration, nav links, role guards | Phase 3, 5, 7, 8 |
| 10 | Testing | Unit tests for hooks (polling, state transitions), component tests (3-step modal flow, filter sync, expand/collapse), E2E (export flow, deletion flow, audit log pagination) | Phase 9 |

---

## Testing Notes

### Critical Paths

| Path | Why Critical |
|------|-------------|
| Data export polling | Must handle PROCESSING→READY transition, stop on unmount, resume on revisit |
| 3-step deletion modal | Cannot skip steps; phrase must match exactly; re-auth must succeed; irreversible action |
| Deletion cooling-off cancel | Must work within 72h window; button disabled after window expires |
| Audit log filter + pagination | Filter changes must reset to page 1; URL params must stay in sync |
| Admin CSV export | Must apply current filters; handle large exports (streaming download) |
| Backup trigger | Optimistic row insertion; auto-refresh while in_progress |

### Edge Cases

| Case | Expected Behavior |
|------|-------------------|
| Export requested while one is PROCESSING | API rejects (409). Show "Export already in progress" toast. |
| Export file download link expired | API returns 410. Show "Link expired, refreshing..." → re-fetch export status. |
| Deletion requested with existing PENDING deletion | API rejects (409). Show current pending status. |
| Typing "delete my data" (wrong case) | Phrase validation fails — case-sensitive match required. |
| Audit log with 0 results | Show empty state: "No audit log entries match your filters." with [Clear Filters]. |
| Admin CSV export with >100k rows | Backend streams CSV. Frontend shows download progress via browser. |
| Manual backup triggered while one is in_progress | Disable [Trigger] button. Show "Backup already in progress." |
| Integrity check finds errors | Warning banner with count. Error rows highlighted red. |
| Suspended tenant visits /privacy | Read-only. Export still available. Deletion shows "Contact support." |
| Member (non-owner) visits /privacy | Read-only data inventory. Export and deletion hidden (owner-only). |
