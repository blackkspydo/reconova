<script lang="ts">
	import { goto } from '$app/navigation';
	import { AuthLogo, AuthGlow, AuthCard } from '$lib/components/auth';
	import { PasswordInput, TextInput, Button, Alert } from '$lib/components/ui';
	import { getAuthStore } from '$lib/stores/auth';
	import type { ApiError } from '$lib/types/auth';

	const auth = getAuthStore();

	let email = $state('');
	let password = $state('');
	let isSubmitting = $state(false);
	let error = $state<string | null>(null);

	async function handleSubmit(e: Event) {
		e.preventDefault();
		if (!email || !password) return;

		isSubmitting = true;
		error = null;

		try {
			const res = await auth.login(email, password);

			if (res.requiresTwoFactor) {
				goto('/auth/2fa-verify');
			} else if (res.requires2faSetup) {
				auth.setTempToken(res.tempToken!, '2fa_setup');
				goto('/auth/2fa-setup');
			} else if (res.requiresPasswordChange) {
				auth.setTempToken(res.tempToken!, 'password_change');
				goto('/auth/change-password?expired');
			} else {
				await auth.loadUser();
				goto('/dashboard');
			}
		} catch (err) {
			if ((err as ApiError).message) {
				error = (err as ApiError).message;
			} else {
				error = 'An unexpected error occurred. Please try again.';
			}
		} finally {
			isSubmitting = false;
		}
	}
</script>

<svelte:head>
	<title>Log In — Reconova</title>
</svelte:head>

<AuthGlow variant="login" />

<div class="w-full max-w-md flex flex-col gap-8 relative z-10">
	<AuthLogo />

	<AuthCard>
		<div class="text-center">
			<h2 class="text-white text-[28px] font-bold leading-tight">Welcome back</h2>
			<p class="text-text-muted mt-2 text-sm">Sign in to your account to continue</p>
		</div>

		{#if error}
			<Alert variant="error">{error}</Alert>
		{/if}

		<form class="flex flex-col gap-6" onsubmit={handleSubmit}>
			<TextInput
				label="Email"
				id="email"
				type="email"
				bind:value={email}
				placeholder="you@company.com"
				required
			/>

			<PasswordInput
				label="Password"
				id="password"
				bind:value={password}
			>
				{#snippet labelRight()}
					<a class="text-brand hover:text-brand-dark text-sm font-medium transition-colors" href="/auth/forgot-password">
						Forgot password?
					</a>
				{/snippet}
			</PasswordInput>

			<Button type="submit" variant="primary" fullWidth loading={isSubmitting}>
				{isSubmitting ? 'Logging in...' : 'Log In'}
			</Button>
		</form>

		<div class="text-center text-sm text-text-muted">
			Don't have an account?
			<a class="text-brand font-medium hover:text-brand-dark transition-colors" href="/auth/register">Register</a>
		</div>
	</AuthCard>
</div>
