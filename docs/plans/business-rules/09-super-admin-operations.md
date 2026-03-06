# 9. Super Admin & Operations

> Covers: BR-ADM-001 through BR-ADM-012 (original 10 rules expanded + 2 new monitoring rules).
> Impersonation mechanics reference Section 2. Feature override mechanics reference Section 5.
> Framework versioning mechanics reference Section 13. Error response schema in Section 12.

---

### 9.1 Super Admin Identity

> Covers: BR-ADM-001, BR-ADM-002

**BR-ADM-001: Super admin creation** — Super admins are seeded directly in the database. No self-registration, invitation flow, or admin panel creation.

| Aspect | Rule |
|--------|------|
| Creation method | Direct DB INSERT into `super_admins` and `users` tables by infra team |
| Role | `users.role = 'SUPER_ADMIN'` |
| 2FA | Mandatory. Must enroll on first login. No access without 2FA. |
| Minimum count | Platform should have at least 2 super admins (required for critical config approval, section 11.6) |
| Deletion | Soft-delete only. Cannot self-delete. Another super admin or infra team removes. Cannot delete the last super admin. |

**BR-ADM-002: Super admin audit trail** — All super admin actions are logged with `is_super_admin = true` in the audit log (section 10.4). Super admin sessions include an `admin_session_id` for grouping related actions within a session.

---

### 9.2 Tenant Impersonation

> Covers: BR-ADM-003
> Tenant status effects during impersonation reference Section 2.

**BR-ADM-003: Impersonation rules:**

```
BR-ADM-003A: Start Impersonation Session
-------------------------------------------
Input: admin_user_id, target_tenant_id, reason

1. VALIDATE admin_user_id has role SUPER_ADMIN
2. LOAD target tenant
   IF status = 'DEACTIVATED' -> REJECT "ERR_ADM_002"
3. CHECK for existing active impersonation session for this admin
   IF exists -> REJECT "ERR_ADM_004"
4. IF reason is blank or < 10 characters -> REJECT "ERR_ADM_003"
5. CREATE impersonation_session {
     admin_user_id, target_tenant_id, reason,
     started_at: NOW(),
     expires_at: NOW() + config(admin.impersonation.session_ttl_minutes)
   }
6. ISSUE temporary JWT with:
   - user_id = admin_user_id
   - tenant_id = target_tenant_id
   - role = TENANT_OWNER (scoped to target tenant)
   - impersonated_by = admin_user_id
   - exp = session expires_at
7. AUDIT_LOG("admin.impersonation_started", {
     admin_user_id, target_tenant_id, reason,
     session_ttl: config(admin.impersonation.session_ttl_minutes)
   })
8. RETURN session token
```

```
BR-ADM-003B: End Impersonation Session
-----------------------------------------
Input: admin_user_id

1. LOAD active impersonation session for admin
   IF NOT found -> REJECT "No active impersonation session"
2. SET session.ended_at = NOW()
3. INVALIDATE impersonation JWT
4. AUDIT_LOG("admin.impersonation_ended", {
     admin_user_id, target_tenant_id,
     duration_minutes: (ended_at - started_at)
   })
5. RETURN admin to their super admin context
```

#### Impersonation Constraints

| Constraint | Rule |
|-----------|------|
| Session TTL | Config: `admin.impersonation.session_ttl_minutes` (default 60 min) |
| Concurrent sessions | One impersonation session per admin at a time |
| Extension | Cannot extend. Must end and start new session (new audit entry). |
| Auto-expiry | Session auto-expires at `expires_at`. Actions after expiry rejected with 401. |

#### Restricted Actions During Impersonation

| Action | Allowed | Reason |
|--------|:-------:|--------|
| View tenant data (scans, domains, results) | Yes | Primary purpose of impersonation |
| Create/cancel scans | Yes | Debugging and support |
| View billing info | Yes | Support use case |
| Change tenant owner password | No | Security — admin should not have credential access |
| Change/disable 2FA settings | No | Security — prevents admin from locking out tenant |
| Delete the tenant | No | Destructive — must be done from admin panel |
| Change billing/payment methods | No | Financial — must be done by tenant owner |
| Create/modify integrations | Yes | Support and debugging |

---

### 9.3 Credit Adjustments

> Covers: BR-ADM-004

**BR-ADM-004: Credit adjustment rules:**

```
BR-ADM-004A: Admin Credit Adjustment
---------------------------------------
Input: tenant_id, amount, type, reason, admin_user_id

1. VALIDATE admin_user_id has role SUPER_ADMIN
   IF NOT -> REJECT "ERR_ADM_001"
2. VALIDATE amount > 0
3. VALIDATE type is 'refund' (add credits) or 'consumption' (deduct credits)
4. VALIDATE reason is not blank (min 10 characters)
5. LOAD tenant subscription
   IF type = 'consumption':
     VALIDATE credits_remaining >= amount
     IF NOT -> REJECT "ERR_ADM_005"
6. INSERT credit_transactions {
     tenant_id,
     amount: (type = 'refund' ? +amount : -amount),
     type,
     description: "Admin adjustment: " + reason,
     created_at: NOW()
   }
7. UPDATE tenant_subscriptions SET
     credits_remaining = credits_remaining + (type = 'refund' ? +amount : -amount)
8. AUDIT_LOG("admin.credit_adjustment", {
     tenant_id, amount, type, reason, admin_user_id,
     credits_before, credits_after
   })
9. RETURN updated credit balance
```

---

### 9.4 Feature Overrides

> Covers: BR-ADM-005
> Override CRUD mechanics are defined in Section 5 (BR-FLAG-003A/B/C). This subsection captures the admin-specific rules.

**BR-ADM-005: Feature override rules:**

| Rule | Description |
|------|-------------|
| Who | Only SUPER_ADMIN can create, update, or delete overrides |
| Scope | Only SUBSCRIPTION flags can be overridden. Operational flags cannot. |
| Reason | Required (min 10 characters) for audit trail |
| Persistence | Overrides survive plan changes — never auto-removed |
| Grant or revoke | Override can enable a feature (grant) or disable it (revoke) regardless of plan |
| Visibility | Tenant sees `reason: "override"` in feature API but NOT who created it or why |

> Full override algorithms in Section 5.5.

---

### 9.5 Tenant Suspension

> Covers: BR-ADM-006

**BR-ADM-006: Tenant suspension criteria** — Super admin may suspend a tenant for documented reasons:

| Reason Code | Description |
|-------------|-------------|
| `FRAUD` | Fraudulent payment activity detected |
| `ABUSE` | Excessive scanning, API abuse, or platform misuse |
| `TOS_VIOLATION` | Violation of terms of service |
| `SECURITY` | Tenant account compromised or used for malicious activity |
| `MANUAL_REVIEW` | Administrative hold pending investigation |

```
BR-ADM-006A: Admin Tenant Suspension
---------------------------------------
Input: tenant_id, reason_code, reason_detail, admin_user_id

1. VALIDATE admin_user_id has role SUPER_ADMIN
   IF NOT -> REJECT "ERR_ADM_001"
2. LOAD tenant
   IF status = 'SUSPENDED' -> REJECT "ERR_ADM_006"
   IF status = 'DEACTIVATED' -> REJECT "ERR_ADM_007"
3. VALIDATE reason_code is valid (see table above)
4. VALIDATE reason_detail is not blank (min 10 characters)
5. UPDATE tenant SET status = 'SUSPENDED', suspended_reason = reason_code
6. CANCEL all active scan jobs for tenant (refund credits for unexecuted steps)
7. DISABLE all scan schedules for tenant (set status = 'DISABLED')
8. INVALIDATE all active JWT sessions for tenant users
9. INVALIDATE feature flag cache for tenant
10. AUDIT_LOG("admin.tenant_suspended", {
      tenant_id, reason_code, reason_detail, admin_user_id
    })
11. NOTIFY tenant owner email:
    "Your Reconova account has been suspended. Reason: {reason_code}. Contact support for details."
12. RETURN updated tenant record
```

```
BR-ADM-006B: Admin Tenant Reactivation
-----------------------------------------
Input: tenant_id, reason, admin_user_id

1. VALIDATE admin_user_id has role SUPER_ADMIN
2. LOAD tenant. VALIDATE status = 'SUSPENDED'
3. UPDATE tenant SET status = 'ACTIVE', suspended_reason = NULL
4. INVALIDATE feature flag cache for tenant
5. AUDIT_LOG("admin.tenant_reactivated", {
     tenant_id, reason, admin_user_id
   })
6. NOTIFY tenant owner email:
   "Your Reconova account has been reactivated."
7. Note: Scan schedules remain DISABLED. Tenant must manually re-enable (section 5.8).
```

---

### 9.6 Platform API Key Management

> Covers: BR-ADM-007

**BR-ADM-007: Platform API key management** — Super admin manages shared API key pools for external providers (Shodan, SecurityTrails, Censys, VirusTotal).

#### `platform_api_keys` Field Constraints

| Field | Type | Constraints |
|-------|------|-------------|
| `id` | uuid | PK. Immutable. |
| `provider` | string(50) | NOT NULL. Provider name: `shodan`, `securitytrails`, `censys`, `virustotal`. |
| `api_key_encrypted` | text | NOT NULL. AES-256 encrypted (section 10.3, BR-DATA-008). |
| `rate_limit` | integer | NOT NULL. Max requests per minute for this key. |
| `usage_count` | integer | NOT NULL. Default 0. Lifetime usage counter. |
| `monthly_quota` | integer | NULL. Monthly request limit. NULL = unlimited. |
| `monthly_usage` | integer | NOT NULL. Default 0. Reset on 1st of each month. |
| `status` | string | NOT NULL. CHECK (`ACTIVE`, `INACTIVE`, `RATE_LIMITED`). |
| `added_by` | uuid | NOT NULL. FK -> `users.id`. SUPER_ADMIN. |
| `last_used_at` | timestamp | NULL. Last time this key was used in a scan. |
| `created_at` | timestamp | NOT NULL. Auto-set. |

#### Key Management Operations

```
BR-ADM-007A: Add API Key
---------------------------
Input: provider, api_key, rate_limit, monthly_quota, admin_user_id

1. VALIDATE admin_user_id has role SUPER_ADMIN
2. ENCRYPT api_key with AES-256 using ENCRYPTION_MASTER_KEY
3. INSERT platform_api_keys {
     provider, api_key_encrypted, rate_limit, monthly_quota,
     status: 'ACTIVE', added_by: admin_user_id
   }
4. AUDIT_LOG("admin.api_key_added", {
     provider, key_id: new_id, admin_user_id
   })
   (NEVER log the actual API key)
5. RETURN key record (masked: ****{last4})
```

```
BR-ADM-007B: Rotate API Key
------------------------------
Input: key_id, new_api_key, admin_user_id

1. VALIDATE admin_user_id has role SUPER_ADMIN
2. LOAD existing key
3. SET old key status = 'INACTIVE'
4. INSERT new platform_api_keys {
     provider: old.provider, api_key_encrypted: encrypt(new_api_key),
     rate_limit: old.rate_limit, monthly_quota: old.monthly_quota,
     status: 'ACTIVE', added_by: admin_user_id
   }
5. AUDIT_LOG("admin.api_key_rotated", {
     provider, old_key_id: key_id, new_key_id, admin_user_id
   })
6. RETURN new key record (masked)
```

```
BR-ADM-007C: Deactivate API Key
----------------------------------
Input: key_id, admin_user_id

1. VALIDATE admin_user_id has role SUPER_ADMIN
2. LOAD key. VALIDATE status is 'ACTIVE' or 'RATE_LIMITED'
3. UPDATE platform_api_keys SET status = 'INACTIVE'
4. NEVER delete the key row (audit trail preservation)
5. AUDIT_LOG("admin.api_key_deactivated", {
     key_id, provider, admin_user_id
   })
```

#### Key Selection for Scans

```
BR-ADM-007D: Select API Key for Scan
---------------------------------------
Input: provider

1. LOAD all platform_api_keys WHERE provider = provider AND status = 'ACTIVE'
2. FILTER keys where monthly_usage < monthly_quota (if quota set)
3. IF no keys available:
   REJECT scan step with "ERR_ADM_010"
   LOG("admin.no_api_keys", { provider })
   ALERT super admin: "All API keys exhausted for {provider}"
4. SELECT key with lowest usage_count (round-robin load balancing)
5. INCREMENT usage_count and monthly_usage atomically
6. UPDATE last_used_at = NOW()
7. RETURN decrypted API key (in-memory only, never persisted decrypted)
```

#### Monthly Usage Reset

```
BR-ADM-007E: Monthly Quota Reset
-----------------------------------
Trigger: 1st of each month, 00:00 UTC

1. UPDATE platform_api_keys SET monthly_usage = 0 WHERE monthly_quota IS NOT NULL
2. UPDATE platform_api_keys SET status = 'ACTIVE' WHERE status = 'RATE_LIMITED'
3. LOG("admin.api_key_quota_reset", { keys_reset: count })
```

---

### 9.7 Scan Step Pricing

> Covers: BR-ADM-009

**BR-ADM-009: Scan step pricing changes** — Super admin configures credit costs per scan step per subscription tier.

| Rule | Description |
|------|-------------|
| Effective for | New scans only. In-progress scans use pricing at job creation time. |
| Granularity | Per `check_type`, per `tier_id` |
| Audit | All pricing changes audit-logged with old and new values |
| Notification | No tenant notification (pricing is internal to credit system) |

```
BR-ADM-009A: Update Scan Step Pricing
----------------------------------------
Input: check_type, tier_id, new_credits_per_domain, admin_user_id

1. VALIDATE admin_user_id has role SUPER_ADMIN
   IF NOT -> REJECT "ERR_ADM_001"
2. LOAD existing pricing for (check_type, tier_id)
   IF NOT found -> create new row
3. STORE old_credits = existing.credits_per_domain (or 0 if new)
4. UPSERT scan_step_pricing {
     check_type, tier_id,
     credits_per_domain: new_credits_per_domain,
     updated_by: admin_user_id,
     updated_at: NOW()
   }
5. AUDIT_LOG("admin.pricing_updated", {
     check_type, tier_id,
     old_credits_per_domain: old_credits,
     new_credits_per_domain,
     admin_user_id
   })
6. RETURN updated pricing record
```

---

### 9.8 System Maintenance Mode

> Covers: BR-ADM-010
> Maintenance mode uses the `maintenance_mode` operational flag defined in Section 5.1.

**BR-ADM-010: Maintenance mode:**

```
BR-ADM-010A: Enable Maintenance Mode
---------------------------------------
Input: reason, estimated_duration_minutes, admin_user_id

1. VALIDATE admin_user_id has role SUPER_ADMIN
2. SET operational flag 'maintenance_mode' to DISABLED
   (DISABLED = maintenance IS active, feature availability is "disabled")
3. INVALIDATE all tenant feature flag caches (section 5.6)
4. Effects:
   a. All new scan creation requests -> REJECT with ERR_SYS_002
   b. Running scans -> complete normally (not interrupted)
   c. API read operations -> continue normally
   d. Frontend -> shows maintenance banner with estimated duration
   e. Scheduled scans -> will not trigger while maintenance active
5. AUDIT_LOG("admin.maintenance_enabled", {
     reason, estimated_duration_minutes, admin_user_id
   })
6. NOTIFY all active tenant sessions:
   "Platform maintenance in progress. Estimated duration: {minutes} minutes. Scans temporarily unavailable."
```

```
BR-ADM-010B: Disable Maintenance Mode
----------------------------------------
Input: admin_user_id

1. VALIDATE admin_user_id has role SUPER_ADMIN
2. SET operational flag 'maintenance_mode' to ENABLED
   (ENABLED = normal operations resumed)
3. INVALIDATE all tenant feature flag caches
4. AUDIT_LOG("admin.maintenance_disabled", {
     admin_user_id,
     duration_actual_minutes: (NOW() - maintenance_start)
   })
5. Scheduled scans resume on their next cron trigger
```

---

### 9.9 Admin Monitoring Dashboard

> Covers: BR-ADM-011, BR-ADM-012

**BR-ADM-011: Monitoring metrics** — The super admin dashboard displays real-time platform health. All metrics query read replicas or cached aggregates, never the primary production database.

#### Tenant Metrics

| Metric | Source | Refresh |
|--------|--------|---------|
| Total active tenants | `tenants WHERE status = 'ACTIVE'` | 5 min |
| Tenants by plan tier | Group by plan_id | 5 min |
| New tenants (last 7/30 days) | `tenants WHERE created_at > threshold` | 5 min |
| Suspended tenants | `tenants WHERE status = 'SUSPENDED'` | 5 min |
| Churn (cancelled subscriptions, 30 days) | Subscription cancellation events | Daily |

#### Scan Metrics

| Metric | Source | Refresh |
|--------|--------|---------|
| Active scans (currently running) | `scan_jobs WHERE status = 'RUNNING'` across all tenants | 30 sec |
| Queue depth | Redis queue length | 30 sec |
| Scans completed (last 24h) | Scan completion events | 5 min |
| Scan failure rate (last 24h) | Failed / total scans | 5 min |
| Average scan duration | Completed scan timestamps | 5 min |

#### Worker Metrics

| Metric | Source | Refresh |
|--------|--------|---------|
| Active workers | Worker heartbeat registry | 30 sec |
| Stale workers (no heartbeat) | Workers past `scanning.worker.stale_threshold_minutes` | 30 sec |
| Worker utilization | Jobs in progress / total workers | 30 sec |

#### Credit & Revenue Metrics

| Metric | Source | Refresh |
|--------|--------|---------|
| Total credits consumed (30 days) | `credit_transactions` aggregation | Hourly |
| Credits consumed by tier | Group by plan | Hourly |
| Revenue (MRR estimate) | Active subscriptions x plan price | Daily |
| Credit pack purchases (30 days) | `credit_transactions WHERE type = 'purchase'` | Hourly |

#### System Health

| Metric | Source | Refresh |
|--------|--------|---------|
| API error rate (5xx, last hour) | Error logs | 1 min |
| API latency (p50, p95, p99) | Request timing logs | 1 min |
| DB connection pool usage | PostgreSQL pg_stat_activity | 1 min |
| Redis memory usage | Redis INFO command | 1 min |
| API key pool health (per provider) | `platform_api_keys` active count | 5 min |

**BR-ADM-012: Monitoring alerts** — Automated alerts for super admins:

| Condition | Alert Level | Action |
|-----------|------------|--------|
| Queue depth > 80% of `scanning.queue.max_depth` | Warning | Notify: consider scaling workers |
| Zero active workers | Critical | Page on-call: no workers processing scans |
| API key pool < `integrations.api_key.pool_min_keys` for any provider | Warning | Notify: add more API keys for {provider} |
| Tenant DB backup failure | Warning | Flag tenant for manual backup |
| All API keys for a provider quota-exhausted | Critical | Provider integration unavailable until key refresh or month reset |
| Credit consumption anomaly (tenant uses > 5x daily average) | Warning | Possible scan abuse or misconfiguration |
| Worker stale (no heartbeat past threshold) | Warning | Notify: worker {id} may need restart |
| DB connection pool > 80% capacity | Warning | Notify: consider connection pool scaling |

---

### 9.10 Compliance Framework Management

> Covers: BR-ADM-008
> Framework versioning lifecycle is defined in Section 13 (BR-VER-014 through BR-VER-018).

**BR-ADM-008: Framework lifecycle management** — Super admin creates and manages compliance frameworks.

| State | Admin Action | Tenant Visibility |
|-------|-------------|------------------|
| `DRAFT` | Create/edit framework, add controls, configure check mappings | Not visible to tenants |
| `ACTIVE` | Publish framework (set status to ACTIVE) | Visible and selectable (if plan allows via `plan_compliance_access`) |
| `DEPRECATED` | Auto-triggered when new version published (section 13.4) | Visible with deprecation banner, new selections blocked |
| `SUNSET` | Auto-triggered after grace period (section 13.4) | Hidden from selection, historical assessments preserved |

```
BR-ADM-008A: Create Compliance Framework
-------------------------------------------
Input: name, version, region, description, controls[], admin_user_id

1. VALIDATE admin_user_id has role SUPER_ADMIN
2. INSERT compliance_frameworks {
     name, version, region, description,
     status: 'DRAFT', created_by: admin_user_id
   }
3. FOR EACH control in controls[]:
   INSERT compliance_controls {
     framework_id, control_id, title, description, category,
     min_security_recommendations_json
   }
4. AUDIT_LOG("admin.framework_created", {
     framework_id, name, version, controls_count, admin_user_id
   })
5. RETURN framework record
```

```
BR-ADM-008B: Publish Compliance Framework
--------------------------------------------
Input: framework_id, admin_user_id

1. VALIDATE admin_user_id has role SUPER_ADMIN
2. LOAD framework. VALIDATE status = 'DRAFT'
3. VALIDATE framework has at least 1 control
4. VALIDATE control_check_mappings exist for at least 1 control
5. IF framework.previous_version_id is set:
   TRIGGER framework deprecation flow (BR-VER-016A in section 13)
6. SET framework status = 'ACTIVE'
7. AUDIT_LOG("admin.framework_published", {
     framework_id, name, version, admin_user_id
   })
8. RETURN updated framework
```

---

### 9.11 Permissions Matrix

| Action | `TENANT_OWNER` | `SUPER_ADMIN` |
|--------|:--------------:|:-------------:|
| View admin dashboard | No | Yes |
| View monitoring metrics | No | Yes |
| View cross-tenant data | No | Yes |
| Start impersonation session | No | Yes |
| End impersonation session | N/A | Yes (own sessions) |
| Adjust tenant credits | No | Yes |
| Suspend tenant | No | Yes |
| Reactivate tenant | No | Yes |
| Manage platform API keys | No | Yes |
| View API key values (masked) | No | Yes |
| Update scan step pricing | No | Yes |
| Enable/disable maintenance mode | No | Yes |
| Create compliance frameworks | No | Yes |
| Publish compliance frameworks | No | Yes |
| View platform-wide audit logs | No | Yes |
| Configure monitoring alert thresholds | No | Yes |

---

### 9.12 Edge Cases

| Scenario | Behavior |
|----------|----------|
| Last super admin tries to delete themselves | Rejected with ERR_ADM_009. Platform must have at least 1 super admin. |
| Super admin impersonates suspended tenant | Allowed (for investigation). Write operations blocked by suspension rules (section 2). Read operations work normally. |
| Credit deduction exceeds available balance | Rejected with ERR_ADM_005. Admin can set balance to exactly 0 but not negative. |
| Pricing change during scan creation (race condition) | Scan uses pricing snapshot captured at the start of creation. Atomic read of pricing before credit deduction. |
| All API keys for a provider are inactive | Scan steps using that provider skip with error, credits refunded. Super admin alerted. |
| Maintenance mode enabled during impersonation | Impersonation session continues. Admin can browse tenant data. Scan creation blocked for all tenants. |
| Two super admins try to suspend same tenant simultaneously | First suspension succeeds. Second gets ERR_ADM_006. Both attempts audit-logged. |
| Super admin rotates API key while scans use old key | In-flight scans complete normally (key already decrypted in worker memory). New scans pick up the new key. |
| Monitoring dashboard accessed during high load | Queries use read replicas and cached aggregates. Never hits primary production DB. |
| Super admin account locked (failed logins) | Same lockout rules as regular users (section 1). Another super admin or infra team must unlock via DB. |
| Impersonation session expires mid-action | Action fails with 401 (token expired). Partial writes are rolled back by transaction. Admin must start new session. |
| Super admin credit refund to a tenant on Free tier | Allowed. Credits are added. Tenant can use them if they later subscribe to a paid plan. |
| Monthly API key quota reset while scans are running | Running scans are unaffected (key already in use). Reset enables RATE_LIMITED keys to become ACTIVE again. |

---

### 9.13 Error Codes

| Code | HTTP | Message | Cause |
|------|------|---------|-------|
| `ERR_ADM_001` | 403 | Super admin access required. | Caller does not have SUPER_ADMIN role. |
| `ERR_ADM_002` | 400 | Cannot impersonate deactivated tenant. | Target tenant has status DEACTIVATED. |
| `ERR_ADM_003` | 400 | Impersonation reason required (minimum 10 characters). | Reason is blank or too short. |
| `ERR_ADM_004` | 409 | Impersonation session already active. End current session first. | Admin already has an active impersonation session. |
| `ERR_ADM_005` | 400 | Cannot deduct more credits than available balance. | Credit deduction would make tenant balance negative. |
| `ERR_ADM_006` | 400 | Tenant already suspended. | Attempted to suspend an already-suspended tenant. |
| `ERR_ADM_007` | 400 | Cannot suspend deactivated tenant. | Attempted to suspend a tenant with status DEACTIVATED. |
| `ERR_ADM_008` | 404 | API key not found. | Referenced API key ID doesn't exist in `platform_api_keys`. |
| `ERR_ADM_009` | 400 | Cannot delete last super admin. Platform requires at least one. | Attempted to delete/deactivate the only remaining super admin. |
| `ERR_ADM_010` | 400 | No active API keys available for provider {name}. | All keys for the provider are inactive or quota-exhausted. |

> **Cross-references:** Impersonation constraints align with Section 2 tenant management. Feature override mechanics in Section 5.5. Compliance framework versioning in Section 13.4. Monitoring alert thresholds configurable via Section 11 system_config.
