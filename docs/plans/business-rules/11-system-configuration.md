# 11. System Configuration Reference

> Covers: Configuration management system, master config reference, config versioning, rollback, approval workflows, and caching.
> Error response JSON schema is defined in `12-error-response-schema.md`.

---

### 11.1 Configuration Categories

> Covers: BR-CFG-001, BR-CFG-002

Two tiers of configuration:

#### Bootstrap Configs (Environment Variables)

Minimal set needed before the app can read from the database:

| Config | Env Var | Example |
|--------|---------|---------|
| Control DB connection string | `CONTROL_DB_CONNECTION` | `Host=localhost;Database=reconova_control;...` |
| Redis connection string | `REDIS_CONNECTION` | `localhost:6379` |
| Encryption master key | `ENCRYPTION_MASTER_KEY` | AES-256 key for encrypting API keys at rest |
| App environment | `APP_ENVIRONMENT` | `production`, `staging`, `development` |

#### Business Configs (Database)

Everything else, stored in `system_config` table in the control DB. Hot-reloadable, audited, versioned.

#### Rules

**BR-CFG-001: Config tier separation** — Bootstrap configs are ONLY for values required to establish database and cache connections. All other configs MUST be in the `system_config` table. If a value can be changed at runtime without a deploy, it belongs in the database.

**BR-CFG-002: Bootstrap config immutability at runtime** — Bootstrap env vars cannot be changed via the admin UI. Changes require a redeploy. This is by design — these are infrastructure-level values.

---

### 11.2 System Config Table

> Covers: BR-CFG-003, BR-CFG-004

**BR-CFG-003: Config storage schema:**

#### `system_config` table (control DB)

| Field | Type | Constraints |
|-------|------|-------------|
| `id` | uuid | PK. Immutable. |
| `key` | string(200) | NOT NULL. Unique. Lowercase dot-notation: `auth.jwt.access_token_ttl_minutes`. Immutable after creation. |
| `value` | text | NOT NULL. Stored as string. Application parses by `data_type`. |
| `data_type` | string | NOT NULL. CHECK (`STRING`, `INTEGER`, `BOOLEAN`, `DECIMAL`, `JSON`, `DURATION`). |
| `category` | string(50) | NOT NULL. Domain grouping: `auth`, `tenant`, `billing`, `scanning`, `feature_flags`, `compliance`, `cve`, `integrations`, `admin`, `data`, `rate_limit`, `versioning`. |
| `description` | string(500) | NOT NULL. Human-readable description for admin UI. |
| `default_value` | text | NOT NULL. Factory default. Used when value is reset. |
| `min_value` | text | NULL. Minimum allowed value (for INTEGER, DECIMAL, DURATION). |
| `max_value` | text | NULL. Maximum allowed value (for INTEGER, DECIMAL, DURATION). |
| `allowed_values` | text | NULL. Comma-separated list of valid values (for STRING enums). |
| `is_sensitive` | boolean | NOT NULL. Default `false`. If true, value is masked in admin UI and audit logs. |
| `is_critical` | boolean | NOT NULL. Default `false`. If true, changes require approval workflow (BR-CFG-008). |
| `requires_restart` | boolean | NOT NULL. Default `false`. If true, change only takes effect after service restart. |
| `updated_by` | uuid | NULL. FK -> `users.id`. Last user who changed this config. |
| `updated_at` | timestamp | NULL. Last update timestamp. |
| `created_at` | timestamp | NOT NULL. Auto-set on insert. |

**BR-CFG-004: Config value validation** — Before any config value is saved:

```
BR-CFG-004A: Validate Config Value
------------------------------------
Input: config_key, new_value

1. LOAD config by key
   IF NOT found -> REJECT "ERR_CFG_001"
2. PARSE new_value according to config.data_type
   IF parse fails -> REJECT "ERR_CFG_002"
3. IF config.min_value is set:
   VALIDATE parsed_value >= min_value
   IF fails -> REJECT "ERR_CFG_003"
4. IF config.max_value is set:
   VALIDATE parsed_value <= max_value
   IF fails -> REJECT "ERR_CFG_003"
5. IF config.allowed_values is set:
   VALIDATE new_value IN allowed_values list
   IF fails -> REJECT "ERR_CFG_004"
6. ALL checks pass -> value is valid
```

---

### 11.3 Config Update Algorithm

> Covers: BR-CFG-003

```
BR-CFG-003A: Update Config Value
----------------------------------
Input: config_key, new_value, reason, admin_user_id

1. VALIDATE admin_user_id has role SUPER_ADMIN
   IF NOT -> REJECT "ERR_CFG_005"
2. LOAD config by key
   IF NOT found -> REJECT "ERR_CFG_001"
3. IF config.is_critical = true -> REJECT "ERR_CFG_006"
   (Must use approval workflow for critical configs)
4. VALIDATE new_value (BR-CFG-004A)
5. INSERT system_config_history {
     config_id, old_value: config.value, new_value,
     changed_by: admin_user_id, reason, changed_at: NOW()
   }
6. UPDATE system_config SET
     value = new_value, updated_by = admin_user_id, updated_at = NOW()
7. INVALIDATE config cache (BR-CFG-007)
8. AUDIT_LOG("config.updated", {
     key: config_key, old_value, new_value, reason, admin_user_id
   })
9. IF config.requires_restart = true:
   NOTIFY admin: "Config {key} updated. Service restart required for changes to take effect."
10. RETURN updated config record
```

---

### 11.4 Config Versioning & Rollback

> Covers: BR-CFG-005, BR-CFG-006

**BR-CFG-005: Config change history** — Every config change is recorded in `system_config_history`:

#### `system_config_history` table (control DB)

| Field | Type | Constraints |
|-------|------|-------------|
| `id` | uuid | PK. |
| `config_id` | uuid | NOT NULL. FK -> `system_config.id`. |
| `old_value` | text | NOT NULL. Value before the change. Masked if config.is_sensitive. |
| `new_value` | text | NOT NULL. Value after the change. Masked if config.is_sensitive. |
| `changed_by` | uuid | NOT NULL. FK -> `users.id`. Must be SUPER_ADMIN. |
| `reason` | string(500) | NOT NULL. Required justification. |
| `changed_at` | timestamp | NOT NULL. Auto-set. |
| `rolled_back` | boolean | NOT NULL. Default `false`. Set to true if this change was rolled back. |

**BR-CFG-006: Config rollback** — Super admin can rollback any config to its previous value:

```
BR-CFG-006A: Rollback Config Change
-------------------------------------
Input: config_history_id, admin_user_id, reason

1. VALIDATE admin_user_id has role SUPER_ADMIN
   IF NOT -> REJECT "ERR_CFG_005"
2. LOAD history record
   IF NOT found -> REJECT "ERR_CFG_011"
3. IF history.rolled_back = true -> REJECT "ERR_CFG_012"
4. LOAD current config value
5. INSERT new history record {
     config_id, old_value: current_value, new_value: history.old_value,
     changed_by: admin_user_id, reason: "Rollback: " + reason
   }
6. UPDATE system_config SET value = history.old_value,
     updated_by = admin_user_id, updated_at = NOW()
7. SET history.rolled_back = true
8. INVALIDATE config cache (BR-CFG-007)
9. AUDIT_LOG("config.rolled_back", {
     key: config.key, reverted_from: current_value, reverted_to: history.old_value,
     original_change_id: config_history_id, reason, admin_user_id
   })
10. RETURN updated config record
```

---

### 11.5 Config Caching

> Covers: BR-CFG-007

**BR-CFG-007: Config cache strategy:**

| Aspect | Value |
|--------|-------|
| Store | Redis |
| Key format (single) | `config:{key}` |
| Key format (bulk) | `config:all` |
| TTL | 5 minutes (fallback expiry) |
| Primary invalidation | Explicit on config update |

#### Cache Invalidation Triggers

| Trigger | Action |
|---------|--------|
| Config value updated | Delete `config:{key}` and `config:all` |
| Config value rolled back | Delete `config:{key}` and `config:all` |
| Manual cache invalidation (admin) | Delete all `config:*` keys |

#### Cache Miss Behavior

```
1. On cache miss -> read from database
2. Store result in Redis with 5-minute TTL
3. Return result
```

> **Redis unavailable:** Read directly from DB. Log warning. Do not fail. Performance degradation is acceptable; outage is not.

---

### 11.6 Approval Workflow for Critical Configs

> Covers: BR-CFG-008

**BR-CFG-008: Critical config change approval** — Configs marked `is_critical = true` require a two-step approval process. One super admin requests, a different super admin approves.

#### `config_change_requests` table (control DB)

| Field | Type | Constraints |
|-------|------|-------------|
| `id` | uuid | PK. |
| `config_id` | uuid | NOT NULL. FK -> `system_config.id`. |
| `old_value` | text | NOT NULL. Current value at time of request. |
| `new_value` | text | NOT NULL. Proposed new value. |
| `requested_by` | uuid | NOT NULL. FK -> `users.id`. SUPER_ADMIN. |
| `reason` | string(500) | NOT NULL. Justification for the change. |
| `status` | string | NOT NULL. CHECK (`PENDING`, `APPROVED`, `REJECTED`, `EXPIRED`). |
| `approved_by` | uuid | NULL. FK -> `users.id`. SUPER_ADMIN. Must differ from requested_by. |
| `approved_at` | timestamp | NULL. Set when approved. |
| `rejected_reason` | string(500) | NULL. Reason for rejection. |
| `requested_at` | timestamp | NOT NULL. Auto-set. |
| `expires_at` | timestamp | NOT NULL. Default: requested_at + 24 hours. |

#### Request Algorithm

```
BR-CFG-008A: Request Critical Config Change
----------------------------------------------
Input: config_key, new_value, reason, requester_admin_id

1. VALIDATE requester_admin_id has role SUPER_ADMIN
   IF NOT -> REJECT "ERR_CFG_005"
2. LOAD config by key
   IF NOT found -> REJECT "ERR_CFG_001"
3. IF config.is_critical != true -> use standard update flow (BR-CFG-003A)
4. VALIDATE new_value (BR-CFG-004A)
5. CHECK for existing PENDING request for this config
   IF exists -> REJECT "ERR_CFG_009" (process existing request first)
6. INSERT config_change_requests {
     config_id, old_value: config.value, new_value,
     requested_by: requester_admin_id, reason,
     status: 'PENDING', requested_at: NOW(),
     expires_at: NOW() + 24 hours
   }
7. NOTIFY all other super admins: "Critical config change requested: {config_key}"
8. AUDIT_LOG("config.critical_change_requested", {
     config_key, new_value, requester_admin_id
   })
9. RETURN request record (config NOT yet changed)
```

#### Approval Algorithm

```
BR-CFG-008B: Approve Critical Config Change
----------------------------------------------
Input: request_id, approver_admin_id

1. VALIDATE approver_admin_id has role SUPER_ADMIN
   IF NOT -> REJECT "ERR_CFG_005"
2. LOAD request
   IF NOT found -> REJECT "ERR_CFG_008"
3. IF request.status != 'PENDING' -> REJECT "ERR_CFG_009"
4. IF NOW() > request.expires_at -> SET status = 'EXPIRED', REJECT "ERR_CFG_010"
5. IF approver_admin_id = request.requested_by -> REJECT "ERR_CFG_007"
6. UPDATE request SET status = 'APPROVED', approved_by, approved_at = NOW()
7. APPLY the config change using standard update flow (step 5-9 of BR-CFG-003A)
8. AUDIT_LOG("config.critical_change_approved", {
     config_key, new_value, requested_by, approved_by
   })
9. RETURN updated config record
```

#### Rejection Algorithm

```
BR-CFG-008C: Reject Critical Config Change
----------------------------------------------
Input: request_id, rejector_admin_id, rejected_reason

1. VALIDATE rejector_admin_id has role SUPER_ADMIN
2. LOAD request
   IF NOT found -> REJECT "ERR_CFG_008"
3. IF request.status != 'PENDING' -> REJECT "ERR_CFG_009"
4. UPDATE request SET status = 'REJECTED', rejected_reason
5. NOTIFY requester: "Your config change request for {key} was rejected: {reason}"
6. AUDIT_LOG("config.critical_change_rejected", {
     config_key, rejector_admin_id, rejected_reason
   })
```

#### Expiration

```
BR-CFG-008D: Expire Stale Requests
------------------------------------
Trigger: Scheduled job runs every hour

1. FIND all config_change_requests WHERE status = 'PENDING' AND NOW() > expires_at
2. FOR EACH expired request:
   SET status = 'EXPIRED'
   NOTIFY requester: "Your config change request for {key} has expired after 24 hours."
```

---

### 11.7 Master Configuration Reference

> Covers: BR-CFG-009

**BR-CFG-009: Config seeding** — On fresh deployment, all configs below are seeded with their default values. On application startup, missing configs are auto-created with defaults and logged as warnings.

#### Authentication (category: `auth`)

| Key | Type | Default | Range | Critical | Description |
|-----|------|---------|-------|:--------:|-------------|
| `auth.jwt.access_token_ttl_minutes` | INTEGER | `15` | 5-60 | No | JWT access token lifetime |
| `auth.jwt.refresh_token_ttl_days` | INTEGER | `7` | 1-30 | No | Refresh token lifetime |
| `auth.password.min_length` | INTEGER | `12` | 8-32 | No | Minimum password length |
| `auth.password.require_uppercase` | BOOLEAN | `true` | - | No | Require uppercase in password |
| `auth.password.require_number` | BOOLEAN | `true` | - | No | Require number in password |
| `auth.password.require_special` | BOOLEAN | `true` | - | No | Require special char in password |
| `auth.lockout.max_failed_attempts` | INTEGER | `5` | 3-10 | No | Failed login attempts before lockout |
| `auth.lockout.duration_minutes` | INTEGER | `30` | 5-1440 | No | Account lockout duration |
| `auth.2fa.code_validity_seconds` | INTEGER | `30` | 30-90 | No | TOTP code validity window |
| `auth.2fa.recovery_codes_count` | INTEGER | `10` | 5-20 | No | Recovery codes generated per user |
| `auth.session.max_concurrent` | INTEGER | `5` | 1-20 | No | Max concurrent sessions per user |
| `auth.password_reset.token_ttl_minutes` | INTEGER | `60` | 15-1440 | No | Password reset token lifetime |

#### Tenant Management (category: `tenant`)

| Key | Type | Default | Range | Critical | Description |
|-----|------|---------|-------|:--------:|-------------|
| `tenant.slug.min_length` | INTEGER | `3` | 2-10 | No | Minimum tenant slug length |
| `tenant.slug.max_length` | INTEGER | `50` | 20-100 | No | Maximum tenant slug length |
| `tenant.provisioning.timeout_seconds` | INTEGER | `120` | 30-600 | No | Timeout for tenant DB provisioning |
| `tenant.domain.verification_timeout_hours` | INTEGER | `72` | 24-168 | No | Hours allowed for domain verification |
| `tenant.suspension.grace_period_days` | INTEGER | `30` | 7-90 | Yes | Days before suspended tenant data is purged |
| `tenant.deactivation.data_retention_days` | INTEGER | `90` | 30-365 | Yes | Days to retain data after deactivation |

#### Billing & Credits (category: `billing`)

| Key | Type | Default | Range | Critical | Description |
|-----|------|---------|-------|:--------:|-------------|
| `billing.credits.starter_monthly` | INTEGER | `100` | 10-10000 | Yes | Monthly credit allotment for Starter tier |
| `billing.credits.pro_monthly` | INTEGER | `500` | 10-50000 | Yes | Monthly credit allotment for Pro tier |
| `billing.credits.max_pack_purchases_per_month` | INTEGER | `10` | 1-100 | No | Max credit pack purchases per billing period |
| `billing.stripe.webhook_tolerance_seconds` | INTEGER | `300` | 60-600 | No | Stripe webhook signature time tolerance |
| `billing.subscription.dunning_retry_days` | JSON | `[1,3,5,7]` | - | No | Payment retry schedule (days after failure) |
| `billing.subscription.grace_period_days` | INTEGER | `7` | 3-30 | Yes | Days after payment failure before suspension |

#### Scanning & Workflows (category: `scanning`)

| Key | Type | Default | Range | Critical | Description |
|-----|------|---------|-------|:--------:|-------------|
| `scanning.job.timeout_hours` | INTEGER | `4` | 1-24 | No | Maximum scan job duration before cancellation |
| `scanning.job.max_concurrent_per_tenant` | INTEGER | `3` | 1-20 | No | Max parallel scans per tenant |
| `scanning.queue.max_depth` | INTEGER | `1000` | 100-10000 | Yes | Max items in scan queue before rejecting new scans |
| `scanning.step.default_timeout_minutes` | INTEGER | `30` | 5-120 | No | Default timeout per individual scan step |
| `scanning.results.max_size_mb` | INTEGER | `100` | 10-1000 | No | Max result data size per scan job |
| `scanning.schedule.max_per_tenant_starter` | INTEGER | `0` | 0-0 | No | Max scan schedules for Starter (0 = disabled) |
| `scanning.schedule.max_per_tenant_pro` | INTEGER | `10` | 1-50 | No | Max scan schedules for Pro tier |
| `scanning.schedule.max_per_tenant_enterprise` | INTEGER | `50` | 1-500 | No | Max scan schedules for Enterprise tier |
| `scanning.worker.heartbeat_interval_seconds` | INTEGER | `30` | 10-120 | No | How often workers report health |
| `scanning.worker.stale_threshold_minutes` | INTEGER | `5` | 2-30 | No | Worker considered stale if no heartbeat |

#### Feature Flags (category: `feature_flags`)

| Key | Type | Default | Range | Critical | Description |
|-----|------|---------|-------|:--------:|-------------|
| `feature_flags.cache.ttl_minutes` | INTEGER | `30` | 5-1440 | No | Redis cache TTL for feature flag evaluation |
| `feature_flags.override.min_reason_length` | INTEGER | `10` | 5-100 | No | Minimum characters for override reason |

#### Compliance (category: `compliance`)

| Key | Type | Default | Range | Critical | Description |
|-----|------|---------|-------|:--------:|-------------|
| `compliance.framework.default_grace_period_days` | INTEGER | `90` | 30-365 | No | Default grace period for framework deprecation |
| `compliance.framework.min_grace_period_days` | INTEGER | `30` | 14-90 | No | Minimum allowed grace period |
| `compliance.report.retention_days_starter` | INTEGER | `30` | 7-90 | No | Report retention for Starter tier |
| `compliance.report.retention_days_pro` | INTEGER | `90` | 30-365 | No | Report retention for Pro tier |
| `compliance.report.retention_days_enterprise` | INTEGER | `365` | 90-730 | No | Report retention for Enterprise tier |

#### CVE Monitoring (category: `cve`)

| Key | Type | Default | Range | Critical | Description |
|-----|------|---------|-------|:--------:|-------------|
| `cve.feed.sync_interval_hours` | INTEGER | `6` | 1-24 | No | How often CVE feeds are checked |
| `cve.alert.severity_threshold` | STRING | `HIGH` | HIGH,CRITICAL | No | Minimum severity for auto-alerting tenants |
| `cve.feed.max_age_days` | INTEGER | `365` | 30-730 | No | Max CVE age to import |

#### Integrations (category: `integrations`)

| Key | Type | Default | Range | Critical | Description |
|-----|------|---------|-------|:--------:|-------------|
| `integrations.api_key.pool_min_keys` | INTEGER | `2` | 1-10 | No | Minimum API keys per provider before alert |
| `integrations.webhook.timeout_seconds` | INTEGER | `10` | 5-60 | No | Timeout for outgoing webhook calls |
| `integrations.webhook.max_retries` | INTEGER | `3` | 0-10 | No | Max retry attempts for failed webhooks |
| `integrations.webhook.retry_backoff_seconds` | JSON | `[10,60,300]` | - | No | Backoff intervals between retries |
| `integrations.notification.batch_window_seconds` | INTEGER | `60` | 10-300 | No | Batch window for grouping notifications |

#### Rate Limiting (category: `rate_limit`)

| Key | Type | Default | Range | Critical | Description |
|-----|------|---------|-------|:--------:|-------------|
| `rate_limit.starter.requests_per_minute` | INTEGER | `60` | 10-500 | No | API rate limit for Starter tier |
| `rate_limit.pro.requests_per_minute` | INTEGER | `200` | 50-1000 | No | API rate limit for Pro tier |
| `rate_limit.enterprise.requests_per_minute` | INTEGER | `500` | 100-5000 | No | API rate limit for Enterprise tier |
| `rate_limit.starter.scans_per_hour` | INTEGER | `5` | 1-20 | No | Scan creation limit for Starter tier |
| `rate_limit.pro.scans_per_hour` | INTEGER | `20` | 5-100 | No | Scan creation limit for Pro tier |
| `rate_limit.enterprise.scans_per_hour` | INTEGER | `50` | 10-500 | No | Scan creation limit for Enterprise tier |
| `rate_limit.super_admin.requests_per_minute` | INTEGER | `1000` | 100-5000 | No | Global super admin rate limit |
| `rate_limit.unauthenticated.requests_per_minute` | INTEGER | `30` | 10-100 | No | IP-based limit for unauthenticated requests |

#### API Versioning (category: `versioning`)

| Key | Type | Default | Range | Critical | Description |
|-----|------|---------|-------|:--------:|-------------|
| `versioning.api.min_deprecation_months` | INTEGER | `6` | 3-24 | Yes | Minimum months before deprecated API can be sunset |
| `versioning.migration.breaking_hold_hours` | INTEGER | `24` | 12-168 | Yes | Hold period after staging before production rollout |

#### Platform Operations (category: `admin`)

| Key | Type | Default | Range | Critical | Description |
|-----|------|---------|-------|:--------:|-------------|
| `admin.impersonation.session_ttl_minutes` | INTEGER | `60` | 15-480 | No | Max impersonation session duration |
| `admin.audit_log.retention_days` | INTEGER | `365` | 180-730 | Yes | Minimum audit log retention |
| `admin.error_alert.500_threshold_per_minute` | INTEGER | `5` | 1-50 | No | 500 error rate triggering critical alert |
| `admin.error_alert.429_threshold_per_minute` | INTEGER | `100` | 10-500 | No | Per-tenant 429 rate triggering abuse warning |
| `admin.error_alert.401_threshold_per_minute` | INTEGER | `50` | 10-200 | No | Per-IP 401 rate triggering brute force warning |
| `admin.config.change_request_ttl_hours` | INTEGER | `24` | 6-168 | No | Hours before unprocessed critical config requests expire |

---

### 11.8 Permissions Matrix

| Action | `TENANT_OWNER` | `SUPER_ADMIN` |
|--------|:--------------:|:-------------:|
| View system config values | No | Yes |
| View sensitive config values (unmasked) | No | Yes |
| Update non-critical config | No | Yes |
| Request critical config change | No | Yes |
| Approve critical config change | No | Yes (different admin than requester) |
| Reject critical config change | No | Yes |
| Rollback config change | No | Yes |
| View config change history | No | Yes |
| View pending change requests | No | Yes |
| View config cache status | No | Yes |
| Invalidate config cache manually | No | Yes |
| Manage bootstrap env vars | No | Infra team (outside app) |

> **Tenant owners have zero access** to system configuration. All config management is restricted to SUPER_ADMIN. This is consistent with the admin-only pattern across the platform.

---

### 11.9 Edge Cases

| Scenario | Behavior |
|----------|----------|
| Config key not found in DB | Use hardcoded application default. Log warning. Auto-seed the missing config on next startup. |
| Config value fails validation on update | Reject update. Current value remains unchanged. Return validation error with specific constraint that failed. |
| Critical config change request with no other super admin to approve | Cannot be approved (self-approval not allowed). Alert system that platform needs at least 2 super admins for critical config changes. |
| Config cache and DB disagree | Cache TTL expires, next read refreshes from DB. Manual cache invalidation available for immediate sync. |
| Redis down during config read | Read directly from DB. Log warning. Performance degradation but no outage. |
| Rollback of a config that was changed multiple times | Rollback reverts to the value BEFORE the specific change being rolled back. Creates a new history entry. Does not cascade — only the selected change is reverted. |
| Config update during active scans | New value takes effect immediately for new operations. In-flight scans continue with the value they started with (scan job captures config snapshot at creation). |
| Two super admins update same non-critical config simultaneously | Last write wins. Both changes recorded in history. No conflict resolution needed — audit trail shows the sequence. |
| Critical config change request expires after 24 hours | Status set to `EXPIRED`. Requester notified. Must create a new request to proceed. |
| Config value set to its current value | Change is accepted and recorded in history (for audit trail). No functional impact. |
| Application startup with empty system_config table | All configs auto-seeded with defaults from the master reference (11.7). Logged as info: "Config seeded: {count} values." |
| Config with `requires_restart = true` is updated | Value saved immediately. Admin notified: "Service restart required." Config reads return old cached value until restart loads new value. |
| Sensitive config (`is_sensitive = true`) in audit logs | Value is masked as `***SENSITIVE***` in history records and audit logs. Only the admin UI shows the actual value. |

---

### 11.10 Error Codes

| Code | HTTP | Message | Cause |
|------|------|---------|-------|
| `ERR_CFG_001` | 404 | Configuration key not found. | Attempted to read or update a config key that doesn't exist. |
| `ERR_CFG_002` | 400 | Invalid configuration value. Expected {data_type}. | Value doesn't match the config's declared data_type. |
| `ERR_CFG_003` | 400 | Value out of range. Must be between {min} and {max}. | Value outside allowed min/max bounds. |
| `ERR_CFG_004` | 400 | Invalid value. Must be one of: {allowed_values}. | Value not in the allowed values list. |
| `ERR_CFG_005` | 403 | Insufficient permissions to modify system configuration. | Caller is not SUPER_ADMIN. |
| `ERR_CFG_006` | 403 | Critical configuration change requires approval workflow. | Attempted to directly update a config marked `is_critical = true`. |
| `ERR_CFG_007` | 403 | Cannot approve own configuration change request. | Approver is the same admin who requested the change. |
| `ERR_CFG_008` | 404 | Configuration change request not found. | Referenced change request ID doesn't exist. |
| `ERR_CFG_009` | 409 | Configuration change request already processed. | Attempted to approve/reject a request that is not in PENDING status. |
| `ERR_CFG_010` | 400 | Configuration change request has expired. | Request is older than the configured TTL (default 24 hours). |
| `ERR_CFG_011` | 404 | Configuration history record not found. | Referenced history ID doesn't exist for rollback. |
| `ERR_CFG_012` | 409 | Configuration change already rolled back. | Attempted to rollback a change that was already rolled back. |

> **Cross-references:** Config values referenced in this section are enforced in their respective domain sections. For example, `auth.lockout.max_failed_attempts` is enforced by BR-AUTH-007 (section 1), `scanning.job.timeout_hours` by BR-SCAN-014 (section 4), and rate limits by BR-ERR-008 (section 12).
