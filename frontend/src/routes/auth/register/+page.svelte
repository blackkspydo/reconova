<script lang="ts">
	import { goto } from '$app/navigation';
	import { AuthLogo, AuthGlow, AuthCard } from '$lib/components/auth';
	import { TextInput, PasswordInput, PasswordStrength, Button, StepIndicator, Alert } from '$lib/components/ui';
	import { getAuthStore } from '$lib/stores/auth';
	import type { ApiError } from '$lib/types/auth';

	const auth = getAuthStore();

	let currentStep = $state<1 | 2>(1);
	let isSubmitting = $state(false);
	let error = $state<string | null>(null);

	// Step 1
	let email = $state('');
	let password = $state('');
	let confirmPassword = $state('');
	let confirmError = $state<string | null>(null);

	// Step 2
	let tenantName = $state('');

	function validateStep1() {
		confirmError = null;
		if (password && confirmPassword && password !== confirmPassword) {
			confirmError = "Passwords don't match";
			return false;
		}
		if (!email || !password || !confirmPassword) return false;
		if (password.length < 12) return false;
		return true;
	}

	function handleNext(e: Event) {
		e.preventDefault();
		if (!validateStep1()) return;
		currentStep = 2;
	}

	function handleBack() {
		currentStep = 1;
	}

	async function handleSubmit(e: Event) {
		e.preventDefault();
		if (!tenantName) return;

		isSubmitting = true;
		error = null;

		try {
			await auth.register(email, password, tenantName);
			goto('/auth/2fa-setup');
		} catch (err) {
			error = (err as ApiError).message ?? 'Something went wrong. Please try again.';
		} finally {
			isSubmitting = false;
		}
	}

	function handleConfirmBlur() {
		if (confirmPassword && password !== confirmPassword) {
			confirmError = "Passwords don't match";
		} else {
			confirmError = null;
		}
	}
</script>

<svelte:head>
	<title>Create Account — Reconova</title>
</svelte:head>

<AuthGlow variant={currentStep === 1 ? 'register-1' : 'register-2'} />

<div class="w-full max-w-md flex flex-col gap-8 relative z-10">
	<AuthLogo />

	<AuthCard>
		{#if currentStep === 1}
			<!-- STEP 1: Credentials -->
			<div class="text-center">
				<h2 class="text-white text-[28px] font-bold leading-tight">Create your account</h2>
				<div class="mt-3">
					<StepIndicator currentStep={1} totalSteps={2} />
				</div>
				<p class="text-text-muted mt-2 text-sm">Step 1 of 2</p>
			</div>

			{#if error}
				<Alert variant="error">{error}</Alert>
			{/if}

			<form class="flex flex-col gap-5" onsubmit={handleNext}>
				<TextInput
					label="Email"
					id="reg-email"
					type="email"
					bind:value={email}
					placeholder="you@company.com"
					required
				/>

				<div>
					<PasswordInput
						label="Password"
						id="reg-password"
						bind:value={password}
						placeholder="Create a strong password"
					/>
					<PasswordStrength password={password} />
				</div>

				<PasswordInput
					label="Confirm Password"
					id="reg-confirm"
					bind:value={confirmPassword}
					placeholder="Re-enter your password"
					error={confirmError}
					onblur={handleConfirmBlur}
				/>

				<Button type="submit" variant="primary" fullWidth>
					Next &rarr;
				</Button>
			</form>

			<div class="text-center text-sm text-text-muted">
				Already have an account?
				<a class="text-brand font-medium hover:text-brand-dark transition-colors" href="/auth/login">Log in</a>
			</div>
		{:else}
			<!-- STEP 2: Organization -->
			<div class="text-center">
				<h2 class="text-white text-[28px] font-bold leading-tight">Set up your organization</h2>
				<div class="mt-3">
					<StepIndicator currentStep={2} totalSteps={2} />
				</div>
				<p class="text-text-muted mt-2 text-sm">Step 2 of 2</p>
			</div>

			{#if error}
				<Alert variant="error">{error}</Alert>
			{/if}

			<form class="flex flex-col gap-5" onsubmit={handleSubmit}>
				<div>
					<TextInput
						label="Organization Name"
						id="reg-org"
						bind:value={tenantName}
						placeholder="Acme Corp"
						required
					/>
					<p class="text-text-muted text-xs mt-1.5">This will be your tenant name in Reconova.</p>
				</div>

				<div class="flex gap-3 mt-2">
					<Button type="button" variant="secondary" onclick={handleBack} fullWidth>
						&larr; Back
					</Button>
					<Button type="submit" variant="primary" loading={isSubmitting} fullWidth>
						{isSubmitting ? 'Creating...' : 'Create Account'}
					</Button>
				</div>
			</form>
		{/if}
	</AuthCard>
</div>
