# Implementation Guide (Compliance Engine)

Scope: State management, API integration, component architecture, and build checklist for compliance dashboard, assessment viewing, report download, framework requests (tenant), and framework/control/mapping management with tier access and request review (super admin).

---

## State Management

### Compliance Dashboard State (Tenant)

```typescript
interface ComplianceFramework {
  id: string;
  name: string;
  version: string;
  region: string | null;
  description: string;
  status: 'DRAFT' | 'ACTIVE' | 'DEPRECATED';
  is_public: boolean;
  is_template: boolean;
}

interface FrameworkSelection {
  framework_id: string;
  framework: ComplianceFramework;
  enabled_at: string;
  disabled_at: string | null;  // null = currently selected
}

interface LatestAssessment {
  id: string;
  framework_id: string;
  framework_name: string;
  overall_score: number;        // 0.00 - 100.00
  total_controls: number;
  assessed_controls: number;
  passing_controls: number;
  generated_at: string;
  domain: string;
  trend_direction: 'improving' | 'declining' | 'stable' | null;  // null if <3 assessments
}

interface ComplianceDashboardState {
  frameworks: ComplianceFramework[];       // available for selection
  selections: FrameworkSelection[];         // tenant's selected frameworks
  latestAssessments: LatestAssessment[];   // latest per selected framework
  requests: FrameworkRequest[];
  isLoading: boolean;
  error: string | null;
}
```

### Framework Detail State (Tenant)

```typescript
interface ComplianceAssessment {
  id: string;
  scan_job_id: string;
  framework_id: string;
  overall_score: number;
  total_controls: number;
  assessed_controls: number;
  passing_controls: number;
  generated_at: string;
  domain: string;
}

interface TrendDataPoint {
  assessment_id: string;
  score: number;
  date: string;
}

interface FrameworkDetailState {
  framework: ComplianceFramework | null;
  assessments: ComplianceAssessment[];
  trendData: TrendDataPoint[];
  hasTrend: boolean;               // trendData.length >= 3
  trendDirection: 'improving' | 'declining' | 'stable' | null;
  isLoading: boolean;
  error: string | null;
}
```

### Assessment Detail State

```typescript
interface ControlResult {
  id: string;
  control_id: string;
  title: string;
  description: string;
  category: string;
  status: 'PASS' | 'FAIL' | 'PARTIAL' | 'NOT_ASSESSED';
  evidence_json: Record<string, unknown>;
  findings_count: number;
  recommendations: Recommendation[];
  min_security_recommendations: MinSecurityRec[];
}

interface Recommendation {
  text: string;
  source: string;  // mapping or min_security
}

interface MinSecurityRec {
  priority: 'CRITICAL' | 'HIGH' | 'MEDIUM' | 'LOW';
  recommendation: string;
}

interface AssessmentDetailState {
  assessment: ComplianceAssessment | null;
  framework: ComplianceFramework | null;
  controlResults: ControlResult[];
  controlsByCategory: Record<string, ControlResult[]>;  // grouped
  statusFilter: 'ALL' | 'PASS' | 'FAIL' | 'PARTIAL' | 'NOT_ASSESSED';
  statusCounts: Record<string, number>;
  isLoading: boolean;
  error: string | null;
}
```

### Framework Request State

```typescript
interface FrameworkRequest {
  id: string;
  tenant_id: string;
  tenant_name: string;          // populated for admin view
  requested_by: string;
  framework_name: string;
  description: string;
  status: 'SUBMITTED' | 'REVIEWED' | 'ACCEPTED' | 'REJECTED';
  admin_notes: string | null;
  reviewed_by: string | null;
  created_at: string;
  updated_at: string;
}

interface RequestFormState {
  values: {
    framework_name: string;
    description: string;
  };
  errors: {
    framework_name: string | null;
    description: string | null;
  };
  touched: {
    framework_name: boolean;
    description: boolean;
  };
  isSubmitting: boolean;
  isValid: boolean;
}
```

### Admin Framework Management State

```typescript
interface AdminFrameworkState {
  frameworks: AdminFramework[];
  statusFilter: 'ALL' | 'DRAFT' | 'ACTIVE' | 'DEPRECATED';
  searchQuery: string;
  isLoading: boolean;
  error: string | null;
}

interface AdminFramework extends ComplianceFramework {
  controls_count: number;
  mappings_count: number;
  plan_access: PlanAccess[];
}

interface PlanAccess {
  plan_id: string;
  plan_name: string;
  enabled: boolean;
}
```

### Admin Framework Detail State

```typescript
interface AdminFrameworkDetailState {
  framework: AdminFramework | null;
  controls: AdminControl[];
  activeTab: 'controls' | 'tier_access';
  expandedControlId: string | null;
  isLoading: boolean;
  error: string | null;
}

interface AdminControl {
  id: string;
  control_id: string;
  title: string;
  description: string;
  category: string;
  min_security_recommendations_json: MinSecurityRec[];
  mappings: ControlCheckMapping[];
}

interface ControlCheckMapping {
  id: string;
  check_type: string;
  severity_threshold: 'CRITICAL' | 'HIGH' | 'MEDIUM' | 'LOW' | null;
  pass_condition_json: PassCondition;
  recommendation_json: string | null;
}

interface PassCondition {
  type: 'no_findings_above_threshold' | 'specific_check' | 'finding_count_below' | 'custom';
  expected_value?: boolean;       // for specific_check
  max_count?: number;             // for finding_count_below
  expression?: string;            // for custom
}
```

### Admin Request Review State

```typescript
interface AdminRequestState {
  requests: FrameworkRequest[];
  statusFilter: 'ALL' | 'SUBMITTED' | 'REVIEWED' | 'ACCEPTED' | 'REJECTED';
  isLoading: boolean;
  error: string | null;
}

interface ReviewFormState {
  values: {
    status: 'REVIEWED' | 'ACCEPTED' | 'REJECTED';
    admin_notes: string;
  };
  errors: {
    admin_notes: string | null;
  };
  isSubmitting: boolean;
  isValid: boolean;
}
```

### Control / Mapping Form State

```typescript
interface ControlFormState {
  values: {
    control_id: string;
    title: string;
    category: string;
    description: string;
    min_security_recommendations_json: string;  // raw JSON text
  };
  errors: Record<string, string | null>;
  isSubmitting: boolean;
  isValid: boolean;
}

interface MappingFormState {
  values: {
    check_type: string;
    severity_threshold: string | null;
    pass_condition_type: PassCondition['type'];
    pass_condition_params: Record<string, unknown>;
    recommendation: string;
  };
  errors: Record<string, string | null>;
  isSubmitting: boolean;
  isValid: boolean;
}
```

---

## API Integration

### Endpoint Table

#### Tenant Endpoints

| Method | Path | Description | Request | Response |
|--------|------|-------------|---------|----------|
| `GET` | `/api/compliance/frameworks` | List available frameworks (tier-filtered) | — | `ComplianceFramework[]` |
| `GET` | `/api/compliance/selections` | List tenant's framework selections | — | `FrameworkSelection[]` |
| `POST` | `/api/compliance/selections` | Select a framework | `{ framework_id }` | `FrameworkSelection` |
| `DELETE` | `/api/compliance/selections/{framework_id}` | Deselect a framework | — | `204` |
| `GET` | `/api/compliance/assessments/latest` | Latest assessment per framework | — | `LatestAssessment[]` |
| `GET` | `/api/compliance/assessments` | List assessments | `?framework_id=` | `ComplianceAssessment[]` |
| `GET` | `/api/compliance/assessments/{id}` | Assessment detail + control results | — | `AssessmentDetailResponse` |
| `GET` | `/api/compliance/trends/{framework_id}` | Trend data for framework | — | `TrendDataPoint[]` |
| `GET` | `/api/compliance/reports/{assessment_id}` | Download report | `?format=pdf\|html` | File download |
| `GET` | `/api/compliance/requests` | List own requests | — | `FrameworkRequest[]` |
| `POST` | `/api/compliance/requests` | Submit framework request | `CreateRequestPayload` | `FrameworkRequest` |

#### Admin Endpoints

| Method | Path | Description | Request | Response |
|--------|------|-------------|---------|----------|
| `GET` | `/api/admin/compliance/frameworks` | List all frameworks | — | `AdminFramework[]` |
| `POST` | `/api/admin/compliance/frameworks` | Create framework | `CreateFrameworkPayload` | `AdminFramework` |
| `GET` | `/api/admin/compliance/frameworks/{id}` | Framework detail + controls + mappings | — | `AdminFrameworkDetail` |
| `PUT` | `/api/admin/compliance/frameworks/{id}` | Update framework metadata | `UpdateFrameworkPayload` | `AdminFramework` |
| `PUT` | `/api/admin/compliance/frameworks/{id}/publish` | Publish framework | — | `AdminFramework` |
| `PUT` | `/api/admin/compliance/frameworks/{id}/deprecate` | Deprecate framework | — | `AdminFramework` |
| `PUT` | `/api/admin/compliance/frameworks/{id}/reactivate` | Reactivate framework | — | `AdminFramework` |
| `DELETE` | `/api/admin/compliance/frameworks/{id}` | Delete draft | `{ reason? }` | `204` |
| `PUT` | `/api/admin/compliance/frameworks/{id}/access` | Set tier access | `{ plan_id, enabled }` | `PlanAccess` |
| `POST` | `/api/admin/compliance/frameworks/{fid}/controls` | Add control | `CreateControlPayload` | `AdminControl` |
| `PUT` | `/api/admin/compliance/frameworks/{fid}/controls/{cid}` | Update control | `UpdateControlPayload` | `AdminControl` |
| `DELETE` | `/api/admin/compliance/frameworks/{fid}/controls/{cid}` | Delete control | — | `204` |
| `POST` | `/api/admin/compliance/controls/{cid}/mappings` | Add mapping | `CreateMappingPayload` | `ControlCheckMapping` |
| `PUT` | `/api/admin/compliance/controls/{cid}/mappings/{mid}` | Update mapping | `UpdateMappingPayload` | `ControlCheckMapping` |
| `DELETE` | `/api/admin/compliance/controls/{cid}/mappings/{mid}` | Delete mapping | — | `204` |
| `GET` | `/api/admin/compliance/requests` | List all requests | — | `FrameworkRequest[]` |
| `PUT` | `/api/admin/compliance/requests/{id}` | Review request | `ReviewRequestPayload` | `FrameworkRequest` |

### Request Types

```typescript
interface CreateRequestPayload {
  framework_name: string;  // min 3 chars
  description: string;     // min 20 chars
}

interface CreateFrameworkPayload {
  name: string;            // max 200, unique
  version: string;
  region: string | null;
  description: string;     // max 2000
  is_public: boolean;
}

interface UpdateFrameworkPayload {
  name?: string;
  version?: string;
  region?: string | null;
  description?: string;
  is_public?: boolean;
}

interface CreateControlPayload {
  control_id: string;      // max 50, unique within framework
  title: string;           // max 300
  category: string;        // max 100
  description?: string;    // max 2000
  min_security_recommendations_json?: MinSecurityRec[];
}

interface UpdateControlPayload {
  title?: string;
  category?: string;
  description?: string;
  min_security_recommendations_json?: MinSecurityRec[];
}

interface CreateMappingPayload {
  check_type: string;
  severity_threshold?: 'CRITICAL' | 'HIGH' | 'MEDIUM' | 'LOW';
  pass_condition_json: PassCondition;
  recommendation_json?: string;
}

interface UpdateMappingPayload {
  severity_threshold?: 'CRITICAL' | 'HIGH' | 'MEDIUM' | 'LOW' | null;
  pass_condition_json?: PassCondition;
  recommendation_json?: string;
}

interface ReviewRequestPayload {
  status: 'REVIEWED' | 'ACCEPTED' | 'REJECTED';
  admin_notes: string;     // min 10 chars
}
```

### Response Types

```typescript
interface AssessmentDetailResponse {
  assessment: ComplianceAssessment;
  framework: ComplianceFramework;
  control_results: ControlResult[];
}

interface AdminFrameworkDetail {
  framework: AdminFramework;
  controls: AdminControl[];
  plan_access: PlanAccess[];
}

interface ApiError {
  error: {
    code: string;
    message: string;
    details?: Record<string, unknown>;
  };
}
```

### Caching & Refresh Strategy

| Data | Cache Strategy | Invalidation |
|------|---------------|--------------|
| Available frameworks | Cache for session | Rarely changes |
| Selections | Cache, refresh on select/deselect | After selection CRUD |
| Latest assessments | No cache (dashboard load) | New scans auto-generate |
| Assessment detail | Cache by ID (immutable) | Never (assessments are immutable) |
| Trend data | No cache | After new assessment |
| Framework requests | No cache | After submit |
| Admin framework list | No cache | After CRUD |
| Admin framework detail | No cache | After control/mapping/access changes |
| Admin requests | No cache | After review |

---

## Component Architecture

### Component Tree

```
<App>
├── <FeatureGate feature="compliance_checks">
│
├── <ComplianceDashboardPage>               # /compliance
│   ├── <FrameworkSelectionSection>
│   │   ├── <FrameworkSelectCard />          # × N
│   │   └── <ComingSoonCard />              # × N (public drafts)
│   │
│   ├── <AssessmentScoresSection>
│   │   └── <FrameworkScoreCard />           # × N (selected)
│   │       ├── <ScoreBar />
│   │       └── <TrendSparkline />
│   │
│   ├── <FrameworkRequestsSection>
│   │   ├── <RequestStatusRow />             # × N
│   │   └── <FrameworkRequestModal />
│   │
│   └── <DeselectConfirmationModal />
│
├── <FrameworkDetailPage>                    # /compliance/frameworks/{id}
│   ├── <LatestScoreCard />
│   ├── <TrendChart />
│   └── <AssessmentHistoryTable />
│
├── <AssessmentDetailPage>                   # /compliance/assessments/{id}
│   ├── <ScoreSummaryCard />
│   ├── <StatusFilterBar />
│   ├── <ControlCategoryGroup />             # × N (per category)
│   │   └── <ControlResultRow />             # × N (per control)
│   │       └── <ControlExpanded />          # evidence, recommendations
│   └── <ReportDownloadButton />
│       └── <FormatSelector />               # PDF / HTML
│
└── <AdminCompliancePages>
    ├── <AdminFrameworkListPage>              # /admin/compliance
    │   ├── <FrameworkStatusFilter />
    │   ├── <AdminFrameworkTable />
    │   ├── <PublishConfirmationModal />
    │   ├── <DeprecateConfirmationModal />
    │   ├── <DeleteDraftModal />
    │   └── <ReactivateConfirmationModal />
    │
    ├── <AdminFrameworkDetailPage>            # /admin/compliance/frameworks/{id}
    │   ├── <FrameworkMetadataCard />
    │   ├── <TabNav tabs={['Controls','Tier Access']} />
    │   ├── <ControlsTab>
    │   │   ├── <AdminControlTable />
    │   │   │   └── <ControlRow />           # expandable
    │   │   │       └── <MappingTable />
    │   │   ├── <AddControlModal />
    │   │   ├── <EditControlModal />
    │   │   ├── <AddMappingModal />
    │   │   │   └── <PassConditionForm />    # dynamic form fields
    │   │   └── <EditMappingModal />
    │   └── <TierAccessTab>
    │       └── <PlanAccessToggleList />
    │
    ├── <AdminCreateFrameworkPage>            # /admin/compliance/frameworks/new
    │   └── <FrameworkCreateForm />
    │
    └── <AdminRequestsPage>                  # /admin/compliance/requests
        ├── <RequestStatusFilter />
        ├── <AdminRequestTable />
        └── <ReviewRequestModal />
```

### Key Component Specifications

#### `<FrameworkScoreCard />`

| Prop | Type | Description |
|------|------|-------------|
| `assessment` | `LatestAssessment` | Latest assessment data |
| `onClick` | `() => void` | Navigate to framework detail |

| Responsibility |
|---------------|
| Display framework name, latest score with color-coded progress bar |
| Show trend sparkline (if trend_direction not null) |
| Show "No assessments yet" empty state |
| Show "Deprecated" badge if framework deprecated |
| Clickable — navigates to framework detail |

---

#### `<TrendChart />`

| Prop | Type | Description |
|------|------|-------------|
| `data` | `TrendDataPoint[]` | Score-over-time data |
| `direction` | `'improving' \| 'declining' \| 'stable' \| null` | Trend direction |

| Responsibility |
|---------------|
| Render line chart with score (Y-axis 0-100) over time (X-axis) |
| Show direction indicator text |
| If < 3 data points, show "Run {N} more scans" message instead |

---

#### `<ControlResultRow />`

| Prop | Type | Description |
|------|------|-------------|
| `control` | `ControlResult` | Control result data |
| `isExpanded` | `boolean` | Whether detail is visible |
| `onToggle` | `() => void` | Toggle expanded state |

| Responsibility |
|---------------|
| Show control_id, title, status badge (collapsed) |
| On expand: show description, evidence, findings count, recommendations |
| Status badge colors: PASS green, FAIL red, PARTIAL yellow, NOT_ASSESSED grey |
| Always show min_security_recommendations (even on PASS) |

---

#### `<StatusFilterBar />`

| Prop | Type | Description |
|------|------|-------------|
| `counts` | `Record<string, number>` | Count per status |
| `activeFilter` | `string` | Currently selected filter |
| `onFilter` | `(status: string) => void` | Filter change handler |

| Responsibility |
|---------------|
| Render filter chips: All, Pass, Fail, Partial, Not Assessed |
| Show count per filter |
| Highlight active filter |

---

#### `<ReportDownloadButton />`

| Prop | Type | Description |
|------|------|-------------|
| `assessmentId` | `string` | Assessment to generate report for |

| Responsibility |
|---------------|
| Check compliance_reports feature flag |
| If enabled: show dropdown with PDF / HTML options, trigger file download |
| If disabled: show LockedBadge "Requires Pro" |
| Show loading spinner during download |
| Handle download errors with retry toast |

---

#### `<PassConditionForm />`

| Prop | Type | Description |
|------|------|-------------|
| `type` | `PassCondition['type']` | Selected condition type |
| `values` | `Record<string, unknown>` | Current param values |
| `onChange` | `(values) => void` | Param change handler |

| Responsibility |
|---------------|
| Render dynamic form fields based on selected pass condition type |
| `no_findings_above_threshold`: uses severity_threshold dropdown (no extra fields) |
| `specific_check`: expected_value boolean radio (True/False) |
| `finding_count_below`: max_count integer input |
| `custom`: expression textarea |
| Validate per type requirements |

---

#### `<ReviewRequestModal />`

| Prop | Type | Description |
|------|------|-------------|
| `request` | `FrameworkRequest` | Request to review |
| `onSubmit` | `(data: ReviewRequestPayload) => void` | Submit handler |
| `onClose` | `() => void` | Close handler |

| Responsibility |
|---------------|
| Show request details read-only (tenant, framework, description, date) |
| Status dropdown with valid transitions based on current status |
| Admin notes textarea with min 10 char validation |
| Disable [Update Status] until valid |
| For terminal statuses (ACCEPTED/REJECTED): show read-only admin notes, no form |

---

### Shared / Reusable Components

| Component | Used By | Notes |
|-----------|---------|-------|
| `<Modal />` | All modals | Standard modal with title, body, actions |
| `<Toast />` | All notifications | Global toast provider |
| `<FeatureGate />` | Dashboard entry, report button | From feature-flags plan |
| `<LockedBadge />` | Report download button | From feature-flags plan |
| `<SkeletonLoader />` | All loading states | Configurable rows/shape |
| `<EmptyState />` | No assessments, no selections | Icon + message + optional CTA |
| `<Badge />` | Status badges (PASS/FAIL/DRAFT/etc) | Variant-based coloring |
| `<TabNav />` | Admin framework detail tabs | Tab navigation with active state |
| `<SearchInput />` | Admin framework search | Debounced |
| `<ProgressBar />` | Score display | Color-coded by score range |

---

## File Structure

### NEW Files

```
src/
├── pages/
│   ├── compliance/
│   │   ├── ComplianceDashboardPage.tsx
│   │   ├── FrameworkDetailPage.tsx
│   │   └── AssessmentDetailPage.tsx
│   └── admin/
│       └── compliance/
│           ├── AdminFrameworkListPage.tsx
│           ├── AdminFrameworkDetailPage.tsx
│           ├── AdminCreateFrameworkPage.tsx
│           └── AdminRequestsPage.tsx
│
├── components/
│   └── compliance/
│       ├── FrameworkSelectCard.tsx
│       ├── ComingSoonCard.tsx
│       ├── FrameworkScoreCard.tsx
│       ├── ScoreBar.tsx
│       ├── TrendSparkline.tsx
│       ├── TrendChart.tsx
│       ├── AssessmentHistoryTable.tsx
│       ├── ScoreSummaryCard.tsx
│       ├── StatusFilterBar.tsx
│       ├── ControlCategoryGroup.tsx
│       ├── ControlResultRow.tsx
│       ├── ControlExpanded.tsx
│       ├── ReportDownloadButton.tsx
│       ├── FormatSelector.tsx
│       ├── FrameworkRequestModal.tsx
│       ├── RequestStatusRow.tsx
│       ├── DeselectConfirmationModal.tsx
│       ├── AdminFrameworkTable.tsx
│       ├── FrameworkMetadataCard.tsx
│       ├── AdminControlTable.tsx
│       ├── ControlRow.tsx
│       ├── MappingTable.tsx
│       ├── AddControlModal.tsx
│       ├── EditControlModal.tsx
│       ├── AddMappingModal.tsx
│       ├── EditMappingModal.tsx
│       ├── PassConditionForm.tsx
│       ├── PlanAccessToggleList.tsx
│       ├── PublishConfirmationModal.tsx
│       ├── DeprecateConfirmationModal.tsx
│       ├── DeleteDraftModal.tsx
│       ├── ReactivateConfirmationModal.tsx
│       ├── ReviewRequestModal.tsx
│       └── FrameworkCreateForm.tsx
│
├── hooks/
│   └── compliance/
│       ├── useComplianceDashboard.ts
│       ├── useFrameworkDetail.ts
│       ├── useAssessmentDetail.ts
│       ├── useFrameworkRequests.ts
│       ├── useAdminFrameworks.ts
│       ├── useAdminFrameworkDetail.ts
│       └── useAdminRequests.ts
│
├── api/
│   └── compliance.ts                    # All compliance API calls
│
└── types/
    └── compliance.ts                    # All compliance TypeScript types
```

### EXISTING Files to Modify

| File | Change |
|------|--------|
| `src/router.tsx` (or equivalent) | Add `/compliance/*` and `/admin/compliance/*` routes |
| `src/components/layout/AppSidebar.tsx` | Add "Compliance" nav item with `<FeatureGate>` |

---

## Build Checklist

Build in this order to ensure dependencies are satisfied:

1. **Types & API layer**
   - [ ] Define all TypeScript types in `types/compliance.ts`
   - [ ] Implement API client functions in `api/compliance.ts`

2. **Compliance dashboard (tenant entry point)**
   - [ ] `useComplianceDashboard` hook
   - [ ] `<FrameworkSelectCard />` — select/deselect toggle
   - [ ] `<ComingSoonCard />` — public draft preview
   - [ ] `<DeselectConfirmationModal />`
   - [ ] `<FrameworkScoreCard />` — score card with sparkline
   - [ ] `<ScoreBar />` — color-coded progress bar
   - [ ] `<TrendSparkline />` — compact trend line
   - [ ] `<RequestStatusRow />` — request with badge
   - [ ] `<FrameworkRequestModal />` — submit request form
   - [ ] `<ComplianceDashboardPage />` — compose three sections

3. **Framework detail (tenant)**
   - [ ] `useFrameworkDetail` hook
   - [ ] `<TrendChart />` — full trend line chart
   - [ ] `<AssessmentHistoryTable />` — assessment list
   - [ ] `<LatestScoreCard />` — large score display
   - [ ] `<FrameworkDetailPage />`

4. **Assessment detail**
   - [ ] `useAssessmentDetail` hook
   - [ ] `<ScoreSummaryCard />` — score breakdown
   - [ ] `<StatusFilterBar />` — filter chips
   - [ ] `<ControlCategoryGroup />` — category header + controls
   - [ ] `<ControlResultRow />` — collapsible control
   - [ ] `<ControlExpanded />` — evidence + recommendations
   - [ ] `<ReportDownloadButton />` + `<FormatSelector />`
   - [ ] `<AssessmentDetailPage />`

5. **Admin: Framework management**
   - [ ] `useAdminFrameworks` hook
   - [ ] `<AdminFrameworkTable />` — framework list with actions
   - [ ] `<PublishConfirmationModal />`
   - [ ] `<DeprecateConfirmationModal />`
   - [ ] `<DeleteDraftModal />` — with conditional reason field
   - [ ] `<ReactivateConfirmationModal />`
   - [ ] `<AdminFrameworkListPage />`

6. **Admin: Create framework**
   - [ ] `<FrameworkCreateForm />`
   - [ ] `<AdminCreateFrameworkPage />`

7. **Admin: Framework detail (controls + mappings)**
   - [ ] `useAdminFrameworkDetail` hook
   - [ ] `<FrameworkMetadataCard />` — editable for DRAFT
   - [ ] `<AdminControlTable />` — expandable rows
   - [ ] `<ControlRow />` — with mapping sub-table
   - [ ] `<MappingTable />` — mappings per control
   - [ ] `<AddControlModal />` / `<EditControlModal />`
   - [ ] `<AddMappingModal />` / `<EditMappingModal />`
   - [ ] `<PassConditionForm />` — dynamic pass condition fields
   - [ ] `<PlanAccessToggleList />` — tier access tab
   - [ ] `<AdminFrameworkDetailPage />` — tab layout

8. **Admin: Framework requests**
   - [ ] `useAdminRequests` hook
   - [ ] `<AdminRequestTable />` — request list with filters
   - [ ] `<ReviewRequestModal />` — status transition + notes
   - [ ] `<AdminRequestsPage />`

9. **Routing & navigation**
   - [ ] Add `/compliance`, `/compliance/frameworks/{id}`, `/compliance/assessments/{id}` routes
   - [ ] Add `/admin/compliance`, `/admin/compliance/frameworks/new`, `/admin/compliance/frameworks/{id}`, `/admin/compliance/requests` routes
   - [ ] Add "Compliance" nav item with FeatureGate
   - [ ] Add Admin > Compliance nav items (super admin only)
   - [ ] Guard admin routes with role check

---

## Testing Notes

### Scenarios to Cover

| Area | Test Scenarios |
|------|---------------|
| **Feature gating** | compliance_checks disabled → locked placeholder; operational → banner |
| **Framework selection** | Select adds to list, deselect confirms, duplicate selection handled |
| **Coming Soon** | Public draft shown as "Coming Soon", not selectable |
| **Deprecated framework** | Badge shown, historical data preserved, no select option |
| **Assessment scores** | Color coding by score range, sparkline shows trend direction |
| **Trend chart** | Shows when ≥3 assessments, "Run more scans" when <3 |
| **Assessment detail** | Controls grouped by category, filter by status, expand/collapse |
| **Control results** | Correct badge per status, evidence displayed, min security recs always shown |
| **Report download** | PDF/HTML format selector, file download, compliance_reports gating |
| **Framework requests** | Submit with validation, duplicate rejection, status badges |
| **Admin CRUD** | Create framework, add controls, add mappings, edit, delete |
| **Admin publish** | Validation checks (controls, mappings, tier access), confirmation |
| **Admin deprecate/reactivate** | State transitions, changelog notes |
| **Admin delete draft** | Simple for non-public, reason required for public |
| **Tier access** | Toggle per plan, auto-save |
| **Mapping pass conditions** | Dynamic form per type, validation per type |
| **Request review** | Valid transitions only, admin notes required, terminal states read-only |
| **Role guards** | Members read-only, admin routes blocked for non-admin |
