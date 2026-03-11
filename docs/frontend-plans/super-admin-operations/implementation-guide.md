# Implementation Guide (Super Admin & Operations)

Scope: State management, API integration patterns, component tree, and build checklist for admin dashboard, monitoring, alerts, maintenance mode, and impersonation indicator.

---

## State Interfaces

```typescript
// ─── Admin Dashboard ───

interface AdminDashboardState {
  summary: DashboardSummary | null;
  loading: boolean;
  error: string | null;
  maintenanceMode: MaintenanceModeState;
  activeAlertsCount: number;
}

interface DashboardSummary {
  active_tenants: number;
  active_tenants_trend_pct: number;
  active_scans: number;
  queue_depth: number;
  credits_consumed_30d: number;
  credits_trend_pct: number;
  system_health_status: 'HEALTHY' | 'WARNING' | 'CRITICAL';
  system_error_rate: number;
}

interface MaintenanceModeState {
  active: boolean;
  reason: string | null;
  estimated_duration_minutes: number | null;
  started_at: string | null;
  estimated_end_at: string | null;
}

// ─── Monitoring Detail ───

interface MonitoringState {
  tenantMetrics: MetricSectionState<TenantMetrics>;
  scanMetrics: MetricSectionState<ScanMetrics>;
  workerMetrics: MetricSectionState<WorkerMetrics>;
  creditMetrics: MetricSectionState<CreditMetrics>;
  systemHealth: MetricSectionState<SystemHealthMetrics>;
  expandedMetric: string | null;        // metric_name or null
  collapsedSections: Set<string>;       // section names
  notificationPermission: 'granted' | 'denied' | 'default';
}

interface MetricSectionState<T> {
  data: T | null;
  loading: boolean;
  error: string | null;
  lastUpdatedAt: string | null;
}

interface TenantMetrics {
  active_tenants: number;
  tenants_by_plan: { plan: string; count: number }[];
  new_tenants_7d: number;
  new_tenants_30d: number;
  suspended_tenants: number;
  churn_30d: number;
}

interface ScanMetrics {
  active_scans: number;
  active_scan_list: ActiveScan[];      // drill-down data
  queue_depth: number;
  queue_max_depth: number;
  completed_24h: number;
  failure_rate_24h: number;
  avg_duration_seconds: number;
}

interface ActiveScan {
  job_id: string;
  tenant_name: string;
  domain: string;
  progress_pct: number;
  started_at: string;
}

interface WorkerMetrics {
  active_workers: number;
  stale_workers: number;
  stale_worker_list: StaleWorker[];    // drill-down data
  utilization_pct: number;
}

interface StaleWorker {
  worker_id: string;
  last_heartbeat_at: string;
  current_job_id: string | null;
}

interface CreditMetrics {
  consumed_30d: number;
  consumed_by_tier: { tier: string; amount: number }[];
  mrr_estimate: number;
  pack_purchases_30d: number;
  top_consumers: { tenant_name: string; amount: number }[];
}

interface SystemHealthMetrics {
  api_error_rate_1h: number;
  api_latency_p50_ms: number;
  api_latency_p95_ms: number;
  api_latency_p99_ms: number;
  db_pool_current: number;
  db_pool_max: number;
  redis_memory_mb: number;
  api_key_pool: ApiKeyPoolHealth[];
}

interface ApiKeyPoolHealth {
  provider: string;
  active_keys: number;
  total_keys: number;
  quota_exhausted: number;
}

// ─── Alert Management ───

interface AlertManagementState {
  activeAlerts: ActiveAlert[];
  alertRules: AlertRule[];
  loading: boolean;
  error: string | null;
  editingRuleId: string | null;
  editForm: AlertThresholdFormState;
}

interface ActiveAlert {
  id: string;
  alert_type: string;
  level: 'CRITICAL' | 'WARNING';
  condition_description: string;
  current_value: number;
  threshold: number;
  triggered_at: string;
}

interface AlertRule {
  id: string;
  alert_type: string;
  condition_label: string;
  current_value: number;
  threshold: number;
  level: 'CRITICAL' | 'WARNING';
  enabled: boolean;
  min_threshold: number;
  max_threshold: number;
}

interface AlertThresholdFormState {
  threshold: string;
  errors: Record<string, string>;
  submitting: boolean;
}

// ─── Maintenance Mode Modal ───

interface MaintenanceFormState {
  reason: string;
  estimated_duration_minutes: string;
  errors: Record<string, string>;
  submitting: boolean;
}

// ─── Impersonation Indicator (Global) ───

interface ImpersonationState {
  active: boolean;
  tenant_name: string | null;
  tenant_id: string | null;
  reason: string | null;
  started_at: string | null;
  expires_at: string | null;
  time_remaining_seconds: number;
  expanded: boolean;
  ending: boolean;
}

// ─── Alert Header Badge (Global) ───

interface AdminAlertBadgeState {
  count: number;
  recentAlerts: ActiveAlert[];     // top 5 for dropdown
  dropdownOpen: boolean;
  loading: boolean;
}
```

---

## API Endpoints

### Dashboard Endpoints

| Method | Endpoint | Request | Response | Used By |
|--------|----------|---------|----------|---------|
| GET | `/api/admin/dashboard/summary` | — | `{ summary: DashboardSummary, maintenance: MaintenanceModeState, active_alerts_count: number }` | Dashboard page load |

### Monitoring Endpoints

| Method | Endpoint | Request | Response | Used By | Refresh |
|--------|----------|---------|----------|---------|---------|
| GET | `/api/admin/monitoring/tenants` | — | `{ metrics: TenantMetrics }` | Monitoring page | 5 min |
| GET | `/api/admin/monitoring/scans` | — | `{ metrics: ScanMetrics }` | Monitoring page | 30 sec |
| GET | `/api/admin/monitoring/workers` | — | `{ metrics: WorkerMetrics }` | Monitoring page | 30 sec |
| GET | `/api/admin/monitoring/credits` | — | `{ metrics: CreditMetrics }` | Monitoring page | 60 min |
| GET | `/api/admin/monitoring/system-health` | — | `{ metrics: SystemHealthMetrics }` | Monitoring page | 1 min |

### Alert Endpoints

| Method | Endpoint | Request | Response | Used By |
|--------|----------|---------|----------|---------|
| GET | `/api/admin/monitoring/alerts/active` | — | `{ alerts: ActiveAlert[] }` | Alerts page + header badge |
| GET | `/api/admin/monitoring/alerts/rules` | — | `{ rules: AlertRule[] }` | Alerts page |
| PUT | `/api/admin/monitoring/alerts/rules/{id}` | `{ threshold?, enabled? }` | `{ rule: AlertRule }` | Edit threshold / toggle |

### Maintenance Endpoints

| Method | Endpoint | Request | Response | Used By |
|--------|----------|---------|----------|---------|
| GET | `/api/admin/maintenance/status` | — | `{ maintenance: MaintenanceModeState }` | Header badge |
| POST | `/api/admin/maintenance/enable` | `{ reason, estimated_duration_minutes }` | `{ maintenance: MaintenanceModeState }` | Enable modal submit |
| POST | `/api/admin/maintenance/disable` | — | `{ maintenance: MaintenanceModeState }` | Disable confirmation |

### Impersonation Endpoints

| Method | Endpoint | Request | Response | Used By |
|--------|----------|---------|----------|---------|
| GET | `/api/admin/impersonation/status` | — | `{ session: ImpersonationSession \| null }` | Global indicator check |
| POST | `/api/admin/impersonation/end` | — | `204 No Content` | End session button |

> Note: Impersonation **start** is in the tenant management plan (`POST /api/admin/tenants/{id}/impersonate`).

### Request Types

```typescript
interface EnableMaintenanceRequest {
  reason: string;
  estimated_duration_minutes: number;
}

interface UpdateAlertRuleRequest {
  threshold?: number;
  enabled?: boolean;
}

interface ImpersonationSession {
  admin_user_id: string;
  tenant_id: string;
  tenant_name: string;
  reason: string;
  started_at: string;
  expires_at: string;
}
```

---

## Component Tree

```
/admin (Dashboard)
└── AdminDashboardPage
    ├── AdminHeader (persistent across all /admin/* routes)
    │   ├── AdminLogo + "Reconova Admin"
    │   ├── AlertBadge
    │   │   ├── NotificationIcon + count badge
    │   │   └── AlertDropdown (on click)
    │   │       ├── AlertDropdownItem[]     — level icon, description, time ago
    │   │       └── ViewAllAlertsLink       — → /admin/monitoring/alerts
    │   ├── MaintenanceToggle
    │   │   ├── StatusLabel                 — "Maintenance: Off" / "Maintenance: ON (43m)"
    │   │   └── onClick → MaintenanceModal or DisableConfirmation
    │   └── AdminProfileMenu
    ├── AdminSidebar (persistent across all /admin/* routes)
    │   └── NavLinks[]                      — Dashboard, Tenants, Users, Features, etc.
    ├── DashboardContent
    │   ├── MaintenanceBanner               — shown when maintenance active
    │   ├── SummaryCardGrid
    │   │   ├── SummaryCard (Active Tenants) — count + trend arrow
    │   │   ├── SummaryCard (Active Scans)   — count + queue depth
    │   │   ├── SummaryCard (Credits 30d)    — count + trend arrow
    │   │   └── SummaryCard (System Health)  — status dot + error rate
    │   ├── QuickLinksGrid
    │   │   └── QuickLinkCard[]             — icon + label → admin section
    │   └── MonitoringLink                  — [View Monitoring →]
    └── MaintenanceModal                    — enable form / disable confirmation
        ├── EnableMaintenanceForm
        │   ├── WarningBanner
        │   ├── ReasonTextarea
        │   ├── DurationInput
        │   └── ModalFooter                 — [Cancel] [Enable Maintenance]
        └── DisableConfirmation
            └── ModalFooter                 — [Cancel] [Disable Maintenance]

/admin/monitoring
└── MonitoringPage
    ├── Breadcrumb                          — ← Dashboard / Monitoring
    ├── BrowserNotificationPrompt           — first visit only
    ├── MetricSection (Tenant Metrics)
    │   ├── SectionHeader                   — title + updated time + collapse toggle
    │   ├── MetricCardGrid
    │   │   └── MetricCard[]                — value + label + click to drill down
    │   └── DrillDownPanel                  — expanded detail (bar chart, list, etc.)
    ├── MetricSection (Scan Metrics)
    │   └── (same structure)
    ├── MetricSection (Worker Metrics)
    │   └── (same structure)
    ├── MetricSection (Credit & Revenue)
    │   └── (same structure)
    ├── MetricSection (System Health)
    │   └── (same structure)
    └── ManageAlertsLink                    — [Manage Alerts →]

/admin/monitoring/alerts
└── AlertManagementPage
    ├── Breadcrumb                          — ← Monitoring / Alert Management
    ├── ActiveAlertsPanel
    │   ├── ActiveAlertRow[]
    │   │   ├── LevelBadge                  — 🔴 CRITICAL / 🟡 WARNING
    │   │   ├── ConditionDescription
    │   │   ├── TriggeredTime               — relative timestamp
    │   │   └── onClick → navigate to monitoring section
    │   └── EmptyState                      — "No active alerts. All systems healthy."
    └── AlertRulesTable
        └── AlertRuleRow[]
            ├── ConditionLabel
            ├── CurrentValueCell
            ├── ThresholdCell               — display or inline edit input
            ├── LevelBadge
            ├── EnableToggle
            └── EditButton                  — [✏] → inline edit mode
                └── InlineEditControls      — [Save] [Cancel]

Global Components (rendered outside route tree):
└── ImpersonationIndicator
    ├── CollapsedPill                       — tenant name + timer (bottom-right)
    │   ├── TimerDisplay                    — countdown, color changes at thresholds
    │   └── onClick → expand
    ├── ExpandedPanel                       — full details + [End Session]
    │   ├── TenantInfo                      — name, ID
    │   ├── TimerDisplay
    │   ├── ReasonDisplay
    │   ├── RestrictedActionsWarning
    │   └── EndSessionButton                — → confirmation
    └── EndSessionConfirmation              — confirm dialog
```

---

## Key Component Specifications

### 1. SummaryCard

```
Props:
  label: string
  value: number | string
  trend?: { direction: 'up' | 'down' | 'flat'; pct: number }
  status?: 'HEALTHY' | 'WARNING' | 'CRITICAL'
  subtitle?: string
  onClick: () => void

Behavior:
  - Displays large metric value with label
  - Optional trend arrow (▲ green for positive, ▼ red for negative)
  - Status dot for system health card (green/yellow/red)
  - Click navigates to monitoring page (relevant section)
  - Loading state: skeleton shimmer
```

### 2. MetricSection

```
Props:
  title: string
  refreshInterval: number              // ms
  fetchFn: () => Promise<T>
  children: (data: T) => ReactNode

Behavior:
  - Fetches data on mount and at refreshInterval
  - Pauses refresh when document hidden (visibilitychange API)
  - Independent loading/error per section
  - Collapsible: click header to toggle
  - Shows "Updated: {time}" in header
  - On error: shows stale data warning + [Retry], continues auto-refresh
  - On refresh success: smooth value transition (no flash)
```

### 3. MetricCard

```
Props:
  label: string
  value: number | string
  alertLevel?: 'CRITICAL' | 'WARNING' | null
  onClick: () => void

Behavior:
  - Displays metric value with label
  - If alertLevel set: colored border (red/yellow) + alert icon
  - CRITICAL: red border + pulse animation
  - WARNING: yellow border
  - Click toggles drill-down panel below card grid
  - Only one drill-down expanded per section at a time
```

### 4. AlertRuleRow

```
Props:
  rule: AlertRule
  editing: boolean
  onEdit: () => void
  onSave: (threshold: number) => void
  onCancel: () => void
  onToggle: (enabled: boolean) => void

Behavior:
  - Display mode: condition, current value, threshold, level badge, toggle
  - Edit mode: threshold becomes input, [Save] [Cancel] appear
  - Toggle fires PUT immediately (optimistic update + rollback)
  - Disabled rules: row appears dimmed
  - Current value highlighted if exceeds threshold
  - Validation: threshold must be numeric, within min/max range
```

### 5. ImpersonationIndicator

```
Props: none (reads from global impersonation state)

Behavior:
  - Rendered at app root level (outside router)
  - Only visible when JWT contains impersonated_by claim
  - Collapsed: floating pill, bottom-right, semi-transparent
  - Timer: client-side countdown from expires_at, updates every second
  - Timer colors:
    - > 10 min: default styling
    - ≤ 10 min: yellow background
    - ≤ 2 min: red background + pulse CSS animation
  - Expanded: click pill to show detail panel
  - Click outside panel: collapse
  - [End Session]: confirmation → POST /api/admin/impersonation/end
  - On expiry (timer hits 0): toast warning, clear state, redirect to admin
  - z-index: above all other UI (modals, sidebars, etc.)
```

### 6. MaintenanceToggle

```
Props:
  maintenance: MaintenanceModeState

Behavior:
  - INACTIVE: shows "Maintenance: Off" as clickable button
    - Click → opens EnableMaintenanceModal
  - ACTIVE: shows "Maintenance: ON ({remaining}m)" with orange background
    - Click → opens DisableConfirmation
  - Timer: calculates remaining from estimated_end_at, updates every minute
  - Visible in admin header on all /admin/* pages
```

### 7. AlertBadge

```
Props:
  count: number

Behavior:
  - count = 0: notification icon only, no badge
  - count > 0: red circle badge with count number
  - count > 99: shows "99+"
  - Click: opens dropdown with up to 5 recent active alerts
  - Dropdown items: click → navigate to /admin/monitoring with section highlighted
  - "View All Alerts" link → /admin/monitoring/alerts
  - Auto-refreshes count every 60 seconds
  - Click outside dropdown: close
```

---

## File Structure

```
src/
├── pages/
│   └── admin/
│       ├── AdminDashboardPage.tsx                    [NEW]
│       ├── monitoring/
│       │   ├── MonitoringPage.tsx                    [NEW]
│       │   └── alerts/
│       │       └── AlertManagementPage.tsx           [NEW]
│       └── +layout.tsx                               [EXISTING — add maintenance + alert state]
├── components/
│   └── admin/
│       ├── dashboard/
│       │   ├── SummaryCard.tsx                       [NEW]
│       │   ├── SummaryCardGrid.tsx                   [NEW]
│       │   ├── QuickLinksGrid.tsx                    [NEW]
│       │   ├── QuickLinkCard.tsx                     [NEW]
│       │   └── MaintenanceBanner.tsx                 [NEW]
│       ├── monitoring/
│       │   ├── MetricSection.tsx                     [NEW]
│       │   ├── MetricCard.tsx                        [NEW]
│       │   ├── MetricCardGrid.tsx                    [NEW]
│       │   ├── DrillDownPanel.tsx                    [NEW]
│       │   ├── BrowserNotificationPrompt.tsx         [NEW]
│       │   └── drill-downs/
│       │       ├── TenantBreakdown.tsx               [NEW]
│       │       ├── ActiveScanList.tsx                [NEW]
│       │       ├── StaleWorkerList.tsx               [NEW]
│       │       ├── TopConsumers.tsx                  [NEW]
│       │       └── LatencyDetail.tsx                 [NEW]
│       ├── alerts/
│       │   ├── ActiveAlertsPanel.tsx                 [NEW]
│       │   ├── ActiveAlertRow.tsx                    [NEW]
│       │   ├── AlertRulesTable.tsx                   [NEW]
│       │   └── AlertRuleRow.tsx                      [NEW]
│       ├── header/
│       │   ├── AdminHeader.tsx                       [EXISTING — add AlertBadge + MaintenanceToggle]
│       │   ├── AlertBadge.tsx                        [NEW]
│       │   ├── AlertDropdown.tsx                     [NEW]
│       │   ├── MaintenanceToggle.tsx                 [NEW]
│       │   └── MaintenanceModal.tsx                  [NEW]
│       ├── impersonation/
│       │   ├── ImpersonationIndicator.tsx            [NEW]
│       │   ├── ImpersonationPill.tsx                 [NEW]
│       │   ├── ImpersonationPanel.tsx                [NEW]
│       │   └── EndSessionConfirmation.tsx            [NEW]
│       └── shared/
│           ├── LevelBadge.tsx                        [NEW]
│           └── ConfirmDialog.tsx                     [EXISTING — reuse from common]
├── hooks/
│   └── admin/
│       ├── useAdminDashboard.ts                      [NEW]
│       ├── useMonitoringSection.ts                   [NEW]
│       ├── useAlertManagement.ts                     [NEW]
│       ├── useMaintenanceMode.ts                     [NEW]
│       ├── useImpersonation.ts                       [NEW]
│       ├── useAlertBadge.ts                          [NEW]
│       └── useAutoRefresh.ts                         [NEW]
├── services/
│   └── admin/
│       ├── dashboardService.ts                       [NEW]
│       ├── monitoringService.ts                      [NEW]
│       ├── alertService.ts                           [NEW]
│       ├── maintenanceService.ts                     [NEW]
│       └── impersonationService.ts                   [NEW]
├── types/
│   └── admin.ts                                      [NEW]
└── routes/
    └── index.tsx                                     [EXISTING — add monitoring + alerts routes]
```

---

## Build Checklist

```
Phase 1: Types & Services
  □ Create types/admin.ts with all interfaces above
  □ Create dashboardService.ts (summary fetch)
  □ Create monitoringService.ts (5 metric category endpoints)
  □ Create alertService.ts (active alerts + rules CRUD)
  □ Create maintenanceService.ts (status + enable/disable)
  □ Create impersonationService.ts (status + end)

Phase 2: Admin Header Components
  □ AlertBadge with count badge and dropdown
  □ AlertDropdown with recent alerts list
  □ MaintenanceToggle with status display
  □ MaintenanceModal (enable form + disable confirmation)
  □ Integrate into existing AdminHeader

Phase 3: Admin Dashboard
  □ AdminDashboardPage with data fetching
  □ SummaryCardGrid with 4 summary cards
  □ MaintenanceBanner (shown when active)
  □ QuickLinksGrid with navigation cards
  □ useAdminDashboard hook

Phase 4: Monitoring Page
  □ MonitoringPage with 5 metric sections
  □ MetricSection with auto-refresh + collapse + error handling
  □ useAutoRefresh hook (visibility-aware interval)
  □ MetricCard with alert-level highlighting
  □ DrillDownPanel with section-specific components
  □ Drill-down components: TenantBreakdown, ActiveScanList, StaleWorkerList, etc.
  □ BrowserNotificationPrompt
  □ useMonitoringSection hook

Phase 5: Alert Management
  □ AlertManagementPage
  □ ActiveAlertsPanel with alert rows
  □ AlertRulesTable with inline editing
  □ AlertRuleRow with edit/toggle functionality
  □ useAlertManagement hook

Phase 6: Impersonation Indicator
  □ ImpersonationIndicator (root-level mount)
  □ ImpersonationPill (collapsed view with countdown)
  □ ImpersonationPanel (expanded detail view)
  □ EndSessionConfirmation
  □ useImpersonation hook (JWT claim detection + timer)
  □ Timer logic: client-side countdown with color thresholds
  □ Auto-expiry handling: toast + redirect

Phase 7: Routes & Integration
  □ Add /admin route (dashboard)
  □ Add /admin/monitoring route
  □ Add /admin/monitoring/alerts route
  □ Update admin sidebar navigation
  □ Mount ImpersonationIndicator at app root
  □ Verify role guard on admin layout
```

---

## Testing Notes

```
Dashboard Tests:
  - Summary cards render with correct values and trends
  - Quick links navigate to correct admin sections
  - Maintenance banner appears when mode is active
  - Error state shows retry button, quick links still render

Monitoring Tests:
  - All 5 metric sections load independently
  - Auto-refresh fires at correct intervals per section
  - Refresh pauses when tab/window not visible
  - Partial section failure doesn't affect other sections
  - Metric cards highlight when threshold breached
  - Drill-down expands/collapses correctly (one at a time per section)
  - Section collapse/expand works
  - Browser notification prompt: shown once, respects permission choice

Alert Management Tests:
  - Active alerts sorted by level (CRITICAL first)
  - Empty state when no active alerts
  - Inline threshold edit: validation, save, cancel
  - Toggle enabled/disabled: optimistic update + rollback on error
  - Threshold range validation per alert type

Maintenance Mode Tests:
  - Enable: form validation (reason ≥10 chars, duration required)
  - Enable: success updates header badge + shows dashboard banner
  - Disable: confirmation dialog, success clears state
  - Timer in header updates remaining time

Impersonation Indicator Tests:
  - Only visible when impersonation JWT claim present
  - Timer countdown: updates every second
  - Color transitions: default → yellow (≤10 min) → red (≤2 min)
  - Expand/collapse on click
  - End session: confirmation → API call → redirect to admin
  - Auto-expiry at 0:00: toast + clear state + redirect
  - z-index: renders above modals and other overlays
  - Restricted actions disabled with tooltip during impersonation

Cross-Cutting:
  - Role guard: non-super-admin redirected from all /admin/* routes
  - 2FA enforcement: first login without 2FA → forced enrollment
  - All admin actions audit-logged (verify via API)
  - Loading states: skeletons for cards and sections
```
