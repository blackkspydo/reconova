# 2. Tenant Management

> Covers: BR-TNT-001 through BR-TNT-014 from the original business rules.
> Error response JSON schema is defined in `00-index.md` Section 12 reference.

### 2.1 Tenant Entity States

> Covers: BR-TNT-005, BR-TNT-010

#### Status Values

| Status | Meaning |
|--------|---------|
| `PROVISIONING` | Tenant created, database cloning in progress. No user access. |
| `ACTIVE` | Fully provisioned and operational. All plan features available. |
| `SUSPENDED` | Blocked by super admin. All access denied. Data preserved. |
| `DEACTIVATED` | Deletion initiated. 30-day backup window in progress. No access. |

#### State Machine

```
                    ┌──────────────┐
      User Signup   │ PROVISIONING │
     ──────────────►│              │
                    └──────┬───────┘
                           │ DB cloned + migrations applied
                           ▼
                    ┌──────────────┐
                    │    ACTIVE    │◄──────────────────────┐
                    │              │   Super admin         │
                    └──┬───────┬───┘   reactivates         │
                       │       │                           │
      Super admin      │       │ Super admin               │
      suspends         │       │ approves deletion         │
                       ▼       ▼                           │
              ┌────────────┐  ┌──────────────┐             │
              │ SUSPENDED  │  │ DEACTIVATED  │             │
              │            │──┤  (30-day     │             │
              └────────────┘  │   retention) │             │
                    │         └──────────────┘             │
                    │                                      │
                    └──────────────────────────────────────┘
```

#### State Transition Table

| From | To | Trigger | Who | Side Effects |
|------|----|---------|-----|-------------|
| _(new)_ | `PROVISIONING` | User signs up and creates tenant | System | Template DB clone initiated. `tenant_databases` record created with status `PROVISIONING`. |
| `PROVISIONING` | `ACTIVE` | DB clone + migrations succeed | System | `tenant_databases.status` → `ACTIVE`. User can log in. Default subscription plan assigned. |
| `PROVISIONING` | `PROVISIONING` | DB clone/migration fails (retry ≤ 3) | System | Retry with exponential backoff (2s, 4s, 8s). Failure logged. |
| `PROVISIONING` | `PROVISIONING` | Max retries (3) exhausted | System | Tenant stays `PROVISIONING`. Super admin notified. Manual intervention required. |
| `ACTIVE` | `SUSPENDED` | Super admin suspends tenant | `SUPER_ADMIN` | All user sessions invalidated. Running scans cancelled (partial results preserved as `CANCELLED`). Scheduled scans disabled. Webhook notification sent. |
| `SUSPENDED` | `ACTIVE` | Super admin reactivates tenant | `SUPER_ADMIN` | Access restored. Scheduled scans remain disabled — tenant must re-enable manually. Audit log entry created. |
| `ACTIVE` | `DEACTIVATED` | Super admin approves deletion request | `SUPER_ADMIN` | Tenant DB backup initiated. All active sessions invalidated. All scheduled scans disabled. 30-day retention countdown starts. |
| `SUSPENDED` | `DEACTIVATED` | Super admin approves deletion of suspended tenant | `SUPER_ADMIN` | Same as ACTIVE → DEACTIVATED. |

**Invalid transitions:** `PROVISIONING` cannot go to `SUSPENDED` or `DEACTIVATED`. `DEACTIVATED` cannot transition to any state (terminal).

---

### 2.2 Field Constraints

> Covers: BR-TNT-002, BR-TNT-004

#### `tenants` Table

| Field | Type | Constraints |
|-------|------|------------|
| `id` | UUID | System-generated. Immutable. Primary key. |
| `name` | string | Required. Max 200 chars. Free-text display name. Mutable by `TENANT_OWNER`. |
| `slug` | string | Required. Max 100 chars. Globally unique. Lowercase alphanumeric + hyphens only. Generated from `name` at creation. Immutable after creation. Indexed. |
| `status` | string | Required. CHECK (`PROVISIONING`, `ACTIVE`, `SUSPENDED`, `DEACTIVATED`). Default: `PROVISIONING`. |
| `plan_id` | UUID | Nullable. FK → `subscription_plans.id`. Null during `PROVISIONING`. Set when tenant activates (defaults to free plan if no plan selected). |
| `deletion_requested_at` | timestamp | Nullable. Set when tenant owner submits deletion request. Cleared if request is denied. Used to track the 30-day retention window once `DEACTIVATED`. |
| `deletion_requested_by` | UUID | Nullable. FK → `users.id`. The user who requested deletion. |
| `created_at` | timestamp | System-generated. Immutable. Default: `NOW()`. |

#### `tenant_databases` Table

| Field | Type | Constraints |
|-------|------|------------|
| `id` | UUID | System-generated. Immutable. Primary key. |
| `tenant_id` | UUID | Required. FK → `tenants.id`. Unique (one-to-one). Immutable. |
| `connection_string` | string | Required. Encrypted at rest. Contains host, port, database name, credentials for the tenant's dedicated PostgreSQL database. |
| `status` | string | Required. CHECK (`PROVISIONING`, `ACTIVE`, `FAILED`). Default: `PROVISIONING`. |
| `template_version` | string | Required. Records which template DB version was cloned. Used for migration tracking. |
| `retry_count` | int | Default: 0. Tracks provisioning retry attempts. Max: 3. |
| `last_retry_at` | timestamp | Nullable. Timestamp of the most recent provisioning retry attempt. |
| `backed_up_at` | timestamp | Nullable. Set when tenant DB backup completes during deactivation. |

---

### 2.3 Tenant Creation & Provisioning

> Covers: BR-TNT-001, BR-TNT-005

#### Self-Service Registration

Any user can sign up and create a tenant. No approval required. Tenant creation happens as part of the registration flow — user account and tenant are created together.

#### Provisioning Algorithm

```
BR-TNT-001: Tenant Creation & Provisioning
───────────────────────────────────────────
Input: tenant_name, user_id

1. GENERATE slug from tenant_name (see §2.4 Slug Rules)
   IF slug already exists → REJECT "ERR_TNT_001"

2. INSERT tenant record:
   {
     id: new_uuid(),
     name: tenant_name,
     slug: slug,
     status: "PROVISIONING",
     plan_id: null,
     created_at: now()
   }

3. LINK user to tenant:
   SET user.tenant_id = tenant.id

4. INITIATE async provisioning:
   CLONE template_db → tenant_{slug}_db
   INSERT tenant_databases record:
   {
     tenant_id: tenant.id,
     connection_string: build_connection_string(slug),
     status: "PROVISIONING",
     template_version: current_template_version,
     retry_count: 0
   }

5. ON CLONE SUCCESS:
   APPLY all base_migrations to tenant_{slug}_db
   FOR EACH migration:
     INSERT tenant_migrations record (type: "base", status: "applied")

6. ON ALL MIGRATIONS APPLIED:
   SET tenant.status = "ACTIVE"
   SET tenant.plan_id = free_plan_id
   SET tenant_databases.status = "ACTIVE"
   AUDIT_LOG("tenant.provisioned", tenant.id, { user_id })

7. ON FAILURE at any point (clone or migration):
   INVOKE retry logic (see BR-TNT-005 below)
```

#### Provisioning Retry Algorithm

```
BR-TNT-005: Provisioning Retry
───────────────────────────────
Input: tenant_id, failure_reason

1. LOAD tenant_databases WHERE tenant_id = tenant_id
2. INCREMENT retry_count
   SET last_retry_at = now()

3. IF retry_count > 3:
     SET tenant_databases.status = "FAILED"
     NOTIFY super_admin:
       "Tenant provisioning failed after 3 retries: {tenant.name} ({tenant.slug})"
     AUDIT_LOG("tenant.provisioning_failed", tenant.id, { retries: 3, reason: failure_reason })
     RETURN

4. WAIT exponential_backoff(retry_count):
     retry 1 → 2 seconds
     retry 2 → 4 seconds
     retry 3 → 8 seconds

5. IF failure was during CLONE:
     DROP partial database IF EXISTS tenant_{slug}_db
     RE-CLONE template_db → tenant_{slug}_db

6. IF failure was during MIGRATION:
     RESUME from last successful migration version

7. ON SUCCESS → continue BR-TNT-001 step 6
   ON FAILURE → GOTO step 1 (re-enter retry loop)
```

#### Provisioning Constraints

| Constraint | Value |
|-----------|-------|
| Max retry attempts | 3 |
| Backoff strategy | Exponential: 2s, 4s, 8s |
| After max retries | `tenant_databases.status` → `FAILED`. Super admin notified. Tenant stays `PROVISIONING`. |
| Super admin resolution | Can manually trigger retry (resets `retry_count` to 0) or delete the failed tenant. |
| Concurrent provisioning | No limit on concurrent tenant provisions. Each runs independently. |

---

### 2.4 Tenant Slug Rules

> Covers: BR-TNT-002

#### Slug Generation Algorithm

```
BR-TNT-002: Slug Generation
────────────────────────────
Input: tenant_name

1. CONVERT tenant_name to lowercase
2. REPLACE spaces with hyphens
3. REMOVE all characters that are NOT alphanumeric or hyphens
4. COLLAPSE consecutive hyphens into a single hyphen
5. TRIM leading and trailing hyphens
6. IF result is empty → REJECT "ERR_TNT_002"
7. IF length(result) > 100 → TRUNCATE to 100 chars, trim trailing hyphen
8. CHECK uniqueness against tenants.slug
   IF exists → REJECT "ERR_TNT_001"
9. RETURN slug
```

#### Slug Constraints

| Constraint | Value |
|-----------|-------|
| Character set | Lowercase `a-z`, digits `0-9`, hyphens `-` |
| Max length | 100 characters |
| Min length | 1 character (after sanitization) |
| Uniqueness | Globally unique across all tenants (including `DEACTIVATED`). |
| Mutability | Immutable after creation. Cannot be changed or recycled. |
| Reserved slugs | `admin`, `api`, `app`, `www`, `mail`, `support`, `help`, `status`, `docs`. System rejects these. |

#### Examples

| Input Name | Generated Slug |
|-----------|---------------|
| `Acme Corp` | `acme-corp` |
| `My Company!!!` | `my-company` |
| `---Test---` | `test` |
| `Company  Name  Here` | `company-name-here` |
| `123 Security` | `123-security` |
| `@#$%` | Rejected — empty after sanitization (`ERR_TNT_002`) |

---

### 2.5 Tenant Isolation & Resolution

> Covers: BR-TNT-004

#### Database-per-Tenant Strategy

Each tenant gets a dedicated PostgreSQL database cloned from the template database. No shared tables between tenants. This provides the strongest isolation level, appropriate for a security product with compliance requirements.

| Aspect | Detail |
|--------|--------|
| Isolation level | Full database isolation. No shared data tables. |
| Database naming | `tenant_{slug}_db` |
| Template source | `reconova_template` database containing all tenant-scoped tables. |
| Connection management | Each tenant's connection string stored encrypted in `tenant_databases`. |
| Cross-tenant queries | Not possible. No database links between tenant databases. |
| Shared data | Platform-level data (plans, feature flags, compliance frameworks, CVE database) lives in the control database only. |

#### Tenant Resolution Algorithm

```
BR-TNT-004: Tenant Context Resolution
──────────────────────────────────────
Input: incoming_request

1. EXTRACT tenant_id from JWT claims
   IF no JWT or no tenant_id claim → REJECT "ERR_TNT_003"

2. CACHE CHECK: Redis key "tenant:conn:{tenant_id}"
   IF cached → SET connection_string = cached_value, GOTO step 5

3. LOAD tenant_databases WHERE tenant_id = tenant_id
   IF NOT found → REJECT "ERR_TNT_004"

4. CACHE connection_string in Redis:
   SET "tenant:conn:{tenant_id}" = connection_string
   TTL: 5 minutes

5. LOAD tenant WHERE id = tenant_id
   IF tenant.status == "SUSPENDED" → REJECT "ERR_TNT_005"
   IF tenant.status == "DEACTIVATED" → REJECT "ERR_TNT_006"
   IF tenant.status == "PROVISIONING" → REJECT "ERR_TNT_007"

6. CREATE DbContext with tenant's connection_string
   ALL queries within this request scope target the tenant's database

7. RETURN tenant_context { tenant_id, connection_string }
```

#### Cache Invalidation

| Event | Action |
|-------|--------|
| Tenant suspended | DELETE Redis key `tenant:conn:{tenant_id}`. Next request will re-fetch and hit status check. |
| Tenant deactivated | DELETE Redis key `tenant:conn:{tenant_id}`. |
| Tenant reactivated | DELETE Redis key `tenant:conn:{tenant_id}`. Re-cached on next request. |
| Connection string rotated | DELETE Redis key `tenant:conn:{tenant_id}`. |

---

### 2.6 Membership Model

> Covers: BR-TNT-003

#### V1: Single User per Tenant

In V1, each tenant has exactly one user — the `TENANT_OWNER` who created it. A user cannot belong to multiple tenants.

| Constraint | Detail |
|-----------|--------|
| Users per tenant | Exactly 1 (`TENANT_OWNER`). |
| Tenants per user | Exactly 1. A user cannot be a member of multiple tenants. |
| Role assignment | Automatically set to `TENANT_OWNER` at registration. |
| Role mutability | Immutable in V1. No role changes within a tenant. |
| User transfer | Not supported. A user cannot move to a different tenant. |
| Super admins | Exist in the control database with `SUPER_ADMIN` role. `tenant_id` is nullable for super admins — they are not tied to any tenant. |

#### Enforcement

```
BR-TNT-003: One Tenant per User
────────────────────────────────
Enforced at: Registration flow (BR-AUTH-001 in §1)

1. During registration, user and tenant are created together
2. user.tenant_id is set to the new tenant's ID
3. user.tenant_id is immutable after creation
4. No API endpoint exists to join an existing tenant
5. No API endpoint exists to create a second tenant
```

#### [POST-MVP] Multi-User Organization Model

Future versions will support multiple users per tenant with a role and permission model:

- Roles: `TENANT_ADMIN`, `MANAGER`, `ANALYST`, `VIEWER`
- Module-level permissions (e.g., `vulnerability_scanning:execute`)
- Custom roles created by tenant admins
- SSO for enterprise tenants
- Invitation flow for adding users to an existing tenant

These are documented here for awareness but are **not part of V1 scope**. No error codes are reserved for multi-user features.

---

### 2.7 Subscription Expiry & Free Tier

> Covers: BR-TNT-011, BR-TNT-012

#### Subscription Expiry Behavior

When a subscription expires or payment fails, the tenant is **immediately** downgraded to the free tier. No grace period.

```
BR-TNT-011: Subscription Expiry Downgrade
──────────────────────────────────────────
Input: tenant_id, trigger (expiry | payment_failure)

1. LOAD tenant_subscription WHERE tenant_id = tenant_id
2. SET tenant_subscription.status = "EXPIRED"
3. SET tenant.plan_id = free_plan_id
4. CREATE new tenant_subscription:
   {
     tenant_id: tenant_id,
     plan_id: free_plan_id,
     status: "ACTIVE",
     current_period_start: now(),
     current_period_end: null,
     credits_remaining: 0,
     credits_used_this_period: 0
   }
5. DISABLE all scan_schedules for tenant
   SET scan_schedules.enabled = false
6. DISABLE all integration_configs for tenant
   SET integration_configs.enabled = false
7. AUDIT_LOG("tenant.downgraded_to_free", tenant_id, { trigger, previous_plan_id })
8. NOTIFY tenant_owner via email:
   "Your subscription has ended. You've been moved to the free tier."
```

#### Free Tier Capabilities

| Capability | Allowed |
|-----------|---------|
| View existing scan results | Yes |
| Run new scans | No |
| Add new domains | No |
| Generate compliance reports | No |
| Scheduled scans | No (disabled on downgrade) |
| Integrations (Slack, webhooks, etc.) | No (disabled on downgrade) |
| CVE monitoring alerts | No |
| Access API | Read-only for existing data |
| View billing history | Yes |
| Upgrade to paid plan | Yes |

#### Free Tier Enforcement

```
BR-TNT-012: Free Tier Access Control
─────────────────────────────────────
Enforced at: API middleware, after tenant resolution

1. LOAD tenant.plan_id
2. IF plan is free tier:
   IF request is a write operation for:
     - scan creation → REJECT "ERR_TNT_008"
     - domain creation → REJECT "ERR_TNT_009"
     - compliance report generation → REJECT "ERR_TNT_010"
     - schedule creation/update → REJECT "ERR_TNT_011"
     - integration creation/update → REJECT "ERR_TNT_012"
   ALLOW read operations on existing data
```

#### Re-Upgrade Behavior

When a free-tier tenant upgrades to a paid plan:
- New `tenant_subscription` record created with the paid plan
- Credits allocated per the new plan's `monthly_credits`
- Scan schedules and integrations remain disabled — tenant must manually re-enable them
- Previously disabled schedules/integrations are preserved, not deleted

---

### 2.8 Suspension & Reactivation

> Covers: BR-TNT-006, BR-TNT-007

#### Suspension Algorithm

```
BR-TNT-006: Tenant Suspension
──────────────────────────────
Input: tenant_id, reason, super_admin_id

1. LOAD tenant WHERE id = tenant_id
   IF NOT found → REJECT "ERR_TNT_004"
   IF tenant.status != "ACTIVE" → REJECT "ERR_TNT_013"

2. SET tenant.status = "SUSPENDED"

3. INVALIDATE all user sessions for this tenant:
   DELETE all sessions WHERE user_id IN (SELECT id FROM users WHERE tenant_id = tenant_id)

4. CANCEL running scans:
   FOR EACH scan_job WHERE tenant_id = tenant_id AND status = "RUNNING":
     SET scan_job.status = "CANCELLED"
     Partial results preserved in scan_results (not deleted)

5. DISABLE scheduled scans:
   SET scan_schedules.enabled = false WHERE tenant_id = tenant_id

6. INVALIDATE tenant cache:
   DELETE Redis key "tenant:conn:{tenant_id}"

7. AUDIT_LOG("tenant.suspended", tenant_id, { reason, suspended_by: super_admin_id })

8. NOTIFY tenant_owner via email:
   "Your account has been suspended. Contact support for more information."
```

#### Suspension Effects Summary

| Aspect | Effect |
|--------|--------|
| User login | Blocked. Tenant status check in login flow rejects with `ERR_TNT_005`. |
| Active sessions | Immediately invalidated. All sessions deleted. |
| Running scans | Cancelled. Partial results preserved as `CANCELLED`. |
| Scheduled scans | Disabled. |
| API access | All requests rejected at tenant resolution middleware. |
| Data | Fully preserved. No data deleted. |
| Billing | Subscription continues unless separately cancelled. No automatic refunds. |
| Integrations | Inaccessible but configuration preserved. |

#### Reactivation Algorithm

```
BR-TNT-007: Tenant Reactivation
────────────────────────────────
Input: tenant_id, super_admin_id

1. LOAD tenant WHERE id = tenant_id
   IF NOT found → REJECT "ERR_TNT_004"
   IF tenant.status != "SUSPENDED" → REJECT "ERR_TNT_014"

2. SET tenant.status = "ACTIVE"

3. INVALIDATE tenant cache:
   DELETE Redis key "tenant:conn:{tenant_id}"

4. AUDIT_LOG("tenant.reactivated", tenant_id, { reactivated_by: super_admin_id })

5. NOTIFY tenant_owner via email:
   "Your account has been reactivated. You can now log in."
```

#### Reactivation Effects Summary

| Aspect | Effect |
|--------|--------|
| User login | Restored. Tenant status check passes. |
| Sessions | None restored. User must log in again. |
| Scheduled scans | Remain disabled. Tenant owner must manually re-enable. |
| Integrations | Accessible again. Configurations preserved from before suspension. |
| Scan history | All prior results (including `CANCELLED` partial results) visible. |
| Billing | If subscription expired during suspension, tenant is on free tier upon reactivation (per §2.7). |

---

### 2.9 Deletion & Data Retention

> Covers: BR-TNT-008, BR-TNT-009

#### Deletion Request Flow

```
BR-TNT-008: Tenant Deletion Request
────────────────────────────────────
Input: tenant_id, user_id (TENANT_OWNER)

1. LOAD tenant WHERE id = tenant_id
   IF NOT found → REJECT "ERR_TNT_004"
   IF tenant.status NOT IN ("ACTIVE", "SUSPENDED") → REJECT "ERR_TNT_015"

2. IF tenant.deletion_requested_at IS NOT NULL → REJECT "ERR_TNT_016"

3. SET tenant.deletion_requested_at = now()
   SET tenant.deletion_requested_by = user_id

4. AUDIT_LOG("tenant.deletion_requested", tenant_id, { requested_by: user_id })

5. NOTIFY super_admins:
   "Tenant '{tenant.name}' ({tenant.slug}) has requested deletion. Awaiting approval."

6. RETURN { message: "Deletion request submitted. Awaiting admin approval." }
```

#### Deletion Approval Flow

```
BR-TNT-008: Tenant Deletion Approval
─────────────────────────────────────
Input: tenant_id, super_admin_id, action (approve | deny)

1. LOAD tenant WHERE id = tenant_id
   IF NOT found → REJECT "ERR_TNT_004"
   IF tenant.deletion_requested_at IS NULL → REJECT "ERR_TNT_017"

2. IF action == "deny":
     SET tenant.deletion_requested_at = null
     SET tenant.deletion_requested_by = null
     AUDIT_LOG("tenant.deletion_denied", tenant_id, { denied_by: super_admin_id })
     NOTIFY tenant_owner: "Your deletion request has been denied."
     RETURN

3. IF action == "approve":
     SET tenant.status = "DEACTIVATED"

4. INVALIDATE all user sessions for this tenant:
   DELETE all sessions WHERE user_id IN (SELECT id FROM users WHERE tenant_id = tenant_id)

5. DISABLE all scheduled scans:
   SET scan_schedules.enabled = false WHERE tenant_id = tenant_id

6. CANCEL running scans:
   FOR EACH scan_job WHERE tenant_id = tenant_id AND status = "RUNNING":
     SET scan_job.status = "CANCELLED"

7. INVALIDATE tenant cache:
   DELETE Redis key "tenant:conn:{tenant_id}"

8. INITIATE tenant database backup (async):
   ON BACKUP COMPLETE → SET tenant_databases.backed_up_at = now()

9. AUDIT_LOG("tenant.deactivated", tenant_id, { approved_by: super_admin_id })

10. NOTIFY tenant_owner via email:
    "Your account has been scheduled for deletion. Data will be retained for 30 days."
```

#### Data Retention Policy

```
BR-TNT-009: Data Retention & Cleanup
─────────────────────────────────────
Runs: Daily background job

1. FIND tenants WHERE status = "DEACTIVATED"
     AND deletion_requested_at + 30 days <= now()
     AND tenant_databases.backed_up_at IS NOT NULL

2. FOR EACH tenant:
   a. DROP DATABASE tenant_{slug}_db
   b. DELETE tenant_databases record
   c. SOFT-DELETE tenant record (retain in control DB)
   d. AUDIT_LOG("tenant.database_dropped", tenant.id)

3. Control DB records retained for minimum 1 year after deactivation:
   - tenant record (soft-deleted)
   - audit_logs entries
   - billing history (credit_transactions, tenant_subscriptions)
   - user records (soft-deleted)

4. AFTER 1 year: Records eligible for permanent deletion per data compliance policy (see §10).
```

#### Retention Timeline

| Event | Timing | Action |
|-------|--------|--------|
| Deletion approved | Day 0 | Tenant → `DEACTIVATED`. Backup initiated. All access blocked. |
| Backup completes | Day 0–1 | `tenant_databases.backed_up_at` set. |
| Database dropped | Day 30 | Tenant DB dropped. `tenant_databases` record deleted. |
| Control DB records purged | Day 365+ | Soft-deleted records eligible for permanent deletion. |

#### Constraints

| Constraint | Value |
|-----------|-------|
| Who can request deletion | `TENANT_OWNER` only. |
| Who can approve/deny | `SUPER_ADMIN` only. |
| Backup before drop | Mandatory. DB is not dropped until `backed_up_at` is set. |
| Retention window | 30 days from `deletion_requested_at`. |
| Control DB retention | Minimum 1 year. |
| Cancel after approval | Not supported. Once `DEACTIVATED`, it is terminal. Super admin must manually intervene at database level if reversal is needed. |

---

### 2.10 Tenant Impersonation

> Covers: BR-TNT-014

#### Impersonation Algorithm

```
BR-TNT-014: Tenant Impersonation
─────────────────────────────────
Input: super_admin_id, target_user_id

1. VERIFY caller has SUPER_ADMIN role
   IF NOT → REJECT "ERR_TNT_018"

2. LOAD target_user WHERE id = target_user_id
   IF NOT found → REJECT "ERR_AUTH_017"

3. LOAD tenant WHERE id = target_user.tenant_id
   IF tenant.status != "ACTIVE" → REJECT "ERR_TNT_019"

4. CREATE impersonation session:
   {
     user_id: target_user_id,
     refresh_token_hash: sha256(refresh_token),
     is_used: false,
     ip_address: super_admin_ip,
     user_agent: super_admin_user_agent,
     created_at: now(),
     last_active_at: now(),
     expires_at: now() + 1 hour
   }

5. GENERATE jwt_token (15 min expiry) with claims:
   {
     user_id: target_user_id,
     tenant_id: target_user.tenant_id,
     role: target_user.role,
     session_id: session.id,
     is_impersonation: true,
     impersonated_by: super_admin_id
   }

6. AUDIT_LOG("tenant.impersonation_started", tenant_id, {
     super_admin_id,
     target_user_id,
     ip_address: super_admin_ip,
     expires_at: now() + 1 hour
   })

7. RETURN { token, refresh_token, expires_in: 900, impersonation: true }
```

#### Impersonation Constraints

| Constraint | Value |
|-----------|-------|
| Who can impersonate | `SUPER_ADMIN` only. |
| Target | Any `TENANT_OWNER` in an `ACTIVE` tenant. |
| Max session duration | 1 hour. Hard limit. Not extendable. |
| Session refresh | Allowed within the 1-hour window. `expires_at` is not extended on refresh. |
| Concurrent impersonation | Super admin can have one impersonation session at a time. Starting a new one invalidates the previous. |
| Audit trail | Every impersonation creates an explicit audit entry. All actions during impersonation are logged with `is_impersonation = true` and `impersonated_by = super_admin_id`. |
| Impersonate another super admin | Not allowed. Can only impersonate `TENANT_OWNER` users. |

#### Impersonation End

```
BR-TNT-014: Impersonation Session End
──────────────────────────────────────
Triggers: manual logout, session expiry (1 hour), or new impersonation started

1. DELETE impersonation session

2. AUDIT_LOG("tenant.impersonation_ended", tenant_id, {
     super_admin_id,
     target_user_id,
     reason: "manual" | "expired" | "replaced"
   })
```

#### Impersonation Visibility

The impersonation state is visible in the JWT claims. The frontend should display a clear indicator (e.g., banner) when operating under impersonation. The tenant owner is **not** notified when their account is being impersonated.

---

### 2.11 Domain Ownership Verification

> Covers: BR-TNT-013

#### [POST-MVP] DNS TXT Record Verification

Domain ownership verification is deferred from MVP. Until implemented, scans work on any domain without verification.

#### MVP Behavior

| Aspect | Detail |
|--------|--------|
| Verification required | No. Any domain can be added and scanned. |
| Duplicate domains across tenants | Allowed. Multiple tenants can scan the same domain independently. |
| Domain limit | Enforced by subscription plan's `max_domains` field. |

#### [POST-MVP] Planned Verification Flow

```
BR-TNT-013: Domain Ownership Verification
──────────────────────────────────────────
[POST-MVP] — Reserved for future implementation

Input: tenant_id, domain_name

1. GENERATE verification token: "reconova-verify={random_32_hex}"

2. INSTRUCT user to create DNS TXT record:
   _reconova-verify.{domain_name} TXT "{verification_token}"

3. STORE verification_token in domains table:
   SET domains.verification_token = token
   SET domains.verification_status = "PENDING"

4. POLL DNS (async, max 72 hours):
   QUERY TXT records for _reconova-verify.{domain_name}
   IF token found:
     SET domains.verification_status = "VERIFIED"
     SET domains.verified_at = now()
     AUDIT_LOG("domain.verified", tenant_id, { domain: domain_name })
   IF 72 hours elapsed without match:
     SET domains.verification_status = "FAILED"
     NOTIFY tenant_owner: "Domain verification failed for {domain_name}"

5. ONCE VERIFIED:
   Scans can only run on verified domains
   Unverified domains are read-only (can be added but not scanned)
```

#### [POST-MVP] Reserved Error Codes

| Code | HTTP | Message | Cause |
|------|------|---------|-------|
| `ERR_TNT_020` | 400 | Domain verification is pending | Scan attempted on unverified domain |
| `ERR_TNT_021` | 400 | Domain verification failed. Please retry. | Verification timed out after 72 hours |

---

### 2.12 Edge Cases

> Cross-cutting edge cases for tenant management

| Scenario | Behavior |
|----------|----------|
| User signs up with a name that produces a slug collision | Slug generation rejects with `ERR_TNT_001`. User must choose a different tenant name. No auto-suffixing (e.g., `acme-corp-2`). |
| User signs up with a name that sanitizes to empty string | Rejected with `ERR_TNT_002`. Example: tenant name `@#$%`. |
| User signs up with a reserved slug (`admin`, `api`, `www`, etc.) | Rejected with `ERR_TNT_001`. Same error as slug collision — no distinction exposed. |
| Provisioning fails on first attempt, succeeds on retry | Tenant activates normally. `retry_count` reflects the number of attempts. No user-visible impact. |
| Provisioning fails after 3 retries | Tenant stays `PROVISIONING`. `tenant_databases.status` → `FAILED`. Super admin notified. User cannot log in (tenant resolution rejects `PROVISIONING` status). |
| Super admin suspends a tenant with running scans | Running scans cancelled immediately. Partial results preserved with status `CANCELLED`. Scan credits consumed up to the cancellation point are not refunded. |
| Super admin suspends a tenant that is already `SUSPENDED` | Rejected with `ERR_TNT_013`. Only `ACTIVE` tenants can be suspended. |
| Super admin reactivates a tenant whose subscription expired during suspension | Tenant reactivates to `ACTIVE` but on free tier (subscription expiry was processed while suspended per §2.7). Tenant must upgrade to regain paid features. |
| Tenant owner requests deletion while tenant is `SUSPENDED` | Allowed. Deletion request is stored. Super admin can approve/deny. |
| Tenant owner requests deletion twice | Second request rejected with `ERR_TNT_016`. Previous request is already pending. |
| Super admin denies deletion request, tenant owner re-requests | Allowed. `deletion_requested_at` was cleared on denial, so a new request can be submitted. |
| Deletion approved but backup fails | Database is NOT dropped. Daily cleanup job skips tenants where `backed_up_at` is null. Super admin must investigate backup failure. |
| Two users try to register the same tenant name simultaneously | One succeeds, the other gets `ERR_TNT_001` (slug uniqueness enforced by database unique index). No partial state — transaction rolls back on conflict. |
| Impersonation session expires mid-action | Current API call completes (JWT is still valid for its 15-min lifetime). Next token refresh fails because session `expires_at` has passed. Super admin is logged out of impersonation. |
| Super admin impersonates a user, then the tenant gets suspended by another super admin | Impersonation session continues until next API call hits tenant resolution, which rejects with `ERR_TNT_005`. Impersonation effectively ends. |
| Tenant name is updated after creation | Allowed — `name` is mutable. Slug remains unchanged (immutable). Display name updates everywhere but URLs/identifiers stay the same. |
| Redis cache contains stale connection string after DB migration | Cache TTL is 5 minutes. Stale entries expire naturally. For immediate invalidation, cache is explicitly deleted on tenant status changes (see §2.5). |
| Free tier tenant attempts to create a scan schedule | Rejected with `ERR_TNT_011`. Free tier enforcement at API middleware level. |

---

### 2.13 Error Codes

> All `ERR_TNT_*` error codes for tenant management

| Code | HTTP | Message | Cause |
|------|------|---------|-------|
| `ERR_TNT_001` | 409 | Tenant name is unavailable | Slug collision with existing tenant or reserved slug |
| `ERR_TNT_002` | 400 | Tenant name is invalid | Name produces empty slug after sanitization |
| `ERR_TNT_003` | 401 | Tenant context could not be resolved | No tenant_id in JWT claims |
| `ERR_TNT_004` | 404 | Tenant not found | Tenant ID does not exist in control DB |
| `ERR_TNT_005` | 403 | Account is suspended. Contact support. | Tenant status is `SUSPENDED` |
| `ERR_TNT_006` | 403 | Account has been deactivated. | Tenant status is `DEACTIVATED` |
| `ERR_TNT_007` | 403 | Account is being set up. Please wait. | Tenant status is `PROVISIONING` |
| `ERR_TNT_008` | 403 | Upgrade required to run scans | Free tier tenant attempted scan creation |
| `ERR_TNT_009` | 403 | Upgrade required to add domains | Free tier tenant attempted domain creation |
| `ERR_TNT_010` | 403 | Upgrade required to generate compliance reports | Free tier tenant attempted compliance report generation |
| `ERR_TNT_011` | 403 | Upgrade required to manage scan schedules | Free tier tenant attempted schedule creation/update |
| `ERR_TNT_012` | 403 | Upgrade required to manage integrations | Free tier tenant attempted integration creation/update |
| `ERR_TNT_013` | 409 | Tenant is not in a valid state for this action | Suspension attempted on non-ACTIVE tenant, or other invalid state transition |
| `ERR_TNT_014` | 409 | Tenant is not suspended | Reactivation attempted on non-SUSPENDED tenant |
| `ERR_TNT_015` | 409 | Tenant cannot be deleted in its current state | Deletion requested on `DEACTIVATED` or `PROVISIONING` tenant |
| `ERR_TNT_016` | 409 | Deletion request already pending | Tenant owner submitted a second deletion request |
| `ERR_TNT_017` | 400 | No deletion request pending for this tenant | Super admin tried to approve/deny deletion with no pending request |
| `ERR_TNT_018` | 403 | Insufficient permissions for impersonation | Non-super-admin attempted impersonation |
| `ERR_TNT_019` | 400 | Cannot impersonate users in inactive tenants | Impersonation attempted on non-ACTIVE tenant |
| `ERR_TNT_020` | 400 | Domain verification is pending | [POST-MVP] Scan attempted on unverified domain |
| `ERR_TNT_021` | 400 | Domain verification failed. Please retry. | [POST-MVP] Verification timed out after 72 hours |

---
