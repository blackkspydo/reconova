# Screens & Wireframes (Super Admin & Operations)

Scope: ASCII wireframes for admin dashboard, monitoring detail, alert management, maintenance mode modal, and impersonation indicator.

---

## Route Structure

| Route | Screen | Access |
|-------|--------|--------|
| `/admin` | Admin Dashboard | SUPER_ADMIN |
| `/admin/monitoring` | Monitoring Detail | SUPER_ADMIN |
| `/admin/monitoring/alerts` | Alert Management | SUPER_ADMIN |
| (overlay) | Maintenance Mode Modal | SUPER_ADMIN |
| (global floating) | Impersonation Indicator | SUPER_ADMIN (during active session) |

---

## Screen 1: Admin Dashboard

### State 1A: Default (Loaded)

```
┌──────────────────────────────────────────────────────────────────────┐
│ ┌─ Admin Header ──────────────────────────────────────────────────┐  │
│ │ Reconova Admin                    🔔 3  ⚙ Maintenance: Off  👤 │  │
│ └─────────────────────────────────────────────────────────────────┘  │
├────────────┬─────────────────────────────────────────────────────────┤
│            │                                                         │
│  Admin     │  Dashboard                                              │
│  Sidebar   │                                                         │
│            │  ┌──────────────┐ ┌──────────────┐ ┌──────────────┐    │
│  Dashboard │  │ Active       │ │ Active       │ │ Credits      │    │
│  ──────    │  │ Tenants      │ │ Scans        │ │ Consumed     │    │
│  Tenants   │  │              │ │              │ │ (30d)        │    │
│  Users     │  │    1,247     │ │      23      │ │   48,320     │    │
│  Features  │  │   ▲ +12%    │ │  Queue: 45   │ │   ▲ +8%     │    │
│  Integr.   │  └──────────────┘ └──────────────┘ └──────────────┘    │
│  Compliance│                                                         │
│  Billing   │  ┌──────────────┐                                      │
│  CVE       │  │ System       │                                      │
│  Scans     │  │ Health       │                                      │
│  Monitoring│  │              │                                      │
│            │  │   ● Healthy  │                                      │
│            │  │  0.2% errors │                                      │
│            │  └──────────────┘                                      │
│            │                                                         │
│            │  Quick Links                                            │
│            │  ┌────────┐ ┌────────┐ ┌────────┐ ┌────────┐          │
│            │  │Tenants │ │ Users  │ │Features│ │Integr. │          │
│            │  └────────┘ └────────┘ └────────┘ └────────┘          │
│            │  ┌────────┐ ┌────────┐ ┌────────┐ ┌────────┐          │
│            │  │Complnce│ │Billing │ │  CVE   │ │ Scans  │          │
│            │  └────────┘ └────────┘ └────────┘ └────────┘          │
│            │                                                         │
│            │  [View Monitoring →]                                    │
│            │                                                         │
└────────────┴─────────────────────────────────────────────────────────┘
```

### State 1B: Loading

```
┌──────────────────────────────────────────────────────────────────────┐
│ ┌─ Admin Header ──────────────────────────────────────────────────┐  │
│ │ Reconova Admin                    🔔 ·  ⚙ ···············  👤 │  │
│ └─────────────────────────────────────────────────────────────────┘  │
├────────────┬─────────────────────────────────────────────────────────┤
│            │                                                         │
│  Admin     │  Dashboard                                              │
│  Sidebar   │                                                         │
│            │  ┌──────────────┐ ┌──────────────┐ ┌──────────────┐    │
│            │  │ ░░░░░░░░░░░░ │ │ ░░░░░░░░░░░░ │ │ ░░░░░░░░░░░░ │    │
│            │  │ ░░░░░░░░░░░░ │ │ ░░░░░░░░░░░░ │ │ ░░░░░░░░░░░░ │    │
│            │  │ ░░░░░░░░░░░░ │ │ ░░░░░░░░░░░░ │ │ ░░░░░░░░░░░░ │    │
│            │  └──────────────┘ └──────────────┘ └──────────────┘    │
│            │                                                         │
│            │  ┌──────────────┐                                      │
│            │  │ ░░░░░░░░░░░░ │                                      │
│            │  │ ░░░░░░░░░░░░ │                                      │
│            │  └──────────────┘                                      │
│            │                                                         │
│            │  Quick Links                                            │
│            │  ┌────────┐ ┌────────┐ ┌────────┐ ┌────────┐          │
│            │  │ ░░░░░░ │ │ ░░░░░░ │ │ ░░░░░░ │ │ ░░░░░░ │          │
│            │  └────────┘ └────────┘ └────────┘ └────────┘          │
│            │                                                         │
└────────────┴─────────────────────────────────────────────────────────┘
```

### State 1C: Error

```
┌──────────────────────────────────────────────────────────────────────┐
│ ┌─ Admin Header ──────────────────────────────────────────────────┐  │
│ │ Reconova Admin                    🔔 ·  ⚙ Maintenance: Off  👤 │  │
│ └─────────────────────────────────────────────────────────────────┘  │
├────────────┬─────────────────────────────────────────────────────────┤
│            │                                                         │
│  Admin     │  Dashboard                                              │
│  Sidebar   │                                                         │
│            │  ┌──────────────────────────────────────────────────┐   │
│            │  │                                                  │   │
│            │  │   ⚠ Failed to load dashboard data.              │   │
│            │  │                                                  │   │
│            │  │   [Retry]                                        │   │
│            │  │                                                  │   │
│            │  └──────────────────────────────────────────────────┘   │
│            │                                                         │
│            │  Quick Links                                            │
│            │  ┌────────┐ ┌────────┐ ┌────────┐ ┌────────┐          │
│            │  │Tenants │ │ Users  │ │Features│ │Integr. │          │
│            │  └────────┘ └────────┘ └────────┘ └────────┘          │
│            │  (Quick links always render — static content)           │
│            │                                                         │
└────────────┴─────────────────────────────────────────────────────────┘
```

### State 1D: Maintenance Mode Active

```
┌──────────────────────────────────────────────────────────────────────┐
│ ┌─ Admin Header ──────────────────────────────────────────────────┐  │
│ │ Reconova Admin            🔔 3  ⚙ Maintenance: ON (43m)    👤 │  │
│ │                                   ▲ orange badge + timer        │  │
│ └─────────────────────────────────────────────────────────────────┘  │
├────────────┬─────────────────────────────────────────────────────────┤
│            │                                                         │
│  Admin     │  ┌──────────────────────────────────────────────────┐   │
│  Sidebar   │  │ 🟠 Maintenance mode active. Scan creation is    │   │
│            │  │ blocked. Est. remaining: 43 minutes.             │   │
│            │  │                           [Disable Maintenance]  │   │
│            │  └──────────────────────────────────────────────────┘   │
│            │                                                         │
│            │  Dashboard                                              │
│            │  (... summary cards as State 1A ...)                    │
│            │                                                         │
└────────────┴─────────────────────────────────────────────────────────┘
```

### State 1E: With Active Alerts

```
┌──────────────────────────────────────────────────────────────────────┐
│ ┌─ Admin Header ──────────────────────────────────────────────────┐  │
│ │ Reconova Admin                    🔔 3  ⚙ Maintenance: Off  👤 │  │
│ │                                    ▲                            │  │
│ │                        ┌───────────┴──────────────────┐        │  │
│ │                        │ Active Alerts                 │        │  │
│ │                        │                               │        │  │
│ │                        │ 🔴 Zero active workers  2m   │        │  │
│ │                        │ 🟡 Queue depth at 85%  15m   │        │  │
│ │                        │ 🟡 Worker stale: w-03  22m   │        │  │
│ │                        │                               │        │  │
│ │                        │ [View All Alerts]             │        │  │
│ │                        └───────────────────────────────┘        │  │
│ └─────────────────────────────────────────────────────────────────┘  │
├────────────┬─────────────────────────────────────────────────────────┤
│            │  (... dashboard content as State 1A ...)                │
│            │  System Health card shows red dot instead of green:     │
│            │  ┌──────────────┐                                      │
│            │  │ System       │                                      │
│            │  │ Health       │                                      │
│            │  │              │                                      │
│            │  │  🔴 Issues   │                                      │
│            │  │  3 alerts    │                                      │
│            │  └──────────────┘                                      │
└────────────┴─────────────────────────────────────────────────────────┘
```

---

## Screen 2: Monitoring Detail

### State 2A: Default (All Sections Loaded)

```
┌──────────────────────────────────────────────────────────────────────┐
│ ┌─ Admin Header ──────────────────────────────────────────────────┐  │
│ │ Reconova Admin                    🔔 3  ⚙ Maintenance: Off  👤 │  │
│ └─────────────────────────────────────────────────────────────────┘  │
├────────────┬─────────────────────────────────────────────────────────┤
│            │                                                         │
│  Admin     │  ← Dashboard  /  Monitoring                             │
│  Sidebar   │                                                         │
│            │  ┌─ Tenant Metrics ──────────── Updated: 12:05 ──┐     │
│            │  │                                                │     │
│            │  │ ┌──────────┐ ┌──────────┐ ┌──────────┐       │     │
│            │  │ │ Active   │ │ New (7d) │ │ Suspended│       │     │
│            │  │ │  1,247   │ │    34    │ │    8     │       │     │
│            │  │ └──────────┘ └──────────┘ └──────────┘       │     │
│            │  │ ┌──────────┐ ┌──────────┐                    │     │
│            │  │ │ New (30d)│ │ Churn    │                    │     │
│            │  │ │   142    │ │   12     │                    │     │
│            │  │ └──────────┘ └──────────┘                    │     │
│            │  └────────────────────────────────────────────────┘     │
│            │                                                         │
│            │  ┌─ Scan Metrics ──────────── Updated: 12:05:30 ─┐     │
│            │  │                                                │     │
│            │  │ ┌──────────┐ ┌──────────┐ ┌──────────┐       │     │
│            │  │ │ Active   │ │ Queue    │ │ Done(24h)│       │     │
│            │  │ │   23     │ │   45     │ │  1,203   │       │     │
│            │  │ └──────────┘ └──────────┘ └──────────┘       │     │
│            │  │ ┌──────────┐ ┌──────────┐                    │     │
│            │  │ │ Fail Rate│ │ Avg Time │                    │     │
│            │  │ │  2.1%    │ │  4m 32s  │                    │     │
│            │  │ └──────────┘ └──────────┘                    │     │
│            │  └────────────────────────────────────────────────┘     │
│            │                                                         │
│            │  ┌─ Worker Metrics ──────────── Updated: 12:05:30 ┐    │
│            │  │                                                 │    │
│            │  │ ┌──────────┐ ┌──────────┐ ┌──────────┐        │    │
│            │  │ │ Active   │ │ Stale    │ │ Utiliz.  │        │    │
│            │  │ │   8      │ │   1      │ │  72%     │        │    │
│            │  │ └──────────┘ └──────────┘ └──────────┘        │    │
│            │  └─────────────────────────────────────────────────┘    │
│            │                                                         │
│            │  ┌─ Credit & Revenue ──────── Updated: 11:00 ────┐     │
│            │  │                                                │     │
│            │  │ ┌──────────┐ ┌──────────┐ ┌──────────┐       │     │
│            │  │ │ Consumed │ │ MRR Est. │ │ Packs    │       │     │
│            │  │ │  48,320  │ │ $12,400  │ │   67     │       │     │
│            │  │ │  (30d)   │ │          │ │  (30d)   │       │     │
│            │  │ └──────────┘ └──────────┘ └──────────┘       │     │
│            │  └────────────────────────────────────────────────┘     │
│            │                                                         │
│            │  ┌─ System Health ──────────── Updated: 12:05 ───┐     │
│            │  │                                                │     │
│            │  │ ┌──────────┐ ┌──────────┐ ┌──────────┐       │     │
│            │  │ │ API Err  │ │ Latency  │ │ DB Pool  │       │     │
│            │  │ │  0.2%    │ │ p95: 42ms│ │  34/100  │       │     │
│            │  │ └──────────┘ └──────────┘ └──────────┘       │     │
│            │  │ ┌──────────┐ ┌──────────┐                    │     │
│            │  │ │ Redis    │ │ API Keys │                    │     │
│            │  │ │  128MB   │ │ 12 active│                    │     │
│            │  │ └──────────┘ └──────────┘                    │     │
│            │  └────────────────────────────────────────────────┘     │
│            │                                                         │
│            │  [Manage Alerts →]                                      │
│            │                                                         │
└────────────┴─────────────────────────────────────────────────────────┘
```

### State 2B: With Alert-Triggering Metrics

```
(Same layout as 2A but affected metric cards have colored borders)

│  │ ┌──────────┐ ┌──────────┐ ┌──────────┐       │
│  │ │ Active   │ │🟡Queue   │ │ Done(24h)│       │
│  │ │   23     │ │▌  85     │ │  1,203   │       │
│  │ │          │ │▌⚠ >80%  │ │          │       │
│  │ └──────────┘ └──────────┘ └──────────┘       │
                    ▲ yellow border + warning icon

│  │ ┌──────────┐ ┌──────────┐ ┌──────────┐        │
│  │ │🔴Active  │ │ Stale    │ │ Utiliz.  │        │
│  │ │▌   0     │ │   1      │ │  0%      │        │
│  │ │▌⚠ CRIT  │ │          │ │          │        │
│  │ └──────────┘ └──────────┘ └──────────┘        │
      ▲ red border + pulse animation
```

### State 2C: Section Collapsed

```
│            │  ┌─ Tenant Metrics ─────────── Updated: 12:05 ──▶┐     │
│            │  └────────────────────────────────────────────────┘     │
│            │    ▲ collapsed — click header to expand                 │
│            │    ▶ chevron points right when collapsed                │
│            │                                                         │
│            │  ┌─ Scan Metrics ──────────── Updated: 12:05:30 ─┐     │
│            │  │ (... expanded content ...)                     │     │
│            │  └────────────────────────────────────────────────┘     │
```

### State 2D: Metric Card Drilled Down

```
│  │ ┌──────────┐ ┌──────────┐ ┌──────────┐       │
│  │ │ Active   │ │ New (7d) │ │ Suspended│       │
│  │ │  1,247 ▼ │ │    34    │ │    8     │       │
│  │ └──────────┘ └──────────┘ └──────────┘       │
│  │                                                │
│  │ ┌─ Active Tenants Breakdown ────────────────┐ │
│  │ │                                            │ │
│  │ │  Starter    ████████████████░░░  842 (68%) │ │
│  │ │  Pro        ████████░░░░░░░░░░  312 (25%) │ │
│  │ │  Enterprise ██░░░░░░░░░░░░░░░░   93  (7%) │ │
│  │ │                                            │ │
│  │ └────────────────────────────────────────────┘ │
```

### State 2E: Section Fetch Error (Partial)

```
│            │  ┌─ Tenant Metrics ──────────────────────────────┐     │
│            │  │                                                │     │
│            │  │  (... metrics render normally ...)             │     │
│            │  │                                                │     │
│            │  └────────────────────────────────────────────────┘     │
│            │                                                         │
│            │  ┌─ Scan Metrics ────────────────────────────────┐     │
│            │  │                                                │     │
│            │  │   ⚠ Failed to load scan metrics.              │     │
│            │  │   Data may be stale. Last updated: 12:03      │     │
│            │  │   [Retry]                                      │     │
│            │  │                                                │     │
│            │  └────────────────────────────────────────────────┘     │
│            │                                                         │
│            │  ┌─ Worker Metrics ──────────────────────────────┐     │
│            │  │                                                │     │
│            │  │  (... metrics render normally ...)             │     │
│            │  │                                                │     │
│            │  └────────────────────────────────────────────────┘     │
```

### State 2F: Browser Notification Prompt (First Visit)

```
│            │                                                         │
│  Admin     │  ← Dashboard  /  Monitoring                             │
│  Sidebar   │                                                         │
│            │  ┌──────────────────────────────────────────────────┐   │
│            │  │ 🔔 Enable browser notifications for critical     │   │
│            │  │ alerts?               [Enable]  [Not Now]        │   │
│            │  └──────────────────────────────────────────────────┘   │
│            │                                                         │
│            │  ┌─ Tenant Metrics ──────────── Updated: 12:05 ──┐     │
│            │  │  (... content ...)                             │     │
```

---

## Screen 3: Alert Management

### State 3A: Default (Alerts + Rules)

```
┌──────────────────────────────────────────────────────────────────────┐
│ ┌─ Admin Header ──────────────────────────────────────────────────┐  │
│ │ Reconova Admin                    🔔 3  ⚙ Maintenance: Off  👤 │  │
│ └─────────────────────────────────────────────────────────────────┘  │
├────────────┬─────────────────────────────────────────────────────────┤
│            │                                                         │
│  Admin     │  ← Monitoring  /  Alert Management                     │
│  Sidebar   │                                                         │
│            │  ┌─ Active Alerts ───────────────────────────────┐     │
│            │  │                                                │     │
│            │  │ 🔴 CRITICAL  Zero active workers               │     │
│            │  │              Triggered 2 minutes ago            │     │
│            │  │                                                │     │
│            │  │ 🟡 WARNING   Queue depth at 85% (threshold 80%)│     │
│            │  │              Triggered 15 minutes ago           │     │
│            │  │                                                │     │
│            │  │ 🟡 WARNING   Worker stale: worker-03           │     │
│            │  │              Triggered 22 minutes ago           │     │
│            │  │                                                │     │
│            │  └────────────────────────────────────────────────┘     │
│            │                                                         │
│            │  ┌─ Alert Rules ─────────────────────────────────┐     │
│            │  │                                                │     │
│            │  │ Condition            Current  Threshold  Level │     │
│            │  │ ──────────────────── ─────── ────────── ───── │     │
│            │  │ Queue depth %           85%       80%   WARN  │     │
│            │  │   [✏]                                   [ON]  │     │
│            │  │                                                │     │
│            │  │ Zero active workers       0         1   CRIT  │     │
│            │  │   [✏]                                   [ON]  │     │
│            │  │                                                │     │
│            │  │ API key pool min          4         3   WARN  │     │
│            │  │   [✏]                                   [ON]  │     │
│            │  │                                                │     │
│            │  │ DB pool capacity %       34%       80%  WARN  │     │
│            │  │   [✏]                                   [ON]  │     │
│            │  │                                                │     │
│            │  │ Credit anomaly (x avg)  1.2x      5.0x  WARN  │     │
│            │  │   [✏]                                   [ON]  │     │
│            │  │                                                │     │
│            │  │ Worker stale (min)        —        10   WARN  │     │
│            │  │   [✏]                                   [ON]  │     │
│            │  │                                                │     │
│            │  │ API key quota exhausted   0         1   CRIT  │     │
│            │  │   [✏]                                   [ON]  │     │
│            │  │                                                │     │
│            │  │ DB backup failure         0         1   WARN  │     │
│            │  │   [✏]                                   [ON]  │     │
│            │  │                                                │     │
│            │  └────────────────────────────────────────────────┘     │
│            │                                                         │
└────────────┴─────────────────────────────────────────────────────────┘
```

### State 3B: No Active Alerts

```
│            │  ┌─ Active Alerts ───────────────────────────────┐     │
│            │  │                                                │     │
│            │  │   ✓ No active alerts. All systems healthy.     │     │
│            │  │                                                │     │
│            │  └────────────────────────────────────────────────┘     │
```

### State 3C: Inline Threshold Edit

```
│            │  │ Condition            Current  Threshold  Level │     │
│            │  │ ──────────────────── ─────── ────────── ───── │     │
│            │  │ Queue depth %           85%   [  85  ]  WARN  │     │
│            │  │   [Save] [Cancel]         ▲ editable   [ON]  │     │
│            │  │                            input              │     │
│            │  │ Zero active workers       0         1   CRIT  │     │
│            │  │   [✏]                                   [ON]  │     │
```

### State 3D: Threshold Validation Error

```
│            │  │ Queue depth %           85%   [  150  ] WARN  │     │
│            │  │   [Save] [Cancel]                       [ON]  │     │
│            │  │   ⚠ Threshold must be between 1 and 100.      │     │
```

### State 3E: Alert Rule Disabled

```
│            │  │ Queue depth %           85%       80%   WARN  │     │
│            │  │   [✏]                                   [OFF] │     │
│            │  │   ▲ row appears dimmed/muted                   │     │
```

### State 3F: Loading

```
│            │  ┌─ Active Alerts ───────────────────────────────┐     │
│            │  │ ░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░ │     │
│            │  │ ░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░ │     │
│            │  └────────────────────────────────────────────────┘     │
│            │                                                         │
│            │  ┌─ Alert Rules ─────────────────────────────────┐     │
│            │  │ ░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░ │     │
│            │  │ ░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░ │     │
│            │  │ ░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░ │     │
│            │  └────────────────────────────────────────────────┘     │
```

---

## Screen 4: Maintenance Mode Modal

### State 4A: Enable Maintenance

```
┌──────────────────────────────────────────────────────────────────┐
│                                                              │
│  Enable Maintenance Mode                              ✕     │
│                                                              │
│  ┌────────────────────────────────────────────────────────┐  │
│  │ ⚠ Warning                                             │  │
│  │ Enabling maintenance mode will:                       │  │
│  │ • Block new scan creation for ALL tenants             │  │
│  │ • Prevent scheduled scans from triggering             │  │
│  │ • Show maintenance banner to all users                │  │
│  │ Running scans will complete normally.                 │  │
│  └────────────────────────────────────────────────────────┘  │
│                                                              │
│  Reason *                                                    │
│  ┌────────────────────────────────────────────────────────┐  │
│  │ Database migration for v2.4 release                   │  │
│  └────────────────────────────────────────────────────────┘  │
│                                                              │
│  Estimated Duration *                                        │
│  ┌──────┐                                                    │
│  │  30  │ minutes                                            │
│  └──────┘                                                    │
│                                                              │
│                      [Cancel]  [Enable Maintenance]          │
│                                                              │
└──────────────────────────────────────────────────────────────┘
```

### State 4B: Validation Error

```
│  Reason *                                                    │
│  ┌────────────────────────────────────────────────────────┐  │
│  │ short                                                 │  │
│  └────────────────────────────────────────────────────────┘  │
│  ⚠ Reason must be at least 10 characters.                    │
│                                                              │
│  Estimated Duration *                                        │
│  ┌──────┐                                                    │
│  │      │ minutes                                            │
│  └──────┘                                                    │
│  ⚠ Estimated duration is required.                           │
```

### State 4C: Submitting

```
│                      [Cancel]  [Enabling... ◌]               │
│                                 ▲ disabled, spinner          │
```

### State 4D: Disable Maintenance Confirmation

```
┌──────────────────────────────────────────────────────────────┐
│                                                              │
│  Disable Maintenance Mode                             ✕     │
│                                                              │
│  Disable maintenance mode and resume normal operations?      │
│                                                              │
│  • Scan creation will be re-enabled for all tenants          │
│  • Scheduled scans will resume on their next trigger         │
│  • Maintenance banner will be removed                        │
│                                                              │
│                   [Cancel]  [Disable Maintenance]            │
│                                                              │
└──────────────────────────────────────────────────────────────┘
```

---

## Screen 5: Impersonation Indicator

### State 5A: Collapsed (Floating Pill)

```
                                              ┌──────────────────────────┐
                                              │ 👤 Acme Corp   ⏱ 42:15  │
                                              └──────────────────────────┘
                                                ▲ fixed bottom-right corner
                                                  semi-transparent background
                                                  click to expand
```

### State 5B: Expanded (Detail Panel)

```
                                    ┌──────────────────────────────────────┐
                                    │ Impersonating                        │
                                    │                                      │
                                    │ Tenant: Acme Corp                    │
                                    │ ID: tnt_abc123                       │
                                    │ Time remaining: 42:15                │
                                    │ Reason: "Investigating scan failure" │
                                    │                                      │
                                    │ ⚠ Restricted during impersonation:  │
                                    │ • Password changes                   │
                                    │ • 2FA settings                       │
                                    │ • Tenant deletion                    │
                                    │ • Billing/payment changes            │
                                    │                                      │
                                    │ [End Session]                        │
                                    └──────────────────────────────────────┘
                                      ▲ anchored to bottom-right
                                        click outside to collapse
```

### State 5C: Timer Warning (≤10 min)

```
                                              ┌──────────────────────────┐
                                              │ 👤 Acme Corp   ⏱ 09:45  │
                                              │ ▲ yellow background      │
                                              └──────────────────────────┘
```

### State 5D: Timer Critical (≤2 min)

```
                                              ┌──────────────────────────┐
                                              │ 👤 Acme Corp   ⏱ 01:30  │
                                              │ ▲ red background + pulse │
                                              └──────────────────────────┘
```

### State 5E: End Session Confirmation

```
                                    ┌──────────────────────────────────────┐
                                    │ End Impersonation Session?           │
                                    │                                      │
                                    │ End impersonation session for        │
                                    │ Acme Corp? You will be returned      │
                                    │ to the admin panel.                  │
                                    │                                      │
                                    │           [Cancel]  [End Session]    │
                                    └──────────────────────────────────────┘
```

### State 5F: Session Expired

```
                                              ┌──────────────────────────┐
                                              │ ⏱ Session Expired        │
                                              │ ▲ fades out after 3s     │
                                              └──────────────────────────┘
                                              then: redirect to admin panel
```

---

## Screen 6: Admin Header (Persistent)

### State 6A: Normal

```
┌──────────────────────────────────────────────────────────────────────┐
│ Reconova Admin              🔔 ·  ⚙ Maintenance: Off           👤  │
│                              ▲     ▲                             ▲   │
│                         no alerts  toggle button            profile  │
└──────────────────────────────────────────────────────────────────────┘
```

### State 6B: With Active Alerts

```
┌──────────────────────────────────────────────────────────────────────┐
│ Reconova Admin              🔔 3  ⚙ Maintenance: Off           👤  │
│                              ▲ red badge with count                  │
└──────────────────────────────────────────────────────────────────────┘
```

### State 6C: Maintenance Active

```
┌──────────────────────────────────────────────────────────────────────┐
│ Reconova Admin              🔔 3  🟠 Maintenance: ON (43m)     👤  │
│                                    ▲ orange, shows remaining time    │
└──────────────────────────────────────────────────────────────────────┘
```

---

## Conditional Rendering Rules

| Condition | Effect |
|-----------|--------|
| `role !== SUPER_ADMIN` | Redirect to `/dashboard`. No admin screens rendered. |
| `!has_2fa_enrolled` (first login) | Redirect to `/auth/2fa/setup`. Block all admin access. |
| `maintenance_mode === ACTIVE` | Orange badge in header. Maintenance banner on dashboard. |
| `active_alerts_count > 0` | Red notification badge in header with count. |
| `impersonation_session active` | Floating indicator on ALL pages (not just admin). |
| `impersonation_timer ≤ 10min` | Yellow indicator background. |
| `impersonation_timer ≤ 2min` | Red indicator background + pulse animation. |
| `metric > alert_threshold` | Card border color matches alert level (yellow/red). |
| `metric section loading` | Skeleton placeholder for that section only. |
| `metric section error` | Stale data warning with [Retry] for that section. |
| `browser_notification_permission === 'default'` | Show notification prompt on first monitoring visit. |
