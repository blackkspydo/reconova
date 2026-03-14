<script lang="ts">
	import { page } from '$app/state';
	import { AuthLogo, AuthGlow, AuthCard } from '$lib/components/auth';
	import { PasswordInput, PasswordStrength, Button, Alert } from '$lib/components/ui';

	let newPassword = $state('');
	let confirmPassword = $state('');
	let confirmError = $state<string | null>(null);
	let isSubmitting = $state(false);
	let error = $state<string | null>(null);

	// Forced change shows info banner; voluntary change (from settings) won't have this
	let isForced = $derived(page.url.searchParams.has('expired') || true);

	function handleConfirmBlur() {
		if (confirmPassword && newPassword !== confirmPassword) {
			confirmError = "Passwords don't match";
		} else {
			confirmError = null;
		}
	}

	async function handleSubmit(e: Event) {
		e.preventDefault();
		if (!newPassword || !confirmPassword) return;
		if (newPassword !== confirmPassword) {
			confirmError = "Passwords don't match";
			return;
		}

		isSubmitting = true;
		error = null;

		try {
			// TODO: POST /api/auth/password/change { new_password }
			console.log('Change password:', { newPassword });
		} catch {
			error = 'Something went wrong. Please try again.';
		} finally {
			isSubmitting = false;
		}
	}
</script>

<svelte:head>
	<title>Change Password — Reconova</title>
</svelte:head>

<AuthGlow variant="change-password" />

<div class="w-full max-w-md flex flex-col gap-8 relative z-10">
	<AuthLogo />

	<AuthCard>
		<div class="text-center">
			<h2 class="text-white text-[28px] font-bold leading-tight">Change your password</h2>
			<p class="text-text-muted mt-2 text-sm">Set a new secure password for your account</p>
		</div>

		{#if isForced}
			<Alert variant="info">
				Your password has expired. Please set a new password to continue.
			</Alert>
		{/if}

		{#if error}
			<Alert variant="error">{error}</Alert>
		{/if}

		<form class="flex flex-col gap-5" onsubmit={handleSubmit}>
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
				id="confirm-password"
				bind:value={confirmPassword}
				placeholder="Re-enter your new password"
				error={confirmError}
			/>

			<Button type="submit" variant="primary" fullWidth loading={isSubmitting}>
				{isSubmitting ? 'Updating...' : 'Set New Password'}
			</Button>
		</form>
	</AuthCard>
</div>
