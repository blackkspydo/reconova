# 13. Version History & Versioning Strategy

> Covers: Full versioning strategy for the Reconova platform — document changelog, API versioning, database migration versioning, compliance framework versioning, and workflow template versioning.
> Error response JSON schema is defined in `00-index.md` Section 12 reference.

---

### 13.1 Document Version History

> Covers: BR-VER-001, BR-VER-002

#### Rules

**BR-VER-001: Document version format** — This business rules document uses semantic versioning: `MAJOR.MINOR`. MAJOR increments when sections are added/removed or rules fundamentally change. MINOR increments for corrections, clarifications, or edge case additions.

**BR-VER-002: Change tracking** — Every document update must include a row in the version history table below: version, date, author, sections affected, and summary.

#### Version History Table

| Version | Date | Author | Sections Affected | Summary |
|---------|------|--------|-------------------|---------|
| 1.0 | 2026-03-01 | System | All | Initial business rules document (135 rules across 10 domains) |
| 2.0 | 2026-03-01 | System | 1-5 | Restructured into modular section files. Sections 1-5 fully expanded with field constraints, algorithms, edge cases, and error codes. |
| 2.1 | 2026-03-07 | System | 13 | Added Section 13: Version History & Versioning Strategy (BR-VER-001 through BR-VER-023) |

---

### 13.2 API Versioning

> Covers: BR-VER-003 through BR-VER-007

Internal URL path versioning between SvelteKit frontend and .NET backend. No external API consumers currently — tenants use the platform directly via the SvelteKit frontend.

#### Rules

**BR-VER-003: URL path versioning** — All API routes follow `/api/v{N}/{resource}` pattern (e.g., `/api/v1/scans`). Version number is a positive integer, starting at 1.

**BR-VER-004: Version lifecycle states** — Each API version has a lifecycle: `CURRENT` -> `DEPRECATED` -> `SUNSET`. Only one version can be `CURRENT` at a time. Multiple versions can be `DEPRECATED` simultaneously.

**BR-VER-005: Deprecation policy** — When a new API version is introduced: old version moves to `DEPRECATED`. Deprecated versions continue working but return a `Deprecation` response header with the sunset date. Minimum deprecation period: 6 months before sunset.

**BR-VER-006: Sunset behavior** — After sunset date, requests to the old version return `410 Gone` with a body directing to the current version. All sunset actions audit-logged.

**BR-VER-007: Breaking change definition** — A breaking change is any modification that would cause existing frontend requests to fail: removing fields from responses, changing field types, removing endpoints, changing authentication requirements, altering error code semantics. Non-breaking changes (adding new optional fields, new endpoints) do NOT require a new version.

#### Field Constraints

##### `api_versions` table (control DB)

| Field | Type | Constraints |
|-------|------|-------------|
| `id` | uuid | PK. Immutable. |
| `version` | integer | NOT NULL. Unique. Positive integer. |
| `status` | string | NOT NULL. CHECK (`CURRENT`, `DEPRECATED`, `SUNSET`). |
| `released_at` | timestamp | NOT NULL. When this version became CURRENT. |
| `deprecated_at` | timestamp | NULL. Set when status moves to DEPRECATED. |
| `sunset_at` | timestamp | NULL. Set when status moves to SUNSET. |
| `changelog` | text | NOT NULL. Summary of changes from previous version. |

#### API Version Lifecycle Algorithm

```
BR-VER-004A: Publish New API Version
-------------------------------------
Input: new_version_number, changelog, admin_user_id

1. VALIDATE admin_user_id has role SUPER_ADMIN
   IF NOT -> REJECT "ERR_VER_011"
2. VALIDATE new_version_number = current_max_version + 1
   IF NOT -> REJECT "ERR_VER_012"
3. UPDATE api_versions SET status = 'DEPRECATED', deprecated_at = NOW()
   WHERE status = 'CURRENT'
4. INSERT api_versions {
     version: new_version_number, status: 'CURRENT',
     released_at: NOW(), changelog
   }
5. AUDIT_LOG("api.version_published", {
     new_version: new_version_number, admin_user_id
   })
6. RETURN new version record
```

```
BR-VER-006A: Sunset API Version
--------------------------------
Input: version_number, admin_user_id

1. VALIDATE admin_user_id has role SUPER_ADMIN
   IF NOT -> REJECT "ERR_VER_011"
2. LOAD api_version by version_number
   IF NOT found -> REJECT "ERR_VER_013"
3. IF api_version.status != 'DEPRECATED' -> REJECT "ERR_VER_014"
4. IF (NOW() - api_version.deprecated_at) < 6 months -> REJECT "ERR_VER_015"
5. UPDATE api_versions SET status = 'SUNSET', sunset_at = NOW()
   WHERE version = version_number
6. AUDIT_LOG("api.version_sunset", {
     version: version_number, admin_user_id
   })
7. RETURN updated version record
```

---

### 13.3 Database Migration Versioning

> Covers: BR-VER-008 through BR-VER-013

Strict rules for the database-per-tenant migration system. Builds on the PRD (section 4.5) with formal safety rules.

#### Rules

**BR-VER-008: Mandatory rollback scripts** — Every migration (base or tenant-specific) MUST include both `up_script` and `down_script` in `migration_scripts`. A migration without a rollback script cannot be registered. The `down_script` must be tested against a staging tenant DB before being accepted.

**BR-VER-009: Pre-migration validation** — Before applying any migration to a tenant DB:

```
BR-VER-009A: Pre-Migration Validation
--------------------------------------
Input: tenant_id, migration_id

1. VERIFY tenant DB is accessible (connection check)
   IF NOT -> SKIP tenant, LOG("migration.tenant_unreachable", { tenant_id, migration_id })
2. VERIFY current schema version matches expected pre-migration state
   IF NOT -> SKIP tenant, LOG("migration.version_mismatch", { tenant_id, expected, actual })
3. VERIFY no other migration is in-progress for this tenant
   CHECK tenant_migrations WHERE tenant_id = tenant_id AND status = 'IN_PROGRESS'
   IF found -> SKIP tenant, LOG("migration.locked", { tenant_id, blocking_migration_id })
4. IF any check fails -> skip tenant, log error, continue to next tenant
   Do NOT block the migration batch.
5. ALL checks pass -> PROCEED with migration
```

**BR-VER-010: Migration ordering** — Migrations are applied in strict sequential order by version number. Gaps are not allowed (version 5 cannot be applied before version 4). If a tenant is behind multiple versions, apply them sequentially, committing each before starting the next.

**BR-VER-011: Per-tenant failure isolation** — If a migration fails on one tenant:

```
BR-VER-011A: Migration Failure Handling
----------------------------------------
Input: tenant_id, migration_id, error

1. EXECUTE down_script to rollback the failed migration
   IF rollback succeeds:
     SET tenant_migrations.status = 'FAILED' with error details
   IF rollback itself fails:
     SET tenant_migrations.status = 'FAILED' with error + rollback_error
     CREATE critical super admin alert: "migration.rollback_failed"
     FLAG tenant DB for manual intervention
     Do NOT retry automatically
2. CONTINUE applying the migration to remaining tenants
3. CREATE super admin alert: "migration.tenant_failed" {
     tenant_id, migration_id, error, rollback_status
   }
4. Tenant status remains ACTIVE — migration failure does NOT suspend the tenant
   The tenant operates on the previous schema version
```

**BR-VER-012: Breaking change review process** — Migrations that alter or drop columns, change constraints, or modify indexes are classified as `BREAKING`. Breaking migrations require:

```
BR-VER-012A: Breaking Migration Approval
-----------------------------------------
Input: migration_id, admin_user_id

1. VALIDATE migration.is_breaking = true
2. VALIDATE admin_user_id has role SUPER_ADMIN
   IF NOT -> REJECT "ERR_VER_005"
3. CHECK staging execution record exists for this migration
   IF NOT -> REJECT "ERR_VER_006"
4. CHECK staging execution completed at least 24 hours ago
   IF NOT -> REJECT "24-hour hold period not elapsed. Earliest apply: {staging_completed_at + 24h}"
5. SET migration.approved_by = admin_user_id, approved_at = NOW()
6. AUDIT_LOG("migration.breaking_approved", {
     migration_id, admin_user_id, staging_completed_at
   })
7. Migration is now eligible for production rollout
```

**BR-VER-013: Migration audit logging** — Every migration attempt is logged in `tenant_migrations` with: tenant_id, version, type, status, applied_by (super admin or system), applied_at, error_details. Rollbacks are logged as separate rows with status `ROLLED_BACK`.

#### Additional Fields

##### `base_migrations` table (additions)

| Field | Type | Constraints |
|-------|------|-------------|
| `is_breaking` | boolean | NOT NULL. Default `false`. Set to `true` for migrations that alter/drop columns, change constraints, or modify indexes. |
| `approved_by` | uuid | NULL. FK -> `users.id`. Required when `is_breaking = true`. Must be SUPER_ADMIN. |
| `approved_at` | timestamp | NULL. Required when `is_breaking = true`. |
| `staging_completed_at` | timestamp | NULL. When the migration was successfully applied to the staging tenant DB. |

##### `tenant_migrations` status values

| Status | Meaning |
|--------|---------|
| `IN_PROGRESS` | Migration currently executing. Acts as a lock — no other migration can start for this tenant. |
| `APPLIED` | Migration succeeded. |
| `ROLLED_BACK` | Migration was rolled back (manual or auto on failure). |
| `FAILED` | Migration failed and rollback attempted. Needs manual review if rollback also failed. |

---

### 13.4 Compliance Framework Versioning

> Covers: BR-VER-014 through BR-VER-018

Forced migration with grace period when compliance frameworks are updated.

#### Rules

**BR-VER-014: Framework version format** — Each compliance framework has a `version` field (string, e.g., "2023", "2024-r1"). When a super admin updates a framework's controls, a new version is created — the old version is NOT edited in-place.

**BR-VER-015: Framework version lifecycle** — States: `DRAFT` -> `ACTIVE` -> `DEPRECATED` -> `SUNSET`.

| State | Meaning |
|-------|---------|
| `DRAFT` | Being prepared by super admin. Not visible to tenants. |
| `ACTIVE` | Available for tenant selection and new assessments. |
| `DEPRECATED` | Grace period. Visible to tenants with a banner: "This framework version will be retired on {sunset_date}. New version available." New tenant selections blocked — only the new version can be selected. Existing selections continue working. |
| `SUNSET` | No longer usable. All tenant selections auto-migrated to the new version. Old assessments preserved read-only. |

**BR-VER-016: Grace period enforcement** — When a new framework version is published:

```
BR-VER-016A: Framework Deprecation Flow
----------------------------------------
Input: new_framework_id, admin_user_id

1. LOAD new framework. VALIDATE status = 'DRAFT'
2. LOAD old framework (via new_framework.previous_version_id)
   IF old framework exists AND status = 'ACTIVE':
     a. SET old framework status = 'DEPRECATED', deprecated_at = NOW()
     b. SET old framework sunset_date = NOW() + grace_period_days
     c. NOTIFY all tenants with active selections for old framework:
        "Framework {name} updated to {new_version}. Your assessments will migrate on {sunset_date}."
     d. SCHEDULE reminder notifications at: 60 days, 30 days, 7 days, 1 day before sunset
3. SET new framework status = 'ACTIVE'
4. AUDIT_LOG("compliance.framework_version_published", {
     new_framework_id, old_framework_id, grace_period_days, sunset_date, admin_user_id
   })
5. RETURN updated frameworks
```

```
BR-VER-016B: Framework Sunset Execution
----------------------------------------
Trigger: sunset_date reached for a DEPRECATED framework

1. LOAD all tenant_compliance_selections referencing the deprecated framework
2. LOAD new framework (the one whose previous_version_id = deprecated framework)
3. FOR EACH tenant selection:
   a. UPDATE tenant_compliance_selections SET framework_id = new_framework.id
   b. LOG("compliance.tenant_migrated", { tenant_id, old_framework_id, new_framework_id })
4. SET deprecated framework status = 'SUNSET'
5. AUDIT_LOG("compliance.framework_sunset", {
     framework_id, tenants_migrated: count
   })
```

**BR-VER-017: Assessment preservation** — Existing compliance assessments (`compliance_assessments` + `control_results`) are NEVER modified or deleted during framework version migration. They retain a reference to the framework version they were generated against. Historical reports remain downloadable and accurate to their point-in-time state.

**BR-VER-018: Control mapping migration** — When migrating to a new framework version:

```
BR-VER-018A: Control Mapping During Version Creation
-----------------------------------------------------
Input: new_framework_id, control_mappings[], admin_user_id

1. Super admin defines mapping during new version creation:
   control_mappings = [
     { old_control_id, new_control_id },  // mapped
     { old_control_id, null },            // retired (no equivalent in new version)
   ]
2. New controls with no old equivalent -> shown as "not yet assessed"
3. STORE mappings in framework_control_mappings table
4. On next scan after migration:
   - Assessments generated against new version's controls
   - Unmapped old controls -> flagged as "retired" in historical reports
   - New controls with no history -> shown as "not yet assessed" with no score impact
```

#### Additional Fields

##### `compliance_frameworks` table (additions)

| Field | Type | Constraints |
|-------|------|-------------|
| `previous_version_id` | uuid | NULL. FK -> `compliance_frameworks.id`. Links to the version this one supersedes. |
| `grace_period_days` | integer | NOT NULL. Default 90. Min 30. Days between DEPRECATED and SUNSET. |
| `sunset_date` | timestamp | NULL. Calculated: `deprecated_at + grace_period_days`. Set when status moves to DEPRECATED. |
| `deprecated_at` | timestamp | NULL. Set when status moves to DEPRECATED. |

##### New table: `framework_control_mappings` (control DB)

| Field | Type | Constraints |
|-------|------|-------------|
| `id` | uuid | PK. Immutable. |
| `old_framework_id` | uuid | NOT NULL. FK -> `compliance_frameworks.id`. |
| `new_framework_id` | uuid | NOT NULL. FK -> `compliance_frameworks.id`. |
| `old_control_id` | uuid | NOT NULL. FK -> `compliance_controls.id`. |
| `new_control_id` | uuid | NULL. FK -> `compliance_controls.id`. NULL means old control is retired with no equivalent. |
| `created_by` | uuid | NOT NULL. FK -> `users.id`. Must be SUPER_ADMIN. |
| `created_at` | timestamp | NOT NULL. Auto-set on insert. |

---

### 13.5 Workflow Template Versioning

> Covers: BR-VER-019 through BR-VER-023

Versioned system templates with opt-in updates for tenant-cloned workflows.

#### Rules

**BR-VER-019: Template version tracking** — System workflow templates (`workflow_templates` where `is_system = true`) have a `version` integer field, starting at 1. Each time a super admin modifies a system template, the version increments. The previous version's `steps_json` is preserved in a `workflow_template_versions` history table.

**BR-VER-020: System template auto-update** — When a super admin publishes a new system template version:

```
BR-VER-020A: Publish System Template Version
----------------------------------------------
Input: template_id, new_steps_json, changelog, admin_user_id

1. VALIDATE admin_user_id has role SUPER_ADMIN
   IF NOT -> REJECT "ERR_VER_011"
2. LOAD template. VALIDATE is_system = true
   IF NOT -> REJECT "Cannot version non-system templates"
3. INSERT workflow_template_versions {
     template_id, version: template.version, steps_json: template.steps_json,
     changelog, published_by: admin_user_id, published_at: NOW()
   }
4. UPDATE workflow_templates SET
     steps_json = new_steps_json, version = version + 1
   WHERE id = template_id
5. IDENTIFY all tenant workflows where source_template_id = template_id
6. FOR EACH cloned workflow:
   NOTIFY tenant: "workflow.template_updated" {
     template_name, new_version: template.version,
     tenant_workflow_id, diff_available: true
   }
7. AUDIT_LOG("workflow.template_version_published", {
     template_id, new_version: template.version, changelog, admin_user_id,
     tenants_notified: count
   })
8. Tenants using unmodified system template (not cloned) automatically
   get the new version on their next scan. No action needed.
```

**BR-VER-021: Cloned workflow update notification** — When a system template is updated, all tenants who cloned that template receive a notification of type `workflow.template_updated` with template name, new version, and a link to the diff view.

**BR-VER-022: Opt-in update for cloned workflows** — Tenants can view a diff between their cloned workflow and the latest system template version:

```
BR-VER-022A: Accept Workflow Template Update
----------------------------------------------
Input: tenant_workflow_id, tenant_user_id

1. LOAD tenant workflow
2. VALIDATE workflow.source_template_id is NOT NULL
   IF NULL -> REJECT "ERR_VER_009" (not a cloned workflow)
3. LOAD latest system template version
4. IF workflow.source_template_version = template.version
   -> REJECT "ERR_VER_009" (already up to date)
5. UPDATE tenant workflow SET
     steps_json = template.steps_json,
     source_template_version = template.version
6. AUDIT_LOG("workflow.update_accepted", {
     tenant_workflow_id, old_version: previous_source_version,
     new_version: template.version, tenant_user_id
   })
7. RETURN updated workflow
```

Options for the tenant:
1. **Accept update** — Replace cloned workflow's `steps_json` with the new template version. Preserves tenant's custom name and ID.
2. **Dismiss** — Keep current version. Notification is dismissed. Tenant can still manually update later via the diff view.

> Cherry-pick (selectively applying individual step changes) is a future enhancement. V1 supports accept/dismiss only.

**BR-VER-023: Clone version pinning** — Each tenant workflow cloned from a system template stores `source_template_id` and `source_template_version` (the version at the time of cloning). This enables the diff view and "update available" indicator.

#### New Tables & Fields

##### New table: `workflow_template_versions` (control DB)

| Field | Type | Constraints |
|-------|------|-------------|
| `id` | uuid | PK. Immutable. |
| `template_id` | uuid | NOT NULL. FK -> `workflow_templates.id`. |
| `version` | integer | NOT NULL. The version number this row captures. |
| `steps_json` | jsonb | NOT NULL. The steps configuration at this version. |
| `changelog` | text | NOT NULL. Super admin description of what changed. |
| `published_by` | uuid | NOT NULL. FK -> `users.id`. Must be SUPER_ADMIN. |
| `published_at` | timestamp | NOT NULL. Auto-set on insert. |
| | | Composite unique: `(template_id, version)`. |

##### `workflow_templates` table (additions)

| Field | Type | Constraints |
|-------|------|-------------|
| `version` | integer | NOT NULL. Default 1. Increments on each publish. |

##### Tenant `workflows` table (additions)

| Field | Type | Constraints |
|-------|------|-------------|
| `source_template_id` | uuid | NULL. FK -> `workflow_templates.id`. Set when workflow is cloned from a system template. NULL for from-scratch custom workflows. |
| `source_template_version` | integer | NULL. The system template version this workflow was cloned from (or last updated to). NULL if not cloned. |

---

### 13.6 Permissions Matrix

| Action | `TENANT_OWNER` | `SUPER_ADMIN` |
|--------|:--------------:|:-------------:|
| View document version history | N/A (internal doc) | N/A (internal doc) |
| View API version status | Yes (via response headers) | Yes (admin panel) |
| Create/deprecate/sunset API version | No | Yes |
| Apply database migration | No | Yes (or system auto) |
| Approve breaking migration | No | Yes |
| Rollback tenant migration | No | Yes |
| View migration status for own tenant | No | Yes |
| Create new compliance framework version | No | Yes |
| Set grace period for framework deprecation | No | Yes |
| View framework deprecation notice | Yes (own tenant) | Yes (all tenants) |
| Update system workflow template | No | Yes |
| View workflow template update notification | Yes (own tenant) | Yes (all tenants) |
| Accept/dismiss cloned workflow update | Yes | Yes (via impersonation) |
| View workflow version diff | Yes (own workflows) | Yes (all tenants) |

---

### 13.7 Edge Cases

| Scenario | Behavior |
|----------|----------|
| Migration fails on all tenants | All tenants individually rolled back. Super admin alerted with batch failure report. Migration marked as `FAILED` globally. |
| Migration in-progress when tenant is suspended | Migration completes (or fails/rolls back). Suspension doesn't interrupt an active migration. |
| Two breaking migrations queued | Applied sequentially. Second migration cannot start until first is fully propagated across all tenants. |
| Compliance framework deprecated with 0 tenants using it | Skip notification. Move directly to SUNSET after grace period (or immediately if super admin chooses). |
| Tenant clones workflow, then original template is deleted | Clone is unaffected. `source_template_id` becomes a dangling reference. "Update available" indicator never shows. Clone operates independently. |
| Tenant accepts workflow update, then wants to revert | No built-in revert. Tenant can manually re-edit their workflow. The previous `steps_json` is not preserved in tenant DB (only system template versions are tracked). |
| API version sunset while tenant has active scan | Active scans complete normally (they don't use versioned API paths internally). Sunset only affects incoming HTTP requests. |
| Grace period for framework is changed after deprecation starts | New grace period takes effect. Sunset date recalculated. Tenants re-notified if date moved earlier. |
| Migration rollback script itself fails | Set status to `FAILED`. Create critical super admin alert. Tenant DB may be in inconsistent state — flag for manual intervention. Do NOT retry automatically. |
| Super admin publishes framework version with no control mappings | Allowed. All old controls treated as "retired". All new controls treated as "not yet assessed". Warning logged for admin review. |
| Tenant dismisses workflow update, then template is updated again | Tenant sees the latest version diff (cumulative changes since their pinned version). Only one "update available" notification active at a time per workflow. |
| API version 1 deprecated, version 2 current, version 3 published | Version 1 stays DEPRECATED (can be sunset if 6-month period elapsed). Version 2 moves to DEPRECATED. Version 3 becomes CURRENT. |
| Migration applied to tenant that was just created (fresh clone) | Fresh clone from template already has all base migrations applied. The migration system checks current schema version and skips already-applied versions. |

---

### 13.8 Error Codes

| Code | HTTP | Message | Cause |
|------|------|---------|-------|
| `ERR_VER_001` | 410 | API version {N} has been sunset. Use /api/v{current}/ instead. | Request to a sunset API version. |
| `ERR_VER_002` | 400 | Migration missing rollback script. | Attempted to register a migration without `down_script`. |
| `ERR_VER_003` | 409 | Migration already in progress for tenant {id}. | Another migration is locked (`IN_PROGRESS`) for this tenant. |
| `ERR_VER_004` | 400 | Migration version gap detected. Expected {N}, got {M}. | Attempted to apply a migration out of order. |
| `ERR_VER_005` | 403 | Breaking migration requires super admin approval. | Attempted to apply a breaking migration without `approved_by`. |
| `ERR_VER_006` | 400 | Breaking migration must be tested on staging first. | No staging execution record found for this breaking migration. |
| `ERR_VER_007` | 400 | Grace period must be at least 30 days. | Attempted to set compliance framework grace period below minimum. |
| `ERR_VER_008` | 409 | Framework already has an active version. Deprecate current first. | Attempted to publish a new framework version while the old one is still `ACTIVE`. |
| `ERR_VER_009` | 404 | No update available for this workflow. | Tenant attempted to accept a workflow update but source template hasn't changed since their last sync, or workflow is not a clone. |
| `ERR_VER_010` | 400 | Cannot sunset framework during grace period. Wait until {date}. | Attempted to force-sunset a framework before the grace period expires. |
| `ERR_VER_011` | 403 | Insufficient permissions. Super admin role required. | Caller does not have `SUPER_ADMIN` role for a versioning admin action. |
| `ERR_VER_012` | 400 | Invalid API version number. Must be sequential. | New API version number is not current_max + 1. |
| `ERR_VER_013` | 404 | API version not found. | Referenced API version number does not exist. |
| `ERR_VER_014` | 400 | Only deprecated API versions can be sunset. | Attempted to sunset an API version that is not in `DEPRECATED` state. |
| `ERR_VER_015` | 400 | Minimum deprecation period (6 months) not elapsed. | Attempted to sunset an API version before the 6-month deprecation window. |

> **Cross-references:** Database migration rules extend the PRD section 4.5 migration strategy. Compliance framework lifecycle references BR-ADM-008 (section 9). Workflow template management references section 4 scanning workflows.
