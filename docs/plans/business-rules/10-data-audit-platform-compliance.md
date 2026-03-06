# 10. Data, Audit & Platform Compliance

> Covers: BR-DATA-001 through BR-DATA-023 (original 19 rules expanded + 4 new GDPR/privacy rules).
> Migration versioning mechanics are defined in Section 13 (BR-VER-008 through BR-VER-013).
> Error response JSON schema is defined in `12-error-response-schema.md`.

---

### 10.1 Data Retention

> Covers: BR-DATA-001 through BR-DATA-005

**BR-DATA-001: Scan result retention by tier** — Scan results in tenant DBs are auto-purged (hard delete) after the retention period:

| Tier | Retention | Config Key |
|------|-----------|------------|
| Starter | 30 days | `compliance.report.retention_days_starter` |
| Pro | 90 days | `compliance.report.retention_days_pro` |
| Enterprise | 1 year | `compliance.report.retention_days_enterprise` |

#### Purge Algorithm

```
BR-DATA-001A: Scan Result Purge
---------------------------------
Trigger: Daily scheduled job (02:00 UTC)

1. FOR EACH active tenant:
   a. RESOLVE retention period from tenant's current plan
   b. IDENTIFY scan_results WHERE scan_job.completed_at < (NOW() - retention_period)
   c. HARD DELETE identified scan_results rows
   d. HARD DELETE vulnerabilities rows linked to deleted scan_results
   e. HARD DELETE orphaned scan_jobs with no remaining scan_results
   f. LOG("data.scan_results_purged", {
        tenant_id, scan_results_deleted, scan_jobs_deleted, retention_days
      })
2. Do NOT purge:
   - compliance_assessments or control_results (independent retention)
   - screenshots (purged separately by storage cleanup job)
   - subdomains, ports, technologies (retained as discovery data, not scan-lifecycle)
```

**BR-DATA-002: Audit log retention** — Minimum 1 year for all audit logs. Not configurable per tenant. Config: `admin.audit_log.retention_days` (default 365, min 180). Purge only via automated retention job. Audit logs older than the retention period are hard deleted in monthly batches.

**BR-DATA-003: Tenant DB backups** — Daily encrypted backups of each tenant database.

| Aspect | Value |
|--------|-------|
| Frequency | Daily, 03:00 UTC |
| Encryption | AES-256 using `ENCRYPTION_MASTER_KEY` |
| Retention | 30 days, then auto-deleted |
| Verification | Weekly restore test on random 10% sample of tenant DBs |
| Failure handling | Alert super admin. Retry once. If second attempt fails, flag tenant for manual backup. |

**BR-DATA-004: Credit transaction retention** — Credit transaction history (`credit_transactions`) is retained for the lifetime of the tenant account. Never auto-purged. Required for billing disputes, audit trail, and financial reconciliation.

**BR-DATA-005: Notification history retention** — 90 days, then hard deleted by the daily purge job (02:00 UTC). Notification configs (`integration_configs`, `notification_rules`) are NOT purged — only the delivery history.

---

### 10.2 Purge Strategy

> Covers: BR-DATA-023

**BR-DATA-023: Purge strategy** — Different data types use different deletion strategies:

| Data Type | Location | Strategy | Reason |
|-----------|----------|----------|--------|
| Scan results, vulnerabilities | Tenant DB | Hard delete | Bulky, retention-limited, no audit requirement after expiry |
| Notification history | Tenant DB | Hard delete | Delivery logs, low audit value after 90 days |
| Migration backup files | File storage | Physical delete | Storage cleanup, backup already served its purpose |
| Screenshot files | Object storage | Hard delete | Storage cleanup, linked scan results already purged |
| Tenant records (on deletion) | Control DB | Soft delete (`deleted_at`) | Retained for billing reconciliation and audit trail |
| User records (on deletion) | Control DB | Soft delete (`deleted_at`) | Retained for audit log references |
| Subscription records (on deletion) | Control DB | Soft delete (`deleted_at`) | Retained for financial records |
| Credit transactions | Control DB | Never deleted | Lifetime retention, financial audit requirement |
| Audit logs | Control DB | Hard delete after retention | Only by automated job after minimum 1 year |

> **Soft delete convention:** Soft-deleted records have a `deleted_at` timestamp set. Application queries filter `WHERE deleted_at IS NULL` by default. Super admin can query soft-deleted records for audit purposes.

---

### 10.3 Security & Encryption

> Covers: BR-DATA-006 through BR-DATA-010

**BR-DATA-006: Encryption at rest** — All PostgreSQL databases (control DB and all tenant DBs) encrypted at rest using disk-level encryption (AES-256). Managed by infrastructure layer. Application layer does not manage disk encryption.

**BR-DATA-007: Encryption in transit** — TLS 1.2+ required for ALL connections:

| Connection | Minimum TLS | Enforcement |
|-----------|-------------|-------------|
| Client -> API | TLS 1.2 | HTTP requests rejected with 301 redirect to HTTPS |
| API -> PostgreSQL | TLS 1.2 | `sslmode=require` in connection string |
| API -> Redis | TLS 1.2 | In production and staging environments |
| Worker -> PostgreSQL | TLS 1.2 | Same as API |
| Worker -> External APIs | TLS 1.2 | Reject connections to APIs without TLS |
| Worker -> External tools | N/A | Local tool execution, no network TLS needed |

**BR-DATA-008: API key encryption** — Platform API keys (Shodan, SecurityTrails, etc.) stored encrypted with AES-256 using the `ENCRYPTION_MASTER_KEY` bootstrap env var.

| Aspect | Rule |
|--------|------|
| Storage | Encrypted at rest in `platform_api_keys.api_key_encrypted` |
| Decryption | Only in memory during active use by scan workers |
| Logging | Never logged in plaintext. Masked as `****{last4}` |
| Admin UI | Masked as `****{last4}`. Full value shown only on explicit reveal action (audit-logged) |
| Key rotation | When master key is rotated, all API keys re-encrypted with new key |

**BR-DATA-009: Password hashing** — BCrypt with minimum cost factor of 12.

| Aspect | Rule |
|--------|------|
| Algorithm | BCrypt |
| Cost factor | Minimum 12 (configurable up, never down) |
| Storage | `users.password_hash` column |
| API exposure | Never included in API responses, exports, or logs |
| Migration | If cost factor increases, existing hashes are re-hashed on next successful login |

**BR-DATA-010: 2FA secret storage** — TOTP secrets stored encrypted with AES-256 (same master key as API keys).

| Aspect | Rule |
|--------|------|
| TOTP secret | Encrypted with AES-256. Decrypted only during TOTP validation. |
| Recovery codes | Stored as BCrypt hashes (cost factor 12). One-time use — hash deleted after successful use. |
| Key rotation | Same as API key rotation — re-encrypt all secrets when master key changes. Grace period: old key accepted for 24 hours. |

---

### 10.4 Audit Logging

> Covers: BR-DATA-011 through BR-DATA-013

**BR-DATA-011: Auditable events** — Every significant system action is logged:

| Category | Events |
|----------|--------|
| Authentication | `auth.login`, `auth.logout`, `auth.login_failed`, `auth.2fa_setup`, `auth.2fa_verified`, `auth.password_changed`, `auth.password_reset`, `auth.session_revoked`, `auth.account_locked` |
| Tenant lifecycle | `tenant.created`, `tenant.updated`, `tenant.suspended`, `tenant.reactivated`, `tenant.deactivated`, `tenant.deleted` |
| Domain management | `domain.added`, `domain.verified`, `domain.removed` |
| Scanning | `scan.created`, `scan.started`, `scan.completed`, `scan.failed`, `scan.cancelled`, `scan.schedule_created`, `scan.schedule_updated`, `scan.schedule_deleted` |
| Billing | `billing.plan_changed`, `billing.credit_purchased`, `billing.credit_allotted`, `billing.refund_issued`, `billing.payment_failed`, `billing.subscription_cancelled` |
| Feature flags | `feature.flag_created`, `feature.flag_updated`, `feature.flag_deleted`, `feature.override_created`, `feature.override_updated`, `feature.override_deleted`, `feature.operational_toggled` |
| Compliance | `compliance.framework_selected`, `compliance.assessment_generated`, `compliance.report_downloaded` |
| Integrations | `integration.created`, `integration.updated`, `integration.deleted`, `notification.sent`, `notification.failed` |
| Super admin | `admin.impersonation_started`, `admin.impersonation_ended`, `admin.tenant_override`, `admin.config_changed`, `admin.migration_applied`, `admin.api_key_rotated` |
| Config changes | `config.updated`, `config.rolled_back`, `config.critical_requested`, `config.critical_approved`, `config.critical_rejected` |
| Data operations | `data.export_requested`, `data.export_completed`, `data.deletion_requested`, `data.deletion_completed`, `data.deletion_cancelled` |

**BR-DATA-012: Audit log immutability** — Audit logs are append-only. Cannot be modified or deleted by any user including super admin.

#### Enforcement

| Layer | Mechanism |
|-------|-----------|
| Database | Application DB role has INSERT only on `audit_logs`. No UPDATE or DELETE grants. |
| Application | No update/delete endpoints for audit logs. No ORM delete methods wired. |
| API | GET-only endpoints for audit log access. 405 Method Not Allowed for other verbs. |
| Retention | Only the automated retention job (running as infrastructure role) can delete expired logs. |

**BR-DATA-013: Audit log fields** — Every audit log entry includes:

#### `audit_logs` table (control DB)

| Field | Type | Constraints |
|-------|------|-------------|
| `id` | uuid | PK. Immutable. |
| `tenant_id` | uuid | NULL for platform-level actions. FK -> `tenants.id` for tenant-scoped actions. |
| `user_id` | uuid | NOT NULL. FK -> `users.id`. System actions use a reserved system user ID. |
| `action` | string(100) | NOT NULL. Dot-notation event name. |
| `resource_type` | string(50) | NOT NULL. Entity type: `tenant`, `scan_job`, `feature_flag`, `system_config`, etc. |
| `resource_id` | string(100) | NULL. ID of affected resource. NULL for non-resource actions (login, logout). |
| `details_json` | jsonb | NOT NULL. Structured event data. Content varies by action. Default `{}`. |
| `ip_address` | string(45) | NOT NULL. Client IP (IPv4 or IPv6). |
| `user_agent` | string(500) | NOT NULL. Client user agent string. |
| `timestamp` | timestamp | NOT NULL. UTC. Auto-set on insert. Immutable. |
| `is_super_admin` | boolean | NOT NULL. Whether the acting user is a super admin. |
| `impersonated_by` | uuid | NULL. FK -> `users.id`. Set if action performed during impersonation. |

> **Partitioning:** `audit_logs` table is partitioned by month on `timestamp` for query performance. Indexes on `tenant_id`, `user_id`, `action`, and `timestamp`.

---

### 10.5 Data Isolation

> Covers: BR-DATA-014, BR-DATA-015

**BR-DATA-014: Cross-tenant data isolation** — No API endpoint or query can access data from a different tenant's database. Enforced at multiple layers:

| Layer | Mechanism |
|-------|-----------|
| Middleware | Tenant resolution extracts `tenant_id` from JWT. DbContext created with tenant's connection string. |
| Database | Each tenant has a separate PostgreSQL database. No shared tables for tenant data. |
| Application | All repository queries scoped to resolved tenant DbContext. No raw SQL with cross-DB queries. |
| API | Endpoints accepting resource IDs always verify the resource belongs to the requesting tenant. |
| Worker | Scan workers receive `tenant_id` with each job. Worker creates tenant-scoped DbContext. |
| Cache | Redis keys include `tenant_id` in key prefix. No cross-tenant cache pollution. |

#### Isolation Verification

```
BR-DATA-014A: Tenant Isolation Check
---------------------------------------
Performed at: Every API request, every worker job

1. EXTRACT tenant_id from JWT claims (API) or job payload (worker)
2. RESOLVE tenant database connection from control DB (cached in Redis)
3. CREATE DbContext with tenant-specific connection string
4. ALL queries within this request/job scope target ONLY this tenant's DB
5. IF request includes a resource_id:
   LOAD resource from tenant DB
   IF NOT found -> REJECT 404 (resource doesn't exist in this tenant's scope)
```

**BR-DATA-015: Tenant DB naming convention** — Database name format: `tenant_{slug}` where `slug` is the sanitized tenant slug.

| Rule | Example |
|------|---------|
| Format | `tenant_{slug}` |
| Slug | Lowercase, alphanumeric + hyphens only |
| Example | Tenant "Acme Corp" with slug `acme-corp` -> database `tenant_acme-corp` |
| Uniqueness | Enforced by tenant slug uniqueness constraint (section 2) |

---

### 10.6 Migration Data Safety

> Covers: BR-DATA-016 through BR-DATA-019
> Migration versioning mechanics (ordering, rollback, approval) are defined in Section 13 (BR-VER-008 through BR-VER-013). This subsection adds data-specific safety rules.

**BR-DATA-016: Pre-migration backup** — Before applying any base migration to a tenant DB, create a point-in-time backup.

```
BR-DATA-016A: Migration Backup Flow
--------------------------------------
Input: tenant_id, migration_id

1. CREATE point-in-time backup of tenant DB
2. VERIFY backup integrity (checksum)
3. STORE backup reference: {
     tenant_id, migration_id, backup_path, checksum, created_at
   }
4. PROCEED with migration (BR-VER-009A validation, then apply)
5. IF migration succeeds:
   RETAIN backup for 7 days, then auto-delete
6. IF migration fails AND rollback succeeds:
   DELETE backup immediately (no longer needed)
7. IF migration fails AND rollback fails:
   RETAIN backup indefinitely
   ALERT super admin with backup reference for manual restore
```

**BR-DATA-017: Data integrity verification** — After each successful migration, run integrity checks:

```
BR-DATA-017A: Post-Migration Integrity Check
-----------------------------------------------
Input: tenant_id, migration_id

1. VERIFY foreign key constraints are intact (no orphaned references)
2. VERIFY all indexes exist and are valid (pg_index.indisvalid)
3. VERIFY CHECK constraints pass on all modified tables
4. IF any check fails:
   a. ALERT super admin: "migration.integrity_warning" {
        tenant_id, migration_id, failed_checks
      }
   b. Do NOT auto-rollback (data may be fine, schema change intentional)
   c. Flag tenant migration as NEEDS_REVIEW
5. LOG("migration.integrity_check", {
     tenant_id, migration_id, all_passed: bool, checks_failed: []
   })
```

**BR-DATA-018: Migration backup cleanup** — Migration backup files are cleaned up based on migration outcome:

| Scenario | Cleanup |
|----------|---------|
| Migration succeeded | Delete backup after 7 days |
| Migration rolled back successfully | Delete backup immediately |
| Migration failed, rollback failed | Retain until super admin confirms manual resolution |

**BR-DATA-019: Schema conflict detection** — Before applying a base migration to a tenant with tenant-specific migrations:

```
BR-DATA-019A: Schema Conflict Check
--------------------------------------
Input: tenant_id, base_migration_id

1. LOAD all tenant-specific migrations applied to this tenant
2. FOR EACH tenant-specific migration:
   a. COMPARE target tables/columns against base migration's targets
   b. IF overlap detected (same table + column modified by both):
      FLAG conflict: { tenant_id, base_migration_id, conflicting_tenant_migration_id, table, column }
3. IF any conflicts detected:
   a. SKIP this tenant for this migration batch
   b. ALERT super admin: "migration.schema_conflict" { tenant_id, conflicts }
   c. Continue applying migration to other tenants
4. IF no conflicts -> proceed with migration
```

---

### 10.7 Data Privacy & Portability

> Covers: BR-DATA-020 through BR-DATA-022

**BR-DATA-020: Data export (right to portability)** — Tenant owners can request a full export of their tenant data.

```
BR-DATA-020A: Tenant Data Export
----------------------------------
Input: tenant_id, requested_by

1. VALIDATE requested_by is TENANT_OWNER or SUPER_ADMIN
2. CHECK for existing pending/processing export for this tenant
   IF exists -> REJECT "ERR_DATA_001"
3. CREATE export_job { tenant_id, status: 'PROCESSING', requested_at: NOW() }
4. EXPORT tenant DB data as JSON:
   - domains, subdomains, ports, technologies
   - screenshots (metadata + storage URLs, not binary data)
   - scan_jobs, scan_results, vulnerabilities
   - workflows, workflow_templates (custom only), scan_schedules
   - compliance_assessments, control_results
   - integration_configs, notification_rules, notification_history
5. EXPORT control DB data for this tenant as JSON:
   - tenant record (excluding internal fields)
   - subscription history, credit transactions
   - feature flag overrides
   - audit logs (tenant-scoped only)
6. PACKAGE as encrypted ZIP file (AES-256, password sent separately)
7. GENERATE time-limited download URL (24 hours)
8. NOTIFY tenant: "Your data export is ready. Download within 24 hours."
9. AUDIT_LOG("data.export_completed", {
     tenant_id, file_size_mb, tables_exported, requested_by
   })
10. Auto-delete export file after 24 hours or first download (whichever comes first)
```

**BR-DATA-021: Data deletion (right to erasure)** — Tenant owners can request deletion of all their data.

```
BR-DATA-021A: Tenant Data Deletion Request
---------------------------------------------
Input: tenant_id, requested_by, confirmation_phrase

1. VALIDATE requested_by is TENANT_OWNER
2. VALIDATE confirmation_phrase matches "DELETE {tenant_slug}" (case-sensitive)
   IF NOT -> REJECT "ERR_DATA_004"
3. CHECK for existing pending deletion request
   IF exists -> REJECT "ERR_DATA_003"
4. REQUIRE re-authentication (password + 2FA)
   IF fails -> REJECT "ERR_DATA_005"
5. CREATE deletion_request {
     tenant_id, status: 'PENDING',
     requested_by, requested_at: NOW(),
     scheduled_at: NOW() + 72 hours
   }
6. NOTIFY tenant: "Deletion scheduled for {scheduled_at}. Cancel within 72 hours by visiting your account settings."
7. AUDIT_LOG("data.deletion_requested", { tenant_id, scheduled_at })
```

```
BR-DATA-021B: Deletion Execution
-----------------------------------
Trigger: scheduled_at reached for a PENDING deletion request

1. VERIFY request.status is still 'PENDING' (not cancelled)
2. CREATE final backup of tenant DB (retained 30 days for legal hold)
3. CANCEL all active subscriptions via Stripe API
4. DROP tenant database
5. SOFT-DELETE control DB records:
   - SET tenants.deleted_at = NOW()
   - SET users.deleted_at = NOW() (for users linked to this tenant)
   - SET tenant_subscriptions.deleted_at = NOW()
6. RETAIN (do NOT delete):
   - audit_logs (minimum 1 year retention)
   - credit_transactions (lifetime retention)
7. SET deletion_request.status = 'COMPLETED'
8. SEND final email to tenant owner: "Your Reconova account and data have been deleted."
9. AUDIT_LOG("data.deletion_completed", {
     tenant_id, backup_path, records_soft_deleted
   })
```

```
BR-DATA-021C: Cancel Deletion
--------------------------------
Input: tenant_id, requested_by

1. LOAD pending deletion request for tenant
   IF NOT found -> REJECT "ERR_DATA_007"
2. IF NOW() > request.scheduled_at -> REJECT "ERR_DATA_007" (too late)
3. SET request.status = 'CANCELLED'
4. NOTIFY tenant: "Deletion request cancelled. Your account is safe."
5. AUDIT_LOG("data.deletion_cancelled", { tenant_id, requested_by })
```

**BR-DATA-022: Data processing transparency** — Tenants can view what data Reconova stores about them via a data inventory endpoint.

```
GET /api/v1/data-inventory
Authorization: Bearer {token}

Response 200:
{
  "data_categories": [
    {
      "category": "Scan Results",
      "description": "Subdomain, port, technology, and vulnerability scan data",
      "retention": "30 days (Starter) / 90 days (Pro) / 1 year (Enterprise)",
      "location": "Tenant database",
      "deletable": true
    },
    {
      "category": "Billing Information",
      "description": "Subscription history, credit transactions, payment references",
      "retention": "Lifetime (credit transactions), soft-deleted on account deletion",
      "location": "Control database",
      "deletable": false
    },
    {
      "category": "Audit Logs",
      "description": "Login history, action history, admin actions",
      "retention": "Minimum 1 year",
      "location": "Control database",
      "deletable": false
    },
    {
      "category": "Account Information",
      "description": "Email, tenant name, domain list",
      "retention": "Active account lifetime",
      "location": "Control database",
      "deletable": true
    }
  ],
  "third_party_processors": [
    {
      "name": "Stripe",
      "purpose": "Payment processing",
      "data_shared": "Email, payment method tokens"
    },
    {
      "name": "Shodan / SecurityTrails / Censys",
      "purpose": "Scan data enrichment",
      "data_shared": "Target domains and subdomains (during scans only)"
    }
  ],
  "data_residency": "Configured by platform operator. Contact support for details.",
  "export_available": true,
  "deletion_available": true
}
```

---

### 10.8 Permissions Matrix

| Action | `TENANT_OWNER` | `SUPER_ADMIN` |
|--------|:--------------:|:-------------:|
| View own audit logs | Yes (own tenant) | Yes (any tenant) |
| View cross-tenant audit logs | No | Yes |
| Modify/delete audit logs | No | No (immutable) |
| Request data export | Yes (own tenant) | Yes (any tenant) |
| Download data export | Yes (own tenant) | Yes (any tenant) |
| Request data deletion | Yes (own tenant) | Yes (any tenant, via admin panel) |
| Cancel deletion request | Yes (within 72h) | Yes (any time before execution) |
| View data inventory | Yes (own tenant) | Yes (any tenant) |
| View backup status | No | Yes |
| Trigger manual backup | No | Yes |
| Restore from backup | No | Yes |
| View migration status | No | Yes |
| Run data integrity check | No | Yes |
| View soft-deleted records | No | Yes |
| Purge soft-deleted records permanently | No | No (automated only) |

---

### 10.9 Edge Cases

| Scenario | Behavior |
|----------|----------|
| Data export requested during active scan | Export includes all completed results. In-progress scan data is excluded. Export note: "Active scan in progress — results not included." |
| Deletion requested with active subscription | Subscription cancelled via Stripe as part of deletion flow. Refund follows Stripe's cancellation policy — no automatic refund from Reconova. |
| Deletion cooling-off period expires on weekend | System processes deletion regardless of day/time. The 72-hour period is strict. |
| Super admin views audit log of suspended tenant | Allowed. Audit logs are independent of tenant status. |
| Tenant DB backup fails | Alert super admin. Retry once after 30 minutes. If second attempt fails, flag tenant for manual backup. Daily backup job continues for other tenants. |
| Audit log table grows very large | Partitioned by month. Partitions older than retention period dropped by automated job. Archived partitions remain queryable via partition-aware queries. |
| Data export file exceeds 1GB | Split into multiple ZIP files (max 500MB each). Download response returns manifest with links to each part. |
| Deletion request followed by re-signup with same email | Allowed after deletion completes. New tenant created fresh. No data restoration from previous tenant. Old soft-deleted records remain with original tenant_id. |
| TOTP secret decryption fails after key rotation | Re-encryption happens during key rotation process. Grace period: old key accepted for 24 hours after rotation. If both keys fail, user must re-enroll 2FA via recovery code. |
| Scan worker attempts to write to wrong tenant DB | Impossible by architecture — worker creates DbContext from tenant_id in job payload, resolved via control DB. Invalid tenant_id -> worker fails the job. |
| Migration backup storage fills up | Alert super admin. Block new migrations until storage freed. Do NOT skip backups. Existing scans continue normally. |
| Purge job finds zero records to delete | Normal case for tenants with short history. Log info and continue. No error. |
| Soft-deleted tenant record queried by audit log join | Audit log retains tenant_id reference. Join returns soft-deleted tenant data (with deleted_at set). Display in admin UI shows "[Deleted] {tenant_name}". |
| Two export requests from same tenant in quick succession | Second request rejected with ERR_DATA_001. Only one export per tenant at a time. |

---

### 10.10 Error Codes

| Code | HTTP | Message | Cause |
|------|------|---------|-------|
| `ERR_DATA_001` | 400 | Data export already in progress. | Tenant already has a pending or processing export job. |
| `ERR_DATA_002` | 404 | Data export not found or expired. | Download URL expired (24h) or export doesn't exist. |
| `ERR_DATA_003` | 400 | Data deletion request already pending. | Tenant already has a pending deletion request. |
| `ERR_DATA_004` | 400 | Invalid confirmation phrase. Expected "DELETE {slug}". | Deletion confirmation doesn't match expected format. |
| `ERR_DATA_005` | 403 | Re-authentication required for data deletion. | Tenant must re-verify password + 2FA before deletion proceeds. |
| `ERR_DATA_006` | 400 | Deletion cooling-off period not elapsed. | Attempted to force-complete deletion before 72 hours. |
| `ERR_DATA_007` | 400 | Cannot cancel deletion request. Already processing or completed. | Attempted to cancel after 72-hour period expired or deletion already executed. |
| `ERR_DATA_008` | 500 | Tenant database backup failed. | Infrastructure error during backup creation. |
| `ERR_DATA_009` | 500 | Data integrity check failed after migration. | Post-migration verification detected issues. |
| `ERR_DATA_010` | 403 | Audit logs are immutable. Cannot modify or delete. | Attempted to update or delete audit log entries. |

> **Cross-references:** Migration versioning rules (ordering, rollback, approval) in Section 13 (BR-VER-008 through BR-VER-013). Data retention config values in Section 11 (system_config reference). Backup encryption uses the same master key defined in Section 10.3 (BR-DATA-008).
