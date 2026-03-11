# Implementation Guide (Integrations)

Scope: State management, API integration patterns, component tree, and build checklist for tenant notification integrations and super admin platform API key management.

---

## State Interfaces

```typescript
// ─── Tenant: Integration List ───

interface IntegrationListState {
  integrations: Integration[];
  loading: boolean;
  error: string | null;
  tierLimits: TierIntegrationLimits;
}

interface Integration {
  id: string;
  tenant_id: string;
  channel: IntegrationChannel;
  name: string;
  enabled: boolean;
  config_json: EmailConfig | SlackConfig | JiraConfig | WebhookConfig | SiemConfig;
  status: IntegrationStatus;
  last_test_at: string | null;
  last_test_success: boolean | null;
  created_at: string;
  updated_at: string;
}

type IntegrationChannel = 'EMAIL' | 'SLACK' | 'JIRA' | 'WEBHOOK' | 'SIEM';
type IntegrationStatus = 'ACTIVE' | 'ERROR' | 'DISABLED' | 'LIMIT_REACHED';

interface EmailConfig {
  recipients: string[];          // max 10
  reply_to?: string;
}

interface SlackConfig {
  webhook_url: string;
  channel: string;               // display only, extracted from webhook
  mention_on_critical?: boolean;
}

interface JiraConfig {
  instance_url: string;
  project_key: string;
  issue_type: string;            // 'Bug' | 'Task' | 'Story'
  api_token: string;             // write-only, API returns has_api_token: boolean
  assignee_email?: string;
}

interface WebhookConfig {
  url: string;
  secret?: string;               // write-only, API returns has_secret: boolean
  headers?: Record<string, string>; // max 10 custom headers
}

interface SiemConfig {
  endpoint_url: string;
  format: 'CEF' | 'LEEF' | 'JSON';
  api_key?: string;              // write-only, API returns has_api_key: boolean
  include_raw_cve?: boolean;
}

interface TierIntegrationLimits {
  tier: 'STARTER' | 'PRO' | 'ENTERPRISE';
  limits: Record<IntegrationChannel, { max: number; used: number }>;
  // Starter: EMAIL=2, others=0
  // Pro: EMAIL=5, SLACK=5, others=0
  // Enterprise: all=unlimited (-1)
}

// ─── Tenant: Integration Detail (Expanded) ───

interface IntegrationDetailState {
  activeTab: 'config' | 'rules' | 'history';
  editing: boolean;
  formState: IntegrationFormState;
  rules: NotificationRule[];
  rulesLoading: boolean;
  history: DeliveryHistoryState;
  testInProgress: boolean;
}

interface IntegrationFormState {
  channel: IntegrationChannel;
  name: string;
  config_json: Partial<EmailConfig | SlackConfig | JiraConfig | WebhookConfig | SiemConfig>;
  enabled: boolean;
  errors: Record<string, string>;
  submitting: boolean;
  dirty: boolean;
}

// ─── Tenant: Notification Rules ───

interface NotificationRule {
  id: string;
  integration_id: string;
  event_type: NotificationEventType;
  severity_filter: SeverityFilter[];
  enabled: boolean;
  created_at: string;
}

type NotificationEventType =
  | 'scan_completed'
  | 'scan_failed'
  | 'vulnerability_critical'
  | 'vulnerability_high'
  | 'compliance_violation'
  | 'certificate_expiry'
  | 'scheduled_scan_missed';

type SeverityFilter = 'CRITICAL' | 'HIGH' | 'MEDIUM' | 'LOW';

interface RuleFormState {
  event_type: NotificationEventType | null;
  severity_filter: SeverityFilter[];
  enabled: boolean;
  errors: Record<string, string>;
  submitting: boolean;
}

// ─── Tenant: Delivery History ───

interface DeliveryHistoryState {
  entries: DeliveryHistoryEntry[];
  loading: boolean;
  page: number;
  totalPages: number;
  hasMore: boolean;
}

interface DeliveryHistoryEntry {
  id: string;
  integration_id: string;
  event_type: NotificationEventType;
  status: 'DELIVERED' | 'FAILED' | 'PENDING' | 'RETRYING';
  attempts: number;
  max_attempts: number;          // always 4
  last_attempt_at: string;
  next_retry_at: string | null;
  error_message: string | null;
  created_at: string;
}

// ─── Tenant: Add Integration Modal ───

interface AddIntegrationState {
  step: 'channel_select' | 'configure';
  selectedChannel: IntegrationChannel | null;
  formState: IntegrationFormState;
}

// ─── Admin: API Key Pool ───

interface AdminApiKeyState {
  keys: ApiKey[];
  loading: boolean;
  error: string | null;
  groupedByProvider: Record<string, ApiKey[]>;
}

interface ApiKey {
  id: string;
  provider: string;              // 'NVD' | 'SHODAN' | 'CENSYS' | 'VIRUSTOTAL' | etc.
  key_name: string;
  status: ApiKeyStatus;
  quota_limit: number;
  quota_used: number;
  quota_reset_at: string;
  rate_limit_rpm: number;
  created_at: string;
  last_used_at: string | null;
  disabled_at: string | null;
}

type ApiKeyStatus = 'ACTIVE' | 'QUOTA_EXCEEDED' | 'RATE_LIMITED' | 'DISABLED' | 'EXPIRED';

interface ApiKeyFormState {
  provider: string;
  key_name: string;
  api_key: string;               // write-only
  quota_limit: number;
  rate_limit_rpm: number;
  errors: Record<string, string>;
  submitting: boolean;
}

// ─── Admin: Usage Dashboard ───

interface AdminUsageDashboardState {
  summary: UsageSummary;
  providerBreakdown: ProviderUsage[];
  timeRange: '24h' | '7d' | '30d';
  loading: boolean;
}

interface UsageSummary {
  total_requests: number;
  total_errors: number;
  error_rate: number;
  active_keys: number;
  exhausted_keys: number;
}

interface ProviderUsage {
  provider: string;
  requests: number;
  errors: number;
  quota_used_pct: number;
  keys_active: number;
  keys_total: number;
}

// ─── Admin: Provider Config ───

interface AdminProviderConfigState {
  providers: ProviderConfig[];
  loading: boolean;
  editingId: string | null;
  formState: ProviderFormState;
}

interface ProviderConfig {
  id: string;
  name: string;
  base_url: string;
  default_rate_limit_rpm: number;
  circuit_breaker_threshold: number;
  enabled: boolean;
  created_at: string;
}

interface ProviderFormState {
  default_rate_limit_rpm: number;
  circuit_breaker_threshold: number;
  enabled: boolean;
  errors: Record<string, string>;
  submitting: boolean;
}
```

---

## API Endpoints

### Tenant Integration Endpoints

| Method | Endpoint | Request | Response | Used By |
|--------|----------|---------|----------|---------|
| GET | `/api/integrations` | — | `{ integrations: Integration[], limits: TierIntegrationLimits }` | Integration list page load |
| POST | `/api/integrations` | `{ channel, name, config_json, enabled }` | `{ integration: Integration }` | Add integration submit |
| GET | `/api/integrations/{id}` | — | `{ integration: Integration }` | Detail expansion (if not in list) |
| PUT | `/api/integrations/{id}` | `{ name?, config_json?, enabled? }` | `{ integration: Integration }` | Edit integration save |
| DELETE | `/api/integrations/{id}` | — | `204 No Content` | Delete integration confirmed |
| POST | `/api/integrations/{id}/test` | — | `{ success: boolean, message: string }` | [Test] button click |
| PUT | `/api/integrations/{id}/toggle` | `{ enabled: boolean }` | `{ integration: Integration }` | Enable/disable toggle |
| GET | `/api/integrations/{id}/rules` | — | `{ rules: NotificationRule[] }` | Rules tab load |
| POST | `/api/integrations/{id}/rules` | `{ event_type, severity_filter, enabled }` | `{ rule: NotificationRule }` | Add rule submit |
| PUT | `/api/integrations/{id}/rules/{ruleId}` | `{ severity_filter?, enabled? }` | `{ rule: NotificationRule }` | Edit rule save |
| DELETE | `/api/integrations/{id}/rules/{ruleId}` | — | `204 No Content` | Delete rule confirmed |
| GET | `/api/integrations/{id}/history` | `?page=&per_page=20` | `{ entries: DeliveryHistoryEntry[], total, page, total_pages }` | History tab load |

### Tenant Request Types

```typescript
interface CreateIntegrationRequest {
  channel: IntegrationChannel;
  name: string;
  config_json: EmailConfig | SlackConfig | JiraConfig | WebhookConfig | SiemConfig;
  enabled: boolean;
}

interface UpdateIntegrationRequest {
  name?: string;
  config_json?: Partial<EmailConfig | SlackConfig | JiraConfig | WebhookConfig | SiemConfig>;
  enabled?: boolean;
}

interface CreateRuleRequest {
  event_type: NotificationEventType;
  severity_filter: SeverityFilter[];
  enabled: boolean;
}

interface UpdateRuleRequest {
  severity_filter?: SeverityFilter[];
  enabled?: boolean;
}
```

### Admin Integration Endpoints

| Method | Endpoint | Request | Response | Used By |
|--------|----------|---------|----------|---------|
| GET | `/api/admin/integrations/keys` | — | `{ keys: ApiKey[] }` | API Keys tab load |
| POST | `/api/admin/integrations/keys` | `{ provider, key_name, api_key, quota_limit, rate_limit_rpm }` | `{ key: ApiKey }` | Add key submit |
| PUT | `/api/admin/integrations/keys/{id}` | `{ quota_limit?, rate_limit_rpm? }` | `{ key: ApiKey }` | Edit key save |
| DELETE | `/api/admin/integrations/keys/{id}` | — | `204 No Content` | Delete key confirmed |
| POST | `/api/admin/integrations/keys/{id}/rotate` | `{ new_api_key }` | `{ key: ApiKey }` | Rotate key submit |
| PUT | `/api/admin/integrations/keys/{id}/toggle` | `{ enabled: boolean }` | `{ key: ApiKey }` | Enable/disable key |
| POST | `/api/admin/integrations/keys/{id}/reset-quota` | — | `{ key: ApiKey }` | Reset quota confirmed |
| GET | `/api/admin/integrations/usage` | `?range=24h\|7d\|30d` | `{ summary: UsageSummary, providers: ProviderUsage[] }` | Usage dashboard load |
| GET | `/api/admin/integrations/providers` | — | `{ providers: ProviderConfig[] }` | Provider config tab load |
| PUT | `/api/admin/integrations/providers/{id}` | `{ default_rate_limit_rpm?, circuit_breaker_threshold?, enabled? }` | `{ provider: ProviderConfig }` | Edit provider save |

### Admin Request Types

```typescript
interface CreateApiKeyRequest {
  provider: string;
  key_name: string;
  api_key: string;
  quota_limit: number;
  rate_limit_rpm: number;
}

interface RotateApiKeyRequest {
  new_api_key: string;
}

interface UpdateProviderConfigRequest {
  default_rate_limit_rpm?: number;
  circuit_breaker_threshold?: number;
  enabled?: boolean;
}
```

---

## Component Tree

```
/settings/integrations (Tenant)
└── IntegrationsPage
    ├── PageHeader
    │   ├── Title: "Integrations"
    │   ├── IntegrationCountBadge         — "{used}/{max} integrations"
    │   └── AddIntegrationButton          — opens modal, disabled if at limit
    ├── FeatureGate channel="notifications_*"
    │   └── LockedPlaceholder             — per-channel upgrade prompt
    ├── TierLimitBanner                   — shown when near/at limit
    ├── IntegrationList
    │   ├── IntegrationRow[]              — collapsed: name, channel icon, status, toggle
    │   │   ├── ChannelIcon               — EMAIL/SLACK/JIRA/WEBHOOK/SIEM icon
    │   │   ├── StatusBadge               — ACTIVE (green) / ERROR (red) / DISABLED (gray)
    │   │   ├── EnableToggle              — inline toggle
    │   │   └── ExpandChevron
    │   └── IntegrationDetail (expanded)
    │       ├── DetailTabs
    │       │   ├── ConfigTab
    │       │   │   ├── EmailConfigForm | SlackConfigForm | JiraConfigForm
    │       │   │   │   | WebhookConfigForm | SiemConfigForm
    │       │   │   ├── TestButton        — [Test Connection]
    │       │   │   └── ActionButtons     — [Save] [Delete]
    │       │   ├── RulesTab
    │       │   │   ├── RuleList
    │       │   │   │   └── RuleRow[]     — event type, severity chips, toggle, delete
    │       │   │   └── AddRuleButton     — inline form
    │       │   │       └── RuleForm      — event type dropdown, severity checkboxes, enabled
    │       │   └── HistoryTab
    │       │       ├── HistoryTable       — event, status, attempts, timestamp
    │       │       │   └── HistoryRow[]
    │       │       │       └── StatusBadge — DELIVERED/FAILED/PENDING/RETRYING
    │       │       └── Pagination
    │       └── WebhookSecretDisplay      — webhook only: masked secret + [Reveal] + [Regenerate]
    ├── EmptyState                         — "No integrations configured yet."
    └── AddIntegrationModal
        ├── ChannelSelector               — grid of channel cards
        │   └── ChannelCard[]             — icon, name, description, disabled if not in tier
        └── ChannelConfigStep
            ├── EmailConfigForm | SlackConfigForm | JiraConfigForm
            │   | WebhookConfigForm | SiemConfigForm
            └── ModalFooter               — [Back] [Create Integration]

/admin/integrations (Super Admin)
└── AdminIntegrationsPage
    ├── PageHeader: "Platform Integrations"
    ├── TabBar: [API Keys] [Usage Dashboard] [Provider Config]
    ├── ApiKeysTab
    │   ├── ProviderGroup[]               — grouped by provider
    │   │   ├── ProviderHeader            — provider name, key count badge
    │   │   └── ApiKeyTable
    │   │       └── ApiKeyRow[]
    │   │           ├── KeyNameCell        — name + status badge
    │   │           ├── QuotaBar          — progress bar (used/limit)
    │   │           ├── RateLimitCell     — "{rpm} req/min"
    │   │           ├── LastUsedCell      — relative timestamp
    │   │           └── ActionMenu        — [Rotate] [Edit] [Disable/Enable] [Reset Quota] [Delete]
    │   ├── AddKeyButton                  — opens modal
    │   └── AddKeyModal
    │       ├── ProviderSelect
    │       ├── KeyNameInput
    │       ├── ApiKeyInput               — password field
    │       ├── QuotaLimitInput
    │       ├── RateLimitInput
    │       └── ModalFooter               — [Cancel] [Add API Key]
    ├── UsageDashboardTab
    │   ├── TimeRangeSelector             — [24h] [7d] [30d]
    │   ├── SummaryCards
    │   │   ├── TotalRequestsCard
    │   │   ├── ErrorRateCard
    │   │   ├── ActiveKeysCard
    │   │   └── ExhaustedKeysCard
    │   └── ProviderBreakdownTable
    │       └── ProviderRow[]
    │           ├── ProviderNameCell
    │           ├── RequestsCell
    │           ├── ErrorsCell
    │           ├── QuotaBar              — aggregate usage %
    │           └── KeyStatusCell         — "{active}/{total} keys"
    └── ProviderConfigTab
        └── ProviderTable
            └── ProviderRow[]
                ├── ProviderNameCell
                ├── BaseUrlCell
                ├── RateLimitCell         — editable inline
                ├── CircuitBreakerCell    — editable inline
                ├── EnableToggle
                └── ActionMenu            — [Edit] [Save]
```

---

## Key Component Specifications

### 1. IntegrationRow

```
Props:
  integration: Integration
  onToggle: (id: string, enabled: boolean) => void
  onExpand: (id: string) => void
  expanded: boolean

Behavior:
  - Collapsed: channel icon + name + status badge + enable toggle + chevron
  - Click row → expand to show IntegrationDetail with tabs
  - Toggle fires PUT /api/integrations/{id}/toggle
  - Status badge: ACTIVE=green dot, ERROR=red dot with tooltip, DISABLED=gray
  - If integration has recent delivery failures, show warning icon
```

### 2. ChannelConfigForm (polymorphic)

```
Props:
  channel: IntegrationChannel
  config: Partial<*Config>
  errors: Record<string, string>
  onSubmit: (config: *Config) => void
  mode: 'create' | 'edit'

Renders channel-specific form:
  EMAIL → recipients list input (tag-style), reply_to
  SLACK → webhook_url, mention_on_critical checkbox
  JIRA → instance_url, project_key, issue_type dropdown, api_token (password), assignee_email
  WEBHOOK → url, custom headers (key-value pairs), secret display (edit mode only)
  SIEM → endpoint_url, format dropdown (CEF/LEEF/JSON), api_key (password), include_raw_cve

Sensitive fields (api_token, secret, api_key):
  - Create mode: visible password input
  - Edit mode: "••••••••" with [Change] button to reveal input
  - API returns has_* boolean, never the actual value
```

### 3. RuleForm (inline)

```
Props:
  integrationId: string
  existingRules: NotificationRule[]
  onSave: (rule: CreateRuleRequest) => void

Behavior:
  - Event type dropdown: filters out types already configured for this integration
  - Severity filter: checkbox group (CRITICAL, HIGH, MEDIUM, LOW)
    - Only shown for event types that support severity: vulnerability_critical, vulnerability_high, compliance_violation
  - Enabled toggle: default true
  - Inline validation: event_type required
  - Submit → POST /api/integrations/{id}/rules
```

### 4. DeliveryHistoryTable

```
Props:
  integrationId: string

Behavior:
  - Fetches GET /api/integrations/{id}/history?page=1&per_page=20
  - Columns: Event Type, Status, Attempts (e.g., "3/4"), Last Attempt, Error
  - Status badge colors: DELIVERED=green, FAILED=red, PENDING=gray, RETRYING=yellow
  - FAILED rows show error_message in expandable detail
  - RETRYING rows show "Next retry: {next_retry_at}" tooltip
  - Pagination: [Previous] [Next] with page indicator
  - Auto-refresh: none (manual refresh button)
```

### 5. ApiKeyRow (Admin)

```
Props:
  apiKey: ApiKey
  onAction: (id: string, action: 'rotate' | 'disable' | 'enable' | 'reset-quota' | 'delete') => void

Behavior:
  - Quota progress bar: green (<70%), yellow (70-90%), red (>90%)
  - Status badge: ACTIVE=green, QUOTA_EXCEEDED=red, RATE_LIMITED=yellow, DISABLED=gray, EXPIRED=red outline
  - Action availability by status:
    - ACTIVE: Rotate, Disable, Delete
    - QUOTA_EXCEEDED: Rotate, Reset Quota, Disable, Delete
    - RATE_LIMITED: Rotate, Disable, Delete (rate limit auto-resets)
    - DISABLED: Enable, Delete
    - EXPIRED: Rotate (re-activates), Delete
  - Last used: relative time ("2 hours ago"), "Never" if null
```

### 6. WebhookSecretDisplay

```
Props:
  integrationId: string
  hasSecret: boolean

Behavior:
  - Default state: "••••••••••••" with [Reveal] and [Regenerate] buttons
  - [Reveal] → one-time fetch GET /api/integrations/{id}/webhook-secret
    - Display secret in monospace with [Copy] button
    - Auto-hide after 30 seconds
    - Cannot reveal again without regenerating
  - [Regenerate] → confirmation dialog → POST /api/integrations/{id}/regenerate-secret
    - Shows new secret once, then masks
    - Warning: "Existing webhook consumers will stop receiving events until updated."
```

---

## File Structure

```
src/
├── pages/
│   ├── settings/
│   │   └── integrations/
│   │       └── IntegrationsPage.tsx              [NEW]
│   └── admin/
│       └── integrations/
│           └── AdminIntegrationsPage.tsx          [NEW]
├── components/
│   └── integrations/
│       ├── IntegrationList.tsx                    [NEW]
│       ├── IntegrationRow.tsx                     [NEW]
│       ├── IntegrationDetail.tsx                  [NEW]
│       ├── AddIntegrationModal.tsx                [NEW]
│       ├── ChannelSelector.tsx                    [NEW]
│       ├── ChannelIcon.tsx                        [NEW]
│       ├── config-forms/
│       │   ├── EmailConfigForm.tsx                [NEW]
│       │   ├── SlackConfigForm.tsx                [NEW]
│       │   ├── JiraConfigForm.tsx                 [NEW]
│       │   ├── WebhookConfigForm.tsx              [NEW]
│       │   └── SiemConfigForm.tsx                 [NEW]
│       ├── RuleList.tsx                           [NEW]
│       ├── RuleForm.tsx                           [NEW]
│       ├── RuleRow.tsx                            [NEW]
│       ├── DeliveryHistoryTable.tsx               [NEW]
│       ├── WebhookSecretDisplay.tsx               [NEW]
│       ├── StatusBadge.tsx                        [NEW]
│       ├── TierLimitBanner.tsx                    [NEW]
│       ├── admin/
│       │   ├── ApiKeysTab.tsx                     [NEW]
│       │   ├── ApiKeyRow.tsx                      [NEW]
│       │   ├── ApiKeyModal.tsx                    [NEW]
│       │   ├── QuotaBar.tsx                       [NEW]
│       │   ├── UsageDashboardTab.tsx              [NEW]
│       │   ├── SummaryCards.tsx                    [NEW]
│       │   ├── ProviderBreakdownTable.tsx         [NEW]
│       │   ├── ProviderConfigTab.tsx              [NEW]
│       │   └── ProviderRow.tsx                    [NEW]
│       └── shared/
│           ├── EnableToggle.tsx                    [EXISTING — reuse from scanning]
│           └── ConfirmDialog.tsx                   [EXISTING — reuse from common]
├── hooks/
│   └── integrations/
│       ├── useIntegrations.ts                     [NEW]
│       ├── useIntegrationDetail.ts                [NEW]
│       ├── useNotificationRules.ts                [NEW]
│       ├── useDeliveryHistory.ts                  [NEW]
│       ├── useAdminApiKeys.ts                     [NEW]
│       ├── useAdminUsageDashboard.ts              [NEW]
│       └── useAdminProviderConfig.ts              [NEW]
├── services/
│   └── integrations/
│       ├── integrationService.ts                  [NEW]
│       └── adminIntegrationService.ts             [NEW]
├── types/
│   └── integrations.ts                            [NEW]
└── routes/
    └── index.tsx                                  [EXISTING — add integration routes]
```

---

## Build Checklist

```
Phase 1: Types & Services
  □ Create types/integrations.ts with all interfaces above
  □ Create integrationService.ts (tenant CRUD + rules + history + test)
  □ Create adminIntegrationService.ts (keys + usage + providers)

Phase 2: Tenant — Integration List
  □ IntegrationsPage with FeatureGate wrapper
  □ IntegrationList with empty state
  □ IntegrationRow with channel icon, status badge, enable toggle
  □ TierLimitBanner (near/at limit messaging)
  □ useIntegrations hook (fetch list + limits)

Phase 3: Tenant — Add Integration
  □ AddIntegrationModal with channel selection step
  □ ChannelSelector with tier-aware disabled cards
  □ EmailConfigForm, SlackConfigForm, JiraConfigForm, WebhookConfigForm, SiemConfigForm
  □ Client-side validation per channel schema

Phase 4: Tenant — Integration Detail
  □ IntegrationDetail with config/rules/history tabs
  □ Edit mode for config forms (sensitive field masking)
  □ WebhookSecretDisplay (reveal + regenerate)
  □ TestButton with loading state and result toast
  □ Delete integration confirmation flow
  □ useIntegrationDetail hook

Phase 5: Tenant — Notification Rules
  □ RuleList with RuleRow components
  □ RuleForm (inline add) with event type + severity filter
  □ Rule toggle and delete
  □ useNotificationRules hook

Phase 6: Tenant — Delivery History
  □ DeliveryHistoryTable with pagination
  □ Status badges (DELIVERED/FAILED/PENDING/RETRYING)
  □ Error detail expansion for failed entries
  □ useDeliveryHistory hook

Phase 7: Admin — API Keys
  □ AdminIntegrationsPage with tab navigation
  □ ApiKeysTab with provider grouping
  □ ApiKeyRow with QuotaBar + action menu
  □ ApiKeyModal (add + rotate modes)
  □ Disable/enable/delete/reset-quota confirmation flows
  □ useAdminApiKeys hook

Phase 8: Admin — Usage Dashboard
  □ UsageDashboardTab with time range selector
  □ SummaryCards (total requests, error rate, active/exhausted keys)
  □ ProviderBreakdownTable
  □ useAdminUsageDashboard hook

Phase 9: Admin — Provider Config
  □ ProviderConfigTab with inline editing
  □ ProviderRow with rate limit + circuit breaker fields
  □ Enable/disable provider toggle
  □ useAdminProviderConfig hook

Phase 10: Routes & Integration
  □ Add /settings/integrations route (tenant owner)
  □ Add /admin/integrations route (super admin)
  □ Add navigation links in sidebar
  □ Unsaved changes guards on config edit forms
  □ Verify FeatureGate per-channel gating works
```

---

## Testing Notes

```
Tenant Tests:
  - Integration CRUD: create each channel type, verify config form renders correctly
  - Tier limits: verify add button disabled when at limit, correct limit display per tier
  - Enable/disable toggle: verify optimistic update + rollback on error
  - Notification rules: add/edit/delete rules, verify event type uniqueness per integration
  - Delivery history: pagination, status badges, error detail expansion
  - Webhook secret: reveal once → auto-hide, regenerate with confirmation
  - Test connection: loading state, success/failure toast
  - Sensitive fields: never displayed in edit mode, has_* boolean flags
  - Unsaved changes: guard on config edit tab, discard confirmation

Admin Tests:
  - API key CRUD: add with all fields, rotate with new key
  - Quota visualization: progress bar thresholds (green/yellow/red)
  - Action availability: correct actions per key status
  - Usage dashboard: time range switching, summary card values
  - Provider config: inline edit, circuit breaker threshold validation
  - Reset quota: confirmation dialog, quota bar resets to 0

Cross-Cutting:
  - Feature gate per channel: locked channels show LockedPlaceholder
  - Downgraded tenant: integrations auto-disabled, read-only with upgrade banner
  - Error handling: all ERR_INT_* codes mapped to correct UI actions
  - Loading states: skeleton for list, spinner for actions
```
