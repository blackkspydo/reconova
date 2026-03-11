# User Flows (Tenant Management)

Scope: All user journeys for tenant lifecycle — tenant-owner flows (provisioning, dashboard, domains, settings, billing) and super-admin flows (tenant list, detail, suspend/reactivate/delete, impersonation).

---

## Preconditions

| Flow Category | Required State |
|--------------|----------------|
| Provisioning Status | User just completed registration + 2FA. Has temp token. Tenant in `PROVISIONING`. |
| Tenant-Owner Flows (Dashboard, Domains, Settings, Billing) | Authenticated. Tenant `ACTIVE`. Valid JWT with `tenant_id`. |
| Free-Tier Blocked Flows | Authenticated. Tenant `ACTIVE`. Plan = `free`. |
| Admin Flows | Authenticated. Role = `SUPER_ADMIN`. |
| Impersonation Flows | Authenticated as `SUPER_ADMIN`. Target tenant `ACTIVE`. |

---

## Tenant-Owner Flows

### Flow 1: Provisioning Status

```
[Registration + 2FA Complete] -> redirect to /auth/provisioning
    | (page_view: provisioning_status)
    v
Display step checklist:
  done  Account created
  done  2FA configured
  ...   Preparing your database...
  ...   Applying configurations...
    |
    | Poll GET /api/tenants/me/status every 3 seconds
    v
Check response.status
    |-- "PROVISIONING" -> update step indicators, continue polling
    |-- "ACTIVE" -> (tenant_provisioned)
    |       v
    |   Auto-redirect to /(app)/dashboard
    |
    +-- "FAILED" (tenant_databases.status) -> (tenant_provisioning_failed)
            v
        Show error state:
          "Something went wrong setting up your workspace."
          "Our team has been notified and will resolve this shortly."
          [Contact Support] -> mailto:support@reconova.io
```

**Edge Cases:**
- User refreshes provisioning page -> re-check status, resume polling
- User navigates away and comes back -> re-check, if ACTIVE redirect to dashboard
- Polling exceeds 60 seconds -> show "This is taking longer than usual. Please wait." message
- User closes browser during provisioning -> next login attempt checks tenant status; if PROVISIONING -> redirect back to provisioning page

**Analytics Events:**

| Event | Parameters | When |
|-------|-----------|------|
| `provisioning_page_view` | `tenant_id` | Page load |
| `tenant_provisioned` | `tenant_id`, `duration_ms` | Status becomes ACTIVE |
| `tenant_provisioning_failed` | `tenant_id`, `duration_ms` | Status becomes FAILED |
| `provisioning_support_clicked` | `tenant_id` | User clicks Contact Support |

---

### Flow 2: Dashboard

```
[User navigates to /(app)/dashboard]
    | (dashboard_page_view)
    v
Check tenant plan
    |-- Free tier -> Show global upgrade banner at top
    +-- Paid tier -> No banner
    |
    v
Load dashboard data (parallel API calls):
  GET /api/dashboard/summary
    |
    v
Render overview cards:
  [Domains] [Scans] [Credits] [Compliance]
    |
    v
Render recent activity feed:
  - Latest scan results (last 5)
  - Quick actions: [New Scan] [Add Domain] [View Reports]
      |
      v
  Check plan for quick action buttons:
      |-- Free tier -> buttons disabled (grayed out)
      +-- Paid tier -> buttons enabled
```

**Card Click Behavior:**
- Domains card -> navigate to `/(app)/domains`
- Scans card -> navigate to `/(app)/scans` (covered in scanning plan)
- Credits card -> navigate to `/(app)/billing`
- Compliance card -> navigate to `/(app)/compliance` (covered in compliance plan)

**Dashboard States:**

| State | Behavior |
|-------|----------|
| Loading | Skeleton cards (4 placeholder cards with shimmer) |
| Success | Populated cards with data |
| Error (API failure) | Toast: "Failed to load dashboard data" + retry button |
| Empty (new tenant, no data) | Cards show 0 values + "Get started" prompts |

**Analytics Events:**

| Event | Parameters | When |
|-------|-----------|------|
| `dashboard_page_view` | `tenant_id`, `plan` | Page load |
| `dashboard_card_clicked` | `card_type` (domains/scans/credits/compliance) | Card click |
| `dashboard_quick_action_clicked` | `action` (new_scan/add_domain/view_reports) | Quick action click |
| `dashboard_upgrade_banner_clicked` | `tenant_id` | Banner upgrade link clicked |

---

### Flow 3: Domain Management

```
[User navigates to /(app)/domains]
    | (domains_page_view)
    v
Check plan
    |-- Free tier -> Show upgrade banner + disable [Add] form
    +-- Paid tier -> Show add form enabled
    |
    v
Load domains: GET /api/domains
    |
    v
Display domain list with count: "Domains (3/5)"
  Shows: domain name, date added, last scanned
    |
    | User types domain in inline form + clicks [Add]
    | (domain_add_submitted)
    v
Validate domain format (client-side):
    |-- Invalid format -> inline error: "Enter a valid domain (e.g., example.com)"
    +-- Valid -> POST /api/domains { name: "example.com" }
        |
        v
    Check response:
        |-- 201 Created -> (domain_added)
        |       v
        |   Add domain to list, clear form, show toast: "Domain added"
        |   Update count: "Domains (4/5)"
        |
        |-- 409 (duplicate) -> inline error: "This domain already exists"
        |
        |-- 403 ERR_TNT_009 -> Should not happen (form disabled on free tier)
        |       But if it does -> toast: "Upgrade required to add domains"
        |
        +-- 403 (max domains reached) -> inline error: "Domain limit reached (5/5). Upgrade for more."
```

**Domain Deletion Flow:**
```
[User clicks [Delete] on a domain]
    |
    v
Show confirmation dialog: "Delete example.com? Scan history for this domain will also be removed."
    |-- Cancel -> dismiss
    +-- Confirm -> DELETE /api/domains/{id}
        | (domain_deleted)
        v
    Remove from list, update count, toast: "Domain deleted"
```

**Domain Detail Navigation:**
```
[User clicks domain name in list]
    | (domain_detail_view)
    v
Navigate to /(app)/domains/[id]
  Show: domain name, date added, verification status [POST-MVP: pending/verified/failed]
  Show: scan history for this domain (list of past scans with status + date)
  Action: [Run Scan] (disabled on free tier) -> navigates to scan initiation (Section 4)
  Action: [Delete Domain] -> same deletion flow as above
```

**Analytics Events:**

| Event | Parameters | When |
|-------|-----------|------|
| `domains_page_view` | `tenant_id`, `domain_count`, `plan` | Page load |
| `domain_add_submitted` | `domain_name` | Add form submitted |
| `domain_added` | `domain_id`, `domain_name` | Successfully added |
| `domain_add_failed` | `error_code`, `domain_name` | Add rejected |
| `domain_deleted` | `domain_id`, `domain_name` | Successfully deleted |
| `domain_detail_view` | `domain_id` | Detail page view |

---

### Flow 4: Tenant Settings

```
[User navigates to /(app)/settings]
    | (settings_page_view)
    v
Load tenant info: GET /api/tenants/me
Load user info: GET /api/users/me
    |
    v
Display settings sections:

-- Organization --
  Tenant Name: [Acme Corp        ] [Save]
  Slug: acme-corp (read-only, shown as subdomain context)
  Created: March 1, 2026

-- Account --
  Email: john@acme.com (read-only)
  Role: Tenant Owner (read-only)
  2FA: Enabled (link to auth settings)
  [Change Password] -> /auth/change-password

-- Danger Zone --
  [Delete Account] -> triggers deletion request modal
```

**Tenant Name Update Flow:**
```
[User edits tenant name + clicks [Save]]
    | (tenant_name_update_submitted)
    v
Validate:
    |-- Empty -> inline error: "Organization name is required"
    |-- > 200 chars -> inline error: "Maximum 200 characters"
    +-- Valid -> PATCH /api/tenants/me { name: "New Name" }
        |
        v
    Check response:
        |-- 200 OK -> (tenant_name_updated)
        |       v
        |   Toast: "Organization name updated"
        |   Update display, update nav/header if name shown
        |
        +-- Error -> toast: "Failed to update organization name"
```

**Delete Account Flow:**
```
[User clicks [Delete Account]]
    | (delete_account_initiated)
    v
Open confirmation modal:
  "Delete Account"
  "This action cannot be undone."
  "Your data will be retained for 30 days, then permanently deleted."
  "Type '{tenant.slug}' to confirm:"
  [_____________]
  [Cancel] [Request Deletion] (disabled until slug matches)
    |
    |-- Cancel -> (delete_account_cancelled) -> dismiss
    |
    +-- Slug matches + click [Request Deletion]
        | (delete_account_requested)
        v
    POST /api/tenants/me/deletion-request
        |
        v
    Check response:
        |-- 200 OK -> dismiss modal
        |       v
        |   Toast: "Deletion request submitted. An admin will review it."
        |   Show pending banner on settings page:
        |   "Deletion request pending admin approval"
        |
        |-- 409 ERR_TNT_016 -> "Deletion request already pending"
        |
        +-- 409 ERR_TNT_015 -> "Account cannot be deleted in its current state"
```

**Analytics Events:**

| Event | Parameters | When |
|-------|-----------|------|
| `settings_page_view` | `tenant_id` | Page load |
| `tenant_name_update_submitted` | `old_name`, `new_name` | Name save clicked |
| `tenant_name_updated` | `tenant_id` | Successfully updated |
| `delete_account_initiated` | `tenant_id` | Delete button clicked |
| `delete_account_cancelled` | `tenant_id` | Cancel in modal |
| `delete_account_requested` | `tenant_id` | Deletion request submitted |

---

### Flow 5: Billing Overview

```
[User navigates to /(app)/billing]
    | (billing_page_view)
    v
Load billing data:
  GET /api/billing/subscription
  GET /api/billing/credits
  GET /api/billing/history
    |
    v
Display billing sections:

-- Current Plan --
  Plan: Professional ($99/mo)  [Change Plan]
  Status: Active
  Next billing date: April 1, 2026
  OR
  Plan: Free  [Upgrade]
  Status: Active (no billing)

-- Credit Balance --
  Credits remaining: 450 / 1000
  Credits used this period: 550
  [Purchase Credits] (links to Stripe checkout)

-- Usage This Period --
  Bar chart or simple table showing credit consumption by scan type

-- Billing History --
  Table: Date | Description | Amount | Status | Invoice
  [Load more] pagination
```

**Change Plan / Upgrade Flow:**
```
[User clicks [Change Plan] or [Upgrade]]
    | (billing_plans_view)
    v
Navigate to /(app)/billing/plans
    |
    v
Load plans: GET /api/billing/plans
    |
    v
Display plan comparison cards:
  [Free $0/mo] [Pro $99/mo] [Enterprise $299/mo]
  Each with feature list and [Current] or [Upgrade] button
    |
    | User clicks [Upgrade] on a plan
    | (plan_upgrade_initiated)
    v
POST /api/billing/checkout { plan_id }
    |
    v
Response: { checkout_url }
    |
    v
Redirect to Stripe checkout URL (external)
    |
    v
Stripe redirects back to /(app)/billing?status=success
    |-- success -> toast: "Plan upgraded successfully!"
    |           Reload billing data to reflect new plan
    +-- cancelled -> toast: "Upgrade cancelled"
```

**Analytics Events:**

| Event | Parameters | When |
|-------|-----------|------|
| `billing_page_view` | `tenant_id`, `plan` | Page load |
| `billing_plans_view` | `tenant_id`, `current_plan` | Plans page load |
| `plan_upgrade_initiated` | `current_plan`, `target_plan` | Upgrade button clicked |
| `plan_upgrade_completed` | `tenant_id`, `new_plan` | Return from Stripe with success |
| `credits_purchase_initiated` | `tenant_id` | Purchase credits clicked |
| `billing_history_viewed` | `tenant_id` | History section scrolled to / paginated |

---

### Flow 6: Blocked State Pages

```
[User attempts login with suspended/deactivated tenant]
    |
    v
Login API returns error
    |-- ERR_TNT_005 (suspended) -> redirect to /auth/suspended
    |       v
    |   Display:
    |     "Account Suspended"
    |     "Your account has been suspended. Contact support for more information."
    |     [Contact Support] -> mailto:support@reconova.io
    |     [Back to Login] -> /auth/login
    |
    |-- ERR_TNT_006 (deactivated) -> redirect to /auth/deactivated
    |       v
    |   Display:
    |     "Account Deactivated"
    |     "Your account has been permanently deactivated."
    |     "Data will be retained for 30 days from the deletion date."
    |     [Contact Support] -> mailto:support@reconova.io
    |
    +-- ERR_TNT_007 (provisioning) -> redirect to /auth/provisioning
            (covered in Flow 1)
```

**Mid-Session Suspension:**
```
[Tenant suspended while user is logged in]
    |
    v
Next API call returns 403 ERR_TNT_005
    |
    v
API client interceptor catches tenant status errors:
    |
    v
Clear local auth state (tokens, user data)
    |
    v
Redirect to /auth/suspended
```

**Analytics Events:**

| Event | Parameters | When |
|-------|-----------|------|
| `blocked_page_view` | `type` (suspended/deactivated/provisioning) | Page load |
| `blocked_support_clicked` | `type` | Support link clicked |

---

## Super Admin Flows

### Flow 7: Admin Tenant List

```
[Admin navigates to /(admin)/admin/tenants]
    | (admin_tenants_page_view)
    v
Load tenants: GET /api/admin/tenants?page=1&limit=20
    |
    v
Display tenant list table:
  Search: [Search by name or slug...         ]
  Filters: [All] [ACTIVE] [SUSPENDED] [PROVISIONING] [DEACTIVATED]

  | Tenant Name | Slug | Status | Plan | Created | Last Active |

  Click row -> navigate to tenant detail
    | (admin_tenant_selected)
    v
Navigate to /(admin)/admin/tenants/[id]
```

**Search & Filter Behavior:**
- Search debounced (300ms), searches `name` and `slug`
- Status filter updates URL query params for shareable links
- Results update without page reload

**Analytics Events:**

| Event | Parameters | When |
|-------|-----------|------|
| `admin_tenants_page_view` | `page`, `filter` | Page load |
| `admin_tenant_search` | `query` | Search submitted |
| `admin_tenant_filter_changed` | `filter` | Filter selection changed |
| `admin_tenant_selected` | `tenant_id` | Row clicked |

---

### Flow 8: Admin Tenant Detail

```
[Admin navigates to /(admin)/admin/tenants/[id]]
    | (admin_tenant_detail_view)
    v
Load tenant: GET /api/admin/tenants/{id}
    |
    v
Display tenant detail:

-- Tenant Info --
  Name, Slug, Status (badge), Plan, Created, Last Active, Owner email

-- Pending Actions --
  (shown if deletion_requested_at is not null)
  "Deletion requested on {date} by {email}"
  [Approve Deletion] [Deny Deletion]

-- Actions --
  (conditional on tenant status)
```

**Action Buttons by State:**

| Tenant Status | Available Actions |
|--------------|-------------------|
| `ACTIVE` | [Suspend] [Impersonate] |
| `ACTIVE` + deletion pending | [Approve Deletion] [Deny Deletion] [Suspend] [Impersonate] |
| `SUSPENDED` | [Reactivate] |
| `SUSPENDED` + deletion pending | [Approve Deletion] [Deny Deletion] [Reactivate] |
| `PROVISIONING` | (no actions — display "Provisioning in progress") |
| `PROVISIONING` + FAILED | [Retry Provisioning] [Delete Tenant] |
| `DEACTIVATED` | (no actions — display "Deactivated on {date}. Data retained until {date + 30 days}.") |

---

### Flow 9: Admin Suspend / Reactivate / Delete

**Suspend Flow:**
```
[Admin clicks [Suspend] on ACTIVE tenant]
    |
    v
Confirmation dialog:
  "Suspend {tenant_name}?"
  "This will immediately:"
  "- Log out all users"
  "- Cancel running scans"
  "- Disable scheduled scans"
  Reason: [___________________] (required)
  [Cancel] [Suspend Tenant]
    |
    +-- Confirm -> POST /api/admin/tenants/{id}/suspend { reason }
        | (admin_tenant_suspended)
        v
    Check response:
        |-- 200 OK -> Update status badge to SUSPENDED
        |           Toast: "Tenant suspended"
        |           Refresh action buttons
        +-- Error -> toast with error message
```

**Reactivate Flow:**
```
[Admin clicks [Reactivate] on SUSPENDED tenant]
    |
    v
Confirmation dialog:
  "Reactivate {tenant_name}?"
  "The tenant owner will be able to log in again."
  "Note: Scheduled scans and integrations will remain disabled."
  [Cancel] [Reactivate]
    |
    +-- Confirm -> POST /api/admin/tenants/{id}/reactivate
        | (admin_tenant_reactivated)
        v
    200 OK -> Update status badge to ACTIVE
             Toast: "Tenant reactivated"
```

**Approve/Deny Deletion Flow:**
```
[Admin clicks [Approve Deletion]]
    |
    v
Confirmation dialog:
  "Approve deletion of {tenant_name}?"
  "This is irreversible. The tenant will be deactivated and data deleted after 30 days."
  [Cancel] [Approve]
    |
    +-- Confirm -> POST /api/admin/tenants/{id}/deletion { action: "approve" }
        | (admin_tenant_deletion_approved)
        v
    200 OK -> Update status badge to DEACTIVATED
             Toast: "Tenant deactivated. Data will be deleted in 30 days."
             Action buttons cleared

[Admin clicks [Deny Deletion]]
    |
    v
POST /api/admin/tenants/{id}/deletion { action: "deny" }
    | (admin_tenant_deletion_denied)
    v
200 OK -> Remove pending deletion banner
         Toast: "Deletion request denied"
```

**Analytics Events:**

| Event | Parameters | When |
|-------|-----------|------|
| `admin_tenant_detail_view` | `tenant_id`, `tenant_status` | Page load |
| `admin_tenant_suspended` | `tenant_id`, `reason` | Suspend confirmed |
| `admin_tenant_reactivated` | `tenant_id` | Reactivate confirmed |
| `admin_tenant_deletion_approved` | `tenant_id` | Deletion approved |
| `admin_tenant_deletion_denied` | `tenant_id` | Deletion denied |

---

### Flow 10: Admin Impersonation

```
[Admin clicks [Impersonate] on ACTIVE tenant detail]
    | (admin_impersonation_initiated)
    v
Confirmation dialog:
  "Impersonate {user_email}?"
  "You will view the platform as this user for up to 1 hour."
  "All actions will be logged."
  [Cancel] [Start Impersonation]
    |
    +-- Confirm -> POST /api/admin/tenants/{id}/impersonate
        |
        v
    Response: { token, refresh_token, expires_in, impersonation: true }
        |
        v
    Store impersonation tokens in sessionStorage (not localStorage)
        |
        v
    Open new browser tab: /(app)/dashboard
    (new tab uses impersonation tokens)
        |
        v
    In new tab:
      Impersonation banner shown at top:
      "Viewing as: {user_email} ({tenant_name})"
      "Session expires in: mm:ss  [End Impersonation]"
        |
        | Countdown timer shows remaining session time
        |
        v
    Impersonation ends when:
        |-- Admin clicks [End Impersonation]
        |       v
        |   DELETE /api/auth/session (impersonation session)
        |   Clear impersonation tokens
        |   Close tab (or redirect to a "session ended" page)
        |
        |-- Session expires (1 hour)
        |       v
        |   Token refresh fails (session expired server-side)
        |   Show: "Impersonation session expired"
        |   Close tab (or redirect)
        |
        +-- Another impersonation started (previous invalidated)
                v
            Next API call fails -> same as expiry behavior
```

**Impersonation Restrictions:**
- Impersonation banner is ALWAYS visible — cannot be dismissed
- Write actions are allowed (super admin viewing as user)
- Settings page shows "Viewing as impersonated user" in danger zone — delete account not available during impersonation

**Analytics Events:**

| Event | Parameters | When |
|-------|-----------|------|
| `admin_impersonation_initiated` | `tenant_id`, `target_user_id` | Confirm clicked |
| `admin_impersonation_ended` | `tenant_id`, `reason` (manual/expired/replaced) | Session ended |
| `impersonation_action_performed` | `action`, `tenant_id` | Any action during impersonation |
