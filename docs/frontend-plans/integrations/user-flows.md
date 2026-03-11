# User Flows (Integrations)

Scope: User journeys for tenant notification integration management (CRUD, rules, history, testing) and super admin platform API key management, usage tracking, and provider configuration.

---

## Preconditions

| Condition | Required For | Check |
|-----------|-------------|-------|
| Authenticated | All flows | Valid session token |
| Tenant Owner role | Integration CRUD, rules, testing | `user.role === 'TENANT_OWNER'` |
| Super Admin role | Admin integrations panel | `user.role === 'SUPER_ADMIN'` |
| Channel feature flag enabled | Creating specific channel type | Feature store check per channel |
| Active integration count < tier limit | Creating/enabling integrations | Count check against tier limit |

---

## Flow 1: Integration List — Page Load

```
[User] → navigate to /settings/integrations
    │ (analytics: integrations_page_viewed)
    ▼
FETCH parallel:
  GET /api/integrations
  GET /api/integrations/limits
    │
    ├─ Loading → Skeleton: 3 integration cards
    ├─ Network error → Error state with [Retry]
    └─ Success → Render integration list
         │
         ├─ 0 integrations → Empty state:
         │     "No integrations configured."
         │     "Set up notifications to stay informed about scans, vulnerabilities, and compliance."
         │     [Add Integration]
         │
         └─ Integrations exist → Render:
               ├─ Active count: "{N} of {limit} active" (or "{N} active" for Enterprise)
               ├─ Integration cards (sorted by type, then created date)
               │     ├─ Channel icon + name + status badge (enabled/disabled)
               │     ├─ IF role = TENANT_OWNER → Quick actions: [Test] [Edit] [Delete]
               │     │     + Enable/Disable toggle
               │     └─ IF role = TENANT_MEMBER → (view-only, no actions)
               └─ [Add Integration] button
                     └─ IF at limit → Button disabled, tooltip: "Integration limit reached"
```

---

## Flow 2: Add Integration — Channel Selection

```
[Tenant Owner] → click [Add Integration]
    │ (analytics: integration_create_started)
    ▼
OPEN modal: "Add Integration" — Step 1: Select Channel
  ├─ Email          ● Available (Starter+)
  ├─ Slack          ● Available / 🔒 Requires Pro
  ├─ Jira           ● Available / 🔒 Requires Enterprise
  ├─ Webhook        ● Available / 🔒 Requires Enterprise
  ├─ SIEM           ● Available / 🔒 Requires Enterprise
  └─ Custom API     🔒 Coming Soon [POST-MVP]
    │
    ▼
[User] → select available channel
    ├─ Locked channel → Toast: "Upgrade to {tier} to use {channel}." + [View Plans]
    └─ Available channel → Advance to Step 2: Configure
```

---

## Flow 3: Add Integration — Email Configuration

```
[User] → selected Email in Step 1
    │
    ▼
SHOW config form:
  ├─ Integration Name (optional, default "Email")
  ├─ Recipients * (email input, add multiple, max 10)
  │     ├─ Type email → press Enter or comma → adds chip
  │     ├─ Invalid email → Inline error: "Invalid email address."
  │     └─ >10 recipients → Inline error: "Maximum 10 recipients."
  ├─ [Cancel] [Create Integration]
    │
    ▼
[User] → fill form → click [Create Integration]
    │ (analytics: integration_created, { type: "EMAIL" })
    ▼
VALIDATE: ≥1 recipient, all valid email format
    ├─ Invalid → Focus first invalid field
    └─ Valid →
         POST /api/integrations
           body: { type: "EMAIL", config_json: { recipients: [...] } }
           │
           ├─ Loading → Disable [Create Integration], show spinner
           ├─ Success → Close modal, refresh list, toast: "Email integration created."
           ├─ ERR_INT_011 (403) → Toast: "Integration limit reached for your plan."
           ├─ ERR_INT_012 (400) → Inline errors on relevant fields
           └─ Network error → Toast (error) + re-enable button
```

---

## Flow 4: Add Integration — Slack Configuration

```
[User] → selected Slack in Step 1
    │
    ▼
SHOW config form:
  ├─ Integration Name (optional, default "Slack")
  ├─ Webhook URL * (must be HTTPS)
  │     ├─ Placeholder: "https://hooks.slack.com/services/..."
  │     └─ On blur: validate HTTPS URL format
  ├─ [Cancel] [Create Integration]
    │
    ▼
[User] → fill form → click [Create Integration]
    │ (analytics: integration_created, { type: "SLACK" })
    ▼
POST /api/integrations
  body: { type: "SLACK", config_json: { webhook_url: "..." } }
    │
    ├─ Success → Close modal, refresh list, toast: "Slack integration created."
    ├─ ERR_INT_010 (403) → Toast: "Slack is not available on your plan."
    ├─ ERR_INT_012 (400) → Inline error: "Webhook URL must be a valid HTTPS URL."
    └─ (same error handling pattern as Flow 3)
```

---

## Flow 5: Add Integration — Jira Configuration

```
[User] → selected Jira in Step 1
    │
    ▼
SHOW config form:
  ├─ Integration Name (optional, default "Jira")
  ├─ Instance URL * (must be HTTPS)
  │     Placeholder: "https://yourteam.atlassian.net"
  ├─ API Token * (password field)
  ├─ Project Key * (e.g., "SEC")
  ├─ Issue Type * (dropdown: Bug, Task, Story, Epic)
  ├─ [Cancel] [Create Integration]
    │
    ▼
[User] → fill form → click [Create Integration]
    │ (analytics: integration_created, { type: "JIRA" })
    ▼
POST /api/integrations
  body: { type: "JIRA", config_json: { instance_url, api_token, project_key, issue_type } }
    │
    ├─ Success → Close modal, refresh list, toast: "Jira integration created."
    ├─ ERR_INT_012 (400) → Inline errors per field
    └─ (same error handling pattern)
```

---

## Flow 6: Add Integration — Webhook Configuration

```
[User] → selected Webhook in Step 1
    │
    ▼
SHOW config form:
  ├─ Integration Name (optional, default "Webhook")
  ├─ Endpoint URL * (must be HTTPS)
  │     Placeholder: "https://your-server.com/webhooks/reconova"
  ├─ Info: "A webhook secret will be auto-generated for HMAC verification."
  ├─ [Cancel] [Create Integration]
    │
    ▼
[User] → fill form → click [Create Integration]
    │ (analytics: integration_created, { type: "WEBHOOK" })
    ▼
POST /api/integrations
  body: { type: "WEBHOOK", config_json: { endpoint_url: "..." } }
    │
    ├─ Success →
    │     Close modal, refresh list
    │     Show webhook secret in a one-time display modal:
    │       "Your webhook secret has been generated."
    │       [████████████████████████████████] [Copy]
    │       "Save this secret — it won't be shown again in full."
    │     Toast: "Webhook integration created."
    └─ (same error handling pattern)
```

---

## Flow 7: Add Integration — SIEM Configuration

```
[User] → selected SIEM in Step 1
    │
    ▼
SHOW config form:
  ├─ Integration Name (optional, default "SIEM")
  ├─ Syslog Host * (hostname or IP)
  ├─ Syslog Port * (1-65535, default 514)
  ├─ Protocol * (radio: TCP / UDP / TLS)
  │     Info: "UDP has no delivery guarantee. Use TCP or TLS for reliable delivery."
  ├─ Format * (radio: SYSLOG / CEF)
  ├─ [Cancel] [Create Integration]
    │
    ▼
[User] → fill form → click [Create Integration]
    │ (analytics: integration_created, { type: "SIEM" })
    ▼
POST /api/integrations
  body: { type: "SIEM", config_json: { syslog_host, syslog_port, protocol, format } }
    │
    ├─ Success → Close modal, refresh list, toast: "SIEM integration created."
    ├─ ERR_INT_012 (400) → Inline errors (invalid host, port out of range, etc.)
    └─ (same error handling pattern)
```

---

## Flow 8: Edit Integration

```
[Tenant Owner] → click [Edit] on integration card
    │ (analytics: integration_edit_started, { integration_id, type })
    ▼
OPEN modal: "Edit {Channel} Integration" (pre-filled with current config)
  ├─ Same fields as create form for that channel type
  ├─ IF type = WEBHOOK AND endpoint_url changed:
  │     Warning: "Changing the endpoint URL will regenerate your webhook secret."
  ├─ [Cancel] [Save Changes]
    │
    ▼
[User] → modify fields → click [Save Changes]
    │ (analytics: integration_updated, { integration_id, type })
    ▼
PUT /api/integrations/{id}
  body: { config_json: { ... } }
    │
    ├─ Loading → Disable [Save Changes], show spinner
    ├─ Success →
    │     Close modal, refresh integration card
    │     IF webhook secret regenerated → Show new secret display modal
    │     Toast: "Integration updated."
    ├─ ERR_INT_009 (404) → Toast: "Integration not found." → Refresh list
    ├─ ERR_INT_012 (400) → Inline errors
    └─ Network error → Toast (error) + re-enable button
```

---

## Flow 9: Delete Integration

```
[Tenant Owner] → click [Delete] on integration card
    │ (analytics: integration_delete_started, { integration_id, type })
    ▼
SHOW confirmation modal:
  "Delete {channel} integration?"
  "All notification rules for this integration will be removed.
   Delivery history will be preserved but unlinked."
  [Cancel] [Delete]
    │
    ▼
[User] → click [Delete]
    │ (analytics: integration_deleted, { integration_id, type })
    ▼
DELETE /api/integrations/{id}
    │
    ├─ Loading → Disable [Delete], show spinner
    ├─ Success → Close modal, refresh list, update active count, toast: "Integration deleted."
    ├─ ERR_INT_009 (404) → Toast: "Integration not found." → Refresh list
    └─ Network error → Toast (error) + re-enable button
```

---

## Flow 10: Enable/Disable Integration

```
[Tenant Owner] → click enable/disable toggle on integration card
    │ (analytics: integration_toggled, { integration_id, type, enabled })
    ▼
IF enabling:
  CHECK active count < tier limit
    ├─ At limit → Toast: "Integration limit reached. Disable another integration first." → Revert toggle
    └─ Under limit → CONTINUE
         │
         ▼
       PUT /api/integrations/{id}/toggle
         body: { enabled: true }
           │
           ├─ Success → Update card status, update active count
           │     Toast: "Integration enabled."
           ├─ ERR_INT_010 (403) → Toast: "This channel is not available on your plan." → Revert toggle
           ├─ ERR_INT_011 (403) → Toast: "Integration limit reached." → Revert toggle
           └─ Network error → Toast (error) + revert toggle

IF disabling:
  PUT /api/integrations/{id}/toggle
    body: { enabled: false }
      │
      ├─ Success → Update card status, update active count
      │     Toast: "Integration disabled. Notifications will not be sent."
      └─ Network error → Toast (error) + revert toggle
```

---

## Flow 11: Test Integration

```
[Tenant Owner] → click [Test] on integration card
    │ (analytics: integration_test_started, { integration_id, type })
    ▼
POST /api/integrations/{id}/test
    │
    ├─ Loading → Replace [Test] with spinner, show "Testing..."
    ├─ Success (delivered) →
    │     Toast (success): "Test notification sent successfully."
    │     (analytics: integration_test_succeeded, { integration_id, type })
    │
    ├─ Success (failed delivery) →
    │     Toast (error): "Test failed: {error_message}"
    │     e.g., "HTTP 403 Forbidden", "Connection timeout", "Invalid webhook URL"
    │     (analytics: integration_test_failed, { integration_id, type, error })
    │
    ├─ ERR_INT_009 (404) → Toast: "Integration not found." → Refresh list
    └─ Network error → Toast (error) + restore button
```

---

## Flow 12: Configure Notification Rules (Inline)

```
[Tenant Owner] → expand integration card → click "Rules" tab
    │ (analytics: integration_rules_viewed, { integration_id })
    ▼
FETCH GET /api/integrations/{id}/rules
    │
    ├─ Loading → Skeleton checkboxes
    └─ Success → Render event type list with toggles:
         │
         │  ┌─────────────────────────────────────────────────────┐
         │  │ Event Type              │ Enabled │ Severity Filter │
         │  ├─────────────────────────┼─────────┼─────────────────┤
         │  │ Scan Complete           │ [✓]     │ All             │
         │  │ Scan Failed             │ [✓]     │ All             │
         │  │ CVE Alert (Critical)    │ [✓]     │ CRITICAL only   │
         │  │ CVE Alert (High Digest) │ [ ]     │ —               │
         │  │ Credit Low              │ [ ]     │ —               │
         │  │ Compliance Report Ready │ [✓]     │ All             │
         │  │ Compliance Score Change │ [ ]     │ —               │
         │  └─────────────────────────┴─────────┴─────────────────┘
         │
         └─ [Add Rule] for unconfigured event types

[User] → toggle an event type ON
    │ (analytics: notification_rule_created, { integration_id, event_type })
    ▼
POST /api/integrations/{id}/rules
  body: { event_type: "SCAN_COMPLETE", severity_filter: null, enabled: true }
    │
    ├─ Success → Update toggle state, toast: "Rule added."
    ├─ ERR_INT_014 (409) → Toast: "Rule already exists." → Refresh rules
    ├─ ERR_INT_015 (400) → Toast: "Maximum rules reached for your plan."
    ├─ ERR_INT_016 (400) → Toast: "Maximum 20 rules per integration."
    └─ Network error → Toast (error) + revert toggle

[User] → toggle an event type OFF
    │ (analytics: notification_rule_deleted, { integration_id, event_type })
    ▼
DELETE /api/integrations/{id}/rules/{rule_id}
    │
    ├─ Success → Update toggle state, toast: "Rule removed."
    └─ Network error → Toast (error) + revert toggle

[User] → click severity filter dropdown on a rule
    │
    ▼
SHOW severity multi-select: [CRITICAL] [HIGH] [WARNING] [INFO]
  ├─ All selected = severity_filter: null (all severities)
  ├─ Subset selected = severity_filter: ["CRITICAL", "HIGH"]
    │
    ▼
PUT /api/integrations/{id}/rules/{rule_id}
  body: { severity_filter: ["CRITICAL", "HIGH"] }
    │
    ├─ Success → Update filter display
    └─ Network error → Toast (error) + revert
```

---

## Flow 13: View Notification History (Per-Integration)

```
[Tenant Owner/Member] → expand integration card → click "History" tab
    │ (analytics: integration_history_viewed, { integration_id })
    ▼
FETCH GET /api/integrations/{id}/history?page=1&per_page=20
    │
    ├─ Loading → Skeleton table
    ├─ Error → Error state with [Retry]
    └─ Success → Render delivery history table:
         │
         │  ┌─────────────────────┬──────────┬───────────┬──────────┐
         │  │ Event               │ Status   │ Sent At   │ Error    │
         │  ├─────────────────────┼──────────┼───────────┼──────────┤
         │  │ Scan Complete       │ DELIVERED│ 2m ago    │ —        │
         │  │ CVE Alert Critical  │ DELIVERED│ 1h ago    │ —        │
         │  │ Scan Failed         │ FAILED   │ 3h ago    │ Timeout  │
         │  │ Test                │ DELIVERED│ 1d ago    │ —        │
         │  └─────────────────────┴──────────┴───────────┴──────────┘
         │
         ├─ Status badges: DELIVERED (green), RETRY_* (yellow), FAILED (red), PENDING (grey)
         ├─ Click row → expand to show full payload + error detail
         └─ Pagination controls
         │
         └─ 0 history → "No notifications sent yet."
```

---

## Flow 14: View Webhook Secret

```
[Tenant Owner] → expand webhook integration → click "Configuration" tab
    │
    ▼
SHOW config with masked secret:
  ├─ Endpoint URL: https://example.com/webhooks/reconova
  ├─ Webhook Secret: ••••••••••••••••••••••••  [Reveal] [Regenerate]
    │
    ├─ [Reveal] → GET /api/integrations/{id}/secret
    │     ├─ Success → Show full secret for 30 seconds, then re-mask
    │     │     [████████████████████████████████] [Copy]
    │     └─ Error → Toast (error)
    │
    └─ [Regenerate] → Confirmation: "Regenerate webhook secret? Your endpoint will need to be updated."
         ├─ [Cancel] → Close
         └─ [Regenerate] →
              POST /api/integrations/{id}/regenerate-secret
                │
                ├─ Success → Show new secret display, toast: "Webhook secret regenerated."
                └─ Error → Toast (error)
```

---

## Flow 15: Super Admin — Admin Integrations Panel Load

```
[Super Admin] → navigate to /admin/integrations
    │ (analytics: admin_integrations_viewed)
    ▼
DEFAULT to API Keys tab
    │
FETCH GET /api/admin/integrations/keys
    │
    ├─ Loading → Skeleton table grouped by provider
    ├─ Error → Error state with [Retry]
    └─ Success → Render key pool grouped by provider:
         │
         ├─ Provider section: "Shodan" (2 active keys, 1 retired)
         │     ├─ Key table: ID (masked), status, quota usage, last used
         │     └─ Actions: [Add Key] [Rotate] [Disable/Enable]
         │
         ├─ Provider section: "SecurityTrails" (1 active key)
         │     └─ ...
         │
         ├─ Quota indicators:
         │     ├─ usage < 80% → Green bar
         │     ├─ usage 80-99% → Yellow bar + "Approaching quota" warning
         │     └─ usage = 100% → Red bar + "Quota exhausted" badge
         │
         └─ Provider with 0 active keys → "No active keys" warning + [Add Key]
```

---

## Flow 16: Super Admin — Add Platform API Key

```
[Super Admin] → click [Add Key] on a provider section
    │ (analytics: admin_api_key_create_started, { provider })
    ▼
OPEN modal: "Add API Key — {Provider}"
  ├─ API Key * (password field)
  ├─ Rate Limit * (calls per rate window, provider-specific)
  ├─ Monthly Quota * (max calls per month)
  ├─ [Cancel] [Add Key]
    │
    ▼
[Super Admin] → fill form → click [Add Key]
    │ (analytics: admin_api_key_created, { provider })
    ▼
POST /api/admin/integrations/keys
  body: { provider, api_key, rate_limit, monthly_quota }
    │
    ├─ Loading → Disable button, show spinner
    ├─ Success → Close modal, refresh key table, toast: "API key added."
    ├─ ERR_INT_001 (400) → Toast: "Unknown provider."
    └─ Network error → Toast (error) + re-enable button
```

---

## Flow 17: Super Admin — Rotate API Key

```
[Super Admin] → click [Rotate] on a key row
    │ (analytics: admin_api_key_rotate_started, { key_id, provider })
    ▼
OPEN modal: "Rotate API Key"
  ├─ Current key: ••••••{last4} (status: {status})
  ├─ "The current key will be retired and replaced with the new key."
  ├─ New API Key * (password field)
  ├─ [Cancel] [Rotate Key]
    │
    ▼
[Super Admin] → enter new key → click [Rotate Key]
    │ (analytics: admin_api_key_rotated, { old_key_id, provider })
    ▼
POST /api/admin/integrations/keys/{id}/rotate
  body: { new_api_key }
    │
    ├─ Loading → Disable button, show spinner
    ├─ Success →
    │     Close modal, refresh table
    │     Old key shows RETIRED status
    │     New key appears with ACTIVE status
    │     Toast: "API key rotated successfully."
    ├─ ERR_INT_002 (404) → Toast: "Key not found." → Refresh table
    └─ Network error → Toast (error) + re-enable button
```

---

## Flow 18: Super Admin — Disable/Enable API Key

```
[Super Admin] → click [Disable] on an ACTIVE key row
    │ (analytics: admin_api_key_disabled, { key_id, provider })
    ▼
SHOW confirmation: "Disable this API key? It will no longer be used for scans."
  [Cancel] [Disable]
    │
    └─ [Disable] →
         PUT /api/admin/integrations/keys/{id}
           body: { status: "DISABLED" }
           │
           ├─ Success → Update key status badge, toast: "Key disabled."
           │     CHECK: any active keys remaining for this provider?
           │     └─ None → Show "No active keys" warning on provider section
           └─ Network error → Toast (error)

[Super Admin] → click [Enable] on a DISABLED key row
    │ (analytics: admin_api_key_enabled, { key_id, provider })
    ▼
PUT /api/admin/integrations/keys/{id}
  body: { status: "ACTIVE" }
    │
    ├─ Success → Update key status badge, toast: "Key enabled."
    └─ Network error → Toast (error)
```

---

## Flow 19: Super Admin — Usage Dashboard

```
[Super Admin] → click "Usage Dashboard" tab
    │ (analytics: admin_usage_dashboard_viewed)
    ▼
FETCH GET /api/admin/integrations/usage?period=month
    │
    ├─ Loading → Skeleton cards + table
    ├─ Error → Error state with [Retry]
    └─ Success → Render usage dashboard:
         │
         ├─ Summary cards: Total calls today, Total calls this month
         │
         ├─ Usage by provider (table or bar chart):
         │     ├─ Shodan: 12,450 calls (62% of quota)
         │     ├─ SecurityTrails: 5,200 calls (52% of quota)
         │     └─ Censys: 890 calls (9% of quota)
         │
         ├─ Top consumers by tenant (table):
         │     ├─ Tenant A: 3,200 calls
         │     ├─ Tenant B: 2,800 calls
         │     └─ ...
         │
         └─ Date range filter: [Today] [This Week] [This Month] [Custom]

[Super Admin] → change date range
    │ (analytics: admin_usage_filter_changed, { period })
    ▼
FETCH GET /api/admin/integrations/usage?period={period}&from={date}&to={date}
    │
    ├─ Loading → Table loading overlay
    └─ Success → Update charts/tables
```

---

## Flow 20: Super Admin — Provider Rate Limit Config

```
[Super Admin] → click "Provider Config" tab
    │ (analytics: admin_provider_config_viewed)
    ▼
FETCH GET /api/admin/integrations/providers
    │
    ├─ Loading → Skeleton table
    └─ Success → Render provider config table:
         │
         │  ┌────────────────┬────────────┬────────────┬──────────┐
         │  │ Provider       │ Calls/Hour │ Calls/Day  │ Status   │
         │  ├────────────────┼────────────┼────────────┼──────────┤
         │  │ Shodan         │ 50         │ 500        │ Active   │
         │  │ SecurityTrails │ 30         │ 300        │ Active   │
         │  │ Censys         │ 30         │ 300        │ Active   │
         │  │ VirusTotal     │ 20         │ 200        │ POST-MVP │
         │  └────────────────┴────────────┴────────────┴──────────┘
         │
         └─ Per-row: [Edit] (except POST-MVP providers)

[Super Admin] → click [Edit] on provider row
    │ (analytics: admin_provider_config_edit_started, { provider })
    ▼
OPEN modal: "Edit Rate Limits — {Provider}"
  ├─ Calls per tenant per hour * (min 1)
  ├─ Calls per tenant per day * (min 1, must be ≥ calls/hour)
  ├─ [Cancel] [Save]
    │
    ▼
[Super Admin] → modify values → click [Save]
    │ (analytics: admin_provider_config_updated, { provider })
    ▼
PUT /api/admin/integrations/providers/{slug}
  body: { tenant_calls_per_hour, tenant_calls_per_day }
    │
    ├─ Success → Close modal, refresh table, toast: "Rate limits updated."
    └─ Network error → Toast (error) + re-enable button
```

---

## Flow 21: Super Admin — Reset Key Quota

```
[Super Admin] → click [Reset Quota] on a QUOTA_EXHAUSTED key
    │ (analytics: admin_api_key_quota_reset, { key_id, provider })
    ▼
SHOW confirmation: "Reset quota for this key? Usage count will be set to 0."
  [Cancel] [Reset Quota]
    │
    └─ [Reset Quota] →
         POST /api/admin/integrations/keys/{id}/reset-quota
           │
           ├─ Success →
           │     Update key: status → ACTIVE, usage_count → 0
           │     Toast: "Quota reset. Key is now active."
           └─ Network error → Toast (error)
```

---

## Flow 22: Downgraded Tenant — Auto-Disabled Integrations

```
[User] → navigates to /settings/integrations after plan downgrade
    │
    ▼
CHECK: were integrations auto-disabled?
    ├─ Yes → Show info banner:
    │     "Some integrations were disabled due to your plan change.
    │      Your {channel} integrations are no longer available on {new_plan}."
    │     [Upgrade Plan]
    │     Disabled integrations show "Disabled (plan change)" badge
    │     Config preserved but not editable until re-enabled (requires upgrade)
    │
    └─ No → Normal view (Flow 1)
```
