# Integrations — Frontend Plan

Scope: Tenant notification integration management (Email, Slack, Jira, Webhook, SIEM) with per-integration notification rules, delivery history, and test functionality; super admin platform API key pool management, usage tracking dashboard, and provider rate limit configuration.

**Based on:** `docs/plans/business-rules/08-integrations.md` (BR-INT-001 — BR-INT-012)
**Last updated:** 2026-03-10

---

## Documentation Index

| # | Artifact | Description | Audience |
|---|----------|-------------|----------|
| 1 | [README.md](./README.md) | Overview, state machines, navigation map | All |
| 2 | [user-flows.md](./user-flows.md) | User journey flowcharts, branching logic | Design / Frontend |
| 3 | [screens-wireframes.md](./screens-wireframes.md) | ASCII wireframes for every screen state | Design / Frontend |
| 4 | [implementation-guide.md](./implementation-guide.md) | State management, API integration, components | Frontend devs |
| 5 | [reference.md](./reference.md) | Error handling, validation, security | Frontend devs |

---

## Business Rule Coverage

| BR Code | Rule Name | Frontend Feature |
|---------|-----------|------------------|
| BR-INT-001 | Add Platform API Key | Admin: API key creation form per provider |
| BR-INT-002 | Rotate API Key | Admin: key rotation action (retire old + create new) |
| BR-INT-003 | Per-Tenant Rate Limit | Admin: rate limit config per provider (display only for tenants) |
| BR-INT-004 | API Usage Tracking | Admin: usage dashboard with per-provider, per-tenant, per-key charts |
| BR-INT-005 | Provider Access Check | (Backend-only — frontend shows provider availability by tier) |
| BR-INT-007 | Create/Update/Delete Integration | Tenant: integration CRUD with channel-specific config forms |
| BR-INT-007c | Test Integration | Tenant: [Test] button per integration with delivery result |
| BR-INT-008 | Notification Delivery & Retry | Tenant: delivery history with status badges (delivered/retry/failed) |
| BR-INT-009 | Notification Rules | Tenant: per-integration event type toggles with severity filters |
| BR-INT-010 | Integration Limits by Tier | Tenant: active count indicator, locked channels for lower tiers |
| BR-INT-010a | Re-enable Integration | Tenant: enable toggle with tier limit check |
| BR-INT-012 | Webhook Security | Tenant: webhook secret display (copy-to-clipboard) for verification |

### Post-MVP Features (Documented but Not Built)

| BR Code | Feature | Marker |
|---------|---------|--------|
| BR-INT-005a/b/c | Custom API Connectors | `[POST-MVP]` — Enterprise-only custom REST API connectors |
| BR-INT-006 | VirusTotal Integration | `[POST-MVP]` — Provider marked post-MVP in registry |

---

## User Roles

| Role | Screens Accessible | Key Actions |
|------|-------------------|-------------|
| **Tenant Owner** | Integration list, integration detail (config + rules + history) | CRUD integrations, configure notification rules, test integrations, view history |
| **Tenant Member** | Integration list (read-only) | View configured integrations and history (cannot create/edit/delete) |
| **Super Admin** | Admin integrations panel (API keys, usage, providers) | CRUD API keys, rotate keys, view usage, configure rate limits |

---

## Platform API Key Status Lifecycle

```
              ┌──────────┐
  Added ──────►  ACTIVE   │◄─── Re-enable (admin)
              └──┬──┬──┬──┘        ▲
                 │  │  │           │
   Rate limited  │  │  │ Disable   │
                 ▼  │  ▼           │
     ┌──────────────┐ ┌──────────┐│
     │ RATE_LIMITED  │ │ DISABLED ├┘
     └──────┬───────┘ └──────────┘
            │
   Auto-recover (rate window)
            │
            ▼
     ┌──────────┐
     │  ACTIVE  │ (returns to pool)
     └──────────┘

   Quota flow:
     ACTIVE ──► QUOTA_EXHAUSTED ──► ACTIVE (monthly reset or admin override)

   Rotation:
     ACTIVE ──► RETIRED (terminal, replaced by new key)
```

### Key Status Values

| Status | Meaning | Badge Color |
|--------|---------|-------------|
| ACTIVE | Available for scan jobs | Green |
| RATE_LIMITED | Temporarily exhausted, auto-recovers | Yellow |
| QUOTA_EXHAUSTED | Monthly quota reached | Orange |
| RETIRED | Replaced by rotation | Grey |
| DISABLED | Manually disabled by admin | Red |

### Key State Transitions

| Current | Action | Next | Who |
|---------|--------|------|-----|
| ACTIVE | Rate limit hit | RATE_LIMITED | System |
| RATE_LIMITED | Rate window resets | ACTIVE | System |
| ACTIVE | Quota exhausted | QUOTA_EXHAUSTED | System |
| QUOTA_EXHAUSTED | Monthly reset or admin override | ACTIVE | System / Admin |
| ACTIVE | Rotation (new key replaces) | RETIRED | Admin |
| ACTIVE | Manual disable | DISABLED | Admin |
| DISABLED | Re-enable | ACTIVE | Admin |

---

## Notification Delivery Status Model

```
┌─────────┐     ┌───────────┐     ┌───────────┐     ┌───────────┐
│ PENDING │────►│  RETRY_1  │────►│  RETRY_2  │────►│  RETRY_3  │
└────┬────┘     └─────┬─────┘     └─────┬─────┘     └─────┬─────┘
     │                │                  │                  │
     │ success        │ success          │ success          │ fail
     ▼                ▼                  ▼                  ▼
┌───────────┐   ┌───────────┐     ┌───────────┐     ┌──────────┐
│ DELIVERED │   │ DELIVERED │     │ DELIVERED │     │  FAILED  │
└───────────┘   └───────────┘     └───────────┘     └──────────┘
```

### Delivery Status Badges

| Status | Badge | Description |
|--------|-------|-------------|
| PENDING | Grey | Queued, not yet attempted |
| DELIVERED | Green | Successfully sent |
| RETRY_1 | Yellow | 1st attempt failed, retrying in 1 min |
| RETRY_2 | Yellow | 2nd attempt failed, retrying in 5 min |
| RETRY_3 | Orange | 3rd attempt failed, final retry in 15 min |
| FAILED | Red | All 4 attempts exhausted |

---

## Integration Limits by Tier

| Tier | Max Active Integrations | Available Channels | Max Rules/Integration | Max Rules Total |
|------|------------------------|-------------------|----------------------|----------------|
| Starter | 2 | Email | 20 | 50 |
| Pro | 5 | Email, Slack | 20 | 100 |
| Enterprise | Unlimited | Email, Slack, Jira, Webhook, SIEM | 20 | Unlimited |

---

## Notification Event Types

| Event Type | Slug | Default Severity | Description |
|------------|------|-----------------|-------------|
| Scan Complete | `SCAN_COMPLETE` | INFO | Scan job finished successfully |
| Scan Failed | `SCAN_FAILED` | HIGH | Scan job failed or timed out |
| CVE Alert (Critical) | `CVE_ALERT_CRITICAL` | CRITICAL | New critical CVE matches tech stack |
| CVE Alert (High Digest) | `CVE_ALERT_HIGH_DIGEST` | HIGH | Daily digest of high-severity CVE matches |
| Credit Low | `CREDIT_LOW` | WARNING | Credits below 20% of monthly allotment |
| Compliance Report Ready | `COMPLIANCE_REPORT_READY` | INFO | Compliance assessment completed |
| Compliance Score Change | `COMPLIANCE_SCORE_CHANGE` | WARNING | Score changed by ≥5 points |

---

## Screen Navigation Map

```
/settings/integrations (Tenant)
  ├── Integration List (default)
  │     ├── Active integrations count: "{N} of {limit} active"
  │     ├── Integration cards (per configured integration)
  │     │     ├── Channel icon + name + status (enabled/disabled)
  │     │     ├── Quick actions: [Test] [Enable/Disable] [Edit] [Delete]
  │     │     └── Expand → Integration detail
  │     │           ├── Configuration tab (channel-specific fields)
  │     │           ├── Notification Rules tab (event type toggles + severity filters)
  │     │           └── History tab (recent deliveries with status badges)
  │     ├── [Add Integration] → channel selection → config modal
  │     └── Locked channels shown with <LockedBadge> for unavailable tiers
  │
  └── Add Integration flow
        ├── Step 1: Select channel type (Email/Slack/Jira/Webhook/SIEM)
        └── Step 2: Configure channel-specific settings → Create

/admin/integrations (Super Admin)
  ├── API Keys tab (default)
  │     ├── Provider groups (Shodan, SecurityTrails, Censys, etc.)
  │     │     ├── Key pool table: status, quota, usage %, last used
  │     │     └── Actions: Add Key, Rotate, Disable, Enable
  │     └── Quota alert indicators (80% warning, 100% exhausted)
  │
  ├── Usage Dashboard tab
  │     ├── Usage summary cards (total calls today/month)
  │     ├── Usage by provider (bar chart or table)
  │     ├── Usage by tenant (top consumers table)
  │     └── Date range filter
  │
  └── Provider Config tab
        ├── Provider list with rate limit settings
        └── Per-provider: tenant calls/hour, tenant calls/day (editable)
```

### Screen Summary

| # | Screen | Route | Access | Notes |
|---|--------|-------|--------|-------|
| 1 | Integration List | `/settings/integrations` | Tenant Owner (edit), Members (view) | Channel CRUD + expand to detail |
| 2 | Integration Detail (expanded) | `/settings/integrations` (inline) | Tenant Owner (edit), Members (view) | Config + rules + history tabs |
| 3 | Add Integration Modal | `/settings/integrations` (modal) | Tenant Owner | Channel selection + config form |
| 4 | Admin API Keys | `/admin/integrations` (keys tab) | Super Admin | Key pool per provider |
| 5 | Admin Usage Dashboard | `/admin/integrations` (usage tab) | Super Admin | API call analytics |
| 6 | Admin Provider Config | `/admin/integrations` (providers tab) | Super Admin | Rate limit settings |

---

## Feature Flag Integration

| Flag | Effect |
|------|--------|
| `notifications_email` | Email channel availability. Starter+ (always available). |
| `notifications_slack` | Slack channel availability. Pro+ only. |
| `notifications_jira` | Jira channel availability. Enterprise only. |
| `notifications_webhook` | Webhook channel availability. Enterprise only. |
| `notifications_siem` | SIEM channel availability. Enterprise only. |

See [Feature Flags & Access Control plan](../feature-flags-access-control/README.md) for gating component specs.

---

## Banners & Global States

| Condition | Banner/Indicator | Actions |
|-----------|-----------------|---------|
| Channel not available on plan | `<LockedBadge>` on channel type in add flow | [Upgrade Plan] tooltip |
| Active integration limit reached | Warning: "You've reached the integration limit for your plan." | [Upgrade Plan] CTA |
| Integration test failed | Toast (error) with failure reason | [Retry Test] on integration card |
| Integration delivery failures (recent) | Warning badge on integration card | Expand → History tab to investigate |
| Webhook secret regenerated | Info toast: "Webhook secret regenerated. Update your endpoint." | Copy new secret |
| Platform API key quota at 80% | Admin: yellow warning on key row | No action (informational) |
| Platform API key quota exhausted | Admin: red badge, key status QUOTA_EXHAUSTED | [Reset Quota] or wait for monthly reset |
| All keys for a provider exhausted/disabled | Admin: provider section shows "No active keys" warning | [Add Key] CTA |
| Tenant downgraded — integrations auto-disabled | Info banner: "Some integrations were disabled due to plan change." | [Upgrade Plan] CTA |
