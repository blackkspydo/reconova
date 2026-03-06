# 4. Scanning & Workflows

> Covers: BR-SCAN-001 through BR-SCAN-021 from the original business rules.
> Error response JSON schema is defined in `00-index.md` Section 12 reference.

### 4.1 Scan Job States

> Covers: BR-SCAN-007

#### Status Values

| Status | Meaning |
|--------|---------|
| `QUEUED` | Scan job created, credits deducted, waiting in Redis queue for worker pickup. |
| `RUNNING` | Worker picked up the job. Steps executing sequentially. |
| `COMPLETED` | All workflow steps finished successfully. Full results available. |
| `PARTIAL` | Some steps completed, but one or more steps failed or timed out. Partial results available. Credits for failed/skipped steps refunded. |
| `FAILED` | Scan could not produce any results. Typically the first step failed with no independent steps remaining. All unexecuted step credits refunded. |
| `CANCELLED` | Scan was cancelled by tenant, by system (suspension), or timed out entirely (4-hour limit). Credits for unexecuted steps refunded. |

#### State Machine

```
                    ┌──────────────┐
   Scan created     │    QUEUED    │
  ─────────────────►│              │
                    └──────┬───────┘
                           │ Worker picks up job
                           ▼
                    ┌──────────────┐
                    │   RUNNING    │
                    └──┬──┬──┬──┬─┘
                       │  │  │  │
            All steps  │  │  │  │ Tenant/system
            succeed    │  │  │  │ cancels or
                       │  │  │  │ 4-hour timeout
                       ▼  │  │  ▼
              ┌──────────┐│  │┌──────────────┐
              │COMPLETED ││  ││  CANCELLED   │
              └──────────┘│  │└──────────────┘
                          │  │
             Some steps   │  │ All steps
             fail, some   │  │ fail / first
             succeed      │  │ step fails
                          ▼  ▼
                 ┌──────────┐┌──────────┐
                 │ PARTIAL  ││  FAILED  │
                 └──────────┘└──────────┘
```

#### State Transition Table

| From | To | Trigger | Who | Side Effects |
|------|----|---------|-----|-------------|
| _(new)_ | `QUEUED` | Scan job created, credits deducted | `TENANT_OWNER` | `scan_jobs` record inserted. Credits deducted (§3.6). Job pushed to Redis queue. |
| `QUEUED` | `RUNNING` | Worker picks up job from queue | System | `scan_jobs.started_at` set. |
| `QUEUED` | `CANCELLED` | Tenant cancels before worker pickup | `TENANT_OWNER` | Job removed from queue. All step credits refunded. |
| `QUEUED` | `CANCELLED` | Tenant suspended (BR-TNT-006) | `SUPER_ADMIN` | Same as tenant cancel. |
| `RUNNING` | `COMPLETED` | All workflow steps finish successfully | System | `scan_jobs.completed_at` set. Notifications sent if configured. Compliance mapping triggered. |
| `RUNNING` | `PARTIAL` | Some steps succeed, some fail/timeout/skip | System | `scan_jobs.completed_at` set. Credits refunded for failed/skipped steps (§3.9). |
| `RUNNING` | `FAILED` | No steps produced results | System | `scan_jobs.completed_at` set. All unexecuted step credits refunded. |
| `RUNNING` | `CANCELLED` | Tenant cancels running scan | `TENANT_OWNER` | Current step completes, then scan stops. Unexecuted step credits refunded. |
| `RUNNING` | `CANCELLED` | Tenant suspended (BR-TNT-006) | `SUPER_ADMIN` | Same as tenant cancel. Partial results preserved. |
| `RUNNING` | `CANCELLED` | 4-hour scan timeout exceeded | System | Scan forcefully stopped. Partial results preserved. Unexecuted step credits refunded. |

**Terminal states:** `COMPLETED`, `PARTIAL`, `FAILED`, `CANCELLED`. No transitions out of these.

---

### 4.2 Field Constraints

> Covers: BR-SCAN-001, BR-SCAN-003, BR-SCAN-007, BR-SCAN-012, BR-SCAN-013

#### `domains` Table (Tenant DB)

| Field | Type | Constraints |
|-------|------|------------|
| `id` | UUID | System-generated. Immutable. Primary key. |
| `domain` | string | Required. Max 253 chars. Valid domain format (no IPs, no URLs, no subdomains). Unique per tenant (case-insensitive). Stored lowercase. |
| `status` | string | Required. CHECK (`ACTIVE`, `PENDING_VERIFICATION`, `VERIFIED`). Default: `ACTIVE` (MVP). `PENDING_VERIFICATION` and `VERIFIED` reserved for post-MVP domain verification (§2.11). |
| `added_by` | UUID | Required. FK → `users.id` (control DB). The user who added the domain. Immutable. |
| `verified_at` | timestamp | Nullable. [POST-MVP] Set when DNS TXT verification succeeds. |
| `created_at` | timestamp | System-generated. Immutable. Default: `NOW()`. |

#### `subdomains` Table (Tenant DB)

| Field | Type | Constraints |
|-------|------|------------|
| `id` | UUID | System-generated. Immutable. Primary key. |
| `domain_id` | UUID | Required. FK → `domains.id`. Indexed. |
| `subdomain` | string | Required. Max 253 chars. Full subdomain (e.g., `api.example.com`). |
| `source` | string | Required. Max 50 chars. Discovery source (e.g., `subfinder`, `amass`, `securitytrails`). |
| `first_seen` | timestamp | Required. When first discovered. Immutable. |
| `last_seen` | timestamp | Required. Updated on each scan that rediscovers this subdomain. |

**Composite unique constraint:** `(domain_id, subdomain)` — no duplicate subdomains per domain.

#### `ports` Table (Tenant DB)

| Field | Type | Constraints |
|-------|------|------------|
| `id` | UUID | System-generated. Immutable. Primary key. |
| `subdomain_id` | UUID | Required. FK → `subdomains.id`. Indexed. |
| `port` | int | Required. Range: 1–65535. |
| `protocol` | string | Required. Max 10 chars. E.g., `tcp`, `udp`. |
| `service` | string | Nullable. Max 100 chars. Detected service name (e.g., `http`, `ssh`, `mysql`). |
| `banner` | string | Nullable. Max 2000 chars. Service banner text. |
| `discovered_at` | timestamp | Required. Default: `NOW()`. |

**Composite unique constraint:** `(subdomain_id, port, protocol)`.

#### `technologies` Table (Tenant DB)

| Field | Type | Constraints |
|-------|------|------------|
| `id` | UUID | System-generated. Immutable. Primary key. |
| `subdomain_id` | UUID | Required. FK → `subdomains.id`. Indexed. |
| `tech_name` | string | Required. Max 100 chars. E.g., `nginx`, `React`, `WordPress`. |
| `version` | string | Nullable. Max 50 chars. Detected version if available. |
| `category` | string | Required. Max 50 chars. E.g., `web-server`, `framework`, `cms`, `cdn`. |
| `detected_at` | timestamp | Required. Default: `NOW()`. |

#### `screenshots` Table (Tenant DB)

| Field | Type | Constraints |
|-------|------|------------|
| `id` | UUID | System-generated. Immutable. Primary key. |
| `subdomain_id` | UUID | Required. FK → `subdomains.id`. Indexed. |
| `url` | string | Required. Max 2048 chars. The URL that was screenshotted. |
| `storage_path` | string | Required. Max 500 chars. Path to screenshot file (local disk or S3). |
| `taken_at` | timestamp | Required. Default: `NOW()`. |

#### `scan_jobs` Table (Tenant DB)

| Field | Type | Constraints |
|-------|------|------------|
| `id` | UUID | System-generated. Immutable. Primary key. |
| `domain_id` | UUID | Required. FK → `domains.id`. Indexed. |
| `workflow_id` | UUID | Required. FK → `workflows.id`. |
| `status` | string | Required. CHECK (`QUEUED`, `RUNNING`, `COMPLETED`, `PARTIAL`, `FAILED`, `CANCELLED`). Default: `QUEUED`. |
| `steps_json` | string | Required. JSON array of workflow steps with their pricing snapshot at creation time. Immutable after creation. |
| `total_credits` | int | Required. Total credits deducted at creation. Used for refund calculations. |
| `current_step` | int | Nullable. Index (0-based) of the currently executing step. Null when `QUEUED`. |
| `started_at` | timestamp | Nullable. Set when worker picks up the job. |
| `completed_at` | timestamp | Nullable. Set when scan reaches a terminal state. |
| `created_at` | timestamp | System-generated. Immutable. Default: `NOW()`. |
| `created_by` | UUID | Required. FK → `users.id` (control DB). |
| `cancelled_by` | UUID | Nullable. FK → `users.id`. Set if manually cancelled. |
| `cancellation_reason` | string | Nullable. Max 200 chars. E.g., `user_requested`, `tenant_suspended`, `timeout`. |

#### `scan_results` Table (Tenant DB)

| Field | Type | Constraints |
|-------|------|------------|
| `id` | UUID | System-generated. Immutable. Primary key. |
| `scan_job_id` | UUID | Required. FK → `scan_jobs.id`. Indexed. |
| `check_type` | string | Required. Max 50 chars. The step type that produced this result. |
| `target` | string | Required. Max 253 chars. The subdomain/domain targeted. |
| `data_json` | string | Required. JSON blob containing step-specific result data. |
| `severity` | string | Nullable. CHECK (`INFO`, `LOW`, `MEDIUM`, `HIGH`, `CRITICAL`). Set for vulnerability-producing steps. |
| `created_at` | timestamp | System-generated. Immutable. Default: `NOW()`. |

#### `vulnerabilities` Table (Tenant DB)

| Field | Type | Constraints |
|-------|------|------------|
| `id` | UUID | System-generated. Immutable. Primary key. |
| `scan_result_id` | UUID | Required. FK → `scan_results.id`. Indexed. |
| `cve` | string | Nullable. Max 20 chars. CVE identifier (e.g., `CVE-2024-1234`). Null for non-CVE findings. |
| `severity` | string | Required. CHECK (`LOW`, `MEDIUM`, `HIGH`, `CRITICAL`). |
| `description` | string | Required. Max 2000 chars. Vulnerability description. |
| `remediation` | string | Nullable. Max 2000 chars. Suggested fix. |
| `created_at` | timestamp | System-generated. Immutable. Default: `NOW()`. |

#### `workflows` Table (Tenant DB)

| Field | Type | Constraints |
|-------|------|------------|
| `id` | UUID | System-generated. Immutable. Primary key. |
| `name` | string | Required. Max 100 chars. |
| `template_id` | UUID | Nullable. FK → `workflow_templates.id`. Set if duplicated from a template. |
| `steps_json` | string | Required. JSON array of `WorkflowStepDefinition` objects. Min 1 step, max 15 steps. |
| `created_by` | UUID | Required. FK → `users.id` (control DB). |
| `created_at` | timestamp | System-generated. Immutable. Default: `NOW()`. |
| `updated_at` | timestamp | Required. Updated on modification. |

#### `workflow_templates` Table (Tenant DB)

| Field | Type | Constraints |
|-------|------|------------|
| `id` | UUID | System-generated. Immutable. Primary key. |
| `name` | string | Required. Max 100 chars. |
| `description` | string | Nullable. Max 500 chars. |
| `steps_json` | string | Required. JSON array of `WorkflowStepDefinition` objects. |
| `is_system` | boolean | Required. Default: true. System templates cannot be modified or deleted. |

#### `scan_schedules` Table (Tenant DB)

| Field | Type | Constraints |
|-------|------|------------|
| `id` | UUID | System-generated. Immutable. Primary key. |
| `domain_id` | UUID | Required. FK → `domains.id`. Indexed. |
| `workflow_id` | UUID | Required. FK → `workflows.id`. |
| `cron_expression` | string | Required. Max 100 chars. Standard cron format. Minimum interval: 24 hours. |
| `enabled` | boolean | Required. Default: true. Set false on suspension, downgrade, or manual disable. |
| `estimated_credits` | int | Required. Estimated credits per execution at time of schedule creation. |
| `last_run_at` | timestamp | Nullable. Updated after each execution. |
| `next_run_at` | timestamp | Nullable. Calculated from cron expression. |
| `created_at` | timestamp | System-generated. Immutable. Default: `NOW()`. |
| `created_by` | UUID | Required. FK → `users.id` (control DB). |

---

### 4.3 Domain Management

> Covers: BR-SCAN-001, BR-SCAN-002, BR-SCAN-003, BR-SCAN-004

#### Add Domain Algorithm

```
BR-SCAN-001/002/003: Add Domain
────────────────────────────────
Input: tenant_id, domain_name, user_id

1. VALIDATE domain format:
   IF NOT matches valid domain pattern (RFC 1035) → REJECT "ERR_SCAN_001"
   IF is IP address → REJECT "ERR_SCAN_001"
   IF contains path or protocol (e.g., https://...) → REJECT "ERR_SCAN_001"
   IF is a subdomain (more than one dot, excluding TLDs like .co.uk) → REJECT "ERR_SCAN_002"

2. NORMALIZE: lowercase(domain_name)

3. CHECK uniqueness within tenant:
   LOAD domain WHERE domain = normalized AND tenant = tenant_id
   IF exists → REJECT "ERR_SCAN_003"

4. CHECK domain limit:
   COUNT domains WHERE tenant = tenant_id
   LOAD plan.max_domains for tenant
   IF count >= max_domains AND max_domains != -1 → REJECT "ERR_SCAN_004"

5. INSERT domain:
   {
     domain: normalized,
     status: "ACTIVE",
     added_by: user_id,
     created_at: now()
   }

6. AUDIT_LOG("scan.domain_added", tenant_id, { domain: normalized })
```

#### Domain Format Rules

| Rule | Detail |
|------|--------|
| Valid characters | Letters, digits, hyphens, dots. Per RFC 1035. |
| Max length | 253 characters total. 63 per label. |
| No IP addresses | Reject `192.168.1.1`, `10.0.0.1`, IPv6. |
| No URLs | Reject `https://example.com/path`. Strip protocol/path if provided? No — reject entirely. |
| No subdomains | Reject `api.example.com`. Subdomains are discovered by scans. |
| Case | Stored lowercase. Uniqueness check case-insensitive. |
| TLD validation | Must have at least one dot. Bare words rejected (e.g., `localhost`). |

#### Domain Limits by Tier

| Tier | Max Domains |
|------|------------|
| Free | 0 (cannot add domains) |
| Starter | 3 |
| Pro | 20 |
| Enterprise | Unlimited (`-1`) |

#### Delete Domain

```
BR-SCAN-004: Delete Domain
──────────────────────────
Input: tenant_id, domain_id, user_id

1. LOAD domain WHERE id = domain_id AND tenant = tenant_id
   IF NOT found → REJECT "ERR_SCAN_005"

2. CHECK for active scans:
   LOAD scan_jobs WHERE domain_id = domain_id AND status IN ("QUEUED", "RUNNING")
   IF any exist → REJECT "ERR_SCAN_006"

3. DISABLE associated schedules:
   SET scan_schedules.enabled = false WHERE domain_id = domain_id

4. DELETE domain record
   CASCADE: subdomains, ports, technologies, screenshots for this domain

5. AUDIT_LOG("scan.domain_deleted", tenant_id, { domain: domain.domain })
```

#### Domain Constraints

| Constraint | Detail |
|-----------|--------|
| Cross-tenant duplicates | Allowed. Multiple tenants can scan the same domain independently. Each tenant's data is isolated in their own DB. |
| Domain transfer | Not supported. A domain cannot be moved between tenants. |
| Domain editing | Domain name is immutable after creation. Delete and re-add if wrong. |
| Deletion cascade | Deleting a domain removes all associated subdomains, ports, technologies, screenshots, scan results, and schedules. |

---

### 4.4 Workflow Templates & Custom Workflows

> Covers: BR-SCAN-014, BR-SCAN-015, BR-SCAN-016, BR-SCAN-017

#### System Workflow Templates

| Template | Steps (in order) | Use Case |
|----------|-----------------|----------|
| Quick Recon | `subdomain_enum` → `tech_detect` → `screenshot` | Fast attack surface overview |
| Full Scan | `subdomain_enum` → `port_scan` → `tech_detect` → `screenshot` → `vuln_scan` | Comprehensive assessment |
| Web App Scan | `subdomain_enum` → `tech_detect` → `vuln_scan` → `screenshot` | Web application focus |
| Compliance Check | `subdomain_enum` → `port_scan` → `vuln_scan` → `compliance_check` | SOC/NIST readiness |
| Continuous Monitor | `subdomain_enum` → `tech_detect` | Lightweight ongoing monitoring (designed for scheduled use) |

System templates are seeded at tenant database creation. They are marked `is_system = true` and **cannot be modified or deleted** by tenants.

#### Step Dependency Map

Steps may depend on the output of previous steps. If a dependency step fails, dependent steps are skipped and credits refunded (per §4.7).

| Step | Depends On | Output |
|------|-----------|--------|
| `subdomain_enum` | None (root step) | List of discovered subdomains |
| `port_scan` | `subdomain_enum` | Open ports per subdomain |
| `tech_detect` | `subdomain_enum` | Technologies per subdomain |
| `screenshot` | `subdomain_enum` | Screenshot images per subdomain |
| `vuln_scan` | `subdomain_enum` | Vulnerabilities per subdomain |
| `compliance_check` | `vuln_scan` | Compliance control results |
| `shodan_lookup` | `subdomain_enum` | Shodan enrichment data |
| `securitytrails_lookup` | None (uses root domain) | DNS history, additional subdomains |
| `censys_lookup` | `subdomain_enum` | Certificate and host data |
| `custom_connector` | Configurable | Custom API results |

#### Custom Workflow Creation

```
BR-SCAN-015: Create Custom Workflow
────────────────────────────────────
Input: tenant_id, name, steps[], source_template_id (optional)

1. CHECK feature flag: custom_workflows
   IF NOT enabled for tenant's plan → REJECT "ERR_SCAN_007"

2. VALIDATE step count:
   IF steps.length < 1 → REJECT "ERR_SCAN_008"
   IF steps.length > 15 → REJECT "ERR_SCAN_009"

3. VALIDATE step types:
   FOR EACH step IN steps:
     IF step.check_type NOT IN known_check_types → REJECT "ERR_SCAN_010"

4. CHECK workflow limit:
   COUNT workflows WHERE tenant = tenant_id AND template_id IS NOT NULL
   IF count >= 20 → REJECT "ERR_SCAN_011"

5. INSERT workflow:
   {
     name: name,
     template_id: source_template_id,
     steps_json: serialize(steps),
     created_by: user_id,
     created_at: now(),
     updated_at: now()
   }

6. AUDIT_LOG("scan.workflow_created", tenant_id, { name, step_count: steps.length })
```

#### Workflow Constraints

| Constraint | Detail |
|-----------|--------|
| System templates | 5 predefined. Cannot be modified or deleted by tenants. |
| Custom workflows | Pro+ tiers only. Max 20 per tenant. |
| Steps per workflow | Min: 1. Max: 15. |
| Step execution | Strictly sequential in defined order (§4.6). |
| Step data flow | Output of each step is available to subsequent steps via `PreviousStepResults`. |
| Duplicate steps | Allowed. A workflow can include the same check type multiple times (e.g., with different parameters). |
| Workflow deletion | Allowed if no active scan jobs reference it. Schedules referencing it must be disabled first. |
| Workflow editing | Steps can be updated. Changes apply to future scans only. In-progress scans use the snapshot from `scan_jobs.steps_json`. |

---

### 4.5 Scan Job Creation & Queuing

> Covers: BR-SCAN-005, BR-SCAN-006

#### Scan Creation Algorithm

```
BR-SCAN-005/006: Create Scan Job
─────────────────────────────────
Input: tenant_id, domain_id, workflow_id, user_id

1. LOAD domain WHERE id = domain_id AND tenant = tenant_id
   IF NOT found → REJECT "ERR_SCAN_005"

2. LOAD workflow WHERE id = workflow_id
   IF NOT found → REJECT "ERR_SCAN_012"

3. CHECK one-active-scan-per-domain:
   LOAD scan_jobs WHERE domain_id = domain_id AND status IN ("QUEUED", "RUNNING")
   IF any exist → REJECT "ERR_SCAN_013"

4. CHECK concurrent scan limit:
   COUNT scan_jobs WHERE tenant = tenant_id AND status IN ("QUEUED", "RUNNING")
   LOAD max_concurrent for tenant's plan:
     Starter: 1
     Pro: 3
     Enterprise: tenant-specific override OR default 10
   IF count >= max_concurrent → REJECT "ERR_SCAN_014"

5. FILTER workflow steps by feature flags:
   FOR EACH step IN workflow.steps:
     IF check_type NOT enabled for tenant's plan → REMOVE step, note as skipped
   IF no steps remain after filtering → REJECT "ERR_SCAN_015"

6. PERFORM credit check and deduction (BR-BILL-011/012 from §3.6):
   Calculate required credits for remaining steps
   IF insufficient → REJECT "ERR_BILL_007"
   DEDUCT credits

7. SNAPSHOT pricing:
   FOR EACH step, record credits_per_domain at current pricing
   Store in steps_json (immutable for refund calculations)

8. INSERT scan_job:
   {
     domain_id,
     workflow_id,
     status: "QUEUED",
     steps_json: serialized steps with pricing snapshot,
     total_credits: total deducted,
     current_step: null,
     created_by: user_id,
     created_at: now()
   }

9. PUSH to Redis queue:
   ScanJobMessage {
     scan_job_id,
     tenant_id,
     tenant_connection_string,
     domain: domain.domain,
     steps: filtered_steps
   }

10. AUDIT_LOG("scan.job_created", tenant_id, {
      scan_job_id, domain: domain.domain, workflow: workflow.name,
      steps: step_count, credits: total_credits
    })

11. RETURN { scan_job_id, status: "QUEUED", estimated_steps: step_count }
```

#### Concurrent Scan Limits

| Tier | Max Concurrent Scans | Configurable |
|------|---------------------|-------------|
| Starter | 1 | No |
| Pro | 3 | No |
| Enterprise | 10 (default) | Yes — super admin can override per tenant |

#### One-Active-Scan-Per-Domain Rule

Regardless of the tier's concurrent limit, only **one scan can run against a specific domain at a time**. This prevents duplicate discovery work and conflicting writes to the same domain's data.

| Scenario | Behavior |
|----------|----------|
| Tenant (Pro) starts scan on `example.com` | Allowed if no active scan on `example.com`. |
| Same tenant starts second scan on `example.com` | Rejected with `ERR_SCAN_013`. Must wait for first to complete or cancel it. |
| Same tenant starts scan on `other.com` | Allowed if concurrent limit not reached. |
| Scheduled scan fires while manual scan running on same domain | Scheduled execution skipped. Next scheduled run will attempt again. |

#### Queue Behavior

| Aspect | Detail |
|--------|--------|
| Queue technology | Redis stream/list. |
| Ordering | FIFO. Jobs processed in creation order. |
| Worker pickup | Worker atomically pops from queue. No double-processing. |
| Queue timeout | If a job sits in queue for >1 hour without pickup, mark as `FAILED` and refund all credits. Log alert for super admin. |
| Worker crash | If worker crashes mid-job, scan remains `RUNNING`. Health check job detects stale `RUNNING` scans (no progress for 30 min) and marks as `FAILED` with refund. |

---

### 4.6 Scan Execution Pipeline

> Covers: BR-SCAN-012, BR-SCAN-016

#### Execution Algorithm

```
BR-SCAN-016: Scan Step Execution
─────────────────────────────────
Input: ScanJobMessage (from Redis queue)

1. SET scan_job.status = "RUNNING"
   SET scan_job.started_at = now()

2. INITIALIZE previous_step_results = {}
   INITIALIZE completed_steps = 0
   INITIALIZE failed_steps = 0

3. FOR EACH step IN scan_job.steps (in order):

   a. SET scan_job.current_step = step.index

   b. CHECK cancellation flag:
      IF scan_job marked for cancellation → GOTO cancellation handler

   c. CHECK step dependency:
      IF step depends on a previous step that failed/was skipped:
        MARK step as SKIPPED
        INCREMENT failed_steps
        REFUND step credits (BR-BILL-013)
        CONTINUE to next step

   d. EXECUTE step:
      context = {
        scan_job_id,
        domain,
        tenant_db,
        parameters: step.parameters,
        previous_step_results
      }
      result = step_executor.ExecuteAsync(context, cancellation_token)

   e. ON STEP SUCCESS:
      WRITE results to tenant DB (§4.10)
      MERGE results into previous_step_results
      INCREMENT completed_steps

   f. ON STEP FAILURE:
      INVOKE retry logic (§4.7)
      IF still failed after retries:
        MARK step as FAILED
        INCREMENT failed_steps
        REFUND step credits (BR-BILL-013)
        CONTINUE to next step

4. DETERMINE final status:
   IF completed_steps == total_steps → SET status = "COMPLETED"
   ELSE IF completed_steps > 0 → SET status = "PARTIAL"
   ELSE → SET status = "FAILED"

5. SET scan_job.completed_at = now()

6. IF status IN ("COMPLETED", "PARTIAL"):
   TRIGGER compliance mapping (if compliance_check step was included)
   TRIGGER notifications (if configured)

7. AUDIT_LOG("scan.job_completed", tenant_id, {
     scan_job_id, status, completed_steps, failed_steps, duration
   })
```

#### Step Data Flow

Each step receives the accumulated results from all previous steps via `previous_step_results`. This is how output feeds into subsequent steps.

```
Step 1 (subdomain_enum)
  → outputs: { subdomains: ["api.example.com", "www.example.com", ...] }

Step 2 (port_scan)
  ← receives: { subdomains: [...] }
  → outputs: { subdomains: [...], ports: { "api.example.com": [80, 443, 8080], ... } }

Step 3 (tech_detect)
  ← receives: { subdomains: [...], ports: {...} }
  → outputs: { subdomains: [...], ports: {...}, technologies: {...} }

...and so on
```

#### Results Written Per Step

| Step | Writes To |
|------|----------|
| `subdomain_enum` | `subdomains` table |
| `port_scan` | `ports` table |
| `tech_detect` | `technologies` table |
| `screenshot` | `screenshots` table + file storage |
| `vuln_scan` | `scan_results` + `vulnerabilities` tables |
| `compliance_check` | `compliance_assessments` + `control_results` tables |
| `shodan_lookup` | `scan_results` table (enrichment data in `data_json`) |
| `securitytrails_lookup` | `subdomains` table (additional discoveries) + `scan_results` |
| `censys_lookup` | `scan_results` table (certificate/host data in `data_json`) |
| `custom_connector` | `scan_results` table (custom data in `data_json`) |

All steps also write a `scan_results` record with the step's `check_type` and full result `data_json`.

---

### 4.7 Step Retry & Timeout

> Covers: BR-SCAN-009, BR-SCAN-010

#### Step Retry Algorithm

```
BR-SCAN-010: Step Retry on Failure
───────────────────────────────────
Input: step, context, max_retries = 2

1. attempt = 0

2. LOOP:
   attempt += 1
   TRY:
     result = step_executor.ExecuteAsync(context, cancellation_token)
     RETURN result (success)
   CATCH error:
     IF attempt > max_retries (total 3 attempts):
       LOG("scan.step_failed", { step: step.check_type, attempts: attempt, error })
       RETURN failure
     WAIT backoff:
       attempt 1 → 5 seconds
       attempt 2 → 15 seconds
     LOG("scan.step_retry", { step: step.check_type, attempt, error })
     GOTO LOOP
```

#### Retry Constraints

| Constraint | Value |
|-----------|-------|
| Max retries per step | 2 (3 total attempts) |
| Backoff strategy | Fixed: 5s, 15s |
| Retryable failures | Network errors, tool process crashes, transient API failures |
| Non-retryable failures | Invalid configuration, missing tool binary, authentication errors |
| Retry scope | Only the failed step is retried. Previous step results are preserved. |

#### Timeout Rules

```
BR-SCAN-009: Scan Timeouts
───────────────────────────
STEP TIMEOUT:
1. Each step execution has a 30-minute hard timeout
2. IF step exceeds 30 minutes:
   CANCEL step via cancellation_token
   MARK step as FAILED (timeout)
   Do NOT retry timed-out steps
   REFUND step credits
   CONTINUE to next step (if independent)
   LOG("scan.step_timeout", { step: step.check_type, duration: "30m" })

SCAN TIMEOUT:
1. Entire scan job has a 4-hour hard timeout
2. IF scan exceeds 4 hours:
   CANCEL current step via cancellation_token
   MARK scan as CANCELLED with reason "timeout"
   REFUND credits for current step + all remaining steps
   PRESERVE all results from completed steps
   LOG("scan.job_timeout", { scan_job_id, duration: "4h", completed_steps })
```

#### Timeout Constants

| Timeout | Value | Configurable |
|---------|-------|-------------|
| Step timeout | 30 minutes | No. Fixed system constant. |
| Scan timeout | 4 hours | No. Fixed system constant. |
| Queue timeout | 1 hour | No. Job in queue >1 hour without pickup → `FAILED`. |
| Stale scan detection | 30 minutes | No. Running scan with no step progress for 30 min → `FAILED`. |

#### Failure Resolution Matrix

| Failure Type | Retry | Continue to Next Step | Credits |
|-------------|-------|----------------------|---------|
| Step transient error | Yes (up to 2 retries) | Yes, if next step is independent | Refund failed step |
| Step timeout (30 min) | No | Yes, if next step is independent | Refund failed step |
| Scan timeout (4 hours) | No | No, scan terminates | Refund current + remaining steps |
| Tool binary missing | No (non-retryable) | Yes, if next step is independent | Refund failed step |
| Dependency step failed | No (skipped) | Skipped automatically | Refund skipped step |
| Worker crash | No | No, detected by health check | Refund all steps |

---

### 4.8 Scan Cancellation

> Covers: BR-SCAN-008

#### Tenant-Initiated Cancellation

```
BR-SCAN-008: Cancel Scan (Tenant)
─────────────────────────────────
Input: tenant_id, scan_job_id, user_id

1. LOAD scan_job WHERE id = scan_job_id AND tenant = tenant_id
   IF NOT found → REJECT "ERR_SCAN_005"

2. IF scan_job.status NOT IN ("QUEUED", "RUNNING") → REJECT "ERR_SCAN_016"

3. IF scan_job.status == "QUEUED":
   REMOVE job from Redis queue
   SET scan_job.status = "CANCELLED"
   SET scan_job.cancelled_by = user_id
   SET scan_job.cancellation_reason = "user_requested"
   SET scan_job.completed_at = now()
   REFUND all step credits (full refund — nothing executed)

4. IF scan_job.status == "RUNNING":
   SET cancellation flag (checked between steps)
   Worker completes current step, then stops
   SET scan_job.status = "CANCELLED"
   SET scan_job.cancelled_by = user_id
   SET scan_job.cancellation_reason = "user_requested"
   SET scan_job.completed_at = now()
   REFUND credits for unexecuted steps only
   Completed step results are preserved

5. AUDIT_LOG("scan.job_cancelled", tenant_id, {
     scan_job_id,
     reason: "user_requested",
     cancelled_by: user_id,
     steps_completed: completed_count,
     credits_refunded: refund_amount
   })
```

#### System-Initiated Cancellation

| Trigger | Reason | Behavior |
|---------|--------|----------|
| Tenant suspension (BR-TNT-006) | `tenant_suspended` | All `QUEUED` and `RUNNING` scans cancelled. Partial results preserved. Unexecuted step credits refunded. |
| Scan timeout (BR-SCAN-009) | `timeout` | Current step cancelled. All remaining steps skipped. Partial results preserved. |
| Worker crash (health check) | `worker_failure` | Scan marked `FAILED`. All unexecuted step credits refunded. |
| Queue timeout (1 hour) | `queue_timeout` | Job never picked up. Full credit refund. |

#### Cancellation Refund Rules

| Status at Cancellation | Completed Steps | Refunded |
|----------------------|----------------|----------|
| `QUEUED` | 0 | 100% — full credit refund |
| `RUNNING`, step 1 of 5 in progress | 0 | Steps 1–5 refunded (current step included since not completed) |
| `RUNNING`, step 3 of 5 just completed | 3 | Steps 4–5 refunded. Steps 1–3 not refunded. |
| `RUNNING`, step 3 of 5 in progress | 2 | Steps 3–5 refunded (current step included). Steps 1–2 not refunded. |

#### Cancellation Flag Mechanism

The cancellation flag is a shared state (Redis key or database column) checked by the worker **between steps** — not mid-step. This ensures:
- The current step always completes cleanly (no partial writes)
- Results from the current step are persisted before cancellation
- No data corruption from mid-operation termination

```
Redis key: "scan:cancel:{scan_job_id}" = "1"
Worker checks this key before starting each new step.
```

---

### 4.9 Feature Flag Enforcement

> Covers: BR-SCAN-011

#### Per-Step Feature Check

```
BR-SCAN-011: Feature Flag Enforcement in Scans
────────────────────────────────────────────────
Enforced at: Two points — scan creation (§4.5) and scan execution (§4.6)

AT SCAN CREATION:
1. FOR EACH step IN workflow.steps:
   LOAD feature flag for step.check_type
   EVALUATE: plan_features + tenant_feature_overrides (see §5)
   IF NOT enabled:
     REMOVE step from execution list
     LOG("scan.step_filtered", { check_type: step.check_type, reason: "feature_disabled" })

2. IF no steps remain after filtering → REJECT "ERR_SCAN_015"
3. Calculate credits only for remaining (enabled) steps

AT SCAN EXECUTION:
1. BEFORE each step, re-verify feature flag
   (Guards against flag changes between creation and execution)
   IF flag was disabled after creation:
     SKIP step
     REFUND step credits
     LOG("scan.step_skipped_runtime", { check_type, reason: "feature_disabled_after_creation" })
```

#### Check Type to Feature Flag Mapping

| Check Type | Feature Flag | Available Tiers |
|-----------|-------------|-----------------|
| `subdomain_enum` | `subdomain_enumeration` | Starter, Pro, Enterprise |
| `port_scan` | `port_scanning` | Starter, Pro, Enterprise |
| `tech_detect` | `tech_detection` | Starter, Pro, Enterprise |
| `screenshot` | `screenshot_capture` | Starter, Pro, Enterprise |
| `vuln_scan` | `vulnerability_scanning` | Pro, Enterprise |
| `compliance_check` | `compliance_reports` | Pro, Enterprise |
| `shodan_lookup` | `shodan_integration` | Pro, Enterprise |
| `securitytrails_lookup` | `securitytrails_integration` | Pro, Enterprise |
| `censys_lookup` | `censys_integration` | Enterprise |
| `custom_connector` | `custom_api_connectors` | Enterprise |

#### Override Behavior

Super admin can override feature flags per tenant (see §5 Feature Flags). If a super admin enables `vulnerability_scanning` for a Starter tenant, that tenant can include `vuln_scan` steps in workflows and scans.

#### Edge Cases

| Scenario | Behavior |
|----------|----------|
| Tenant selects Full Scan template on Starter tier | `vuln_scan` and `compliance_check` steps filtered out at creation. Scan runs with remaining steps. Credits calculated for enabled steps only. |
| Super admin disables a feature flag mid-scan | Step is skipped at runtime. Credits refunded for that step. |
| All steps in a workflow are disabled for tenant's tier | Scan creation rejected with `ERR_SCAN_015`. |
| Tenant upgrades mid-scan | Already-filtered steps remain excluded. New capabilities apply to future scans only. |

---

### 4.10 Results Persistence & Immutability

> Covers: BR-SCAN-012, BR-SCAN-013

#### Write-After-Each-Step

```
BR-SCAN-012: Results Persistence
────────────────────────────────
Rule: Results are written to the tenant DB after EACH step completes successfully.

1. Step completes → results written immediately
2. Even if subsequent steps fail, earlier results are preserved
3. Each step writes to its relevant tables (see §4.6 Results Written Per Step)
4. Each step also writes a scan_results record with full data_json

Write order within a step:
  a. Write entity records (subdomains, ports, technologies, etc.)
  b. Write scan_results record linking results to scan_job
  c. Write vulnerabilities records (if vuln_scan step)
  d. COMMIT transaction

If write fails:
  Retry write once
  If still fails: mark step as FAILED, refund credits
  Previously written entity records from this step are rolled back (transaction)
```

#### Immutability Rules

```
BR-SCAN-013: Scan Results Immutability
───────────────────────────────────────
1. Scan results are APPEND-ONLY
   New scans add new records — they do NOT update or delete previous scan results

2. Tenants CANNOT:
   - Modify scan results
   - Delete individual scan results
   - Delete scan jobs
   - Alter severity ratings
   - Edit vulnerability descriptions

3. Super admin CAN:
   - Purge all scan data for a tenant (as part of tenant deletion per §2.9)
   - No selective deletion of individual results

4. Data updates across scans:
   - Subdomains: first_seen is immutable, last_seen updated on rediscovery
   - Ports: new scan creates new port records (associated with new scan_job)
   - Technologies: new scan creates new technology records
   - Vulnerabilities: each scan produces independent findings
```

#### Data Retention by Tier

| Tier | Scan Results Retained |
|------|----------------------|
| Starter | 30 days |
| Pro | 90 days |
| Enterprise | 1 year |

#### Data Retention Cleanup

```
BR-SCAN-013: Scan Data Cleanup
──────────────────────────────
Runs: Daily background job (per tenant)

1. LOAD tenant's plan data retention period

2. FIND scan_jobs WHERE completed_at < (now() - retention_period)
   AND status IN ("COMPLETED", "PARTIAL", "FAILED", "CANCELLED")

3. FOR EACH expired scan_job:
   DELETE scan_results WHERE scan_job_id = scan_job.id
   DELETE vulnerabilities WHERE scan_result_id IN (expired results)
   DELETE scan_job record

4. NOTE: Entity records (subdomains, ports, technologies, screenshots)
   are NOT deleted by retention cleanup — they represent the tenant's
   current asset inventory. Only scan job results are rotated.

5. AUDIT_LOG("scan.data_cleanup", tenant_id, {
     jobs_deleted: count,
     retention_days: retention_period
   })
```

#### Immutability Exceptions

| Exception | Detail |
|-----------|--------|
| `subdomains.last_seen` | Updated on rediscovery. Not a result modification — it's asset tracking. |
| Scan data retention cleanup | Expired results are deleted per tier policy. This is automated, not user-initiated. |
| Tenant deletion | All data purged as part of tenant deactivation (§2.9). |

---

### 4.11 Scheduled Scans

> Covers: BR-SCAN-018, BR-SCAN-019, BR-SCAN-020, BR-SCAN-021

#### Scheduling Availability

| Tier | Scheduled Scans |
|------|----------------|
| Free | No |
| Starter | No |
| Pro | Yes |
| Enterprise | Yes |

#### Create Schedule

```
BR-SCAN-018/019/020: Create Scan Schedule
──────────────────────────────────────────
Input: tenant_id, domain_id, workflow_id, cron_expression, user_id

1. CHECK feature flag: scheduled_scans
   IF NOT enabled for tenant's plan → REJECT "ERR_SCAN_017"

2. LOAD domain WHERE id = domain_id AND tenant = tenant_id
   IF NOT found → REJECT "ERR_SCAN_005"

3. LOAD workflow WHERE id = workflow_id
   IF NOT found → REJECT "ERR_SCAN_012"

4. VALIDATE cron_expression:
   IF NOT valid cron format → REJECT "ERR_SCAN_018"

5. CHECK minimum interval (BR-SCAN-019):
   PARSE cron_expression to determine frequency
   IF interval < 24 hours → REJECT "ERR_SCAN_019"

6. CHECK schedule limit (BR-SCAN-020):
   COUNT scan_schedules WHERE tenant = tenant_id AND enabled = true
   IF count >= 10 → REJECT "ERR_SCAN_020"

7. PERFORM credit pre-check (BR-BILL-017 from §3.11):
   Calculate estimated credits for one execution
   IF insufficient → REJECT "ERR_BILL_007"

8. INSERT scan_schedule:
   {
     domain_id,
     workflow_id,
     cron_expression,
     enabled: true,
     estimated_credits,
     next_run_at: calculate_next_run(cron_expression),
     created_by: user_id,
     created_at: now()
   }

9. AUDIT_LOG("scan.schedule_created", tenant_id, {
     schedule_id, domain: domain.domain, cron: cron_expression
   })
```

#### Schedule Execution

```
BR-SCAN-018: Scheduled Scan Execution
──────────────────────────────────────
Trigger: Background job checks scan_schedules WHERE next_run_at <= now() AND enabled = true

1. LOAD scan_schedule
   IF NOT enabled → SKIP

2. CHECK tenant status:
   IF tenant.status != "ACTIVE" → SKIP

3. CHECK feature flag: scheduled_scans
   IF NOT enabled → SKIP (plan may have been downgraded)

4. CHECK one-active-scan-per-domain (BR-SCAN-006):
   IF domain has active scan → SKIP this execution
   SET next_run_at to next cron occurrence
   LOG("scan.schedule_skipped", { reason: "domain_busy" })

5. PERFORM credit check (BR-BILL-017 from §3.11):
   Calculate required credits at current pricing
   IF insufficient → SKIP, notify tenant
   SET next_run_at to next cron occurrence

6. CREATE scan job via standard flow (§4.5)
   All validations (concurrent limits, feature flags, credits) apply

7. UPDATE scan_schedule:
   SET last_run_at = now()
   SET next_run_at = calculate_next_run(cron_expression)

8. AUDIT_LOG("scan.schedule_executed", tenant_id, { schedule_id, scan_job_id })
```

#### Auto-Disable Rules (BR-SCAN-021)

| Event | Effect |
|-------|--------|
| Tenant suspended (BR-TNT-006) | All schedules set `enabled = false`. |
| Tenant reactivated (BR-TNT-007) | Schedules remain disabled. Tenant must re-enable manually. |
| Subscription expired / downgraded to free (BR-TNT-011) | All schedules set `enabled = false`. |
| Plan downgraded from Pro to Starter | All schedules set `enabled = false` (Starter doesn't support scheduling). |
| Domain deleted | Associated schedules set `enabled = false`. |
| Re-upgrade to Pro/Enterprise | Schedules remain disabled. Tenant must re-enable manually. |

#### Schedule Constraints

| Constraint | Value |
|-----------|-------|
| Minimum interval | 24 hours. No sub-daily scheduling. |
| Max active schedules per tenant | 10 |
| Available tiers | Pro, Enterprise |
| Credit handling | Soft reservation — pre-check at creation, real check at execution (§3.11) |
| Missed execution | If system is down when schedule fires, the missed execution is skipped. Next occurrence runs as scheduled. No backfill. |
| Schedule editing | Can update cron_expression and workflow_id. Validations re-run. |
| Disable/enable | Tenant can manually enable/disable. Counts toward the 10-schedule limit only when enabled. |

---

### 4.12 Permissions Matrix

> Cross-cutting permissions for scanning & workflows

| Action | `TENANT_OWNER` | `SUPER_ADMIN` |
|--------|:-:|:-:|
| Add domain | Yes (own tenant) | No (tenant self-service) |
| Delete domain | Yes (own tenant) | No (tenant self-service) |
| View domains | Yes (own tenant) | Yes (any tenant, via impersonation) |
| List workflow templates | Yes | Yes |
| Create custom workflow | Yes (if plan allows) | No (tenant self-service) |
| Edit custom workflow | Yes (own tenant) | No |
| Delete custom workflow | Yes (own tenant) | No |
| Start scan | Yes (own tenant) | No (tenant self-service) |
| Cancel scan | Yes (own tenant) | No (use suspension to cancel all) |
| View scan results | Yes (own tenant) | Yes (any tenant, via impersonation) |
| Delete scan results | No | No (only via tenant deletion) |
| Purge scan data | No | Only via tenant deletion (§2.9) |
| Create scan schedule | Yes (if plan allows) | No (tenant self-service) |
| Edit scan schedule | Yes (own tenant) | No |
| Enable/disable schedule | Yes (own tenant) | System auto-disables on suspension/downgrade |
| Delete scan schedule | Yes (own tenant) | No |
| Configure concurrent scan limit (Enterprise) | No | Yes (per-tenant override) |
| Configure scan step pricing | No | Yes (§3.10) |

#### Notes

- Super admins do not directly manage tenant scans. They manage the platform (pricing, feature flags, concurrent limits).
- Super admins can view tenant scan data via impersonation (§2.10), which creates a full audit trail.
- Scan result deletion is not available to anyone directly. Results are removed only through data retention cleanup or tenant deletion.

---

### 4.13 Edge Cases

> Cross-cutting edge cases for scanning & workflows

| Scenario | Behavior |
|----------|----------|
| Tenant adds domain `Example.COM` | Stored as `example.com`. Uniqueness check case-insensitive. |
| Tenant adds `https://example.com/path` | Rejected with `ERR_SCAN_001`. Must be bare domain only. |
| Tenant adds `api.example.com` (subdomain) | Rejected with `ERR_SCAN_002`. Subdomains are discovered by scans, not added manually. |
| Tenant at domain limit adds another domain | Rejected with `ERR_SCAN_004`. Must delete an existing domain or upgrade plan. |
| Tenant deletes domain with a `QUEUED` scan | Rejected with `ERR_SCAN_006`. Must cancel the scan first, then delete. |
| Tenant starts Full Scan on Starter tier | `vuln_scan` and `compliance_check` steps filtered out by feature flags. Remaining steps execute. Credits calculated for enabled steps only. |
| All steps in a workflow are disabled for tenant's tier | Scan creation rejected with `ERR_SCAN_015`. |
| Tenant hits concurrent scan limit | New scan rejected with `ERR_SCAN_014`. Must wait for a running scan to complete or cancel one. |
| Two scans started simultaneously on the same domain | First to insert `scan_jobs` record succeeds. Second rejected with `ERR_SCAN_013` (one-active-scan-per-domain). Database unique constraint or check prevents race condition. |
| Scan step fails on first attempt, succeeds on retry | Retry is transparent. Step is marked successful. No credits refunded. |
| Scan step fails after all 3 attempts | Step marked as failed. Credits for this step refunded. Next independent step continues. Dependent steps skipped and refunded. |
| `subdomain_enum` fails (root step) | All subsequent steps depend on it. All skipped and refunded. Scan marked `FAILED`. |
| `port_scan` fails but `screenshot` and `tech_detect` follow | `screenshot` and `tech_detect` depend on `subdomain_enum` (not `port_scan`). They continue. Scan marked `PARTIAL`. |
| Step times out at 30 minutes | Step cancelled and marked failed. No retry for timeouts. Credits refunded. Next independent step continues. |
| Scan reaches 4-hour timeout mid-step | Current step cancelled. Scan marked `CANCELLED`. All remaining step credits refunded. Completed results preserved. |
| Worker crashes mid-scan | Health check detects no progress for 30 minutes. Scan marked `FAILED`. All unexecuted step credits refunded. Completed results preserved. |
| Job sits in Redis queue for >1 hour | Queue timeout. Scan marked `FAILED`. Full credit refund. Super admin alerted. |
| Scheduled scan fires but domain has active scan | Execution skipped silently. `next_run_at` updated to next cron occurrence. No notification (expected behavior for overlapping schedules). |
| Scheduled scan fires but tenant is on free tier | Execution skipped. Schedule remains enabled but non-functional until tenant upgrades. |
| Tenant creates 10 schedules, disables 2, tries to create another | Allowed. Limit counts only enabled schedules. New schedule is the 9th enabled. |
| Tenant sets cron to run every hour | Rejected with `ERR_SCAN_019`. Minimum interval is 24 hours. |
| Workflow edited while a scan using it is running | Running scan uses the `steps_json` snapshot from `scan_jobs`. Not affected by workflow edits. |
| Workflow deleted while a schedule references it | Deletion blocked. Schedule must be disabled/deleted first. |
| Custom workflow with duplicate steps (e.g., two `subdomain_enum`) | Allowed. Both steps execute sequentially. Credits charged for each. Second run may discover additional subdomains from different sources/parameters. |
| Scan results written but subsequent DB commit fails | Transaction rolled back for that step. Step marked failed. Credits refunded. Previously committed steps unaffected. |
| Tenant downgrades from Pro to Starter with custom workflows | Custom workflows preserved but tenant cannot create new ones or edit existing. Existing workflows can still be used for manual scans (feature-flag-filtered at execution). Schedules auto-disabled. |
| Pricing changes between scan creation and refund | Refund uses the pricing snapshot from `scan_jobs.steps_json`, not current pricing. Tenant always gets back what was charged. |

---

### 4.14 Error Codes

> All `ERR_SCAN_*` error codes for scanning & workflows

| Code | HTTP | Message | Cause |
|------|------|---------|-------|
| `ERR_SCAN_001` | 400 | Invalid domain format | Domain contains IP, URL, protocol, path, or invalid characters |
| `ERR_SCAN_002` | 400 | Subdomains cannot be added directly | Input is a subdomain (e.g., `api.example.com`). Subdomains are discovered by scans. |
| `ERR_SCAN_003` | 409 | Domain already exists | Duplicate domain within the same tenant (case-insensitive) |
| `ERR_SCAN_004` | 403 | Domain limit reached for your plan | Tenant at `max_domains` for their subscription tier |
| `ERR_SCAN_005` | 404 | Domain not found | Domain ID does not exist in tenant's database |
| `ERR_SCAN_006` | 409 | Cannot delete domain with active scans | Domain has `QUEUED` or `RUNNING` scan jobs |
| `ERR_SCAN_007` | 403 | Custom workflows require a Pro or Enterprise plan | Tenant on Starter/Free tier attempted custom workflow creation |
| `ERR_SCAN_008` | 400 | Workflow must have at least one step | Empty steps array provided |
| `ERR_SCAN_009` | 400 | Workflow cannot exceed 15 steps | More than 15 steps in workflow definition |
| `ERR_SCAN_010` | 400 | Unknown scan step type | `check_type` not recognized by the system |
| `ERR_SCAN_011` | 403 | Custom workflow limit reached (max 20) | Tenant has 20 custom workflows |
| `ERR_SCAN_012` | 404 | Workflow not found | Workflow ID does not exist in tenant's database |
| `ERR_SCAN_013` | 409 | A scan is already running on this domain | One-active-scan-per-domain rule violated |
| `ERR_SCAN_014` | 429 | Concurrent scan limit reached | Tenant at max concurrent scans for their tier |
| `ERR_SCAN_015` | 400 | No scan steps available for your plan | All workflow steps filtered out by feature flags |
| `ERR_SCAN_016` | 409 | Scan cannot be cancelled in its current state | Cancel attempted on `COMPLETED`, `PARTIAL`, `FAILED`, or already `CANCELLED` scan |
| `ERR_SCAN_017` | 403 | Scheduled scans require a Pro or Enterprise plan | Starter/Free tier attempted to create a schedule |
| `ERR_SCAN_018` | 400 | Invalid cron expression | Cron format could not be parsed |
| `ERR_SCAN_019` | 400 | Minimum schedule interval is 24 hours | Cron expression resolves to sub-daily frequency |
| `ERR_SCAN_020` | 403 | Schedule limit reached (max 10 active) | Tenant has 10 enabled schedules |

---
