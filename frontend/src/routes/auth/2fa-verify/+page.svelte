<script lang="ts">
	import { goto } from '$app/navigation';
	import { AuthLogo, AuthGlow, AuthCard } from '$lib/components/auth';
	import { OtpInput, Button, Alert } from '$lib/components/ui';
	import { getAuthStore } from '$lib/stores/auth';
	import type { ApiError } from '$lib/types/auth';

	const auth = getAuthStore();

	let otpCode = $state('');
	let isSubmitting = $state(false);
	let error = $state<string | null>(null);

	async function handleSubmit(e: Event) {
		e.preventDefault();
		if (otpCode.length !== 6) return;

		isSubmitting = true;
		error = null;

		try {
			await auth.verify2fa(otpCode);
			goto('/dashboard');
		} catch (err) {
			error = (err as ApiError).message ?? 'Invalid code. Try again.';
			otpCode = '';
		} finally {
			isSubmitting = false;
		}
	}
</script>

<svelte:head>
	<title>Verify 2FA — Reconova</title>
</svelte:head>

<AuthGlow variant="2fa-verify" />

<div class="w-full max-w-md flex flex-col gap-8 relative z-10">
	<AuthLogo />

	<AuthCard>
		<div class="text-center">
			<h2 class="text-white text-[28px] font-bold leading-tight">Two-factor authentication</h2>
			<p class="text-text-muted mt-2 text-sm">Enter the 6-digit code from your authenticator app</p>
		</div>

		{#if error}
			<Alert variant="error">{error}</Alert>
		{/if}

		<form class="flex flex-col gap-6" onsubmit={handleSubmit}>
			<OtpInput bind:value={otpCode} error={error} />

			<Button type="submit" variant="primary" fullWidth loading={isSubmitting} disabled={otpCode.length !== 6}>
				{isSubmitting ? 'Verifying...' : 'Verify'}
			</Button>
		</form>

		<div class="text-center text-sm text-text-muted">
			Having trouble?
			<a class="text-brand font-medium hover:text-brand-dark transition-colors" href="mailto:support@reconova.io">Contact support</a>
		</div>
	</AuthCard>
</div>
