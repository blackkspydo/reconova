# Screens & Wireframes (Feature Flags & Access Control)

Scope: ASCII wireframes for all feature flag screens, covering every state (default, loading, error, empty) with conditional rendering rules. Includes tenant-facing feature visibility, reusable gating components, and super admin management interfaces.

---

## Route Structure

| Route | Access | Description |
|-------|--------|-------------|
| `/settings/features` | Tenant Owner (view), Members (view) | Plan & Features page |
| `/admin/features` | Super Admin | Feature management overview |
| `/admin/features/operational` | Super Admin | Operational flag toggles |
| `/admin/features/overrides` | Super Admin | Tenant override management |

---

## Screen 1: Plan & Features (`/settings/features`)

### State: Default (Mixed Available/Locked)

```
┌──────────────────────────────────────────────────────────────────────┐
│  Settings > Plan & Features                                          │
├──────────────────────────────────────────────────────────────────────┤
│                                                                      │
│  Current Plan: Pro                              [Upgrade Plan]       │
│                                                                      │
│  ┌─── Scanning ──────────────────────────────────────────────────┐  │
│  │                                                                │  │
│  │  ✓ Subdomain Enumeration               Included                │  │
│  │  ✓ Port Scanning                        Included                │  │
│  │  ✓ Technology Detection                 Included                │  │
│  │  ✓ Screenshot Capture                   Included                │  │
│  │  ✓ Vulnerability Scanning               Included                │  │
│  │  🔒 Custom Workflows                   Requires Enterprise     │  │
│  │  ✓ Scheduled Scans                      Included                │  │
│  │                                                                │  │
│  └────────────────────────────────────────────────────────────────┘  │
│                                                                      │
│  ┌─── Compliance ────────────────────────────────────────────────┐  │
│  │                                                                │  │
│  │  ✓ Compliance Checks                    Included                │  │
│  │  ✓ Compliance Reports                   Included                │  │
│  │                                                                │  │
│  └────────────────────────────────────────────────────────────────┘  │
│                                                                      │
│  ┌─── Integrations ──────────────────────────────────────────────┐  │
│  │                                                                │  │
│  │  ✓ Shodan Integration                   Included                │  │
│  │  ✓ SecurityTrails Integration           Included                │  │
│  │  ✓ Censys Integration                   Included                │  │
│  │  🔒 Custom API Connectors              Requires Enterprise     │  │
│  │                                                                │  │
│  └────────────────────────────────────────────────────────────────┘  │
│                                                                      │
│  ┌─── Notifications ─────────────────────────────────────────────┐  │
│  │                                                                │  │
│  │  ✓ Slack Notifications                  Included                │  │
│  │  ✓ Jira Integration                     Included                │  │
│  │  ✓ Webhook Notifications                Included                │  │
│  │  ✓ SIEM Integration                     Included                │  │
│  │                                                                │  │
│  └────────────────────────────────────────────────────────────────┘  │
│                                                                      │
│  ┌─── Monitoring ────────────────────────────────────────────────┐  │
│  │                                                                │  │
│  │  ✓ CVE Monitoring                       Included                │  │
│  │                                                                │  │
│  └────────────────────────────────────────────────────────────────┘  │
│                                                                      │
└──────────────────────────────────────────────────────────────────────┘
```

### State: Free Tier (All Locked)

```
┌──────────────────────────────────────────────────────────────────────┐
│  Settings > Plan & Features                                          │
├──────────────────────────────────────────────────────────────────────┤
│                                                                      │
│  ┌─── 🔒 No Active Plan ────────────────────────────────────────┐  │
│  │                                                                │  │
│  │  You're on the Free tier. Upgrade to unlock scanning,          │  │
│  │  compliance, integrations, and more.                           │  │
│  │                                                                │  │
│  │  [View Plans]                                                  │  │
│  │                                                                │  │
│  └────────────────────────────────────────────────────────────────┘  │
│                                                                      │
│  ┌─── Scanning ──────────────────────────────────────────────────┐  │
│  │                                                                │  │
│  │  🔒 Subdomain Enumeration               Requires Starter      │  │
│  │  🔒 Port Scanning                       Requires Starter      │  │
│  │  🔒 Technology Detection                Requires Starter      │  │
│  │  🔒 Screenshot Capture                  Requires Starter      │  │
│  │  🔒 Vulnerability Scanning              Requires Pro          │  │
│  │  🔒 Custom Workflows                   Requires Enterprise    │  │
│  │  🔒 Scheduled Scans                    Requires Pro           │  │
│  │                                                                │  │
│  └────────────────────────────────────────────────────────────────┘  │
│  ...                                                                 │
└──────────────────────────────────────────────────────────────────────┘
```

### State: Operational Flag Disabled (Feature Unavailable)

```
┌─── Scanning ──────────────────────────────────────────────────────┐
│                                                                    │
│  ✓ Subdomain Enumeration                   Included                │
│  ✓ Port Scanning                            Included                │
│  ✓ Technology Detection                     Included                │
│  ✓ Screenshot Capture                       Included                │
│  ⚠ Vulnerability Scanning                  Temporarily unavailable │
│  🔒 Custom Workflows                       Requires Enterprise     │
│  ✓ Scheduled Scans                          Included                │
│                                                                    │
└────────────────────────────────────────────────────────────────────┘
```

### State: Loading

```
┌──────────────────────────────────────────────────────────────────────┐
│  Settings > Plan & Features                                          │
├──────────────────────────────────────────────────────────────────────┤
│                                                                      │
│  Current Plan: ░░░░░░░░                         ░░░░░░░░░░░          │
│                                                                      │
│  ┌─── ░░░░░░░░ ──────────────────────────────────────────────────┐  │
│  │                                                                │  │
│  │  ░░░░░░░░░░░░░░░░░░░░░░░░      ░░░░░░░░░                      │  │
│  │  ░░░░░░░░░░░░░░░░░░░░░░░░      ░░░░░░░░░                      │  │
│  │  ░░░░░░░░░░░░░░░░░░░░░░░░      ░░░░░░░░░                      │  │
│  │                                                                │  │
│  └────────────────────────────────────────────────────────────────┘  │
│  ...                                                                 │
└──────────────────────────────────────────────────────────────────────┘
```

### State: Error

```
┌──────────────────────────────────────────────────────────────────────┐
│  Settings > Plan & Features                                          │
├──────────────────────────────────────────────────────────────────────┤
│                                                                      │
│  ┌─── Error ─────────────────────────────────────────────────────┐  │
│  │                                                                │  │
│  │  Unable to load feature information.                           │  │
│  │  [Retry]                                                       │  │
│  │                                                                │  │
│  └────────────────────────────────────────────────────────────────┘  │
│                                                                      │
└──────────────────────────────────────────────────────────────────────┘
```

### Conditional Rendering

| Condition | Display |
|-----------|---------|
| Feature enabled (reason: plan/override) | ✓ "Included" green badge |
| Feature locked (reason: plan) | 🔒 "Requires {tier}" amber badge |
| Feature disabled (reason: operational) | ⚠ "Temporarily unavailable" grey badge |
| Free tier (no subscription) | All locked + upgrade banner at top |
| Pending downgrade | Info banner: "Some features will be locked on {date}" |

---

## Screen 2: Upgrade Modal (Global Overlay)

**Location:** Overlay, triggered from any locked feature interaction across the app.

### State: Default

```
┌──────────────────────────────────────────────────────────────────────┐
│                                                                      │
│         ┌────────────────────────────────────────────┐               │
│         │                                        [✕] │               │
│         │  Upgrade to unlock                          │               │
│         │  Vulnerability Scanning                     │               │
│         │                                             │               │
│         │  ────────────────────────────────────────── │               │
│         │                                             │               │
│         │  Scan targets for known vulnerabilities     │               │
│         │  and receive remediation recommendations.   │               │
│         │                                             │               │
│         │  Available on: Pro and above                │               │
│         │  Your plan:    Starter                      │               │
│         │                                             │               │
│         │  ────────────────────────────────────────── │               │
│         │                                             │               │
│         │  [View Plans]        [See All Features]     │               │
│         │   (primary)            (text link)          │               │
│         │                                             │               │
│         └────────────────────────────────────────────┘               │
│                                                                      │
└──────────────────────────────────────────────────────────────────────┘
           backdrop: semi-transparent overlay
```

### State: Operational Disabled (No Upgrade Path)

```
         ┌────────────────────────────────────────────┐
         │                                        [✕] │
         │  Feature Unavailable                        │
         │                                             │
         │  ────────────────────────────────────────── │
         │                                             │
         │  Vulnerability Scanning is temporarily      │
         │  unavailable for maintenance.               │
         │                                             │
         │  No action required — it will be restored   │
         │  automatically.                             │
         │                                             │
         │  ────────────────────────────────────────── │
         │                                             │
         │  [Close]                                    │
         │                                             │
         └────────────────────────────────────────────┘
```

### Conditional Rendering

| Condition | Display |
|-----------|---------|
| reason: "plan" | Upgrade variant: tier comparison + [View Plans] CTA |
| reason: "operational_disabled" | Unavailable variant: maintenance message + [Close] only |
| Feature description available | Show description text |
| Feature description missing | Hide description section |

---

## Screen 3: Sidebar Navigation — Gated Items

**Location:** App sidebar, visible on all authenticated pages.

### State: Pro Plan (Some Locked)

```
┌──────────────────────┐
│  ◀ Reconova           │
│                       │
│  Dashboard            │
│  Domains              │
│  Scans                │
│  Compliance           │
│  CVE Monitoring       │
│  Integrations         │
│  Notifications        │
│  🔒 Custom Workflows │
│                       │
│  ─────────────────── │
│  Settings             │
└──────────────────────┘
```

### State: Starter Plan (More Locked)

```
┌──────────────────────┐
│  ◀ Reconova           │
│                       │
│  Dashboard            │
│  Domains              │
│  Scans                │
│  🔒 Compliance       │
│  🔒 CVE Monitoring   │
│  Integrations         │
│  Notifications        │
│  🔒 Custom Workflows │
│                       │
│  ─────────────────── │
│  Settings             │
└──────────────────────┘
```

### State: Operational Flag Off (Module Disabled)

```
┌──────────────────────┐
│  ◀ Reconova           │
│                       │
│  Dashboard            │
│  Domains              │
│  Scans                │
│  ⚠ Compliance        │  ◄─ operational flag off
│  CVE Monitoring       │
│  Integrations         │
│  Notifications        │
│                       │
│  ─────────────────── │
│  Settings             │
└──────────────────────┘
```

### Conditional Rendering

| Condition | Nav Item Display |
|-----------|-----------------|
| Feature enabled | Normal text, clickable |
| Feature locked (plan) | 🔒 icon prefix, clickable → opens Upgrade Modal |
| Feature disabled (operational) | ⚠ icon prefix, clickable → shows unavailable message |
| Module always visible (Scans, Integrations, Notifications) | Normal — individual sub-features gated internally |

---

## Screen 4: Scan Step Selector — Gated Steps

**Location:** Within Scan Creation form (cross-reference with Scanning plan).

### State: Pro Plan (Custom Workflows Locked)

```
┌─── Select Scan Steps ────────────────────────────────────────────────┐
│                                                                      │
│  ☑ Subdomain Enumeration              2 credits/domain               │
│  ☑ Port Scanning                       3 credits/domain               │
│  ☑ Technology Detection                1 credit/domain                │
│  ☐ Screenshot Capture                  2 credits/domain               │
│  ☑ Vulnerability Scanning              5 credits/domain               │
│  🔒 Custom Workflows                  Requires Enterprise            │
│                                                                      │
│  ──────────────────────────────────────────────────────────────────  │
│  Estimated cost: 13 credits/domain × 3 domains = 39 credits         │
│  Available: 340 credits                                              │
│                                                                      │
└──────────────────────────────────────────────────────────────────────┘
```

### State: Starter Plan (Several Locked)

```
┌─── Select Scan Steps ────────────────────────────────────────────────┐
│                                                                      │
│  ☑ Subdomain Enumeration              2 credits/domain               │
│  ☑ Port Scanning                       3 credits/domain               │
│  ☑ Technology Detection                1 credit/domain                │
│  ☐ Screenshot Capture                  2 credits/domain               │
│  🔒 Vulnerability Scanning            Requires Pro                   │
│  🔒 Custom Workflows                  Requires Enterprise            │
│                                                                      │
│  ──────────────────────────────────────────────────────────────────  │
│  Estimated cost: 6 credits/domain × 3 domains = 18 credits          │
│  Available: 80 credits                                               │
│                                                                      │
└──────────────────────────────────────────────────────────────────────┘
```

### State: Operational Flag Off (Step Hidden)

```
┌─── Select Scan Steps ────────────────────────────────────────────────┐
│                                                                      │
│  ☑ Subdomain Enumeration              2 credits/domain               │
│  ☑ Port Scanning                       3 credits/domain               │
│  ☑ Technology Detection                1 credit/domain                │
│  ☐ Screenshot Capture                  2 credits/domain               │
│                                                                      │
│  ⚠ Vulnerability Scanning is temporarily unavailable.               │
│                                                                      │
│  🔒 Custom Workflows                  Requires Enterprise            │
│                                                                      │
└──────────────────────────────────────────────────────────────────────┘
```

### Conditional Rendering

| Condition | Step Display |
|-----------|-------------|
| Feature enabled | Selectable checkbox with credit cost |
| Feature locked (plan) | 🔒 non-selectable, "Requires {tier}" — click opens Upgrade Modal |
| Feature disabled (operational) | Hidden from selection + inline warning note |
| Scheduled scans locked | "Schedule" toggle disabled with "Requires Pro" tooltip |

---

## Screen 5: Admin Feature Overview (`/admin/features`)

### State: Default — Subscription Flags Tab

```
┌──────────────────────────────────────────────────────────────────────┐
│  Admin > Feature Management                                          │
├──────────────────────────────────────────────────────────────────────┤
│                                                                      │
│  [Subscription Flags]  [Operational Flags]  [Tenant Overrides]       │
│  ═══════════════════                                                 │
│                                                                      │
│  Filter: [All Modules ▼]   Search: [________________]               │
│                                                                      │
│  ┌────────────────────────────────────────────────────────────────┐  │
│  │ Flag Name              │ Module     │ Free │Star│ Pro │Ent    │  │
│  ├────────────────────────┼────────────┼──────┼────┼─────┼───────┤  │
│  │ subdomain_enumeration  │ scanning   │  ✗   │ ✓  │  ✓  │  ✓   │  │
│  │ port_scanning          │ scanning   │  ✗   │ ✓  │  ✓  │  ✓   │  │
│  │ technology_detection   │ scanning   │  ✗   │ ✓  │  ✓  │  ✓   │  │
│  │ screenshot_capture     │ scanning   │  ✗   │ ✓  │  ✓  │  ✓   │  │
│  │ vulnerability_scanning │ scanning   │  ✗   │ ✗  │  ✓  │  ✓   │  │
│  │ custom_workflows       │ scanning   │  ✗   │ ✗  │  ✗  │  ✓   │  │
│  │ scheduled_scans        │ scanning   │  ✗   │ ✗  │  ✓  │  ✓   │  │
│  │ compliance_checks      │ compliance │  ✗   │ ✗  │  ✓  │  ✓   │  │
│  │ compliance_reports     │ compliance │  ✗   │ ✗  │  ✓  │  ✓   │  │
│  │ shodan_integration     │ integrat.  │  ✗   │ ✗  │  ✓  │  ✓   │  │
│  │ securitytrails_integ.  │ integrat.  │  ✗   │ ✗  │  ✓  │  ✓   │  │
│  │ censys_integration     │ integrat.  │  ✗   │ ✗  │  ✓  │  ✓   │  │
│  │ custom_api_connectors  │ integrat.  │  ✗   │ ✗  │  ✗  │  ✓   │  │
│  │ notification_slack     │ notific.   │  ✗   │ ✗  │  ✓  │  ✓   │  │
│  │ notification_jira      │ notific.   │  ✗   │ ✗  │  ✓  │  ✓   │  │
│  │ notification_webhook   │ notific.   │  ✗   │ ✗  │  ✓  │  ✓   │  │
│  │ notification_siem      │ notific.   │  ✗   │ ✗  │  ✓  │  ✓   │  │
│  │ cve_monitoring         │ monitoring │  ✗   │ ✗  │  ✓  │  ✓   │  │
│  └────────────────────────┴────────────┴──────┴────┴─────┴───────┘  │
│                                                                      │
│  18 subscription flags                                               │
│                                                                      │
└──────────────────────────────────────────────────────────────────────┘
```

### State: Loading

```
┌──────────────────────────────────────────────────────────────────────┐
│  Admin > Feature Management                                          │
├──────────────────────────────────────────────────────────────────────┤
│                                                                      │
│  [Subscription Flags]  [Operational Flags]  [Tenant Overrides]       │
│  ═══════════════════                                                 │
│                                                                      │
│  ░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░  │
│  ░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░  │
│  ░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░  │
│  ░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░  │
│                                                                      │
└──────────────────────────────────────────────────────────────────────┘
```

### State: Filtered by Module

```
│  Filter: [Scanning ▼]     Search: [________________]                │
│                                                                      │
│  ┌────────────────────────────────────────────────────────────────┐  │
│  │ Flag Name              │ Module   │ Free │Star│ Pro │Ent      │  │
│  ├────────────────────────┼──────────┼──────┼────┼─────┼─────────┤  │
│  │ subdomain_enumeration  │ scanning │  ✗   │ ✓  │  ✓  │  ✓     │  │
│  │ port_scanning          │ scanning │  ✗   │ ✓  │  ✓  │  ✓     │  │
│  │ ...                    │          │      │    │     │         │  │
│  └────────────────────────┴──────────┴──────┴────┴─────┴─────────┘  │
│                                                                      │
│  7 of 18 subscription flags (filtered: scanning)                     │
```

---

## Screen 6: Admin Operational Flags (`/admin/features/operational`)

### State: Default (All Enabled)

```
┌──────────────────────────────────────────────────────────────────────┐
│  Admin > Feature Management                                          │
├──────────────────────────────────────────────────────────────────────┤
│                                                                      │
│  [Subscription Flags]  [Operational Flags]  [Tenant Overrides]       │
│                        ═══════════════════                           │
│                                                                      │
│  These flags affect ALL tenants globally. Toggle with caution.       │
│                                                                      │
│  ┌────────────────────────────────────────────────────────────────┐  │
│  │                                                                │  │
│  │  ┌─ maintenance_mode ─────────────────────────────────────┐   │  │
│  │  │  Platform maintenance mode                      [ON ◉] │   │  │
│  │  │  Module: platform                                       │   │  │
│  │  │  Last changed: Never                                    │   │  │
│  │  └─────────────────────────────────────────────────────────┘   │  │
│  │                                                                │  │
│  │  ┌─ vuln_scanning_global ─────────────────────────────────┐   │  │
│  │  │  Global vulnerability scanning                  [ON ◉] │   │  │
│  │  │  Module: scanning                                       │   │  │
│  │  │  Last changed: Never                                    │   │  │
│  │  └─────────────────────────────────────────────────────────┘   │  │
│  │                                                                │  │
│  │  ┌─ compliance_global ────────────────────────────────────┐   │  │
│  │  │  Global compliance engine                       [ON ◉] │   │  │
│  │  │  Module: compliance                                     │   │  │
│  │  │  Last changed: Never                                    │   │  │
│  │  └─────────────────────────────────────────────────────────┘   │  │
│  │                                                                │  │
│  │  ┌─ cve_monitoring_global ────────────────────────────────┐   │  │
│  │  │  Global CVE monitoring                          [ON ◉] │   │  │
│  │  │  Module: monitoring                                     │   │  │
│  │  │  Last changed: Never                                    │   │  │
│  │  └─────────────────────────────────────────────────────────┘   │  │
│  │                                                                │  │
│  │  ┌─ api_global ───────────────────────────────────────────┐   │  │
│  │  │  Global API access                              [ON ◉] │   │  │
│  │  │  Module: platform                                       │   │  │
│  │  │  Last changed: Never                                    │   │  │
│  │  └─────────────────────────────────────────────────────────┘   │  │
│  │                                                                │  │
│  └────────────────────────────────────────────────────────────────┘  │
│                                                                      │
│  5 operational flags                                                 │
│                                                                      │
└──────────────────────────────────────────────────────────────────────┘
```

### State: One Flag Disabled

```
│  ┌─ vuln_scanning_global ─────────────────────────────────────┐  │
│  │  Global vulnerability scanning                  [◉ OFF]    │  │
│  │  Module: scanning                                ⚠ DISABLED│  │
│  │  Last changed: Mar 9, 2026 by admin@reconova.io            │  │
│  └────────────────────────────────────────────────────────────┘  │
```

### State: Confirmation Modal — Disabling

```
         ┌────────────────────────────────────────────┐
         │                                        [✕] │
         │  ⚠ Disable vuln_scanning_global?           │
         │                                             │
         │  ────────────────────────────────────────── │
         │                                             │
         │  This will immediately affect ALL tenants.  │
         │                                             │
         │  Vulnerability scanning will be             │
         │  unavailable platform-wide.                 │
         │                                             │
         │  Active scans: current step completes,      │
         │  next vuln scan steps will be skipped.      │
         │                                             │
         │  ────────────────────────────────────────── │
         │                                             │
         │  [Disable for All Tenants]       [Cancel]   │
         │   (destructive/red)                         │
         │                                             │
         └────────────────────────────────────────────┘
```

### State: Confirmation Modal — Enabling

```
         ┌────────────────────────────────────────────┐
         │                                        [✕] │
         │  Enable vuln_scanning_global?               │
         │                                             │
         │  ────────────────────────────────────────── │
         │                                             │
         │  This will restore vulnerability scanning   │
         │  for all tenants (based on their plan).     │
         │                                             │
         │  ────────────────────────────────────────── │
         │                                             │
         │  [Enable]                        [Cancel]   │
         │   (primary)                                 │
         │                                             │
         └────────────────────────────────────────────┘
```

---

## Screen 7: Admin Tenant Overrides (`/admin/features/overrides`)

### State: No Tenant Selected

```
┌──────────────────────────────────────────────────────────────────────┐
│  Admin > Feature Management                                          │
├──────────────────────────────────────────────────────────────────────┤
│                                                                      │
│  [Subscription Flags]  [Operational Flags]  [Tenant Overrides]       │
│                                             ═══════════════════      │
│                                                                      │
│  Search tenant: [________________________]                           │
│                                                                      │
│  ┌────────────────────────────────────────────────────────────────┐  │
│  │                                                                │  │
│  │            Select a tenant to view and manage                  │  │
│  │            their feature overrides.                            │  │
│  │                                                                │  │
│  └────────────────────────────────────────────────────────────────┘  │
│                                                                      │
└──────────────────────────────────────────────────────────────────────┘
```

### State: Tenant Selected — With Overrides

```
┌──────────────────────────────────────────────────────────────────────┐
│  Admin > Feature Management                                          │
├──────────────────────────────────────────────────────────────────────┤
│                                                                      │
│  [Subscription Flags]  [Operational Flags]  [Tenant Overrides]       │
│                                             ═══════════════════      │
│                                                                      │
│  Search tenant: [Acme Corp_________________]                         │
│                                                                      │
│  Tenant: Acme Corp  |  Plan: Starter  |  Overrides: 2               │
│                                                                      │
│  ┌──────────────────┬──────────┬────────────┬──────────┬──────────┐ │
│  │ Feature          │ Module   │ Plan       │ Override │ Actions  │ │
│  ├──────────────────┼──────────┼────────────┼──────────┼──────────┤ │
│  │ subdomain_enum   │ scanning │ Included   │ —        │[Add]     │ │
│  │ port_scanning    │ scanning │ Included   │ —        │[Add]     │ │
│  │ technology_det.  │ scanning │ Included   │ —        │[Add]     │ │
│  │ screenshot_cap.  │ scanning │ Included   │ —        │[Add]     │ │
│  │ vuln_scanning    │ scanning │ Not in plan│ ✓ Enabled│[Edit][Del│ │
│  │                  │          │            │ by admin │          │ │
│  │ custom_workflows │ scanning │ Not in plan│ —        │[Add]     │ │
│  │ scheduled_scans  │ scanning │ Not in plan│ ✓ Enabled│[Edit][Del│ │
│  │                  │          │            │ by admin │          │ │
│  │ compliance_chk   │ complnc  │ Not in plan│ —        │[Add]     │ │
│  │ ...              │          │            │          │          │ │
│  └──────────────────┴──────────┴────────────┴──────────┴──────────┘ │
│                                                                      │
│  18 features shown  |  2 overrides active                            │
│                                                                      │
└──────────────────────────────────────────────────────────────────────┘
```

### State: Tenant Selected — No Overrides

```
│  Tenant: Acme Corp  |  Plan: Pro  |  Overrides: 0                    │
│                                                                      │
│  ┌──────────────────┬──────────┬────────────┬──────────┬──────────┐ │
│  │ Feature          │ Module   │ Plan       │ Override │ Actions  │ │
│  ├──────────────────┼──────────┼────────────┼──────────┼──────────┤ │
│  │ subdomain_enum   │ scanning │ Included   │ —        │[Add]     │ │
│  │ port_scanning    │ scanning │ Included   │ —        │[Add]     │ │
│  │ ...              │          │            │          │          │ │
│  └──────────────────┴──────────┴────────────┴──────────┴──────────┘ │
│                                                                      │
│  No overrides active. All features follow plan defaults.             │
```

### State: Empty Search Results

```
│  Search tenant: [nonexistent_________________]                       │
│                                                                      │
│  No tenants found matching "nonexistent".                            │
```

---

## Screen 8: Create/Edit Override Modal

### State: Create Override

```
         ┌────────────────────────────────────────────┐
         │  Add Override                           [✕] │
         │                                             │
         │  Tenant:  Acme Corp                         │
         │  Feature: vulnerability_scanning             │
         │  Module:  scanning                          │
         │  Plan status: Not in plan                   │
         │                                             │
         │  ────────────────────────────────────────── │
         │                                             │
         │  Access:                                    │
         │    (●) Enable    ( ) Disable                │
         │                                             │
         │  Reason:                                    │
         │  ┌──────────────────────────────────────┐   │
         │  │ Customer is on a trial period for    │   │
         │  │ vulnerability scanning features.     │   │
         │  │                                      │   │
         │  └──────────────────────────────────────┘   │
         │  32 / 500 characters (min 10)               │
         │                                             │
         │  ────────────────────────────────────────── │
         │                                             │
         │  [Create Override]               [Cancel]   │
         │   (primary)                                 │
         │                                             │
         └────────────────────────────────────────────┘
```

### State: Create — Validation Error (Reason Too Short)

```
         │  Reason:                                    │
         │  ┌──────────────────────────────────────┐   │
         │  │ Trial                                │   │
         │  │                                      │   │
         │  └──────────────────────────────────────┘   │
         │  ⚠ Reason must be at least 10 characters    │
         │  5 / 500 characters (min 10)                │
         │                                             │
         │  ────────────────────────────────────────── │
         │                                             │
         │  [Create Override]               [Cancel]   │
         │   (disabled)                                │
```

### State: Edit Override

```
         ┌────────────────────────────────────────────┐
         │  Edit Override                          [✕] │
         │                                             │
         │  Tenant:  Acme Corp                         │
         │  Feature: vulnerability_scanning             │
         │  Current: Enabled by admin@reco... on 3/5   │
         │                                             │
         │  ────────────────────────────────────────── │
         │                                             │
         │  Access:                                    │
         │    (●) Enable    ( ) Disable                │
         │                                             │
         │  Reason:                                    │
         │  ┌──────────────────────────────────────┐   │
         │  │ Customer is on a trial period for    │   │
         │  │ vulnerability scanning features.     │   │
         │  └──────────────────────────────────────┘   │
         │  52 / 500 characters (min 10)               │
         │                                             │
         │  ────────────────────────────────────────── │
         │                                             │
         │  [Save Changes]                  [Cancel]   │
         │   (primary)                                 │
         │                                             │
         └────────────────────────────────────────────┘
```

---

## Screen 9: Delete Override Confirmation Modal

```
         ┌────────────────────────────────────────────┐
         │  Remove Override                        [✕] │
         │                                             │
         │  Remove override for vulnerability_scanning │
         │  on tenant Acme Corp?                       │
         │                                             │
         │  ────────────────────────────────────────── │
         │                                             │
         │  Current override: Enabled                  │
         │  Plan default:     Not in plan              │
         │  After removal:    Feature will be locked   │
         │                                             │
         │  ────────────────────────────────────────── │
         │                                             │
         │  [Remove Override]               [Cancel]   │
         │   (destructive/red)                         │
         │                                             │
         └────────────────────────────────────────────┘
```

---

## Reusable Gating Components

These components are used across all modules, not just the feature flags screens.

### FeatureGate Wrapper

**Usage:** Wraps any content that requires a feature flag check.

```
IF feature enabled:
┌──────────────────────────────────────────────────────────────────────┐
│  {children rendered normally}                                        │
└──────────────────────────────────────────────────────────────────────┘

IF feature locked (plan):
┌──────────────────────────────────────────────────────────────────────┐
│                                                                      │
│  ┌───────────────────────────────────────────────────────────────┐   │
│  │  🔒 {Feature Name} requires {required_tier}                   │   │
│  │                                                               │   │
│  │  Upgrade your plan to access this feature.                    │   │
│  │                                                               │   │
│  │  [Upgrade Plan]                                               │   │
│  └───────────────────────────────────────────────────────────────┘   │
│                                                                      │
└──────────────────────────────────────────────────────────────────────┘

IF feature disabled (operational):
┌──────────────────────────────────────────────────────────────────────┐
│                                                                      │
│  ┌───────────────────────────────────────────────────────────────┐   │
│  │  ⚠ {Feature Name} is temporarily unavailable                  │   │
│  │                                                               │   │
│  │  This feature is under maintenance. No action required.       │   │
│  └───────────────────────────────────────────────────────────────┘   │
│                                                                      │
└──────────────────────────────────────────────────────────────────────┘
```

### LockedBadge (Inline)

```
Enabled:      ✓ Included
Locked:       🔒 Requires Pro          (clickable → Upgrade Modal)
Unavailable:  ⚠ Temporarily unavailable (not clickable)
```

### OperationalBanner (Module Header)

```
┌──────────────────────────────────────────────────────────────────────┐
│  ⚠ This module is temporarily unavailable for maintenance.          │
│  No action required — it will be restored automatically.             │
└──────────────────────────────────────────────────────────────────────┘
```
