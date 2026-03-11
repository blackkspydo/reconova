# Reference (Billing & Credits)

Scope: Error handling matrix, input validation rules, security considerations, and frontend-to-backend action mapping.

---

## Error Handling Matrix

### Authentication & Authorization Errors

| Error Code | HTTP | User Message | UI Action |
|-----------|------|-------------|-----------|
| `ERR_BILL_001` | 403 | "Your tenant account is not active." | Toast (error). Redirect to dashboard. |
| `ERR_BILL_008` | 403 | "An active subscription is required to purchase credits." | Toast (error). Show [View Plans] link. |
| `ERR_BILL_009` | 403 | "Please resolve your outstanding payment before purchasing credits." | Toast (error). Show [Update Payment Method] link → Stripe portal. |
| `ERR_BILL_011` | 403 | "You don't have permission to perform this action." | Toast (error). No further action. |

### Subscription Errors

| Error Code | HTTP | User Message | UI Action |
|-----------|------|-------------|-----------|
| `ERR_BILL_002` | 409 | "You already have an active subscription." | Toast (info). Redirect to billing overview. |
| `ERR_BILL_003` | 404 | "This plan is no longer available." | Toast (error). Refresh plans list. |
| `ERR_BILL_004` | 404 | "No active subscription found." | Toast (error). Redirect to plan comparison. |
| `ERR_BILL_005` | 400 | "The selected plan is not an upgrade from your current plan." | Toast (error). Stay on plan comparison. |
| `ERR_BILL_006` | 400 | "The selected plan is not a downgrade from your current plan." | Toast (error). Stay on plan comparison. |

### Credit Errors

| Error Code | HTTP | User Message | UI Action |
|-----------|------|-------------|-----------|
| `ERR_BILL_007` | 402 | "Insufficient credits. You need {shortfall} more." | Show `<InsufficientCreditsModal />` with required, available, shortfall. |
| `ERR_BILL_010` | 404 | "This credit pack is no longer available." | Toast (error). Refresh credit packs list. |
| `ERR_BILL_012` | 400 | "Please provide a reason for this adjustment." | Inline error on reason field. |
| `ERR_BILL_013` | 400 | "Cannot deduct more than the available balance ({available} credits)." | Inline error on amount field. Update available balance display. |

### Pricing Errors

| Error Code | HTTP | User Message | UI Action |
|-----------|------|-------------|-----------|
| `ERR_BILL_014` | 400 | "Credits per domain cannot be negative." | Inline error on the pricing cell. Revert cell value. |
| `ERR_BILL_015` | 400 | "Pricing not configured for this check type and tier." | Toast (warning) during scan creation. Link to admin pricing page (if admin). |
| `ERR_BILL_016` | 404 | "Scan schedule not found." | Toast (error). Refresh schedule list. |

### Network & System Errors

| Scenario | User Message | UI Action |
|----------|-------------|-----------|
| Network timeout | "Unable to connect. Check your connection and try again." | Toast (error) + [Retry] button. |
| 500 Internal Server Error | "Something went wrong. Please try again." | Toast (error) + [Retry] button. |
| Stripe redirect failure | "Payment could not be processed. Please try again." | Toast (error). Return to billing overview. |
| Stripe portal unavailable | "Payment management is temporarily unavailable." | Toast (error). Suggest trying again later. |

### Error Response Parsing

```typescript
function handleBillingError(error: ApiError): void {
  const { code, message, details } = error.error;

  switch (code) {
    case 'ERR_BILL_007':
      // Special handling: show modal with credit details
      openInsufficientCreditsModal({
        required: details?.required as number,
        available: details?.available as number,
        shortfall: details?.shortfall as number,
      });
      break;

    case 'ERR_BILL_012':
    case 'ERR_BILL_013':
    case 'ERR_BILL_014':
      // Inline field errors — set form error state
      setFieldError(code, message);
      break;

    case 'ERR_BILL_002':
      // Redirect cases
      showToast('info', message);
      navigateTo('/settings/billing');
      break;

    case 'ERR_BILL_003':
    case 'ERR_BILL_010':
      // Stale data — refresh list
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

### Plan Selection

| Field | Type | Constraints | Error Message |
|-------|------|-------------|---------------|
| `plan_id` | UUID | Required. Must be ACTIVE plan. | "Please select a plan." |
| `billing_interval` | enum | Required. `MONTHLY` or `ANNUAL`. | "Please select a billing interval." |

### Credit Pack Purchase

| Field | Type | Constraints | Error Message |
|-------|------|-------------|---------------|
| `credit_pack_id` | UUID | Required. Must be ACTIVE pack. | "Please select a credit pack." |

### Admin: Credit Adjustment

| Field | Type | Constraints | Error Message |
|-------|------|-------------|---------------|
| `tenant_id` | UUID | Required. Tenant must exist. | "Please select a tenant." |
| `amount` | integer | Required. Non-zero. If negative, abs(amount) ≤ available balance. | "Amount is required." / "Cannot deduct more than available ({N} credits)." |
| `reason` | string | Required. Min 1 char. Max 500 chars. | "Reason is required." / "Reason must be under 500 characters." |

### Admin: Scan Pricing

| Field | Type | Constraints | Error Message |
|-------|------|-------------|---------------|
| `credits_per_domain` | integer | Required. Min 0. | "Must be 0 or greater." |

### Client-Side Validation Timing

| Trigger | Behavior |
|---------|----------|
| Field blur | Validate that field, show inline error if invalid |
| Form submit | Validate all fields, focus first invalid field |
| Amount input (admin) | Live validate against available balance for negative values |
| Pricing cell edit | Validate on blur (min 0, integer only) |

---

## Security Considerations

### Secure Storage

| Data | Storage | Justification |
|------|---------|---------------|
| JWT access token | Memory (state) | Never persisted to disk |
| Subscription status | Memory (state) | Fetched per session from API |
| Credit balance | Memory (state) | Frequently changes, always re-fetched |
| Plans list | Memory (state/cache) | Public data, no sensitivity |
| Stripe checkout URL | Not stored | Used immediately for redirect, then discarded |
| Stripe customer portal URL | Not stored | Used immediately for redirect, then discarded |
| Card/payment details | Never touches frontend | Handled entirely by Stripe |

### Rate Limiting Awareness

| Action | Rate Limit | Frontend Handling |
|--------|-----------|-------------------|
| Stripe checkout creation | Backend enforced | Disable button after click, re-enable on return/error |
| Credit pack purchase | Backend enforced | Disable [Purchase] button during checkout flow |
| Plan change | Backend enforced | Disable [Confirm] button after click |
| Admin credit adjustment | Backend enforced | Disable [Apply] during submission |
| Admin pricing save | Backend enforced | Disable [Save] during submission |
| Credit balance fetch | Soft cache (per navigation) | Don't re-fetch within same page view |
| Transaction history | Standard pagination | No special handling needed |

### Button Debouncing

| Action | Debounce Strategy |
|--------|-------------------|
| Subscribe / Upgrade / Downgrade | Disable button on click → re-enable on API response or Stripe return |
| Purchase Credits | Disable button on click → re-enable on API response or Stripe return |
| Cancel Subscription | Disable button on click → re-enable on API response |
| Apply Adjustment | Disable button on click → re-enable on API response |
| Save Pricing | Disable button on click → re-enable on API response |
| Tenant search (admin) | Debounce input 300ms before API call |
| Credit estimate (scan) | Debounce selection change 500ms before API call |

### Input Sanitization

| Input | Sanitization |
|-------|-------------|
| Tenant search query (admin) | Trim whitespace. No HTML. Max 100 chars. |
| Adjustment reason (admin) | Trim whitespace. No HTML. Max 500 chars. |
| Pricing cell value | Parse as integer. Reject non-numeric. Floor decimals. |
| URL params (`?status=`) | Whitelist allowed values: `success`, `upgraded`, `credits_purchased`, `cancelled`. Ignore unknown. |

### Stripe Security

| Concern | Handling |
|---------|---------|
| Payment data | Never handled by frontend — Stripe Checkout and Portal are hosted by Stripe |
| Checkout URLs | Generated server-side, short-lived, single-use |
| Portal URLs | Generated server-side, short-lived |
| Return URL tampering | Server validates subscription state independently of URL params |
| Webhook security | Backend responsibility — frontend never receives webhooks |

---

## Key Actions → Backend Use Cases Mapping

### Tenant Owner Actions

| Frontend Action | Backend Use Case | Endpoint | Triggers |
|----------------|-----------------|----------|----------|
| View billing overview | Load subscription + credit balance | `GET /api/billing/subscription` + `GET /api/billing/credits` | Page load |
| View available plans | List active plans | `GET /api/billing/plans` | Plan comparison page load |
| Subscribe to plan | BR-BILL-003: Create Subscription | `POST /api/billing/checkout` | [Subscribe] click |
| Upgrade plan | BR-BILL-004: Plan Upgrade | `POST /api/billing/checkout` | [Confirm Upgrade] click |
| Downgrade plan | BR-BILL-005: Plan Downgrade | `POST /api/billing/downgrade` | [Schedule Downgrade] click |
| Cancel pending downgrade | Clear pending_plan_id | `DELETE /api/billing/pending-downgrade` | [Cancel Downgrade] click |
| Cancel subscription | BR-BILL-006: Subscription Cancellation | `POST /api/billing/cancel` | [Cancel Subscription] click |
| Update payment method | Stripe Customer Portal | `GET /api/billing/portal` → redirect | [Update Payment] click |
| View transaction history | List credit transactions | `GET /api/billing/credits/transactions` | History page load |
| Purchase credit pack | BR-BILL-015: Credit Pack Purchase | `POST /api/billing/credits/purchase` | [Purchase] click |
| Estimate scan cost | BR-BILL-011: Credit Balance Check | `POST /api/billing/credits/estimate` | Scan creation: step/domain change |

### Super Admin Actions

| Frontend Action | Backend Use Case | Endpoint | Triggers |
|----------------|-----------------|----------|----------|
| Search tenants | List tenants by query | `GET /api/admin/tenants?q=` | Admin search input |
| View tenant credits | Load tenant credit balance | `GET /api/billing/credits` (tenant context) | Tenant selection |
| Adjust credits | BR-BILL-016: Manual Credit Adjustment | `PUT /api/admin/tenants/{id}/credits` | [Apply Adjustment] click |
| View pricing matrix | Load scan step pricing | `GET /api/admin/billing/pricing` | Pricing page load |
| Update pricing | BR-BILL-009: Scan Pricing Config | `PUT /api/admin/billing/pricing` | [Confirm Changes] click |

### System-Triggered UI Updates

| Backend Event | Frontend Effect | How Applied |
|--------------|----------------|-------------|
| `invoice.payment_failed` (webhook) | Show PAST_DUE banner, disable plan changes + credit purchases | Next API fetch reflects new status |
| `invoice.payment_succeeded` (webhook) | Remove PAST_DUE banner, refresh credits | Next API fetch reflects new status |
| `customer.subscription.deleted` (webhook) | Show free tier state, remove subscription UI | Next API fetch reflects EXPIRED status |
| `checkout.session.completed` (webhook) | Update credit balance (pack purchase) | Stripe return redirect triggers refetch |
| Credit allotment reset (background job) | New allotment balance, reset used count | Next API fetch reflects new period |
| Scan completion (consumption/refund) | Credit balance change | Next balance fetch reflects change |

---

## State Management Notes

### What State to Track

| State | Scope | Persistence |
|-------|-------|-------------|
| Current subscription | Global (header widget + banners) | Memory. Fetch on app load + navigation. |
| Credit balance | Global (header widget) | Memory. Fetch on app load + navigation. |
| Active banner type | Derived from subscription + credits | Computed, not stored. |
| Plans list | Billing pages only | Memory. Fetch once per session. |
| Selected billing interval | Plan comparison page | Component state. Default: MONTHLY. |
| Transaction filters | Transaction history page | Component state. Reset on page leave. |
| Transaction pagination | Transaction history page | Component state. Reset on filter change. |
| Admin selected tenant | Admin adjustment page | Component state. Reset on page leave. |
| Pricing matrix edits | Admin pricing page | Component state. Lost on navigation (with unsaved changes warning). |
| Low credits dismissed | Session | Memory. Reset on new session. |

### Reset Behavior

| Event | State Reset |
|-------|-------------|
| Stripe checkout return | Refetch subscription + credits + clear URL params |
| Plan change confirmed | Refetch subscription + credits |
| Subscription cancelled | Refetch subscription |
| Credit pack purchased | Refetch credits |
| Admin adjustment applied | Refetch target tenant credits. Clear form. |
| Admin pricing saved | Refetch pricing matrix. Clear modified flags. |
| Navigation away from billing | Clear page-level state (filters, pagination). Keep global state. |
| Logout | Clear all billing state |

### Unsaved Changes Guard

| Page | Guard Behavior |
|------|---------------|
| Admin pricing page | If `hasUnsavedChanges`, show browser confirm dialog on navigation: "You have unsaved pricing changes. Discard?" |
| All other billing pages | No guard needed (no local edits to lose) |
