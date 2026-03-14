<script lang="ts">
	import { AuthLogo, AuthGlow, AuthCard } from '$lib/components/auth';
	import { PasswordInput, TextInput, Button, Alert } from '$lib/components/ui';

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
			// TODO: POST /api/auth/login
			console.log('Login:', { email, password });
		} catch {
			error = 'Invalid email or password';
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
