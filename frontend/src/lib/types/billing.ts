export interface SubscriptionPlan {
	id: string;
	name: string;
	monthly_credits: number;
	max_domains: number;
	price_monthly: number;
	price_annual: number;
	features_json: Record<string, boolean>;
	status: 'ACTIVE' | 'DEPRECATED' | 'ARCHIVED';
}

export interface TenantSubscription {
	id: string;
	tenant_id: string;
	plan_id: string;
	plan: SubscriptionPlan;
	stripe_subscription_id: string | null;
	stripe_customer_id: string | null;
	status: 'ACTIVE' | 'PAST_DUE' | 'CANCELLED' | 'EXPIRED';
	billing_interval: 'MONTHLY' | 'ANNUAL';
	current_period_start: string;
	current_period_end: string | null;
	credits_remaining: number;
	credits_used_this_period: number;
	pending_plan_id: string | null;
	pending_plan: SubscriptionPlan | null;
}

export interface CreditBalance {
	allotment_remaining: number;
	allotment_total: number;
	purchased_balance: number;
	total_available: number;
	used_this_period: number;
	resets_at: string | null;
}

export interface CreditTransaction {
	id: string;
	tenant_id: string;
	amount: number;
	type: 'ALLOTMENT' | 'CONSUMPTION' | 'PURCHASE' | 'REFUND' | 'ADJUSTMENT';
	scan_job_id: string | null;
	description: string | null;
	created_by: string | null;
	created_at: string;
}

export interface CreditPack {
	id: string;
	name: string;
	credits: number;
	price: number;
	status: 'ACTIVE' | 'ARCHIVED';
}

export interface CheckoutRequest {
	plan_id: string;
	billing_interval: 'MONTHLY' | 'ANNUAL';
}
