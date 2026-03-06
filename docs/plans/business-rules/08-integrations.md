# 8. Integrations

> Covers: BR-INT-001 through BR-INT-012 from the original business rules.
> Error response JSON schema is defined in `00-index.md` Section 12 reference.

---

## 8.1 Integration Overview

Reconova integrations are split into two domains:

### Platform Data Source Integrations (Super Admin Managed)

Third-party security APIs consumed during scans. Reconova acts as a mediator — tenants never manage API keys.

- **Managed by:** `SUPER_ADMIN`
- **Key model:** Shared pool of platform API keys per provider
- **Cost model:** Absorbed into scan credit pricing
- **Tables:** `platform_api_keys`, `api_usage_tracking` (Control DB)

### Tenant Notification Integrations (Tenant Configured)

Outbound notifications for scan results, CVE alerts, compliance reports, and operational events.

- **Managed by:** `TENANT_OWNER`
- **Channel gating:** Feature flag (§5.1) controls channel availability per tier
- **Count limit:** Max active integrations per tier (§8.10)
- **Tables:** `integration_configs`, `notification_rules`, `notification_history` (Tenant DB)

### Integration Flow Diagram

```
PLATFORM DATA SOURCES                    TENANT NOTIFICATIONS
─────────────────────                    ────────────────────
Super Admin                              Tenant Owner
    │                                        │
    ▼                                        ▼
platform_api_keys                        integration_configs
    │                                        │
    ▼                                        ▼
Scan Worker ──► api_usage_tracking       notification_rules
    │                                        │
    ▼                                        ▼
scan_results (Tenant DB)                 Event Router
                                             │
                                             ▼
                                         notification_history
                                             │
                                             ▼
                                    Email / Slack / Jira /
                                    Webhook / SIEM
```

---

## 8.2 Field Constraints

### Control DB: `platform_api_keys`

| Field | Type | Constraints |
|-------|------|-------------|
| `id` | UUID | PK, auto-generated |
| `provider` | string | NOT NULL, must match provider registry (§8.5) |
| `api_key_encrypted` | string | NOT NULL, AES-256 encrypted |
| `rate_limit` | integer | NOT NULL, max calls per rate window (provider-specific) |
| `usage_count` | integer | NOT NULL, DEFAULT 0, resets monthly |
| `monthly_quota` | integer | NOT NULL, max calls per calendar month |
| `status` | string | NOT NULL, CHECK (`ACTIVE`, `RATE_LIMITED`, `QUOTA_EXHAUSTED`, `RETIRED`, `DISABLED`), DEFAULT `ACTIVE` |
| `added_by` | UUID | NOT NULL, FK → `users.id`, immutable |
| `created_at` | timestamp | NOT NULL, DEFAULT NOW(), immutable |

### Control DB: `api_usage_tracking`

| Field | Type | Constraints |
|-------|------|-------------|
| `id` | UUID | PK, auto-generated |
| `api_key_id` | UUID | NOT NULL, FK → `platform_api_keys.id` |
| `tenant_id` | UUID | NOT NULL, FK → `tenants.id` |
| `scan_job_id` | UUID | NOT NULL |
| `provider` | string | NOT NULL |
| `calls_made` | integer | NOT NULL, > 0 |
| `timestamp` | timestamp | NOT NULL, DEFAULT NOW(), immutable |

**Retention:** Rows older than 12 months archived and purged. Monthly aggregates retained indefinitely.

### Tenant DB: `integration_configs`

| Field | Type | Constraints |
|-------|------|-------------|
| `id` | UUID | PK, auto-generated |
| `type` | string | NOT NULL, CHECK (`EMAIL`, `SLACK`, `JIRA`, `WEBHOOK`, `SIEM`, `CUSTOM_API`) |
| `provider` | string | NOT NULL, same as `type` for notifications; provider slug for `CUSTOM_API` |
| `config_json` | jsonb | NOT NULL, schema varies by type (see §8.7, §8.6) |
| `enabled` | boolean | NOT NULL, DEFAULT true |
| `created_at` | timestamp | NOT NULL, DEFAULT NOW(), immutable |

**Notes:**
- `CUSTOM_API` type is `[POST-MVP]`
- For `WEBHOOK` type, `config_json` includes auto-generated `webhook_secret` (32-byte hex)

### Tenant DB: `notification_rules`

| Field | Type | Constraints |
|-------|------|-------------|
| `id` | UUID | PK, auto-generated |
| `integration_id` | UUID | NOT NULL, FK → `integration_configs.id`, ON DELETE CASCADE |
| `event_type` | string | NOT NULL, CHECK (`SCAN_COMPLETE`, `SCAN_FAILED`, `CVE_ALERT_CRITICAL`, `CVE_ALERT_HIGH_DIGEST`, `CREDIT_LOW`, `COMPLIANCE_REPORT_READY`, `COMPLIANCE_SCORE_CHANGE`) |
| `severity_filter` | string[] | NULLABLE, array of severity values; null = all severities |
| `enabled` | boolean | NOT NULL, DEFAULT true |

**Unique constraint:** (`integration_id`, `event_type`) — one rule per event type per integration.

### Tenant DB: `notification_history`

| Field | Type | Constraints |
|-------|------|-------------|
| `id` | UUID | PK, auto-generated |
| `rule_id` | UUID | NULLABLE, FK → `notification_rules.id`, ON DELETE SET NULL |
| `event_type` | string | NOT NULL |
| `payload_json` | jsonb | NOT NULL |
| `status` | string | NOT NULL, CHECK (`PENDING`, `DELIVERED`, `RETRY_1`, `RETRY_2`, `RETRY_3`, `FAILED`) |
| `sent_at` | timestamp | NOT NULL, DEFAULT NOW() |
| `error` | string | NULLABLE, error message on failure |

**Retention:** 90 days, then auto-purged.

---

## 8.3 Platform API Key Management

Covers BR-INT-001 (platform-managed keys only) and BR-INT-002 (key rotation & quota alerts).

**Design Principle:** Tenants never touch API keys. Reconova acts as a mediator — super admin manages a shared pool of keys per provider, and the system distributes usage across them.

**Key Pool Model:**

Each provider can have multiple active keys. The system round-robins across active keys for load distribution.

```
BR-INT-001: Add Platform API Key
──────────────────────────────────
Input: provider, api_key, rate_limit, monthly_quota
Actor: SUPER_ADMIN

1. VALIDATE provider is a known provider (see §8.5 Provider Registry)
   IF unknown → REJECT "ERR_INT_001"
2. ENCRYPT api_key using AES-256
3. SET status = ACTIVE, usage_count = 0
4. INSERT into platform_api_keys
5. LOG audit event: API_KEY_ADDED
6. RETURN key_id (never return decrypted key)
```

```
BR-INT-002: Rotate API Key
───────────────────────────
Input: key_id, new_api_key
Actor: SUPER_ADMIN

1. FETCH existing key by key_id
   IF not found → REJECT "ERR_INT_002"
2. SET old key status = RETIRED
3. ENCRYPT new_api_key using AES-256
4. INSERT new key with same provider, rate_limit, monthly_quota
5. SET new key status = ACTIVE, usage_count = 0
6. LOG audit event: API_KEY_ROTATED (old_key_id, new_key_id)
7. RETURN new key_id
```

**Key Status Lifecycle:**

| Status | Meaning |
|--------|---------|
| `ACTIVE` | In use, available for scan jobs |
| `RATE_LIMITED` | Temporarily exhausted, auto-recovers next rate window |
| `QUOTA_EXHAUSTED` | Monthly quota reached, blocked until reset or manual override |
| `RETIRED` | Replaced by rotation, no longer used |
| `DISABLED` | Manually disabled by super admin |

**State Transitions:**

| From | To | Trigger | Who |
|------|----|---------|-----|
| `ACTIVE` | `RATE_LIMITED` | Rate limit hit on API call | System |
| `RATE_LIMITED` | `ACTIVE` | Rate window resets (provider-specific) | System |
| `ACTIVE` | `QUOTA_EXHAUSTED` | usage_count >= monthly_quota | System |
| `QUOTA_EXHAUSTED` | `ACTIVE` | Monthly quota reset (1st of month) or super admin override | System / SUPER_ADMIN |
| `ACTIVE` | `RETIRED` | Key rotation (new key replaces it) | SUPER_ADMIN |
| `ACTIVE` | `DISABLED` | Super admin manually disables | SUPER_ADMIN |
| `DISABLED` | `ACTIVE` | Super admin re-enables | SUPER_ADMIN |
| `RETIRED` | — | Terminal state | — |

**Quota Alert Thresholds:**

| Threshold | Action |
|-----------|--------|
| 80% of monthly_quota | Notify super admin: `API_KEY_QUOTA_WARNING` |
| 100% of monthly_quota | Set status = `QUOTA_EXHAUSTED`, notify super admin: `API_KEY_QUOTA_EXHAUSTED` |

**Monthly Quota Reset:**

- Scheduled job runs at midnight UTC on the 1st of each month
- Resets `usage_count = 0` for all non-RETIRED keys
- Transitions `QUOTA_EXHAUSTED` keys back to `ACTIVE`
- Logs audit event: `API_KEY_QUOTA_RESET`

**Key Selection Algorithm (used by Scan Workers):**

```
SELECT_API_KEY(provider):
1. FETCH all keys WHERE provider = provider AND status = ACTIVE
   IF none available → RETURN null (triggers skip behavior per BR-INT-006)
2. ORDER BY usage_count ASC (least-used first)
3. RETURN first key
```

---

## 8.4 API Rate Limiting & Usage Tracking

Covers BR-INT-003 (rate limiting per tenant) and BR-INT-004 (API usage tracking).

**Per-Tenant Rate Limiting:**

Each provider has a configurable per-tenant rate limit to prevent one tenant from exhausting the shared pool.

```
BR-INT-003: Per-Tenant Rate Limit Check
────────────────────────────────────────
Input: tenant_id, provider, scan_job_id
Actor: System (Scan Worker)

1. FETCH rate_limit config for provider (from provider registry, see §8.5)
   - tenant_calls_per_hour: max API calls per tenant per hour
   - tenant_calls_per_day: max API calls per tenant per day
2. COUNT calls in api_usage_tracking WHERE
   tenant_id = tenant_id AND provider = provider
   AND timestamp >= NOW() - 1 HOUR
   IF count >= tenant_calls_per_hour → REJECT "ERR_INT_003"
3. COUNT calls in api_usage_tracking WHERE
   tenant_id = tenant_id AND provider = provider
   AND timestamp >= NOW() - 24 HOURS
   IF count >= tenant_calls_per_day → REJECT "ERR_INT_004"
4. PROCEED with API call
```

**Rate Limit Configuration:**

Super admin configures per-provider limits. Defaults:

| Provider | Per-Tenant/Hour | Per-Tenant/Day |
|----------|----------------|----------------|
| Shodan | 50 | 500 |
| SecurityTrails | 30 | 300 |
| Censys | 30 | 300 |
| VirusTotal | 20 | 200 | `[POST-MVP]` |
| Nuclei Templates | No limit | No limit |

Limits are stored in the provider registry (§8.5) and cached in Redis. Super admin can adjust per provider without restart.

**Usage Tracking:**

Every third-party API call is recorded in `api_usage_tracking`:

```
BR-INT-004: Track API Usage
────────────────────────────
Input: api_key_id, tenant_id, scan_job_id, provider, calls_made
Actor: System (Scan Worker)

1. INSERT into api_usage_tracking:
   - api_key_id: key used for this call
   - tenant_id: tenant who triggered the scan
   - scan_job_id: scan that initiated the call
   - provider: provider name
   - calls_made: number of API calls in this batch
   - timestamp: NOW()
2. INCREMENT usage_count on platform_api_keys by calls_made
3. CHECK quota thresholds (§8.3 Quota Alert Thresholds)
```

**Usage Aggregation Queries (Super Admin Dashboard):**

| Query | Purpose |
|-------|---------|
| SUM(calls_made) GROUP BY provider, DATE | Daily usage per provider |
| SUM(calls_made) GROUP BY tenant_id, provider | Per-tenant usage per provider |
| SUM(calls_made) GROUP BY api_key_id | Per-key utilization |
| SUM(calls_made) WHERE timestamp >= period_start GROUP BY tenant_id | Billing-period attribution |

**Data Retention:**

`api_usage_tracking` rows older than 12 months are archived and purged. Aggregated monthly summaries are retained indefinitely for cost analysis.

---

## 8.5 Data Source Provider Registry

Covers BR-INT-005 (provider availability by tier).

**Provider Registry:**

A hardcoded-in-application registry defining all supported data source providers. Not a database table — changes require deployment.

| Provider | Slug | Min Tier | Feature Flag | Purpose |
|----------|------|----------|-------------|---------|
| Shodan | `shodan` | `PRO` | `shodan_integration` | Passive recon, open ports, services |
| SecurityTrails | `securitytrails` | `PRO` | `shodan_integration` | DNS history, subdomain discovery |
| Censys | `censys` | `ENTERPRISE` | `custom_api_connectors` | Certificate transparency, hosts |
| VirusTotal | `virustotal` | `PRO` | `virustotal_integration` | URL/domain reputation `[POST-MVP]` |
| Nuclei Templates | `nuclei` | `STARTER` | `vulnerability_scanning` | Vuln scanning templates |
| Custom Connectors | `custom` | `ENTERPRISE` | `custom_api_connectors` | Tenant-defined REST APIs `[POST-MVP]` (see §8.6) |

**Tier Enum (application code):**

```csharp
public enum SubscriptionTier { Starter, Pro, Enterprise }
// Persisted as: "STARTER", "PRO", "ENTERPRISE"
```

**Provider Access Check Algorithm:**

```
BR-INT-005: Check Provider Access
──────────────────────────────────
Input: tenant_id, provider_slug
Actor: System (Scan Worker)

1. LOOKUP provider in registry by slug
   IF not found → REJECT "ERR_INT_005"
2. IF provider is POST-MVP → REJECT "ERR_INT_006"
3. EVALUATE feature flag for provider (per §5.4 Evaluation Algorithm)
   IF disabled → REJECT "ERR_INT_007"
4. RETURN provider config (rate limits, base_url, auth_type)
```

**Provider Config Shape (in application code):**

```csharp
public record ProviderConfig
{
    string Slug;
    string DisplayName;
    string FeatureFlag;
    string MinTier;          // "STARTER" | "PRO" | "ENTERPRISE"
    int TenantCallsPerHour;
    int TenantCallsPerDay;
    string BaseUrl;
    string AuthType;         // "API_KEY_HEADER" | "API_KEY_QUERY" | "BEARER"
    string AuthHeaderName;   // e.g., "X-Api-Key", "Authorization"
    bool IsPostMvp;
}
```

---

## 8.6 Custom API Connectors `[POST-MVP]`

Enterprise tenants can define custom REST API connectors to integrate proprietary or niche data sources into their scan workflows. Each connector is a full request/response template that the scan worker executes and maps results into scan data.

**Custom Connector Definition (stored in `integration_configs` with type = `CUSTOM_API`):**

The `config_json` field stores the full connector template:

```json
{
  "name": "Internal Asset DB",
  "base_url": "https://assets.corp.example.com/api",
  "auth": {
    "type": "BEARER",
    "token_encrypted": "..."
  },
  "request": {
    "method": "GET",
    "path": "/subdomains?domain={{domain}}",
    "headers": { "Accept": "application/json" },
    "timeout_seconds": 30
  },
  "response": {
    "format": "JSON",
    "results_path": "$.data.subdomains[*]",
    "field_mappings": {
      "subdomain": "$.hostname",
      "first_seen": "$.discovered_at",
      "source": "'custom:internal-asset-db'"
    }
  },
  "rate_limit": {
    "calls_per_hour": 100
  }
}
```

**Template Variables:**

| Variable | Replaced With |
|----------|--------------|
| `{{domain}}` | Target domain from scan job |
| `{{subdomain}}` | Target subdomain (if step operates on subdomains) |
| `{{scan_job_id}}` | Current scan job ID |

**Supported Auth Types:**

| Type | Fields |
|------|--------|
| `BEARER` | `token_encrypted` |
| `API_KEY_HEADER` | `header_name`, `key_encrypted` |
| `API_KEY_QUERY` | `param_name`, `key_encrypted` |
| `BASIC` | `username`, `password_encrypted` |
| `NONE` | — |

All secrets encrypted AES-256 at rest. Decrypted only at execution time by scan worker.

**Response Mapping:**

- `results_path`: JSONPath expression to extract the array of results
- `field_mappings`: maps connector response fields to Reconova's internal schema (subdomains, ports, technologies, vulnerabilities)
- Static values prefixed with `'` (e.g., `'custom:internal-asset-db'` for source attribution)

**Connector Validation Algorithm:**

```
BR-INT-005a: Create Custom Connector [POST-MVP]
────────────────────────────────────────────────
Input: name, config_json
Actor: TENANT_OWNER (Enterprise only)

1. CHECK feature flag `custom_api_connectors`
   IF disabled → REJECT "ERR_INT_007"
2. VALIDATE config_json schema:
   - base_url must be HTTPS
   - method must be GET or POST
   - timeout_seconds must be 5–60
   - results_path must be valid JSONPath
   - field_mappings must map to known Reconova fields
   IF invalid → REJECT "ERR_INT_008"
3. ENCRYPT any secret fields (token, key, password)
4. INSERT into integration_configs with type = CUSTOM_API, enabled = false
5. LOG audit event: CUSTOM_CONNECTOR_CREATED
6. RETURN integration_id
```

**Connector Test:**

```
BR-INT-005b: Test Custom Connector [POST-MVP]
──────────────────────────────────────────────
Input: integration_id
Actor: TENANT_OWNER

1. FETCH connector config
   IF not found → REJECT "ERR_INT_009"
2. EXECUTE request against base_url with a test domain (tenant's first verified domain)
3. APPLY results_path and field_mappings to response
4. RETURN:
   - http_status: actual response code
   - results_count: number of extracted results
   - sample_results: first 5 mapped results (preview)
   - errors: any mapping failures
5. Do NOT persist results — test only
```

**Connector Execution (during scan):**

```
BR-INT-005c: Execute Custom Connector [POST-MVP]
─────────────────────────────────────────────────
Input: integration_id, domain, scan_job_id
Actor: System (Scan Worker)

1. FETCH connector config, DECRYPT secrets
2. RENDER template variables ({{domain}}, etc.)
3. EXECUTE HTTP request with configured timeout
   IF timeout or HTTP error → mark step INCOMPLETE, LOG error
4. PARSE response using results_path
   IF parse fails → mark step INCOMPLETE, LOG error
5. MAP fields using field_mappings
6. WRITE mapped results to tenant DB (subdomains, ports, etc.)
7. TRACK usage in api_usage_tracking (provider = "custom:{connector_name}")
```

**Limits:**

| Constraint | Value |
|------------|-------|
| Max connectors per tenant | 10 |
| Max timeout per request | 60 seconds |
| Max response body size | 5 MB |
| base_url protocol | HTTPS only |

---

## 8.7 Notification Channel Configuration

Covers BR-INT-007 (notification config ownership) and BR-INT-010 (channel limits by tier).

**Supported Channels:**

| Channel | Type Slug | Feature Flag (§5.1) | Config Fields |
|---------|-----------|-------------------|---------------|
| Email | `EMAIL` | `notifications_email` | `recipients[]` (email addresses) |
| Slack | `SLACK` | `notifications_slack` | `webhook_url` |
| Jira | `JIRA` | `notifications_jira` | `instance_url`, `api_token_encrypted`, `project_key`, `issue_type` |
| Webhook | `WEBHOOK` | `notifications_webhook` | `endpoint_url`, `webhook_secret` (auto-generated) |
| SIEM | `SIEM` | `notifications_siem` | `syslog_host`, `syslog_port`, `protocol` (`TCP`/`UDP`/`TLS`), `format` (`SYSLOG`/`CEF`) |

**Channel Gating:** Both feature flag AND count limit apply.

- Feature flag controls whether the channel type is available at all (§5.1)
- Count limit controls how many active integrations a tenant can have (§8.10)

**Create Integration Algorithm:**

```
BR-INT-007: Create Notification Integration
────────────────────────────────────────────
Input: type, provider (= type slug), config_json
Actor: TENANT_OWNER

1. CHECK feature flag for channel type (e.g., `notifications_slack`)
   IF disabled → REJECT "ERR_INT_010"
2. COUNT active integrations for tenant (enabled = true)
   IF count >= tier limit (§8.10) → REJECT "ERR_INT_011"
3. VALIDATE config_json per channel type:
   - EMAIL: at least 1 recipient, all valid email format, max 10 recipients
   - SLACK: webhook_url must be HTTPS
   - JIRA: instance_url HTTPS, api_token present, project_key non-empty
   - WEBHOOK: endpoint_url must be HTTPS
   - SIEM: syslog_host non-empty, syslog_port 1-65535, protocol valid
   IF invalid → REJECT "ERR_INT_012"
4. ENCRYPT any secret fields (api_token, webhook_secret)
5. IF type = WEBHOOK → auto-generate webhook_secret (32-byte hex)
6. INSERT into integration_configs with enabled = true
7. LOG audit event: INTEGRATION_CREATED
8. RETURN integration_id
```

**Update Integration:**

```
BR-INT-007a: Update Notification Integration
─────────────────────────────────────────────
Input: integration_id, config_json
Actor: TENANT_OWNER

1. FETCH integration by id (tenant-scoped)
   IF not found → REJECT "ERR_INT_009"
2. VALIDATE updated config_json (same rules as create)
3. IF type = WEBHOOK AND endpoint_url changed → regenerate webhook_secret
4. UPDATE integration_configs
5. LOG audit event: INTEGRATION_UPDATED
```

**Delete Integration:**

```
BR-INT-007b: Delete Notification Integration
─────────────────────────────────────────────
Input: integration_id
Actor: TENANT_OWNER

1. FETCH integration by id (tenant-scoped)
   IF not found → REJECT "ERR_INT_009"
2. DELETE notification_rules referencing this integration (CASCADE)
3. SET notification_history.rule_id = NULL for related history (SET NULL)
4. DELETE integration_configs row
5. LOG audit event: INTEGRATION_DELETED
```

**Test Integration:**

```
BR-INT-007c: Test Notification Integration
───────────────────────────────────────────
Input: integration_id
Actor: TENANT_OWNER

1. FETCH integration by id (tenant-scoped)
   IF not found → REJECT "ERR_INT_009"
2. SEND test payload per channel type:
   - EMAIL: "Test notification from Reconova"
   - SLACK: formatted test message block
   - JIRA: create test issue with [TEST] prefix, then immediately close it
   - WEBHOOK: POST test event payload with HMAC signature
   - SIEM: send test syslog/CEF message
3. RECORD in notification_history with event_type = TEST
4. RETURN delivery status (success/failure + error message if failed)
```

---

## 8.8 Notification Event Types & Routing

Covers BR-INT-009 (notification event types).

**Event Type Registry:**

| Event Type | Slug | Default Severity | Description |
|------------|------|-----------------|-------------|
| Scan Complete | `SCAN_COMPLETE` | `INFO` | Scan job finished successfully |
| Scan Failed | `SCAN_FAILED` | `HIGH` | Scan job failed or timed out |
| CVE Alert (Critical) | `CVE_ALERT_CRITICAL` | `CRITICAL` | New critical CVE matches tenant tech stack |
| CVE Alert (High Digest) | `CVE_ALERT_HIGH_DIGEST` | `HIGH` | Daily digest of high-severity CVE matches |
| Credit Low | `CREDIT_LOW` | `WARNING` | Credits below 20% of monthly allotment |
| Compliance Report Ready | `COMPLIANCE_REPORT_READY` | `INFO` | Compliance assessment completed |
| Compliance Score Change | `COMPLIANCE_SCORE_CHANGE` | `WARNING` | Compliance score changed by ≥ 5 points from previous |

**Notification Rule Model:**

A rule links an integration to one or more event types with optional severity filtering.

```
BR-INT-009: Create Notification Rule
─────────────────────────────────────
Input: integration_id, event_type, severity_filter, enabled
Actor: TENANT_OWNER

1. FETCH integration by id (tenant-scoped)
   IF not found → REJECT "ERR_INT_009"
2. VALIDATE event_type is a known slug
   IF unknown → REJECT "ERR_INT_013"
3. CHECK for duplicate: same integration_id + event_type
   IF exists → REJECT "ERR_INT_014"
4. VALIDATE severity_filter (if provided):
   - Must be array of valid severities: CRITICAL, HIGH, WARNING, INFO
   - If null → all severities match
5. INSERT into notification_rules with enabled = enabled
6. RETURN rule_id
```

**Severity Filter Logic:**

- `severity_filter = null` → rule fires for all severities of that event type
- `severity_filter = ["CRITICAL", "HIGH"]` → rule fires only when event severity is CRITICAL or HIGH
- Useful for channels like SIEM where tenants may only want critical alerts

**Event Routing Algorithm:**

```
BR-INT-009a: Route Notification Event
──────────────────────────────────────
Input: tenant_id, event_type, severity, payload
Actor: System

1. FETCH all notification_rules WHERE
   event_type = event_type AND enabled = true
   JOIN integration_configs WHERE enabled = true
2. FOR EACH matching rule:
   a. IF rule.severity_filter IS NOT NULL
      AND severity NOT IN rule.severity_filter → SKIP
   b. ENQUEUE notification delivery job:
      - integration_id
      - rule_id
      - event_type
      - severity
      - payload
3. RETURN count of notifications enqueued
```

**Default Rules:**

When a tenant creates their first integration, no rules are auto-created. Tenant must explicitly configure which events route to which integrations. This prevents unwanted noise.

**Payload Shape (common envelope):**

```json
{
  "event_type": "SCAN_COMPLETE",
  "severity": "INFO",
  "timestamp": "2026-03-07T12:00:00Z",
  "tenant_id": "uuid",
  "data": {
    // event-specific fields
  }
}
```

**Event-Specific Data Fields:**

| Event Type | Data Fields |
|------------|------------|
| `SCAN_COMPLETE` | `scan_job_id`, `domain`, `workflow_name`, `duration_seconds`, `findings_count` |
| `SCAN_FAILED` | `scan_job_id`, `domain`, `workflow_name`, `error_reason`, `failed_step` |
| `CVE_ALERT_CRITICAL` | `cve_id`, `severity`, `affected_tech`, `affected_domains[]` |
| `CVE_ALERT_HIGH_DIGEST` | `alerts[]` (array of `{cve_id, severity, affected_tech, affected_domains[]}`) |
| `CREDIT_LOW` | `credits_remaining`, `credits_total`, `percentage_remaining` |
| `COMPLIANCE_REPORT_READY` | `assessment_id`, `framework_name`, `overall_score`, `domain` |
| `COMPLIANCE_SCORE_CHANGE` | `framework_name`, `previous_score`, `new_score`, `delta`, `domain` |

---

## 8.9 Notification Delivery & Reliability

Covers BR-INT-008 (retry logic) and BR-INT-012 (webhook security).

**Delivery Algorithm:**

```
BR-INT-008: Deliver Notification
─────────────────────────────────
Input: integration_id, rule_id, event_type, severity, payload
Actor: System (Background Worker)

1. FETCH integration config, DECRYPT secrets
2. FORMAT payload per channel type:
   - EMAIL: render HTML template with payload data
   - SLACK: build Slack Block Kit message
   - JIRA: create issue body with event details
   - WEBHOOK: JSON payload + HMAC signature header
   - SIEM: format as SYSLOG or CEF per config
3. SEND to channel endpoint
4. INSERT into notification_history:
   - rule_id, event_type, payload_json, status, sent_at
5. IF success → status = DELIVERED
6. IF failure → status = RETRY_1, schedule retry
```

**Retry Strategy (Exponential Backoff):**

| Attempt | Delay | Status on Failure |
|---------|-------|-------------------|
| 1st (initial) | immediate | `RETRY_1` |
| 2nd | 1 minute | `RETRY_2` |
| 3rd | 5 minutes | `RETRY_3` |
| 4th (final) | 15 minutes | `FAILED` |

After 3 retries (4 total attempts), notification is marked `FAILED`. No further retries.

**Notification History Statuses:**

| Status | Meaning |
|--------|---------|
| `PENDING` | Enqueued, not yet attempted |
| `DELIVERED` | Successfully sent |
| `RETRY_1` | 1st attempt failed, waiting for retry |
| `RETRY_2` | 2nd attempt failed, waiting for retry |
| `RETRY_3` | 3rd attempt failed, waiting for final retry |
| `FAILED` | All attempts exhausted |

**Webhook Security (BR-INT-012):**

```
HMAC Signature Generation:
──────────────────────────
1. SERIALIZE payload as JSON (canonical, sorted keys)
2. COMPUTE HMAC-SHA256(webhook_secret, payload_json)
3. SET header: X-Reconova-Signature: sha256={hex_digest}
4. SET header: X-Reconova-Timestamp: {unix_timestamp}
```

Tenants verify webhooks by:
1. Recomputing HMAC-SHA256 with their webhook_secret
2. Comparing with `X-Reconova-Signature` header
3. Rejecting if timestamp is older than 5 minutes (replay protection)

**Channel-Specific Delivery Details:**

| Channel | Delivery Method | Timeout | Success Criteria |
|---------|----------------|---------|-----------------|
| `EMAIL` | SMTP / transactional email service | 10s | 2xx from mail service |
| `SLACK` | POST to webhook_url | 10s | HTTP 200 |
| `JIRA` | POST to REST API `/rest/api/3/issue` | 15s | HTTP 201 |
| `WEBHOOK` | POST to endpoint_url with HMAC | 10s | HTTP 2xx |
| `SIEM` | TCP/UDP/TLS to syslog_host:syslog_port | 5s | Connection ack (TCP/TLS) or send success (UDP) |

**Failed Notification Visibility:**

- Tenant can view notification_history including failed deliveries
- Failed notifications show error message (e.g., "HTTP 403 Forbidden", "Connection timeout")
- No automatic disabling of integrations on repeated failures — tenant must investigate and fix

---

## 8.10 Integration Limits by Tier

Covers BR-INT-010 (notification channel limits).

**Active Integration Limits:**

| Tier | Max Active Integrations | Available Channels |
|------|------------------------|-------------------|
| `STARTER` | 2 | `EMAIL` |
| `PRO` | 5 | `EMAIL`, `SLACK` |
| `ENTERPRISE` | Unlimited | `EMAIL`, `SLACK`, `JIRA`, `WEBHOOK`, `SIEM` |

"Active" = `enabled = true` in `integration_configs`. Disabling an integration frees up a slot.

**Limit Enforcement:**

- Checked at integration creation (§8.7 step 2) and when re-enabling a disabled integration
- Disabled integrations do NOT count toward the limit
- On downgrade: existing integrations beyond new limit are disabled (oldest first), per §5.8 downgrade behavior

**Notification Rules Limits:**

| Constraint | Value |
|------------|-------|
| Max rules per integration | 20 |
| Max rules per tenant (total) | 50 (`STARTER`), 100 (`PRO`), Unlimited (`ENTERPRISE`) |

**Re-enable Check:**

```
BR-INT-010a: Re-enable Integration
───────────────────────────────────
Input: integration_id
Actor: TENANT_OWNER

1. FETCH integration by id (tenant-scoped)
   IF not found → REJECT "ERR_INT_009"
2. CHECK feature flag for channel type
   IF disabled → REJECT "ERR_INT_010"
3. COUNT active integrations for tenant
   IF count >= tier limit → REJECT "ERR_INT_011"
4. SET enabled = true
5. LOG audit event: INTEGRATION_ENABLED
```

---

## 8.11 Permissions Matrix

| Action | `TENANT_OWNER` | `SUPER_ADMIN` |
|--------|---------------|---------------|
| Add platform API key | — | Yes |
| Rotate platform API key | — | Yes |
| Disable/enable platform API key | — | Yes |
| View API usage (per-tenant) | Own tenant only | All tenants |
| Configure rate limits per provider | — | Yes |
| Create notification integration | Yes | — |
| Update notification integration | Yes (own) | — |
| Delete notification integration | Yes (own) | — |
| Test notification integration | Yes (own) | — |
| Enable/disable notification integration | Yes (own) | — |
| Create notification rules | Yes | — |
| Update/delete notification rules | Yes (own) | — |
| View notification history | Yes (own) | All tenants |
| Create custom connector `[POST-MVP]` | Yes (Enterprise) | — |
| Update custom connector `[POST-MVP]` | Yes (own) | — |
| Delete custom connector `[POST-MVP]` | Yes (own) | — |
| Test custom connector `[POST-MVP]` | Yes (own) | — |
| Override tenant integration limits | — | Yes |
| View platform API key quota/usage | — | Yes |

**Notes:**
- Super admin does NOT manage tenant notification integrations — tenants are fully self-service
- Super admin CAN view notification history across tenants for debugging/support
- Custom connector actions are Enterprise-gated AND `[POST-MVP]`

---

## 8.12 Edge Cases

| Scenario | Behavior |
|----------|----------|
| All API keys for a provider are `QUOTA_EXHAUSTED` or `DISABLED` | Scan step using that provider is skipped. Step marked `INCOMPLETE` in scan results. Credits for that step are refunded (per §4.10 credit refund). Tenant notified if notification rules configured for `SCAN_COMPLETE`. |
| All API keys for a provider are `RATE_LIMITED` | Scan worker waits up to 5 minutes for rate window reset. If still limited after 5 min → skip step, mark `INCOMPLETE`, refund credits. |
| Tenant hits per-tenant rate limit mid-scan | Remaining API calls for that provider in that scan are skipped. Step marked `INCOMPLETE` with partial results preserved. Credits for that step are NOT refunded (partial work done). |
| Tenant downgrades from Pro to Starter, has Slack integration | Slack feature flag disabled → Slack integrations set `enabled = false`. Integration config preserved but inactive. Count limit also reduced (5 → 2); if more than 2 Email integrations active, oldest disabled. |
| Tenant deletes an integration with pending notifications in queue | Pending notifications for that integration are delivered (integration config already loaded into job). Future notifications will fail gracefully (integration not found → mark `FAILED`, no retry). |
| Webhook endpoint returns 3xx redirect | Do NOT follow redirects. Treat as failure. Tenant must configure the final URL directly. |
| Webhook endpoint returns 2xx but with error body | Treat as success (HTTP status is the contract). Payload is logged in notification_history for tenant debugging. |
| Notification payload exceeds channel limit (e.g., Slack 3000 char block) | Truncate payload with `... [truncated]` suffix. Include link to full details in Reconova dashboard. |
| Tenant configures same email address in two separate EMAIL integrations | Allowed. Each integration is independent. Recipient may get duplicate notifications if rules overlap — tenant's responsibility. |
| JIRA API token expires or becomes invalid | Delivery fails, retries per §8.9. After final failure, notification marked `FAILED`. Integration stays enabled — tenant sees failures in history and must update token. |
| Super admin disables a provider's operational flag while scans are in progress | Running scan steps for that provider complete (already executing). New scans will skip that provider at step execution time. |
| Platform API key monthly quota resets while key is `DISABLED` | `usage_count` resets to 0 but status stays `DISABLED`. Super admin must manually re-enable. |
| Tenant creates max notification rules then disables an integration | Disabled integration's rules still count toward total rule limit. Tenant must delete rules to free up slots. |
| SIEM endpoint unreachable (UDP) | UDP has no delivery guarantee. Status set to `DELIVERED` on send (fire-and-forget). For guaranteed delivery, tenant should use TCP or TLS protocol. |

---

## 8.13 Error Codes

| Code | HTTP | Message | Cause |
|------|------|---------|-------|
| `ERR_INT_001` | 400 | Unknown provider | Provider slug not found in registry |
| `ERR_INT_002` | 404 | API key not found | Key ID does not exist |
| `ERR_INT_003` | 429 | Hourly rate limit exceeded for this provider | Tenant exceeded per-tenant hourly call limit |
| `ERR_INT_004` | 429 | Daily rate limit exceeded for this provider | Tenant exceeded per-tenant daily call limit |
| `ERR_INT_005` | 400 | Unknown provider | Provider slug not in registry (scan worker context) |
| `ERR_INT_006` | 403 | This provider is not yet available | Provider is marked `[POST-MVP]` |
| `ERR_INT_007` | 403 | This integration is not available on your plan | Feature flag disabled for tenant's tier |
| `ERR_INT_008` | 400 | Invalid connector configuration | Custom connector config_json validation failed `[POST-MVP]` |
| `ERR_INT_009` | 404 | Integration not found | Integration ID not found or not owned by tenant |
| `ERR_INT_010` | 403 | This notification channel is not available on your plan | Channel feature flag disabled for tenant's tier |
| `ERR_INT_011` | 403 | Integration limit reached for your plan | Tenant at max active integrations for tier |
| `ERR_INT_012` | 400 | Invalid integration configuration | Config validation failed (bad URL, missing fields, etc.) |
| `ERR_INT_013` | 400 | Unknown event type | Event type slug not in registry |
| `ERR_INT_014` | 409 | Rule already exists for this event type on this integration | Duplicate integration_id + event_type combination |
| `ERR_INT_015` | 400 | Maximum notification rules reached | Tenant at max total rules for tier |
| `ERR_INT_016` | 400 | Maximum rules per integration reached | Integration at 20 rules limit |
