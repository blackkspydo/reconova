# User Flows (Compliance Engine)

Scope: User journeys for compliance framework selection, assessment viewing, report download, trend analysis, framework requests (tenant), and framework/control/mapping management with tier access and request review (super admin).

---

## Preconditions

| Flow Group | Auth State | Role | Data Requirements |
|-----------|------------|------|-------------------|
| Framework selection | Authenticated | Tenant Owner | Active tenant, compliance_checks enabled, Pro+ plan |
| View assessments | Authenticated | Any tenant member | Active tenant, compliance_checks enabled |
| Download reports | Authenticated | Tenant Owner / Member | compliance_reports enabled (Pro+) |
| Submit framework request | Authenticated | Tenant Owner | Active tenant |
| Admin framework management | Authenticated | Super Admin | — |
| Admin tier access | Authenticated | Super Admin | ≥1 plan exists |
| Admin request review | Authenticated | Super Admin | Requests exist |

---

## Flow 1: Compliance Dashboard — Page Load

**Entry points:**
- Clicks "Compliance" in sidebar navigation
- Redirected from scan results compliance tab

```
[User navigates to /compliance]
    │ (analytics: compliance_dashboard_viewed)
    ▼
Feature gate check: isEnabled("compliance_checks")
    │
    ├─ Disabled (plan) → <FeatureGate> locked placeholder
    │   "Compliance checking requires Pro plan."
    │   [Upgrade Plan] → /settings/billing/plans
    │   (stop — no further loading)
    │
    ├─ Disabled (operational) → <OperationalBanner>
    │   "Compliance is temporarily unavailable for maintenance."
    │   (stop — no further loading)
    │
    └─ Enabled → load dashboard
        │
        ▼
    Parallel API calls:
        ├─ GET /api/compliance/frameworks (available + selected)
        ├─ GET /api/compliance/assessments/latest (latest per framework)
        └─ GET /api/compliance/requests (own tenant requests)
            │
            ├─ Loading → skeleton dashboard
            │
            └─ Success → render three sections:
                │
                │ Section 1: Framework Selection
                │   ACTIVE frameworks where plan_compliance_access.enabled=true
                │   Each shows: name, version, region, [Select]/[Deselect] toggle
                │   "Coming Soon" frameworks (DRAFT, is_public=true) — name + desc only
                │   Deprecated frameworks with existing selections — "Deprecated" badge
                │
                │ Section 2: Assessment Scores
                │   One card per selected framework
                │   Each shows: framework name, latest score, trend sparkline
                │   Click card → /compliance/frameworks/{id}
                │   Empty state if no frameworks selected
                │
                │ Section 3: Framework Requests
                │   Existing requests with status badges
                │   [Request a Framework] CTA
                │
                └─► Dashboard rendered
```

---

## Flow 2: Select / Deselect Framework

**Preconditions:** Tenant Owner role, compliance_checks enabled

```
[Compliance Dashboard → Framework Selection section]
    │
    ├─ Clicks [Select] on an ACTIVE framework
    │   │ (analytics: framework_selected, {framework_id, framework_name})
    │   ▼
    │ API: POST /api/compliance/selections {framework_id}
    │   │
    │   ├─ Success (201)
    │   │   ▼
    │   │ Toggle updates to [Deselect]
    │   │ Toast: "Framework selected. It will be assessed on your next scan."
    │   │ Assessment scores section shows new card (no data yet)
    │   │
    │   ├─ ERR_COMP_009 (already selected)
    │   │   └─► Toast (info): "Framework already selected."
    │   │       Refresh selections
    │   │
    │   ├─ ERR_COMP_007 (framework not available for tier)
    │   │   └─► Toast (error): "This framework is not available on your plan."
    │   │       Should not occur if UI filters correctly
    │   │
    │   └─ ERR_COMP_006 (framework not found)
    │       └─► Toast (error): "Framework not found."
    │           Refresh framework list
    │
    └─ Clicks [Deselect] on a selected framework
        │ (analytics: framework_deselected, {framework_id})
        ▼
    [Deselect Confirmation Modal]
        │ "Stop assessing against {Framework Name}?"
        │ "Future scans won't generate compliance results for this framework."
        │ "Existing assessment data will be preserved."
        │ CTA: [Deselect]  [Cancel]
        │
        ├─ Clicks [Cancel] → modal closes
        │
        └─ Clicks [Deselect]
            │ (analytics: framework_deselected_confirmed, {framework_id})
            ▼
        API: DELETE /api/compliance/selections/{framework_id}
            │
            ├─ Success (200)
            │   ▼
            │ Toggle updates to [Select]
            │ Toast: "Framework deselected."
            │ Assessment card remains (historical data preserved)
            │ Card shows "No longer assessed" indicator
            │
            └─ ERR_COMP_010 (not selected)
                └─► Toast (info): "Framework was already deselected."
                    Refresh selections
```

---

## Flow 3: View Framework Detail (Tenant)

**Entry point:** Clicks framework score card on dashboard

```
[Compliance Dashboard] → clicks framework score card
    │ (analytics: framework_detail_viewed, {framework_id})
    ▼
[Navigate to /compliance/frameworks/{id}]
    │
    ▼
Parallel API calls:
    ├─ GET /api/compliance/frameworks/{id} (framework info)
    ├─ GET /api/compliance/assessments?framework_id={id} (assessment list)
    └─ GET /api/compliance/trends/{framework_id} (trend data)
        │
        ├─ Loading → skeleton page
        │
        └─ Success → render framework detail
            │
            │ Header: Framework name, version, region, status badge
            │ Latest score: large score display with color coding
            │
            │ Trend Chart section:
            │   ├─ ≥3 assessments → line chart (score over time)
            │   │   Direction indicator: "Improving" / "Declining" / "Stable"
            │   │
            │   └─ <3 assessments → "Run {N} more scans to see trends."
            │
            │ Assessment History table:
            │   Date | Score | Controls (pass/fail/partial/na) | Domain | [View]
            │   Sorted by date descending
            │   Click [View] → /compliance/assessments/{id}
            │
            │ IF framework deprecated:
            │   Banner: "This framework has been deprecated. No new assessments."
            │
            └─► Page rendered
```

---

## Flow 4: View Assessment Detail

**Entry point:** Clicks assessment row in framework detail

```
[Framework Detail] → clicks [View] on an assessment
    │ (analytics: assessment_detail_viewed, {assessment_id})
    ▼
[Navigate to /compliance/assessments/{id}]
    │
    ▼
API: GET /api/compliance/assessments/{id}
    │ Returns: assessment metadata + all control_results
    │
    ├─ Loading → skeleton page
    │
    └─ Success → render assessment detail
        │
        │ Header:
        │   Framework name | Score: {X}% | Date: {date}
        │   Domain: {domain} | Scan: {scan_job_id link}
        │   Controls: {pass}/{total} passing
        │
        │ Score Breakdown:
        │   ████████████░░░░░  72.5%
        │   Pass: 29 | Fail: 8 | Partial: 3 | Not Assessed: 2
        │
        │ Filter bar:
        │   [All (42)] [Pass (29)] [Fail (8)] [Partial (3)] [Not Assessed (2)]
        │
        │ Controls grouped by category:
        │   ┌─ Access Control (12 controls)
        │   ├─ Data Protection (8 controls)
        │   ├─ Incident Response (6 controls)
        │   └─ ...
        │
        │ Each control row (collapsed):
        │   Control ID | Title | Status badge
        │
        │ Each control row (expanded):
        │   Description
        │   Evidence summary (from evidence_json)
        │   Findings: {N} findings
        │   Recommendations (from recommendations array)
        │   Min security recommendations (always shown)
        │
        │ [Download Report] button (bottom)
        │   IF compliance_reports enabled → active
        │   IF compliance_reports disabled → <LockedBadge> "Requires Pro"
        │
        └─► Page rendered
```

---

## Flow 5: Download Compliance Report

**Preconditions:** compliance_reports feature flag enabled (Pro+)

```
[Assessment Detail] → clicks [Download Report]
    │ (analytics: compliance_report_download_initiated, {assessment_id})
    ▼
[Download Format Selector]
    │ Inline dropdown or button group:
    │   [PDF]  [HTML]
    │
    └─ Clicks format option
        │ (analytics: compliance_report_downloaded, {assessment_id, format})
        ▼
    API: GET /api/compliance/reports/{assessment_id}?format={pdf|html}
        │ Response: file download (Content-Disposition: attachment)
        │
        ├─ Success → browser download initiated
        │   Toast: "Report downloaded."
        │
        ├─ ERR_COMP_014 (reports feature disabled)
        │   └─► Toast (error): "Upgrade to Pro to download compliance reports."
        │       Show [Upgrade Plan] link
        │
        ├─ ERR_COMP_012 (assessment not found)
        │   └─► Toast (error): "Assessment not found."
        │       Navigate back to framework detail
        │
        └─ Network error / 500
            └─► Toast (error): "Report generation failed. Try again."
                [Retry] button
```

---

## Flow 6: Submit Framework Request

**Preconditions:** Tenant Owner role

```
[Compliance Dashboard → Framework Requests section]
    │ Shows existing requests with status badges:
    │   "ISO 27001" — SUBMITTED (pending)
    │   "HIPAA" — ACCEPTED
    │
    └─ Clicks [Request a Framework]
        │ (analytics: framework_request_initiated)
        ▼
    [Framework Request Modal]
        │ Framework Name: [________________] (required, ≥3 chars)
        │ Description: [textarea] (required, ≥20 chars)
        │   Placeholder: "Describe why you need this framework and which
        │                 compliance standards it should cover..."
        │ CTA: [Submit Request]  [Cancel]
        │
        ├─ Name < 3 chars → inline error: "Name must be at least 3 characters."
        ├─ Description < 20 chars → inline error: "Description must be at least 20 characters."
        ├─ [Submit Request] disabled until both valid
        │
        ├─ Clicks [Cancel] → modal closes
        │
        └─ Clicks [Submit Request]
            │ (analytics: framework_request_submitted, {framework_name})
            ▼
        API: POST /api/compliance/requests {framework_name, description}
            │
            ├─ Success (201)
            │   ▼
            │ Modal closes
            │ Toast: "Framework request submitted."
            │ Requests list refreshes — new request appears as SUBMITTED
            │
            ├─ ERR_COMP_016 (duplicate open request)
            │   └─► Toast (error): "You already have an open request for this framework."
            │       Modal stays open
            │
            ├─ ERR_COMP_017 (validation error)
            │   └─► Inline errors on relevant fields
            │
            └─ ERR_COMP_015 (permission denied)
                └─► Toast (error): "Only tenant owners can submit requests."
```

---

## Flow 7: Super Admin — Framework Management Overview

**Entry point:** Admin Panel > Compliance

```
[Admin navigates to /admin/compliance]
    │ (analytics: admin_compliance_viewed)
    ▼
API: GET /api/admin/compliance/frameworks
    │
    ├─ Loading → skeleton table
    │
    └─ Success → render framework management page
        │
        │ Filter bar: [All Statuses ▼] [Search: ________]
        │
        │ Framework table:
        │   Name | Version | Region | Status | Controls | Plans | Actions
        │
        │ Status badges: DRAFT (grey), ACTIVE (green), DEPRECATED (orange)
        │ Template badge: "Template" (for seeded, immutable)
        │
        │ Actions per status:
        │   DRAFT:      [Edit] [Publish] [Delete]
        │   ACTIVE:     [View] [Deprecate]
        │   DEPRECATED: [View] [Reactivate]
        │
        │ [Create Framework] button (top)
        │
        └─► Page rendered
```

---

## Flow 8: Super Admin — Create Framework

```
[Admin Framework List] → clicks [Create Framework]
    │ (analytics: admin_framework_create_initiated)
    ▼
[Navigate to /admin/compliance/frameworks/new]
    │
    │ Framework Creation Form:
    │   Name: [________________] (required, ≤200 chars, unique)
    │   Version: [________________] (required)
    │   Region: [________________] (optional)
    │   Description: [textarea] (required, ≤2000 chars)
    │   Public Preview: [  ] checkbox ("Show as Coming Soon to tenants")
    │
    │   CTA: [Create Framework]  [Cancel]
    │
    ├─ Clicks [Cancel] → navigate back to framework list
    │
    └─ Clicks [Create Framework]
        │ (analytics: admin_framework_created, {name})
        ▼
    API: POST /api/admin/compliance/frameworks
         {name, version, region, description, is_public}
        │
        ├─ Success (201)
        │   ▼
        │ Toast: "Framework created as draft."
        │ Navigate to /admin/compliance/frameworks/{id}
        │ (ready to add controls)
        │
        ├─ ERR_COMP_002 (duplicate name)
        │   └─► Inline error on name field: "A framework with this name already exists."
        │
        └─ ERR_COMP_001 (permission denied)
            └─► Toast (error): "Only super admins can create frameworks."
```

---

## Flow 9: Super Admin — Framework Detail (Edit Controls + Mappings)

```
[Admin Framework List] → clicks framework name or [Edit]/[View]
    │ (analytics: admin_framework_detail_viewed, {framework_id})
    ▼
[Navigate to /admin/compliance/frameworks/{id}]
    │
    ▼
API: GET /api/admin/compliance/frameworks/{id}
    │ Returns: framework metadata + controls + mappings + tier access
    │
    ├─ Loading → skeleton page
    │
    └─ Success → render framework detail
        │
        │ Header: Framework name, version, status badge
        │ IF DRAFT: [Edit] metadata, [Publish], [Delete]
        │ IF ACTIVE: [Deprecate]
        │ IF DEPRECATED: [Reactivate]
        │
        │ Tab navigation:
        │   [Controls] [Tier Access]
        │
        │ === Controls Tab (default) ===
        │
        │ [Add Control] button
        │ Control table:
        │   Control ID | Title | Category | Mappings | Actions
        │   Click row → expand to show mappings
        │
        │ Expanded control:
        │   Description
        │   Min Security Recommendations (JSON display)
        │   Mappings table:
        │     Check Type | Severity | Pass Condition | [Edit] [Delete]
        │   [Add Mapping] button
        │
        │ === Tier Access Tab ===
        │
        │ Plan toggle matrix:
        │   Plan Name | Enabled [toggle]
        │   Starter   | [ ]
        │   Pro       | [✓]
        │   Enterprise| [✓]
        │
        └─► Page rendered
```

---

## Flow 10: Super Admin — Add/Edit Control

```
[Framework Detail → Controls Tab] → clicks [Add Control]
    │ (analytics: admin_control_create_initiated, {framework_id})
    ▼
[Add Control Modal]
    │ Control ID: [________________] (required, ≤50 chars, unique within framework)
    │ Title: [________________] (required, ≤300 chars)
    │ Category: [________________] (required, ≤100 chars)
    │ Description: [textarea] (≤2000 chars)
    │ Min Security Recommendations: [JSON editor] (optional, JSONB array)
    │   Hint: '[{"priority": "HIGH", "recommendation": "..."}]'
    │
    │ CTA: [Add Control]  [Cancel]
    │
    └─ Clicks [Add Control]
        │ (analytics: admin_control_created, {framework_id, control_id})
        ▼
    API: POST /api/admin/compliance/frameworks/{fid}/controls
         {control_id, title, category, description, min_security_recommendations_json}
        │
        ├─ Success (201)
        │   ▼
        │ Toast: "Control added."
        │ Control appears in table
        │ Modal closes
        │
        ├─ ERR_COMP_003 (duplicate control_id within framework)
        │   └─► Inline error: "Control ID already exists in this framework."
        │
        └─ ERR_COMP_004 (validation error)
            └─► Inline errors on relevant fields

[Edit Control] — same modal pre-filled with existing values
    │ API: PUT /api/admin/compliance/frameworks/{fid}/controls/{cid}
    │ Same error handling
```

---

## Flow 11: Super Admin — Add/Edit Control-Check Mapping

```
[Control expanded] → clicks [Add Mapping]
    │ (analytics: admin_mapping_create_initiated, {control_id})
    ▼
[Add Mapping Modal]
    │ Check Type: [dropdown] (required, valid check types from §4.9)
    │   Options: subdomain_enumeration, port_scanning, technology_detection,
    │            screenshot_capture, vulnerability_scanning
    │
    │ Severity Threshold: [dropdown] (optional)
    │   Options: (none), CRITICAL, HIGH, MEDIUM, LOW
    │
    │ Pass Condition: [dropdown + config]
    │   Type: [no_findings_above_threshold ▼]
    │     • no_findings_above_threshold — uses severity_threshold
    │     • specific_check — requires: expected_value (boolean)
    │     • finding_count_below — requires: max_count (integer)
    │     • custom — requires: expression (string)
    │   Dynamic form fields based on selected type
    │
    │ Recommendation: [textarea] (optional)
    │   "Guidance shown when this mapping fails."
    │
    │ CTA: [Add Mapping]  [Cancel]
    │
    └─ Clicks [Add Mapping]
        │ (analytics: admin_mapping_created, {control_id, check_type})
        ▼
    API: POST /api/admin/compliance/controls/{cid}/mappings
         {check_type, severity_threshold, pass_condition_json, recommendation_json}
        │
        ├─ Success (201)
        │   ▼
        │ Toast: "Mapping added."
        │ Mapping appears in control's mapping table
        │ Modal closes
        │
        ├─ ERR_COMP_005 (invalid check type)
        │   └─► Inline error on check type field
        │
        └─ ERR_COMP_004 (validation error)
            └─► Inline errors on relevant fields
```

---

## Flow 12: Super Admin — Publish Framework

**Preconditions:** Framework in DRAFT status

```
[Framework Detail] → clicks [Publish]
    │ (analytics: admin_framework_publish_initiated, {framework_id})
    ▼
Pre-publish validation (client-side check):
    │
    ├─ Controls count = 0
    │   └─► Toast (warning): "Add at least one control before publishing."
    │       Publish blocked
    │
    ├─ Controls with 0 mappings exist
    │   └─► Toast (warning): "{N} controls have no check mappings."
    │       Publish blocked
    │
    ├─ No plan tier has access enabled
    │   └─► Toast (warning): "Enable access for at least one plan tier."
    │       Publish blocked
    │
    └─ All validations pass
        │
        ▼
    [Publish Confirmation Modal]
        │ "Publish {Framework Name}?"
        │ "This framework will become available to tenants on enabled plan tiers."
        │ Summary: {N} controls, {M} mappings, {P} plan tiers enabled
        │ CTA: [Publish]  [Cancel]
        │
        ├─ Clicks [Cancel] → modal closes
        │
        └─ Clicks [Publish]
            │ (analytics: admin_framework_published, {framework_id})
            ▼
        API: PUT /api/admin/compliance/frameworks/{id}/publish
            │
            ├─ Success (200)
            │   ▼
            │ Status badge updates to ACTIVE (green)
            │ Toast: "Framework published. Changelog entry created."
            │ Action buttons update (now shows [Deprecate])
            │
            ├─ ERR_COMP_008 (no controls/mappings)
            │   └─► Toast (error): "Framework must have controls with mappings."
            │
            └─ ERR_COMP_007 (no tier access)
                └─► Toast (error): "Enable access for at least one plan tier."
```

---

## Flow 13: Super Admin — Deprecate Framework

```
[Framework Detail (ACTIVE)] → clicks [Deprecate]
    │ (analytics: admin_framework_deprecate_initiated, {framework_id})
    ▼
[Deprecate Confirmation Modal]
    │ "Deprecate {Framework Name}?"
    │ "Tenants with existing selections will keep their historical data."
    │ "No new assessments will be generated for this framework."
    │ "A changelog entry will be published."
    │ CTA: [Deprecate]  [Cancel]
    │
    ├─ Clicks [Cancel] → modal closes
    │
    └─ Clicks [Deprecate]
        │ (analytics: admin_framework_deprecated, {framework_id})
        ▼
    API: PUT /api/admin/compliance/frameworks/{id}/deprecate
        │
        ├─ Success (200)
        │   ▼
        │ Status badge updates to DEPRECATED (orange)
        │ Toast: "Framework deprecated. Changelog entry created."
        │ Actions update (now shows [Reactivate])
        │
        └─ ERR_COMP_001 (permission denied)
            └─► Toast (error): "Insufficient permissions."
```

---

## Flow 14: Super Admin — Delete Draft Framework

```
[Framework Detail (DRAFT)] → clicks [Delete]
    │ (analytics: admin_framework_delete_initiated, {framework_id})
    ▼
    ├─ IF is_public = false:
    │   ▼
    │ [Simple Confirmation Modal]
    │   "Delete draft framework {Name}?"
    │   "This cannot be undone."
    │   CTA: [Delete]  [Cancel]
    │
    └─ IF is_public = true:
        ▼
    [Delete Public Draft Modal]
        │ "Delete public draft {Name}?"
        │ "This framework is visible to tenants as 'Coming Soon'."
        │ "A changelog entry will be published noting its removal."
        │ Reason: [textarea] (required, ≥10 chars)
        │ CTA: [Delete]  [Cancel]
        │
        ├─ Reason < 10 chars → [Delete] disabled
        │   Inline hint: "Reason must be at least 10 characters."

    [Either modal] → clicks [Delete]
        │ (analytics: admin_framework_deleted, {framework_id, is_public})
        ▼
    API: DELETE /api/admin/compliance/frameworks/{id}
         {reason} (if is_public)
        │
        ├─ Success (200)
        │   ▼
        │ Toast: "Framework deleted."
        │ Navigate back to /admin/compliance
        │ IF was public: "Changelog entry created."
        │
        ├─ ERR_COMP_022 (reason required for public draft)
        │   └─► Inline error on reason field
        │
        └─ ERR_COMP_021 (not a draft)
            └─► Toast (error): "Only draft frameworks can be deleted."
```

---

## Flow 15: Super Admin — Reactivate Framework

```
[Framework Detail (DEPRECATED)] → clicks [Reactivate]
    │ (analytics: admin_framework_reactivate_initiated, {framework_id})
    ▼
[Reactivate Confirmation Modal]
    │ "Reactivate {Framework Name}?"
    │ "Tenants will be able to select this framework again."
    │ "A changelog entry will be published."
    │ CTA: [Reactivate]  [Cancel]
    │
    └─ Clicks [Reactivate]
        │ (analytics: admin_framework_reactivated, {framework_id})
        ▼
    API: PUT /api/admin/compliance/frameworks/{id}/reactivate
        │
        ├─ Success (200)
        │   ▼
        │ Status updates to ACTIVE
        │ Toast: "Framework reactivated. Changelog entry created."
        │
        └─ ERR_COMP_001 (permission denied)
            └─► Toast (error): "Insufficient permissions."
```

---

## Flow 16: Super Admin — Configure Tier Access

```
[Framework Detail → Tier Access Tab]
    │ (analytics: admin_tier_access_viewed, {framework_id})
    ▼
[Tier Access Matrix]
    │ Plan toggles:
    │   Starter:     [ ]
    │   Pro:         [✓]
    │   Enterprise:  [✓]
    │
    └─ Clicks toggle on a plan
        │ (analytics: admin_tier_access_toggled, {framework_id, plan_id, enabled})
        ▼
    API: PUT /api/admin/compliance/frameworks/{fid}/access
         {plan_id, enabled}
        │
        ├─ Success (200)
        │   ▼
        │ Toggle updates
        │ Toast: "{Plan} access {enabled/disabled} for {Framework}."
        │
        └─ ERR_COMP_001 (permission denied)
            └─► Toast (error): "Insufficient permissions."
```

---

## Flow 17: Super Admin — Review Framework Request

**Entry point:** Admin Panel > Compliance > Requests tab

```
[Admin navigates to /admin/compliance/requests]
    │ (analytics: admin_compliance_requests_viewed)
    ▼
API: GET /api/admin/compliance/requests
    │
    ├─ Loading → skeleton table
    │
    └─ Success → render request list
        │
        │ Filter: [All ▼] [SUBMITTED] [REVIEWED] [ACCEPTED] [REJECTED]
        │
        │ Request table:
        │   Tenant | Framework Name | Submitted | Status | Actions
        │
        │ Status badges:
        │   SUBMITTED (blue), REVIEWED (yellow), ACCEPTED (green), REJECTED (red)
        │
        └─ Clicks [Review] on a request
            │ (analytics: admin_request_review_initiated, {request_id})
            ▼
        [Review Request Modal]
            │ Tenant: {tenant_name} (read-only)
            │ Framework: {framework_name} (read-only)
            │ Description: {description} (read-only)
            │ Submitted: {date} (read-only)
            │ Current status: {status}
            │
            │ ────────────────────────────
            │
            │ New Status: [REVIEWED ▼] / [ACCEPTED ▼] / [REJECTED ▼]
            │   Available transitions depend on current status:
            │     SUBMITTED → REVIEWED, ACCEPTED, REJECTED
            │     REVIEWED → ACCEPTED, REJECTED
            │     ACCEPTED/REJECTED → (terminal, no action)
            │
            │ Admin Notes: [textarea] (required, ≥10 chars)
            │   Placeholder: "Provide feedback on this request..."
            │
            │ CTA: [Update Status]  [Cancel]
            │
            ├─ Notes < 10 chars → [Update Status] disabled
            │
            └─ Clicks [Update Status]
                │ (analytics: admin_request_reviewed, {request_id, new_status})
                ▼
            API: PUT /api/admin/compliance/requests/{id}
                 {status, admin_notes}
                │
                ├─ Success (200)
                │   ▼
                │ Modal closes
                │ Toast: "Request updated to {status}."
                │ Table refreshes
                │
                ├─ ERR_COMP_019 (invalid transition)
                │   └─► Toast (error): "Cannot transition from {current} to {new}."
                │
                └─ ERR_COMP_018 (notes required)
                    └─► Inline error on notes field
```
