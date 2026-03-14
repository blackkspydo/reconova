<script lang="ts">
	import { getAuthStore } from '$lib/stores/auth';
	import { PasswordInput, PasswordStrength, Button, Alert } from '$lib/components/ui';
	import type { ApiError } from '$lib/types/auth';

	const auth = getAuthStore();

	let currentPassword = $state('');
	let newPassword = $state('');
	let confirmPassword = $state('');
	let confirmError = $state<string | null>(null);
	let isSubmitting = $state(false);
	let error = $state<string | null>(null);
	let success = $state(false);

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
		if (newPassword !== confirmPassword) {
			confirmError = "Passwords don't match";
			return;
		}

		isSubmitting = true;
		error = null;
		success = false;

		try {
			await auth.changePassword(newPassword, currentPassword);
			success = true;
			currentPassword = '';
			newPassword = '';
			confirmPassword = '';
		} catch (err) {
			const apiErr = err as ApiError;
			error = apiErr.message || 'Failed to change password.';
		} finally {
			isSubmitting = false;
		}
	}
</script>

<svelte:head>
	<title>Settings — Reconova</title>
</svelte:head>

<div class="max-w-2xl">
	<h1 class="text-2xl font-bold text-white">Account Settings</h1>
	<p class="text-text-secondary mt-1 text-sm">Manage your account and security preferences</p>

	<!-- Account Info -->
	<div class="mt-8 bg-surface rounded-xl border border-[rgba(255,255,255,0.08)] p-6">
		<h2 class="text-lg font-semibold text-white mb-4">Account Information</h2>
		<div class="space-y-3">
			<div class="flex items-center justify-between">
				<span class="text-sm text-text-muted">Email</span>
				<span class="text-sm text-text">{auth.user?.email}</span>
			</div>
			<div class="flex items-center justify-between">
				<span class="text-sm text-text-muted">Role</span>
				<span class="text-sm text-text">{auth.user?.role === 'SUPER_ADMIN' ? 'Super Admin' : 'Tenant Owner'}</span>
			</div>
			<div class="flex items-center justify-between">
				<span class="text-sm text-text-muted">2FA Status</span>
				<span class="text-sm {auth.user?.twoFactorEnabled ? 'text-success' : 'text-warning'}">
					{auth.user?.twoFactorEnabled ? 'Enabled' : 'Disabled'}
				</span>
			</div>
			<div class="flex items-center justify-between">
				<span class="text-sm text-text-muted">Member since</span>
				<span class="text-sm text-text">{auth.user?.createdAt ? new Date(auth.user.createdAt).toLocaleDateString() : '—'}</span>
			</div>
		</div>
	</div>

	<!-- Change Password -->
	<div class="mt-6 bg-surface rounded-xl border border-[rgba(255,255,255,0.08)] p-6">
		<h2 class="text-lg font-semibold text-white mb-4">Change Password</h2>

		{#if success}
			<Alert variant="success">Password updated successfully.</Alert>
			<div class="mt-4"></div>
		{/if}

		{#if error}
			<Alert variant="error">{error}</Alert>
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
				/>
				<PasswordStrength password={newPassword} />
			</div>

			<PasswordInput
				label="Confirm New Password"
				id="confirm-new-password"
				bind:value={confirmPassword}
				placeholder="Re-enter new password"
				error={confirmError}
			/>

			<div class="mt-2">
				<Button type="submit" variant="primary" loading={isSubmitting}>
					{isSubmitting ? 'Updating...' : 'Update Password'}
				</Button>
			</div>
		</form>
	</div>
</div>
