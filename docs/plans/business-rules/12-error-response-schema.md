# 12. Error Response Schema

> Covers: Standard error JSON envelope, error code naming convention, rate limiting, HTTP status code usage, system error codes, and master error code registry.
> This section is the single reference for all API error formatting across Reconova.

---

### 12.1 Standard Error Envelope

> Covers: BR-ERR-001, BR-ERR-002, BR-ERR-003

#### Rules

**BR-ERR-001: Standard error response format** — All API error responses MUST use this JSON envelope:

```json
{
  "error": {
    "code": "ERR_AUTH_003",
    "status": 401,
    "message": "Invalid or expired token.",
    "request_id": "req_a1b2c3d4e5",
    "timestamp": "2026-03-07T10:30:00Z",
    "details": []
  }
}
```

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `code` | string | Yes | Machine-readable error code. Format: `ERR_{DOMAIN}_{NNN}`. |
| `status` | integer | Yes | HTTP status code (mirrors the response status). |
| `message` | string | Yes | Human-readable error description. Safe to display to end users. No internal details, stack traces, or DB info. |
| `request_id` | string | Yes | Unique request identifier for tracing. Format: `req_{uuid_short}`. |
| `timestamp` | string (ISO 8601) | Yes | UTC timestamp of when the error occurred. |
| `details` | array | Yes | Field-level or sub-errors. Empty array `[]` when not applicable. |

**BR-ERR-002: Validation error details format** — For 400 Bad Request responses with field-level validation failures, the `details` array contains:

```json
{
  "error": {
    "code": "ERR_VALIDATION",
    "status": 400,
    "message": "Validation failed. See details.",
    "request_id": "req_x7y8z9",
    "timestamp": "2026-03-07T10:30:00Z",
    "details": [
      {
        "field": "email",
        "code": "INVALID_FORMAT",
        "message": "Must be a valid email address."
      },
      {
        "field": "password",
        "code": "TOO_SHORT",
        "message": "Must be at least 12 characters."
      }
    ]
  }
}
```

#### Detail Field Schema

| Detail Field | Type | Required | Description |
|-------------|------|----------|-------------|
| `field` | string | Yes | The request field that failed validation (dot notation for nested: `config.webhook_url`). |
| `code` | string | Yes | Validation rule that failed. |
| `message` | string | Yes | Human-readable description of the validation failure. |

#### Validation Codes

| Code | Meaning |
|------|---------|
| `REQUIRED` | Field is missing or null. |
| `INVALID_FORMAT` | Field value does not match expected format (email, URL, UUID, etc.). |
| `TOO_SHORT` | String length or array count below minimum. |
| `TOO_LONG` | String length or array count above maximum. |
| `OUT_OF_RANGE` | Numeric value outside allowed range. |
| `NOT_UNIQUE` | Value already exists (unique constraint violation). |
| `INVALID_VALUE` | Value not in allowed set (e.g., invalid enum value). |

**BR-ERR-003: No sensitive data in error responses** — Error messages MUST NOT contain: stack traces, database column/table names, internal service names, raw SQL, API keys, file paths, or any PII beyond what the user submitted. Violations are treated as security incidents.

---

### 12.2 Error Code Naming Convention

> Covers: BR-ERR-004, BR-ERR-005

**BR-ERR-004: Error code format** — All error codes follow the pattern `ERR_{DOMAIN}_{NNN}`:

| Component | Rule |
|-----------|------|
| Prefix | Always `ERR_` |
| Domain | Uppercase abbreviation of the business domain (3-6 chars) |
| Number | 3-digit, zero-padded, sequential within domain |

#### Registered Domains

| Domain Code | Section | Range |
|-------------|---------|-------|
| `AUTH` | 1. Authentication & Account Security | ERR_AUTH_001 - ERR_AUTH_015 |
| `TNT` | 2. Tenant Management | ERR_TNT_001 - ERR_TNT_014 |
| `BILL` | 3. Billing & Credits | ERR_BILL_001 - ERR_BILL_018 |
| `SCAN` | 4. Scanning & Workflows | ERR_SCAN_001 - ERR_SCAN_021 |
| `FLAG` | 5. Feature Flags & Access Control | ERR_FLAG_001 - ERR_FLAG_012 |
| `COMP` | 6. Compliance Engine | ERR_COMP_001+ |
| `CVE` | 7. CVE Monitoring | ERR_CVE_001+ |
| `INT` | 8. Integrations | ERR_INT_001+ |
| `ADM` | 9. Super Admin & Operations | ERR_ADM_001+ |
| `DATA` | 10. Data, Audit & Platform Compliance | ERR_DATA_001+ |
| `VER` | 13. Version History & Versioning | ERR_VER_001 - ERR_VER_015 |
| `VALIDATION` | Cross-cutting | ERR_VALIDATION (single code) |
| `RATE` | Cross-cutting | ERR_RATE_001+ |
| `SYS` | System/infrastructure | ERR_SYS_001+ |

> Sections 7-11 ranges will be populated as those sections are completed.

**BR-ERR-005: Error code immutability** — Once an error code is published and used by clients, it MUST NOT be reassigned to a different meaning. Retired error codes are marked as deprecated but never reused.

---

### 12.3 Rate Limiting

> Covers: BR-ERR-006 through BR-ERR-010

**BR-ERR-006: Rate limit response format** — When a request is rate-limited, respond with HTTP 429 and the standard error envelope:

```json
{
  "error": {
    "code": "ERR_RATE_001",
    "status": 429,
    "message": "Rate limit exceeded. Try again in 30 seconds.",
    "request_id": "req_abc123",
    "timestamp": "2026-03-07T10:30:00Z",
    "details": [
      {
        "field": "rate_limit",
        "code": "LIMIT_EXCEEDED",
        "message": "60 requests per minute allowed. Retry after 30 seconds."
      }
    ]
  }
}
```

**BR-ERR-007: Rate limit response headers** — ALL API responses (success and error) MUST include these headers:

| Header | Description | Example |
|--------|-------------|---------|
| `X-RateLimit-Limit` | Max requests allowed in the current window | `60` |
| `X-RateLimit-Remaining` | Requests remaining in the current window | `42` |
| `X-RateLimit-Reset` | Unix timestamp when the window resets | `1741342800` |
| `Retry-After` | Seconds until the client can retry (only on 429 responses) | `30` |

**BR-ERR-008: Per-tenant rate limits** — Rate limits are scoped per-tenant (not per-user), identified by `tenant_id` from JWT claims. Limits scale by subscription tier:

| Tier | Requests/minute | Scan creation/hour | Burst allowance |
|------|----------------|--------------------|----|
| Starter | 60 | 5 | 10 extra for 5 seconds |
| Pro | 200 | 20 | 30 extra for 5 seconds |
| Enterprise | 500 | 50 | 100 extra for 5 seconds |

**BR-ERR-009: Rate limit storage** — Rate limit counters are stored in Redis using sliding window counters. Key format: `ratelimit:{tenant_id}:{endpoint_group}:{window}`. Counters expire automatically with the window TTL.

**BR-ERR-010: Super admin rate limit exemption** — Super admin requests are exempt from per-tenant rate limits but subject to a global super admin limit of 1000 requests/minute to prevent accidental runaway scripts.

---

### 12.4 HTTP Status Code Usage

> Covers: BR-ERR-011, BR-ERR-012

**BR-ERR-011: HTTP status code mapping** — Reconova uses these HTTP status codes consistently across all endpoints:

| Status | Usage | When |
|--------|-------|------|
| `200` | Success | Read operations, updates that return the updated resource |
| `201` | Created | Resource successfully created (POST) |
| `204` | No Content | Successful deletion, operations with no response body |
| `400` | Bad Request | Validation failures, malformed request body, business rule violations (invalid input) |
| `401` | Unauthorized | Missing/invalid/expired JWT, 2FA required |
| `403` | Forbidden | Authenticated but insufficient permissions, feature flag blocked, plan-gated |
| `404` | Not Found | Resource does not exist or tenant has no access |
| `409` | Conflict | Duplicate resource, concurrent modification, state conflict |
| `410` | Gone | Sunset API version, permanently removed resource |
| `422` | Unprocessable Entity | Semantically invalid request (valid JSON but violates business rules) |
| `429` | Too Many Requests | Rate limit exceeded |
| `500` | Internal Server Error | Unhandled exception. Logged with request_id for debugging. Error message is generic. |
| `503` | Service Unavailable | Maintenance mode (operational flag), dependent service down |

**BR-ERR-012: 500 error handling** — Internal server errors (500) MUST:

```
BR-ERR-012A: Internal Error Handling
--------------------------------------
Trigger: Unhandled exception in request pipeline

1. GENERATE request_id if not already assigned
2. LOG full exception with:
   - Stack trace
   - Request context (method, path, headers, body hash)
   - tenant_id, user_id (if available)
   - request_id
3. RETURN standard error envelope:
   {
     code: "ERR_SYS_001",
     status: 500,
     message: "An unexpected error occurred. Reference: {request_id}",
     request_id, timestamp, details: []
   }
4. NEVER expose internal details in the response
5. INCREMENT 500 error counter
6. IF 500 error rate > 5 per minute:
   CREATE critical alert for operations team
```

---

### 12.5 System Error Codes

> Covers: BR-ERR-013

**BR-ERR-013: System-level error codes** — Cross-cutting errors not tied to a specific business domain:

| Code | HTTP | Message | Cause |
|------|------|---------|-------|
| `ERR_SYS_001` | 500 | An unexpected error occurred. Reference: {request_id}. | Unhandled server exception. |
| `ERR_SYS_002` | 503 | Service temporarily unavailable. Maintenance in progress. | Maintenance mode operational flag is active. |
| `ERR_SYS_003` | 503 | Service temporarily unavailable. Please try again later. | Dependent service (Redis, external API) is unreachable. |
| `ERR_SYS_004` | 400 | Invalid request format. | Malformed JSON, unsupported content type. |
| `ERR_SYS_005` | 401 | Authentication required. | No Authorization header present. |
| `ERR_RATE_001` | 429 | Rate limit exceeded. Try again in {N} seconds. | Per-tenant rate limit hit. |
| `ERR_RATE_002` | 429 | Scan creation rate limit exceeded. Try again in {N} minutes. | Scan creation hourly limit hit. |
| `ERR_VALIDATION` | 400 | Validation failed. See details. | One or more request fields failed validation. Details array populated. |

---

### 12.6 Master Error Code Registry

> Consolidated lookup of all error codes across all sections for quick reference.

#### Authentication & Account Security (Section 1)

| Code | HTTP | Message |
|------|------|---------|
| `ERR_AUTH_001` | 400 | Email already registered. |
| `ERR_AUTH_002` | 400 | Password does not meet strength requirements. |
| `ERR_AUTH_003` | 401 | Invalid or expired token. |
| `ERR_AUTH_004` | 401 | Invalid email or password. |
| `ERR_AUTH_005` | 403 | 2FA verification required. |
| `ERR_AUTH_006` | 400 | Invalid 2FA code. |
| `ERR_AUTH_007` | 403 | Account locked due to too many failed attempts. |
| `ERR_AUTH_008` | 401 | Refresh token invalid or expired. |
| `ERR_AUTH_009` | 400 | 2FA is already enabled. |
| `ERR_AUTH_010` | 400 | 2FA setup required before accessing platform. |
| `ERR_AUTH_011` | 403 | Account is deactivated. |
| `ERR_AUTH_012` | 400 | Invalid recovery code. |
| `ERR_AUTH_013` | 429 | Too many password reset requests. |
| `ERR_AUTH_014` | 400 | Password reset token expired. |
| `ERR_AUTH_015` | 403 | Session revoked. Please log in again. |

#### Tenant Management (Section 2)

| Code | HTTP | Message |
|------|------|---------|
| `ERR_TNT_001` | 400 | Tenant name already taken. |
| `ERR_TNT_002` | 400 | Invalid tenant slug format. |
| `ERR_TNT_003` | 404 | Tenant not found. |
| `ERR_TNT_004` | 403 | Tenant is suspended. |
| `ERR_TNT_005` | 400 | Maximum domain limit reached for this plan. |
| `ERR_TNT_006` | 400 | Domain already added to this tenant. |
| `ERR_TNT_007` | 400 | Invalid domain format. |
| `ERR_TNT_008` | 403 | Tenant is deactivated. Contact support. |
| `ERR_TNT_009` | 400 | Domain verification failed. |
| `ERR_TNT_010` | 403 | Cannot modify tenant in current state. |
| `ERR_TNT_011` | 400 | Tenant database provisioning failed. |
| `ERR_TNT_012` | 409 | Tenant slug already in use. |
| `ERR_TNT_013` | 400 | Cannot delete tenant with active subscription. |
| `ERR_TNT_014` | 403 | Impersonation session active. Some actions restricted. |

#### Billing & Credits (Section 3)

| Code | HTTP | Message |
|------|------|---------|
| `ERR_BILL_001` | 400 | Invalid subscription plan. |
| `ERR_BILL_002` | 402 | Insufficient credits. |
| `ERR_BILL_003` | 400 | Stripe payment failed. |
| `ERR_BILL_004` | 400 | Subscription already active. |
| `ERR_BILL_005` | 400 | Cannot downgrade with active scheduled scans exceeding new plan limits. |
| `ERR_BILL_006` | 400 | Credit pack purchase failed. |
| `ERR_BILL_007` | 400 | Invalid coupon code. |
| `ERR_BILL_008` | 400 | Subscription cancellation failed. |
| `ERR_BILL_009` | 402 | Payment method required. |
| `ERR_BILL_010` | 400 | Cannot purchase credits without active subscription. |
| `ERR_BILL_011` | 400 | Annual billing change restricted during current period. |
| `ERR_BILL_012` | 409 | Credit transaction conflict. Retry. |
| `ERR_BILL_013` | 400 | Refund amount exceeds original transaction. |
| `ERR_BILL_014` | 404 | Credit transaction not found. |
| `ERR_BILL_015` | 400 | Maximum credit pack purchase limit reached. |
| `ERR_BILL_016` | 400 | Stripe webhook signature verification failed. |
| `ERR_BILL_017` | 400 | Subscription plan not available in your region. |
| `ERR_BILL_018` | 400 | Credit allotment already applied for this period. |

#### Scanning & Workflows (Section 4)

| Code | HTTP | Message |
|------|------|---------|
| `ERR_SCAN_001` | 400 | Invalid domain target. |
| `ERR_SCAN_002` | 400 | Workflow not found or not accessible. |
| `ERR_SCAN_003` | 402 | Insufficient credits for this scan. |
| `ERR_SCAN_004` | 409 | Scan already in progress for this domain. |
| `ERR_SCAN_005` | 400 | No valid scan steps in workflow. |
| `ERR_SCAN_006` | 400 | Invalid workflow configuration. |
| `ERR_SCAN_007` | 403 | Scan step not available on current plan. |
| `ERR_SCAN_008` | 404 | Scan job not found. |
| `ERR_SCAN_009` | 400 | Cannot cancel scan in current state. |
| `ERR_SCAN_010` | 400 | Scan schedule cron expression invalid. |
| `ERR_SCAN_011` | 400 | Maximum concurrent scans reached. |
| `ERR_SCAN_012` | 500 | Scan worker execution failed. |
| `ERR_SCAN_013` | 400 | Domain not verified. Complete verification first. |
| `ERR_SCAN_014` | 400 | Scan timeout exceeded (4 hours). |
| `ERR_SCAN_015` | 403 | All workflow steps blocked by feature flags. |
| `ERR_SCAN_016` | 400 | Cannot modify system workflow template. |
| `ERR_SCAN_017` | 403 | Scan step disabled by operational flag during execution. |
| `ERR_SCAN_018` | 400 | Duplicate scan schedule for this domain and workflow. |
| `ERR_SCAN_019` | 400 | Maximum scan schedules reached for this plan. |
| `ERR_SCAN_020` | 400 | Scan result data exceeds storage limit. |
| `ERR_SCAN_021` | 503 | Scan queue is full. Try again later. |

#### Feature Flags & Access Control (Section 5)

| Code | HTTP | Message |
|------|------|---------|
| `ERR_FLAG_001` | 404 | Feature flag not found. |
| `ERR_FLAG_002` | 400 | Invalid flag type. |
| `ERR_FLAG_003` | 403 | Upgrade to {tier} to access {feature}. |
| `ERR_FLAG_004` | 403 | Feature temporarily unavailable. |
| `ERR_FLAG_005` | 403 | Insufficient permissions to manage feature flags. |
| `ERR_FLAG_006` | 400 | Override reason is required (minimum 10 characters). |
| `ERR_FLAG_007` | 400 | Operational flags cannot be overridden per tenant. |
| `ERR_FLAG_008` | 404 | Tenant not found. |
| `ERR_FLAG_009` | 409 | Override already exists. Use update endpoint. |
| `ERR_FLAG_010` | 404 | Override not found. |
| `ERR_FLAG_011` | 400 | Feature flag name already exists. |
| `ERR_FLAG_012` | 400 | Cannot delete flag with active overrides. |

#### Compliance Engine (Section 6)

> Error codes defined in section 6 document. Will be added to registry when section is finalized.

#### CVE Monitoring (Section 7)

> Pending — will be populated when section 7 is completed.

#### Integrations (Section 8)

> Pending — will be populated when section 8 is completed.

#### Super Admin & Operations (Section 9)

> Pending — will be populated when section 9 is completed.

#### Data, Audit & Platform Compliance (Section 10)

> Pending — will be populated when section 10 is completed.

#### Version History & Versioning (Section 13)

| Code | HTTP | Message |
|------|------|---------|
| `ERR_VER_001` | 410 | API version {N} has been sunset. Use /api/v{current}/ instead. |
| `ERR_VER_002` | 400 | Migration missing rollback script. |
| `ERR_VER_003` | 409 | Migration already in progress for tenant {id}. |
| `ERR_VER_004` | 400 | Migration version gap detected. Expected {N}, got {M}. |
| `ERR_VER_005` | 403 | Breaking migration requires super admin approval. |
| `ERR_VER_006` | 400 | Breaking migration must be tested on staging first. |
| `ERR_VER_007` | 400 | Grace period must be at least 30 days. |
| `ERR_VER_008` | 409 | Framework already has an active version. Deprecate current first. |
| `ERR_VER_009` | 404 | No update available for this workflow. |
| `ERR_VER_010` | 400 | Cannot sunset framework during grace period. Wait until {date}. |
| `ERR_VER_011` | 403 | Insufficient permissions. Super admin role required. |
| `ERR_VER_012` | 400 | Invalid API version number. Must be sequential. |
| `ERR_VER_013` | 404 | API version not found. |
| `ERR_VER_014` | 400 | Only deprecated API versions can be sunset. |
| `ERR_VER_015` | 400 | Minimum deprecation period (6 months) not elapsed. |

#### System & Rate Limiting (Section 12)

| Code | HTTP | Message |
|------|------|---------|
| `ERR_SYS_001` | 500 | An unexpected error occurred. Reference: {request_id}. |
| `ERR_SYS_002` | 503 | Service temporarily unavailable. Maintenance in progress. |
| `ERR_SYS_003` | 503 | Service temporarily unavailable. Please try again later. |
| `ERR_SYS_004` | 400 | Invalid request format. |
| `ERR_SYS_005` | 401 | Authentication required. |
| `ERR_RATE_001` | 429 | Rate limit exceeded. Try again in {N} seconds. |
| `ERR_RATE_002` | 429 | Scan creation rate limit exceeded. Try again in {N} minutes. |
| `ERR_VALIDATION` | 400 | Validation failed. See details. |

---

### 12.7 Error Logging Rules

> Covers: BR-ERR-014, BR-ERR-015

**BR-ERR-014: Error audit logging** — All errors with HTTP status >= 400 are logged to the audit system with: `request_id`, `tenant_id`, `user_id`, `error_code`, `endpoint`, `ip_address`, `timestamp`. Client-error (4xx) logs are retained for 90 days. Server-error (5xx) logs are retained for 1 year.

**BR-ERR-015: Error alerting thresholds** — Automated alerts for operations team:

| Condition | Alert Level | Action |
|-----------|------------|--------|
| 500 errors > 5/minute | Critical | Page on-call engineer |
| 429 errors > 100/minute for single tenant | Warning | Flag potential abuse |
| 401 errors > 50/minute from single IP | Warning | Flag potential brute force (cross-ref with BR-AUTH lockout) |
| 503 errors (maintenance mode active) | Info | Confirm intentional maintenance |

---

### 12.8 Permissions Matrix

| Action | `TENANT_OWNER` | `SUPER_ADMIN` |
|--------|:--------------:|:-------------:|
| Receive error responses with standard envelope | Yes | Yes |
| View own request_id in error responses | Yes | Yes |
| Look up error details by request_id | No | Yes |
| View rate limit headers on responses | Yes | Yes |
| View error alerting dashboard | No | Yes |
| Configure alert thresholds | No | Yes |
| View cross-tenant error logs | No | Yes |
| Modify rate limit tiers | No | Yes |

---

### 12.9 Edge Cases

| Scenario | Behavior |
|----------|----------|
| Multiple validation errors on same field | Include all violations in details array. Each gets its own entry. |
| Error during error serialization | Fallback to plain text: `500 Internal Server Error. Reference: {request_id}`. Log the serialization failure. |
| Rate limit hit during scan execution | Scan worker is exempt from API rate limits. Only HTTP API requests are rate-limited. |
| Tenant switches plan mid-window | New rate limits take effect immediately. Current window counters reset. |
| Redis unavailable for rate limiting | Allow the request through (fail open). Log warning. Do NOT block requests due to rate limit infrastructure failure. |
| Error code collision between sections | Prevented by domain prefix. Each section owns its domain prefix exclusively. |
| Deprecated error code encountered | Return the error normally but add `deprecated: true` field to the envelope. Log for tracking. |
| Unauthenticated request hits rate limit | Rate limit by IP address instead of tenant_id. IP-based limit: 30 requests/minute. |
| Error response for non-JSON Accept header | Still return JSON error envelope. Reconova API always responds with `application/json`. |
| Concurrent requests exhaust rate limit simultaneously | Redis atomic operations ensure accurate counting. One request gets through, others receive 429. No race conditions. |
