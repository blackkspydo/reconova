<script lang="ts">
	import { goto } from '$app/navigation';
	import { getAuthStore } from '$lib/stores/auth';
	import { getToastStore } from '$lib/stores/toast';
	import { PasswordInput, PasswordStrength, Button, Alert, OtpInput } from '$lib/components/ui';
	import { validatePassword } from '$lib/utils/validation';
	import { api, authApi } from '$lib/api/client';
	import type { ApiError, TwoFactorSetupResponse } from '$lib/types/auth';
	import QRCode from 'qrcode';

	const auth = getAuthStore();
	const toast = getToastStore();

	// ── Change Password state ──
	let currentPassword = $state('');
	let newPassword = $state('');
	let confirmPassword = $state('');
	let confirmError = $state<string | null>(null);
	let passwordError = $state<string | null>(null);
	let isSubmitting = $state(false);
	let pwError = $state<string | null>(null);

	// ── 2FA setup state ──
	let show2faSetup = $state(false);
	let is2faLoading = $state(false);
	let twoFaError = $state<string | null>(null);
	let qrUri = $state('');
	let manualKey = $state('');
	let qrDataUrl = $state('');
	let showManualKey = $state(false);
	let otpCode = $state('');
	let isVerifying2fa = $state(false);

	// ── Logout state ──
	let isLoggingOut = $state(false);

	// ── Derived ──
	let passwordValidation = $derived(validatePassword(newPassword));

	let formattedRole = $derived(
		auth.user?.role === 'SUPER_ADMIN' ? 'Super Admin' : 'Tenant Owner'
	);

	let memberSince = $derived(
		auth.user?.createdAt
			? new Date(auth.user.createdAt).toLocaleDateString('en-US', {
					year: 'numeric',
					month: 'long',
					day: 'numeric',
				})
			: '—'
	);

	// ── QR code generation ──
	$effect(() => {
		if (qrUri) {
			QRCode.toDataURL(qrUri, {
				width: 200,
				margin: 1,
				color: { dark: '#000000', light: '#FFFFFF' },
			}).then((url: string) => {
				qrDataUrl = url;
			});
		}
	});

	// ── Password handlers ──
	function handleNewPasswordBlur() {
		if (newPassword && !passwordValidation.isValid) {
			passwordError = 'Password does not meet all requirements';
		} else {
			passwordError = null;
		}
	}

	function handleConfirmBlur() {
		if (confirmPassword && newPassword !== confirmPassword) {
			confirmError = "Passwords don't match";
		} else {
			confirmError = null;
		}
	}

	async function handleChangePassword(e: Event) {
		e.preventDefault();
		if (!currentPassword || !newPassword || !confirmPassword) return;

		if (!passwordValidation.isValid) {
			passwordError = 'Password does not meet all requirements';
			return;
		}

		if (newPassword !== confirmPassword) {
			confirmError = "Passwords don't match";
			return;
		}

		isSubmitting = true;
		pwError = null;

		try {
			await auth.changePassword(newPassword, currentPassword);
			toast.success('Password updated successfully.');
			currentPassword = '';
			newPassword = '';
			confirmPassword = '';
			passwordError = null;
			confirmError = null;
		} catch (err) {
			const apiErr = err as ApiError;
			pwError = apiErr.message || 'Failed to change password.';
		} finally {
			isSubmitting = false;
		}
	}

	// ── 2FA handlers ──
	async function handleEnable2fa() {
		show2faSetup = true;
		is2faLoading = true;
		twoFaError = null;
		qrDataUrl = '';
		qrUri = '';
		manualKey = '';
		otpCode = '';

		try {
			const res = (await api('/auth/2fa/setup')) as TwoFactorSetupResponse;
			qrUri = res.qrUri;
			manualKey = res.secret;
		} catch (err) {
			const apiErr = err as ApiError;
			twoFaError = apiErr.message || 'Failed to load 2FA setup. Please try again.';
		} finally {
			is2faLoading = false;
		}
	}

	async function handleVerify2fa(e: Event) {
		e.preventDefault();
		if (otpCode.length !== 6) return;

		isVerifying2fa = true;
		twoFaError = null;

		try {
			await api('/auth/2fa/verify', { method: 'POST', body: { totpCode: otpCode } });
			await auth.loadUser();
			show2faSetup = false;
			otpCode = '';
			toast.success('Two-factor authentication has been enabled.');
		} catch (err) {
			const apiErr = err as ApiError;
			twoFaError = apiErr.message || 'Invalid code. Check your authenticator and try again.';
			otpCode = '';
		} finally {
			isVerifying2fa = false;
		}
	}

	function cancel2faSetup() {
		show2faSetup = false;
		twoFaError = null;
		otpCode = '';
		qrUri = '';
		manualKey = '';
		qrDataUrl = '';
		showManualKey = false;
	}

	function copyKey() {
		navigator.clipboard.writeText(manualKey.replace(/\s/g, ''));
		toast.success('Key copied to clipboard.');
	}

	// ── Session handlers ──
	async function handleLogoutAll() {
		isLoggingOut = true;
		try {
			await auth.logout();
			goto('/auth/login');
		} catch {
			toast.error('Failed to sign out. Please try again.');
			isLoggingOut = false;
		}
	}
</script>

<svelte:head>
	<title>Settings — Reconova</title>
</svelte:head>

<div class="max-w-2xl">
	<h1 class="text-2xl font-bold text-white">Account Settings</h1>
	<p class="text-text-secondary mt-1 text-sm">Manage your account and security preferences</p>

	<!-- ═══════════════ Account Information ═══════════════ -->
	<div class="mt-8 bg-surface rounded-xl border border-[rgba(255,255,255,0.08)] p-6">
		<h2 class="text-lg font-semibold text-white mb-4">Account Information</h2>
		<div class="grid grid-cols-2 gap-y-4 gap-x-8">
			<div>
				<p class="text-xs text-text-muted uppercase tracking-wider mb-1">Email</p>
				<p class="text-sm text-white">{auth.user?.email ?? '—'}</p>
			</div>
			<div>
				<p class="text-xs text-text-muted uppercase tracking-wider mb-1">Role</p>
				<p class="text-sm text-white">{formattedRole}</p>
			</div>
			<div>
				<p class="text-xs text-text-muted uppercase tracking-wider mb-1">Tenant</p>
				<p class="text-sm text-white">{auth.user?.tenantId ?? '—'}</p>
			</div>
			<div>
				<p class="text-xs text-text-muted uppercase tracking-wider mb-1">Member Since</p>
				<p class="text-sm text-white">{memberSince}</p>
			</div>
		</div>
	</div>

	<!-- ═══════════════ Two-Factor Authentication ═══════════════ -->
	<div class="mt-6 bg-surface rounded-xl border border-[rgba(255,255,255,0.08)] p-6">
		<h2 class="text-lg font-semibold text-white mb-4">Two-Factor Authentication</h2>

		{#if auth.user?.twoFactorEnabled}
			<!-- 2FA is enabled -->
			<div class="flex items-center gap-3 mb-3">
				<span class="flex items-center gap-2">
					<span class="inline-block w-2.5 h-2.5 rounded-full bg-green-500"></span>
					<span class="text-sm text-green-400 font-medium">2FA is enabled</span>
				</span>
			</div>
			<p class="text-xs text-text-muted">
				Your account is protected with two-factor authentication. Contact your administrator to
				reset 2FA if needed.
			</p>
		{:else if show2faSetup}
			<!-- 2FA setup flow -->
			{#if is2faLoading}
				<div class="flex items-center justify-center py-10">
					<svg class="w-7 h-7 animate-spin text-brand" fill="none" viewBox="0 0 24 24">
						<circle
							class="opacity-25"
							cx="12"
							cy="12"
							r="10"
							stroke="currentColor"
							stroke-width="4"
						></circle>
						<path
							class="opacity-75"
							fill="currentColor"
							d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"
						></path>
					</svg>
				</div>
			{:else}
				{#if twoFaError}
					<Alert variant="error">{twoFaError}</Alert>
					<div class="mt-4"></div>
				{/if}

				<form class="flex flex-col gap-5" onsubmit={handleVerify2fa}>
					<!-- Step 1: Scan QR -->
					<div class="flex gap-3">
						<span
							class="flex items-center justify-center w-6 h-6 rounded-full bg-brand/20 text-brand text-xs font-bold shrink-0 mt-0.5"
							>1</span
						>
						<div class="flex-1">
							<p class="text-white text-sm font-medium">
								Scan this QR code with your authenticator app
							</p>
							<p class="text-text-muted text-xs mt-0.5">
								Google Authenticator, Authy, or similar
							</p>
							<div class="mt-3 flex justify-center">
								<div
									class="w-40 h-40 bg-white rounded-lg p-2 flex items-center justify-center"
								>
									{#if qrDataUrl}
										<img src={qrDataUrl} alt="QR Code" class="w-full h-full" />
									{:else}
										<div
											class="w-full h-full flex items-center justify-center text-gray-400 text-xs"
										>
											Loading...
										</div>
									{/if}
								</div>
							</div>

							<div class="mt-3">
								<button
									type="button"
									class="text-brand text-xs font-medium hover:text-brand-dark transition-colors"
									onclick={() => (showManualKey = !showManualKey)}
								>
									{showManualKey ? 'Hide manual key' : "Can't scan? Show manual key"}
								</button>

								{#if showManualKey}
									<div
										class="mt-2 flex items-center gap-2 bg-surface rounded-lg px-3 py-2 border border-[rgba(255,255,255,0.08)]"
									>
										<code class="text-xs text-text font-mono flex-1 tracking-wider"
											>{manualKey}</code
										>
										<button
											type="button"
											class="text-text-muted hover:text-white transition-colors"
											onclick={copyKey}
											aria-label="Copy key"
										>
											<svg
												class="w-4 h-4"
												fill="none"
												viewBox="0 0 24 24"
												stroke="currentColor"
												stroke-width="2"
											>
												<path
													stroke-linecap="round"
													stroke-linejoin="round"
													d="M15.666 3.888A2.25 2.25 0 0 0 13.5 2.25h-3c-1.03 0-1.9.693-2.166 1.638m7.332 0c.055.194.084.4.084.612v0a.75.75 0 0 1-.75.75H9.75a.75.75 0 0 1-.75-.75v0c0-.212.03-.418.084-.612m7.332 0c.646.049 1.288.11 1.927.184 1.1.128 1.907 1.077 1.907 2.185V19.5a2.25 2.25 0 0 1-2.25 2.25H6.75A2.25 2.25 0 0 1 4.5 19.5V6.257c0-1.108.806-2.057 1.907-2.185a48.208 48.208 0 0 1 1.927-.184"
												/>
											</svg>
										</button>
									</div>
								{/if}
							</div>
						</div>
					</div>

					<!-- Step 2: Enter code -->
					<div class="flex gap-3">
						<span
							class="flex items-center justify-center w-6 h-6 rounded-full bg-brand/20 text-brand text-xs font-bold shrink-0 mt-0.5"
							>2</span
						>
						<div class="flex-1">
							<p class="text-white text-sm font-medium mb-3">Enter the 6-digit code</p>
							<OtpInput bind:value={otpCode} />
						</div>
					</div>

					<div class="flex gap-3 mt-1">
						<Button
							type="submit"
							variant="primary"
							loading={isVerifying2fa}
							disabled={otpCode.length !== 6}
						>
							{isVerifying2fa ? 'Verifying...' : 'Verify & Enable'}
						</Button>
						<Button type="button" variant="secondary" onclick={cancel2faSetup}>
							Cancel
						</Button>
					</div>
				</form>
			{/if}
		{:else}
			<!-- 2FA is disabled -->
			<div class="flex items-center gap-3 mb-4">
				<span class="flex items-center gap-2">
					<span class="inline-block w-2.5 h-2.5 rounded-full bg-amber-500"></span>
					<span class="text-sm text-amber-400 font-medium">2FA is not enabled</span>
				</span>
			</div>
			<p class="text-xs text-text-muted mb-4">
				Add an extra layer of security to your account by enabling two-factor authentication.
			</p>
			<Button variant="primary" onclick={handleEnable2fa}>Enable 2FA</Button>
		{/if}
	</div>

	<!-- ═══════════════ Change Password ═══════════════ -->
	<div class="mt-6 bg-surface rounded-xl border border-[rgba(255,255,255,0.08)] p-6">
		<h2 class="text-lg font-semibold text-white mb-4">Change Password</h2>

		{#if pwError}
			<Alert variant="error">{pwError}</Alert>
			<div class="mt-4"></div>
		{/if}

		<form class="flex flex-col gap-4" onsubmit={handleChangePassword}>
			<PasswordInput
				label="Current Password"
				id="current-password"
				bind:value={currentPassword}
				placeholder="Enter current password"
			/>

			<div>
				<PasswordInput
					label="New Password"
					id="new-password"
					bind:value={newPassword}
					placeholder="Create a strong password"
					error={passwordError}
					onblur={handleNewPasswordBlur}
				/>
				<PasswordStrength password={newPassword} />
			</div>

			<PasswordInput
				label="Confirm New Password"
				id="confirm-new-password"
				bind:value={confirmPassword}
				placeholder="Re-enter new password"
				error={confirmError}
				onblur={handleConfirmBlur}
			/>

			<div class="mt-2">
				<Button type="submit" variant="primary" loading={isSubmitting}>
					{isSubmitting ? 'Updating...' : 'Update Password'}
				</Button>
			</div>
		</form>
	</div>

	<!-- ═══════════════ Active Sessions ═══════════════ -->
	<div class="mt-6 mb-8 bg-surface rounded-xl border border-[rgba(255,255,255,0.08)] p-6">
		<h2 class="text-lg font-semibold text-white mb-4">Active Sessions</h2>
		<p class="text-sm text-text-secondary mb-4">
			You are currently logged in. To sign out of all sessions, click below.
		</p>
		<Button variant="destructive" onclick={handleLogoutAll} loading={isLoggingOut}>
			{isLoggingOut ? 'Signing out...' : 'Sign Out All Sessions'}
		</Button>
	</div>
</div>
