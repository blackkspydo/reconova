# User Flows (Billing & Credits)

Scope: User journey flowcharts for subscription management, credit operations, and super admin billing actions.

---

## Preconditions

| Flow Group | Auth State | Role | Data Requirements |
|-----------|------------|------|-------------------|
| Subscription management | Authenticated | Tenant Owner | Active tenant |
| Credit operations | Authenticated | Tenant Owner | Active subscription |
| View billing | Authenticated | Any tenant member | Active tenant |
| Admin credit adjustment | Authenticated | Super Admin | Target tenant exists |
| Admin pricing config | Authenticated | Super Admin | Plans exist |

---

## Flow 1: First-Time Subscription (New Tenant)

**Entry points:**
- Clicks "Upgrade" from free-tier banner
- Navigates to Settings > Billing > Change Plan
- Clicks credit balance widget (shows "No plan")

```
[Free Tier User] → clicks [View Plans] or [Upgrade]
    │ (analytics: billing_plans_viewed)
    ▼
[Plan Comparison Page]
    │ Shows 3 plans: Starter, Pro, Enterprise
    │ Default interval: Monthly
    │ Toggle: [Monthly] [Annual] (shows savings %)
    │
    ├─ Clicks [Contact Sales] on Enterprise
    │   │ (analytics: enterprise_contact_clicked)
    │   └─► Opens contact/sales form or mailto link
    │
    └─ Clicks [Select Plan] on Starter or Pro
        │ (analytics: plan_selected, {plan_id, interval})
        ▼
    [Subscription Confirmation Modal]
        │ Shows: plan name, price, billing interval
        │ Shows: "You'll receive {X} credits/month"
        │ Shows: pro-rated amount for first period
        │ CTA: [Subscribe — ${price}/mo]  [Cancel]
        │
        ├─ Clicks [Cancel]
        │   └─► Modal closes, back to plan comparison
        │
        └─ Clicks [Subscribe]
            │ (analytics: subscription_checkout_initiated, {plan_id, interval})
            ▼
        [Stripe Checkout] (external redirect)
            │
            ├─ Payment succeeds → Stripe redirects to /settings/billing?status=success
            │   │ (analytics: subscription_created, {plan_id, interval})
            │   ▼
            │ [Billing Overview] with success toast:
            │   "Welcome to {Plan}! You have {X} credits to get started."
            │
            └─ Payment fails / user cancels → Stripe redirects to /settings/billing?status=cancelled
                │ (analytics: subscription_checkout_cancelled)
                ▼
              [Plan Comparison Page] with info toast:
                "Checkout was not completed. You can try again anytime."
```

---

## Flow 2: Plan Upgrade

**Preconditions:** Active subscription (status ACTIVE or CANCELLED)

```
[Billing Overview] → clicks [Change Plan]
    │ (analytics: billing_plans_viewed)
    ▼
[Plan Comparison Page]
    │ Current plan highlighted with "Current Plan" badge
    │ Higher plans show [Upgrade] button
    │ Lower plans show [Downgrade] button
    │
    └─ Clicks [Upgrade] on higher plan
        │ (analytics: plan_upgrade_initiated, {current_plan, new_plan})
        ▼
    [Upgrade Confirmation Modal]
        │ Shows: "{Current Plan} → {New Plan}"
        │ Shows: "Pro-rated charge: ${amount} today"
        │ Shows: "Your credits will reset to {X}"
        │ Shows: "New features available immediately"
        │ IF subscription was CANCELLED:
        │   Shows: "This will reactivate your subscription"
        │ IF pending downgrade exists:
        │   Shows: "This will cancel your pending downgrade"
        │ CTA: [Confirm Upgrade]  [Cancel]
        │
        ├─ Clicks [Cancel] → modal closes
        │
        └─ Clicks [Confirm Upgrade]
            │ (analytics: plan_upgrade_confirmed, {current_plan, new_plan})
            ▼
        API: POST /api/billing/checkout {plan_id, billing_interval}
            │
            ├─ Success → Stripe Checkout redirect
            │   ├─ Payment succeeds → /settings/billing?status=upgraded
            │   │   ▼
            │   │ Success toast: "Upgraded to {Plan}! Credits reset to {X}."
            │   │
            │   └─ Payment fails → /settings/billing?status=cancelled
            │       ▼
            │     Info toast: "Upgrade was not completed."
            │
            ├─ ERR_BILL_004 (no active subscription)
            │   └─► Error toast: "No active subscription found."
            │
            ├─ ERR_BILL_005 (cannot upgrade to lower plan)
            │   └─► Error toast: "Selected plan is not an upgrade."
            │
            └─ ERR_BILL_009 (past due)
                └─► Error toast: "Please resolve your outstanding payment first."
                    Show [Update Payment Method] link
```

---

## Flow 3: Plan Downgrade

**Preconditions:** Active subscription, status = ACTIVE

```
[Plan Comparison Page] → clicks [Downgrade] on lower plan
    │ (analytics: plan_downgrade_initiated, {current_plan, new_plan})
    ▼
[Downgrade Confirmation Modal]
    │ Shows: "{Current Plan} → {New Plan}"
    │ Shows: "Takes effect on {current_period_end}"
    │ Shows: "You'll keep full access until then"
    │ Shows: "Credits will reset to {X} on {date}"
    │ IF domain_count > new_plan.max_domains:
    │   Warning: "You have {N} domains but {Plan} allows {M}.
    │            You won't be able to add new domains until
    │            you're under the limit."
    │ Shows features being lost (diff table)
    │ CTA: [Schedule Downgrade]  [Cancel]
    │
    ├─ Clicks [Cancel] → modal closes
    │
    └─ Clicks [Schedule Downgrade]
        │ (analytics: plan_downgrade_confirmed, {current_plan, new_plan, effective_date})
        ▼
    API: POST /api/billing/downgrade {plan_id}
        │
        ├─ Success (200)
        │   ▼
        │ [Billing Overview] with success toast:
        │   "Downgrade to {Plan} scheduled for {date}."
        │ Shows pending downgrade indicator:
        │   "Downgrading to {Plan} on {date}" + [Cancel Downgrade]
        │
        ├─ ERR_BILL_006 (cannot downgrade to higher plan)
        │   └─► Error toast: "Selected plan is not a downgrade."
        │
        └─ ERR_BILL_004 (no active subscription)
            └─► Error toast: "No active subscription found."
```

### Cancel Pending Downgrade

```
[Billing Overview] → clicks [Cancel Downgrade]
    │ (analytics: downgrade_cancel_initiated)
    ▼
[Confirmation Dialog]
    │ "Cancel your scheduled downgrade to {Plan}?"
    │ "You'll stay on {Current Plan}."
    │ CTA: [Keep Current Plan]  [No, Continue Downgrade]
    │
    └─ Clicks [Keep Current Plan]
        │ (analytics: downgrade_cancelled)
        ▼
    API: DELETE /api/billing/pending-downgrade
        │
        └─ Success → toast: "Downgrade cancelled. You're staying on {Plan}."
           Pending downgrade indicator removed
```

---

## Flow 4: Subscription Cancellation

**Preconditions:** Active subscription, status = ACTIVE

```
[Billing Overview] → clicks [Cancel Subscription]
    │ (analytics: cancellation_initiated)
    ▼
[Cancellation Confirmation Modal]
    │ Shows: "Your {Plan} subscription"
    │ Shows: "You'll retain access until {current_period_end}"
    │ Shows: "After that, you'll be on the free tier:"
    │   • No scans
    │   • No domains
    │   • Read-only access
    │ Shows: "Unused credits will be lost"
    │ CTA: [Cancel Subscription]  [Keep My Plan]
    │
    ├─ Clicks [Keep My Plan] → modal closes
    │   (analytics: cancellation_abandoned)
    │
    └─ Clicks [Cancel Subscription]
        │ (analytics: subscription_cancelled, {plan_id, days_remaining})
        ▼
    API: POST /api/billing/cancel
        │
        ├─ Success (200)
        │   ▼
        │ [Billing Overview] with toast:
        │   "Subscription cancelled. Access until {date}."
        │ Status changes to CANCELLED
        │ Banner: "Your plan cancels on {date}" + [Reactivate]
        │
        └─ ERR_BILL_004 (no active subscription)
            └─► Error toast: "No active subscription to cancel."
```

---

## Flow 5: Payment Failure (Past Due)

**Trigger:** System (Stripe webhook), not user-initiated

```
[Stripe webhook: invoice.payment_failed]
    │ System sets subscription status = PAST_DUE
    ▼
[Next page load / real-time update]
    │
    ▼
[Past Due Banner] appears globally:
    │ "Payment failed — update your payment method to avoid losing access."
    │ [Update Payment Method] → Stripe Customer Portal (external)
    │
    │ During PAST_DUE:
    │   • Full access continues (scans, credits work normally)
    │   • [Change Plan] button → disabled, tooltip: "Resolve payment first"
    │   • [Purchase Credits] button → disabled, tooltip: "Resolve payment first"
    │   • Stripe retries automatically (~2 weeks)
    │
    ├─ Payment succeeds (webhook: invoice.payment_succeeded)
    │   │ Status → ACTIVE
    │   ▼
    │ Banner removed. Toast: "Payment successful. You're all set!"
    │ Credits reset for new period
    │
    └─ All retries fail (webhook: customer.subscription.deleted)
        │ Status → EXPIRED
        ▼
    Downgraded to free tier
    Free tier banner: "Your subscription has expired. Upgrade to continue scanning."
    (analytics: subscription_expired, {plan_id, past_due_duration})
```

---

## Flow 6: Credit Pack Purchase

**Preconditions:** Active subscription (not free tier), status ≠ PAST_DUE

```
[Billing Overview] → clicks [Purchase Credits]
    │ (analytics: credit_packs_viewed)
    ▼
[Credit Packs Page]
    │ Shows available credit pack cards:
    │   Pack name | Credits | Price | [Purchase]
    │
    └─ Clicks [Purchase] on a pack
        │ (analytics: credit_pack_selected, {pack_id, credits, price})
        ▼
    [Purchase Confirmation Modal]
        │ Shows: "{Pack Name} — {X} credits for ${price}"
        │ Shows: "Credits never expire and carry over between periods"
        │ CTA: [Purchase — ${price}]  [Cancel]
        │
        ├─ Clicks [Cancel] → modal closes
        │
        └─ Clicks [Purchase]
            │ (analytics: credit_pack_checkout_initiated, {pack_id})
            ▼
        API: POST /api/billing/credits/purchase {credit_pack_id}
            │
            ├─ Success → Stripe Checkout redirect (one-time payment)
            │   ├─ Payment succeeds → /settings/billing?status=credits_purchased
            │   │   ▼
            │   │ Toast: "{X} credits added to your balance!"
            │   │ Credit balance widget updates
            │   │ (analytics: credit_pack_purchased, {pack_id, credits})
            │   │
            │   └─ Payment cancelled → /settings/billing/credit-packs
            │       ▼
            │     Info toast: "Credit purchase was not completed."
            │
            ├─ ERR_BILL_008 (no active subscription)
            │   └─► Error toast: "Active subscription required to purchase credits."
            │
            ├─ ERR_BILL_009 (past due)
            │   └─► Error toast: "Resolve your outstanding payment first."
            │
            └─ ERR_BILL_010 (pack not found)
                └─► Error toast: "This credit pack is no longer available."
                    Refresh pack list
```

---

## Flow 7: Credit Balance Check (During Scan Creation)

**Context:** This flow integrates with the Scanning frontend plan. Documented here for completeness.

```
[Scan Creation Form] → user selects domains + workflow steps
    │ On selection change:
    ▼
API: POST /api/billing/credits/estimate {workflow_steps[], domain_count}
    │
    ├─ Returns: {estimated_cost, available_credits, sufficient: true}
    │   ▼
    │ [Cost Estimate Display]
    │   "Estimated cost: {X} credits"
    │   "Available: {Y} credits"
    │   [Start Scan] button enabled
    │
    └─ Returns: {estimated_cost, available_credits, sufficient: false, shortfall}
        ▼
    [Insufficient Credits Warning]
        "This scan requires {X} credits but you only have {Y}."
        "You need {shortfall} more credits."
        [Start Scan] button disabled
        [Purchase Credits] → /settings/billing/credit-packs
        [Reduce Scope] → user can deselect steps/domains
```

---

## Flow 8: View Transaction History

```
[Billing Overview] → clicks [View Transaction History]
    │ (analytics: transaction_history_viewed)
    ▼
[Transaction History Page]
    │ Filter bar: [All Types ▼] [Date Range ▼]
    │ Type filter options: All, Allotment, Consumption, Purchase, Refund, Adjustment
    │
    │ Table columns: Date | Type | Description | Amount | Balance After
    │ Sorted by date descending (newest first)
    │ Pagination: 25 per page
    │
    ├─ Loading state → skeleton table rows
    │
    ├─ Empty state (no transactions)
    │   └─► "No credit transactions yet."
    │
    ├─ Filter applied → API re-fetches with query params
    │   (analytics: transaction_filter_applied, {type, date_range})
    │
    └─ Page navigation → API fetches next page
        (analytics: transaction_page_viewed, {page})
```

---

## Flow 9: Super Admin — Credit Adjustment

**Preconditions:** Super Admin role

```
[Admin Panel] → navigates to Billing > Credit Adjustment
    │ (analytics: admin_credit_adjustment_viewed)
    ▼
[Credit Adjustment Page]
    │ [Search Tenant] input field
    │
    └─ Types tenant name/ID → search results dropdown
        │
        └─ Selects tenant
            │ (analytics: admin_tenant_selected, {tenant_id})
            ▼
        [Tenant Credit Summary]
            │ Shows: Tenant name, current plan, credit balance
            │   Allotment: {X} | Purchased: {Y} | Total: {Z}
            │
            ▼
        [Adjustment Form]
            │ Amount: [input] (positive to add, negative to deduct)
            │ Reason: [textarea] (required)
            │ CTA: [Apply Adjustment]  [Cancel]
            │
            ├─ Amount empty or 0 → [Apply] disabled
            ├─ Reason empty → [Apply] disabled, inline hint: "Reason is required"
            │
            └─ Clicks [Apply Adjustment]
                │
                ├─ IF negative amount:
                │   ▼
                │ [Confirmation Modal]
                │   "Deduct {X} credits from {Tenant}?"
                │   "Current balance: {Y}. After adjustment: {Z}."
                │   [Confirm Deduction]  [Cancel]
                │
                └─ IF positive amount (or confirmed deduction):
                    │ (analytics: admin_credit_adjusted, {tenant_id, amount, type})
                    ▼
                API: PUT /api/admin/tenants/{id}/credits {amount, reason}
                    │
                    ├─ Success (200)
                    │   ▼
                    │ Toast: "Adjusted {tenant} credits by {±amount}."
                    │ Tenant credit summary refreshes
                    │ Form resets
                    │
                    ├─ ERR_BILL_011 (insufficient permissions)
                    │   └─► Error toast: "You don't have permission."
                    │
                    ├─ ERR_BILL_012 (reason required)
                    │   └─► Inline error on reason field
                    │
                    └─ ERR_BILL_013 (would go below zero)
                        └─► Error toast: "Cannot deduct more than available ({X} credits)."
```

---

## Flow 10: Super Admin — Scan Pricing Configuration

**Preconditions:** Super Admin role

```
[Admin Panel] → navigates to Billing > Scan Pricing
    │ (analytics: admin_pricing_config_viewed)
    ▼
[Scan Pricing Configuration Page]
    │ Matrix table: rows = check types, columns = plan tiers
    │ Each cell shows credits_per_domain (editable inline)
    │ N/A cells for features not available on a tier (non-editable)
    │
    │ ┌──────────────────┬─────────┬─────┬────────────┐
    │ │ Check Type       │ Starter │ Pro │ Enterprise │
    │ ├──────────────────┼─────────┼─────┼────────────┤
    │ │ subdomain_enum   │ [2]     │ [1] │ [1]        │
    │ │ port_scan        │ [3]     │ [2] │ [1]        │
    │ │ vuln_scan        │ N/A     │ [3] │ [2]        │
    │ │ ...              │         │     │            │
    │ └──────────────────┴─────────┴─────┴────────────┘
    │
    │ Edit mode: click cell → inline number input
    │ Unsaved changes highlighted with indicator
    │
    ├─ Edits a cell value
    │   │ Validate: min 0, integer only
    │   │ Cell highlighted as "modified"
    │   │ [Save Changes] button appears
    │   │
    │   └─ Clicks [Save Changes]
    │       │ (analytics: admin_pricing_updated, {changes_count})
    │       ▼
    │   [Confirmation Modal]
    │       "Update pricing for {N} check types?"
    │       "Changes apply to new scans only. In-progress scans are not affected."
    │       [Confirm]  [Cancel]
    │       │
    │       └─ Clicks [Confirm]
    │           ▼
    │       API: PUT /api/admin/billing/pricing {updates[]}
    │           │
    │           ├─ Success → toast: "Pricing updated."
    │           │   All cells reset to "saved" state
    │           │
    │           └─ ERR_BILL_014 (negative value)
    │               └─► Inline error on the cell
    │
    └─ Clicks [Reset] → discard unsaved changes
```

---

## Flow 11: Billing Overview Page Load

```
[User navigates to /settings/billing]
    │ (analytics: billing_overview_viewed)
    ▼
Parallel API calls:
    ├─ GET /api/billing/plans (available plans)
    ├─ GET /api/billing/credits (current balance)
    └─ GET /api/billing/subscription (current subscription)
        │
        ├─ All loading → skeleton UI
        │
        ├─ All succeed → render billing overview
        │   │
        │   ├─ IF no subscription (free tier):
        │   │   Show: "You're on the Free tier"
        │   │   CTA: [View Plans] (prominent)
        │   │   Credit section hidden
        │   │
        │   ├─ IF subscription ACTIVE:
        │   │   Show: plan name, next billing date, credit balance
        │   │   Actions: [Change Plan] [Purchase Credits] [Cancel]
        │   │   IF pending_plan_id set:
        │   │     Show: "Downgrading to {plan} on {date}" + [Cancel Downgrade]
        │   │
        │   ├─ IF subscription CANCELLED:
        │   │   Show: plan name, "Cancels on {date}"
        │   │   Actions: [Reactivate] (upgrade flow) [View Plans]
        │   │
        │   ├─ IF subscription PAST_DUE:
        │   │   Show: plan name, past-due warning
        │   │   Actions: [Update Payment] (Stripe portal)
        │   │   [Change Plan] disabled
        │   │   [Purchase Credits] disabled
        │   │
        │   └─ IF subscription EXPIRED:
        │       Same as free tier display
        │
        └─ API error → error state with retry
            "Unable to load billing information."
            [Retry] button
```

---

## Flow 12: Credit Balance Widget (Global Header)

```
[Any authenticated page loads]
    │
    ▼
API: GET /api/billing/credits (cached, refreshes on navigation)
    │
    ├─ No subscription (free tier):
    │   Widget shows: "Free" + [Upgrade] link
    │
    ├─ Has subscription:
    │   Widget shows: "{X} credits" (total available)
    │   Color coding:
    │     • Green: >50% remaining
    │     • Yellow: 20-50% remaining
    │     • Red: <20% remaining
    │   Click → navigates to /settings/billing
    │
    └─ API error → widget shows: "—" (dash)
        Click → still navigates to /settings/billing
```
