# Screens & Wireframes (Billing & Credits)

Scope: ASCII wireframes for all billing screens, covering every state (default, loading, error, empty) with conditional rendering rules.

---

## Route Structure

| Route | Access | Description |
|-------|--------|-------------|
| `/settings/billing` | Tenant Owner (edit), Members (view) | Billing overview |
| `/settings/billing/plans` | Tenant Owner | Plan comparison & selection |
| `/settings/billing/transactions` | Tenant Owner | Credit transaction history |
| `/settings/billing/credit-packs` | Tenant Owner | Purchase credit packs |
| `/admin/billing/credits` | Super Admin | Credit adjustment |
| `/admin/billing/pricing` | Super Admin | Scan step pricing config |

---

## Screen 1: Credit Balance Widget (Global Header)

**Location:** Top navigation bar, visible on all authenticated pages.

### State: Has Subscription (>50% credits)

```
┌──────────────────────────────────────────────────────────────────────┐
│  ◀ Reconova    [Dashboard] [Scans] [Domains] [Settings]    ┌─────┐ │
│                                                             │⚡127│ │
│                                                             └─────┘ │
└──────────────────────────────────────────────────────────────────────┘
                                                          credits ▲
                                                      (green, clickable)
```

### State: Low Credits (<20% remaining)

```
┌──────────────────────────────────────────────────────────────────────┐
│  ◀ Reconova    [Dashboard] [Scans] [Domains] [Settings]    ┌─────┐ │
│                                                             │⚡ 14│ │
│                                                             └─────┘ │
└──────────────────────────────────────────────────────────────────────┘
                                                          credits ▲
                                                        (red, clickable)
```

### State: Free Tier (No Subscription)

```
┌──────────────────────────────────────────────────────────────────────┐
│  ◀ Reconova    [Dashboard] [Scans] [Domains] [Settings]  [Upgrade] │
└──────────────────────────────────────────────────────────────────────┘
                                                         primary button
```

### State: Loading / Error

```
┌──────────────────────────────────────────────────────────────────────┐
│  ◀ Reconova    [Dashboard] [Scans] [Domains] [Settings]    ┌─────┐ │
│                                                             │  —  │ │
│                                                             └─────┘ │
└──────────────────────────────────────────────────────────────────────┘
                                                       dash = fallback
```

### Conditional Rendering

| Condition | Display |
|-----------|---------|
| No subscription | [Upgrade] primary button |
| Credits > 50% of allotment | Green text `⚡{N}` |
| Credits 20–50% | Yellow text `⚡{N}` |
| Credits < 20% | Red text `⚡{N}` |
| API error / loading | Dash `—` |

---

## Screen 2: Billing Overview (`/settings/billing`)

### State: Active Subscription (Default)

```
┌──────────────────────────────────────────────────────────────────────┐
│  Settings > Billing                                                  │
├──────────────────────────────────────────────────────────────────────┤
│                                                                      │
│  ┌─── Current Plan ─────────────────────────────────────────────┐   │
│  │                                                               │   │
│  │  Pro Plan                                    [Change Plan]    │   │
│  │  $49/month · Renews Mar 28, 2026                              │   │
│  │                                                               │   │
│  │  ┌─ Credits ──────────────────────────────────────────────┐   │   │
│  │  │                                                         │   │   │
│  │  │  ████████████░░░░░░░░  340 / 500 allotment remaining   │   │   │
│  │  │                        + 50 purchased                   │   │   │
│  │  │                        ─────                            │   │   │
│  │  │                        390 total available              │   │   │
│  │  │                                                         │   │   │
│  │  │  Used this period: 160                                  │   │   │
│  │  │  Resets: Mar 28, 2026                                   │   │   │
│  │  │                                                         │   │   │
│  │  └─────────────────────────────────────────────────────────┘   │   │
│  │                                                               │   │
│  └───────────────────────────────────────────────────────────────┘   │
│                                                                      │
│  ┌─── Quick Actions ────────────────────────────────────────────┐   │
│  │                                                               │   │
│  │  [Purchase Credits]    [Manage Payment Method]                │   │
│  │  [View Transaction History]                                   │   │
│  │                                                               │   │
│  │  [Cancel Subscription]  (text link, muted)                    │   │
│  │                                                               │   │
│  └───────────────────────────────────────────────────────────────┘   │
│                                                                      │
└──────────────────────────────────────────────────────────────────────┘
```

### State: Active with Pending Downgrade

```
┌──────────────────────────────────────────────────────────────────────┐
│  Settings > Billing                                                  │
├──────────────────────────────────────────────────────────────────────┤
│                                                                      │
│  ┌─── ⚠ Pending Change ────────────────────────────────────────┐   │
│  │  Downgrading to Starter on Mar 28, 2026.                      │   │
│  │  You'll keep Pro access until then.          [Cancel Downgrade]│   │
│  └───────────────────────────────────────────────────────────────┘   │
│                                                                      │
│  ┌─── Current Plan ─────────────────────────────────────────────┐   │
│  │                                                               │   │
│  │  Pro Plan                                    [Change Plan]    │   │
│  │  $49/month · Ends Mar 28, 2026                                │   │
│  │  ...                                                          │   │
│  └───────────────────────────────────────────────────────────────┘   │
│  ...                                                                 │
└──────────────────────────────────────────────────────────────────────┘
```

### State: Past Due

```
┌──────────────────────────────────────────────────────────────────────┐
│  Settings > Billing                                                  │
├──────────────────────────────────────────────────────────────────────┤
│                                                                      │
│  ┌─── ⛔ Payment Failed ───────────────────────────────────────┐   │
│  │  Your last payment failed. Update your payment method to      │   │
│  │  avoid losing access.              [Update Payment Method]    │   │
│  └───────────────────────────────────────────────────────────────┘   │
│                                                                      │
│  ┌─── Current Plan ─────────────────────────────────────────────┐   │
│  │                                                               │   │
│  │  Pro Plan                                    [Change Plan]    │   │
│  │  $49/month · Payment past due                 (disabled)      │   │
│  │  ...                                                          │   │
│  └───────────────────────────────────────────────────────────────┘   │
│                                                                      │
│  ┌─── Quick Actions ────────────────────────────────────────────┐   │
│  │                                                               │   │
│  │  [Purchase Credits]       (disabled, "Resolve payment first") │   │
│  │  [View Transaction History]                                   │   │
│  │  [Manage Payment Method]                                      │   │
│  │                                                               │   │
│  └───────────────────────────────────────────────────────────────┘   │
│                                                                      │
└──────────────────────────────────────────────────────────────────────┘
```

### State: Cancelled (Access Until Period End)

```
┌──────────────────────────────────────────────────────────────────────┐
│  Settings > Billing                                                  │
├──────────────────────────────────────────────────────────────────────┤
│                                                                      │
│  ┌─── Subscription Cancelled ──────────────────────────────────┐   │
│  │  Your Pro plan cancels on Mar 28, 2026. You'll retain full    │   │
│  │  access until then.                         [Reactivate]      │   │
│  └───────────────────────────────────────────────────────────────┘   │
│                                                                      │
│  ┌─── Current Plan ─────────────────────────────────────────────┐   │
│  │                                                               │   │
│  │  Pro Plan (Cancelling)                       [View Plans]     │   │
│  │  Access until Mar 28, 2026                                    │   │
│  │  ...credits section unchanged...                              │   │
│  └───────────────────────────────────────────────────────────────┘   │
│  ...                                                                 │
└──────────────────────────────────────────────────────────────────────┘
```

### State: Free Tier (No Subscription)

```
┌──────────────────────────────────────────────────────────────────────┐
│  Settings > Billing                                                  │
├──────────────────────────────────────────────────────────────────────┤
│                                                                      │
│  ┌─── No Active Plan ──────────────────────────────────────────┐   │
│  │                                                               │   │
│  │  You're on the Free tier.                                     │   │
│  │  Upgrade to start scanning and monitoring your domains.       │   │
│  │                                                               │   │
│  │                    [View Plans]                                │   │
│  │                                                               │   │
│  └───────────────────────────────────────────────────────────────┘   │
│                                                                      │
└──────────────────────────────────────────────────────────────────────┘
```

### State: Loading

```
┌──────────────────────────────────────────────────────────────────────┐
│  Settings > Billing                                                  │
├──────────────────────────────────────────────────────────────────────┤
│                                                                      │
│  ┌─── Current Plan ─────────────────────────────────────────────┐   │
│  │                                                               │   │
│  │  ░░░░░░░░░░░░░░░░░░░░░░░░░░                                  │   │
│  │  ░░░░░░░░░░░░░░░░░░                                          │   │
│  │                                                               │   │
│  │  ┌─ Credits ──────────────────────────────────────────────┐   │   │
│  │  │  ░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░                      │   │   │
│  │  │  ░░░░░░░░░░░░░░░                                        │   │   │
│  │  └─────────────────────────────────────────────────────────┘   │   │
│  │                                                               │   │
│  └───────────────────────────────────────────────────────────────┘   │
│                                                                      │
└──────────────────────────────────────────────────────────────────────┘
```

### State: Error

```
┌──────────────────────────────────────────────────────────────────────┐
│  Settings > Billing                                                  │
├──────────────────────────────────────────────────────────────────────┤
│                                                                      │
│  ┌───────────────────────────────────────────────────────────────┐   │
│  │                                                               │   │
│  │          Unable to load billing information.                  │   │
│  │                     [Retry]                                   │   │
│  │                                                               │   │
│  └───────────────────────────────────────────────────────────────┘   │
│                                                                      │
└──────────────────────────────────────────────────────────────────────┘
```

### Conditional Rendering

| Condition | Behavior |
|-----------|----------|
| `role != TENANT_OWNER` | Hide [Change Plan], [Cancel], [Purchase Credits]. Show read-only view. |
| `subscription.status == PAST_DUE` | Disable [Change Plan], [Purchase Credits]. Show past-due banner. |
| `subscription.status == CANCELLED` | Show cancellation banner with [Reactivate]. |
| `subscription.pending_plan_id != null` | Show pending downgrade banner with [Cancel Downgrade]. |
| No subscription | Show free tier CTA. Hide credits section. |

---

## Screen 3: Plan Comparison (`/settings/billing/plans`)

### State: Default (Active Subscription)

```
┌──────────────────────────────────────────────────────────────────────┐
│  Settings > Billing > Plans                                          │
├──────────────────────────────────────────────────────────────────────┤
│                                                                      │
│  Choose Your Plan              Billing: [● Monthly] [○ Annual]      │
│                                         Save 20% with annual        │
│                                                                      │
│  ┌─ Starter ──────┐  ┌─ Pro ★ ────────┐  ┌─ Enterprise ─────┐     │
│  │                 │  │  CURRENT PLAN  │  │                   │     │
│  │  $19/mo         │  │                │  │  Custom pricing   │     │
│  │  ($190/yr)      │  │  $49/mo        │  │                   │     │
│  │                 │  │  ($470/yr)     │  │  • Unlimited      │     │
│  │  • 3 domains    │  │               │  │    domains        │     │
│  │  • 100 credits  │  │  • 20 domains  │  │  • Custom credits │     │
│  │  • Basic scans  │  │  • 500 credits │  │  • All features   │     │
│  │  • Email alerts │  │  • Full scans  │  │  • All            │     │
│  │                 │  │  • Compliance  │  │    integrations   │     │
│  │                 │  │  • Integrations│  │  • Priority       │     │
│  │                 │  │  • Slack+Email │  │    support        │     │
│  │                 │  │               │  │  • 1-year         │     │
│  │                 │  │               │  │    retention      │     │
│  │                 │  │               │  │                   │     │
│  │  [Downgrade]    │  │  Current Plan  │  │  [Contact Sales]  │     │
│  │                 │  │               │  │                   │     │
│  └─────────────────┘  └────────────────┘  └───────────────────┘     │
│                                                                      │
│  ┌─── Feature Comparison ───────────────────────────────────────┐   │
│  │                                                               │   │
│  │  Feature               Starter    Pro        Enterprise      │   │
│  │  ─────────────────────────────────────────────────────────    │   │
│  │  Domains               3          20         Unlimited       │   │
│  │  Monthly credits       100        500        Custom          │   │
│  │  Subdomain enum        ✓          ✓          ✓               │   │
│  │  Port scanning         ✓          ✓          ✓               │   │
│  │  Vuln scanning         ✗          ✓          ✓               │   │
│  │  Compliance reports    ✗          ✓          ✓               │   │
│  │  Shodan integration    ✗          ✓          ✓               │   │
│  │  SecurityTrails        ✗          ✓          ✓               │   │
│  │  Censys integration    ✗          ✗          ✓               │   │
│  │  Custom connectors     ✗          ✗          ✓               │   │
│  │  Custom workflows      ✗          ✓          ✓               │   │
│  │  Scheduled scans       ✗          ✓          ✓               │   │
│  │  Notifications         Email      Slack+     All             │   │
│  │                                   Email                      │   │
│  │  Data retention        30 days    90 days    1 year          │   │
│  │                                                               │   │
│  └───────────────────────────────────────────────────────────────┘   │
│                                                                      │
└──────────────────────────────────────────────────────────────────────┘
```

### State: Free Tier User

```
Same layout but:
- No "CURRENT PLAN" badge on any card
- All three cards show [Select Plan] instead of Upgrade/Downgrade
- Header: "Choose a plan to get started"
```

### State: Cancelled Subscription

```
Same layout but:
- Current plan badge shows "CANCELLING"
- Higher plans show [Reactivate & Upgrade]
- Same plan shows [Reactivate]
```

### Upgrade Confirmation Modal

```
┌──────────────────────────────────────────────────────────────────────┐
│                                                                      │
│  Upgrade to Pro                                              [✕]    │
│  ────────────────────────────────────────────────────────────────    │
│                                                                      │
│  Starter → Pro                                                       │
│                                                                      │
│  • Pro-rated charge today: $32.50                                    │
│  • Your credits will reset to 500                                    │
│  • New features available immediately                                │
│                                                                      │
│  ┌───────────────────────────────────────────────────────────────┐   │
│  │  ℹ This will cancel your pending downgrade to Starter.        │   │
│  └───────────────────────────────────────────────────────────────┘   │
│  (only shown IF pending_plan_id is set)                              │
│                                                                      │
│                          [Cancel]    [Confirm Upgrade — $32.50]      │
│                                                                      │
└──────────────────────────────────────────────────────────────────────┘
```

### Downgrade Confirmation Modal

```
┌──────────────────────────────────────────────────────────────────────┐
│                                                                      │
│  Downgrade to Starter                                        [✕]    │
│  ────────────────────────────────────────────────────────────────    │
│                                                                      │
│  Pro → Starter                                                       │
│  Takes effect: Mar 28, 2026                                          │
│                                                                      │
│  You'll keep full Pro access until then.                             │
│                                                                      │
│  What changes:                                                       │
│  • Credits: 500 → 100 /month                                        │
│  • Domains: 20 → 3                                                   │
│  • Lost features: Vuln scanning, Compliance, Integrations            │
│                                                                      │
│  ┌───────────────────────────────────────────────────────────────┐   │
│  │  ⚠ You have 12 domains. Starter allows 3. You won't be       │   │
│  │  able to add new domains until you're under the limit.        │   │
│  └───────────────────────────────────────────────────────────────┘   │
│  (only shown IF domain_count > new_plan.max_domains)                 │
│                                                                      │
│                       [Cancel]    [Schedule Downgrade]               │
│                                                                      │
└──────────────────────────────────────────────────────────────────────┘
```

### Conditional Rendering

| Condition | Behavior |
|-----------|----------|
| Viewing current plan | Show "Current Plan" badge, no action button |
| Higher plan than current | Show [Upgrade] button |
| Lower plan than current | Show [Downgrade] button |
| Enterprise plan | Show [Contact Sales] instead of price |
| Annual toggle active | Show annual prices, "Save X%" badge |
| `subscription.status == PAST_DUE` | Redirect to billing overview (cannot change plan) |
| `subscription.status == CANCELLED` | Higher plans: [Reactivate & Upgrade] |
| `pending_plan_id` set | Upgrade modal includes pending downgrade cancellation note |
| `domain_count > new_plan.max_domains` | Downgrade modal shows domain limit warning |

---

## Screen 4: Transaction History (`/settings/billing/transactions`)

### State: Default (With Data)

```
┌──────────────────────────────────────────────────────────────────────┐
│  Settings > Billing > Transaction History                            │
├──────────────────────────────────────────────────────────────────────┤
│                                                                      │
│  ┌─── Filters ──────────────────────────────────────────────────┐   │
│  │  Type: [All Types ▼]     Date: [This Period ▼]               │   │
│  └───────────────────────────────────────────────────────────────┘   │
│                                                                      │
│  ┌─── Transactions ─────────────────────────────────────────────┐   │
│  │                                                               │   │
│  │  Date              Type          Description       Amount     │   │
│  │  ─────────────────────────────────────────────────────────    │   │
│  │  Mar 9, 14:23      CONSUMPTION   Scan #4821        −18       │   │
│  │                                   3 domains, Full              │   │
│  │  Mar 8, 09:15      REFUND        Scan #4819         +6       │   │
│  │                                   vuln_scan failed            │   │
│  │  Mar 8, 09:00      CONSUMPTION   Scan #4819        −24       │   │
│  │                                   4 domains, Full              │   │
│  │  Mar 5, 16:45      PURCHASE      50 Credit Pack    +50       │   │
│  │  Mar 1, 00:00      ALLOTMENT     Monthly reset    +500       │   │
│  │                                                               │   │
│  │  Showing 1–5 of 23              [◀ Prev]  1  2  3  [Next ▶]  │   │
│  │                                                               │   │
│  └───────────────────────────────────────────────────────────────┘   │
│                                                                      │
└──────────────────────────────────────────────────────────────────────┘
```

### Type Filter Dropdown

```
┌─────────────────┐
│ ● All Types     │
│ ○ Allotment     │
│ ○ Consumption   │
│ ○ Purchase      │
│ ○ Refund        │
│ ○ Adjustment    │
└─────────────────┘
```

### Date Filter Dropdown

```
┌─────────────────┐
│ ● This Period   │
│ ○ Last Period   │
│ ○ Last 90 Days  │
│ ○ All Time      │
└─────────────────┘
```

### State: Empty (No Transactions)

```
┌──────────────────────────────────────────────────────────────────────┐
│  Settings > Billing > Transaction History                            │
├──────────────────────────────────────────────────────────────────────┤
│                                                                      │
│  ┌─── Filters ──────────────────────────────────────────────────┐   │
│  │  Type: [All Types ▼]     Date: [This Period ▼]               │   │
│  └───────────────────────────────────────────────────────────────┘   │
│                                                                      │
│  ┌───────────────────────────────────────────────────────────────┐   │
│  │                                                               │   │
│  │              No credit transactions yet.                      │   │
│  │              Transactions appear after your first scan.       │   │
│  │                                                               │   │
│  └───────────────────────────────────────────────────────────────┘   │
│                                                                      │
└──────────────────────────────────────────────────────────────────────┘
```

### State: Empty (Filter Applied, No Results)

```
│  ┌───────────────────────────────────────────────────────────────┐   │
│  │                                                               │   │
│  │         No transactions match your filters.                   │   │
│  │                    [Clear Filters]                             │   │
│  │                                                               │   │
│  └───────────────────────────────────────────────────────────────┘   │
```

### State: Loading

```
│  ┌─── Transactions ─────────────────────────────────────────────┐   │
│  │                                                               │   │
│  │  ░░░░░░░░░░░░░░  ░░░░░░░░░  ░░░░░░░░░░░░░░░░░  ░░░░░       │   │
│  │  ░░░░░░░░░░░░░░  ░░░░░░░░░  ░░░░░░░░░░░░░░░░░  ░░░░░       │   │
│  │  ░░░░░░░░░░░░░░  ░░░░░░░░░  ░░░░░░░░░░░░░░░░░  ░░░░░       │   │
│  │  ░░░░░░░░░░░░░░  ░░░░░░░░░  ░░░░░░░░░░░░░░░░░  ░░░░░       │   │
│  │  ░░░░░░░░░░░░░░  ░░░░░░░░░  ░░░░░░░░░░░░░░░░░  ░░░░░       │   │
│  │                                                               │   │
│  └───────────────────────────────────────────────────────────────┘   │
```

---

## Screen 5: Credit Packs (`/settings/billing/credit-packs`)

### State: Default

```
┌──────────────────────────────────────────────────────────────────────┐
│  Settings > Billing > Credit Packs                                   │
├──────────────────────────────────────────────────────────────────────┤
│                                                                      │
│  Purchase additional credits. Credits never expire and carry over    │
│  between billing periods.                                            │
│                                                                      │
│  Current balance: 390 credits                                        │
│                                                                      │
│  ┌─ 50 Credits ───┐  ┌─ 200 Credits ──┐  ┌─ 500 Credits ──┐       │
│  │                 │  │                 │  │  BEST VALUE    │       │
│  │    50 credits   │  │   200 credits   │  │                │       │
│  │                 │  │                 │  │   500 credits  │       │
│  │    $9.99        │  │   $34.99        │  │                │       │
│  │    ($0.20/cr)   │  │   ($0.17/cr)    │  │   $74.99       │       │
│  │                 │  │                 │  │   ($0.15/cr)   │       │
│  │  [Purchase]     │  │  [Purchase]     │  │                │       │
│  │                 │  │                 │  │  [Purchase]    │       │
│  └─────────────────┘  └─────────────────┘  └────────────────┘       │
│                                                                      │
└──────────────────────────────────────────────────────────────────────┘
```

### Purchase Confirmation Modal

```
┌──────────────────────────────────────────────────────────────────────┐
│                                                                      │
│  Purchase 200 Credits                                        [✕]    │
│  ────────────────────────────────────────────────────────────────    │
│                                                                      │
│  200 credits for $34.99                                              │
│                                                                      │
│  • Credits never expire                                              │
│  • Carry over between billing periods                                │
│  • One-time purchase                                                 │
│                                                                      │
│                          [Cancel]    [Purchase — $34.99]             │
│                                                                      │
└──────────────────────────────────────────────────────────────────────┘
```

### State: No Active Subscription

```
┌──────────────────────────────────────────────────────────────────────┐
│  Settings > Billing > Credit Packs                                   │
├──────────────────────────────────────────────────────────────────────┤
│                                                                      │
│  ┌───────────────────────────────────────────────────────────────┐   │
│  │                                                               │   │
│  │  An active subscription is required to purchase credits.      │   │
│  │                       [View Plans]                             │   │
│  │                                                               │   │
│  └───────────────────────────────────────────────────────────────┘   │
│                                                                      │
└──────────────────────────────────────────────────────────────────────┘
```

### State: Past Due

```
┌──────────────────────────────────────────────────────────────────────┐
│  Settings > Billing > Credit Packs                                   │
├──────────────────────────────────────────────────────────────────────┤
│                                                                      │
│  ┌───────────────────────────────────────────────────────────────┐   │
│  │                                                               │   │
│  │  Credit purchases are disabled while your payment is          │   │
│  │  past due. Please update your payment method first.           │   │
│  │                  [Update Payment Method]                       │   │
│  │                                                               │   │
│  └───────────────────────────────────────────────────────────────┘   │
│                                                                      │
└──────────────────────────────────────────────────────────────────────┘
```

### Conditional Rendering

| Condition | Behavior |
|-----------|----------|
| No active subscription | Show subscription required message + [View Plans] |
| `subscription.status == PAST_DUE` | Show past due message + [Update Payment] |
| `role != TENANT_OWNER` | Page not accessible (redirect to billing overview) |
| Pack with best value per credit | Show "BEST VALUE" badge |

---

## Screen 6: Cancellation Confirmation Modal

```
┌──────────────────────────────────────────────────────────────────────┐
│                                                                      │
│  Cancel Subscription                                         [✕]    │
│  ────────────────────────────────────────────────────────────────    │
│                                                                      │
│  Are you sure you want to cancel your Pro plan?                      │
│                                                                      │
│  • You'll retain full access until Mar 28, 2026                      │
│  • After that, you'll be downgraded to the free tier:                │
│    – No scans                                                        │
│    – No domains                                                      │
│    – Read-only access                                                │
│  • Unused allotment credits will be lost at period end               │
│                                                                      │
│                    [Keep My Plan]    [Cancel Subscription]           │
│                                      (destructive/red)              │
│                                                                      │
└──────────────────────────────────────────────────────────────────────┘
```

---

## Screen 7: Insufficient Credits Modal (Scan Context)

```
┌──────────────────────────────────────────────────────────────────────┐
│                                                                      │
│  Insufficient Credits                                        [✕]    │
│  ────────────────────────────────────────────────────────────────    │
│                                                                      │
│  This scan requires more credits than you have available.            │
│                                                                      │
│  Required:    42 credits                                             │
│  Available:   28 credits                                             │
│  Shortfall:   14 credits                                             │
│                                                                      │
│  Options:                                                            │
│  • Purchase more credits                                             │
│  • Reduce scan scope (fewer domains or steps)                        │
│                                                                      │
│              [Reduce Scope]    [Purchase Credits]                    │
│                                                                      │
└──────────────────────────────────────────────────────────────────────┘
```

---

## Screen 8: Admin — Credit Adjustment (`/admin/billing/credits`)

### State: Default (No Tenant Selected)

```
┌──────────────────────────────────────────────────────────────────────┐
│  Admin > Billing > Credit Adjustment                                 │
├──────────────────────────────────────────────────────────────────────┤
│                                                                      │
│  ┌─── Select Tenant ────────────────────────────────────────────┐   │
│  │                                                               │   │
│  │  Search: [                                          🔍]      │   │
│  │  Search by tenant name or ID                                  │   │
│  │                                                               │   │
│  └───────────────────────────────────────────────────────────────┘   │
│                                                                      │
└──────────────────────────────────────────────────────────────────────┘
```

### State: Tenant Selected

```
┌──────────────────────────────────────────────────────────────────────┐
│  Admin > Billing > Credit Adjustment                                 │
├──────────────────────────────────────────────────────────────────────┤
│                                                                      │
│  ┌─── Select Tenant ────────────────────────────────────────────┐   │
│  │  Search: [Acme Corp                              🔍] [✕]    │   │
│  └───────────────────────────────────────────────────────────────┘   │
│                                                                      │
│  ┌─── Tenant: Acme Corp ────────────────────────────────────────┐   │
│  │                                                               │   │
│  │  Plan: Pro          Status: ACTIVE                            │   │
│  │  Period: Mar 1 – Mar 28, 2026                                 │   │
│  │                                                               │   │
│  │  Credits:                                                     │   │
│  │    Allotment remaining:   340                                 │   │
│  │    Purchased balance:      50                                 │   │
│  │    Total available:       390                                 │   │
│  │    Used this period:      160                                 │   │
│  │                                                               │   │
│  └───────────────────────────────────────────────────────────────┘   │
│                                                                      │
│  ┌─── Adjust Credits ───────────────────────────────────────────┐   │
│  │                                                               │   │
│  │  Amount:  [          ]  (positive to add, negative to deduct) │   │
│  │                                                               │   │
│  │  Reason:  [                                                 ] │   │
│  │           [                                                 ] │   │
│  │           Required — will appear in audit log                 │   │
│  │                                                               │   │
│  │                            [Cancel]    [Apply Adjustment]     │   │
│  │                                                               │   │
│  └───────────────────────────────────────────────────────────────┘   │
│                                                                      │
└──────────────────────────────────────────────────────────────────────┘
```

### Deduction Confirmation Modal

```
┌──────────────────────────────────────────────────────────────────────┐
│                                                                      │
│  Confirm Credit Deduction                                    [✕]    │
│  ────────────────────────────────────────────────────────────────    │
│                                                                      │
│  Deduct 50 credits from Acme Corp?                                   │
│                                                                      │
│  Current balance:   390                                              │
│  After adjustment:  340                                              │
│                                                                      │
│  Reason: "Refund for duplicate charge"                               │
│                                                                      │
│                       [Cancel]    [Confirm Deduction]                │
│                                                                      │
└──────────────────────────────────────────────────────────────────────┘
```

### State: Validation Errors

```
│  ┌─── Adjust Credits ───────────────────────────────────────────┐   │
│  │                                                               │   │
│  │  Amount:  [-500     ]                                         │   │
│  │           ⚠ Cannot deduct more than available (390 credits)   │   │
│  │                                                               │   │
│  │  Reason:  [                                                 ] │   │
│  │           ⚠ Reason is required                                │   │
│  │                                                               │   │
│  │                            [Cancel]    [Apply Adjustment]     │   │
│  │                                         (disabled)            │   │
│  └───────────────────────────────────────────────────────────────┘   │
```

---

## Screen 9: Admin — Scan Pricing Config (`/admin/billing/pricing`)

### State: Default (View Mode)

```
┌──────────────────────────────────────────────────────────────────────┐
│  Admin > Billing > Scan Pricing                                      │
├──────────────────────────────────────────────────────────────────────┤
│                                                                      │
│  Configure credits charged per domain for each scan step.            │
│  Changes apply to new scans only.                                    │
│                                                                      │
│  ┌─── Pricing Matrix ───────────────────────────────────────────┐   │
│  │                                                               │   │
│  │  Check Type            Starter    Pro        Enterprise      │   │
│  │  ─────────────────────────────────────────────────────────    │   │
│  │  subdomain_enum        [2]        [1]        [1]             │   │
│  │  port_scan             [3]        [2]        [1]             │   │
│  │  vuln_scan             N/A        [3]        [2]             │   │
│  │  tech_detect           [2]        [1]        [1]             │   │
│  │  screenshot            [1]        [1]        [1]             │   │
│  │  compliance_check      N/A        [4]        [3]             │   │
│  │  shodan_lookup         N/A        [3]        [2]             │   │
│  │  securitytrails        N/A        [3]        [2]             │   │
│  │  censys_lookup         N/A        N/A        [2]             │   │
│  │  custom_connector      N/A        N/A        [3]             │   │
│  │                                                               │   │
│  │  N/A = Feature not available on this tier                     │   │
│  │                                                               │   │
│  └───────────────────────────────────────────────────────────────┘   │
│                                                                      │
│  Last updated: Mar 5, 2026 by admin@reconova.io                      │
│                                                                      │
└──────────────────────────────────────────────────────────────────────┘
```

### State: Editing (Unsaved Changes)

```
│  ┌─── Pricing Matrix ───────────────────────────────────────────┐   │
│  │                                                               │   │
│  │  Check Type            Starter    Pro        Enterprise      │   │
│  │  ─────────────────────────────────────────────────────────    │   │
│  │  subdomain_enum        [2]        [1]        [1]             │   │
│  │  port_scan             [3]       *[1]*       [1]             │   │
│  │  vuln_scan             N/A       *[2]*       [2]             │   │
│  │  ...                                                          │   │
│  │                                                               │   │
│  │  * = modified (unsaved)                                       │   │
│  │                                                               │   │
│  │                     [Discard Changes]    [Save Changes]       │   │
│  │                                                               │   │
│  └───────────────────────────────────────────────────────────────┘   │
```

### Save Confirmation Modal

```
┌──────────────────────────────────────────────────────────────────────┐
│                                                                      │
│  Update Scan Pricing                                         [✕]    │
│  ────────────────────────────────────────────────────────────────    │
│                                                                      │
│  Update pricing for 2 check types?                                   │
│                                                                      │
│  Changes:                                                            │
│  • port_scan (Pro): 2 → 1 credits/domain                            │
│  • vuln_scan (Pro): 3 → 2 credits/domain                            │
│                                                                      │
│  ⚠ Changes apply to new scans only. In-progress scans               │
│    are not affected.                                                 │
│                                                                      │
│                          [Cancel]    [Confirm Changes]               │
│                                                                      │
└──────────────────────────────────────────────────────────────────────┘
```

### State: Loading

```
│  ┌─── Pricing Matrix ───────────────────────────────────────────┐   │
│  │                                                               │   │
│  │  ░░░░░░░░░░░░░░░░░░  ░░░░░░  ░░░░░░  ░░░░░░░░░░            │   │
│  │  ░░░░░░░░░░░░░░░░░░  ░░░░░░  ░░░░░░  ░░░░░░░░░░            │   │
│  │  ░░░░░░░░░░░░░░░░░░  ░░░░░░  ░░░░░░  ░░░░░░░░░░            │   │
│  │  ░░░░░░░░░░░░░░░░░░  ░░░░░░  ░░░░░░  ░░░░░░░░░░            │   │
│  │                                                               │   │
│  └───────────────────────────────────────────────────────────────┘   │
```

---

## Global Banners (Rendered at Top of Every Page)

### Free Tier Banner

```
┌──────────────────────────────────────────────────────────────────────┐
│  ℹ You're on the Free tier. Upgrade to start scanning.  [View Plans]│
└──────────────────────────────────────────────────────────────────────┘
```

### Past Due Banner

```
┌──────────────────────────────────────────────────────────────────────┐
│  ⛔ Payment failed. Update your payment method.  [Update Payment]   │
└──────────────────────────────────────────────────────────────────────┘
```

### Cancellation Banner

```
┌──────────────────────────────────────────────────────────────────────┐
│  ⚠ Your Pro plan cancels on Mar 28, 2026.              [Reactivate] │
└──────────────────────────────────────────────────────────────────────┘
```

### Low Credits Banner

```
┌──────────────────────────────────────────────────────────────────────┐
│  ⚠ Running low on credits (14 remaining).         [Purchase Credits]│
└──────────────────────────────────────────────────────────────────────┘
```

### Banner Priority (Top to Bottom, Show Highest Only)

| Priority | Condition | Banner |
|----------|-----------|--------|
| 1 | `subscription.status == PAST_DUE` | Past Due |
| 2 | No subscription | Free Tier |
| 3 | `subscription.status == CANCELLED` | Cancellation |
| 4 | Credits < 20% of allotment | Low Credits |
