# User Flows (Feature Flags & Access Control)

Scope: User journeys for tenant feature visibility, module-level gating interactions, super admin feature management, operational flag toggles, and tenant override administration.

---

## Preconditions

| Flow Group | Auth State | Role | Data Requirements |
|-----------|------------|------|-------------------|
| View features | Authenticated | Tenant Owner / Member | Active tenant, subscription exists |
| Upgrade prompt interaction | Authenticated | Any tenant member | Feature store loaded |
| Admin feature overview | Authenticated | Super Admin | Feature flags seeded |
| Admin operational flags | Authenticated | Super Admin | Operational flags exist |
| Admin tenant overrides | Authenticated | Super Admin | Target tenant exists |

---

## Flow 1: App Load — Feature Store Initialization

**Trigger:** Every authenticated page load (runs once per session, refreshes on plan change)

```
[User logs in / session starts]
    │
    ▼
API: GET /api/features
    │ (analytics: features_loaded)
    │
    ├─ Success → store feature map in memory
    │   │ Response: { features: [{ name, module, enabled, reason, required_tier, current_tier }] }
    │   │
    │   │ Build feature store:
    │   │   feature_name → { enabled, reason, required_tier }
    │   │
    │   │ Reason values:
    │   │   "plan"                → feature included in current tier
    │   │   "operational_disabled" → global operational flag off
    │   │   "override"            → super admin override active
    │   │   "default"             → no plan/override data, using default
    │   │
    │   └─► App renders with gating applied
    │
    ├─ API error (network / 500)
    │   └─► Feature store defaults to all-locked (fail-safe)
    │       Log error, show degraded UI
    │       Retry on next navigation
    │
    └─ 401 Unauthorized
        └─► Redirect to login (session expired)
```

### Feature Store Refresh Triggers

```
Feature store re-fetches GET /api/features when:
    ├─ Plan change confirmed (upgrade/downgrade completes)
    ├─ User returns from billing checkout (Stripe redirect)
    ├─ Session refresh / token renewal
    └─ Manual refresh (pull-to-refresh on mobile, F5)
```

---

## Flow 2: Tenant — View Plan & Features Page

**Entry points:**
- Navigates to Settings > Plan & Features
- Clicks "View all features" from upgrade modal
- Clicks locked feature indicator's "See all features" link

```
[User navigates to /settings/features]
    │ (analytics: plan_features_viewed)
    ▼
API: GET /api/features (uses cached feature store)
    │
    ├─ Loading → skeleton cards grouped by module
    │
    ├─ Success → render feature list
    │   │
    │   │ Features grouped by module:
    │   │   ┌─ Scanning (7 features)
    │   │   ├─ Compliance (2 features)
    │   │   ├─ Integrations (4 features)
    │   │   ├─ Notifications (4 features)
    │   │   └─ Monitoring (1 feature)
    │   │
    │   │ Each feature row shows:
    │   │   Feature name | Status badge | Required tier (if locked)
    │   │
    │   │ Status badges:
    │   │   ├─ ✓ "Included"        (enabled, reason=plan)
    │   │   ├─ 🔒 "Requires {tier}" (locked, reason=plan)
    │   │   ├─ ⚠ "Unavailable"     (disabled, reason=operational_disabled)
    │   │   └─ ✓ "Enabled"         (enabled, reason=override — shown same as plan)
    │   │
    │   │ Bottom section:
    │   │   Current plan: {tier} | [Upgrade Plan] → /settings/billing/plans
    │   │
    │   └─► User can browse but cannot toggle anything
    │
    └─ Empty state (no features returned)
        └─► "No feature data available. Contact support."
            (analytics: features_empty_state)
```

---

## Flow 3: Module-Level Gating — Locked Feature Encounter

**Trigger:** User navigates to or interacts with a gated module/action

```
[User interacts with gated feature]
    │ Examples:
    │   • Clicks "Vulnerability Scanning" in scan step selector
    │   • Navigates to /compliance
    │   • Clicks "Connect Shodan" in integrations
    │   • Clicks "Configure Slack" in notifications
    │
    ▼
Frontend checks feature store: isEnabled(feature_name)
    │
    ├─ enabled: true → proceed normally
    │
    ├─ enabled: false, reason: "plan" (tier-locked)
    │   │ (analytics: locked_feature_clicked, {feature_name, current_tier, required_tier})
    │   ▼
    │ [Upgrade Modal]
    │   │ Title: "Upgrade to unlock {Feature Name}"
    │   │ Body: "{Feature description from store}"
    │   │ Shows: "Available on: {required_tier} and above"
    │   │ Shows: "Your plan: {current_tier}"
    │   │ CTA: [View Plans] → /settings/billing/plans
    │   │      [See All Features] → /settings/features
    │   │      [✕ Close]
    │   │
    │   ├─ Clicks [View Plans]
    │   │   │ (analytics: upgrade_cta_clicked, {feature_name, source: "upgrade_modal"})
    │   │   └─► Navigate to /settings/billing/plans
    │   │
    │   ├─ Clicks [See All Features]
    │   │   └─► Navigate to /settings/features
    │   │
    │   └─ Clicks [Close] / clicks outside modal
    │       └─► Modal closes, user stays on current page
    │
    └─ enabled: false, reason: "operational_disabled"
        │ (analytics: disabled_feature_accessed, {feature_name})
        ▼
      [Operational Disabled Message]
          │ Inline banner (not modal):
          │   "This feature is temporarily unavailable for maintenance."
          │   "No action required — it will be restored automatically."
          │ No upgrade CTA shown (not a plan issue)
          └─► User stays on page, feature area greyed out
```

---

## Flow 4: Gating in Scan Creation (Cross-Reference)

**Context:** Integrates with Scanning frontend plan. Documented here for feature flag specifics.

```
[Scan Creation → Step Selection]
    │ User selects workflow steps for scan
    │
    ▼
For each scan step type, check feature store:
    │
    ├─ subdomain_enumeration: isEnabled?
    ├─ port_scanning: isEnabled?
    ├─ technology_detection: isEnabled?
    ├─ screenshot_capture: isEnabled?
    ├─ vulnerability_scanning: isEnabled?
    ├─ custom_workflows: isEnabled?
    └─ scheduled_scans: isEnabled?
        │
        ├─ Enabled steps → shown as selectable checkboxes
        │
        ├─ Locked steps (plan) → shown with lock icon + "Requires {tier}"
        │   │ Clicking locked step → opens Upgrade Modal (Flow 3)
        │   │ Cannot be selected for scan
        │   │ (analytics: locked_scan_step_clicked, {step_type, required_tier})
        │
        └─ Disabled steps (operational) → hidden entirely
            │ Tooltip on info icon: "{step} is temporarily unavailable"
```

---

## Flow 5: Gating in Navigation Sidebar

**Trigger:** Sidebar renders on every authenticated page

```
[App sidebar renders]
    │
    ▼
For each nav module, check feature store:
    │
    ├─ Scanning → always visible (individual steps gated internally)
    │
    ├─ Compliance → gated by compliance_checks
    │   ├─ enabled → normal nav link
    │   └─ locked → nav link with lock icon, click → Upgrade Modal
    │
    ├─ CVE Monitoring → gated by cve_monitoring
    │   ├─ enabled → normal nav link
    │   └─ locked → nav link with lock icon, click → Upgrade Modal
    │
    ├─ Integrations → always visible (individual integrations gated internally)
    │
    └─ Notifications → always visible (individual channels gated internally)

Note: Locked nav items are visible (not hidden) to drive upgrade discovery.
```

---

## Flow 6: Post-Downgrade Feature Loss Experience

**Trigger:** Tenant downgrades and loses access to features they previously used

```
[Plan downgrade takes effect]
    │ Feature store refreshes → some features now locked
    │
    ▼
[Next page load after downgrade]
    │
    ├─ Previously accessible modules now show lock indicators
    │
    ├─ Scheduled scans using locked steps:
    │   │ Backend has already disabled them (BR-FLAG-004)
    │   └─► Schedule list shows "Disabled — requires {tier}" status
    │       [Upgrade to re-enable] link
    │
    ├─ Custom workflows using locked features:
    │   │ Backend has locked them (preserved but non-executable)
    │   └─► Workflow list shows "Locked" badge
    │       Cannot execute, edit, or clone
    │       [Upgrade to unlock] link
    │
    ├─ Historical scan results with locked features:
    │   └─► Still viewable (read-only). Results not deleted.
    │       "These results were generated on your previous plan."
    │
    └─ Integration configs for locked integrations:
        └─► Config preserved, marked as "Inactive"
            "Upgrade to {tier} to reconnect."
```

---

## Flow 7: Super Admin — Feature Management Overview

**Entry point:** Admin Panel > Features

```
[Admin navigates to /admin/features]
    │ (analytics: admin_features_viewed)
    ▼
Parallel API calls:
    ├─ GET /api/admin/features (all flags + status)
    └─ GET /api/admin/features/operational (operational flag states)
        │
        ├─ Loading → skeleton table
        │
        └─ Success → render feature management page
            │
            │ Tab navigation:
            │   [Subscription Flags] [Operational Flags] [Tenant Overrides]
            │
            │ Default tab: Subscription Flags
            │   Table: Flag Name | Module | Description | Default | Tier Availability
            │   18 subscription flags grouped by module
            │   Each row shows which tiers include this flag
            │   Search/filter by module
            │   (analytics: admin_subscription_flags_viewed)
            │
            └─► Read-only overview (flags are plan-driven, not manually toggled)
```

---

## Flow 8: Super Admin — Toggle Operational Flag

**Preconditions:** Super Admin role, on operational flags tab

```
[Admin clicks Operational Flags tab]
    │ (analytics: admin_operational_flags_viewed)
    ▼
[Operational Flags Panel]
    │ 5 operational flags displayed as toggle cards:
    │   Flag Name | Module | Current Status (ON/OFF) | [Toggle]
    │
    └─ Clicks toggle on a flag (e.g., vuln_scanning_global)
        │ (analytics: admin_operational_toggle_initiated, {flag_name, current_state})
        ▼
    [Confirmation Modal]
        │ IF disabling:
        │   Title: "Disable {Flag Name}?"
        │   Body: "This will immediately affect ALL tenants."
        │   Impact: "{Feature description} will be unavailable platform-wide."
        │   Warning: "Active scans using this feature: current step completes,
        │            next steps will be skipped."
        │   CTA: [Disable for All Tenants]  [Cancel]
        │
        │ IF enabling:
        │   Title: "Enable {Flag Name}?"
        │   Body: "This will restore {feature} for all tenants (based on their plan)."
        │   CTA: [Enable]  [Cancel]
        │
        ├─ Clicks [Cancel] → modal closes, toggle unchanged
        │
        └─ Clicks [Confirm]
            │ (analytics: admin_operational_flag_toggled, {flag_name, new_state})
            ▼
        API: PUT /api/admin/features/operational/{flag_name} {enabled: true/false}
            │
            ├─ Success (200)
            │   ▼
            │ Toggle updates to new state
            │ Toast: "{Flag Name} is now {enabled/disabled}."
            │ Cache note: "All tenant feature caches invalidated."
            │
            ├─ ERR_FLAG_005 (insufficient permissions)
            │   └─► Error toast: "You don't have permission to manage flags."
            │
            └─ ERR_FLAG_001 (flag not found)
                └─► Error toast: "Feature flag not found. Refresh the page."
```

---

## Flow 9: Super Admin — View Tenant Overrides

**Preconditions:** Super Admin role, on tenant overrides tab

```
[Admin clicks Tenant Overrides tab]
    │ (analytics: admin_overrides_viewed)
    ▼
[Tenant Overrides Panel]
    │ [Search Tenant] input field
    │
    └─ Types tenant name/ID → search results dropdown
        │
        └─ Selects tenant
            │ (analytics: admin_override_tenant_selected, {tenant_id})
            ▼
        API: GET /api/admin/tenants/{id}/features
            │
            ├─ Loading → skeleton table
            │
            └─ Success → render tenant feature table
                │
                │ Shows: Tenant name | Current plan: {tier}
                │
                │ Table: Feature Name | Module | Plan Status | Override | Actions
                │
                │ Plan Status column:
                │   "Included" (plan grants access)
                │   "Not in plan" (plan doesn't grant access)
                │
                │ Override column:
                │   "—" (no override)
                │   "Enabled by {admin}" (override grants)
                │   "Disabled by {admin}" (override revokes)
                │
                │ Actions column:
                │   [Add Override] (if no override exists)
                │   [Edit] [Delete] (if override exists)
                │
                └─► Admin can manage overrides per feature
```

---

## Flow 10: Super Admin — Create Override

```
[Tenant Override Table] → clicks [Add Override] on a feature
    │ (analytics: admin_override_create_initiated, {tenant_id, feature_name})
    ▼
[Create Override Modal]
    │ Title: "Add Override for {Feature Name}"
    │ Tenant: {Tenant Name} (read-only)
    │ Feature: {Feature Name} — {Module} (read-only)
    │ Current plan status: "Included" / "Not in plan"
    │
    │ [Enable ○ / Disable ○] radio toggle
    │ Reason: [textarea] (required, min 10 characters)
    │   Placeholder: "Explain why this override is needed..."
    │
    │ CTA: [Create Override]  [Cancel]
    │
    ├─ Reason < 10 chars → [Create Override] disabled
    │   Inline hint: "Reason must be at least 10 characters"
    │
    ├─ Clicks [Cancel] → modal closes
    │
    └─ Clicks [Create Override]
        │ (analytics: admin_override_created, {tenant_id, feature_name, enabled, reason_length})
        ▼
    API: POST /api/admin/tenants/{tenant_id}/features/{feature_id}/override
         {enabled, reason}
        │
        ├─ Success (201)
        │   ▼
        │ Modal closes
        │ Toast: "Override created for {Feature Name}."
        │ Table refreshes — override column updated
        │
        ├─ ERR_FLAG_007 (operational flag cannot be overridden)
        │   └─► Error toast: "Operational flags cannot have tenant overrides."
        │
        ├─ ERR_FLAG_009 (override already exists)
        │   └─► Error toast: "An override already exists. Use edit instead."
        │
        ├─ ERR_FLAG_006 (reason too short)
        │   └─► Inline error on reason field: "Reason must be at least 10 characters."
        │
        └─ ERR_FLAG_008 (tenant not found)
            └─► Error toast: "Tenant not found."
                Close modal, clear tenant selection
```

---

## Flow 11: Super Admin — Edit Override

```
[Tenant Override Table] → clicks [Edit] on existing override
    │ (analytics: admin_override_edit_initiated, {tenant_id, feature_name})
    ▼
[Edit Override Modal]
    │ Title: "Edit Override for {Feature Name}"
    │ Tenant: {Tenant Name} (read-only)
    │ Feature: {Feature Name} (read-only)
    │ Current override: {Enabled/Disabled} by {admin_name} on {date}
    │
    │ [Enable ○ / Disable ○] radio (pre-selected to current value)
    │ Reason: [textarea] (pre-filled with current reason, editable)
    │
    │ CTA: [Save Changes]  [Cancel]
    │
    ├─ No changes made → [Save Changes] disabled
    │
    └─ Clicks [Save Changes]
        │ (analytics: admin_override_updated, {tenant_id, feature_name, new_enabled})
        ▼
    API: PUT /api/admin/tenants/{tenant_id}/features/{feature_id}/override
         {enabled, reason}
        │
        ├─ Success (200)
        │   ▼
        │ Modal closes
        │ Toast: "Override updated."
        │ Table refreshes
        │
        ├─ ERR_FLAG_010 (override not found)
        │   └─► Error toast: "Override no longer exists."
        │       Close modal, refresh table
        │
        └─ ERR_FLAG_006 (reason too short)
            └─► Inline error on reason field
```

---

## Flow 12: Super Admin — Delete Override

```
[Tenant Override Table] → clicks [Delete] on existing override
    │ (analytics: admin_override_delete_initiated, {tenant_id, feature_name})
    ▼
[Delete Confirmation Modal]
    │ Title: "Remove Override for {Feature Name}?"
    │ "This tenant will revert to their plan defaults for this feature."
    │ Current override: {Enabled/Disabled}
    │ Plan default: {Included/Not in plan}
    │ Result after removal: {Will have access / Will lose access}
    │
    │ CTA: [Remove Override]  [Cancel]
    │
    ├─ Clicks [Cancel] → modal closes
    │
    └─ Clicks [Remove Override]
        │ (analytics: admin_override_deleted, {tenant_id, feature_name})
        ▼
    API: DELETE /api/admin/tenants/{tenant_id}/features/{feature_id}/override
        │
        ├─ Success (200)
        │   ▼
        │ Modal closes
        │ Toast: "Override removed. Tenant reverted to plan defaults."
        │ Table refreshes — override column shows "—"
        │
        └─ ERR_FLAG_010 (override not found)
            └─► Error toast: "Override already removed."
                Refresh table
```

---

## Flow 13: Feature Store — Real-Time Plan Change Sync

**Trigger:** Tenant completes plan change via billing flow

```
[Billing checkout completes — plan upgraded/downgraded]
    │ Redirect back to app with status=success
    │
    ▼
[Feature store detects plan change]
    │ Trigger: URL param status=upgraded/downgraded
    │   OR: billing store emits plan_changed event
    │
    ▼
API: GET /api/features (force refresh, bypass cache)
    │ (analytics: features_refreshed, {trigger: "plan_change"})
    │
    ├─ Success → update feature store
    │   │
    │   ├─ Newly unlocked features:
    │   │   Toast: "New features unlocked with your {tier} plan!"
    │   │   Sidebar nav updates (lock icons removed)
    │   │
    │   └─ Newly locked features (downgrade):
    │       Toast: "Some features are no longer available on {tier}."
    │       Sidebar nav updates (lock icons added)
    │       If user is on a now-locked page → redirect to dashboard
    │
    └─ Error → keep previous feature store
        Retry on next navigation
```
