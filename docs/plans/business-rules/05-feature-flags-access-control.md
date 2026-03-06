# 5. Feature Flags & Access Control

> Covers: BR-FLAG-001 through BR-FLAG-007 from the original business rules.
> Error response JSON schema is defined in `00-index.md` Section 12 reference.

---

### 5.1 Flag Types & Definitions

> Covers: BR-FLAG-001, BR-FLAG-002

Reconova uses two types of feature flags:

#### Flag Types

| Type | Purpose | Controlled By | Scope |
|------|---------|---------------|-------|
| `SUBSCRIPTION` | Gates features by plan tier. Auto-synced on plan change. | System (plan-driven) + super admin override | Per-tenant |
| `OPERATIONAL` | Platform-level kill switches for maintenance, outages, or feature retirement. | Super admin manual toggle | Global (all tenants) |

#### Master Flag Registry — Subscription Flags

| Flag Name | Module | Starter | Pro | Enterprise | Description |
|-----------|--------|:-------:|:---:|:----------:|-------------|
| `subdomain_enumeration` | scanning | Yes | Yes | Yes | Subdomain discovery scan step |
| `port_scanning` | scanning | Yes | Yes | Yes | Port scanning step |
| `technology_detection` | scanning | Yes | Yes | Yes | Technology stack detection step |
| `screenshot_capture` | scanning | Yes | Yes | Yes | Website screenshot capture step |
| `vulnerability_scanning` | scanning | No | Yes | Yes | Vulnerability assessment step |
| `compliance_checks` | scanning | No | Yes | Yes | Compliance framework checks step |
| `compliance_reports` | compliance | No | Yes | Yes | Generate compliance PDF reports |
| `shodan_integration` | integrations | No | Yes | Yes | Shodan data enrichment step |
| `securitytrails_integration` | integrations | No | Yes | Yes | SecurityTrails lookup step |
| `censys_integration` | integrations | No | No | Yes | Censys lookup step |
| `custom_api_connectors` | integrations | No | No | Yes | Custom third-party API connectors |
| `custom_workflows` | scanning | No | Yes | Yes | Create custom workflow templates |
| `scheduled_scans` | scanning | No | Yes | Yes | Schedule recurring scans |
| `notification_slack` | notifications | No | Yes | Yes | Slack notification channel |
| `notification_jira` | notifications | No | No | Yes | Jira ticket creation |
| `notification_webhook` | notifications | No | No | Yes | Custom webhook notifications |
| `notification_siem` | notifications | No | No | Yes | SIEM integration forwarding |
| `cve_monitoring` | monitoring | No | Yes | Yes | CVE feed monitoring and vulnerability alerts |

#### Master Flag Registry — Operational Flags

| Flag Name | Module | Default | Description |
|-----------|--------|---------|-------------|
| `maintenance_mode` | platform | `ENABLED` | When disabled, blocks all scan creation platform-wide |
| `vuln_scanning_global` | scanning | `ENABLED` | Emergency disable for vulnerability scanning |
| `compliance_global` | compliance | `ENABLED` | Emergency disable for compliance checks |
| `cve_monitoring_global` | monitoring | `ENABLED` | Enable/disable CVE monitoring platform-wide |
| `api_global` | platform | `ENABLED` | Global API availability (when disabled, returns 503) |

> Note: Operational flags default to `ENABLED`. Setting to `DISABLED` blocks the feature for ALL tenants regardless of plan or override.

---

### 5.2 Field Constraints

> Covers: BR-FLAG-003, BR-FLAG-004

#### `feature_flags` table

| Field | Type | Constraints |
|-------|------|-------------|
| `id` | uuid | PK. Immutable. |
| `name` | string(100) | NOT NULL. Unique. Lowercase snake_case (e.g., `vulnerability_scanning`). Immutable after creation. |
| `type` | string | NOT NULL. CHECK (`SUBSCRIPTION`, `OPERATIONAL`). Immutable after creation. |
| `module` | string(50) | NOT NULL. Categorization: `scanning`, `compliance`, `integrations`, `notifications`, `monitoring`, `platform`. |
| `default_enabled` | boolean | NOT NULL. Default value when no plan or override applies. |
| `description` | string(500) | NOT NULL. Human-readable description for admin UI. |
| `created_at` | timestamp | NOT NULL. Auto-set on insert. Immutable. |
| `updated_at` | timestamp | NOT NULL. Auto-set on insert and update. |

#### `plan_features` table

| Field | Type | Constraints |
|-------|------|-------------|
| `id` | uuid | PK. Immutable. |
| `plan_id` | uuid | NOT NULL. FK → `subscription_plans.id`. |
| `feature_id` | uuid | NOT NULL. FK → `feature_flags.id`. |
| `enabled` | boolean | NOT NULL. Whether this feature is available on this plan. |
| | | Composite unique: `(plan_id, feature_id)`. |

#### `tenant_feature_overrides` table

| Field | Type | Constraints |
|-------|------|-------------|
| `id` | uuid | PK. Immutable. |
| `tenant_id` | uuid | NOT NULL. FK → `tenants.id`. |
| `feature_id` | uuid | NOT NULL. FK → `feature_flags.id`. Must reference a flag with `type = SUBSCRIPTION`. Cannot override operational flags. |
| `enabled` | boolean | NOT NULL. Override value — `true` grants access, `false` revokes access regardless of plan. |
| `overridden_by` | uuid | NOT NULL. FK → `users.id`. Must be a user with role `SUPER_ADMIN`. |
| `reason` | string(500) | NOT NULL. Required justification for audit trail. |
| `created_at` | timestamp | NOT NULL. Auto-set on insert. Immutable. |
| `updated_at` | timestamp | NOT NULL. Auto-set on update. |
| | | Composite unique: `(tenant_id, feature_id)`. |

> **Constraint:** Operational flags cannot have tenant overrides. The `feature_id` on `tenant_feature_overrides` must reference a flag where `type = SUBSCRIPTION`. Enforced at application level.

---

### 5.3 Plan-to-Feature Mapping

> Covers: BR-FLAG-004

This subsection defines how plan tiers map to feature flags and how that mapping syncs on plan changes.

#### Tier Feature Matrix

| Feature Flag | Free | Starter | Pro | Enterprise |
|-------------|:----:|:-------:|:---:|:----------:|
| `subdomain_enumeration` | No | Yes | Yes | Yes |
| `port_scanning` | No | Yes | Yes | Yes |
| `technology_detection` | No | Yes | Yes | Yes |
| `screenshot_capture` | No | Yes | Yes | Yes |
| `vulnerability_scanning` | No | No | Yes | Yes |
| `compliance_checks` | No | No | Yes | Yes |
| `compliance_reports` | No | No | Yes | Yes |
| `shodan_integration` | No | No | Yes | Yes |
| `securitytrails_integration` | No | No | Yes | Yes |
| `censys_integration` | No | No | No | Yes |
| `custom_api_connectors` | No | No | No | Yes |
| `custom_workflows` | No | No | Yes | Yes |
| `scheduled_scans` | No | No | Yes | Yes |
| `notification_slack` | No | No | Yes | Yes |
| `notification_jira` | No | No | No | Yes |
| `notification_webhook` | No | No | No | Yes |
| `notification_siem` | No | No | No | Yes |
| `cve_monitoring` | No | No | Yes | Yes |

> **Free tier:** All subscription flags are `No`. Free tenants have read-only access to existing scan results only (see §2.7 Subscription Expiry).

#### Plan Change Sync Algorithm

```
BR-FLAG-004: Plan Change Feature Sync
──────────────────────────────────────
Input: tenant_id, new_plan_id

1. LOAD all plan_features rows for new_plan_id
2. FOR EACH feature_flag:
   a. SET plan_enabled = plan_features[feature_flag.id].enabled (or false if no row)
   b. CHECK tenant_feature_overrides for (tenant_id, feature_flag.id)
      IF override exists → SKIP (override survives plan change)
      IF no override → effective value = plan_enabled
3. INVALIDATE Redis cache for tenant (§5.6)
4. IF downgrade (new plan has fewer features):
   a. IDENTIFY features lost (was enabled, now disabled, no override)
   b. FOR EACH lost feature:
      - IF scheduled_scans lost → DISABLE all active schedules for tenant (set status = DISABLED)
      - IF custom_workflows lost → LOCK custom workflows (preserve but cannot execute/edit/clone)
      - IF notification_* lost → DISABLE affected notification channels
   c. LOG("tenant.features_synced", { tenant_id, plan_change: old→new, features_lost: [...], overrides_preserved: [...] })
5. RETURN updated effective feature set
```

> **Key rule:** Tenant overrides always survive plan changes (BR-FLAG-004). A Starter tenant with a super admin override granting `vulnerability_scanning` keeps that access even when switching between Starter plans.

---

### 5.4 Evaluation Algorithm

> Covers: BR-FLAG-001, BR-FLAG-002, BR-FLAG-003

The core feature flag evaluation follows a strict 3-step precedence chain.

#### Precedence Order

```
Operational Flag (global) → Plan Feature (tier) → Tenant Override (per-tenant)
        ▲ highest                                         ▲ lowest
```

If an operational flag is `DISABLED`, evaluation stops immediately — no plan or override check occurs.

#### Evaluation Algorithm

```
BR-FLAG-001: Feature Flag Evaluation
─────────────────────────────────────
Input: tenant_id, feature_name

1. CHECK Redis cache for key tenant:features:{tenant_id}:{feature_name}
   IF cached → RETURN cached value

2. LOAD feature_flag by name
   IF NOT found → REJECT "ERR_FLAG_001"

3. IF feature_flag.type = OPERATIONAL:
   RETURN feature_flag.default_enabled
   (Operational flags have no per-tenant evaluation — they are global)

4. IF feature_flag.type = SUBSCRIPTION:
   a. CHECK operational flags:
      - Find any OPERATIONAL flag that governs the same module
        (e.g., vuln_scanning_global governs vulnerability_scanning)
      - IF operational flag is DISABLED → RETURN false
        LOG("feature.blocked_operational", { tenant_id, feature_name, operational_flag })

   b. CHECK tenant_feature_overrides for (tenant_id, feature_flag.id):
      IF override exists → RETURN override.enabled
        LOG("feature.resolved_override", { tenant_id, feature_name, enabled: override.enabled })

   c. LOAD plan_features for tenant's current plan:
      LOAD tenant → tenant.subscription → subscription.plan_id
      FIND plan_features row for (plan_id, feature_flag.id)
      IF row exists → RETURN row.enabled
      IF no row → RETURN false

5. CACHE result in Redis with TTL 30 minutes
6. RETURN result
```

> **Note on step 4a-4b order:** Operational flags take precedence over overrides. A super admin override cannot bypass an operational disable. This is intentional — operational flags are emergency controls.

#### Bulk Evaluation

```
BR-FLAG-001B: Get All Features for Tenant
──────────────────────────────────────────
Input: tenant_id

1. CHECK Redis cache for key tenant:features:{tenant_id}:all
   IF cached → RETURN cached map

2. LOAD all feature_flags
3. FOR EACH flag:
   EVALUATE using BR-FLAG-001 logic (steps 3-4)
   ADD to result map: { feature_name → { enabled, reason } }
   reason = "operational_disabled" | "override" | "plan" | "default"
4. CACHE full map in Redis with TTL 30 minutes
5. RETURN result map
```

> The `reason` field in bulk evaluation supports the feature visibility API (§5.9) — tenant owners can see *why* a feature is enabled/disabled.

---

### 5.5 Tenant Overrides

> Covers: BR-FLAG-003, BR-FLAG-004

#### Override Rules

- Only `SUPER_ADMIN` can create, update, or delete overrides.
- Only `SUBSCRIPTION` flags can be overridden. Operational flags cannot have tenant overrides.
- A `reason` is always required (audit trail).
- Overrides survive plan changes — they are never auto-removed by the system.
- An override can grant access (enable a feature not in the plan) or revoke access (disable a feature that is in the plan).

#### Override CRUD Algorithms

```
BR-FLAG-003A: Create Tenant Override
─────────────────────────────────────
Input: tenant_id, feature_name, enabled, reason, admin_user_id

1. VALIDATE admin_user_id has role SUPER_ADMIN
   IF NOT → REJECT "ERR_FLAG_005"
2. LOAD feature_flag by feature_name
   IF NOT found → REJECT "ERR_FLAG_001"
3. IF feature_flag.type = OPERATIONAL → REJECT "ERR_FLAG_007"
4. LOAD tenant by tenant_id
   IF NOT found → REJECT "ERR_FLAG_008"
5. IF reason is blank or < 10 characters → REJECT "ERR_FLAG_006"
6. CHECK existing override for (tenant_id, feature_flag.id)
   IF exists → REJECT "ERR_FLAG_009" (use update endpoint instead)
7. INSERT tenant_feature_overrides {
     tenant_id, feature_id, enabled, overridden_by: admin_user_id, reason
   }
8. INVALIDATE Redis cache for tenant (§5.6)
9. AUDIT_LOG("feature.override_created", {
     tenant_id, feature_name, enabled, reason, admin_user_id
   })
10. RETURN override record
```

```
BR-FLAG-003B: Update Tenant Override
─────────────────────────────────────
Input: tenant_id, feature_name, enabled, reason, admin_user_id

1. VALIDATE admin_user_id has role SUPER_ADMIN
   IF NOT → REJECT "ERR_FLAG_005"
2. LOAD existing override for (tenant_id, feature_name)
   IF NOT found → REJECT "ERR_FLAG_010"
3. IF reason is blank or < 10 characters → REJECT "ERR_FLAG_006"
4. UPDATE override: enabled, overridden_by: admin_user_id, reason, updated_at
5. INVALIDATE Redis cache for tenant (§5.6)
6. AUDIT_LOG("feature.override_updated", {
     tenant_id, feature_name, old_enabled, new_enabled, reason, admin_user_id
   })
7. RETURN updated override record
```

```
BR-FLAG-003C: Delete Tenant Override
─────────────────────────────────────
Input: tenant_id, feature_name, admin_user_id

1. VALIDATE admin_user_id has role SUPER_ADMIN
   IF NOT → REJECT "ERR_FLAG_005"
2. LOAD existing override for (tenant_id, feature_name)
   IF NOT found → REJECT "ERR_FLAG_010"
3. DELETE override row
4. INVALIDATE Redis cache for tenant (§5.6)
5. AUDIT_LOG("feature.override_deleted", {
     tenant_id, feature_name, previous_enabled, admin_user_id
   })
6. RETURN success
```

> After deletion, the tenant's effective value for that feature reverts to the plan default.

---

### 5.6 Caching Strategy

> Covers: BR-FLAG-005

#### Cache Design

| Aspect | Value |
|--------|-------|
| Store | Redis |
| Key format (single) | `tenant:features:{tenant_id}:{feature_name}` |
| Key format (bulk) | `tenant:features:{tenant_id}:all` |
| Value (single) | `"true"` or `"false"` |
| Value (bulk) | JSON map: `{ "feature_name": { "enabled": bool, "reason": string }, ... }` |
| TTL | 30 minutes (fallback expiry) |
| Primary invalidation | Explicit on trigger events |

#### Invalidation Triggers

| Trigger | Action | Source |
|---------|--------|--------|
| Plan change (upgrade/downgrade) | Delete all keys matching `tenant:features:{tenant_id}:*` | Subscription service (§3.3) |
| Tenant override created/updated/deleted | Delete all keys matching `tenant:features:{tenant_id}:*` | Override CRUD (§5.5) |
| Operational flag toggled | Delete ALL tenant feature cache keys (`tenant:features:*`) | Admin flag toggle |
| Tenant suspension/deactivation | Delete all keys matching `tenant:features:{tenant_id}:*` | Tenant lifecycle (§2.8) |

#### Cache Miss Behavior

```
1. On cache miss → evaluate from database (BR-FLAG-001)
2. Store result in Redis with 30-minute TTL
3. Return result
```

> **Why invalidate all keys for a tenant (not just the changed flag):** Bulk evaluation cache (`tenant:features:{tenant_id}:all`) contains all flags. Invalidating selectively would leave the bulk cache stale. Simpler and safer to clear all keys for the tenant.

> **Why invalidate ALL tenants on operational flag change:** Operational flags are global. Every tenant's cached result for flags in the affected module could be stale. A full purge is the only safe option.

---

### 5.7 Enforcement Points

> Covers: BR-FLAG-006, BR-FLAG-007

Feature flags are enforced at three layers. Each layer calls `FeatureFlagService.IsEnabledAsync(tenantId, featureName)`.

#### Enforcement Layers

| Layer | When | How | On Block |
|-------|------|-----|----------|
| **API endpoint** | Before processing request | Middleware or attribute-based check | Return HTTP 403 with plan/operational message (BR-FLAG-007) |
| **Scan step execution** | Before running each workflow step | Worker checks flag before step start | Skip step, refund credits, log (§4.9) |
| **UI navigation** | On page load / route guard | Frontend calls bulk feature API | Hide/disable module in navigation |

#### API Endpoint Enforcement

```
BR-FLAG-006A: API Feature Gate
───────────────────────────────
Input: tenant_id, feature_name (from endpoint attribute)

1. CALL IsEnabledAsync(tenant_id, feature_name)
2. IF enabled → PROCEED to controller logic
3. IF NOT enabled:
   a. DETERMINE block reason:
      - IF operational flag disabled → reason = "operational"
      - ELSE → reason = "plan"
   b. IF reason = "operational":
      REJECT "ERR_FLAG_004" (HTTP 403, "Feature temporarily unavailable")
   c. IF reason = "plan":
      LOAD required_tier (lowest tier where flag is enabled)
      REJECT "ERR_FLAG_003" (HTTP 403, "Upgrade to {required_tier} to access {feature_name}")
```

#### Scan Step Enforcement

Defined in §4.9 Feature Flag Enforcement. Two checkpoints:

1. **At scan creation** — filter unavailable steps from workflow, reject if no steps remain (`ERR_SCAN_015`)
2. **At step execution** — re-verify flag before each step, skip + refund if disabled since creation

#### UI Enforcement

```
BR-FLAG-006B: UI Feature Visibility
────────────────────────────────────
Input: tenant_id (from session)

1. Frontend calls GET /api/features (bulk evaluation endpoint)
2. Response: { feature_name: { enabled, reason, required_tier }, ... }
3. FOR EACH navigation item / UI module:
   IF feature flag NOT enabled:
     - Hide from navigation OR show as locked with upgrade prompt
     - Disable interactive elements
   IF feature flag enabled:
     - Show normally
```

> **No client-side-only gating.** UI enforcement is a UX convenience. The API layer is the authoritative gate — a disabled feature always returns 403 regardless of frontend state.

---

### 5.8 Downgrade Behavior

> Covers: BR-FLAG-004

When a tenant loses access to features (plan downgrade, subscription expiry, or override removal), existing resources tied to those features are preserved but locked.

#### Downgrade Effects by Feature

| Lost Feature | Affected Resources | Behavior |
|-------------|-------------------|----------|
| `custom_workflows` | Custom workflow templates | Preserved. Cannot execute, edit, or clone. System workflows unaffected. |
| `scheduled_scans` | Active scan schedules | All active schedules set to `DISABLED`. Preserved but will not trigger. |
| `vulnerability_scanning` | Existing vuln scan results | Results remain accessible (read-only). New scans cannot include vuln step. |
| `compliance_checks` | Existing compliance results | Results remain accessible. New scans cannot include compliance step. |
| `compliance_reports` | Previously generated reports | Reports remain downloadable. Cannot generate new reports. |
| `shodan_integration` | Existing Shodan-enriched results | Results remain accessible. New scans cannot include Shodan step. |
| `securitytrails_integration` | Existing SecurityTrails results | Results remain accessible. New scans cannot include SecurityTrails step. |
| `censys_integration` | Existing Censys results | Results remain accessible. New scans cannot include Censys step. |
| `custom_api_connectors` | Configured connectors | Connector configs preserved. Cannot execute or create new connectors. |
| `notification_slack` | Slack channel configs | Configs preserved. Notifications will not be sent via Slack. |
| `notification_jira` | Jira integration configs | Configs preserved. Jira tickets will not be created. |
| `notification_webhook` | Webhook configs | Configs preserved. Webhooks will not fire. |
| `notification_siem` | SIEM forwarding configs | Configs preserved. SIEM forwarding stops. |

#### Re-upgrade Recovery

When a tenant regains access to a previously lost feature (upgrade or new override):

- **Custom workflows:** Immediately usable again — no action required.
- **Scheduled scans:** Remain `DISABLED`. Tenant must manually re-enable each schedule to confirm intent.
- **Notification channels:** Configs intact. Notifications resume automatically on next trigger.
- **Integration connectors:** Configs intact. Immediately usable again.

> **Scheduled scans require manual re-enable** to prevent surprise scan executions and unexpected credit consumption after upgrade.

---

### 5.9 Feature Visibility API

> Covers: BR-FLAG-007

Tenant owners can view all platform features with their current status and tier requirements.

#### Response Schema

```
GET /api/features
Authorization: Bearer {token}

Response 200:
{
  "features": [
    {
      "name": "vulnerability_scanning",
      "module": "scanning",
      "description": "Vulnerability assessment scan step",
      "enabled": false,
      "reason": "plan",
      "required_tier": "Pro",
      "current_tier": "Starter"
    },
    {
      "name": "subdomain_enumeration",
      "module": "scanning",
      "description": "Subdomain discovery scan step",
      "enabled": true,
      "reason": "plan",
      "required_tier": "Starter",
      "current_tier": "Starter"
    },
    {
      "name": "vuln_scanning_global",
      "module": "scanning",
      "description": "Vulnerability scanning platform availability",
      "enabled": false,
      "reason": "operational",
      "required_tier": null,
      "current_tier": "Starter"
    }
  ]
}
```

#### Response Field Rules

| Field | Rule |
|-------|------|
| `name` | Flag name from registry (§5.1) |
| `module` | Grouping for UI display |
| `description` | Human-readable description |
| `enabled` | Final evaluated result for this tenant |
| `reason` | `"plan"` — tier-gated. `"override"` — super admin override active. `"operational"` — globally disabled. `"default"` — using flag default. |
| `required_tier` | Lowest tier where this flag is enabled. `null` for operational flags. |
| `current_tier` | Tenant's current plan tier |

#### Visibility Rules

| Caller | What They See |
|--------|--------------|
| `TENANT_OWNER` | All features with enabled status, reason, and required tier. Does NOT see override details (who overrode, reason). |
| `SUPER_ADMIN` | All features for any tenant. Includes override details: `overridden_by`, `override_reason`, `override_created_at`. |

> **No information leakage:** Tenant owners see that a feature is enabled via override (reason = `"override"`) but do NOT see the admin who created it or the internal reason. This prevents tenants from gaming the override system.

---

### 5.10 Permissions Matrix

> Covers: BR-FLAG-003, BR-FLAG-006

| Action | `TENANT_OWNER` | `SUPER_ADMIN` |
|--------|:--------------:|:-------------:|
| View own tenant's feature flags | Yes | Yes (any tenant) |
| View all features with tier labels | Yes (own tenant) | Yes (any tenant) |
| View override details (who, reason) | No | Yes |
| Create feature flag definition | No | Yes |
| Update feature flag definition | No | Yes |
| Delete feature flag definition | No | Yes |
| Toggle operational flag | No | Yes |
| Create tenant override | No | Yes |
| Update tenant override | No | Yes |
| Delete tenant override | No | Yes |
| View operational flag status | Yes (via feature API) | Yes (admin panel) |
| Seed plan-feature mappings | No | Yes (or system migration) |
| Invalidate feature cache manually | No | Yes |

> **Tenant owners have read-only access** to feature flag state. All write operations are restricted to `SUPER_ADMIN`. This is consistent with the override rules in §5.5 and the admin-only pattern established in §2.9 (Impersonation).

---

### 5.11 Edge Cases

> Cross-cutting edge cases for the feature flag system.

| Scenario | Behavior |
|----------|----------|
| Tenant override enables feature, then operational flag disables it | Operational flag wins. Feature is blocked. Override remains in database but has no effect until operational flag is re-enabled. |
| Super admin toggles operational flag while scans are in-flight | Currently running step completes. Next step re-checks flag (§4.9). If disabled, step is skipped and credits refunded. |
| Tenant upgrades from Starter to Pro mid-scan | Already-filtered steps remain excluded from current scan. New capabilities apply to future scans only. |
| Tenant downgrades from Pro to Starter with running scan | Running scan completes with originally granted steps. Future scans filter by new plan. |
| Super admin deletes a feature flag that has overrides | CASCADE delete all `plan_features` and `tenant_feature_overrides` rows for that flag. Invalidate all tenant caches. |
| Two super admins update the same override concurrently | Last write wins. Both actions are audit-logged with timestamps. `updated_at` reflects the final state. |
| Tenant on Free tier (subscription expired) attempts any gated action | All subscription flags evaluate to `false`. API returns `ERR_FLAG_003` with message "Upgrade to {tier} to access {feature}". |
| Override exists for a feature the tenant's plan already includes | Override is redundant but harmless. If tenant later downgrades, the override preserves access. |
| Redis cache is unavailable | Fallback to database evaluation. Log warning. Do not block the request — feature flags degrade gracefully. |
| Feature flag name not found in registry | Return `ERR_FLAG_001`. Do not default to enabled or disabled — explicit failure prevents silent misconfiguration. |
| Tenant override created for a tenant in `SUSPENDED` state | Override is stored but has no practical effect. Suspended tenants cannot perform write operations regardless of feature flags (§2.8). |
| Plan-feature mapping is missing for a flag/plan combination | Treat as `false` (feature not available). Log warning for admin investigation. |
| Super admin tries to override an operational flag per-tenant | Rejected with `ERR_FLAG_007`. Operational flags are global only. |
| Bulk feature API called with no features seeded in database | Return empty feature list. Log error for admin — indicates missing seed data. |

---

### 5.12 Error Codes

| Code | HTTP | Message | Cause |
|------|------|---------|-------|
| `ERR_FLAG_001` | 404 | Feature flag not found. | `feature_name` does not match any flag in the registry. |
| `ERR_FLAG_002` | 400 | Invalid flag type. | Attempted to create a flag with a type other than `SUBSCRIPTION` or `OPERATIONAL`. |
| `ERR_FLAG_003` | 403 | Upgrade to {tier} to access {feature}. | Feature is gated by plan tier and tenant's current plan does not include it. No override exists. |
| `ERR_FLAG_004` | 403 | Feature temporarily unavailable. | Feature is blocked by an operational flag. Applies to all tenants regardless of plan or override. |
| `ERR_FLAG_005` | 403 | Insufficient permissions to manage feature flags. | Caller does not have `SUPER_ADMIN` role. |
| `ERR_FLAG_006` | 400 | Override reason is required (minimum 10 characters). | `reason` field is blank or too short when creating/updating an override. |
| `ERR_FLAG_007` | 400 | Operational flags cannot be overridden per tenant. | Attempted to create a tenant override for a flag with `type = OPERATIONAL`. |
| `ERR_FLAG_008` | 404 | Tenant not found. | `tenant_id` in override request does not match any tenant. |
| `ERR_FLAG_009` | 409 | Override already exists for this tenant and feature. Use update endpoint. | Attempted to create a duplicate override for `(tenant_id, feature_id)`. |
| `ERR_FLAG_010` | 404 | Override not found. | Attempted to update or delete an override that does not exist. |
| `ERR_FLAG_011` | 400 | Feature flag name already exists. | Attempted to create a flag with a `name` that is already in the registry. |
| `ERR_FLAG_012` | 400 | Cannot delete feature flag with active overrides. Remove overrides first. | Attempted to delete a flag that still has `tenant_feature_overrides` rows. Delete overrides first or use cascade. |

> **Cross-references:** Feature flag enforcement during scanning uses `ERR_SCAN_007`, `ERR_SCAN_015`, and `ERR_SCAN_017` (§4.14). Those error codes live in the scanning domain since the enforcement happens in the scan creation flow.
