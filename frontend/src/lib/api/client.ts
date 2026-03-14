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

	me: () => api('/users/me'),

	forgotPassword: (body: { email: string }) =>
		api('/auth/forgot-password', { method: 'POST', body, skipAuth: true }),

	changePassword: (body: { currentPassword?: string; newPassword: string }, tempToken?: string) =>
		api('/auth/change-password', {
			method: 'POST',
			body,
			headers: tempToken ? { Authorization: `Bearer ${tempToken}` } : {},
		}),

	get2faSetup: (tempToken: string) =>
		api('/auth/2fa/enable', {
			headers: { Authorization: `Bearer ${tempToken}` },
		}),

	verify2fa: (body: { totpCode: string }, tempToken?: string) =>
		api('/auth/2fa/verify', {
			method: 'POST',
			body,
			headers: tempToken ? { Authorization: `Bearer ${tempToken}` } : {},
		}),

	disable2fa: () => api('/auth/2fa/disable', { method: 'POST' }),

	getSessions: () => api('/auth/sessions'),

	revokeSession: (sessionId: string) => api(`/auth/sessions/${sessionId}`, { method: 'DELETE' }),

	logoutAll: () => api('/auth/logout-all', { method: 'POST' }),

	refresh: () => api('/auth/refresh', { method: 'POST', skipAuth: true }),
};

// Users API (tenant-level user management)
export const usersApi = {
	me: () => api('/users/me'),
	list: () => api('/users'),
	get: (id: string) => api(`/users/${id}`),
	create: (body: { email: string; role: string }) => api('/users', { method: 'POST', body }),
	update: (id: string, body: { role?: string }) => api(`/users/${id}`, { method: 'PUT', body }),
	delete: (id: string) => api(`/users/${id}`, { method: 'DELETE' }),
};

// Tenants API
export const tenantsApi = {
	getCurrent: () => api('/tenants/current'),
	updateCurrent: (body: { name?: string }) => api('/tenants/current', { method: 'PUT', body }),
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
	initiateVerification: (id: string) => api(`/domains/${id}/verify/initiate`, { method: 'POST' }),
	verify: (id: string) => api(`/domains/${id}/verify`, { method: 'POST' }),
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
	getVulnerabilities: (params?: Record<string, string | number | undefined>) => {
		const sp = new URLSearchParams();
		if (params) for (const [k, v] of Object.entries(params)) if (v !== undefined) sp.set(k, String(v));
		const qs = sp.toString();
		return api(`/scans/vulnerabilities${qs ? `?${qs}` : ''}`);
	},
	getVulnerability: (id: string) => api(`/scans/vulnerabilities/${id}`),
	resolveVulnerability: (id: string) => api(`/scans/vulnerabilities/${id}/resolve`, { method: 'POST' }),
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
	getTemplates: () => api('/workflows/templates'),
	getTemplate: (id: string) => api(`/workflows/templates/${id}`),
	createTemplate: (body: { name: string; steps_json: { check_type: string; config?: Record<string, unknown> }[] }) =>
		api('/workflows/templates', { method: 'POST', body }),
	updateTemplate: (id: string, body: { name?: string; steps_json?: { check_type: string; config?: Record<string, unknown> }[] }) =>
		api(`/workflows/templates/${id}`, { method: 'PUT', body }),
	deleteTemplate: (id: string) => api(`/workflows/templates/${id}`, { method: 'DELETE' }),
	execute: (body: { workflow_id: string; domain_id: string }) =>
		api('/workflows/execute', { method: 'POST', body }),
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

// CVE API
export const cveApi = {
	search: (params?: Record<string, string | number | undefined>) => {
		const sp = new URLSearchParams();
		if (params) for (const [k, v] of Object.entries(params)) if (v !== undefined) sp.set(k, String(v));
		const qs = sp.toString();
		return api(`/cve/search${qs ? `?${qs}` : ''}`);
	},
	get: (cveId: string) => api(`/cve/${cveId}`),
	getAlerts: (params?: Record<string, string | number | undefined>) => {
		const sp = new URLSearchParams();
		if (params) for (const [k, v] of Object.entries(params)) if (v !== undefined) sp.set(k, String(v));
		const qs = sp.toString();
		return api(`/cve/alerts${qs ? `?${qs}` : ''}`);
	},
	acknowledgeAlert: (id: string) => api(`/cve/alerts/${id}/acknowledge`, { method: 'POST' }),
	resolveAlert: (id: string) => api(`/cve/alerts/${id}/resolve`, { method: 'POST' }),
};

// Integrations API
export const integrationsApi = {
	list: () => api('/integrations'),
	get: (id: string) => api(`/integrations/${id}`),
	create: (body: { type: string; name: string; config: Record<string, unknown> }) =>
		api('/integrations', { method: 'POST', body }),
	update: (id: string, body: { name?: string; config?: Record<string, unknown>; enabled?: boolean }) =>
		api(`/integrations/${id}`, { method: 'PUT', body }),
	delete: (id: string) => api(`/integrations/${id}`, { method: 'DELETE' }),
	test: (id: string) => api(`/integrations/${id}/test`, { method: 'POST' }),
	getRules: () => api('/integrations/rules'),
	createRule: (body: { integration_id: string; event_type: string; severity_filter: string[] }) =>
		api('/integrations/rules', { method: 'POST', body }),
	updateRule: (id: string, body: { event_type?: string; severity_filter?: string[]; enabled?: boolean }) =>
		api(`/integrations/rules/${id}`, { method: 'PUT', body }),
	deleteRule: (id: string) => api(`/integrations/rules/${id}`, { method: 'DELETE' }),
};

// Compliance API
export const complianceApi = {
	getFrameworks: () => api('/compliance/frameworks'),
	getFramework: (id: string) => api(`/compliance/frameworks/${id}`),
	getControls: (frameworkId: string) => api(`/compliance/frameworks/${frameworkId}/controls`),
	runAssessment: (body: { framework_id: string }) =>
		api('/compliance/assessments', { method: 'POST', body }),
	getAssessments: (params?: Record<string, string | number | undefined>) => {
		const sp = new URLSearchParams();
		if (params) for (const [k, v] of Object.entries(params)) if (v !== undefined) sp.set(k, String(v));
		const qs = sp.toString();
		return api(`/compliance/assessments${qs ? `?${qs}` : ''}`);
	},
	getAssessment: (id: string) => api(`/compliance/assessments/${id}`),
};

// Admin Config API
export const adminConfigApi = {
	list: () => api('/admin/config'),
	get: (key: string) => api(`/admin/config/${key}`),
	reveal: (key: string) => api(`/admin/config/${key}/reveal`),
	update: (key: string, body: { value: string; reason?: string }) =>
		api(`/admin/config/${key}`, { method: 'PUT', body }),
	getHistory: (params?: Record<string, string | number | undefined>) => {
		const sp = new URLSearchParams();
		if (params) for (const [k, v] of Object.entries(params)) if (v !== undefined) sp.set(k, String(v));
		const qs = sp.toString();
		return api(`/admin/config/history${qs ? `?${qs}` : ''}`);
	},
	rollback: (id: string) => api(`/admin/config/history/${id}/rollback`, { method: 'POST' }),
	createChangeRequest: (body: { key: string; proposed_value: string; reason: string }) =>
		api('/admin/config/requests', { method: 'POST', body }),
	getChangeRequests: (params?: Record<string, string | undefined>) => {
		const sp = new URLSearchParams();
		if (params) for (const [k, v] of Object.entries(params)) if (v !== undefined) sp.set(k, String(v));
		const qs = sp.toString();
		return api(`/admin/config/requests${qs ? `?${qs}` : ''}`);
	},
	approveRequest: (id: string) => api(`/admin/config/requests/${id}/approve`, { method: 'POST' }),
	rejectRequest: (id: string) => api(`/admin/config/requests/${id}/reject`, { method: 'POST' }),
	getCacheStatus: () => api('/admin/config/cache/status'),
	invalidateCache: () => api('/admin/config/cache/invalidate', { method: 'POST' }),
};

// Admin Audit API
export const adminAuditApi = {
	list: (params?: Record<string, string | number | undefined>) => {
		const sp = new URLSearchParams();
		if (params) for (const [k, v] of Object.entries(params)) if (v !== undefined) sp.set(k, String(v));
		const qs = sp.toString();
		return api(`/admin/audit${qs ? `?${qs}` : ''}`);
	},
};

// Feature Flags API
export const featureFlagsApi = {
	evaluate: () => api('/feature-flags/evaluate'),
	evaluateFlag: (key: string) => api(`/feature-flags/evaluate/${key}`),
	list: () => api('/feature-flags'),
	create: (body: { key: string; name: string; description?: string; enabled: boolean; tier_gated: boolean; min_tier?: string }) =>
		api('/feature-flags', { method: 'POST', body }),
	update: (id: string, body: { name?: string; description?: string; enabled?: boolean; tier_gated?: boolean; min_tier?: string }) =>
		api(`/feature-flags/${id}`, { method: 'PUT', body }),
	delete: (id: string) => api(`/feature-flags/${id}`, { method: 'DELETE' }),
	getOverrides: (tenantId: string) => api(`/feature-flags/overrides/${tenantId}`),
	setOverride: (tenantId: string, body: { feature_flag_id: string; enabled: boolean }) =>
		api(`/feature-flags/overrides/${tenantId}`, { method: 'POST', body }),
	removeOverride: (tenantId: string, featureFlagId: string) =>
		api(`/feature-flags/overrides/${tenantId}/${featureFlagId}`, { method: 'DELETE' }),
};
