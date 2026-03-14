import { goto } from '$app/navigation';
import type { ApiError } from '$lib/types/auth';

const API_BASE = '/api';

let isRefreshing = false;
let refreshQueue: Array<{ resolve: () => void; reject: (err: unknown) => void }> = [];

function processQueue(error: unknown = null) {
	for (const { resolve, reject } of refreshQueue) {
		if (error) reject(error);
		else resolve();
	}
	refreshQueue = [];
}

async function refreshToken(): Promise<boolean> {
	try {
		const res = await fetch(`${API_BASE}/auth/refresh`, {
			method: 'POST',
			credentials: 'include',
		});
		return res.ok;
	} catch {
		return false;
	}
}

async function handleUnauthorized(): Promise<boolean> {
	if (isRefreshing) {
		return new Promise((resolve, reject) => {
			refreshQueue.push({
				resolve: () => resolve(true),
				reject: () => reject(false),
			});
		});
	}

	isRefreshing = true;
	const success = await refreshToken();
	isRefreshing = false;

	if (success) {
		processQueue();
		return true;
	} else {
		processQueue(new Error('Refresh failed'));
		goto('/auth/login');
		return false;
	}
}

async function parseError(res: Response): Promise<ApiError> {
	try {
		return await res.json();
	} catch {
		return {
			code: 'ERR_UNKNOWN',
			status: res.status,
			message: res.statusText || 'An unexpected error occurred',
			requestId: '',
			timestamp: new Date().toISOString(),
		};
	}
}

interface RequestOptions {
	method?: string;
	body?: unknown;
	headers?: Record<string, string>;
	skipAuth?: boolean;
}

export async function api<T>(endpoint: string, options: RequestOptions = {}): Promise<T> {
	const { method = 'GET', body, headers = {}, skipAuth = false } = options;

	const config: RequestInit = {
		method,
		credentials: 'include',
		headers: {
			'Content-Type': 'application/json',
			...headers,
		},
	};

	if (body) {
		config.body = JSON.stringify(body);
	}

	let res = await fetch(`${API_BASE}${endpoint}`, config);

	if (res.status === 401 && !skipAuth) {
		const refreshed = await handleUnauthorized();
		if (refreshed) {
			res = await fetch(`${API_BASE}${endpoint}`, config);
		}
	}

	if (!res.ok) {
		throw await parseError(res);
	}

	const text = await res.text();
	return text ? JSON.parse(text) : ({} as T);
}

// Convenience methods
export const authApi = {
	login: (body: { email: string; password: string; totpCode?: string }) =>
		api('/auth/login', { method: 'POST', body, skipAuth: true }),

	register: (body: { email: string; password: string; tenantName: string }) =>
		api('/auth/register', { method: 'POST', body, skipAuth: true }),

	logout: () => api('/auth/logout', { method: 'POST' }),

	me: () => api('/auth/me'),

	forgotPassword: (body: { email: string }) =>
		api('/auth/password/forgot', { method: 'POST', body, skipAuth: true }),

	changePassword: (body: { currentPassword?: string; newPassword: string }, tempToken?: string) =>
		api('/auth/password/change', {
			method: 'POST',
			body,
			headers: tempToken ? { Authorization: `Bearer ${tempToken}` } : {},
		}),

	get2faSetup: (tempToken: string) =>
		api('/auth/2fa/setup', {
			headers: { Authorization: `Bearer ${tempToken}` },
		}),

	verify2fa: (body: { totpCode: string }, tempToken?: string) =>
		api('/auth/2fa/verify', {
			method: 'POST',
			body,
			headers: tempToken ? { Authorization: `Bearer ${tempToken}` } : {},
		}),

	refresh: () => api('/auth/refresh', { method: 'POST', skipAuth: true }),
};

export const adminApi = {
	getUsers: (params?: Record<string, string | number | boolean | undefined>) => {
		const searchParams = new URLSearchParams();
		if (params) {
			for (const [key, val] of Object.entries(params)) {
				if (val !== undefined) searchParams.set(key, String(val));
			}
		}
		const qs = searchParams.toString();
		return api(`/admin/users${qs ? `?${qs}` : ''}`);
	},

	getUser: (id: string) => api(`/admin/users/${id}`),

	deactivateUser: (id: string) => api(`/admin/users/${id}/deactivate`, { method: 'POST' }),

	enableUser: (id: string) => api(`/admin/users/${id}/enable`, { method: 'POST' }),

	unlockUser: (id: string) => api(`/admin/users/${id}/unlock`, { method: 'POST' }),

	reset2fa: (id: string) => api(`/admin/users/${id}/reset-2fa`, { method: 'POST' }),

	// Tenant management
	getTenants: (params?: Record<string, string | number | boolean | undefined>) => {
		const sp = new URLSearchParams();
		if (params) for (const [k, v] of Object.entries(params)) if (v !== undefined) sp.set(k, String(v));
		const qs = sp.toString();
		return api(`/admin/tenants${qs ? `?${qs}` : ''}`);
	},
	getTenant: (id: string) => api(`/admin/tenants/${id}`),
	suspendTenant: (id: string, body: { reason: string }) => api(`/admin/tenants/${id}/suspend`, { method: 'POST', body }),
	reactivateTenant: (id: string) => api(`/admin/tenants/${id}/reactivate`, { method: 'POST' }),
	approveDeletion: (id: string) => api(`/admin/tenants/${id}/deletion`, { method: 'POST', body: { action: 'approve' } }),
	denyDeletion: (id: string) => api(`/admin/tenants/${id}/deletion`, { method: 'POST', body: { action: 'deny' } }),
	impersonateTenant: (id: string) => api(`/admin/tenants/${id}/impersonate`, { method: 'POST' }),

	// Pricing
	getPricing: () => api('/admin/billing/pricing'),
	updatePricing: (body: { updates: { check_type: string; tier_id: string; credits_per_domain: number }[] }) =>
		api('/admin/billing/pricing', { method: 'PUT', body }),

	// Credit adjustments
	adjustCredits: (tenantId: string, body: { amount: number; reason: string }) =>
		api(`/admin/tenants/${tenantId}/credits`, { method: 'PUT', body }),
};

// Domain API
export const domainApi = {
	list: () => api('/domains'),
	get: (id: string) => api(`/domains/${id}`),
	create: (body: { domain: string }) => api('/domains', { method: 'POST', body }),
	delete: (id: string) => api(`/domains/${id}`, { method: 'DELETE' }),
	getSubdomains: (id: string, params?: { page?: number; size?: number }) => {
		const sp = new URLSearchParams();
		if (params?.page) sp.set('page', String(params.page));
		if (params?.size) sp.set('size', String(params.size));
		const qs = sp.toString();
		return api(`/domains/${id}/subdomains${qs ? `?${qs}` : ''}`);
	},
	getPorts: (id: string, params?: { page?: number; size?: number }) => {
		const sp = new URLSearchParams();
		if (params?.page) sp.set('page', String(params.page));
		if (params?.size) sp.set('size', String(params.size));
		const qs = sp.toString();
		return api(`/domains/${id}/ports${qs ? `?${qs}` : ''}`);
	},
	getTechnologies: (id: string, params?: { page?: number; size?: number }) => {
		const sp = new URLSearchParams();
		if (params?.page) sp.set('page', String(params.page));
		if (params?.size) sp.set('size', String(params.size));
		const qs = sp.toString();
		return api(`/domains/${id}/technologies${qs ? `?${qs}` : ''}`);
	},
	getScans: (id: string, params?: { page?: number; size?: number }) => {
		const sp = new URLSearchParams();
		if (params?.page) sp.set('page', String(params.page));
		if (params?.size) sp.set('size', String(params.size));
		const qs = sp.toString();
		return api(`/domains/${id}/scans${qs ? `?${qs}` : ''}`);
	},
};

// Scan API
export const scanApi = {
	list: (params?: Record<string, string | number | undefined>) => {
		const sp = new URLSearchParams();
		if (params) for (const [k, v] of Object.entries(params)) if (v !== undefined) sp.set(k, String(v));
		const qs = sp.toString();
		return api(`/scans${qs ? `?${qs}` : ''}`);
	},
	get: (id: string) => api(`/scans/${id}`),
	create: (body: { domain_id: string; workflow_id: string }) => api('/scans', { method: 'POST', body }),
	cancel: (id: string) => api(`/scans/${id}`, { method: 'DELETE' }),
	getResults: (id: string, checkType?: string) => {
		const qs = checkType ? `?check_type=${checkType}` : '';
		return api(`/scans/${id}/results${qs}`);
	},
};

// Workflow API
export const workflowApi = {
	list: () => api('/workflows'),
	get: (id: string) => api(`/workflows/${id}`),
	create: (body: { name: string; steps_json: { check_type: string; config?: Record<string, unknown> }[] }) =>
		api('/workflows', { method: 'POST', body }),
	update: (id: string, body: { name?: string; steps_json?: { check_type: string; config?: Record<string, unknown> }[] }) =>
		api(`/workflows/${id}`, { method: 'PUT', body }),
	delete: (id: string) => api(`/workflows/${id}`, { method: 'DELETE' }),
};

// Schedule API
export const scheduleApi = {
	list: () => api('/scans/schedules'),
	get: (id: string) => api(`/scans/schedules/${id}`),
	create: (body: { domain_id: string; workflow_id: string; cron_expression: string }) =>
		api('/scans/schedules', { method: 'POST', body }),
	update: (id: string, body: { workflow_id?: string; cron_expression?: string }) =>
		api(`/scans/schedules/${id}`, { method: 'PUT', body }),
	delete: (id: string) => api(`/scans/schedules/${id}`, { method: 'DELETE' }),
	enable: (id: string) => api(`/scans/schedules/${id}/enable`, { method: 'POST' }),
	disable: (id: string) => api(`/scans/schedules/${id}/disable`, { method: 'POST' }),
};

// Billing API
export const billingApi = {
	getPlans: () => api('/billing/plans'),
	getSubscription: () => api('/billing/subscription'),
	getCredits: () => api('/billing/credits'),
	getTransactions: (params?: Record<string, string | number | undefined>) => {
		const sp = new URLSearchParams();
		if (params) for (const [k, v] of Object.entries(params)) if (v !== undefined) sp.set(k, String(v));
		const qs = sp.toString();
		return api(`/billing/credits/transactions${qs ? `?${qs}` : ''}`);
	},
	checkout: (body: { plan_id: string; billing_interval: 'MONTHLY' | 'ANNUAL' }) =>
		api('/billing/checkout', { method: 'POST', body }),
	downgrade: (body: { plan_id: string }) => api('/billing/downgrade', { method: 'POST', body }),
	cancelDowngrade: () => api('/billing/pending-downgrade', { method: 'DELETE' }),
	cancelSubscription: () => api('/billing/cancel', { method: 'POST' }),
	getPortal: () => api('/billing/portal'),
	getCreditPacks: () => api('/billing/credit-packs'),
	purchaseCredits: (body: { credit_pack_id: string }) =>
		api('/billing/credits/purchase', { method: 'POST', body }),
	estimateCost: (body: { workflow_steps: string[]; domain_count: number }) =>
		api('/billing/credits/estimate', { method: 'POST', body }),
};
