# 6. Compliance Engine

> Covers: BR-COMP-001 through BR-COMP-011 from the original business rules.
> Error response JSON schema is defined in `00-index.md` Section 12 reference.

---

### 6.1 Framework Lifecycle & States

> Covers: BR-COMP-002

#### Status Values

| Status | Meaning |
|--------|---------|
| `DRAFT` | Framework is being built by super admin. Visibility to tenants controlled by `is_public` flag. Controls and mappings can be added/edited freely. |
| `ACTIVE` | Framework is live and available to tenants (subject to tier gating). Controls and mappings can still be edited, but changes affect future assessments only. |
| `DEPRECATED` | Framework is sunset. Visible to tenants who previously selected it, but cannot be selected for new assessments. Existing assessment results preserved. |

#### Public Preview Flag

| Field | Type | Constraints |
|-------|------|-------------|
| `is_public` | boolean | NOT NULL. Default: `false`. Only applies when status = `DRAFT`. |

| `is_public` | Status | Tenant Visibility |
|:-----------:|--------|-------------------|
| `false` | `DRAFT` | Completely hidden from tenants. |
| `true` | `DRAFT` | Shown to tenants as "Coming Soon". Cannot be selected. No controls or details exposed — name and description only. |
| _(ignored)_ | `ACTIVE` | Fully visible and selectable (subject to tier gating). |
| _(ignored)_ | `DEPRECATED` | Visible to tenants with existing selections. Not selectable for new assessments. |

> `is_public` is only meaningful in `DRAFT` state. For `ACTIVE` and `DEPRECATED` frameworks, visibility is determined by the status itself.

#### State Transitions

| From | To | Trigger | Who | Side Effects |
|------|----|---------|-----|-------------|
| `DRAFT` | `ACTIVE` | Super admin publishes framework | `SUPER_ADMIN` | Framework becomes visible and selectable to tenants on eligible tiers. `is_public` no longer relevant. Changelog entry published. |
| `ACTIVE` | `DEPRECATED` | Super admin deprecates framework | `SUPER_ADMIN` | Tenants cannot select this framework for new assessments. Existing selections and results preserved. Active tenant selections remain but no new assessments generated. Changelog entry published. |
| `DEPRECATED` | `ACTIVE` | Super admin reactivates framework | `SUPER_ADMIN` | Framework becomes selectable again. Existing selections resume generating assessments. Changelog entry published. |
| `DRAFT` | _(deleted)_ | Super admin deletes draft | `SUPER_ADMIN` | Only DRAFT frameworks can be deleted. Permanent removal of framework, controls, and mappings. If `is_public = true`, reason required and changelog entry published. |

```
                ┌──────────┐
  Created ──────►  DRAFT   │  (is_public: true → "Coming Soon")
                └────┬─────┘
                     │ Publish
                     ▼
                ┌──────────┐
                │  ACTIVE  │◄─── Reactivate
                └────┬─────┘         ▲
                     │ Deprecate     │
                     ▼               │
                ┌──────────────┐     │
                │ DEPRECATED   ├─────┘
                └──────────────┘
```

> **ACTIVE and DEPRECATED frameworks cannot be deleted** — they may have associated assessments and control results in tenant databases. Only DRAFT frameworks (which have never been published) can be deleted.

---

### 6.2 Field Constraints

> Covers: BR-COMP-001, BR-COMP-002, BR-COMP-011

#### `compliance_frameworks` table (Control DB)

| Field | Type | Constraints |
|-------|------|-------------|
| `id` | uuid | PK. Immutable. |
| `name` | string(200) | NOT NULL. Unique. e.g., `SOC 2 Type II`, `NIST CSF 2.0`. |
| `version` | string(50) | NOT NULL. Framework version. e.g., `2024.1`. |
| `region` | string(100) | NULL. Geographic applicability. e.g., `Global`, `EU`, `US`. NULL = all regions. |
| `description` | string(2000) | NOT NULL. Human-readable description for tenant display. |
| `is_template` | boolean | NOT NULL. Default: `false`. If `true`, framework was seeded from a system template. Immutable after creation. |
| `is_public` | boolean | NOT NULL. Default: `false`. When `true` and status = `DRAFT`, framework shown to tenants as "Coming Soon" (name + description only). |
| `status` | string | NOT NULL. CHECK (`DRAFT`, `ACTIVE`, `DEPRECATED`). Default: `DRAFT`. |
| `created_by` | uuid | NOT NULL. FK → `users.id`. Must be `SUPER_ADMIN`. |
| `created_at` | timestamp | NOT NULL. Auto-set on insert. Immutable. |
| `updated_at` | timestamp | NOT NULL. Auto-set on insert and update. |

#### `compliance_controls` table (Control DB)

| Field | Type | Constraints |
|-------|------|-------------|
| `id` | uuid | PK. Immutable. |
| `framework_id` | uuid | NOT NULL. FK → `compliance_frameworks.id`. Immutable. |
| `control_id` | string(50) | NOT NULL. Framework-specific control identifier. e.g., `CC6.1`, `PR.AC-3`. Unique within framework: composite unique `(framework_id, control_id)`. |
| `title` | string(300) | NOT NULL. Human-readable control title. |
| `description` | string(2000) | NOT NULL. Full control description. |
| `category` | string(100) | NOT NULL. Grouping within framework. e.g., `Access Control`, `Data Protection`. |
| `min_security_recommendations_json` | jsonb | NOT NULL. Default: `[]`. Baseline security recommendations shown even when control passes. Array of `{ "recommendation": string, "priority": "HIGH" | "MEDIUM" | "LOW" }`. |
| `created_at` | timestamp | NOT NULL. Auto-set on insert. Immutable. |
| `updated_at` | timestamp | NOT NULL. Auto-set on insert and update. |

#### `control_check_mappings` table (Control DB)

| Field | Type | Constraints |
|-------|------|-------------|
| `id` | uuid | PK. Immutable. |
| `control_id` | uuid | NOT NULL. FK → `compliance_controls.id`. |
| `check_type` | string(50) | NOT NULL. Scan step check type that provides evidence. e.g., `port_scan`, `vuln_scan`, `tech_detect`. Must be a valid check type from §4.9. |
| `severity_threshold` | string | NULL. CHECK (`CRITICAL`, `HIGH`, `MEDIUM`, `LOW`). Minimum severity of findings that trigger a fail. NULL = any finding triggers fail. |
| `pass_condition_json` | jsonb | NOT NULL. Defines pass/fail criteria. e.g., `{ "condition": "no_findings_above_threshold" }` or `{ "condition": "specific_check", "check": "tls_enabled", "expected": true }`. |
| `recommendation_json` | jsonb | NOT NULL. Default: `{}`. Remediation guidance shown when this mapping results in a fail. `{ "title": string, "description": string, "references": [string] }`. |
| `created_at` | timestamp | NOT NULL. Auto-set on insert. Immutable. |

#### `plan_compliance_access` table (Control DB)

| Field | Type | Constraints |
|-------|------|-------------|
| `id` | uuid | PK. Immutable. |
| `plan_id` | uuid | NOT NULL. FK → `subscription_plans.id`. |
| `framework_id` | uuid | NOT NULL. FK → `compliance_frameworks.id`. |
| `enabled` | boolean | NOT NULL. Whether this framework is available on this plan. |
| | | Composite unique: `(plan_id, framework_id)`. |

#### `tenant_compliance_selections` table (Tenant DB)

| Field | Type | Constraints |
|-------|------|-------------|
| `id` | uuid | PK. Immutable. |
| `framework_id` | uuid | NOT NULL. References `compliance_frameworks.id` in control DB. |
| `enabled_at` | timestamp | NOT NULL. When the tenant selected this framework. |
| `disabled_at` | timestamp | NULL. When the tenant deselected. NULL = currently selected. |

#### `compliance_assessments` table (Tenant DB)

| Field | Type | Constraints |
|-------|------|-------------|
| `id` | uuid | PK. Immutable. |
| `scan_job_id` | uuid | NOT NULL. FK → `scan_jobs.id`. Immutable. One assessment per (scan_job, framework) pair. |
| `framework_id` | uuid | NOT NULL. References `compliance_frameworks.id` in control DB. Immutable. |
| `overall_score` | decimal(5,2) | NOT NULL. 0.00 to 100.00. Calculated per BR-COMP-006. |
| `total_controls` | integer | NOT NULL. Total controls in framework at time of assessment. |
| `assessed_controls` | integer | NOT NULL. Controls with at least one check mapping that had scan data. |
| `passing_controls` | integer | NOT NULL. Controls with status = `PASS`. |
| `generated_at` | timestamp | NOT NULL. Auto-set on insert. Immutable. |
| | | Composite unique: `(scan_job_id, framework_id)`. |

#### `control_results` table (Tenant DB)

| Field | Type | Constraints |
|-------|------|-------------|
| `id` | uuid | PK. Immutable. |
| `assessment_id` | uuid | NOT NULL. FK → `compliance_assessments.id`. Immutable. |
| `control_id` | uuid | NOT NULL. References `compliance_controls.id` in control DB. Immutable. |
| `status` | string | NOT NULL. CHECK (`PASS`, `FAIL`, `PARTIAL`, `NOT_ASSESSED`). |
| `evidence_json` | jsonb | NOT NULL. Default: `{}`. Scan findings that support the status determination. `{ "findings": [{ "check_type": string, "result": string, "details": any }] }`. |
| `findings_count` | integer | NOT NULL. Default: `0`. Number of findings that contributed to the status. |
| `recommendations` | jsonb | NOT NULL. Default: `[]`. Aggregated remediation recommendations from failing check mappings + `min_security_recommendations_json`. |
| | | Composite unique: `(assessment_id, control_id)`. |

#### `framework_requests` table (Control DB)

| Field | Type | Constraints |
|-------|------|-------------|
| `id` | uuid | PK. Immutable. |
| `tenant_id` | uuid | NOT NULL. FK → `tenants.id`. |
| `requested_by` | uuid | NOT NULL. FK → `users.id`. The tenant owner who submitted the request. |
| `framework_name` | string(200) | NOT NULL. Name of the requested framework. |
| `description` | string(2000) | NOT NULL. Why the tenant needs this framework, specific controls of interest. |
| `status` | string | NOT NULL. CHECK (`SUBMITTED`, `REVIEWED`, `ACCEPTED`, `REJECTED`). Default: `SUBMITTED`. |
| `admin_notes` | string(2000) | NULL. Super admin response/notes. Required when status changes to `REVIEWED`, `ACCEPTED`, or `REJECTED`. |
| `reviewed_by` | uuid | NULL. FK → `users.id`. Super admin who reviewed. Set on first status change from `SUBMITTED`. |
| `created_at` | timestamp | NOT NULL. Auto-set on insert. Immutable. |
| `updated_at` | timestamp | NOT NULL. Auto-set on update. |

---

### 6.3 Framework Management

> Covers: BR-COMP-001, BR-COMP-002

#### Framework CRUD Algorithms

```
BR-COMP-002A: Create Compliance Framework
──────────────────────────────────────────
Input: name, version, region, description, is_public, admin_user_id

1. VALIDATE admin_user_id has role SUPER_ADMIN
   IF NOT → REJECT "ERR_COMP_001"
2. CHECK name uniqueness (case-insensitive)
   IF duplicate → REJECT "ERR_COMP_002"
3. INSERT compliance_frameworks {
     name, version, region, description, is_template: false,
     is_public, status: DRAFT, created_by: admin_user_id
   }
4. AUDIT_LOG("compliance.framework_created", {
     framework_id, name, admin_user_id
   })
5. RETURN framework record
```

```
BR-COMP-002B: Publish Framework (DRAFT → ACTIVE)
─────────────────────────────────────────────────
Input: framework_id, admin_user_id

1. VALIDATE admin_user_id has role SUPER_ADMIN
   IF NOT → REJECT "ERR_COMP_001"
2. LOAD framework by framework_id
   IF NOT found → REJECT "ERR_COMP_003"
3. IF status ≠ DRAFT → REJECT "ERR_COMP_004"
4. CHECK framework has at least 1 control with at least 1 check mapping
   IF NOT → REJECT "ERR_COMP_005"
5. CHECK plan_compliance_access has at least 1 plan with enabled = true
   IF NOT → REJECT "ERR_COMP_006"
6. UPDATE status = ACTIVE
7. AUDIT_LOG("compliance.framework_published", {
     framework_id, name, controls_count, admin_user_id
   })
8. PUBLISH changelog entry: "New compliance framework available: {name}"
9. RETURN updated framework
```

```
BR-COMP-002C: Deprecate Framework (ACTIVE → DEPRECATED)
────────────────────────────────────────────────────────
Input: framework_id, admin_user_id

1. VALIDATE admin_user_id has role SUPER_ADMIN
   IF NOT → REJECT "ERR_COMP_001"
2. LOAD framework by framework_id
   IF NOT found → REJECT "ERR_COMP_003"
3. IF status ≠ ACTIVE → REJECT "ERR_COMP_007"
4. UPDATE status = DEPRECATED
5. AUDIT_LOG("compliance.framework_deprecated", {
     framework_id, name, active_tenant_selections_count, admin_user_id
   })
6. PUBLISH changelog entry: "Compliance framework sunset: {name} — no longer available for new assessments"
7. RETURN updated framework
```

```
BR-COMP-002D: Delete Draft Framework
─────────────────────────────────────
Input: framework_id, reason (optional unless is_public), admin_user_id

1. VALIDATE admin_user_id has role SUPER_ADMIN
   IF NOT → REJECT "ERR_COMP_001"
2. LOAD framework by framework_id
   IF NOT found → REJECT "ERR_COMP_003"
3. IF status ≠ DRAFT → REJECT "ERR_COMP_008"
4. IF is_public = true:
   a. IF reason is blank or < 10 characters → REJECT "ERR_COMP_022"
   b. PUBLISH changelog entry: "Coming Soon framework removed: {name} — {reason}"
5. DELETE all control_check_mappings for framework's controls
6. DELETE all compliance_controls for framework
7. DELETE all plan_compliance_access for framework
8. DELETE framework
9. AUDIT_LOG("compliance.framework_deleted", {
     framework_id, name, was_public: is_public, reason, admin_user_id
   })
10. RETURN success
```

#### Reactivate Framework

```
BR-COMP-002E: Reactivate Framework (DEPRECATED → ACTIVE)
─────────────────────────────────────────────────────────
Input: framework_id, admin_user_id

1. VALIDATE admin_user_id has role SUPER_ADMIN
   IF NOT → REJECT "ERR_COMP_001"
2. LOAD framework by framework_id
   IF NOT found → REJECT "ERR_COMP_003"
3. IF status ≠ DEPRECATED → REJECT "ERR_COMP_007"
4. UPDATE status = ACTIVE
5. AUDIT_LOG("compliance.framework_reactivated", {
     framework_id, name, admin_user_id
   })
6. PUBLISH changelog entry: "Compliance framework restored: {name}"
7. RETURN updated framework
```

#### Tier Gating via `plan_compliance_access`

Super admin configures which plans can access each framework:

```
BR-COMP-001: Set Framework Tier Access
──────────────────────────────────────
Input: framework_id, plan_id, enabled, admin_user_id

1. VALIDATE admin_user_id has role SUPER_ADMIN
   IF NOT → REJECT "ERR_COMP_001"
2. LOAD framework by framework_id
   IF NOT found → REJECT "ERR_COMP_003"
3. UPSERT plan_compliance_access { plan_id, framework_id, enabled }
4. AUDIT_LOG("compliance.tier_access_updated", {
     framework_id, plan_id, enabled, admin_user_id
   })
5. RETURN updated access record
```

> **Tenant framework listing** returns only ACTIVE frameworks where `plan_compliance_access.enabled = true` for the tenant's current plan, plus DRAFT frameworks where `is_public = true` (shown as "Coming Soon"). DEPRECATED frameworks are shown only if the tenant has an existing selection.

#### Seeded Frameworks

The system seeds two frameworks on initial deployment:

| Framework | Version | Controls | Tier Access |
|-----------|---------|----------|-------------|
| SOC 2 Type II | 2024.1 | Key controls from Trust Services Criteria (CC6, CC7, CC8) | Pro, Enterprise |
| NIST CSF 2.0 | 2.0 | Key controls from Identify, Protect, Detect, Respond, Recover | Pro, Enterprise |

Seeded frameworks have `is_template = true` and cannot be deleted (enforced at application level — template frameworks are always ACTIVE or DEPRECATED, never DRAFT after initial seed).

---

### 6.4 Control-to-Check Mappings

> Covers: BR-COMP-003, BR-COMP-005

Control-to-check mappings define how scan results are evaluated against compliance controls. Each mapping links a compliance control to a scan check type and defines pass/fail criteria.

#### Mapping Structure

```
ComplianceControl (e.g., "CC6.1 — Logical Access Security")
  └─ ControlCheckMapping #1: check_type = "port_scan", pass_condition = "no open high-risk ports"
  └─ ControlCheckMapping #2: check_type = "vuln_scan", pass_condition = "no findings above HIGH severity"
  └─ ControlCheckMapping #3: check_type = "tech_detect", pass_condition = "tls_enabled = true"
```

A single control can map to multiple check types. The control's overall status is derived from the aggregate of all its mapping results (see §6.6).

#### Pass Condition Types

| Condition | `pass_condition_json` | Meaning |
|-----------|----------------------|---------|
| No findings above threshold | `{ "condition": "no_findings_above_threshold" }` | Pass if no scan findings at or above `severity_threshold`. Uses the mapping's `severity_threshold` field. |
| Specific check | `{ "condition": "specific_check", "check": "tls_enabled", "expected": true }` | Pass if a specific boolean check in the scan results matches the expected value. |
| Finding count below limit | `{ "condition": "finding_count_below", "max": 0 }` | Pass if the number of findings of the given check type is at or below the max. |
| Custom expression | `{ "condition": "custom", "expression": "open_ports NOT IN (21, 23, 445)" }` | Pass if a custom expression evaluates to true against scan findings. Evaluated server-side. |

#### Example Mappings (SOC 2 Type II)

| Control | Check Type | Severity Threshold | Pass Condition | Recommendation |
|---------|-----------|-------------------|----------------|----------------|
| CC6.1 Logical Access | `vuln_scan` | `HIGH` | `no_findings_above_threshold` | Patch critical vulnerabilities, review access controls |
| CC6.1 Logical Access | `port_scan` | — | `{ "condition": "custom", "expression": "open_ports NOT IN (21, 23, 445)" }` | Close unnecessary ports, disable insecure protocols |
| CC7.1 Monitoring | `vuln_scan` | `CRITICAL` | `no_findings_above_threshold` | Implement continuous vulnerability monitoring |
| CC7.1 Monitoring | `tech_detect` | — | `{ "condition": "specific_check", "check": "waf_detected", "expected": true }` | Deploy WAF for web-facing assets |
| CC8.1 Change Mgmt | `tech_detect` | — | `{ "condition": "specific_check", "check": "version_current", "expected": true }` | Update outdated software components |

#### Mapping Management

```
BR-COMP-002F: Add Control Check Mapping
────────────────────────────────────────
Input: control_id, check_type, severity_threshold, pass_condition_json, recommendation_json, admin_user_id

1. VALIDATE admin_user_id has role SUPER_ADMIN
   IF NOT → REJECT "ERR_COMP_001"
2. LOAD control by control_id
   IF NOT found → REJECT "ERR_COMP_009"
3. VALIDATE check_type is a known scan check type (from §4.9 mapping)
   IF NOT valid → REJECT "ERR_COMP_010"
4. VALIDATE pass_condition_json has a supported condition type
   IF NOT valid → REJECT "ERR_COMP_011"
5. INSERT control_check_mappings {
     control_id, check_type, severity_threshold, pass_condition_json, recommendation_json
   }
6. RETURN mapping record
```

> **Mapping changes affect future assessments only.** Existing assessment results are immutable — they reflect the mappings that were in place at assessment time.

---

### 6.5 Assessment Execution Algorithm

> Covers: BR-COMP-003, BR-COMP-004, BR-COMP-006

#### Auto-Trigger

Compliance assessment is triggered automatically when a scan job reaches `COMPLETED` or `PARTIAL` status. No manual trigger exists.

```
BR-COMP-003: Compliance Assessment Execution
─────────────────────────────────────────────
Input: scan_job_id

Trigger: Scan job status changes to COMPLETED or PARTIAL

1. LOAD scan_job by scan_job_id
   LOAD tenant_id from scan_job

2. CHECK feature flag: compliance_checks for tenant
   IF NOT enabled → SKIP assessment entirely, RETURN
   (No assessment generated for tenants without compliance access)

3. LOAD tenant_compliance_selections where disabled_at IS NULL
   IF no active selections → SKIP, RETURN

4. FOR EACH selected framework:
   a. CHECK plan_compliance_access for (tenant.plan_id, framework_id)
      IF NOT enabled → SKIP this framework
      (Tenant may have selected it on a higher plan and since downgraded)

   b. CHECK framework status
      IF DEPRECATED → SKIP this framework
      (No new assessments for deprecated frameworks)

   c. LOAD all compliance_controls for framework
   d. LOAD all control_check_mappings for each control

   e. FOR EACH control:
      EVALUATE control using BR-COMP-005 logic (§6.6)
      CREATE control_result { assessment_id, control_id, status, evidence_json, findings_count, recommendations }

   f. CALCULATE overall_score using BR-COMP-006 logic:
      assessed_controls = controls where status ≠ NOT_ASSESSED
      passing_controls = controls where status = PASS
      overall_score = (passing_controls / assessed_controls) × 100
      IF assessed_controls = 0 → overall_score = 0.00

   g. INSERT compliance_assessments {
        scan_job_id, framework_id, overall_score,
        total_controls, assessed_controls, passing_controls
      }

5. LOG("compliance.assessment_completed", {
     scan_job_id, tenant_id, frameworks_assessed: count,
     scores: { framework_name: score, ... }
   })
```

#### Assessment Scope

| Rule | Behavior |
|------|----------|
| Single scan scope (BR-COMP-004) | Assessment evaluates only scan results from the triggering scan job. No aggregation across multiple scans. |
| One assessment per scan per framework | Composite unique `(scan_job_id, framework_id)` prevents duplicate assessments. |
| FAILED scans | No assessment generated. Only `COMPLETED` and `PARTIAL` scans trigger assessment. |
| CANCELLED scans | No assessment generated. |
| PARTIAL scans | Assessment runs against available results. Controls mapped to missing check types get `NOT_ASSESSED`. |

#### Score Calculation (BR-COMP-006)

```
overall_score = (passing_controls / assessed_controls) × 100

Where:
  assessed_controls = total_controls − not_assessed_controls
  passing_controls = controls with status = PASS

Special case:
  IF assessed_controls = 0 → overall_score = 0.00
```

Example:
- Framework has 20 controls
- 5 controls have `NOT_ASSESSED` (no scan data for their check types)
- 12 controls `PASS`, 3 controls `FAIL`
- assessed_controls = 20 − 5 = 15
- overall_score = (12 / 15) × 100 = **80.00**

---

### 6.6 Control Result Statuses

> Covers: BR-COMP-005

#### Status Determination Algorithm

Each control is evaluated by aggregating the results of all its check mappings against the scan results.

```
BR-COMP-005: Evaluate Control Status
─────────────────────────────────────
Input: control (with check_mappings[]), scan_results

1. SET mapping_results = []

2. FOR EACH mapping IN control.check_mappings:
   a. FIND scan_results for mapping.check_type
      IF no scan data for this check_type:
        ADD { mapping, result: NO_DATA } to mapping_results
        CONTINUE

   b. EVALUATE mapping.pass_condition_json against scan findings:
      IF condition met → ADD { mapping, result: PASS, evidence }
      IF condition NOT met → ADD { mapping, result: FAIL, evidence, findings }

3. DETERMINE control status from mapping_results:

   IF ALL mapping_results have result = NO_DATA:
     status = NOT_ASSESSED
     (No scan data available for any of this control's check types)

   ELSE IF ANY mapping_result has result = FAIL:
     status = FAIL
     (One or more criteria explicitly not met — strictest interpretation)

   ELSE IF ALL mapping_results with data have result = PASS AND at least one has NO_DATA:
     status = PARTIAL
     (Some criteria met, but incomplete assessment due to missing scan data)

   ELSE IF ALL mapping_results have result = PASS:
     status = PASS
     (All check criteria fully met)

4. COLLECT evidence_json from all mapping_results with data
5. COUNT findings from FAIL mapping_results → findings_count
6. AGGREGATE recommendations:
   - From FAIL mappings: include mapping.recommendation_json
   - Always include: control.min_security_recommendations_json (BR-COMP-007)

7. RETURN { status, evidence_json, findings_count, recommendations }
```

#### Status Summary

| Status | Condition | Included in Score Denominator? |
|--------|-----------|:------------------------------:|
| `PASS` | All check mappings with scan data evaluate to pass. No mappings have missing data. | Yes |
| `FAIL` | At least one check mapping explicitly fails. | Yes |
| `PARTIAL` | All mappings with data pass, but at least one mapping has no scan data. | Yes |
| `NOT_ASSESSED` | No scan data available for any of this control's check types. | No (excluded from denominator) |

#### Status Precedence

```
FAIL > PARTIAL > PASS > NOT_ASSESSED

Any FAIL → control is FAIL (regardless of other mapping results)
No FAIL + any NO_DATA → control is PARTIAL
No FAIL + no NO_DATA + all PASS → control is PASS
All NO_DATA → control is NOT_ASSESSED
```

> **Minimum security recommendations (BR-COMP-007):** The `recommendations` field on `control_results` always includes the control's `min_security_recommendations_json`, even when the control status is `PASS`. These are best-practice guidance items, not failure indicators.

---

### 6.7 Report Generation

> Covers: BR-COMP-008, BR-COMP-009

#### Tier Gating

Report generation requires the `compliance_reports` feature flag (Pro+ only). Starter and Free tenants cannot generate or download reports.

#### Report Formats

| Format | Content Type | Use Case |
|--------|-------------|----------|
| PDF | `application/pdf` | Formal compliance documentation, stakeholder distribution, audit evidence |
| HTML | `text/html` | Browser viewing, email embedding |

#### Report Content Structure

```
1. Executive Summary
   - Framework name and version
   - Assessment date
   - Domain assessed
   - Overall compliance score (percentage + pass/fail/partial/not_assessed counts)
   - Score trend indicator (↑ improved, ↓ declined, → stable) vs. previous assessment

2. Score Breakdown
   - By category (e.g., Access Control: 85%, Data Protection: 70%)
   - Visual: bar chart or table of category scores

3. Per-Control Results
   FOR EACH control (grouped by category):
   - Control ID and title
   - Status badge: PASS / FAIL / PARTIAL / NOT_ASSESSED
   - Evidence summary (from evidence_json)
   - Findings count
   - Remediation recommendations (from failing check mappings)
   - Minimum security recommendations (always shown, even on PASS)

4. Remediation Priority List
   - All FAIL controls sorted by category
   - Actionable steps from recommendation_json
   - References (links to standards documentation)

5. Historical Trend (if ≥ 3 assessments exist)
   - Score over time chart (last N assessments)
   - Per-category trend

6. Report Metadata
   - Generated at timestamp
   - Scan job ID reference
   - Tenant name and domain
   - Framework version used
```

#### Report Generation Algorithm

```
BR-COMP-008: Generate Compliance Report
────────────────────────────────────────
Input: assessment_id, format (PDF | HTML), user_id

1. CHECK feature flag: compliance_reports for tenant
   IF NOT enabled → REJECT "ERR_COMP_012"

2. LOAD assessment by assessment_id
   IF NOT found → REJECT "ERR_COMP_013"
   IF assessment.tenant_id ≠ user's tenant → REJECT "ERR_COMP_014"

3. LOAD framework from control DB
4. LOAD all control_results for assessment
5. LOAD historical assessments for same (tenant, framework, domain)
   ORDER BY generated_at DESC

6. BUILD report content using template (PDF via QuestPDF, HTML via Razor template)
7. RETURN generated report as downloadable file

   Content-Disposition: attachment; filename="{framework_name}_{domain}_{date}.{pdf|html}"
```

> **Reports are generated on-demand**, not pre-generated. No stored report artifacts — each download triggers generation from assessment data. This ensures reports always reflect the latest template formatting.

---

### 6.8 Historical Trends

> Covers: BR-COMP-010

#### Trend Tracking

The system tracks compliance scores over time per framework per domain. Trends are derived from `compliance_assessments` records — no separate trend table needed.

#### Trend Query

```
BR-COMP-010: Get Compliance Trend
─────────────────────────────────
Input: tenant_id, framework_id, domain_id

1. LOAD all compliance_assessments WHERE:
   - scan_job.tenant_id = tenant_id
   - framework_id = framework_id
   - scan_job.domain_id = domain_id
   ORDER BY generated_at ASC

2. IF count < 3 → RETURN { trend_available: false, message: "Minimum 3 assessments required" }

3. BUILD trend data:
   FOR EACH assessment:
     { date: generated_at, score: overall_score, 
       passing: passing_controls, assessed: assessed_controls }

4. CALCULATE trend direction (latest vs previous):
   IF latest.score > previous.score → direction = "improving"
   IF latest.score < previous.score → direction = "declining"
   IF latest.score = previous.score → direction = "stable"

5. RETURN {
     trend_available: true,
     direction,
     data_points: [...],
     first_assessment: earliest.generated_at,
     latest_assessment: latest.generated_at,
     score_change: latest.score - previous.score
   }
```

#### Trend Rules

| Rule | Behavior |
|------|----------|
| Minimum data points | Trend is not displayed until at least 3 assessments exist for the same (framework, domain) combination. |
| Scope | Per framework, per domain. A tenant with 5 domains and 2 frameworks has up to 10 independent trend lines. |
| Data source | Derived from `compliance_assessments` table. No denormalized trend storage. |
| Deprecated frameworks | Trend data preserved and viewable. No new data points added since assessments stop. |
| Domain removed | Trend data preserved in tenant DB. Domain removal does not delete historical assessments (§4.10 immutability). |

> **No trend aggregation across domains.** Each domain is assessed independently. A tenant-level "overall compliance posture" view could aggregate across domains, but the trend itself is always per-domain per-framework.

---

### 6.9 Framework Requests

> Covers: BR-COMP-011

#### Request Lifecycle

```
  SUBMITTED ──► REVIEWED ──► ACCEPTED
                    │
                    └────────► REJECTED
```

| Status | Meaning |
|--------|---------|
| `SUBMITTED` | Tenant owner submitted the request. Awaiting super admin review. |
| `REVIEWED` | Super admin has reviewed but not yet decided. Used for requests needing further investigation. |
| `ACCEPTED` | Request approved. Framework may be created in the future. No SLA on delivery. |
| `REJECTED` | Request denied with explanation in `admin_notes`. |

#### Request Algorithms

```
BR-COMP-011A: Submit Framework Request
──────────────────────────────────────
Input: framework_name, description, user_id

1. VALIDATE user_id has role TENANT_OWNER
   IF NOT → REJECT "ERR_COMP_015"
2. IF framework_name is blank or < 3 characters → REJECT "ERR_COMP_016"
3. IF description is blank or < 20 characters → REJECT "ERR_COMP_017"
4. CHECK existing requests from same tenant with same framework_name (case-insensitive)
   and status IN (SUBMITTED, REVIEWED)
   IF exists → REJECT "ERR_COMP_018"
5. INSERT framework_requests {
     tenant_id, requested_by: user_id, framework_name,
     description, status: SUBMITTED
   }
6. AUDIT_LOG("compliance.framework_requested", {
     tenant_id, framework_name, user_id
   })
7. RETURN request record
```

```
BR-COMP-011B: Review Framework Request
──────────────────────────────────────
Input: request_id, new_status, admin_notes, admin_user_id

1. VALIDATE admin_user_id has role SUPER_ADMIN
   IF NOT → REJECT "ERR_COMP_001"
2. LOAD request by request_id
   IF NOT found → REJECT "ERR_COMP_019"
3. VALIDATE state transition:
   - SUBMITTED → REVIEWED, ACCEPTED, REJECTED
   - REVIEWED → ACCEPTED, REJECTED
   - ACCEPTED → (terminal, no further transitions)
   - REJECTED → (terminal, no further transitions)
   IF invalid transition → REJECT "ERR_COMP_020"
4. IF admin_notes is blank or < 10 characters → REJECT "ERR_COMP_021"
5. UPDATE request: status = new_status, admin_notes, reviewed_by: admin_user_id
6. AUDIT_LOG("compliance.request_reviewed", {
     request_id, framework_name, new_status, admin_notes, admin_user_id
   })
7. RETURN updated request
```

#### Request Visibility

| Caller | What They See |
|--------|--------------|
| `TENANT_OWNER` | Own tenant's requests only. Sees: framework_name, description, status, admin_notes (when present), created_at. |
| `SUPER_ADMIN` | All requests across all tenants. Full details including tenant_id, requested_by, reviewed_by. |

> **No SLA on fulfillment.** Framework requests are treated as feature requests. Acceptance means the request is noted for potential future implementation — it does not guarantee the framework will be created.

---

### 6.10 Permissions Matrix

> Covers: BR-COMP-001, BR-COMP-002, BR-COMP-009, BR-COMP-011

| Action | `TENANT_OWNER` | `SUPER_ADMIN` |
|--------|:--------------:|:-------------:|
| List available frameworks (tier-filtered) | Yes (own tier) | Yes (all) |
| View framework details & controls | Yes (ACTIVE + own selections) | Yes (all, including DRAFT) |
| View "Coming Soon" frameworks (`is_public` DRAFT) | Yes (name + description only) | Yes (full details) |
| Create framework | No | Yes |
| Edit framework / controls / mappings | No | Yes |
| Publish framework (DRAFT → ACTIVE) | No | Yes |
| Deprecate framework (ACTIVE → DEPRECATED) | No | Yes |
| Reactivate framework (DEPRECATED → ACTIVE) | No | Yes |
| Delete draft framework | No | Yes |
| Configure plan tier access | No | Yes |
| Select frameworks for assessment | Yes | No (tenant action) |
| Deselect frameworks | Yes | No (tenant action) |
| View own assessments & control results | Yes | Yes (any tenant) |
| Download compliance report (PDF/HTML) | Yes (Pro+ only) | Yes (any tenant) |
| View historical trends | Yes | Yes (any tenant) |
| Submit framework request | Yes | No |
| View own framework requests | Yes | Yes (all tenants) |
| Review framework requests | No | Yes |

> **Super admins do not select frameworks on behalf of tenants.** Framework selection is a tenant-owned action. Super admins can view any tenant's selections and assessments but do not modify them. This is consistent with the principle that tenant data operations belong to the tenant owner.

---

### 6.11 Edge Cases

> Cross-cutting edge cases for the compliance engine.

| Scenario | Behavior |
|----------|----------|
| Scan completes with `PARTIAL` status (some steps failed) | Assessment runs against available results. Controls mapped to failed/skipped check types get `NOT_ASSESSED`. Score denominator excludes them. |
| Tenant selects a framework, then downgrades to Starter | Framework selection preserved but no new assessments generated (compliance_checks feature flag is false). Existing assessments remain viewable. |
| Super admin edits a control's check mappings after assessments exist | Existing assessment results are immutable. New mappings apply to future assessments only. |
| Super admin deprecates a framework with active tenant selections | No new assessments generated for that framework. Existing selections and results preserved. Tenants can still view historical assessments and trends. |
| Tenant selects a framework that has no check mappings for the scan steps in their workflow | All controls evaluate to `NOT_ASSESSED`. Score = 0.00. Assessment is still created and stored. |
| Two scans complete simultaneously for the same tenant and domain | Each scan generates its own independent assessment. Composite unique `(scan_job_id, framework_id)` prevents conflicts. Both contribute to trend data. |
| Framework has 0 controls when published | Blocked by BR-COMP-002B step 4. Cannot publish a framework with no controls. |
| Tenant deselects a framework between scan start and scan completion | Assessment skips deselected framework. Selection checked at assessment time, not scan creation time. |
| Scan results contain findings for a check type not mapped to any control | Findings are ignored by the compliance engine. Only mapped check types are evaluated. |
| Super admin reactivates a deprecated framework | Framework becomes selectable again. Tenants with existing selections resume getting assessments on next scan. Trend data continues from where it stopped. |
| Tenant submits duplicate framework request | Rejected with `ERR_COMP_018` if an open request (SUBMITTED or REVIEWED) exists for the same framework name from the same tenant. |
| Control's `min_security_recommendations_json` is empty | Recommendations field on control_result contains only failure-based recommendations (if any). No minimum recommendations shown. Valid state — not all controls need baseline guidance. |
| Tenant has compliance_checks enabled but compliance_reports disabled | Assessments are generated and viewable. Report download is blocked with `ERR_COMP_012`. This state shouldn't occur with current tier matrix but is handled defensively. |
| Super admin deletes a draft framework with `is_public = true` | Deletion requires a `reason` (minimum 10 characters, rejected with `ERR_COMP_022` if missing). System logs `AUDIT_LOG("compliance.public_draft_deleted", { framework_id, name, reason, admin_user_id })`. A changelog entry is automatically published to the tenant-facing **Changelog/Roadmap** page explaining why the previously "Coming Soon" framework was removed. Entry includes: framework name, reason (admin-provided), and date. Non-public draft deletion (`is_public = false`) does not require a reason or changelog entry. |

#### Changelog/Roadmap Integration

The following compliance engine events automatically publish entries to the tenant-facing Changelog/Roadmap page in the dashboard:

| Event | Changelog Entry |
|-------|----------------|
| Framework published (DRAFT → ACTIVE) | "New compliance framework available: {name}" |
| Framework deprecated (ACTIVE → DEPRECATED) | "Compliance framework sunset: {name} — no longer available for new assessments" |
| Framework reactivated (DEPRECATED → ACTIVE) | "Compliance framework restored: {name}" |
| Public draft deleted (`is_public = true`) | "Coming Soon framework removed: {name} — {admin_reason}" |

> **Changelog/Roadmap page** is a tenant-facing dashboard page showing platform updates, compliance framework changes, feature releases, and roadmap items. Super admin publishes entries manually or they are auto-generated by system events. Full specification will be covered in §9 (Super Admin & Operations). Compliance engine events are one source of entries — other domains (feature releases, operational changes) will also contribute.

---

### 6.12 Error Codes

| Code | HTTP | Message | Cause |
|------|------|---------|-------|
| `ERR_COMP_001` | 403 | Insufficient permissions to manage compliance frameworks. | Caller does not have `SUPER_ADMIN` role. |
| `ERR_COMP_002` | 409 | Compliance framework name already exists. | Attempted to create a framework with a duplicate name (case-insensitive). |
| `ERR_COMP_003` | 404 | Compliance framework not found. | `framework_id` does not match any framework. |
| `ERR_COMP_004` | 400 | Framework must be in DRAFT status to publish. | Attempted to publish a framework that is not in `DRAFT` state. |
| `ERR_COMP_005` | 400 | Framework must have at least one control with a check mapping before publishing. | Attempted to publish a framework with no controls or no check mappings. |
| `ERR_COMP_006` | 400 | Framework must have tier access configured for at least one plan before publishing. | No `plan_compliance_access` rows with `enabled = true` for this framework. |
| `ERR_COMP_007` | 400 | Framework must be in ACTIVE status to deprecate. | Attempted to deprecate a framework that is not `ACTIVE`. |
| `ERR_COMP_008` | 400 | Only DRAFT frameworks can be deleted. | Attempted to delete an `ACTIVE` or `DEPRECATED` framework. |
| `ERR_COMP_009` | 404 | Compliance control not found. | `control_id` does not match any control. |
| `ERR_COMP_010` | 400 | Invalid check type. | `check_type` in mapping does not match any known scan check type from §4.9. |
| `ERR_COMP_011` | 400 | Invalid pass condition format. | `pass_condition_json` does not contain a supported condition type. |
| `ERR_COMP_012` | 403 | Compliance reports require Pro or Enterprise plan. | Tenant does not have `compliance_reports` feature flag enabled. |
| `ERR_COMP_013` | 404 | Compliance assessment not found. | `assessment_id` does not match any assessment. |
| `ERR_COMP_014` | 403 | Assessment does not belong to your tenant. | Tenant owner attempted to access another tenant's assessment. |
| `ERR_COMP_015` | 403 | Only tenant owners can submit framework requests. | Caller does not have `TENANT_OWNER` role. |
| `ERR_COMP_016` | 400 | Framework name is required (minimum 3 characters). | `framework_name` on request is blank or too short. |
| `ERR_COMP_017` | 400 | Description is required (minimum 20 characters). | `description` on request is blank or too short. |
| `ERR_COMP_018` | 409 | You already have an open request for this framework. | Duplicate request with status `SUBMITTED` or `REVIEWED` exists for the same tenant and framework name. |
| `ERR_COMP_019` | 404 | Framework request not found. | `request_id` does not match any request. |
| `ERR_COMP_020` | 400 | Invalid status transition for framework request. | Attempted an invalid state change (e.g., `ACCEPTED` → `SUBMITTED`). |
| `ERR_COMP_021` | 400 | Admin notes are required (minimum 10 characters). | `admin_notes` is blank or too short when reviewing a request. |
| `ERR_COMP_022` | 400 | Reason is required when deleting a public draft framework (minimum 10 characters). | Attempted to delete a draft framework with `is_public = true` without providing a reason. |

> **Cross-references:** Compliance feature flag enforcement during scanning uses `ERR_SCAN_007` and `ERR_SCAN_015` (§4.14). The `compliance_checks` and `compliance_reports` feature flags are defined in §5.1.
