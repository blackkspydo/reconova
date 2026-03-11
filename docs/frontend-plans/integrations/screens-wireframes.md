# Screens & Wireframes (Integrations)

Scope: ASCII wireframes for tenant notification integration management (list, detail, config forms, rules, history) and super admin platform API key management, usage dashboard, and provider configuration.

---

## Route Structure

| Route | Access | Description |
|-------|--------|-------------|
| `/settings/integrations` | Tenant Owner (edit), Members (view) | Integration list + inline detail |
| `/admin/integrations` | Super Admin | Admin panel (tabbed: keys, usage, providers) |

---

## Screen 1: Integration List

**Route:** `/settings/integrations`
**Access:** Tenant Owner (full), Tenant Member (read-only)

### State 1A: Default — Integrations Exist

```
┌──────────────────────────────────────────────────────────────┐
│ Integrations                              [Add Integration]  │
├──────────────────────────────────────────────────────────────┤
│                                                              │
│  3 of 5 active integrations                                  │
│                                                              │
│  ┌────────────────────────────────────────────────────────┐  │
│  │ ✉ Email — "Scan Alerts"              ● Enabled        │  │
│  │ 2 recipients │ 3 rules active                          │  │
│  │                      [Test]  [Edit]  [Delete]  [○ ON]  │  │
│  └────────────────────────────────────────────────────────┘  │
│                                                              │
│  ┌────────────────────────────────────────────────────────┐  │
│  │ 💬 Slack — "Security Team"           ● Enabled        │  │
│  │ hooks.slack.com/... │ 5 rules active                   │  │
│  │                      [Test]  [Edit]  [Delete]  [○ ON]  │  │
│  └────────────────────────────────────────────────────────┘  │
│                                                              │
│  ┌────────────────────────────────────────────────────────┐  │
│  │ 🔗 Webhook — "CI Pipeline"           ● Enabled        │  │
│  │ api.example.com/... │ 2 rules active                   │  │
│  │                      [Test]  [Edit]  [Delete]  [○ ON]  │  │
│  └────────────────────────────────────────────────────────┘  │
│                                                              │
│  ┌────────────────────────────────────────────────────────┐  │
│  │ 🎫 Jira — "Vuln Tracker"            ○ Disabled        │  │
│  │ team.atlassian.net │ 0 rules active                    │  │
│  │                      [Test]  [Edit]  [Delete]  [○ OFF] │  │
│  └────────────────────────────────────────────────────────┘  │
│                                                              │
└──────────────────────────────────────────────────────────────┘
```

### State 1B: Empty — No Integrations

```
┌──────────────────────────────────────────────────────────────┐
│ Integrations                              [Add Integration]  │
├──────────────────────────────────────────────────────────────┤
│                                                              │
│                  ┌──────────────┐                             │
│                  │  🔔  (icon)  │                             │
│                  └──────────────┘                             │
│                                                              │
│            No integrations configured.                        │
│     Set up notifications to stay informed about               │
│     scans, vulnerabilities, and compliance.                   │
│                                                              │
│                 [Add Integration]                             │
│                                                              │
└──────────────────────────────────────────────────────────────┘
```

### State 1C: At Integration Limit

```
┌──────────────────────────────────────────────────────────────┐
│ Integrations                          [Add Integration 🔒]   │
├──────────────────────────────────────────────────────────────┤
│ ⚠ You've reached the integration limit for your plan (5/5). │
│   Disable or delete an integration to add a new one.         │
│                                       [Upgrade Plan]         │
├──────────────────────────────────────────────────────────────┤
│                                                              │
│  (Integration cards as State 1A)                             │
│                                                              │
└──────────────────────────────────────────────────────────────┘
```

### State 1D: Downgraded — Auto-Disabled Integrations

```
┌──────────────────────────────────────────────────────────────┐
│ Integrations                              [Add Integration]  │
├──────────────────────────────────────────────────────────────┤
│ ℹ Some integrations were disabled due to your plan change.   │
│   Slack integrations are not available on Starter plan.      │
│                                       [Upgrade Plan]         │
├──────────────────────────────────────────────────────────────┤
│                                                              │
│  ┌────────────────────────────────────────────────────────┐  │
│  │ ✉ Email — "Scan Alerts"              ● Enabled        │  │
│  │ ...                                                    │  │
│  └────────────────────────────────────────────────────────┘  │
│                                                              │
│  ┌────────────────────────────────────────────────────────┐  │
│  │ 💬 Slack — "Security Team"   ○ Disabled (plan change) │  │
│  │ 🔒 Not available on Starter plan                       │  │
│  │                                                        │  │
│  └────────────────────────────────────────────────────────┘  │
│                                                              │
└──────────────────────────────────────────────────────────────┘
```

### State 1E: Read-Only (Tenant Member)

```
┌──────────────────────────────────────────────────────────────┐
│ Integrations                                                 │
├──────────────────────────────────────────────────────────────┤
│                                                              │
│  3 active integrations                                       │
│                                                              │
│  ┌────────────────────────────────────────────────────────┐  │
│  │ ✉ Email — "Scan Alerts"              ● Enabled        │  │
│  │ 2 recipients │ 3 rules active                          │  │
│  │ (No action buttons — read-only)                        │  │
│  └────────────────────────────────────────────────────────┘  │
│                                                              │
│  (More integration cards without action buttons...)          │
│                                                              │
└──────────────────────────────────────────────────────────────┘
```

### State 1F: Loading

```
┌──────────────────────────────────────────────────────────────┐
│ Integrations                              [Add Integration]  │
├──────────────────────────────────────────────────────────────┤
│                                                              │
│  ┌────────────────────────────────────────────────────────┐  │
│  │  ░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░  │  │
│  └────────────────────────────────────────────────────────┘  │
│  ┌────────────────────────────────────────────────────────┐  │
│  │  ░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░  │  │
│  └────────────────────────────────────────────────────────┘  │
│  ┌────────────────────────────────────────────────────────┐  │
│  │  ░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░  │  │
│  └────────────────────────────────────────────────────────┘  │
│                                                              │
└──────────────────────────────────────────────────────────────┘
```

### State 1G: Error

```
┌──────────────────────────────────────────────────────────────┐
│ Integrations                                                 │
├──────────────────────────────────────────────────────────────┤
│                                                              │
│                Something went wrong.                         │
│           Unable to load integrations.                       │
│                                                              │
│                       [Retry]                                │
│                                                              │
└──────────────────────────────────────────────────────────────┘
```

### Conditional Rendering

| Condition | Effect |
|-----------|--------|
| `role = TENANT_MEMBER` | State 1E: no actions, no [Add Integration] |
| Active count = tier limit | State 1C: limit warning, [Add] disabled |
| Integrations auto-disabled by downgrade | State 1D: downgrade banner |
| 0 integrations | State 1B: empty state |

---

## Screen 2: Integration Detail (Expanded)

### State 2A: Configuration Tab

```
┌────────────────────────────────────────────────────────────┐
│ ✉ Email — "Scan Alerts"                     ● Enabled     │
│ ───────────────────────────────────────────────────────────│
│ [Configuration ✓]  [Rules]  [History]                      │
│                                                            │
│  Recipients:                                               │
│  ┌──────────────────────────────────────────────────────┐  │
│  │ alice@example.com  ✕ │ bob@example.com  ✕           │  │
│  └──────────────────────────────────────────────────────┘  │
│                                                            │
│  Created: March 5, 2026                                    │
│                                                            │
│                      [Test]  [Edit]  [Delete]  [○ ON]      │
└────────────────────────────────────────────────────────────┘
```

### State 2B: Configuration Tab — Webhook (with Secret)

```
┌────────────────────────────────────────────────────────────┐
│ 🔗 Webhook — "CI Pipeline"                  ● Enabled     │
│ ───────────────────────────────────────────────────────────│
│ [Configuration ✓]  [Rules]  [History]                      │
│                                                            │
│  Endpoint URL:                                             │
│  https://api.example.com/webhooks/reconova                 │
│                                                            │
│  Webhook Secret:                                           │
│  ••••••••••••••••••••••••  [Reveal]  [Regenerate]          │
│                                                            │
│  HMAC Verification:                                        │
│  Header: X-Reconova-Signature: sha256={hmac}               │
│  Header: X-Reconova-Timestamp: {unix_ts}                   │
│                                                            │
│  Created: March 8, 2026                                    │
│                                                            │
│                      [Test]  [Edit]  [Delete]  [○ ON]      │
└────────────────────────────────────────────────────────────┘
```

### State 2C: Rules Tab

```
┌────────────────────────────────────────────────────────────┐
│ ✉ Email — "Scan Alerts"                     ● Enabled     │
│ ───────────────────────────────────────────────────────────│
│ [Configuration]  [Rules ✓]  [History]                      │
│                                                            │
│  Notification Rules                 3 of 20 rules          │
│                                                            │
│  ┌───────────────────────────┬────────┬────────────────┐   │
│  │ Event Type                │ Active │ Severity       │   │
│  ├───────────────────────────┼────────┼────────────────┤   │
│  │ Scan Complete             │  [✓]   │ All        [▾] │   │
│  │ Scan Failed               │  [✓]   │ All        [▾] │   │
│  │ CVE Alert (Critical)      │  [✓]   │ CRITICAL   [▾] │   │
│  │ CVE Alert (High Digest)   │  [ ]   │ —              │   │
│  │ Credit Low                │  [ ]   │ —              │   │
│  │ Compliance Report Ready   │  [ ]   │ —              │   │
│  │ Compliance Score Change   │  [ ]   │ —              │   │
│  └───────────────────────────┴────────┴────────────────┘   │
│                                                            │
└────────────────────────────────────────────────────────────┘
```

### State 2D: Rules Tab — Severity Filter Dropdown

```
│  │ CVE Alert (Critical)      │  [✓]   │ CRITICAL   [▾] │
│  │                           │        │ ┌────────────┐ │
│  │                           │        │ │ ☑ CRITICAL │ │
│  │                           │        │ │ ☐ HIGH     │ │
│  │                           │        │ │ ☐ WARNING  │ │
│  │                           │        │ │ ☐ INFO     │ │
│  │                           │        │ └────────────┘ │
```

### State 2E: History Tab

```
┌────────────────────────────────────────────────────────────┐
│ ✉ Email — "Scan Alerts"                     ● Enabled     │
│ ───────────────────────────────────────────────────────────│
│ [Configuration]  [Rules]  [History ✓]                      │
│                                                            │
│  Delivery History                                          │
│                                                            │
│  ┌─────────────────────┬───────────┬──────────┬─────────┐  │
│  │ Event               │ Status    │ Sent     │ Error   │  │
│  ├─────────────────────┼───────────┼──────────┼─────────┤  │
│  │ Scan Complete       │ DELIVERED │ 2m ago   │ —       │  │
│  │ CVE Alert Critical  │ DELIVERED │ 1h ago   │ —       │  │
│  │ Scan Failed         │ FAILED    │ 3h ago   │ Timeout │  │
│  │ Scan Complete       │ DELIVERED │ 1d ago   │ —       │  │
│  │ Test                │ DELIVERED │ 2d ago   │ —       │  │
│  └─────────────────────┴───────────┴──────────┴─────────┘  │
│                                                            │
│  ◄ 1  2  3 ►                     Showing 1-20 of 47       │
│                                                            │
└────────────────────────────────────────────────────────────┘
```

### State 2F: History Tab — Expanded Row (Failed)

```
│  │ Scan Failed         │ FAILED    │ 3h ago   │ Timeout │  │
│  │ ─────────────────────────────────────────────────────│  │
│  │                                                      │  │
│  │  Attempts: 4 of 4 (all failed)                       │  │
│  │  1st: Mar 10 14:00 — Connection timeout (10s)        │  │
│  │  2nd: Mar 10 14:01 — Connection timeout (10s)        │  │
│  │  3rd: Mar 10 14:06 — Connection timeout (10s)        │  │
│  │  4th: Mar 10 14:21 — Connection timeout (10s)        │  │
│  │                                                      │  │
│  │  Payload:                                            │  │
│  │  { "event_type": "SCAN_FAILED", "severity": "HIGH", │  │
│  │    "data": { "scan_job_id": "...", ... } }           │  │
│  │                                                      │  │
```

### State 2G: History Tab — Empty

```
│  Delivery History                                          │
│                                                            │
│           No notifications sent yet.                        │
│    Configure rules above to start receiving                 │
│    notifications for this integration.                      │
```

---

## Screen 3: Add Integration Modal

### State 3A: Channel Selection (Step 1)

```
┌──────────────────────────────────────────────────┐
│ Add Integration                              ✕   │
├──────────────────────────────────────────────────┤
│                                                  │
│  Select a notification channel:                  │
│                                                  │
│  ┌────────────────────────────────────────────┐  │
│  │ ✉  Email                                  │  │
│  │    Send notifications to email addresses   │  │
│  └────────────────────────────────────────────┘  │
│  ┌────────────────────────────────────────────┐  │
│  │ 💬  Slack                                  │  │
│  │    Post to a Slack channel via webhook     │  │
│  └────────────────────────────────────────────┘  │
│  ┌────────────────────────────────────────────┐  │
│  │ 🎫  Jira              🔒 Requires Enterprise│  │
│  │    Create Jira issues automatically        │  │
│  └────────────────────────────────────────────┘  │
│  ┌────────────────────────────────────────────┐  │
│  │ 🔗  Webhook           🔒 Requires Enterprise│  │
│  │    Send events to a custom endpoint        │  │
│  └────────────────────────────────────────────┘  │
│  ┌────────────────────────────────────────────┐  │
│  │ 📡  SIEM              🔒 Requires Enterprise│  │
│  │    Forward to syslog/CEF endpoint          │  │
│  └────────────────────────────────────────────┘  │
│  ┌────────────────────────────────────────────┐  │
│  │ ⚙  Custom API         🔒 Coming Soon       │  │
│  │    Connect custom REST APIs  [POST-MVP]    │  │
│  └────────────────────────────────────────────┘  │
│                                                  │
└──────────────────────────────────────────────────┘
```

### State 3B: Email Configuration (Step 2)

```
┌──────────────────────────────────────────────────┐
│ Add Email Integration                        ✕   │
├──────────────────────────────────────────────────┤
│                                                  │
│  Name (optional)                                 │
│  ┌──────────────────────────────────────────┐    │
│  │ Scan Alerts                              │    │
│  └──────────────────────────────────────────┘    │
│                                                  │
│  Recipients *                                    │
│  ┌──────────────────────────────────────────┐    │
│  │ alice@example.com ✕ │ bob@ex... ✕ │ ▌   │    │
│  └──────────────────────────────────────────┘    │
│  2 of 10 maximum recipients                      │
│                                                  │
├──────────────────────────────────────────────────┤
│                [Cancel]  [Create Integration]    │
└──────────────────────────────────────────────────┘
```

### State 3C: Slack Configuration (Step 2)

```
┌──────────────────────────────────────────────────┐
│ Add Slack Integration                        ✕   │
├──────────────────────────────────────────────────┤
│                                                  │
│  Name (optional)                                 │
│  ┌──────────────────────────────────────────┐    │
│  │ Security Team                            │    │
│  └──────────────────────────────────────────┘    │
│                                                  │
│  Webhook URL *                                   │
│  ┌──────────────────────────────────────────┐    │
│  │ https://hooks.slack.com/services/...     │    │
│  └──────────────────────────────────────────┘    │
│                                                  │
├──────────────────────────────────────────────────┤
│                [Cancel]  [Create Integration]    │
└──────────────────────────────────────────────────┘
```

### State 3D: Jira Configuration (Step 2)

```
┌──────────────────────────────────────────────────┐
│ Add Jira Integration                         ✕   │
├──────────────────────────────────────────────────┤
│                                                  │
│  Name (optional)                                 │
│  ┌──────────────────────────────────────────┐    │
│  │ Vuln Tracker                             │    │
│  └──────────────────────────────────────────┘    │
│                                                  │
│  Instance URL *                                  │
│  ┌──────────────────────────────────────────┐    │
│  │ https://yourteam.atlassian.net           │    │
│  └──────────────────────────────────────────┘    │
│                                                  │
│  API Token *                                     │
│  ┌──────────────────────────────────────────┐    │
│  │ ••••••••                                 │    │
│  └──────────────────────────────────────────┘    │
│                                                  │
│  Project Key *                                   │
│  ┌──────────────────────────────────────────┐    │
│  │ SEC                                      │    │
│  └──────────────────────────────────────────┘    │
│                                                  │
│  Issue Type *                                    │
│  ┌──────────────────────────────────────────┐    │
│  │ Bug                                  ▾   │    │
│  └──────────────────────────────────────────┘    │
│                                                  │
├──────────────────────────────────────────────────┤
│                [Cancel]  [Create Integration]    │
└──────────────────────────────────────────────────┘
```

### State 3E: Webhook Configuration (Step 2)

```
┌──────────────────────────────────────────────────┐
│ Add Webhook Integration                      ✕   │
├──────────────────────────────────────────────────┤
│                                                  │
│  Name (optional)                                 │
│  ┌──────────────────────────────────────────┐    │
│  │ CI Pipeline                              │    │
│  └──────────────────────────────────────────┘    │
│                                                  │
│  Endpoint URL *                                  │
│  ┌──────────────────────────────────────────┐    │
│  │ https://your-server.com/webhooks/reconova│    │
│  └──────────────────────────────────────────┘    │
│                                                  │
│  ℹ A webhook secret will be auto-generated       │
│    for HMAC-SHA256 verification.                 │
│                                                  │
├──────────────────────────────────────────────────┤
│                [Cancel]  [Create Integration]    │
└──────────────────────────────────────────────────┘
```

### State 3F: SIEM Configuration (Step 2)

```
┌──────────────────────────────────────────────────┐
│ Add SIEM Integration                         ✕   │
├──────────────────────────────────────────────────┤
│                                                  │
│  Name (optional)                                 │
│  ┌──────────────────────────────────────────┐    │
│  │ SIEM                                     │    │
│  └──────────────────────────────────────────┘    │
│                                                  │
│  Syslog Host *                                   │
│  ┌──────────────────────────────────────────┐    │
│  │ siem.example.com                         │    │
│  └──────────────────────────────────────────┘    │
│                                                  │
│  Syslog Port *                                   │
│  ┌──────────────────────────────────────────┐    │
│  │ 514                                      │    │
│  └──────────────────────────────────────────┘    │
│                                                  │
│  Protocol *                                      │
│  (● TCP) (○ UDP) (○ TLS)                        │
│  ⚠ UDP has no delivery guarantee.                │
│                                                  │
│  Format *                                        │
│  (● SYSLOG) (○ CEF)                             │
│                                                  │
├──────────────────────────────────────────────────┤
│                [Cancel]  [Create Integration]    │
└──────────────────────────────────────────────────┘
```

### State 3G: Validation Error

```
│  Webhook URL *                                   │
│  ┌──────────────────────────────────────────┐    │
│  │ http://insecure.example.com              │    │
│  └──────────────────────────────────────────┘    │
│  ⚠ URL must use HTTPS.                           │
```

---

## Screen 4: Webhook Secret Display

```
┌──────────────────────────────────────────────────┐
│ Webhook Secret                               ✕   │
├──────────────────────────────────────────────────┤
│                                                  │
│  Your webhook secret has been generated.         │
│                                                  │
│  ┌──────────────────────────────────────────┐    │
│  │ a1b2c3d4e5f6a7b8c9d0e1f2a3b4c5d6        │    │
│  └──────────────────────────────────────────┘    │
│                                    [Copy]        │
│                                                  │
│  ⚠ Save this secret — it won't be shown         │
│    again in full. Use it to verify HMAC-SHA256   │
│    signatures on incoming webhooks.              │
│                                                  │
├──────────────────────────────────────────────────┤
│                              [I've Saved It]     │
└──────────────────────────────────────────────────┘
```

---

## Screen 5: Delete Integration Confirmation

```
┌──────────────────────────────────────────────────┐
│ Delete Integration                           ✕   │
├──────────────────────────────────────────────────┤
│                                                  │
│  Delete "Scan Alerts" (Email)?                   │
│                                                  │
│  All notification rules for this integration     │
│  will be removed. Delivery history will be       │
│  preserved but unlinked.                         │
│                                                  │
├──────────────────────────────────────────────────┤
│                         [Cancel]  [Delete]       │
└──────────────────────────────────────────────────┘
```

---

## Screen 6: Regenerate Webhook Secret Confirmation

```
┌──────────────────────────────────────────────────┐
│ Regenerate Webhook Secret                    ✕   │
├──────────────────────────────────────────────────┤
│                                                  │
│  Regenerate the webhook secret for               │
│  "CI Pipeline"?                                  │
│                                                  │
│  Your endpoint will need to be updated with      │
│  the new secret to verify HMAC signatures.       │
│                                                  │
├──────────────────────────────────────────────────┤
│                     [Cancel]  [Regenerate]       │
└──────────────────────────────────────────────────┘
```

---

## Screen 7: Admin API Keys Tab

**Route:** `/admin/integrations` (default tab)
**Access:** Super Admin

### State 7A: Default

```
┌──────────────────────────────────────────────────────────────┐
│ Platform Integrations                                        │
├──────────────────────────────────────────────────────────────┤
│ [API Keys ✓]  [Usage Dashboard]  [Provider Config]           │
├──────────────────────────────────────────────────────────────┤
│                                                              │
│  Shodan                                         [Add Key]    │
│  ┌──────┬──────────┬──────────┬────────────┬──────┬───────┐  │
│  │ ID   │ Status   │ Quota    │ Usage      │ Last │       │  │
│  ├──────┼──────────┼──────────┼────────────┼──────┼───────┤  │
│  │ •4a2 │ ACTIVE   │ 20K/mo   │ ████░ 62% │ 5m   │  ⋮    │  │
│  │ •8f1 │ ACTIVE   │ 20K/mo   │ ███░░ 45% │ 12m  │  ⋮    │  │
│  │ •c3d │ RETIRED  │ —        │ —          │ —    │       │  │
│  └──────┴──────────┴──────────┴────────────┴──────┴───────┘  │
│                                                              │
│  SecurityTrails                                 [Add Key]    │
│  ┌──────┬──────────┬──────────┬────────────┬──────┬───────┐  │
│  │ •1b3 │ ACTIVE   │ 10K/mo   │ █████ 98% │ 1h   │  ⋮    │  │
│  └──────┴──────────┴──────────┴────────────┴──────┴───────┘  │
│  ⚠ Approaching quota limit (98%)                             │
│                                                              │
│  Censys                                         [Add Key]    │
│  ┌──────────────────────────────────────────────────────┐    │
│  │  No active keys. Scans using Censys will skip this   │    │
│  │  provider.                        [Add Key]          │    │
│  └──────────────────────────────────────────────────────┘    │
│                                                              │
└──────────────────────────────────────────────────────────────┘
```

### State 7B: Key Row — Actions Menu

```
┌──────────────────┐
│ Rotate           │
│ Disable          │
│ Reset Quota      │
│ ──────────────── │
│ View Usage       │
└──────────────────┘
```

### State 7C: Key with Quota Exhausted

```
│  │ •1b3 │ QUOTA_EXHAUSTED │ 10K/mo │ █████ 100% │ 2d │ ⋮ │
│  ⚠ Quota exhausted. Key is inactive until monthly reset     │
│    or manual override.                    [Reset Quota]      │
```

### State 7D: Empty — No Keys for Any Provider

```
│                                                              │
│          No platform API keys configured.                     │
│   Add API keys to enable third-party data source             │
│   integrations during scans.                                 │
│                                                              │
│               [Add Key for Shodan]                           │
│               [Add Key for SecurityTrails]                   │
│               [Add Key for Censys]                           │
│                                                              │
```

---

## Screen 8: Add/Rotate API Key Modal

### State 8A: Add API Key

```
┌──────────────────────────────────────────────────┐
│ Add API Key — Shodan                         ✕   │
├──────────────────────────────────────────────────┤
│                                                  │
│  API Key *                                       │
│  ┌──────────────────────────────────────────┐    │
│  │ ••••••••                                 │    │
│  └──────────────────────────────────────────┘    │
│                                                  │
│  Rate Limit (calls per rate window) *            │
│  ┌──────────────────────────────────────────┐    │
│  │ 100                                      │    │
│  └──────────────────────────────────────────┘    │
│                                                  │
│  Monthly Quota *                                 │
│  ┌──────────────────────────────────────────┐    │
│  │ 20000                                    │    │
│  └──────────────────────────────────────────┘    │
│                                                  │
├──────────────────────────────────────────────────┤
│                        [Cancel]  [Add Key]       │
└──────────────────────────────────────────────────┘
```

### State 8B: Rotate API Key

```
┌──────────────────────────────────────────────────┐
│ Rotate API Key — Shodan                      ✕   │
├──────────────────────────────────────────────────┤
│                                                  │
│  Current key: ••••••4a2  (ACTIVE)                │
│                                                  │
│  The current key will be retired and replaced    │
│  with the new key. Rate limit and quota settings │
│  will be preserved.                              │
│                                                  │
│  New API Key *                                   │
│  ┌──────────────────────────────────────────┐    │
│  │ ••••••••                                 │    │
│  └──────────────────────────────────────────┘    │
│                                                  │
├──────────────────────────────────────────────────┤
│                     [Cancel]  [Rotate Key]       │
└──────────────────────────────────────────────────┘
```

---

## Screen 9: Admin Usage Dashboard Tab

**Route:** `/admin/integrations` (usage tab)
**Access:** Super Admin

### State 9A: Default

```
┌──────────────────────────────────────────────────────────────┐
│ Platform Integrations                                        │
├──────────────────────────────────────────────────────────────┤
│ [API Keys]  [Usage Dashboard ✓]  [Provider Config]           │
├──────────────────────────────────────────────────────────────┤
│                                                              │
│  Date range: [Today] [This Week] [This Month ✓] [Custom]    │
│                                                              │
│  ┌──────────────┐ ┌──────────────┐                           │
│  │  1,240       │ │  18,540      │                           │
│  │  calls today │ │  calls /mo   │                           │
│  └──────────────┘ └──────────────┘                           │
│                                                              │
│  Usage by Provider                                           │
│  ┌────────────────┬──────────┬───────────┬──────────────┐    │
│  │ Provider       │ Calls    │ % of Quota│ Trend        │    │
│  ├────────────────┼──────────┼───────────┼──────────────┤    │
│  │ Shodan         │ 12,450   │ 62%       │ ████████░░   │    │
│  │ SecurityTrails │ 5,200    │ 52%       │ ██████░░░░   │    │
│  │ Censys         │ 890      │ 9%        │ █░░░░░░░░░   │    │
│  └────────────────┴──────────┴───────────┴──────────────┘    │
│                                                              │
│  Top Consumers (Tenants)                                     │
│  ┌────────────────────────┬──────────┬──────────────────┐    │
│  │ Tenant                 │ Calls    │ Provider Mix     │    │
│  ├────────────────────────┼──────────┼──────────────────┤    │
│  │ Acme Corp              │ 3,200    │ Shodan 80%       │    │
│  │ SecureTech Ltd         │ 2,800    │ Shodan 60%       │    │
│  │ CloudGuard Inc         │ 1,950    │ SecTrails 70%    │    │
│  │ DataShield             │ 1,200    │ Censys 90%       │    │
│  │ NetWatch               │ 890      │ Shodan 100%      │    │
│  └────────────────────────┴──────────┴──────────────────┘    │
│  Showing top 5 │ [View All Tenants]                          │
│                                                              │
└──────────────────────────────────────────────────────────────┘
```

### State 9B: Loading

```
│  ┌──────────────┐ ┌──────────────┐                           │
│  │  ░░░░░░░░░░  │ │  ░░░░░░░░░░  │                           │
│  └──────────────┘ └──────────────┘                           │
│                                                              │
│  ┌────────────────────────────────────────────────────────┐  │
│  │  ░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░  │  │
│  │  ░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░  │  │
│  └────────────────────────────────────────────────────────┘  │
```

### State 9C: Error

```
│                                                              │
│          Unable to load usage data.                          │
│                                                              │
│                    [Retry]                                    │
│                                                              │
```

---

## Screen 10: Admin Provider Config Tab

**Route:** `/admin/integrations` (providers tab)
**Access:** Super Admin

### State 10A: Default

```
┌──────────────────────────────────────────────────────────────┐
│ Platform Integrations                                        │
├──────────────────────────────────────────────────────────────┤
│ [API Keys]  [Usage Dashboard]  [Provider Config ✓]           │
├──────────────────────────────────────────────────────────────┤
│                                                              │
│  Per-Tenant Rate Limits                                      │
│  These limits prevent a single tenant from exhausting the    │
│  shared API key pool.                                        │
│                                                              │
│  ┌────────────────┬────────────┬────────────┬──────┬──────┐  │
│  │ Provider       │ Calls/Hour │ Calls/Day  │ Tier │      │  │
│  ├────────────────┼────────────┼────────────┼──────┼──────┤  │
│  │ Shodan         │ 50         │ 500        │ Pro  │[Edit]│  │
│  │ SecurityTrails │ 30         │ 300        │ Pro  │[Edit]│  │
│  │ Censys         │ 30         │ 300        │ Ent. │[Edit]│  │
│  │ VirusTotal     │ 20         │ 200        │ Pro  │ —    │  │
│  │   └─ [POST-MVP] Not yet available                      │  │
│  │ Nuclei Templ.  │ Unlimited  │ Unlimited  │ All  │ —    │  │
│  └────────────────┴────────────┴────────────┴──────┴──────┘  │
│                                                              │
└──────────────────────────────────────────────────────────────┘
```

### State 10B: Edit Rate Limits Modal

```
┌──────────────────────────────────────────────────┐
│ Edit Rate Limits — Shodan                    ✕   │
├──────────────────────────────────────────────────┤
│                                                  │
│  Calls per tenant per hour *                     │
│  ┌──────────────────────────────────────────┐    │
│  │ 50                                       │    │
│  └──────────────────────────────────────────┘    │
│                                                  │
│  Calls per tenant per day *                      │
│  ┌──────────────────────────────────────────┐    │
│  │ 500                                      │    │
│  └──────────────────────────────────────────┘    │
│  Must be ≥ calls per hour.                       │
│                                                  │
├──────────────────────────────────────────────────┤
│                          [Cancel]  [Save]        │
└──────────────────────────────────────────────────┘
```

---

## Screen 11: Disable Key Confirmation

```
┌──────────────────────────────────────────────────┐
│ Disable API Key                              ✕   │
├──────────────────────────────────────────────────┤
│                                                  │
│  Disable API key ••••••4a2 (Shodan)?             │
│                                                  │
│  This key will no longer be used for scans.      │
│  Scan steps requiring Shodan will use remaining  │
│  active keys, or skip if none available.         │
│                                                  │
├──────────────────────────────────────────────────┤
│                       [Cancel]  [Disable]        │
└──────────────────────────────────────────────────┘
```

---

## Screen 12: Reset Quota Confirmation

```
┌──────────────────────────────────────────────────┐
│ Reset Key Quota                              ✕   │
├──────────────────────────────────────────────────┤
│                                                  │
│  Reset quota for key ••••••1b3 (SecurityTrails)? │
│                                                  │
│  Usage count will be set to 0 and the key        │
│  status will return to ACTIVE.                   │
│                                                  │
├──────────────────────────────────────────────────┤
│                    [Cancel]  [Reset Quota]        │
└──────────────────────────────────────────────────┘
```

---

## Screen Summary

| # | Screen | States | Key Interactions |
|---|--------|--------|-----------------|
| 1 | Integration List | 7 (1A–1G) | CRUD, enable/disable, test, expand to detail |
| 2 | Integration Detail | 7 (2A–2G) | Config view, rules toggles, history table |
| 3 | Add Integration Modal | 7 (3A–3G) | Channel selection + channel-specific forms |
| 4 | Webhook Secret Display | 1 | Copy-to-clipboard, one-time display |
| 5 | Delete Integration Confirmation | 1 | Simple confirmation |
| 6 | Regenerate Secret Confirmation | 1 | Confirmation with warning |
| 7 | Admin API Keys | 4 (7A–7D) | Key pool per provider, status badges, quota bars |
| 8 | Add/Rotate Key Modal | 2 (8A–8B) | Key input + quota config |
| 9 | Admin Usage Dashboard | 3 (9A–9C) | Summary cards, provider table, tenant table |
| 10 | Admin Provider Config | 2 (10A–10B) | Rate limit table + edit modal |
| 11 | Disable Key Confirmation | 1 | Confirmation |
| 12 | Reset Quota Confirmation | 1 | Confirmation |
```
