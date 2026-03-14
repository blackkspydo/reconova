<script lang="ts">
	import { AuthLogo, AuthGlow, AuthCard } from '$lib/components/auth';
	import { TextInput, Button, Alert } from '$lib/components/ui';

	let email = $state('');
	let isSubmitting = $state(false);
	let isSuccess = $state(false);
	let error = $state<string | null>(null);

	async function handleSubmit(e: Event) {
		e.preventDefault();
		if (!email) return;

		isSubmitting = true;
		error = null;

		try {
			// TODO: POST /api/auth/password/forgot { email }
			console.log('Forgot password:', email);
			isSuccess = true;
		} catch {
			error = 'Something went wrong. Please try again.';
		} finally {
			isSubmitting = false;
		}
	}
</script>

<svelte:head>
	<title>Forgot Password — Reconova</title>
</svelte:head>

<AuthGlow variant="forgot-password" />

<div class="w-full max-w-md flex flex-col gap-8 relative z-10">
	<AuthLogo />

	<AuthCard>
		{#if isSuccess}
			<!-- Success state -->
			<div class="text-center">
				<h2 class="text-white text-[28px] font-bold leading-tight">Check your email</h2>
			</div>

			<Alert variant="success">
				If an account exists for that email, we've sent reset instructions.
			</Alert>

			<a href="/auth/login" class="text-brand font-medium text-sm hover:text-brand-dark transition-colors flex items-center gap-1.5 justify-center">
				<svg class="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
					<path stroke-linecap="round" stroke-linejoin="round" d="M10.5 19.5 3 12m0 0 7.5-7.5M3 12h18" />
				</svg>
				Back to Login
			</a>
		{:else}
			<!-- Default state -->
			<div class="text-center">
				<h2 class="text-white text-[28px] font-bold leading-tight">Forgot your password?</h2>
				<p class="text-text-muted mt-2 text-sm">Enter your email and we'll send you instructions to reset your password.</p>
			</div>

			{#if error}
				<Alert variant="error">{error}</Alert>
			{/if}

			<form class="flex flex-col gap-6" onsubmit={handleSubmit}>
				<TextInput
					label="Email"
					id="forgot-email"
					type="email"
					bind:value={email}
					placeholder="you@company.com"
					required
				/>

				<Button type="submit" variant="primary" fullWidth loading={isSubmitting}>
					{isSubmitting ? 'Sending...' : 'Send Reset Link'}
				</Button>
			</form>

			<a href="/auth/login" class="text-brand font-medium text-sm hover:text-brand-dark transition-colors flex items-center gap-1.5 justify-center">
				<svg class="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
					<path stroke-linecap="round" stroke-linejoin="round" d="M10.5 19.5 3 12m0 0 7.5-7.5M3 12h18" />
				</svg>
				Back to Login
			</a>
		{/if}
	</AuthCard>
</div>
