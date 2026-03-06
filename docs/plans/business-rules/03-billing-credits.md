# 3. Billing & Credits

> Covers: BR-BILL-001 through BR-BILL-018 from the original business rules.
> Error response JSON schema is defined in `00-index.md` Section 12 reference.

### 3.1 Subscription Plan Tiers

> Covers: BR-BILL-001, BR-BILL-002

#### Plan Definitions

| Feature | Free | Starter | Pro | Enterprise |
|---------|------|---------|-----|------------|
| Domains | 0 (read-only) | 3 | 20 | Unlimited |
| Monthly credits | 0 | 100 | 500 | Custom |
| Subdomain enumeration | No | Yes | Yes | Yes |
| Port scanning | No | Yes | Yes | Yes |
| Vulnerability scanning | No | No | Yes | Yes |
| Compliance reports | No | No | Yes | Yes |
| Shodan integration | No | No | Yes | Yes |
| SecurityTrails integration | No | No | Yes | Yes |
| Censys integration | No | No | No | Yes |
| Custom API connectors | No | No | No | Yes |
| Custom workflows | No | No | Yes | Yes |
| Scheduled scans | No | No | Yes | Yes |
| Notifications | None | Email | Slack + Email | All |
| Data retention | N/A | 30 days | 90 days | 1 year |
| Billing | None | Monthly/Annual | Monthly/Annual | Custom |

#### Free Tier

The free tier is **not a subscription plan** — it's the default state when no active subscription exists (see §2.7 in Tenant Management). There is no free trial. Users must subscribe to a paid plan to run scans.

#### Plan Status Values

| Status | Meaning |
|--------|---------|
| `ACTIVE` | Plan is available for new subscriptions. |
| `DEPRECATED` | Plan is no longer offered to new subscribers. Existing subscribers remain until they change plans. |
| `ARCHIVED` | Plan is fully retired. No subscribers remain. Kept for billing history references. |

---

### 3.2 Field Constraints

> Covers: BR-BILL-001, BR-BILL-008, BR-BILL-009, BR-BILL-014, BR-BILL-015

#### `subscription_plans` Table

| Field | Type | Constraints |
|-------|------|------------|
| `id` | UUID | System-generated. Immutable. Primary key. |
| `name` | string | Required. Max 100 chars. Unique. E.g., `Starter`, `Pro`, `Enterprise`. |
| `stripe_price_id` | string | Nullable. Stripe Price ID for monthly billing. Null for custom/enterprise plans. |
| `monthly_credits` | int | Required. Credits allotted per billing period. Min: 0. |
| `max_domains` | int | Required. Max domains allowed. Use `-1` for unlimited (Enterprise). |
| `price_monthly` | decimal | Required. Monthly price in USD. Precision: 10,2. |
| `price_annual` | decimal | Required. Annual price in USD. Precision: 10,2. Discounted compared to 12x monthly. |
| `features_json` | string | Required. Default: `{}`. JSON object mapping feature flag names to enabled/disabled. Used for feature gating reference. |
| `status` | string | Required. CHECK (`ACTIVE`, `DEPRECATED`, `ARCHIVED`). Default: `ACTIVE`. |
| `created_at` | timestamp | System-generated. Immutable. Default: `NOW()`. |

#### `tenant_subscriptions` Table

| Field | Type | Constraints |
|-------|------|------------|
| `id` | UUID | System-generated. Immutable. Primary key. |
| `tenant_id` | UUID | Required. FK → `tenants.id`. Indexed. |
| `plan_id` | UUID | Required. FK → `subscription_plans.id`. |
| `stripe_subscription_id` | string | Nullable. Stripe Subscription ID. Null for free tier or manually managed subscriptions. |
| `stripe_customer_id` | string | Nullable. Stripe Customer ID. Set on first subscription. Reused across plan changes. |
| `status` | string | Required. CHECK (`ACTIVE`, `PAST_DUE`, `CANCELLED`, `EXPIRED`). Default: `ACTIVE`. |
| `billing_interval` | string | Required. CHECK (`MONTHLY`, `ANNUAL`). Default: `MONTHLY`. |
| `current_period_start` | timestamp | Required. Start of the current billing period. |
| `current_period_end` | timestamp | Nullable. End of the current billing period. Null for free tier. |
| `credits_remaining` | int | Required. Default: 0. Current allotment credit balance. Reset to `monthly_credits` at period start. |
| `credits_used_this_period` | int | Required. Default: 0. Total credits consumed this period (allotment + purchased). Reset to 0 at period start. |
| `pending_plan_id` | UUID | Nullable. FK → `subscription_plans.id`. Set when a downgrade is scheduled for period end. Cleared on apply. |
| `created_at` | timestamp | System-generated. Immutable. Default: `NOW()`. |

#### `credit_transactions` Table

| Field | Type | Constraints |
|-------|------|------------|
| `id` | UUID | System-generated. Immutable. Primary key. |
| `tenant_id` | UUID | Required. FK → `tenants.id`. Indexed. |
| `amount` | int | Required. Positive for credits added, negative for credits consumed. |
| `type` | string | Required. CHECK (`ALLOTMENT`, `CONSUMPTION`, `PURCHASE`, `REFUND`, `ADJUSTMENT`). |
| `scan_job_id` | UUID | Nullable. FK → scan_jobs (tenant DB). Set for `CONSUMPTION` and `REFUND` transactions. |
| `description` | string | Nullable. Max 500 chars. Human-readable reason. Required for `ADJUSTMENT` type. |
| `created_by` | UUID | Nullable. FK → `users.id`. Set for `ADJUSTMENT` type (super admin who made the adjustment). |
| `created_at` | timestamp | System-generated. Immutable. Default: `NOW()`. |

#### `credit_packs` Table

| Field | Type | Constraints |
|-------|------|------------|
| `id` | UUID | System-generated. Immutable. Primary key. |
| `name` | string | Required. Max 100 chars. E.g., `50 Credits`, `200 Credits`. |
| `stripe_price_id` | string | Nullable. Stripe Price ID for one-time payment. |
| `credits` | int | Required. Number of credits in the pack. Min: 1. |
| `price` | decimal | Required. Price in USD. Precision: 10,2. |
| `status` | string | Required. CHECK (`ACTIVE`, `ARCHIVED`). Default: `ACTIVE`. |
| `created_at` | timestamp | System-generated. Immutable. Default: `NOW()`. |

#### `scan_step_pricing` Table

| Field | Type | Constraints |
|-------|------|------------|
| `id` | UUID | System-generated. Immutable. Primary key. |
| `check_type` | string | Required. Max 50 chars. E.g., `subdomain_enum`, `port_scan`, `vuln_scan`. |
| `tier_id` | UUID | Required. FK → `subscription_plans.id`. |
| `credits_per_domain` | int | Required. Credits consumed per domain for this check type on this tier. Min: 0. |
| `description` | string | Nullable. Max 200 chars. |
| `updated_by` | UUID | Nullable. FK → `users.id`. Super admin who last modified. |
| `updated_at` | timestamp | Required. Default: `NOW()`. Updated on modification. |

**Composite unique constraint:** `(check_type, tier_id)` — one price per check type per tier.

---

### 3.3 Subscription Lifecycle

> Covers: BR-BILL-003, BR-BILL-004, BR-BILL-005, BR-BILL-006

#### Subscription States

| Status | Meaning |
|--------|---------|
| `ACTIVE` | Subscription is current and paid. Full plan access. |
| `PAST_DUE` | Payment failed. Stripe is retrying. Access continues during retry window. |
| `CANCELLED` | Tenant cancelled. Access continues until `current_period_end`. |
| `EXPIRED` | Period ended after cancellation, or all Stripe retries failed. Tenant on free tier. |

#### State Transitions

```
                    ┌──────────────┐
   Subscribe        │    ACTIVE    │◄─────────────────┐
  ─────────────────►│              │                   │
                    └──┬───────┬───┘                   │
                       │       │                       │
          Payment      │       │ Tenant cancels     Payment
          fails        │       │                    succeeds
                       ▼       ▼                       │
              ┌────────────┐  ┌──────────────┐         │
              │  PAST_DUE  │──┤  CANCELLED   │         │
              │            │  │              │         │
              └──────┬─────┘  └──────┬───────┘         │
                     │               │                 │
                     │               │ Period ends     │
           All retries               │                 │
           exhausted  │              │                 │
                      ▼              ▼                 │
              ┌──────────────────────────┐             │
              │         EXPIRED          │             │
              │  (downgraded to free)    │─────────────┘
              └──────────────────────────┘  Re-subscribes
```

#### New Subscription

```
BR-BILL-003: Create Subscription
─────────────────────────────────
Input: tenant_id, plan_id, billing_interval (MONTHLY | ANNUAL)

1. LOAD tenant WHERE id = tenant_id
   IF tenant.status != "ACTIVE" → REJECT "ERR_BILL_001"

2. LOAD current tenant_subscription WHERE tenant_id = tenant_id AND status = "ACTIVE"
   IF exists → REJECT "ERR_BILL_002"

3. LOAD plan WHERE id = plan_id
   IF plan.status != "ACTIVE" → REJECT "ERR_BILL_003"

4. CREATE Stripe customer (if not exists):
   IF no stripe_customer_id for tenant → CREATE via Stripe API

5. CREATE Stripe subscription:
   price = IF billing_interval == "ANNUAL" → plan.stripe_price_id (annual)
           ELSE → plan.stripe_price_id (monthly)

6. INSERT tenant_subscription:
   {
     tenant_id,
     plan_id,
     stripe_subscription_id: from Stripe response,
     stripe_customer_id,
     status: "ACTIVE",
     billing_interval,
     current_period_start: now(),
     current_period_end: calculated from interval,
     credits_remaining: plan.monthly_credits,
     credits_used_this_period: 0
   }

7. SET tenant.plan_id = plan_id

8. INSERT credit_transaction:
   { tenant_id, amount: plan.monthly_credits, type: "ALLOTMENT", description: "Initial credit allotment" }

9. SYNC feature flags for tenant based on new plan (see §5 Feature Flags)

10. AUDIT_LOG("billing.subscription_created", tenant_id, { plan: plan.name, interval: billing_interval })
```

#### Plan Upgrade

```
BR-BILL-004: Plan Upgrade
──────────────────────────
Input: tenant_id, new_plan_id

1. LOAD current subscription WHERE tenant_id = tenant_id AND status IN ("ACTIVE", "CANCELLED")
   IF NOT found → REJECT "ERR_BILL_004"

2. LOAD new_plan WHERE id = new_plan_id
   IF new_plan.monthly_credits <= current_plan.monthly_credits → REJECT "ERR_BILL_005"
   (Use downgrade flow for lower plans)

3. UPDATE Stripe subscription to new plan (pro-rated)

4. SET subscription.plan_id = new_plan_id
   SET subscription.status = "ACTIVE" (reactivates if was CANCELLED)
   SET subscription.credits_remaining = new_plan.monthly_credits
   SET subscription.credits_used_this_period = 0

5. SET tenant.plan_id = new_plan_id

6. INSERT credit_transaction:
   { tenant_id, amount: new_plan.monthly_credits, type: "ALLOTMENT", description: "Upgrade allotment: {new_plan.name}" }

7. SYNC feature flags for tenant based on new plan

8. AUDIT_LOG("billing.plan_upgraded", tenant_id, { from: old_plan.name, to: new_plan.name })
```

#### Plan Downgrade

```
BR-BILL-005: Plan Downgrade
────────────────────────────
Input: tenant_id, new_plan_id

1. LOAD current subscription WHERE tenant_id = tenant_id AND status = "ACTIVE"
   IF NOT found → REJECT "ERR_BILL_004"

2. LOAD new_plan WHERE id = new_plan_id
   IF new_plan.monthly_credits >= current_plan.monthly_credits → REJECT "ERR_BILL_006"
   (Use upgrade flow for higher plans)

3. SCHEDULE Stripe subscription change at period end (no immediate pro-ration)

4. STORE pending downgrade:
   SET subscription.pending_plan_id = new_plan_id

5. AUDIT_LOG("billing.downgrade_scheduled", tenant_id, {
     from: current_plan.name,
     to: new_plan.name,
     effective_at: subscription.current_period_end
   })

6. AT PERIOD END (triggered by Stripe webhook or period reset job):
   SET subscription.plan_id = new_plan_id
   SET subscription.pending_plan_id = null
   SET subscription.credits_remaining = new_plan.monthly_credits
   SET subscription.credits_used_this_period = 0
   SET tenant.plan_id = new_plan_id
   INSERT credit_transaction:
     { tenant_id, amount: new_plan.monthly_credits, type: "ALLOTMENT", description: "Downgrade allotment: {new_plan.name}" }
   SYNC feature flags for tenant
   DISABLE features not available on new plan
   IF tenant domain count > new_plan.max_domains:
     Do NOT delete domains. Tenant cannot add new ones until under limit.
   AUDIT_LOG("billing.plan_downgraded", tenant_id, { from: old_plan.name, to: new_plan.name })
```

#### Subscription Cancellation

```
BR-BILL-006: Subscription Cancellation
───────────────────────────────────────
Input: tenant_id

1. LOAD subscription WHERE tenant_id = tenant_id AND status = "ACTIVE"
   IF NOT found → REJECT "ERR_BILL_004"

2. CANCEL Stripe subscription at period end (not immediate)

3. SET subscription.status = "CANCELLED"

4. AUDIT_LOG("billing.subscription_cancelled", tenant_id, {
     effective_at: subscription.current_period_end
   })

5. NOTIFY tenant_owner:
   "Your subscription has been cancelled. You'll retain access until {current_period_end}."

6. AT PERIOD END:
   Trigger BR-TNT-011 (Subscription Expiry Downgrade from §2.7)
```

---

### 3.4 Stripe Integration

> Covers: BR-BILL-003, BR-BILL-007

#### Stripe Webhook Events

| Stripe Event | System Action |
|-------------|---------------|
| `invoice.payment_succeeded` | Confirm subscription active. If was `PAST_DUE`, set back to `ACTIVE`. |
| `invoice.payment_failed` | Set subscription status to `PAST_DUE`. Stripe handles retries automatically. |
| `customer.subscription.updated` | Sync plan changes (upgrade/downgrade effective). Update `current_period_start` and `current_period_end`. |
| `customer.subscription.deleted` | Subscription fully ended. Trigger BR-TNT-011 (free tier downgrade). |
| `checkout.session.completed` | Credit pack purchase completed. Add credits to tenant balance. |

#### Payment Failure Handling

```
BR-BILL-007: Payment Failure Flow
──────────────────────────────────
Trigger: Stripe webhook `invoice.payment_failed`

1. LOAD tenant_subscription WHERE stripe_subscription_id = event.subscription_id
   IF NOT found → LOG warning, RETURN (orphaned webhook)

2. SET subscription.status = "PAST_DUE"

3. AUDIT_LOG("billing.payment_failed", tenant_id, {
     attempt: event.attempt_count,
     next_retry: event.next_payment_attempt
   })

4. NOTIFY tenant_owner via email:
   "Payment failed for your Reconova subscription. Please update your payment method."

5. STRIPE RETRY SCHEDULE (managed by Stripe):
   Attempt 1: Immediate (initial failure)
   Attempt 2: ~3 days later
   Attempt 3: ~5 days later
   Attempt 4: ~7 days later
   Total retry window: ~2 weeks

6. IF payment succeeds during retry window:
   Stripe sends `invoice.payment_succeeded`
   SET subscription.status = "ACTIVE"
   AUDIT_LOG("billing.payment_recovered", tenant_id)

7. IF all retries exhausted:
   Stripe sends `customer.subscription.deleted`
   SET subscription.status = "EXPIRED"
   Trigger BR-TNT-011 (Subscription Expiry Downgrade from §2.7)
   AUDIT_LOG("billing.subscription_expired", tenant_id, { reason: "payment_failure" })
```

#### Past Due Behavior

| Aspect | Effect |
|--------|--------|
| Tenant access | Full access continues during `PAST_DUE`. No degradation. |
| Scans | Allowed. Credits consumed normally. |
| Plan changes | Blocked. Cannot upgrade or downgrade while `PAST_DUE`. Must resolve payment first. |
| Credit purchases | Blocked. Cannot purchase credit packs while `PAST_DUE`. |
| Duration | Up to ~2 weeks (Stripe retry schedule). |

#### Webhook Security

| Aspect | Detail |
|--------|--------|
| Verification | All webhooks verified using Stripe webhook signing secret. Reject unverified payloads. |
| Idempotency | Process each event ID only once. Store processed event IDs in Redis with 24-hour TTL. Duplicate events are acknowledged (200) but not re-processed. |
| Ordering | Handle out-of-order events by checking timestamps. If event timestamp is older than last processed event for the same subscription, skip. |
| Failure handling | Return 500 to Stripe on processing errors. Stripe will retry. Max 3 retries from Stripe side. |
| Endpoint | `POST /api/webhooks/stripe`. Not authenticated via JWT — verified by Stripe signature only. |

---

### 3.5 Credit System Overview

> Covers: BR-BILL-008, BR-BILL-014

#### Consumption Model

Credits are consumed **per-step per-domain**. Each workflow step (check type) has a configurable credit cost that varies by subscription tier. Higher tiers get discounted rates.

**Example:** Pro tier tenant scans 3 domains with a Full Scan workflow:

| Step | Credits/Domain | Domains | Subtotal |
|------|---------------|---------|----------|
| Subdomain enumeration | 1 | 3 | 3 |
| Port scanning | 2 | 3 | 6 |
| Vulnerability scanning | 3 | 3 | 9 |
| **Total** | | | **18 credits** |

#### Transaction Types

| Type | Amount | Trigger | Description |
|------|--------|---------|-------------|
| `ALLOTMENT` | Positive | Period start, subscription creation, upgrade | Monthly credit allocation from plan. |
| `CONSUMPTION` | Negative | Scan job creation | Credits deducted for scan execution. Links to `scan_job_id`. |
| `PURCHASE` | Positive | Credit pack purchase | One-time credit pack added to balance. |
| `REFUND` | Positive | Scan step failure | Credits returned for failed/unexecuted scan steps. Links to `scan_job_id`. |
| `ADJUSTMENT` | Positive or negative | Super admin action | Manual credit adjustment. Requires `description` and `created_by`. |

#### Balance Calculation

Credits are calculated from `credit_transactions` as the single source of truth. The `credits_remaining` field on `tenant_subscriptions` tracks allotment balance only.

```
BR-BILL-014: Credit Balance Calculation
────────────────────────────────────────
Input: tenant_id

1. LOAD allotment_balance = tenant_subscriptions.credits_remaining
   (Tracks remaining allotment credits for current period)

2. CALCULATE purchased_balance:
   SUM(amount) FROM credit_transactions
   WHERE tenant_id = tenant_id
     AND type IN ("PURCHASE", "ADJUSTMENT")
     AND created_at >= current_period_start
   MINUS
   SUM(ABS(amount)) of purchased credits consumed this period
   (Consumed after allotment was exhausted)

3. total_available = allotment_balance + purchased_balance

4. RETURN total_available
```

#### Consumption Priority

When credits are consumed, **allotment credits are used first**. Purchased credits are consumed only after allotment is exhausted. This maximizes the value of purchased credits since they survive period resets.

```
Deduction order:
1. Deduct from allotment balance (credits_remaining on tenant_subscriptions)
2. If allotment exhausted, deduct from purchased balance (tracked via credit_transactions)
```

---

### 3.6 Credit Consumption & Deduction

> Covers: BR-BILL-011, BR-BILL-012

#### Pre-Scan Balance Check

```
BR-BILL-011: Credit Balance Check
──────────────────────────────────
Input: tenant_id, workflow_steps[], domain_count

1. CALCULATE required_credits:
   FOR EACH step IN workflow_steps:
     LOAD pricing WHERE check_type = step.check_type AND tier_id = tenant.plan_id
     required_credits += pricing.credits_per_domain * domain_count

2. CALCULATE available_credits (per BR-BILL-014 Balance Calculation)

3. IF available_credits < required_credits:
   REJECT "ERR_BILL_007" with detail:
   { required: required_credits, available: available_credits, shortfall: required_credits - available_credits }

4. RETURN { required_credits, available_credits, approved: true }
```

#### Credit Deduction at Scan Creation

```
BR-BILL-012: Credit Deduction
──────────────────────────────
Input: tenant_id, scan_job_id, workflow_steps[], domain_count

1. PERFORM balance check (BR-BILL-011)
   IF insufficient → REJECT "ERR_BILL_007"

2. CALCULATE total_credits (same as balance check)

3. DEDUCT from allotment first:
   IF credits_remaining >= total_credits:
     SET credits_remaining -= total_credits
   ELSE:
     allotment_used = credits_remaining
     purchased_used = total_credits - allotment_used
     SET credits_remaining = 0
     (purchased_used tracked implicitly via transaction)

4. SET credits_used_this_period += total_credits

5. INSERT credit_transaction:
   {
     tenant_id,
     amount: -total_credits,
     type: "CONSUMPTION",
     scan_job_id,
     description: "Scan: {domain_count} domains, {step_count} steps"
   }

6. AUDIT_LOG("billing.credits_consumed", tenant_id, {
     scan_job_id, credits: total_credits, remaining: new_balance
   })
```

#### Deduction Timing

| Aspect | Detail |
|--------|--------|
| When | Credits deducted at scan job creation, **before** execution begins. |
| Why | Prevents over-consumption. Guarantees credits are available. |
| Atomicity | Balance check + deduction in a single database transaction. Prevents race conditions. |
| Concurrent scans | Second scan sees the balance after first scan's deduction. No double-spending. |

---

### 3.7 Credit Allotment & Reset

> Covers: BR-BILL-010

#### Period Reset Algorithm

```
BR-BILL-010: Credit Allotment Reset
────────────────────────────────────
Trigger: Stripe webhook `invoice.payment_succeeded` (new billing period)
         OR daily background job checks for periods that ended

1. LOAD subscription WHERE tenant_id = tenant_id AND status = "ACTIVE"

2. SET subscription.current_period_start = new_period_start (from Stripe)
   SET subscription.current_period_end = new_period_end (from Stripe)

3. SET subscription.credits_remaining = plan.monthly_credits
   SET subscription.credits_used_this_period = 0

4. INSERT credit_transaction:
   {
     tenant_id,
     amount: plan.monthly_credits,
     type: "ALLOTMENT",
     description: "Monthly allotment reset: {plan.name}"
   }

5. AUDIT_LOG("billing.credits_reset", tenant_id, {
     plan: plan.name,
     allotment: plan.monthly_credits,
     purchased_carried_over: purchased_balance
   })
```

#### Allotment vs Purchased Credit Behavior at Reset

| Credit Type | At Period Reset |
|-------------|----------------|
| Allotment | Reset to plan's `monthly_credits`. Unused allotment credits are lost. |
| Purchased (credit packs) | **Survive reset.** Carried over into the next period. Remain available until consumed. |
| Adjustment (super admin) | Treated as purchased. Survive reset. |
| Refund | Treated as purchased. Survive reset. |

#### Purchased Credit Survival

Purchased credits survive because they are tracked via `credit_transactions` rather than `credits_remaining`. At reset, only `credits_remaining` (allotment) is overwritten. The purchased balance is calculated dynamically:

```
purchased_balance = SUM(amount) FROM credit_transactions
  WHERE tenant_id = tenant_id
    AND type IN ("PURCHASE", "ADJUSTMENT", "REFUND")
    AND NOT yet consumed by purchase-tier deductions
```

Since allotment credits are consumed first (§3.5), purchased credits are naturally preserved until the allotment is fully used.

#### Pending Downgrade at Reset

If a downgrade is scheduled (`pending_plan_id` is set), the period reset also applies the downgrade per BR-BILL-005 (§3.3).

---

### 3.8 Credit Packs

> Covers: BR-BILL-015

#### Purchase Flow

```
BR-BILL-015: Credit Pack Purchase
──────────────────────────────────
Input: tenant_id, credit_pack_id

1. LOAD tenant WHERE id = tenant_id
   IF tenant.status != "ACTIVE" → REJECT "ERR_BILL_001"

2. LOAD subscription WHERE tenant_id = tenant_id AND status = "ACTIVE"
   IF NOT found → REJECT "ERR_BILL_008"
   IF subscription.status == "PAST_DUE" → REJECT "ERR_BILL_009"

3. LOAD credit_pack WHERE id = credit_pack_id
   IF NOT found OR status != "ACTIVE" → REJECT "ERR_BILL_010"

4. CREATE Stripe Checkout Session:
   {
     customer: subscription.stripe_customer_id,
     mode: "payment" (one-time, not recurring),
     line_items: [{ price: credit_pack.stripe_price_id, quantity: 1 }],
     metadata: { tenant_id, credit_pack_id }
   }

5. RETURN { checkout_url: stripe_session.url }
```

#### Purchase Completion (Webhook)

```
BR-BILL-015: Credit Pack Fulfillment
─────────────────────────────────────
Trigger: Stripe webhook `checkout.session.completed`

1. EXTRACT tenant_id, credit_pack_id from session metadata

2. LOAD credit_pack WHERE id = credit_pack_id

3. INSERT credit_transaction:
   {
     tenant_id,
     amount: credit_pack.credits,
     type: "PURCHASE",
     description: "Credit pack: {credit_pack.name} ({credit_pack.credits} credits)"
   }

4. AUDIT_LOG("billing.credit_pack_purchased", tenant_id, {
     pack: credit_pack.name,
     credits: credit_pack.credits,
     price: credit_pack.price
   })

5. NOTIFY tenant_owner:
   "{credit_pack.credits} credits have been added to your account."
```

#### Credit Pack Constraints

| Constraint | Detail |
|-----------|--------|
| Subscription required | Must have an `ACTIVE` subscription. Free tier cannot purchase packs. |
| Payment method | One-time Stripe payment. Uses existing Stripe customer. |
| Expiry | Purchased credits do **not** expire. They survive period resets until consumed. |
| Multiple purchases | Allowed. No limit on number of packs purchased per period. |
| Refunds | Credit pack refunds are not automated. Must be handled by super admin via manual adjustment. |

---

### 3.9 Credit Refunds & Adjustments

> Covers: BR-BILL-013, BR-BILL-016

#### Scan Failure Refund

```
BR-BILL-013: Credit Refund on Scan Failure
───────────────────────────────────────────
Trigger: Scan step fails during execution

Input: tenant_id, scan_job_id, failed_step_index, total_steps, domain_count

1. IDENTIFY refundable steps:
   - The failed step itself
   - All subsequent steps that were not executed

2. CALCULATE refund_amount:
   FOR EACH refundable_step:
     LOAD pricing WHERE check_type = step.check_type AND tier_id = tenant.plan_id
     refund_amount += pricing.credits_per_domain * domain_count

3. INSERT credit_transaction:
   {
     tenant_id,
     amount: refund_amount (positive),
     type: "REFUND",
     scan_job_id,
     description: "Refund: {refundable_step_count} steps failed/skipped on scan {scan_job_id}"
   }

4. NOTE: Refunded credits are treated as purchased credits (survive period reset).
   They are NOT added back to credits_remaining.

5. AUDIT_LOG("billing.credits_refunded", tenant_id, {
     scan_job_id,
     refunded: refund_amount,
     failed_step: failed_step.check_type,
     steps_skipped: total_steps - failed_step_index - 1
   })
```

#### Refund Rules

| Scenario | Refunded |
|----------|----------|
| Step fails mid-execution | Yes — failed step + all subsequent unexecuted steps |
| Step completes successfully | No — completed steps are never refunded |
| Entire scan cancelled by user | Yes — all unexecuted steps. Completed steps not refunded. |
| Scan cancelled due to tenant suspension | Yes — all unexecuted steps (per §2.8). Completed steps not refunded. Credits consumed up to cancellation point are kept. |
| Scan completes successfully | No refund |

#### Super Admin Manual Adjustment

```
BR-BILL-016: Manual Credit Adjustment
──────────────────────────────────────
Input: tenant_id, amount (positive or negative), reason, super_admin_id

1. VERIFY caller has SUPER_ADMIN role
   IF NOT → REJECT "ERR_BILL_011"

2. IF reason is empty or null → REJECT "ERR_BILL_012"

3. LOAD tenant WHERE id = tenant_id
   IF NOT found → REJECT "ERR_TNT_004"

4. IF amount < 0:
   CALCULATE current_balance (per BR-BILL-014)
   IF current_balance + amount < 0 → REJECT "ERR_BILL_013"
   (Cannot deduct more credits than tenant has)

5. INSERT credit_transaction:
   {
     tenant_id,
     amount: amount,
     type: "ADJUSTMENT",
     description: reason,
     created_by: super_admin_id
   }

6. AUDIT_LOG("billing.manual_adjustment", tenant_id, {
     amount,
     reason,
     adjusted_by: super_admin_id
   })
```

#### Adjustment Constraints

| Constraint | Detail |
|-----------|--------|
| Who | `SUPER_ADMIN` only. |
| Reason required | Mandatory. Must provide human-readable justification. |
| Negative adjustment | Cannot reduce balance below zero. |
| Survival | Adjustments survive period resets (treated as purchased credits). |
| Audit | Full audit trail with super admin ID and reason. |

---

### 3.10 Scan Credit Pricing

> Covers: BR-BILL-008, BR-BILL-009

#### Pricing Model

Each check type has a per-domain credit cost that varies by subscription tier. Higher tiers get discounted rates per credit, incentivizing upgrades.

#### Default Pricing Matrix

| Check Type | Starter | Pro | Enterprise |
|-----------|---------|-----|------------|
| `subdomain_enum` | 2 | 1 | 1 |
| `port_scan` | 3 | 2 | 1 |
| `vuln_scan` | N/A | 3 | 2 |
| `tech_detect` | 2 | 1 | 1 |
| `screenshot` | 1 | 1 | 1 |
| `compliance_check` | N/A | 4 | 3 |
| `shodan_lookup` | N/A | 3 | 2 |
| `securitytrails_lookup` | N/A | 3 | 2 |
| `censys_lookup` | N/A | N/A | 2 |
| `custom_connector` | N/A | N/A | 3 |

`N/A` = check type not available on this tier (enforced by feature flags, not pricing).

#### Pricing Configuration Algorithm

```
BR-BILL-009: Update Scan Step Pricing
──────────────────────────────────────
Input: check_type, tier_id, credits_per_domain, super_admin_id

1. VERIFY caller has SUPER_ADMIN role
   IF NOT → REJECT "ERR_BILL_011"

2. LOAD plan WHERE id = tier_id
   IF NOT found → REJECT "ERR_BILL_003"

3. IF credits_per_domain < 0 → REJECT "ERR_BILL_014"

4. UPSERT scan_step_pricing:
   SET credits_per_domain = credits_per_domain
   SET updated_by = super_admin_id
   SET updated_at = now()

5. AUDIT_LOG("billing.pricing_updated", null, {
     check_type,
     tier: plan.name,
     old_price: previous_credits_per_domain,
     new_price: credits_per_domain,
     updated_by: super_admin_id
   })
```

#### Pricing Change Rules

| Rule | Detail |
|------|--------|
| Effective for | New scans only. In-progress scans use the price locked at scan creation time. |
| Retroactive | Never. Completed scans are not re-priced. |
| Who can change | `SUPER_ADMIN` only. |
| Minimum price | 0 credits (free check type). |
| Missing pricing | If no `scan_step_pricing` row exists for a check type + tier combination, the scan step is rejected with `ERR_BILL_015`. Pricing must be configured before use. |

---

### 3.11 Scheduled Scan Credit Handling

> Covers: BR-BILL-017, BR-BILL-018

#### Soft Reservation Model

Scheduled scans use a **soft reservation** — credits are checked at scheduling time but not held. The actual balance check and deduction happen at execution time.

#### Schedule Creation

```
BR-BILL-017: Scheduled Scan Credit Pre-Check
─────────────────────────────────────────────
Input: tenant_id, workflow_steps[], domain_count, cron_expression

1. CALCULATE estimated_credits:
   FOR EACH step IN workflow_steps:
     LOAD pricing WHERE check_type = step.check_type AND tier_id = tenant.plan_id
     estimated_credits += pricing.credits_per_domain * domain_count

2. CALCULATE available_credits (per BR-BILL-014)

3. IF available_credits < estimated_credits:
   REJECT "ERR_BILL_007" with detail:
   { required: estimated_credits, available: available_credits, shortfall: estimated_credits - available_credits }

4. CREATE scan_schedule record:
   {
     domain_id,
     workflow_id,
     cron_expression,
     enabled: true,
     estimated_credits: estimated_credits
   }

5. NOTE: No credits deducted. No reservation held.
   Balance may change between scheduling and execution.

6. AUDIT_LOG("scan.schedule_created", tenant_id, {
     estimated_credits, cron: cron_expression
   })
```

#### Scheduled Scan Execution

```
BR-BILL-017: Scheduled Scan Execution Credit Check
───────────────────────────────────────────────────
Trigger: Cron scheduler fires for a scheduled scan

1. LOAD scan_schedule
   IF NOT enabled → SKIP

2. CALCULATE required_credits at current pricing
   (Pricing may have changed since schedule creation)

3. CALCULATE available_credits (per BR-BILL-014)

4. IF available_credits < required_credits:
   SKIP this execution
   INSERT notification:
     "Scheduled scan skipped: insufficient credits ({available} available, {required} needed)"
   AUDIT_LOG("scan.schedule_skipped", tenant_id, {
     reason: "insufficient_credits",
     required: required_credits,
     available: available_credits
   })
   RETURN

5. PROCEED with normal scan creation flow (BR-BILL-012: Credit Deduction)
```

#### Schedule Cancellation

```
BR-BILL-018: Schedule Cancellation
───────────────────────────────────
Input: tenant_id, schedule_id

1. LOAD scan_schedule WHERE id = schedule_id
   IF NOT found → REJECT "ERR_BILL_016"

2. SET scan_schedule.enabled = false

3. NOTE: No credits to release since soft reservation holds no credits.

4. AUDIT_LOG("scan.schedule_cancelled", tenant_id, { schedule_id })
```

#### Soft Reservation Trade-offs

| Aspect | Detail |
|--------|--------|
| Advantage | Simple. No locked credits. Tenant can use full balance freely between executions. |
| Risk | Scan may be skipped if balance is insufficient at execution time. |
| Mitigation | Tenant notified when a scheduled scan is skipped due to insufficient credits. |
| Pricing changes | Scheduled scans use current pricing at execution time, not the price at scheduling time. |
| Multiple schedules | Each schedule is checked independently. No aggregate reservation across schedules. |

---

### 3.12 Permissions Matrix

> Cross-cutting permissions for billing & credits

| Action | `TENANT_OWNER` | `SUPER_ADMIN` |
|--------|:-:|:-:|
| View subscription details | Yes (own tenant) | Yes (any tenant) |
| Create subscription | Yes | No (tenant self-service only) |
| Upgrade plan | Yes | No (tenant self-service only) |
| Downgrade plan | Yes | No (tenant self-service only) |
| Cancel subscription | Yes | No (tenant self-service only) |
| View credit balance | Yes (own tenant) | Yes (any tenant) |
| View credit transaction history | Yes (own tenant) | Yes (any tenant) |
| Purchase credit pack | Yes | No (tenant self-service only) |
| Manually adjust credits | No | Yes |
| Configure scan step pricing | No | Yes |
| Create/modify subscription plans | No | Yes |
| Deprecate/archive plans | No | Yes |
| View billing history | Yes (own tenant) | Yes (any tenant) |
| Update payment method | Yes (via Stripe portal) | No |

#### Notes

- Subscription management (create, upgrade, downgrade, cancel) is **tenant self-service only**. Super admins do not manage subscriptions on behalf of tenants — they manage the platform (plans, pricing, adjustments).
- Super admins can view any tenant's billing data for support purposes.
- Payment method management is handled entirely through Stripe's hosted customer portal. Reconova does not store payment details.

---

### 3.13 Edge Cases

> Cross-cutting edge cases for billing & credits

| Scenario | Behavior |
|----------|----------|
| Tenant upgrades mid-period with 0 credits remaining | Credits reset to new plan's allotment. Previous balance (0) discarded. |
| Tenant upgrades mid-period with purchased credits remaining | Credits reset to new plan's allotment. Purchased credits survive — still available on top of new allotment. |
| Tenant downgrades with more domains than new plan allows | Downgrade proceeds. Existing domains preserved. Tenant cannot add new domains until count is under the new plan's `max_domains`. |
| Tenant cancels then re-subscribes before period ends | Subscription status set back to `ACTIVE`. Credits reset to new plan's allotment. Same Stripe customer ID reused. |
| Two scans submitted simultaneously, only enough credits for one | First transaction to acquire database lock succeeds. Second sees reduced balance and is rejected with `ERR_BILL_007`. Atomicity enforced at database transaction level. |
| Credit pack purchase while subscription is `PAST_DUE` | Rejected with `ERR_BILL_009`. Must resolve payment issue first. |
| Credit pack Stripe payment succeeds but webhook delivery fails | Stripe retries webhook up to 3 times. If all fail, credits are not added. Super admin must manually reconcile via adjustment. |
| Scan fails on first step | All credits for the entire scan are refunded (failed step + all subsequent unexecuted steps). |
| Scan fails on last step | Only the last step's credits are refunded. All prior completed steps are not refunded. |
| Super admin sets pricing to 0 credits for a check type | Allowed. That check type becomes free for the tier. Scans including that step deduct 0 credits for it. |
| Pricing changes while a scan is in progress | In-progress scan uses the price locked at scan creation time. New pricing applies only to future scans. |
| Period resets while a scan is running | Running scan completes normally. Credits were already deducted at creation. New allotment is independent. |
| Scheduled scan fires but tenant was downgraded since scheduling | Execution-time check uses current plan's pricing. If check type is unavailable on new tier (e.g., `vuln_scan` on Starter), scan is skipped with notification. |
| Scheduled scan fires but pricing increased since scheduling | Execution uses current pricing. If credits are insufficient at new price, scan is skipped with notification. |
| Tenant purchases credit pack, then subscription expires | Subscription expires → free tier downgrade. Purchased credits remain in transaction history but are unusable (free tier cannot run scans). If tenant re-subscribes, purchased credits become available again. |
| Super admin deducts credits below what's needed for a running scan | Allowed. Running scan already deducted its credits at creation. Adjustment only affects future scans. |
| Multiple scheduled scans fire at the same time | Each is processed sequentially. First scan deducts credits, subsequent scans check remaining balance. Some may be skipped if insufficient credits. |
| Stripe webhook arrives for a deleted/deactivated tenant | Log warning. Do not process. Return 200 to Stripe to prevent retries. |
| Annual subscriber upgrades plan | Pro-rated by Stripe for the remainder of the annual period. Credits reset to new plan's allotment immediately. |

---

### 3.14 Error Codes

> All `ERR_BILL_*` error codes for billing & credits

| Code | HTTP | Message | Cause |
|------|------|---------|-------|
| `ERR_BILL_001` | 403 | Tenant is not active | Billing operation attempted on non-ACTIVE tenant |
| `ERR_BILL_002` | 409 | Active subscription already exists | Attempted to create subscription when one is already active |
| `ERR_BILL_003` | 404 | Subscription plan not found | Plan ID does not exist or plan is not ACTIVE |
| `ERR_BILL_004` | 404 | No active subscription found | Upgrade/downgrade/cancel attempted with no current subscription |
| `ERR_BILL_005` | 400 | Cannot upgrade to a lower plan | Upgrade endpoint called with a plan that has fewer credits than current |
| `ERR_BILL_006` | 400 | Cannot downgrade to a higher plan | Downgrade endpoint called with a plan that has more credits than current |
| `ERR_BILL_007` | 402 | Insufficient credits | Not enough credits to run scan. Response includes `required`, `available`, `shortfall` |
| `ERR_BILL_008` | 403 | Active subscription required to purchase credits | Credit pack purchase attempted without active subscription |
| `ERR_BILL_009` | 403 | Cannot purchase credits while payment is past due | Credit pack purchase attempted while subscription is PAST_DUE |
| `ERR_BILL_010` | 404 | Credit pack not found | Credit pack ID does not exist or is ARCHIVED |
| `ERR_BILL_011` | 403 | Insufficient permissions | Non-super-admin attempted admin-only billing action |
| `ERR_BILL_012` | 400 | Reason is required for credit adjustments | Super admin adjustment submitted without description |
| `ERR_BILL_013` | 400 | Cannot deduct more credits than available | Negative adjustment would reduce balance below zero |
| `ERR_BILL_014` | 400 | Credits per domain cannot be negative | Pricing configuration with negative value |
| `ERR_BILL_015` | 400 | Pricing not configured for this check type and tier | Scan includes a step with no pricing row for the tenant's plan |
| `ERR_BILL_016` | 404 | Scan schedule not found | Cancel/modify attempted on non-existent schedule |

---
