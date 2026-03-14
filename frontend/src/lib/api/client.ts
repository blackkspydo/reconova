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
};
