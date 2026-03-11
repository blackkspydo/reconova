# Reference (Scanning & Workflows)

Scope: Error handling matrix, input validation rules, security considerations, and frontend-to-backend action mapping.

---

## Error Handling Matrix

### Domain Errors

| Error Code | HTTP | User Message | UI Action |
|-----------|------|-------------|-----------|
| `ERR_SCAN_001` | 400 | "Invalid domain format." | Inline error on domain field. Show hint: "Enter a valid domain like example.com" |
| `ERR_SCAN_002` | 400 | "Enter root domain only (e.g., example.com)." | Inline error on domain field. |
| `ERR_SCAN_003` | 409 | "This domain already exists." | Inline error on domain field. |
| `ERR_SCAN_004` | 403 | "Domain limit reached ({used}/{max})." | Inline error + [Upgrade Plan] link. Disable [Add Domain]. |
| `ERR_SCAN_005` | 404 | "Domain not found." | Toast (error). Redirect to domain list. |
| `ERR_SCAN_006` | 409 | "Cannot delete — active scans exist. Cancel them first." | Toast (error). Show link to active scan(s). |

### Scan Errors

| Error Code | HTTP | User Message | UI Action |
|-----------|------|-------------|-----------|
| `ERR_SCAN_013` | 409 | "A scan is already running on this domain." | Toast (error). Show link to active scan. |
| `ERR_SCAN_014` | 429 | "Concurrent scan limit reached. Wait for a scan to complete or cancel one." | Toast (error). Show link to scan list. |
| `ERR_SCAN_015` | 400 | "No scan steps available on your plan." | Toast (error). Show [Upgrade] link. |
| `ERR_SCAN_016` | 409 | "This scan can no longer be cancelled." | Toast (error). Refresh scan details (status may have changed). |
| `ERR_BILL_007` | 402 | "Insufficient credits." | Show `<InsufficientCreditsModal />` with required, available, shortfall. |

### Workflow Errors

| Error Code | HTTP | User Message | UI Action |
|-----------|------|-------------|-----------|
| `ERR_SCAN_007` | 403 | "Custom workflows require Pro or Enterprise." | Toast (error). Show [Upgrade] link. |
| `ERR_SCAN_008` | 400 | "Workflow must have at least one step." | Inline error in workflow builder. |
| `ERR_SCAN_009` | 400 | "Workflow cannot exceed 15 steps." | Inline error in workflow builder. |
| `ERR_SCAN_010` | 400 | "Unknown scan step type: {type}." | Inline error on the unknown step. |
| `ERR_SCAN_011` | 403 | "Custom workflow limit reached (max 20)." | Toast (error). No further action. |
| `ERR_SCAN_012` | 404 | "Workflow not found." | Toast (error). Redirect to workflow list. |

### Schedule Errors

| Error Code | HTTP | User Message | UI Action |
|-----------|------|-------------|-----------|
| `ERR_SCAN_017` | 403 | "Scheduled scans require Pro or Enterprise." | Toast (error). Show [Upgrade] link. |
| `ERR_SCAN_018` | 400 | "Invalid cron expression." | Inline error on cron input field. |
| `ERR_SCAN_019` | 400 | "Minimum schedule interval is 24 hours." | Inline error on frequency/cron field. |
| `ERR_SCAN_020` | 403 | "Schedule limit reached (max 10 active)." | Toast (error). Show current count. |

### Network & System Errors

| Scenario | User Message | UI Action |
|----------|-------------|-----------|
| Network timeout | "Unable to connect. Check your connection and try again." | Toast (error) + [Retry]. |
| 500 Internal Server Error | "Something went wrong. Please try again." | Toast (error) + [Retry]. |
| Scan details poll fails | Silent retry on next interval. After 3 consecutive failures: "Unable to load scan status." + [Retry]. | |
| Screenshot image fails to load | Show broken image placeholder with "Screenshot unavailable." | |

### Error Response Parsing

```typescript
function handleScanError(error: ApiError): void {
  const { code, message, details } = error.error;

  switch (code) {
    // Inline field errors
    case 'ERR_SCAN_001':
    case 'ERR_SCAN_002':
    case 'ERR_SCAN_003':
    case 'ERR_SCAN_008':
    case 'ERR_SCAN_009':
    case 'ERR_SCAN_010':
    case 'ERR_SCAN_018':
    case 'ERR_SCAN_019':
      setFieldError(code, message);
      break;

    // Insufficient credits — delegate to billing error handler
    case 'ERR_BILL_007':
      openInsufficientCreditsModal({
        required: details?.required as number,
        available: details?.available as number,
        shortfall: details?.shortfall as number,
      });
      break;

    // Feature gate errors — show upgrade link
    case 'ERR_SCAN_004':
    case 'ERR_SCAN_007':
    case 'ERR_SCAN_015':
    case 'ERR_SCAN_017':
      showToast('error', message);
      showUpgradeLink();
      break;

    // Active scan conflicts — show link to active scan
    case 'ERR_SCAN_006':
    case 'ERR_SCAN_013':
      showToast('error', message);
      if (details?.scan_id) showScanLink(details.scan_id as string);
      break;

    // Stale state — refresh data
    case 'ERR_SCAN_005':
    case 'ERR_SCAN_012':
    case 'ERR_SCAN_016':
      showToast('error', message);
      refetchData();
      break;

    default:
      showToast('error', message);
  }
}
```

---

## Input Validation Rules

### Domain Input

| Field | Type | Constraints | Error Message |
|-------|------|-------------|---------------|
| `domain` | string | Required. Max 253 chars. Max 63 chars per label. | "Domain is required." |
| | | Must match RFC 1035 format. | "Invalid domain format." |
| | | No IP addresses (IPv4/IPv6). | "IP addresses are not supported." |
| | | No protocol prefix (http://, https://). | "Enter domain without protocol." |
| | | No path (/page, /api). | "Enter domain without path." |
| | | No subdomains (must be bare domain). | "Enter root domain only (e.g., example.com)." |
| | | Must contain at least one dot. | "Invalid domain format." |
| | | Case-insensitive unique per tenant. | "This domain already exists." |

### Client-Side Domain Validation

```typescript
function validateDomain(input: string): string | null {
  const trimmed = input.trim().toLowerCase();

  if (!trimmed) return 'Domain is required.';
  if (trimmed.length > 253) return 'Domain is too long (max 253 characters).';

  // Check for protocol
  if (/^https?:\/\//i.test(trimmed)) return 'Enter domain without protocol.';

  // Check for path
  if (trimmed.includes('/')) return 'Enter domain without path.';

  // Check for IP address (v4)
  if (/^\d{1,3}(\.\d{1,3}){3}$/.test(trimmed)) return 'IP addresses are not supported.';

  // Check for IPv6
  if (trimmed.includes(':')) return 'IP addresses are not supported.';

  // Check for subdomain (more than one dot before TLD)
  const labels = trimmed.split('.');
  if (labels.length > 2) {
    // Allow two-part TLDs like .co.uk
    const knownTwoPartTlds = ['co.uk', 'com.au', 'co.jp', /* ... */];
    const lastTwo = labels.slice(-2).join('.');
    if (!knownTwoPartTlds.includes(lastTwo) && labels.length > 3) {
      return 'Enter root domain only (e.g., example.com).';
    }
    if (!knownTwoPartTlds.includes(lastTwo)) {
      return 'Enter root domain only (e.g., example.com).';
    }
  }

  // Check label length
  if (labels.some(l => l.length > 63)) return 'Domain label too long (max 63 characters).';

  // Must have at least one dot
  if (!trimmed.includes('.')) return 'Invalid domain format.';

  // RFC 1035 character check
  if (!/^[a-z0-9]([a-z0-9-]*[a-z0-9])?(\.[a-z0-9]([a-z0-9-]*[a-z0-9])?)*$/.test(trimmed)) {
    return 'Invalid domain format.';
  }

  return null; // valid
}
```

### Workflow Builder

| Field | Type | Constraints | Error Message |
|-------|------|-------------|---------------|
| `name` | string | Required. Max 100 chars. | "Workflow name is required." / "Name must be under 100 characters." |
| `steps_json` | array | Min 1 step. Max 15 steps. | "Add at least one step." / "Maximum 15 steps allowed." |
| `steps_json[].check_type` | string | Must be recognized step type. | "Unknown step type." |

### Schedule Creation

| Field | Type | Constraints | Error Message |
|-------|------|-------------|---------------|
| `domain_id` | UUID | Required. Must exist. | "Please select a domain." |
| `workflow_id` | UUID | Required. Must exist. | "Please select a workflow." |
| `cron_expression` | string | Required. Valid cron format. Min 24-hour interval. | "Invalid cron expression." / "Minimum interval is 24 hours." |

### Admin: Concurrent Limit

| Field | Type | Constraints | Error Message |
|-------|------|-------------|---------------|
| `max_concurrent_scans` | integer | Required. Min 1. Max 100. | "Must be between 1 and 100." |

### Client-Side Validation Timing

| Trigger | Behavior |
|---------|----------|
| Domain input blur | Validate format, show inline error |
| Domain input submit | Server-side uniqueness + limit check |
| Workflow step add/remove | Live step count validation |
| Workflow name blur | Required + length check |
| Cron input blur | Format validation + 24h interval check |
| Cron preset selection | Auto-generate valid cron, no validation needed |
| Concurrent limit input | Range validation (1-100) on blur |

---

## Security Considerations

### Secure Storage

| Data | Storage | Justification |
|------|---------|---------------|
| Domain list | Memory (state) | Fetched per session |
| Scan results | Memory (state) | Read-only, fetched per view |
| Screenshots | Server-rendered URLs | Signed URLs with expiry; never cached locally |
| Workflow definitions | Memory (state) | Fetched per session |
| Cron expressions | Memory (state) | No sensitivity |

### Rate Limiting Awareness

| Action | Rate Limit | Frontend Handling |
|--------|-----------|-------------------|
| Add domain | Backend enforced | Disable [Add Domain] during submission |
| Create scan | Backend enforced (+ concurrent limits) | Disable [Start Scan] during submission |
| Cancel scan | Backend enforced | Disable [Cancel] during submission |
| Create workflow | Backend enforced (+ count limit) | Disable [Create] during submission |
| Create schedule | Backend enforced (+ count limit) | Disable [Create] during submission |
| Enable/disable schedule | Backend enforced | Disable toggle during API call |
| Scan details poll | 10-second interval | Client-side interval, no server rate limit concern |
| Domain search (admin) | Debounce 300ms | Client-side debounce |

### Button Debouncing

| Action | Debounce Strategy |
|--------|-------------------|
| Add Domain | Disable on click → re-enable on response |
| Delete Domain | Disable on click → re-enable on response |
| Start Scan | Disable on click → re-enable on response |
| Cancel Scan | Disable on click → re-enable on response |
| Create Workflow | Disable on click → re-enable on response |
| Delete Workflow | Disable on click → re-enable on response |
| Create Schedule | Disable on click → re-enable on response |
| Enable/Disable Schedule | Disable toggle → re-enable on response |
| Save Concurrent Limit | Disable on click → re-enable on response |

### Input Sanitization

| Input | Sanitization |
|-------|-------------|
| Domain name | Trim whitespace. Lowercase. Strip protocol/path. No HTML. Max 253 chars. |
| Workflow name | Trim whitespace. No HTML. Max 100 chars. |
| Cron expression | Trim whitespace. Validate format. No special characters beyond cron syntax. Max 100 chars. |
| Admin tenant search | Trim whitespace. No HTML. Max 100 chars. |
| Concurrent limit | Parse as integer. Clamp to 1-100 range. |

### Screenshot Security

| Concern | Handling |
|---------|---------|
| Direct file access | Screenshots served via signed URLs with expiry from backend. Frontend never constructs storage paths. |
| Image content | Screenshots are system-generated (not user-uploaded). No XSS risk from image content. |
| Lightbox display | Render in `<img>` tag only. No `innerHTML`. |

### Scan Results Security

| Concern | Handling |
|---------|---------|
| Results immutability | Frontend renders read-only. No edit/delete actions exposed. |
| XSS from scan data | All result fields (banners, descriptions, remediation text) rendered as text, never as HTML. |
| CVE references | Display as plain text. Do not auto-link to external CVE databases (avoid open redirect risk). |
| Data retention | Frontend does not enforce retention. Backend handles cleanup. Expired data simply won't appear in API responses. |

---

## Key Actions → Backend Use Cases Mapping

### Domain Actions

| Frontend Action | Backend Use Case | Endpoint | Triggers |
|----------------|-----------------|----------|----------|
| Add domain | BR-SCAN-001: Add Domain | `POST /api/domains` | [Add Domain] submit |
| View domain list | List domains | `GET /api/domains` | Page load |
| View domain details | Get domain + aggregated data | `GET /api/domains/{id}` | Domain click |
| View subdomains | List subdomains for domain | `GET /api/domains/{id}/subdomains` | Overview tab |
| View ports | List ports for domain | `GET /api/domains/{id}/ports` | Overview tab |
| View technologies | List technologies for domain | `GET /api/domains/{id}/technologies` | Overview tab |
| View scan history | List scans for domain | `GET /api/domains/{id}/scans` | Scan History tab |
| Delete domain | BR-SCAN-004: Delete Domain | `DELETE /api/domains/{id}` | [Delete] confirm |

### Scan Actions

| Frontend Action | Backend Use Case | Endpoint | Triggers |
|----------------|-----------------|----------|----------|
| Create scan | BR-SCAN-005: Create Scan Job | `POST /api/scans` | [Start Scan] submit |
| View scan list | List scan jobs | `GET /api/scans` | Page load |
| View scan details | Get scan job + steps | `GET /api/scans/{id}` | Scan click / polling |
| View scan results | Get results by check type | `GET /api/scans/{id}/results` | Terminal state reached |
| Cancel scan | BR-SCAN-008: Cancel Scan | `DELETE /api/scans/{id}` | [Cancel Scan] confirm |
| Estimate cost | BR-BILL-011: Credit Balance Check | `POST /api/billing/credits/estimate` | Domain + workflow selection |

### Workflow Actions

| Frontend Action | Backend Use Case | Endpoint | Triggers |
|----------------|-----------------|----------|----------|
| View workflows | List workflows | `GET /api/workflows` | Page load |
| View workflow details | Get workflow | `GET /api/workflows/{id}` | Workflow click |
| Create custom workflow | BR-SCAN-015: Create Custom Workflow | `POST /api/workflows` | [Create Workflow] submit |
| Edit custom workflow | Update workflow | `PUT /api/workflows/{id}` | [Save Changes] submit |
| Delete custom workflow | Delete workflow | `DELETE /api/workflows/{id}` | [Delete] confirm |
| Use template | Navigate to /scans/new with workflow pre-selected | Client-side | [Use This] click |

### Schedule Actions

| Frontend Action | Backend Use Case | Endpoint | Triggers |
|----------------|-----------------|----------|----------|
| View schedules | List schedules | `GET /api/scans/schedules` | Page load |
| Create schedule | BR-SCAN-018: Create Scan Schedule | `POST /api/scans/schedules` | [Create Schedule] submit |
| Enable schedule | Enable schedule | `POST /api/scans/schedules/{id}/enable` | Toggle on |
| Disable schedule | Disable schedule | `POST /api/scans/schedules/{id}/disable` | Toggle off |
| Delete schedule | BR-SCAN-018: Schedule Cancellation | `DELETE /api/scans/schedules/{id}` | [Delete] confirm |

### Super Admin Actions

| Frontend Action | Backend Use Case | Endpoint | Triggers |
|----------------|-----------------|----------|----------|
| View tenant limits | List Enterprise tenant limits | `GET /api/admin/tenants/scan-limits` | Page load |
| Set custom limit | Configure concurrent scan limit | `PUT /api/admin/tenants/{id}/scan-limit` | [Save] confirm |
| Reset to default | Remove custom override | `DELETE /api/admin/tenants/{id}/scan-limit` | [Reset] confirm |

### System-Triggered UI Updates

| Backend Event | Frontend Effect | How Applied |
|--------------|----------------|-------------|
| Scan step completes | Step pipeline updates | Polling response |
| Scan reaches terminal state | Pipeline finalized, results tabs appear | Polling response → stop polling → fetch results |
| Step retry | Retry indicator in pipeline | Polling response |
| Step skip (dependency failed) | Skip indicator in pipeline | Polling response |
| Scan timeout (4h) | CANCELLED status | Polling response |
| Schedule auto-disabled | Disabled badge + reason | Next schedule list fetch |
| Domain limit change (plan upgrade) | Updated max in domain count badge | Next domain list fetch |

---

## State Management Notes

### What State to Track

| State | Scope | Persistence |
|-------|-------|-------------|
| Domain list | Domains page | Memory. Refresh on mutation. |
| Domain details | Domain details page | Memory. Refresh on scan completion. |
| Scan list (filtered) | Scan list page | Memory. Reset filters on page leave. |
| Scan details + steps | Scan details page | Memory. Polling while active. |
| Scan results | Scan details page | Memory. Cached per scan ID (immutable after completion). |
| Active results tab | Scan details page | Component state. Default: first completed step. |
| Workflows list | Workflows page | Memory. Refresh on mutation. |
| Workflow builder | Workflow builder page | Component state. Lost on navigation. |
| Schedules list | Schedules page | Memory. Refresh on mutation. |
| New schedule form | New schedule page | Component state. Lost on navigation. |
| New scan form | New scan page | Component state. Lost on navigation. |
| Credit estimate | New scan / new schedule | Derived. Re-fetch on domain/workflow change. |

### Reset Behavior

| Event | State Reset |
|-------|-------------|
| Domain added/deleted | Refetch domain list. |
| Scan created | Navigate to scan details. Refetch domain details (last scanned). |
| Scan cancelled | Refetch scan details (now terminal). Stop polling. |
| Scan completes (via polling) | Stop polling. Fetch results. Update results tabs. |
| Workflow created/edited/deleted | Refetch workflow list. |
| Schedule created/toggled/deleted | Refetch schedule list. |
| Navigation away from scan details | Stop polling. Clear scan details state. |
| Plan upgrade/downgrade | Refetch domain list (max limit), workflows (feature gate), schedules (feature gate). |

### Polling Lifecycle

| Condition | Polling State |
|-----------|--------------|
| Scan details page mounted, status QUEUED or RUNNING | Start polling every 10s |
| Poll returns terminal status | Stop polling, fetch results |
| 3 consecutive poll failures | Show error banner, continue polling |
| User navigates away | Stop polling (cleanup in useEffect) |
| User navigates back to same scan | Resume polling if still active |

### Unsaved Changes Guards

| Page | Guard Behavior |
|------|---------------|
| Workflow builder | If steps selected, confirm: "Discard unsaved workflow?" |
| New schedule | If fields filled, confirm: "Discard unsaved schedule?" |
| New scan | No guard (lightweight form, easy to re-create) |
