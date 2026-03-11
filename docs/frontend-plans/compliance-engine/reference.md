# Reference (Compliance Engine)

Scope: Error handling matrix, input validation rules, security considerations, and frontend-to-backend action mapping.

---

## Error Handling Matrix

### Authorization Errors

| Error Code | HTTP | User Message | UI Action |
|-----------|------|-------------|-----------|
| `ERR_COMP_001` | 403 | "You don't have permission to manage compliance frameworks." | Toast (error). Redirect to dashboard. |
| `ERR_COMP_015` | 403 | "Only tenant owners can submit framework requests." | Toast (error). Hide request form. |

### Framework Errors

| Error Code | HTTP | User Message | UI Action |
|-----------|------|-------------|-----------|
| `ERR_COMP_002` | 409 | "A framework with this name already exists." | Inline error on name field. Keep form open. |
| `ERR_COMP_006` | 404 | "Framework not found." | Toast (error). Refresh framework list. |
| `ERR_COMP_007` | 403 | "This framework is not available on your plan." | Toast (error). Should not occur (UI filters by plan). |
| `ERR_COMP_008` | 400 | "Framework must have at least one control with check mappings before publishing." | Toast (error). Stay on framework detail. |
| `ERR_COMP_020` | 400 | "Framework must have tier access enabled for at least one plan." | Toast (error). Switch to Tier Access tab. |
| `ERR_COMP_021` | 400 | "Only draft frameworks can be deleted." | Toast (error). Refresh framework state. |
| `ERR_COMP_022` | 400 | "Reason is required when deleting a public draft." | Inline error on reason field. Keep modal open. |

### Selection Errors

| Error Code | HTTP | User Message | UI Action |
|-----------|------|-------------|-----------|
| `ERR_COMP_009` | 409 | "This framework is already selected." | Toast (info). Refresh selections. |
| `ERR_COMP_010` | 404 | "Framework was already deselected." | Toast (info). Refresh selections. |

### Control & Mapping Errors

| Error Code | HTTP | User Message | UI Action |
|-----------|------|-------------|-----------|
| `ERR_COMP_003` | 409 | "Control ID already exists in this framework." | Inline error on control_id field. Keep modal open. |
| `ERR_COMP_004` | 400 | "Validation error." | Inline errors on relevant fields. Keep modal open. |
| `ERR_COMP_005` | 400 | "Invalid check type." | Inline error on check_type dropdown. Keep modal open. |
| `ERR_COMP_011` | 404 | "Control not found." | Toast (error). Refresh controls list. |

### Assessment & Report Errors

| Error Code | HTTP | User Message | UI Action |
|-----------|------|-------------|-----------|
| `ERR_COMP_012` | 404 | "Assessment not found." | Toast (error). Navigate back to framework detail. |
| `ERR_COMP_013` | 400 | "No scan data available for assessment." | Toast (info). Assessment shows all NOT_ASSESSED. |
| `ERR_COMP_014` | 403 | "Upgrade to Pro to download compliance reports." | Toast (error). Show [Upgrade Plan] link. |

### Framework Request Errors

| Error Code | HTTP | User Message | UI Action |
|-----------|------|-------------|-----------|
| `ERR_COMP_016` | 409 | "You already have an open request for this framework." | Toast (error). Keep modal open. |
| `ERR_COMP_017` | 400 | "Validation error on request fields." | Inline errors on relevant fields. Keep modal open. |
| `ERR_COMP_018` | 400 | "Admin notes are required (min 10 characters)." | Inline error on notes field. Keep modal open. |
| `ERR_COMP_019` | 400 | "Invalid status transition." | Toast (error). Refresh request status. |

### Network & System Errors

| Scenario | User Message | UI Action |
|----------|-------------|-----------|
| Network timeout | "Unable to connect. Check your connection and try again." | Toast (error) + [Retry] button. |
| 500 Internal Server Error | "Something went wrong. Please try again." | Toast (error) + [Retry] button. |
| Report generation failure | "Report generation failed. Please try again." | Toast (error) + [Retry] button on download. |

### Error Response Parsing

```typescript
function handleComplianceError(error: ApiError): void {
  const { code, message, details } = error.error;

  switch (code) {
    case 'ERR_COMP_002':
    case 'ERR_COMP_003':
    case 'ERR_COMP_005':
      // Inline field errors — keep form open
      setFieldError(getFieldForCode(code), message);
      break;

    case 'ERR_COMP_004':
    case 'ERR_COMP_017':
      // Multiple validation errors
      if (details?.fields) {
        for (const [field, msg] of Object.entries(details.fields as Record<string, string>)) {
          setFieldError(field, msg);
        }
      } else {
        showToast('error', message);
      }
      break;

    case 'ERR_COMP_022':
    case 'ERR_COMP_018':
      // Inline error on reason/notes textarea
      setFieldError('reason', message);
      break;

    case 'ERR_COMP_009':
    case 'ERR_COMP_010':
      // Stale state — info toast and refresh
      showToast('info', message);
      refetchSelections();
      break;

    case 'ERR_COMP_012':
      // Navigate back
      showToast('error', message);
      navigateBack();
      break;

    case 'ERR_COMP_014':
      // Upgrade needed — show upgrade link
      showToast('error', message);
      showUpgradeLink('/settings/billing/plans');
      break;

    case 'ERR_COMP_008':
    case 'ERR_COMP_020':
      // Publish pre-condition failures
      showToast('error', message);
      break;

    case 'ERR_COMP_019':
      // Invalid transition — refresh state
      showToast('error', message);
      refetchRequests();
      break;

    case 'ERR_COMP_001':
    case 'ERR_COMP_015':
      // Permission denied
      showToast('error', message);
      navigateTo('/dashboard');
      break;

    default:
      showToast('error', message);
  }
}

function getFieldForCode(code: string): string {
  switch (code) {
    case 'ERR_COMP_002': return 'name';
    case 'ERR_COMP_003': return 'control_id';
    case 'ERR_COMP_005': return 'check_type';
    default: return '';
  }
}
```

---

## Input Validation Rules

### Framework Creation / Edit (Admin)

| Field | Type | Constraints | Error Message |
|-------|------|-------------|---------------|
| `name` | string | Required. Max 200 chars. Unique. | "Name is required." / "Name must be under 200 characters." / "A framework with this name already exists." |
| `version` | string | Required. | "Version is required." |
| `region` | string | Optional. | — |
| `description` | string | Required. Max 2000 chars. | "Description is required." / "Description must be under 2000 characters." |
| `is_public` | boolean | Optional. Default false. | — |

### Control Creation / Edit (Admin)

| Field | Type | Constraints | Error Message |
|-------|------|-------------|---------------|
| `control_id` | string | Required. Max 50 chars. Unique within framework. | "Control ID is required." / "Control ID already exists." |
| `title` | string | Required. Max 300 chars. | "Title is required." |
| `category` | string | Required. Max 100 chars. | "Category is required." |
| `description` | string | Optional. Max 2000 chars. | "Description must be under 2000 characters." |
| `min_security_recommendations_json` | JSON | Optional. Must be valid JSON array. | "Invalid JSON format. Must be an array." |

### Mapping Creation / Edit (Admin)

| Field | Type | Constraints | Error Message |
|-------|------|-------------|---------------|
| `check_type` | string | Required. Must be valid check type. | "Select a check type." |
| `severity_threshold` | enum | Optional. CRITICAL/HIGH/MEDIUM/LOW. | — |
| `pass_condition_json.type` | enum | Required. One of 4 types. | "Select a pass condition type." |
| `pass_condition_json.expected_value` | boolean | Required if type = specific_check. | "Select expected value." |
| `pass_condition_json.max_count` | integer | Required if type = finding_count_below. Min 0. | "Max count is required." / "Must be 0 or greater." |
| `pass_condition_json.expression` | string | Required if type = custom. | "Expression is required." |
| `recommendation_json` | string | Optional. | — |

### Framework Request (Tenant)

| Field | Type | Constraints | Error Message |
|-------|------|-------------|---------------|
| `framework_name` | string | Required. Min 3 chars. Max 200 chars. | "Name must be at least 3 characters." / "Name must be under 200 characters." |
| `description` | string | Required. Min 20 chars. Max 2000 chars. | "Description must be at least 20 characters." / "Description must be under 2000 characters." |

### Request Review (Admin)

| Field | Type | Constraints | Error Message |
|-------|------|-------------|---------------|
| `status` | enum | Required. Valid transition from current status. | "Invalid status transition." |
| `admin_notes` | string | Required. Min 10 chars. Max 500 chars. | "Notes must be at least 10 characters." |

### Delete Public Draft (Admin)

| Field | Type | Constraints | Error Message |
|-------|------|-------------|---------------|
| `reason` | string | Required (only if is_public=true). Min 10 chars. | "Reason must be at least 10 characters." |

### Client-Side Validation Timing

| Trigger | Behavior |
|---------|----------|
| Field blur | Validate that field, show inline error if invalid |
| Form submit | Validate all fields, focus first invalid field |
| Character count | Live count display on textarea fields |
| JSON field | Validate JSON on blur, show parse error |
| Pass condition type change | Show/hide dynamic fields, clear values for previous type |

---

## Security Considerations

### Secure Storage

| Data | Storage | Justification |
|------|---------|---------------|
| Framework list | Memory (state) | Re-fetched per page visit |
| Assessment data | Memory (state) | Immutable after creation, safe to cache by ID |
| Control results | Memory (state) | Part of assessment, immutable |
| Report files | Not stored | Streamed as download, discarded after save dialog |
| Admin framework detail | Memory (state) | Fetched per page visit |
| Request form values | Component state | Discarded on modal close |

### API Authorization

- All `/api/compliance/*` endpoints require authentication
- Selection CRUD requires TENANT_OWNER role
- Report download requires `compliance_reports` feature flag (checked server-side)
- All `/api/admin/compliance/*` endpoints require SUPER_ADMIN role
- Frontend role checks are UX convenience; API is authoritative

### Rate Limiting Awareness

| Action | Rate Limit | Frontend Handling |
|--------|-----------|-------------------|
| Framework selection toggle | Backend enforced | Disable toggle during API call |
| Report download | Backend enforced | Disable button during download, re-enable on complete/error |
| Framework CRUD (admin) | Backend enforced | Disable submit during API call |
| Control CRUD (admin) | Backend enforced | Disable submit during API call |
| Request submission | Backend enforced | Disable [Submit] during API call |
| Request review (admin) | Backend enforced | Disable [Update Status] during API call |

### Button Debouncing

| Action | Debounce Strategy |
|--------|-------------------|
| Select framework | Disable toggle on click → re-enable on API response |
| Deselect framework | Disable [Deselect] on click → re-enable on API response |
| Download report | Disable button on click → re-enable after download completes or error |
| Submit request | Disable [Submit Request] on click → re-enable on API response |
| Create/edit/delete control | Disable button on click → re-enable on response |
| Create/edit/delete mapping | Disable button on click → re-enable on response |
| Publish/deprecate/reactivate | Disable button on click → re-enable on response |
| Update request status | Disable [Update Status] on click → re-enable on response |
| Admin framework search | Debounce input 300ms before filtering |

### Input Sanitization

| Input | Sanitization |
|-------|-------------|
| Framework name/description | Trim whitespace. No HTML tags. Max length enforced. |
| Control ID | Trim whitespace. Max 50 chars. |
| Control title/description | Trim whitespace. No HTML tags. Max length enforced. |
| JSON fields (min_security_recs, pass_condition) | Parse and re-serialize to prevent injection. Validate structure. |
| Request framework_name/description | Trim whitespace. No HTML tags. Max length enforced. |
| Admin notes | Trim whitespace. No HTML tags. Max 500 chars. |
| Delete reason | Trim whitespace. No HTML tags. |

### Report Download Security

| Concern | Handling |
|---------|---------|
| Report content | Generated server-side from immutable assessment data |
| File download | Content-Disposition: attachment with sanitized filename |
| Format parameter | Whitelist: `pdf`, `html` only. Reject unknown values. |
| Feature flag bypass | Server validates `compliance_reports` flag independently |

---

## Key Actions → Backend Use Cases Mapping

### Tenant Owner Actions

| Frontend Action | Backend Use Case | Endpoint | Triggers |
|----------------|-----------------|----------|----------|
| View compliance dashboard | Load frameworks + selections + latest assessments | `GET /api/compliance/frameworks` + `/selections` + `/assessments/latest` | `/compliance` page load |
| Select framework | BR-COMP-003: Assessment will run on next scan | `POST /api/compliance/selections` | [Select] toggle |
| Deselect framework | Remove selection (data preserved) | `DELETE /api/compliance/selections/{id}` | [Deselect] confirmed |
| View framework detail | Load assessments + trend | `GET /api/compliance/assessments?framework_id=` + `/trends/{id}` | Score card click |
| View assessment detail | BR-COMP-005: Load control results | `GET /api/compliance/assessments/{id}` | Assessment row click |
| Download report | BR-COMP-008: Generate PDF/HTML | `GET /api/compliance/reports/{id}?format=` | [Download Report] click |
| Submit framework request | BR-COMP-011A: Create request | `POST /api/compliance/requests` | [Submit Request] click |

### Super Admin Actions

| Frontend Action | Backend Use Case | Endpoint | Triggers |
|----------------|-----------------|----------|----------|
| View framework list | List all frameworks | `GET /api/admin/compliance/frameworks` | `/admin/compliance` page load |
| Create framework | BR-COMP-002A: Create draft | `POST /api/admin/compliance/frameworks` | [Create Framework] submit |
| Edit framework metadata | Update framework | `PUT /api/admin/compliance/frameworks/{id}` | [Edit Metadata] save |
| Publish framework | BR-COMP-002B: DRAFT → ACTIVE | `PUT /api/admin/.../publish` | [Publish] confirmed |
| Deprecate framework | BR-COMP-002C: ACTIVE → DEPRECATED | `PUT /api/admin/.../deprecate` | [Deprecate] confirmed |
| Reactivate framework | BR-COMP-002E: DEPRECATED → ACTIVE | `PUT /api/admin/.../reactivate` | [Reactivate] confirmed |
| Delete draft | BR-COMP-002D: Remove draft | `DELETE /api/admin/compliance/frameworks/{id}` | [Delete] confirmed |
| Configure tier access | BR-COMP-001: Set plan access | `PUT /api/admin/.../access` | Plan toggle click |
| Add/edit control | BR-COMP-002F: Manage controls | `POST/PUT /api/admin/.../controls/...` | Modal submit |
| Add/edit mapping | BR-COMP-002F: Manage mappings | `POST/PUT /api/admin/.../mappings/...` | Modal submit |
| Review request | BR-COMP-011B: Status transition | `PUT /api/admin/compliance/requests/{id}` | [Update Status] click |

### System-Triggered UI Updates

| Backend Event | Frontend Effect | How Applied |
|--------------|----------------|-------------|
| Scan completes → assessment auto-generated | New assessment appears in dashboard + framework detail | Next dashboard/detail load shows new data |
| Framework published/deprecated (changelog) | Tenant sees framework status change | Next framework list load reflects new status |
| Plan change (upgrade) | New frameworks may become available | Feature store + framework list refresh |
| Plan change (downgrade) | Some frameworks may become unavailable | Selections preserved, no new assessments |

---

## State Management Notes

### What State to Track

| State | Scope | Persistence |
|-------|-------|-------------|
| Available frameworks | Dashboard page | Memory. Fetch on page load. |
| Selections | Dashboard page | Memory. Refresh on select/deselect. |
| Latest assessments | Dashboard page | Memory. Fetch on page load. |
| Framework detail | Framework detail page | Memory. Fetch on page load. |
| Assessment detail | Assessment detail page | Memory. Cache by ID (immutable). |
| Trend data | Framework detail page | Memory. Fetch on page load. |
| Framework requests (tenant) | Dashboard page | Memory. Refresh on submit. |
| Status filter (assessment detail) | Assessment detail page | Component state. Default: ALL. |
| Expanded control ID | Assessment detail page | Component state. One at a time. |
| Admin framework list | Admin page | Memory. Refresh on CRUD. |
| Admin framework detail | Admin detail page | Memory. Refresh on changes. |
| Admin control expanded ID | Admin detail page | Component state. One at a time. |
| Admin active tab | Admin detail page | Component state. Default: controls. |
| Admin requests | Admin requests page | Memory. Refresh on review. |
| Admin status filter | Admin list/requests pages | Component state. Default: ALL. |

### Reset Behavior

| Event | State Reset |
|-------|-------------|
| Select/deselect framework | Refresh selections + latest assessments |
| Report downloaded | No state change (file download) |
| Request submitted | Refresh requests list |
| Admin framework created | Navigate to detail, refresh list on return |
| Admin control/mapping CRUD | Refresh framework detail |
| Admin tier access toggled | Update toggle state (auto-save) |
| Admin publish/deprecate/reactivate | Refresh framework state + action buttons |
| Admin delete draft | Navigate back, refresh list |
| Admin request reviewed | Refresh request list |
| Navigation away | Clear page-level state. Keep cached assessment data. |
| Logout | Clear all compliance state |

### Unsaved Changes Guard

| Page | Guard Behavior |
|------|---------------|
| Admin create framework form | If form has values and user navigates away: "Discard changes?" dialog |
| Control/mapping modals | If form has changes and user clicks outside or ✕: "Discard changes?" dialog |
| All other compliance pages | No guard needed (no local edits to lose) |
