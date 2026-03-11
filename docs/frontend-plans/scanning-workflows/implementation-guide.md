# Implementation Guide (Scanning & Workflows)

Scope: State management, API integration, component architecture, and build checklist for all scanning & workflows screens.

---

## State Management

### Domain State

```typescript
interface Domain {
  id: string;
  domain: string;
  status: 'ACTIVE' | 'PENDING_VERIFICATION' | 'VERIFIED';
  added_by: string;
  verified_at: string | null;
  created_at: string;
  last_scanned_at: string | null; // derived from latest scan_job
}

interface DomainListState {
  domains: Domain[];
  totalCount: number;
  maxDomains: number; // from subscription plan
  isLoading: boolean;
  error: string | null;
}
```

### Scan Job State

```typescript
type ScanJobStatus = 'QUEUED' | 'RUNNING' | 'COMPLETED' | 'PARTIAL' | 'FAILED' | 'CANCELLED';

interface ScanJob {
  id: string;
  domain_id: string;
  domain_name: string;
  workflow_id: string;
  workflow_name: string;
  status: ScanJobStatus;
  steps: ScanStep[];
  total_credits: number;
  current_step: number | null;
  started_at: string | null;
  completed_at: string | null;
  created_at: string;
  created_by: string;
  cancelled_by: string | null;
  cancellation_reason: string | null;
}

type StepStatus = 'PENDING' | 'RUNNING' | 'RETRYING' | 'COMPLETED' | 'FAILED' | 'SKIPPED' | 'CANCELLED';

interface ScanStep {
  index: number;
  check_type: string;
  status: StepStatus;
  credits: number;
  attempt: number;    // current attempt (1-3)
  max_attempts: number;
  duration_seconds: number | null;
  error: string | null;
}
```

### Scan Results State

```typescript
interface ScanResults {
  scan_job_id: string;
  subdomains: Subdomain[];
  ports: Port[];
  technologies: Technology[];
  vulnerabilities: Vulnerability[];
  screenshots: Screenshot[];
  scan_results: ScanResultEntry[]; // raw scan_results records
}

interface Subdomain {
  id: string;
  domain_id: string;
  subdomain: string;
  source: string;
  first_seen: string;
  last_seen: string;
}

interface Port {
  id: string;
  subdomain_id: string;
  subdomain_name: string;
  port: number;
  protocol: string;
  service: string | null;
  banner: string | null;
  discovered_at: string;
}

interface Technology {
  id: string;
  subdomain_id: string;
  subdomain_name: string;
  tech_name: string;
  version: string | null;
  category: string;
  detected_at: string;
}

interface Vulnerability {
  id: string;
  scan_result_id: string;
  subdomain_name: string;
  cve: string | null;
  severity: 'LOW' | 'MEDIUM' | 'HIGH' | 'CRITICAL';
  description: string;
  remediation: string | null;
  created_at: string;
}

interface Screenshot {
  id: string;
  subdomain_id: string;
  subdomain_name: string;
  url: string;
  storage_path: string;
  image_url: string; // resolved URL for display
  taken_at: string;
}

interface ScanResultEntry {
  id: string;
  scan_job_id: string;
  check_type: string;
  target: string;
  data_json: Record<string, unknown>;
  severity: 'INFO' | 'LOW' | 'MEDIUM' | 'HIGH' | 'CRITICAL' | null;
  created_at: string;
}
```

### Workflow State

```typescript
interface Workflow {
  id: string;
  name: string;
  template_id: string | null;
  steps_json: WorkflowStepDefinition[];
  is_system: boolean;
  description: string | null;
  created_by: string;
  created_at: string;
  updated_at: string;
}

interface WorkflowStepDefinition {
  check_type: string;
  config?: Record<string, unknown>;
}

interface WorkflowTemplate {
  id: string;
  name: string;
  description: string | null;
  steps_json: WorkflowStepDefinition[];
  is_system: boolean;
}
```

### Schedule State

```typescript
interface ScanSchedule {
  id: string;
  domain_id: string;
  domain_name: string;
  workflow_id: string;
  workflow_name: string;
  cron_expression: string;
  cron_human: string; // human-readable description
  enabled: boolean;
  disabled_reason: string | null; // 'plan_downgraded', 'domain_deleted', etc.
  estimated_credits: number;
  last_run_at: string | null;
  next_run_at: string | null;
  created_at: string;
  created_by: string;
}

interface ScheduleListState {
  schedules: ScanSchedule[];
  activeCount: number;
  maxActive: number; // 10
  isLoading: boolean;
}
```

### Page-Level States

```typescript
interface ScanListState {
  scans: ScanJob[];
  totalCount: number;
  currentPage: number;
  pageSize: number;
  statusFilter: ScanJobStatus | 'ALL';
  domainFilter: string | 'ALL'; // domain_id
  dateFilter: 'TODAY' | 'LAST_7_DAYS' | 'LAST_30_DAYS' | 'ALL_TIME';
  isLoading: boolean;
}

interface ScanDetailsState {
  scanJob: ScanJob | null;
  results: ScanResults | null;
  activeResultsTab: string; // check_type
  isLoading: boolean;
  isPolling: boolean; // true while QUEUED or RUNNING
  error: string | null;
}

interface NewScanState {
  selectedDomainId: string | null;
  selectedWorkflowId: string | null;
  creditEstimate: CreditEstimate | null; // from billing types
  isEstimating: boolean;
  isSubmitting: boolean;
  filteredSteps: string[]; // steps removed by feature flags
  domainHasActiveScan: boolean;
}

interface WorkflowBuilderState {
  name: string;
  selectedSteps: WorkflowStepDefinition[];
  availableStepTypes: AvailableStep[];
  isSubmitting: boolean;
  errors: Record<string, string>;
}

interface AvailableStep {
  check_type: string;
  label: string;
  description: string;
  available: boolean; // false if tier-locked
  tier_required: string | null; // e.g., "Enterprise"
}

interface NewScheduleState {
  selectedDomainId: string | null;
  selectedWorkflowId: string | null;
  frequencyType: 'DAILY' | 'WEEKLY' | 'MONTHLY' | 'CUSTOM';
  cronExpression: string;
  cronHuman: string;
  time: string; // HH:MM
  dayOfWeek: number; // 0-6 for weekly
  dayOfMonth: number; // 1-31 for monthly
  creditEstimate: number | null;
  isSubmitting: boolean;
  errors: Record<string, string>;
}
```

### Admin State

```typescript
interface AdminScanLimitsState {
  tenants: EnterpriseTenantLimit[];
  searchQuery: string;
  isLoading: boolean;
}

interface EnterpriseTenantLimit {
  tenant_id: string;
  tenant_name: string;
  plan_name: string;
  max_concurrent_scans: number;
  is_custom: boolean; // false = using default (10)
}
```

---

## API Integration

### Endpoint Table

| Method | Path | Description | Request | Response |
|--------|------|-------------|---------|----------|
| `POST` | `/api/domains` | Add domain | `{ domain: string }` | `Domain` |
| `GET` | `/api/domains` | List domains | — | `Domain[]` |
| `GET` | `/api/domains/{id}` | Get domain details | — | `DomainDetails` |
| `DELETE` | `/api/domains/{id}` | Delete domain | — | `void` |
| `GET` | `/api/domains/{id}/subdomains` | List subdomains | `?page=&size=` | `Paginated<Subdomain>` |
| `GET` | `/api/domains/{id}/ports` | List ports | `?page=&size=` | `Paginated<Port>` |
| `GET` | `/api/domains/{id}/technologies` | List technologies | `?page=&size=` | `Paginated<Technology>` |
| `GET` | `/api/domains/{id}/scans` | List scans for domain | `?page=&size=` | `Paginated<ScanJob>` |
| `POST` | `/api/scans` | Create scan | `CreateScanRequest` | `ScanJob` |
| `GET` | `/api/scans` | List scan jobs | `?status=&domain=&page=&size=` | `Paginated<ScanJob>` |
| `GET` | `/api/scans/{id}` | Get scan details + steps | — | `ScanJob` |
| `GET` | `/api/scans/{id}/results` | Get scan results | `?check_type=` | `ScanResults` |
| `DELETE` | `/api/scans/{id}` | Cancel scan | — | `ScanJob` |
| `GET` | `/api/workflows` | List workflows | — | `Workflow[]` |
| `POST` | `/api/workflows` | Create custom workflow | `CreateWorkflowRequest` | `Workflow` |
| `GET` | `/api/workflows/{id}` | Get workflow details | — | `Workflow` |
| `PUT` | `/api/workflows/{id}` | Update custom workflow | `UpdateWorkflowRequest` | `Workflow` |
| `DELETE` | `/api/workflows/{id}` | Delete custom workflow | — | `void` |
| `POST` | `/api/scans/schedules` | Create schedule | `CreateScheduleRequest` | `ScanSchedule` |
| `GET` | `/api/scans/schedules` | List schedules | — | `ScanSchedule[]` |
| `GET` | `/api/scans/schedules/{id}` | Get schedule details | — | `ScanSchedule` |
| `PUT` | `/api/scans/schedules/{id}` | Update schedule | `UpdateScheduleRequest` | `ScanSchedule` |
| `DELETE` | `/api/scans/schedules/{id}` | Delete schedule | — | `void` |
| `POST` | `/api/scans/schedules/{id}/enable` | Enable schedule | — | `ScanSchedule` |
| `POST` | `/api/scans/schedules/{id}/disable` | Disable schedule | — | `ScanSchedule` |
| `GET` | `/api/admin/tenants/scan-limits` | List Enterprise tenant limits | `?q=` | `EnterpriseTenantLimit[]` |
| `PUT` | `/api/admin/tenants/{id}/scan-limit` | Set concurrent limit | `{ max_concurrent_scans: number }` | `EnterpriseTenantLimit` |
| `DELETE` | `/api/admin/tenants/{id}/scan-limit` | Reset to default | — | `EnterpriseTenantLimit` |

### Request Types

```typescript
interface CreateScanRequest {
  domain_id: string;
  workflow_id: string;
}

interface CreateWorkflowRequest {
  name: string;
  steps_json: WorkflowStepDefinition[];
}

interface UpdateWorkflowRequest {
  name?: string;
  steps_json?: WorkflowStepDefinition[];
}

interface CreateScheduleRequest {
  domain_id: string;
  workflow_id: string;
  cron_expression: string;
}

interface UpdateScheduleRequest {
  workflow_id?: string;
  cron_expression?: string;
}
```

### Response Types

```typescript
interface DomainDetails extends Domain {
  subdomain_count: number;
  port_count: number;
  technology_count: number;
  latest_scan: ScanJob | null;
}

interface Paginated<T> {
  data: T[];
  total: number;
  page: number;
  page_size: number;
  total_pages: number;
}
```

### Polling Pattern (Running Scans)

```typescript
// Poll scan details every 10 seconds while non-terminal
function useScanPolling(scanId: string) {
  const [scanJob, setScanJob] = useState<ScanJob | null>(null);
  const intervalRef = useRef<number | null>(null);

  useEffect(() => {
    const fetchScan = async () => {
      const data = await api.get(`/api/scans/${scanId}`);
      setScanJob(data);

      const terminalStates: ScanJobStatus[] = [
        'COMPLETED', 'PARTIAL', 'FAILED', 'CANCELLED'
      ];
      if (terminalStates.includes(data.status)) {
        // Stop polling, fetch results
        if (intervalRef.current) clearInterval(intervalRef.current);
        fetchResults(scanId);
      }
    };

    fetchScan(); // initial fetch
    intervalRef.current = window.setInterval(fetchScan, 10_000);

    return () => {
      if (intervalRef.current) clearInterval(intervalRef.current);
    };
  }, [scanId]);

  return scanJob;
}
```

### Cron Expression Helpers

```typescript
// Convert frequency presets to cron expressions
function frequencyToCron(
  type: 'DAILY' | 'WEEKLY' | 'MONTHLY' | 'CUSTOM',
  time: string, // "HH:MM"
  dayOfWeek?: number,
  dayOfMonth?: number
): string {
  const [hour, minute] = time.split(':').map(Number);

  switch (type) {
    case 'DAILY':
      return `${minute} ${hour} * * *`;
    case 'WEEKLY':
      return `${minute} ${hour} * * ${dayOfWeek}`;
    case 'MONTHLY':
      return `${minute} ${hour} ${dayOfMonth} * *`;
    case 'CUSTOM':
      return ''; // user provides raw cron
  }
}

// Validate minimum 24-hour interval
function validateCronInterval(cron: string): boolean {
  // Parse and check that next two occurrences are >= 24h apart
  // Return false if sub-daily
}

// Human-readable cron description
function cronToHuman(cron: string): string {
  // "0 0 * * *" → "Every day at 00:00 UTC"
  // "0 0 * * 1" → "Every Monday at 00:00 UTC"
  // "0 0 1 * *" → "1st of every month at 00:00 UTC"
}
```

### Caching & Refresh Strategy

| Data | Cache Strategy | Invalidation |
|------|---------------|--------------|
| Domain list | Cache, refresh on mutation | After add/delete domain |
| Domain details | Cache per domain ID | After scan completion |
| Scan list | No cache (paginated, filtered) | — |
| Scan details | Cache per scan ID, poll if active | Polling replaces cache |
| Scan results | Cache per scan ID | Immutable after completion |
| Workflows | Cache, refresh on mutation | After create/edit/delete |
| Schedules | Cache, refresh on mutation | After create/toggle/delete |
| Credit estimate | No cache (computed per request) | — |

---

## Component Architecture

### Component Tree

```
<App>
├── <DomainsSection>
│   ├── <DomainListPage>                    # /domains
│   │   ├── <DomainCountBadge />
│   │   ├── <DomainTable />
│   │   ├── <AddDomainModal />
│   │   └── <DeleteDomainModal />
│   │
│   └── <DomainDetailsPage>                 # /domains/{id}
│       ├── <DomainHeader />
│       ├── <DomainOverviewTab />
│       │   ├── <SubdomainTable />
│       │   ├── <PortTable />
│       │   └── <TechnologyTable />
│       └── <DomainScanHistoryTab />
│
├── <ScansSection>
│   ├── <ScanListPage>                      # /scans
│   │   ├── <ScanFilters />
│   │   ├── <ScanTable />
│   │   └── <CancelScanModal />
│   │
│   ├── <ScanDetailsPage>                   # /scans/{id}
│   │   ├── <ScanHeader />
│   │   ├── <StepProgressPipeline />
│   │   │   └── <StepNode />               # per step
│   │   ├── <ScanStatusBanner />
│   │   └── <ScanResultsTabs />
│   │       ├── <SubdomainResultsTab />
│   │       ├── <PortResultsTab />
│   │       ├── <TechnologyResultsTab />
│   │       ├── <VulnerabilityResultsTab />
│   │       │   └── <VulnerabilityCard />
│   │       └── <ScreenshotResultsTab />
│   │           └── <ScreenshotGrid />
│   │               └── <ScreenshotLightbox />
│   │
│   ├── <NewScanPage>                       # /scans/new
│   │   ├── <DomainPicker />
│   │   ├── <WorkflowPicker />
│   │   ├── <ScanCostEstimate />
│   │   ├── <FilteredStepsNotice />
│   │   └── <InsufficientCreditsWarning />  # from billing
│   │
│   ├── <WorkflowsSection>
│   │   ├── <WorkflowListPage>              # /scans/workflows
│   │   │   ├── <WorkflowTemplateCard />    # system templates
│   │   │   └── <CustomWorkflowTable />
│   │   │
│   │   ├── <WorkflowBuilderPage>           # /scans/workflows/new
│   │   │   ├── <StepSelector />
│   │   │   └── <StepOrderList />           # drag-to-reorder
│   │   │
│   │   └── <WorkflowDetailsPage>           # /scans/workflows/{id}
│   │
│   └── <SchedulesSection>
│       ├── <ScheduleListPage>              # /scans/schedules
│       │   ├── <ScheduleTable />
│       │   ├── <ScheduleToggle />
│       │   └── <DeleteScheduleModal />
│       │
│       └── <NewSchedulePage>               # /scans/schedules/new
│           ├── <DomainPicker />            # reused
│           ├── <WorkflowPicker />          # reused
│           ├── <FrequencySelector />
│           ├── <CronInput />
│           └── <ScheduleCreditEstimate />
│
└── <AdminScansSection>
    └── <AdminScanLimitsPage>               # /admin/scans/limits
        ├── <TenantSearch />                # reused from billing
        ├── <TenantLimitTable />
        └── <EditLimitModal />
```

### Key Component Specifications

#### `<StepProgressPipeline />`

| Prop | Type | Description |
|------|------|-------------|
| `steps` | `ScanStep[]` | Ordered list of scan steps |
| `currentStep` | `number \| null` | Index of currently executing step |
| `scanStatus` | `ScanJobStatus` | Overall scan status |

| Responsibility |
|---------------|
| Render horizontal pipeline of step nodes connected by arrows |
| Each node shows: step name, status icon, duration |
| Animate running step (spinner) |
| Show retry indicator with attempt count |
| Grey out skipped/cancelled steps |
| Responsive: stack vertically on narrow screens |

---

#### `<StepNode />`

| Prop | Type | Description |
|------|------|-------------|
| `step` | `ScanStep` | Step data |
| `isActive` | `boolean` | Is this the current step |

| Status → Visual Mapping |
|-------------------------|
| PENDING → grey circle `○` |
| RUNNING → blue spinner `◉` |
| RETRYING → orange spinner `⟳` + "attempt N/3" |
| COMPLETED → green check `✓` + duration |
| FAILED → red X `✗` + error tooltip |
| SKIPPED → grey dash `—` + "dependency failed" tooltip |
| CANCELLED → grey X `✕` |

---

#### `<ScanResultsTabs />`

| Prop | Type | Description |
|------|------|-------------|
| `results` | `ScanResults` | All scan results |
| `completedSteps` | `string[]` | Check types that completed |
| `activeTab` | `string` | Currently active tab |
| `onTabChange` | `(tab: string) => void` | Tab switch handler |

| Responsibility |
|---------------|
| Render tabs only for steps that produced results |
| Show result count in tab label: "Subdomains (42)" |
| Each tab renders appropriate results table/grid |
| Maintain active tab on data refresh |

---

#### `<DomainPicker />`

| Prop | Type | Description |
|------|------|-------------|
| `domains` | `Domain[]` | Available domains |
| `selectedId` | `string \| null` | Currently selected |
| `onSelect` | `(id: string) => void` | Selection handler |
| `preselectedId` | `string \| null` | From URL query param |

| Responsibility |
|---------------|
| Dropdown with searchable domain list |
| Show domain status beside each option |
| Pre-select if ID provided via query param |
| Show "No domains" empty state with [Add Domain] link |

---

#### `<WorkflowPicker />`

| Prop | Type | Description |
|------|------|-------------|
| `workflows` | `Workflow[]` | System + custom workflows |
| `selectedId` | `string \| null` | Currently selected |
| `onSelect` | `(id: string) => void` | Selection handler |

| Responsibility |
|---------------|
| Radio button list grouped by: System Templates, Custom Workflows |
| Show step count and description per workflow |
| Show step names as preview when selected |
| Custom section hidden/locked for Starter tier |

---

#### `<FrequencySelector />`

| Prop | Type | Description |
|------|------|-------------|
| `frequencyType` | `string` | DAILY, WEEKLY, MONTHLY, CUSTOM |
| `cronExpression` | `string` | Current cron |
| `onChange` | `(type, cron) => void` | Change handler |
| `error` | `string \| null` | Validation error |

| Responsibility |
|---------------|
| Radio buttons for preset frequencies |
| Time picker for all presets |
| Day-of-week picker for WEEKLY |
| Day-of-month picker for MONTHLY |
| Raw cron input for CUSTOM with human-readable preview |
| Validate 24-hour minimum interval |

---

#### `<VulnerabilityCard />`

| Prop | Type | Description |
|------|------|-------------|
| `vulnerability` | `Vulnerability` | Vuln data |

| Responsibility |
|---------------|
| Display CVE ID, severity badge, subdomain, description |
| Severity badge colors: CRITICAL (red), HIGH (orange), MEDIUM (yellow), LOW (blue) |
| Show remediation if available |
| Expandable for long descriptions |

---

#### `<ScreenshotGrid />`

| Prop | Type | Description |
|------|------|-------------|
| `screenshots` | `Screenshot[]` | Screenshot list |
| `onSelect` | `(screenshot: Screenshot) => void` | Open lightbox |

| Responsibility |
|---------------|
| Thumbnail grid layout (3 columns) |
| Show subdomain URL under each thumbnail |
| Click opens `<ScreenshotLightbox />` |
| Lazy-load images for performance |

---

### Shared / Reusable Components

| Component | Used By | Notes |
|-----------|---------|-------|
| `<Modal />` | Add/delete domain, cancel scan, delete schedule, edit limit | Standard modal |
| `<Toast />` | All success/error notifications | Global provider |
| `<Pagination />` | Domain details, scan list, results tables | Page controls |
| `<SkeletonLoader />` | All loading states | Configurable |
| `<SearchInput />` | Admin tenant search | Debounced, reused from billing |
| `<EmptyState />` | Domain list, scan list, schedules | Icon + message + CTA |
| `<StatusBadge />` | Scan list, scan details, schedules | Color-coded status label |
| `<DomainPicker />` | New scan, new schedule | Searchable dropdown |
| `<WorkflowPicker />` | New scan, new schedule | Grouped radio list |
| `<FeatureGate />` | Custom workflows, schedules | Upgrade prompt wrapper |
| `<SeverityBadge />` | Vulnerability results | Color-coded severity |

---

## File Structure

### NEW Files

```
src/
├── pages/
│   ├── domains/
│   │   ├── DomainListPage.tsx
│   │   └── DomainDetailsPage.tsx
│   ├── scans/
│   │   ├── ScanListPage.tsx
│   │   ├── ScanDetailsPage.tsx
│   │   ├── NewScanPage.tsx
│   │   ├── workflows/
│   │   │   ├── WorkflowListPage.tsx
│   │   │   ├── WorkflowBuilderPage.tsx
│   │   │   └── WorkflowDetailsPage.tsx
│   │   └── schedules/
│   │       ├── ScheduleListPage.tsx
│   │       └── NewSchedulePage.tsx
│   └── admin/
│       └── scans/
│           └── AdminScanLimitsPage.tsx
│
├── components/
│   ├── domains/
│   │   ├── DomainTable.tsx
│   │   ├── DomainCountBadge.tsx
│   │   ├── DomainHeader.tsx
│   │   ├── AddDomainModal.tsx
│   │   ├── DeleteDomainModal.tsx
│   │   ├── SubdomainTable.tsx
│   │   ├── PortTable.tsx
│   │   └── TechnologyTable.tsx
│   ├── scans/
│   │   ├── ScanFilters.tsx
│   │   ├── ScanTable.tsx
│   │   ├── ScanHeader.tsx
│   │   ├── StepProgressPipeline.tsx
│   │   ├── StepNode.tsx
│   │   ├── ScanStatusBanner.tsx
│   │   ├── ScanResultsTabs.tsx
│   │   ├── SubdomainResultsTab.tsx
│   │   ├── PortResultsTab.tsx
│   │   ├── TechnologyResultsTab.tsx
│   │   ├── VulnerabilityResultsTab.tsx
│   │   ├── VulnerabilityCard.tsx
│   │   ├── ScreenshotResultsTab.tsx
│   │   ├── ScreenshotGrid.tsx
│   │   ├── ScreenshotLightbox.tsx
│   │   ├── CancelScanModal.tsx
│   │   ├── DomainPicker.tsx
│   │   ├── WorkflowPicker.tsx
│   │   ├── ScanCostEstimate.tsx
│   │   └── FilteredStepsNotice.tsx
│   ├── workflows/
│   │   ├── WorkflowTemplateCard.tsx
│   │   ├── CustomWorkflowTable.tsx
│   │   ├── StepSelector.tsx
│   │   └── StepOrderList.tsx
│   ├── schedules/
│   │   ├── ScheduleTable.tsx
│   │   ├── ScheduleToggle.tsx
│   │   ├── FrequencySelector.tsx
│   │   ├── CronInput.tsx
│   │   ├── ScheduleCreditEstimate.tsx
│   │   └── DeleteScheduleModal.tsx
│   └── shared/
│       ├── StatusBadge.tsx
│       ├── SeverityBadge.tsx
│       └── FeatureGate.tsx
│
├── hooks/
│   ├── domains/
│   │   ├── useDomains.ts
│   │   └── useDomainDetails.ts
│   ├── scans/
│   │   ├── useScanList.ts
│   │   ├── useScanDetails.ts
│   │   ├── useScanPolling.ts
│   │   ├── useScanResults.ts
│   │   └── useCreateScan.ts
│   ├── workflows/
│   │   ├── useWorkflows.ts
│   │   └── useWorkflowBuilder.ts
│   └── schedules/
│       ├── useSchedules.ts
│       └── useCreateSchedule.ts
│
├── api/
│   ├── domains.ts
│   ├── scans.ts
│   ├── workflows.ts
│   └── schedules.ts
│
├── types/
│   ├── domains.ts
│   ├── scans.ts
│   ├── workflows.ts
│   └── schedules.ts
│
└── utils/
    └── cron.ts                             # cron parsing/validation/humanization
```

### EXISTING Files to Modify

| File | Change |
|------|--------|
| `src/router.tsx` | Add domain, scan, workflow, schedule routes |
| `src/components/layout/Sidebar.tsx` (or nav) | Add "Domains" and "Scans" top-level nav items with sub-items |
| `src/components/layout/AdminSidebar.tsx` | Add "Scans > Concurrent Limits" admin nav item |

---

## Build Checklist

Build in this order to ensure dependencies are satisfied:

1. **Types & API layer**
   - [ ] Define types in `types/domains.ts`, `types/scans.ts`, `types/workflows.ts`, `types/schedules.ts`
   - [ ] Implement API clients in `api/domains.ts`, `api/scans.ts`, `api/workflows.ts`, `api/schedules.ts`
   - [ ] Implement cron utilities in `utils/cron.ts`

2. **Shared components**
   - [ ] `<StatusBadge />`
   - [ ] `<SeverityBadge />`
   - [ ] `<FeatureGate />`
   - [ ] `<DomainPicker />`
   - [ ] `<WorkflowPicker />`

3. **Domain management**
   - [ ] `useDomains` hook
   - [ ] `<DomainTable />`, `<DomainCountBadge />`
   - [ ] `<AddDomainModal />` with validation
   - [ ] `<DeleteDomainModal />`
   - [ ] `<DomainListPage />`
   - [ ] `useDomainDetails` hook
   - [ ] `<SubdomainTable />`, `<PortTable />`, `<TechnologyTable />`
   - [ ] `<DomainDetailsPage />` with tabs

4. **Scan creation**
   - [ ] `useCreateScan` hook
   - [ ] `<ScanCostEstimate />` (integrates with billing `useCreditEstimate`)
   - [ ] `<FilteredStepsNotice />`
   - [ ] `<NewScanPage />`

5. **Scan list**
   - [ ] `useScanList` hook (pagination + filters)
   - [ ] `<ScanFilters />`, `<ScanTable />`
   - [ ] `<CancelScanModal />`
   - [ ] `<ScanListPage />`

6. **Scan details & results**
   - [ ] `useScanDetails` + `useScanPolling` hooks
   - [ ] `<StepNode />`, `<StepProgressPipeline />`
   - [ ] `<ScanHeader />`, `<ScanStatusBanner />`
   - [ ] `useScanResults` hook
   - [ ] `<SubdomainResultsTab />`, `<PortResultsTab />`, `<TechnologyResultsTab />`
   - [ ] `<VulnerabilityCard />`, `<VulnerabilityResultsTab />`
   - [ ] `<ScreenshotGrid />`, `<ScreenshotLightbox />`, `<ScreenshotResultsTab />`
   - [ ] `<ScanResultsTabs />`
   - [ ] `<ScanDetailsPage />`

7. **Workflows**
   - [ ] `useWorkflows` hook
   - [ ] `<WorkflowTemplateCard />`, `<CustomWorkflowTable />`
   - [ ] `<WorkflowListPage />`
   - [ ] `useWorkflowBuilder` hook
   - [ ] `<StepSelector />`, `<StepOrderList />` (drag-to-reorder)
   - [ ] `<WorkflowBuilderPage />`
   - [ ] `<WorkflowDetailsPage />`

8. **Schedules**
   - [ ] `useSchedules` hook
   - [ ] `<ScheduleTable />`, `<ScheduleToggle />`, `<DeleteScheduleModal />`
   - [ ] `<ScheduleListPage />`
   - [ ] `useCreateSchedule` hook
   - [ ] `<FrequencySelector />`, `<CronInput />`, `<ScheduleCreditEstimate />`
   - [ ] `<NewSchedulePage />`

9. **Admin: Concurrent limits**
   - [ ] `<TenantLimitTable />`, `<EditLimitModal />`
   - [ ] `<AdminScanLimitsPage />`

10. **Routing & navigation**
    - [ ] Add all routes to router
    - [ ] Add Domains + Scans nav items (with sub-items for Workflows, Schedules)
    - [ ] Add admin nav item
    - [ ] Feature-gate schedule and custom workflow routes

---

## Testing Notes

### Scenarios to Cover

| Area | Test Scenarios |
|------|---------------|
| **Domain CRUD** | Add valid domain, reject invalid (IP, URL, subdomain, duplicate), delete with active scan guard, at-limit behavior |
| **Domain details** | Overview tabs with data, empty state, scan history, pagination |
| **Scan creation** | Domain + workflow selection, credit estimate, filtered steps, insufficient credits, domain with active scan |
| **Scan list** | Status/domain/date filters, pagination, running scan inline progress |
| **Scan polling** | 10s polling for QUEUED/RUNNING, stop on terminal, step status transitions |
| **Step pipeline** | All status visuals (pending/running/retrying/completed/failed/skipped/cancelled) |
| **Scan results** | Tab rendering per completed step, pagination within tabs, severity filtering |
| **Screenshots** | Grid layout, lightbox open/close, lazy loading |
| **Vulnerabilities** | Severity ordering, severity filter, CVE display, remediation |
| **Workflows** | System template display, custom workflow CRUD, step limit (15), workflow limit (20) |
| **Workflow builder** | Step selection, drag reorder, feature-gated steps, name validation |
| **Schedules** | Create with presets/custom cron, 24h min validation, enable/disable, auto-disabled display |
| **Feature gates** | Starter blocked from custom workflows/schedules, upgrade prompts shown |
| **Cancel scan** | Cancel QUEUED (full refund) vs RUNNING (partial refund), terminal state rejection |
| **Admin limits** | Search Enterprise tenants, set custom limit, reset to default |
| **Role guards** | Members see read-only, non-owners blocked from mutations |
