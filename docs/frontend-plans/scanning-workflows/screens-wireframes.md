# Screens & Wireframes (Scanning & Workflows)

Scope: ASCII wireframes for all scanning screens, covering every state (default, loading, error, empty) with conditional rendering rules.

---

## Route Structure

| Route | Access | Description |
|-------|--------|-------------|
| `/domains` | All members | Domain list |
| `/domains/{id}` | All members | Domain details (overview + scan history) |
| `/scans` | All members | Scan jobs list |
| `/scans/new` | Tenant Owner | Create new scan |
| `/scans/{id}` | All members | Scan details + results |
| `/scans/workflows` | All members | Workflow templates + custom |
| `/scans/workflows/new` | Tenant Owner (Pro+) | Create custom workflow |
| `/scans/workflows/{id}` | All members | Workflow details |
| `/scans/workflows/{id}/edit` | Tenant Owner (Pro+) | Edit custom workflow |
| `/scans/schedules` | Tenant Owner (Pro+) | Scan schedules list |
| `/scans/schedules/new` | Tenant Owner (Pro+) | Create schedule |
| `/admin/scans/limits` | Super Admin | Concurrent scan limit config |

---

## Screen 1: Domain List (`/domains`)

### State: Default (Has Domains)

```
┌──────────────────────────────────────────────────────────────────────┐
│  Domains                                        3 / 20 used         │
├──────────────────────────────────────────────────────────────────────┤
│                                                                      │
│  [Add Domain]                                                        │
│                                                                      │
│  ┌─── Domain List ──────────────────────────────────────────────┐   │
│  │                                                               │   │
│  │  Domain            Status    Added         Last Scanned       │   │
│  │  ─────────────────────────────────────────────────────────    │   │
│  │  example.com       ACTIVE    Mar 1, 2026   Mar 9, 14:23      │   │
│  │                                    [View] [Start Scan] [Delete]│   │
│  │  mysite.io         ACTIVE    Mar 3, 2026   Mar 8, 09:00      │   │
│  │                                    [View] [Start Scan] [Delete]│   │
│  │  testapp.dev       ACTIVE    Mar 5, 2026   Never              │   │
│  │                                    [View] [Start Scan] [Delete]│   │
│  │                                                               │   │
│  └───────────────────────────────────────────────────────────────┘   │
│                                                                      │
└──────────────────────────────────────────────────────────────────────┘
```

### State: Empty (No Domains)

```
┌──────────────────────────────────────────────────────────────────────┐
│  Domains                                        0 / 20 used         │
├──────────────────────────────────────────────────────────────────────┤
│                                                                      │
│  ┌───────────────────────────────────────────────────────────────┐   │
│  │                                                               │   │
│  │         No domains added yet.                                 │   │
│  │         Add your first domain to start scanning.              │   │
│  │                                                               │   │
│  │                    [Add Domain]                                │   │
│  │                                                               │   │
│  └───────────────────────────────────────────────────────────────┘   │
│                                                                      │
└──────────────────────────────────────────────────────────────────────┘
```

### State: Free Tier

```
┌──────────────────────────────────────────────────────────────────────┐
│  Domains                                                             │
├──────────────────────────────────────────────────────────────────────┤
│                                                                      │
│  ┌─── ℹ Upgrade Required ──────────────────────────────────────┐   │
│  │  Upgrade to add domains and start scanning.     [View Plans]  │   │
│  └───────────────────────────────────────────────────────────────┘   │
│                                                                      │
│  [Add Domain] (disabled)                                             │
│                                                                      │
└──────────────────────────────────────────────────────────────────────┘
```

### State: At Domain Limit

```
│  [Add Domain] (disabled, tooltip: "Domain limit reached (20/20). Upgrade for more.")
```

### Add Domain Modal

```
┌──────────────────────────────────────────────────────────────────────┐
│                                                                      │
│  Add Domain                                                  [✕]    │
│  ────────────────────────────────────────────────────────────────    │
│                                                                      │
│  Domain: [                                        ]                  │
│          Enter a bare domain (e.g., example.com)                     │
│                                                                      │
│  Domains used: 3 / 20                                                │
│                                                                      │
│                              [Cancel]    [Add Domain]                │
│                                                                      │
└──────────────────────────────────────────────────────────────────────┘
```

### Add Domain Modal — Validation Error

```
│  Domain: [https://example.com/path                ]                  │
│          ⚠ Enter domain without protocol or path                     │
```

```
│  Domain: [api.example.com                         ]                  │
│          ⚠ Enter root domain only (e.g., example.com)                │
```

### Delete Domain Confirmation Modal

```
┌──────────────────────────────────────────────────────────────────────┐
│                                                                      │
│  Delete Domain                                               [✕]    │
│  ────────────────────────────────────────────────────────────────    │
│                                                                      │
│  Delete example.com?                                                 │
│                                                                      │
│  This will permanently remove:                                       │
│  • All subdomains, ports, technologies, screenshots                  │
│  • All scan history for this domain                                  │
│  • Associated scan schedules will be disabled                        │
│                                                                      │
│  Active scans must be cancelled first.                               │
│                                                                      │
│                         [Cancel]    [Delete Domain]                  │
│                                      (destructive/red)              │
│                                                                      │
└──────────────────────────────────────────────────────────────────────┘
```

### Conditional Rendering

| Condition | Behavior |
|-----------|----------|
| Free tier | Upgrade banner, [Add Domain] disabled |
| At domain limit | [Add Domain] disabled with tooltip |
| `role != TENANT_OWNER` | Hide [Add Domain], [Delete], [Start Scan] |
| Domain has never been scanned | "Never" in Last Scanned column |

---

## Screen 2: Domain Details (`/domains/{id}`)

### State: Default (Overview Tab, Has Data)

```
┌──────────────────────────────────────────────────────────────────────┐
│  Domains > example.com                                               │
├──────────────────────────────────────────────────────────────────────┤
│                                                                      │
│  example.com                              [Start Scan] [Delete]     │
│  Status: ACTIVE · Added Mar 1, 2026 by user@tenant.com              │
│                                                                      │
│  [● Overview]  [○ Scan History]                                      │
│  ────────────────────────────────────────────────────────────────    │
│                                                                      │
│  ┌─── Subdomains (42) ─────────────────────────────────────────┐   │
│  │                                                               │   │
│  │  Subdomain               Source          First Seen  Last Seen│   │
│  │  ─────────────────────────────────────────────────────────    │   │
│  │  api.example.com         subfinder       Mar 1       Mar 9   │   │
│  │  www.example.com         subfinder       Mar 1       Mar 9   │   │
│  │  mail.example.com        amass           Mar 1       Mar 9   │   │
│  │  staging.example.com     securitytrails  Mar 3       Mar 8   │   │
│  │  ...                                                          │   │
│  │  Showing 1–10 of 42            [◀ Prev]  1  2  3  [Next ▶]   │   │
│  └───────────────────────────────────────────────────────────────┘   │
│                                                                      │
│  ┌─── Open Ports (156) ────────────────────────────────────────┐   │
│  │                                                               │   │
│  │  Subdomain           Port    Protocol   Service              │   │
│  │  ─────────────────────────────────────────────────────────    │   │
│  │  api.example.com     443     tcp        nginx/1.21            │   │
│  │  api.example.com     80      tcp        nginx/1.21            │   │
│  │  www.example.com     443     tcp        cloudflare             │   │
│  │  ...                                                          │   │
│  │  Showing 1–10 of 156           [◀ Prev]  1  2  3  [Next ▶]   │   │
│  └───────────────────────────────────────────────────────────────┘   │
│                                                                      │
│  ┌─── Technologies (28) ───────────────────────────────────────┐   │
│  │                                                               │   │
│  │  Subdomain           Technology    Version    Category        │   │
│  │  ─────────────────────────────────────────────────────────    │   │
│  │  api.example.com     nginx         1.21       web-server      │   │
│  │  api.example.com     Node.js       18.x       runtime         │   │
│  │  www.example.com     React         18.2       framework       │   │
│  │  ...                                                          │   │
│  └───────────────────────────────────────────────────────────────┘   │
│                                                                      │
└──────────────────────────────────────────────────────────────────────┘
```

### State: Scan History Tab

```
│  [○ Overview]  [● Scan History]                                      │
│  ────────────────────────────────────────────────────────────────    │
│                                                                      │
│  ┌─── Scan History ────────────────────────────────────────────┐   │
│  │                                                               │   │
│  │  Scan       Workflow       Status      Started     Duration   │   │
│  │  ─────────────────────────────────────────────────────────    │   │
│  │  #4821      Full Scan      ● COMPLETED Mar 9 14:23  12m      │   │
│  │  #4819      Quick Recon    ◐ PARTIAL   Mar 8 09:00  8m       │   │
│  │  #4802      Full Scan      ● COMPLETED Mar 5 10:15  15m      │   │
│  │  #4791      Quick Recon    ✗ FAILED    Mar 3 08:00  2m       │   │
│  │                                                               │   │
│  └───────────────────────────────────────────────────────────────┘   │
```

### State: Empty (No Scan Data Yet)

```
│  [● Overview]  [○ Scan History]                                      │
│  ────────────────────────────────────────────────────────────────    │
│                                                                      │
│  ┌───────────────────────────────────────────────────────────────┐   │
│  │                                                               │   │
│  │         No scan data yet.                                     │   │
│  │         Run your first scan to discover assets.               │   │
│  │                                                               │   │
│  │                    [Start Scan]                                │   │
│  │                                                               │   │
│  └───────────────────────────────────────────────────────────────┘   │
```

### State: Loading

```
│  ┌─── Subdomains ──────────────────────────────────────────────┐   │
│  │  ░░░░░░░░░░░░░░░░  ░░░░░░░░░  ░░░░░░░░  ░░░░░░░░          │   │
│  │  ░░░░░░░░░░░░░░░░  ░░░░░░░░░  ░░░░░░░░  ░░░░░░░░          │   │
│  │  ░░░░░░░░░░░░░░░░  ░░░░░░░░░  ░░░░░░░░  ░░░░░░░░          │   │
│  └───────────────────────────────────────────────────────────────┘   │
```

---

## Screen 3: Scan Jobs List (`/scans`)

### State: Default (Has Scans)

```
┌──────────────────────────────────────────────────────────────────────┐
│  Scans                                                               │
├──────────────────────────────────────────────────────────────────────┤
│                                                                      │
│  [New Scan]                                                          │
│                                                                      │
│  ┌─── Filters ──────────────────────────────────────────────────┐   │
│  │  Status: [All ▼]   Domain: [All ▼]   Date: [All Time ▼]     │   │
│  └───────────────────────────────────────────────────────────────┘   │
│                                                                      │
│  ┌─── Scan Jobs ────────────────────────────────────────────────┐   │
│  │                                                               │   │
│  │  Domain         Workflow     Status       Started   Dur  Cred │   │
│  │  ─────────────────────────────────────────────────────────    │   │
│  │  example.com    Full Scan    ◉ RUNNING    14:23     2m   8   │   │
│  │                              Step 2/5: port_scan    [Cancel] │   │
│  │                                                               │   │
│  │  mysite.io      Quick Recon  ● COMPLETED  09:15     5m   4   │   │
│  │  example.com    Web App      ◐ PARTIAL    08:00     12m  6   │   │
│  │  testapp.dev    Full Scan    ✗ FAILED     Mar 8     1m   8   │   │
│  │  example.com    Quick Recon  ○ CANCELLED  Mar 7     3m   4   │   │
│  │                                                               │   │
│  │  Showing 1–5 of 23              [◀ Prev]  1  2  3  [Next ▶]  │   │
│  │                                                               │   │
│  └───────────────────────────────────────────────────────────────┘   │
│                                                                      │
└──────────────────────────────────────────────────────────────────────┘
```

### Status Filter Dropdown

```
┌──────────────┐
│ ● All        │
│ ○ Queued     │
│ ○ Running    │
│ ○ Completed  │
│ ○ Partial    │
│ ○ Failed     │
│ ○ Cancelled  │
└──────────────┘
```

### State: Empty

```
│  ┌───────────────────────────────────────────────────────────────┐   │
│  │                                                               │   │
│  │         No scans yet.                                         │   │
│  │         Start your first scan to discover assets.             │   │
│  │                                                               │   │
│  │                    [New Scan]                                  │   │
│  │                                                               │   │
│  └───────────────────────────────────────────────────────────────┘   │
```

### State: Loading

```
│  ┌─── Scan Jobs ────────────────────────────────────────────────┐   │
│  │  ░░░░░░░░░░░  ░░░░░░░░░  ░░░░░░░░  ░░░░░  ░░░  ░░          │   │
│  │  ░░░░░░░░░░░  ░░░░░░░░░  ░░░░░░░░  ░░░░░  ░░░  ░░          │   │
│  │  ░░░░░░░░░░░  ░░░░░░░░░  ░░░░░░░░  ░░░░░  ░░░  ░░          │   │
│  └───────────────────────────────────────────────────────────────┘   │
```

---

## Screen 4: New Scan (`/scans/new`)

### State: Default

```
┌──────────────────────────────────────────────────────────────────────┐
│  Scans > New Scan                                                    │
├──────────────────────────────────────────────────────────────────────┤
│                                                                      │
│  ┌─── 1. Select Domain ────────────────────────────────────────┐   │
│  │                                                               │   │
│  │  Domain: [Select domain ▼]                                    │   │
│  │                                                               │   │
│  │  ┌─────────────────────┐                                      │   │
│  │  │ example.com         │                                      │   │
│  │  │ mysite.io           │                                      │   │
│  │  │ testapp.dev         │                                      │   │
│  │  └─────────────────────┘                                      │   │
│  │                                                               │   │
│  └───────────────────────────────────────────────────────────────┘   │
│                                                                      │
│  ┌─── 2. Select Workflow ──────────────────────────────────────┐   │
│  │                                                               │   │
│  │  System Templates:                                            │   │
│  │   ○ Quick Recon        3 steps · Fast attack surface overview │   │
│  │   ● Full Scan          5 steps · Comprehensive assessment     │   │
│  │   ○ Web App Scan       4 steps · Web application focus        │   │
│  │   ○ Compliance Check   4 steps · SOC/NIST readiness           │   │
│  │   ○ Continuous Monitor 2 steps · Lightweight monitoring       │   │
│  │                                                               │   │
│  │  Custom Workflows:                                            │   │
│  │   ○ My Recon Flow      3 steps                                │   │
│  │                                                               │   │
│  └───────────────────────────────────────────────────────────────┘   │
│                                                                      │
│  ┌─── 3. Review ───────────────────────────────────────────────┐   │
│  │                                                               │   │
│  │  Domain:   example.com                                        │   │
│  │  Workflow: Full Scan                                          │   │
│  │                                                               │   │
│  │  Steps                          Credits                       │   │
│  │  ──────────────────────────────────────                       │   │
│  │  1. subdomain_enum              1                             │   │
│  │  2. port_scan                   2                             │   │
│  │  3. tech_detect                 1                             │   │
│  │  4. screenshot                  1                             │   │
│  │  5. vuln_scan                   3                             │   │
│  │  ──────────────────────────────────────                       │   │
│  │  Total:                         8 credits                     │   │
│  │  Available:                     390 credits                   │   │
│  │                                                               │   │
│  └───────────────────────────────────────────────────────────────┘   │
│                                                                      │
│                           [Cancel]    [Start Scan — 8 credits]      │
│                                                                      │
└──────────────────────────────────────────────────────────────────────┘
```

### State: Steps Filtered by Feature Flags

```
│  ┌─── 3. Review ───────────────────────────────────────────────┐   │
│  │                                                               │   │
│  │  Domain:   example.com                                        │   │
│  │  Workflow: Full Scan                                          │   │
│  │                                                               │   │
│  │  Steps                          Credits                       │   │
│  │  ──────────────────────────────────────                       │   │
│  │  1. subdomain_enum              2                             │   │
│  │  2. port_scan                   3                             │   │
│  │  3. tech_detect                 2                             │   │
│  │  4. screenshot                  1                             │   │
│  │  ✗ vuln_scan                    — (not on your plan)          │   │
│  │  ──────────────────────────────────────                       │   │
│  │  Total:                         8 credits                     │   │
│  │                                                               │   │
│  │  ┌────────────────────────────────────────────────────────┐   │   │
│  │  │  ℹ 1 step unavailable on Starter:                      │   │   │
│  │  │    vuln_scan                          [Upgrade to Pro]  │   │   │
│  │  └────────────────────────────────────────────────────────┘   │   │
│  │                                                               │   │
│  └───────────────────────────────────────────────────────────────┘   │
```

### State: Insufficient Credits

```
│  │  Total:                         18 credits                    │   │
│  │  Available:                     12 credits                    │   │
│  │  Shortfall:                     6 credits                     │   │
│  │                                                               │   │
│  │  ┌────────────────────────────────────────────────────────┐   │   │
│  │  │  ⚠ Insufficient credits for this scan.                 │   │   │
│  │  │              [Purchase Credits]                         │   │   │
│  │  └────────────────────────────────────────────────────────┘   │   │
│  │                                                               │   │
│  └───────────────────────────────────────────────────────────────┘   │
│                                                                      │
│                           [Cancel]    [Start Scan] (disabled)       │
```

### State: No Domains

```
│  ┌─── 1. Select Domain ────────────────────────────────────────┐   │
│  │                                                               │   │
│  │  No domains added yet. Add a domain first.                    │   │
│  │                         [Add Domain]                          │   │
│  │                                                               │   │
│  └───────────────────────────────────────────────────────────────┘   │
```

### Conditional Rendering

| Condition | Behavior |
|-----------|----------|
| No domains | Show "Add domain first" message in step 1 |
| Domain pre-selected via query param | Domain picker pre-filled |
| Steps filtered by feature flags | Show filtered steps with strikethrough + upgrade link |
| Insufficient credits | Show shortfall warning, disable [Start Scan], show [Purchase Credits] |
| Domain has active scan | Show warning after domain selection: "Active scan exists" |
| `role != TENANT_OWNER` | Page not accessible, redirect to scan list |

---

## Screen 5: Scan Details (`/scans/{id}`)

### State: Running (With Step Progress Pipeline)

```
┌──────────────────────────────────────────────────────────────────────┐
│  Scans > #4821                                                       │
├──────────────────────────────────────────────────────────────────────┤
│                                                                      │
│  example.com · Full Scan                          Status: ◉ RUNNING │
│  Started: Mar 9, 14:23 · Duration: 2m 15s                           │
│  Credits: 8                                          [Cancel Scan]   │
│                                                                      │
│  ┌─── Step Progress ───────────────────────────────────────────┐   │
│  │                                                               │   │
│  │  ┌────────┐  ┌────────┐  ┌────────┐  ┌────────┐  ┌────────┐ │   │
│  │  │subdmn  │─►│port    │─►│tech    │─►│screen  │─►│vuln    │ │   │
│  │  │ ✓ Done │  │◉ Run.. │  │○ Pend  │  │○ Pend  │  │○ Pend  │ │   │
│  │  │  1:23  │  │  0:52  │  │        │  │        │  │        │ │   │
│  │  └────────┘  └────────┘  └────────┘  └────────┘  └────────┘ │   │
│  │                                                               │   │
│  │  Current: port_scan (step 2 of 5) · Running for 52s          │   │
│  │                                                               │   │
│  └───────────────────────────────────────────────────────────────┘   │
│                                                                      │
│  Results available after scan completes.                             │
│                                                                      │
└──────────────────────────────────────────────────────────────────────┘
```

### State: Running — Step Retrying

```
│  │  ┌────────┐  ┌────────┐  ┌────────┐  ┌────────┐  ┌────────┐ │   │
│  │  │subdmn  │─►│port    │─►│tech    │─►│screen  │─►│vuln    │ │   │
│  │  │ ✓ Done │  │⟳ Retry │  │○ Pend  │  │○ Pend  │  │○ Pend  │ │   │
│  │  │  1:23  │  │ 2 of 3 │  │        │  │        │  │        │ │   │
│  │  └────────┘  └────────┘  └────────┘  └────────┘  └────────┘ │   │
│  │                                                               │   │
│  │  Current: port_scan (step 2 of 5) · Retrying (attempt 2/3)   │   │
```

### State: Completed — All Steps Succeeded

```
┌──────────────────────────────────────────────────────────────────────┐
│  Scans > #4821                                                       │
├──────────────────────────────────────────────────────────────────────┤
│                                                                      │
│  example.com · Full Scan                       Status: ● COMPLETED  │
│  Started: Mar 9, 14:23 · Duration: 12m 34s                          │
│  Credits: 8                                                          │
│                                                                      │
│  ┌─── Step Progress ───────────────────────────────────────────┐   │
│  │                                                               │   │
│  │  ┌────────┐  ┌────────┐  ┌────────┐  ┌────────┐  ┌────────┐ │   │
│  │  │subdmn  │─►│port    │─►│tech    │─►│screen  │─►│vuln    │ │   │
│  │  │ ✓ Done │  │ ✓ Done │  │ ✓ Done │  │ ✓ Done │  │ ✓ Done │ │   │
│  │  │  1:23  │  │  3:45  │  │  2:10  │  │  1:56  │  │  3:20  │ │   │
│  │  └────────┘  └────────┘  └────────┘  └────────┘  └────────┘ │   │
│  │                                                               │   │
│  └───────────────────────────────────────────────────────────────┘   │
│                                                                      │
│  ┌─── Results ─────────────────────────────────────────────────┐   │
│  │                                                               │   │
│  │  [● Subdomains (42)] [○ Ports (156)] [○ Technologies (28)]   │   │
│  │  [○ Vulnerabilities (7)] [○ Screenshots (12)]                 │   │
│  │  ────────────────────────────────────────────────────────     │   │
│  │                                                               │   │
│  │  ┌─── Subdomains ──────────────────────────────────────┐     │   │
│  │  │                                                       │     │   │
│  │  │  Subdomain              Source        First    Last    │     │   │
│  │  │  ─────────────────────────────────────────────────    │     │   │
│  │  │  api.example.com        subfinder     Mar 1    Mar 9  │     │   │
│  │  │  www.example.com        subfinder     Mar 1    Mar 9  │     │   │
│  │  │  mail.example.com       amass         Mar 1    Mar 9  │     │   │
│  │  │  admin.example.com      subfinder     Mar 9    Mar 9  │     │   │
│  │  │  ...                                                   │     │   │
│  │  │  Showing 1–10 of 42         [◀ Prev] 1 2 3 [Next ▶]   │     │   │
│  │  └───────────────────────────────────────────────────────┘     │   │
│  │                                                               │   │
│  └───────────────────────────────────────────────────────────────┘   │
│                                                                      │
└──────────────────────────────────────────────────────────────────────┘
```

### State: Partial — Some Steps Failed

```
│  ┌─── Step Progress ───────────────────────────────────────────┐   │
│  │                                                               │   │
│  │  ┌────────┐  ┌────────┐  ┌────────┐  ┌────────┐  ┌────────┐ │   │
│  │  │subdmn  │─►│port    │─►│tech    │─►│screen  │─►│vuln    │ │   │
│  │  │ ✓ Done │  │ ✗ Fail │  │ ✓ Done │  │ ✓ Done │  │ ✓ Done │ │   │
│  │  │  1:23  │  │  2:10  │  │  1:45  │  │  1:56  │  │  3:20  │ │   │
│  │  └────────┘  └────────┘  └────────┘  └────────┘  └────────┘ │   │
│  │                                                               │   │
│  └───────────────────────────────────────────────────────────────┘   │
│                                                                      │
│  ┌─── ⚠ Partial Results ──────────────────────────────────────┐   │
│  │  1 step failed: port_scan (after 3 attempts).                 │   │
│  │  Credits for failed step were refunded.                       │   │
│  └───────────────────────────────────────────────────────────────┘   │
│                                                                      │
│  Results tabs: (no Ports tab since port_scan failed)                 │
│  [● Subdomains (42)] [○ Technologies (28)] [○ Vulns (7)] [○ Screenshots (12)]
```

### State: Failed — Root Step Failed

```
│  ┌─── Step Progress ───────────────────────────────────────────┐   │
│  │                                                               │   │
│  │  ┌────────┐  ┌────────┐  ┌────────┐  ┌────────┐  ┌────────┐ │   │
│  │  │subdmn  │─►│port    │─►│tech    │─►│screen  │─►│vuln    │ │   │
│  │  │ ✗ Fail │  │— Skip  │  │— Skip  │  │— Skip  │  │— Skip  │ │   │
│  │  │  0:45  │  │        │  │        │  │        │  │        │ │   │
│  │  └────────┘  └────────┘  └────────┘  └────────┘  └────────┘ │   │
│  │                                                               │   │
│  └───────────────────────────────────────────────────────────────┘   │
│                                                                      │
│  ┌─── ✗ Scan Failed ──────────────────────────────────────────┐   │
│  │  Root step (subdomain_enum) failed. All subsequent steps      │   │
│  │  were skipped. Credits refunded.                              │   │
│  └───────────────────────────────────────────────────────────────┘   │
│                                                                      │
│  No results available.                                               │
```

### State: Cancelled

```
│  ┌─── Step Progress ───────────────────────────────────────────┐   │
│  │                                                               │   │
│  │  ┌────────┐  ┌────────┐  ┌────────┐  ┌────────┐  ┌────────┐ │   │
│  │  │subdmn  │─►│port    │─►│tech    │─►│screen  │─►│vuln    │ │   │
│  │  │ ✓ Done │  │ ✓ Done │  │✕ Cncld │  │✕ Cncld │  │✕ Cncld │ │   │
│  │  │  1:23  │  │  3:45  │  │        │  │        │  │        │ │   │
│  │  └────────┘  └────────┘  └────────┘  └────────┘  └────────┘ │   │
│  │                                                               │   │
│  └───────────────────────────────────────────────────────────────┘   │
│                                                                      │
│  ┌─── Scan Cancelled ─────────────────────────────────────────┐   │
│  │  Cancelled by user. 3 remaining step credits refunded.        │   │
│  │  Completed results are preserved below.                       │   │
│  └───────────────────────────────────────────────────────────────┘   │
│                                                                      │
│  Results tabs for completed steps only:                              │
│  [● Subdomains (42)] [○ Ports (156)]                                 │
```

### State: Queued

```
│  example.com · Full Scan                          Status: ○ QUEUED  │
│  Queued: Mar 9, 14:23 · Waiting for worker                          │
│  Credits: 8                                          [Cancel Scan]   │
│                                                                      │
│  ┌─── Step Progress ───────────────────────────────────────────┐   │
│  │                                                               │   │
│  │  ┌────────┐  ┌────────┐  ┌────────┐  ┌────────┐  ┌────────┐ │   │
│  │  │subdmn  │─►│port    │─►│tech    │─►│screen  │─►│vuln    │ │   │
│  │  │○ Pend  │  │○ Pend  │  │○ Pend  │  │○ Pend  │  │○ Pend  │ │   │
│  │  └────────┘  └────────┘  └────────┘  └────────┘  └────────┘ │   │
│  │                                                               │   │
│  │  Waiting for worker to pick up...                             │   │
│  └───────────────────────────────────────────────────────────────┘   │
```

### Vulnerabilities Tab Detail

```
│  │  [○ Subdomains] [○ Ports] [○ Technologies] [● Vulnerabilities (7)] [○ Screenshots]
│  │  ────────────────────────────────────────────────────────     │   │
│  │                                                               │   │
│  │  Filter: [All Severities ▼]                                   │   │
│  │                                                               │   │
│  │  ┌─── CRITICAL ────────────────────────────────────────┐     │   │
│  │  │  CVE-2024-1234 · api.example.com                     │     │   │
│  │  │  Remote code execution in OpenSSL 3.0.x              │     │   │
│  │  │  Remediation: Upgrade to OpenSSL 3.0.12+             │     │   │
│  │  └──────────────────────────────────────────────────────┘     │   │
│  │                                                               │   │
│  │  ┌─── HIGH ────────────────────────────────────────────┐     │   │
│  │  │  CVE-2024-5678 · www.example.com                     │     │   │
│  │  │  SQL injection in login endpoint                     │     │   │
│  │  │  Remediation: Use parameterized queries              │     │   │
│  │  └──────────────────────────────────────────────────────┘     │   │
│  │  ...                                                          │   │
```

### Screenshots Tab Detail

```
│  │  [○ Subdomains] [○ Ports] [○ Technologies] [○ Vulns] [● Screenshots (12)]
│  │  ────────────────────────────────────────────────────────     │   │
│  │                                                               │   │
│  │  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐          │   │
│  │  │             │  │             │  │             │          │   │
│  │  │  [preview]  │  │  [preview]  │  │  [preview]  │          │   │
│  │  │             │  │             │  │             │          │   │
│  │  │ api.example │  │ www.example │  │ mail.exampl │          │   │
│  │  │    .com     │  │    .com     │  │   e.com     │          │   │
│  │  └─────────────┘  └─────────────┘  └─────────────┘          │   │
│  │                                                               │   │
│  │  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐          │   │
│  │  │             │  │             │  │             │          │   │
│  │  │  [preview]  │  │  [preview]  │  │  [preview]  │          │   │
│  │  │             │  │             │  │             │          │   │
│  │  └─────────────┘  └─────────────┘  └─────────────┘          │   │
│  │                                                               │   │
│  │  Click thumbnail to view full size                            │   │
```

### Cancel Scan Confirmation Modal

```
┌──────────────────────────────────────────────────────────────────────┐
│                                                                      │
│  Cancel Scan                                                 [✕]    │
│  ────────────────────────────────────────────────────────────────    │
│                                                                      │
│  Cancel scan #4821 on example.com?                                   │
│                                                                      │
│  • The current step (port_scan) will complete                        │
│  • Remaining 3 steps will be cancelled                               │
│  • Credits for unexecuted steps will be refunded                     │
│  • Completed results will be preserved                               │
│                                                                      │
│                     [Keep Scanning]    [Cancel Scan]                 │
│                                        (destructive/red)            │
│                                                                      │
└──────────────────────────────────────────────────────────────────────┘
```

---

## Screen 6: Workflow List (`/scans/workflows`)

### State: Default

```
┌──────────────────────────────────────────────────────────────────────┐
│  Scans > Workflows                                                   │
├──────────────────────────────────────────────────────────────────────┤
│                                                                      │
│  ┌─── System Templates ────────────────────────────────────────┐   │
│  │                                                               │   │
│  │  ┌─ Quick Recon ─────────────────────────────────────────┐   │   │
│  │  │  subdomain_enum → tech_detect → screenshot             │   │   │
│  │  │  Fast attack surface overview              [Use This]  │   │   │
│  │  └────────────────────────────────────────────────────────┘   │   │
│  │                                                               │   │
│  │  ┌─ Full Scan ───────────────────────────────────────────┐   │   │
│  │  │  subdomain_enum → port_scan → tech_detect →            │   │   │
│  │  │  screenshot → vuln_scan                                │   │   │
│  │  │  Comprehensive assessment                  [Use This]  │   │   │
│  │  └────────────────────────────────────────────────────────┘   │   │
│  │                                                               │   │
│  │  ┌─ Web App Scan ────────────────────────────────────────┐   │   │
│  │  │  subdomain_enum → tech_detect → vuln_scan → screenshot │   │   │
│  │  │  Web application focus                     [Use This]  │   │   │
│  │  └────────────────────────────────────────────────────────┘   │   │
│  │                                                               │   │
│  │  ┌─ Compliance Check ────────────────────────────────────┐   │   │
│  │  │  subdomain_enum → port_scan → vuln_scan →              │   │   │
│  │  │  compliance_check                                      │   │   │
│  │  │  SOC/NIST readiness                        [Use This]  │   │   │
│  │  └────────────────────────────────────────────────────────┘   │   │
│  │                                                               │   │
│  │  ┌─ Continuous Monitor ──────────────────────────────────┐   │   │
│  │  │  subdomain_enum → tech_detect                          │   │   │
│  │  │  Lightweight ongoing monitoring            [Use This]  │   │   │
│  │  └────────────────────────────────────────────────────────┘   │   │
│  │                                                               │   │
│  └───────────────────────────────────────────────────────────────┘   │
│                                                                      │
│  ┌─── Custom Workflows (3 / 20) ───────────────────────────────┐   │
│  │                                                               │   │
│  │  [Create Workflow]                                            │   │
│  │                                                               │   │
│  │  Name                  Steps   Created      Actions           │   │
│  │  ─────────────────────────────────────────────────────────    │   │
│  │  My Recon Flow         3       Mar 5, 2026  [Edit] [Delete]  │   │
│  │  API Security Scan     4       Mar 7, 2026  [Edit] [Delete]  │   │
│  │  Quick Port Check      2       Mar 8, 2026  [Edit] [Delete]  │   │
│  │                                                               │   │
│  └───────────────────────────────────────────────────────────────┘   │
│                                                                      │
└──────────────────────────────────────────────────────────────────────┘
```

### State: Starter Tier (Custom Workflows Locked)

```
│  ┌─── Custom Workflows ────────────────────────────────────────┐   │
│  │                                                               │   │
│  │  Custom workflows require Pro or Enterprise.    [Upgrade]     │   │
│  │                                                               │   │
│  └───────────────────────────────────────────────────────────────┘   │
```

---

## Screen 7: Custom Workflow Builder (`/scans/workflows/new`)

### State: Default

```
┌──────────────────────────────────────────────────────────────────────┐
│  Scans > Workflows > New Workflow                                    │
├──────────────────────────────────────────────────────────────────────┤
│                                                                      │
│  Name: [                                               ]             │
│                                                                      │
│  ┌─── Available Steps ─────────────────────────────────────────┐   │
│  │                                                               │   │
│  │  ☑ subdomain_enum        Subdomain enumeration               │   │
│  │  ☑ port_scan             Port scanning                       │   │
│  │  ☐ tech_detect           Technology detection                │   │
│  │  ☐ screenshot            Screenshot capture                  │   │
│  │  ☑ vuln_scan             Vulnerability scanning              │   │
│  │  ☐ compliance_check      Compliance checking                 │   │
│  │  ☐ shodan_lookup         Shodan enrichment                   │   │
│  │  ☐ securitytrails        SecurityTrails lookup                │   │
│  │  🔒 censys_lookup        Enterprise only                     │   │
│  │  🔒 custom_connector     Enterprise only                     │   │
│  │                                                               │   │
│  └───────────────────────────────────────────────────────────────┘   │
│                                                                      │
│  ┌─── Selected Steps (3 / 15 max) ─────────────────────────────┐   │
│  │                                                               │   │
│  │  ≡ 1. subdomain_enum                                [✕]     │   │
│  │  ≡ 2. port_scan                                     [✕]     │   │
│  │  ≡ 3. vuln_scan                                     [✕]     │   │
│  │                                                               │   │
│  │  Drag ≡ to reorder                                            │   │
│  │                                                               │   │
│  └───────────────────────────────────────────────────────────────┘   │
│                                                                      │
│                         [Cancel]    [Create Workflow]                │
│                                                                      │
└──────────────────────────────────────────────────────────────────────┘
```

---

## Screen 8: Scan Schedules List (`/scans/schedules`)

### State: Default (Pro+ Tier)

```
┌──────────────────────────────────────────────────────────────────────┐
│  Scans > Schedules                                4 / 10 active     │
├──────────────────────────────────────────────────────────────────────┤
│                                                                      │
│  [Create Schedule]                                                   │
│                                                                      │
│  ┌─── Schedules ────────────────────────────────────────────────┐   │
│  │                                                               │   │
│  │  Domain         Workflow      Schedule     Next Run   Status  │   │
│  │  ─────────────────────────────────────────────────────────    │   │
│  │  example.com    Full Scan     Daily        Mar 10     [●]    │   │
│  │                                00:00 UTC    00:00     Enabled │   │
│  │                                                               │   │
│  │  mysite.io      Quick Recon   Weekly Mon   Mar 16     [●]    │   │
│  │                                00:00 UTC    00:00     Enabled │   │
│  │                                                               │   │
│  │  example.com    Continuous    Daily        Mar 10     [○]    │   │
│  │                 Monitor       06:00 UTC    06:00      Disabled│   │
│  │                                                               │   │
│  │  testapp.dev    Full Scan     Monthly 1st  —          [○]    │   │
│  │                                            Disabled — Plan    │   │
│  │                                            downgraded         │   │
│  │                                                               │   │
│  │                                             [Delete] per row  │   │
│  └───────────────────────────────────────────────────────────────┘   │
│                                                                      │
└──────────────────────────────────────────────────────────────────────┘
```

### State: Starter/Free Tier (Locked)

```
┌──────────────────────────────────────────────────────────────────────┐
│  Scans > Schedules                                                   │
├──────────────────────────────────────────────────────────────────────┤
│                                                                      │
│  ┌───────────────────────────────────────────────────────────────┐   │
│  │                                                               │   │
│  │  Scheduled scans require Pro or Enterprise.      [Upgrade]    │   │
│  │                                                               │   │
│  └───────────────────────────────────────────────────────────────┘   │
│                                                                      │
└──────────────────────────────────────────────────────────────────────┘
```

### State: Empty (Pro+ Tier, No Schedules)

```
│  ┌───────────────────────────────────────────────────────────────┐   │
│  │                                                               │   │
│  │         No scan schedules yet.                                │   │
│  │         Automate recurring scans on your domains.             │   │
│  │                                                               │   │
│  │                    [Create Schedule]                           │   │
│  │                                                               │   │
│  └───────────────────────────────────────────────────────────────┘   │
```

---

## Screen 9: New Schedule (`/scans/schedules/new`)

### State: Default

```
┌──────────────────────────────────────────────────────────────────────┐
│  Scans > Schedules > New Schedule                                    │
├──────────────────────────────────────────────────────────────────────┤
│                                                                      │
│  ┌─── Domain ──────────────────────────────────────────────────┐   │
│  │  Domain: [Select domain ▼]                                    │   │
│  └───────────────────────────────────────────────────────────────┘   │
│                                                                      │
│  ┌─── Workflow ────────────────────────────────────────────────┐   │
│  │  Workflow: [Select workflow ▼]                                │   │
│  └───────────────────────────────────────────────────────────────┘   │
│                                                                      │
│  ┌─── Schedule ────────────────────────────────────────────────┐   │
│  │                                                               │   │
│  │  Frequency:                                                   │   │
│  │   ● Daily       at [00:00] UTC                                │   │
│  │   ○ Weekly      on [Monday ▼] at [00:00] UTC                  │   │
│  │   ○ Monthly     on day [1 ▼] at [00:00] UTC                   │   │
│  │   ○ Custom      cron: [          ]                            │   │
│  │                  Preview: "Every day at 00:00 UTC"            │   │
│  │                                                               │   │
│  └───────────────────────────────────────────────────────────────┘   │
│                                                                      │
│  ┌─── Credit Estimate ─────────────────────────────────────────┐   │
│  │                                                               │   │
│  │  Estimated cost per run: 8 credits                            │   │
│  │  ℹ Credits are checked at execution time, not reserved.       │   │
│  │                                                               │   │
│  └───────────────────────────────────────────────────────────────┘   │
│                                                                      │
│                         [Cancel]    [Create Schedule]                │
│                                                                      │
└──────────────────────────────────────────────────────────────────────┘
```

### State: Custom Cron — Validation Error

```
│  │   ○ Custom      cron: [*/5 * * * *]                           │   │
│  │                  ⚠ Minimum interval is 24 hours.              │   │
```

---

## Screen 10: Admin — Concurrent Scan Limits (`/admin/scans/limits`)

### State: Default

```
┌──────────────────────────────────────────────────────────────────────┐
│  Admin > Scans > Concurrent Limits                                   │
├──────────────────────────────────────────────────────────────────────┤
│                                                                      │
│  Override concurrent scan limits for Enterprise tenants.             │
│                                                                      │
│  ┌─── Default Limits ──────────────────────────────────────────┐   │
│  │  Starter: 1   Pro: 3   Enterprise: 10                        │   │
│  └───────────────────────────────────────────────────────────────┘   │
│                                                                      │
│  ┌─── Enterprise Tenant Overrides ─────────────────────────────┐   │
│  │                                                               │   │
│  │  Search: [                                          🔍]      │   │
│  │                                                               │   │
│  │  Tenant            Plan         Limit    Actions              │   │
│  │  ─────────────────────────────────────────────────────────    │   │
│  │  Acme Corp         Enterprise   10 (default)   [Set Custom]  │   │
│  │  MegaCo            Enterprise   25 (custom)    [Edit] [Reset]│   │
│  │  GlobalSec Inc     Enterprise   10 (default)   [Set Custom]  │   │
│  │                                                               │   │
│  └───────────────────────────────────────────────────────────────┘   │
│                                                                      │
└──────────────────────────────────────────────────────────────────────┘
```

### Edit Limit Modal

```
┌──────────────────────────────────────────────────────────────────────┐
│                                                                      │
│  Set Concurrent Scan Limit                                   [✕]    │
│  ────────────────────────────────────────────────────────────────    │
│                                                                      │
│  Tenant: MegaCo                                                      │
│                                                                      │
│  Concurrent scan limit: [25]                                         │
│  Min: 1  Max: 100                                                    │
│                                                                      │
│  Note: One-active-scan-per-domain is always enforced regardless      │
│  of this limit.                                                      │
│                                                                      │
│                              [Cancel]    [Save]                      │
│                                                                      │
└──────────────────────────────────────────────────────────────────────┘
```

### Conditional Rendering

| Condition | Behavior |
|-----------|----------|
| Only Enterprise tenants shown | Filter tenant list to Enterprise plan |
| Default limit | Show "(default)" label, [Set Custom] action |
| Custom limit | Show "(custom)" label, [Edit] and [Reset] actions |
| [Reset] clicked | Confirmation: "Reset to default (10)?" |
