<script lang="ts">
	import { goto } from '$app/navigation';
	import { AuthLogo, AuthGlow, AuthCard } from '$lib/components/auth';
	import { OtpInput, Button, Alert } from '$lib/components/ui';
	import { getAuthStore } from '$lib/stores/auth';
	import type { ApiError } from '$lib/types/auth';
	import QRCode from 'qrcode';

	const auth = getAuthStore();

	let otpCode = $state('');
	let isSubmitting = $state(false);
	let isLoading = $state(true);
	let error = $state<string | null>(null);
	let showManualKey = $state(false);

	let qrUri = $state('');
	let manualKey = $state('');
	let qrDataUrl = $state('');

	$effect(() => {
		auth.get2faSetup()
			.then((res) => {
				qrUri = res.qrUri;
				manualKey = res.secret;
				isLoading = false;
			})
			.catch(() => {
				goto('/auth/login');
			});
	});

	$effect(() => {
		if (qrUri) {
			QRCode.toDataURL(qrUri, {
				width: 200,
				margin: 1,
				color: { dark: '#000000', light: '#FFFFFF' }
			}).then((url: string) => {
				qrDataUrl = url;
			});
		}
	});

	async function handleSubmit(e: Event) {
		e.preventDefault();
		if (otpCode.length !== 6) return;

		isSubmitting = true;
		error = null;

		try {
			await auth.verify2fa(otpCode);
			goto('/dashboard');
		} catch (err) {
			if ((err as ApiError).message) {
				error = (err as ApiError).message;
			} else {
				error = 'Invalid code. Check your authenticator and try again.';
			}
			otpCode = '';
		} finally {
			isSubmitting = false;
		}
	}

	function copyKey() {
		navigator.clipboard.writeText(manualKey.replace(/\s/g, ''));
	}
</script>

<svelte:head>
	<title>Set Up 2FA — Reconova</title>
</svelte:head>

<AuthGlow variant="2fa-setup" />

<div class="w-full max-w-md flex flex-col gap-8 relative z-10">
	<AuthLogo />

	<AuthCard>
		<div class="text-center">
			<h2 class="text-white text-[28px] font-bold leading-tight">Set up two-factor authentication</h2>
			<p class="text-text-muted mt-2 text-sm">Add an extra layer of security to your account</p>
		</div>

		{#if isLoading}
			<div class="flex items-center justify-center py-12">
				<svg class="w-8 h-8 animate-spin text-brand" fill="none" viewBox="0 0 24 24">
					<circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
					<path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
				</svg>
			</div>
		{:else}
			{#if error}
				<Alert variant="error">{error}</Alert>
			{/if}

			<form class="flex flex-col gap-6" onsubmit={handleSubmit}>
				<!-- Step 1 -->
				<div class="flex gap-3">
					<span class="flex items-center justify-center w-6 h-6 rounded-full bg-brand/20 text-brand text-xs font-bold shrink-0 mt-0.5">1</span>
					<div>
						<p class="text-white text-sm font-medium">Install an authenticator app</p>
						<p class="text-text-muted text-xs mt-0.5">Google Authenticator, Authy, etc.</p>
					</div>
				</div>

				<!-- Step 2 -->
				<div class="flex gap-3">
					<span class="flex items-center justify-center w-6 h-6 rounded-full bg-brand/20 text-brand text-xs font-bold shrink-0 mt-0.5">2</span>
					<div class="flex-1">
						<p class="text-white text-sm font-medium">Scan this QR code</p>
						<div class="mt-3 flex justify-center">
							<div class="w-40 h-40 bg-white rounded-lg p-2 flex items-center justify-center">
								{#if qrDataUrl}
								<img src={qrDataUrl} alt="QR Code" class="w-full h-full" />
							{:else}
								<div class="w-full h-full flex items-center justify-center text-gray-400 text-xs">Loading...</div>
							{/if}
							</div>
						</div>

						<div class="mt-3">
							<button
								type="button"
								class="text-brand text-xs font-medium hover:text-brand-dark transition-colors"
								onclick={() => showManualKey = !showManualKey}
							>
								{showManualKey ? 'Hide manual key' : "Can't scan? Show manual key"}
							</button>

							{#if showManualKey}
								<div class="mt-2 flex items-center gap-2 bg-surface rounded-lg px-3 py-2 border border-[rgba(255,255,255,0.08)]">
									<code class="text-xs text-text font-mono flex-1 tracking-wider">{manualKey}</code>
									<button
										type="button"
										class="text-text-muted hover:text-white transition-colors"
										onclick={copyKey}
										aria-label="Copy key"
									>
										<svg class="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
											<path stroke-linecap="round" stroke-linejoin="round" d="M15.666 3.888A2.25 2.25 0 0 0 13.5 2.25h-3c-1.03 0-1.9.693-2.166 1.638m7.332 0c.055.194.084.4.084.612v0a.75.75 0 0 1-.75.75H9.75a.75.75 0 0 1-.75-.75v0c0-.212.03-.418.084-.612m7.332 0c.646.049 1.288.11 1.927.184 1.1.128 1.907 1.077 1.907 2.185V19.5a2.25 2.25 0 0 1-2.25 2.25H6.75A2.25 2.25 0 0 1 4.5 19.5V6.257c0-1.108.806-2.057 1.907-2.185a48.208 48.208 0 0 1 1.927-.184" />
										</svg>
									</button>
								</div>
							{/if}
						</div>
					</div>
				</div>

				<!-- Step 3 -->
				<div class="flex gap-3">
					<span class="flex items-center justify-center w-6 h-6 rounded-full bg-brand/20 text-brand text-xs font-bold shrink-0 mt-0.5">3</span>
					<div class="flex-1">
						<p class="text-white text-sm font-medium mb-3">Enter the 6-digit code</p>
						<OtpInput bind:value={otpCode} />
					</div>
				</div>

				<Button type="submit" variant="primary" fullWidth loading={isSubmitting} disabled={otpCode.length !== 6}>
					{isSubmitting ? 'Verifying...' : 'Verify & Continue'}
				</Button>
			</form>
		{/if}
	</AuthCard>
</div>
