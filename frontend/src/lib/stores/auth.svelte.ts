import { authApi } from '$lib/api/client';
import type {
	User,
	LoginResponse,
	RegisterResponse,
	TwoFactorSetupResponse,
	AuthTokenResponse,
	SuccessResponse,
} from '$lib/types/auth';

let user = $state<User | null>(null);
let isLoading = $state(true);
let error = $state<string | null>(null);
let tempToken = $state<string | null>(null);
let tempPurpose = $state<'password_change' | '2fa_setup' | '2fa_verify' | null>(null);

export function getAuthStore() {
	return {
		get user() { return user; },
		get isAuthenticated() { return !!user; },
		get isLoading() { return isLoading; },
		get error() { return error; },
		get tempToken() { return tempToken; },
		get tempPurpose() { return tempPurpose; },

		setTempToken(token: string | null, purpose: typeof tempPurpose = null) {
			tempToken = token;
			tempPurpose = purpose;
		},

		clearError() {
			error = null;
		},

		async loadUser() {
			isLoading = true;
			error = null;
			try {
				user = await authApi.me() as User;
			} catch {
				user = null;
			} finally {
				isLoading = false;
			}
		},

		async login(email: string, password: string, totpCode?: string) {
			error = null;
			const res = await authApi.login({ email, password, totpCode }) as LoginResponse;
			return res;
		},

		async register(email: string, password: string, tenantName: string) {
			error = null;
			const res = await authApi.register({ email, password, tenantName }) as RegisterResponse;
			tempToken = res.tempToken;
			tempPurpose = '2fa_setup';
			return res;
		},

		async get2faSetup() {
			if (!tempToken) throw new Error('No temp token');
			return await authApi.get2faSetup(tempToken) as TwoFactorSetupResponse;
		},

		async verify2fa(totpCode: string) {
			const res = await authApi.verify2fa({ totpCode }, tempToken ?? undefined) as AuthTokenResponse;
			tempToken = null;
			tempPurpose = null;
			await this.loadUser();
			return res;
		},

		async changePassword(newPassword: string, currentPassword?: string) {
			const res = await authApi.changePassword(
				{ newPassword, currentPassword },
				tempToken ?? undefined,
			) as SuccessResponse;
			tempToken = null;
			tempPurpose = null;
			return res;
		},

		async forgotPassword(email: string) {
			return await authApi.forgotPassword({ email }) as SuccessResponse;
		},

		async logout() {
			try {
				await authApi.logout();
			} finally {
				user = null;
				tempToken = null;
				tempPurpose = null;
			}
		},
	};
}
