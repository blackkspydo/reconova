<script lang="ts">
	import { billingApi } from '$lib/api/client';
	import type { CreditPack } from '$lib/types/billing';
	import type { ApiError } from '$lib/types/auth';
	import { Button, Alert } from '$lib/components/ui';
	import { SkeletonLoader } from '$lib/components/shared';

	let packs = $state<CreditPack[]>([]);
	let isLoading = $state(true);
	let error = $state<string | null>(null);
	let purchasingId = $state<string | null>(null);

	async function loadPacks() {
		isLoading = true;
		error = null;
		try {
			const result = await billingApi.getCreditPacks() as CreditPack[];
			packs = result.filter((p) => p.status === 'ACTIVE');
		} catch (err) {
			const apiErr = err as ApiError;
			error = apiErr.message || 'Failed to load credit packs.';
		} finally {
			isLoading = false;
		}
	}

	$effect(() => {
		loadPacks();
	});

	function formatPrice(cents: number): string {
		return `$${(cents / 100).toFixed(2)}`;
	}

	async function handlePurchase(pack: CreditPack) {
		purchasingId = pack.id;
		error = null;
		try {
			const result = await billingApi.purchaseCredits({ credit_pack_id: pack.id }) as { checkout_url: string };
			window.location.href = result.checkout_url;
		} catch (err) {
			const apiErr = err as ApiError;
			error = apiErr.message || 'Failed to start purchase.';
			purchasingId = null;
		}
	}
</script>

<svelte:head>
	<title>Purchase Credits — Reconova</title>
</svelte:head>

<div>
	<!-- Back link -->
	<a href="/billing" class="inline-flex items-center gap-1.5 text-sm text-text-secondary hover:text-white transition-colors mb-6">
		<svg class="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
			<path stroke-linecap="round" stroke-linejoin="round" d="M15 19l-7-7 7-7" />
		</svg>
		Back to Billing
	</a>

	<!-- Header -->
	<div class="text-center mb-8">
		<h1 class="text-3xl font-bold text-white">Purchase Credits</h1>
		<p class="text-text-secondary mt-2">Top up your credit balance with a one-time purchase</p>
	</div>

	{#if error}
		<div class="max-w-md mx-auto mb-6">
			<Alert variant="error">{error}</Alert>
		</div>
	{/if}

	{#if isLoading}
		<div class="grid grid-cols-1 md:grid-cols-3 gap-6 max-w-5xl mx-auto">
			{#each Array(3) as _}
				<div class="bg-surface rounded-xl border border-[rgba(255,255,255,0.08)] p-6">
					<SkeletonLoader lines={5} />
				</div>
			{/each}
		</div>
	{:else if packs.length === 0}
		<div class="text-center py-12">
			<p class="text-text-muted">No credit packs available at this time.</p>
		</div>
	{:else}
		<div class="grid grid-cols-1 md:grid-cols-3 gap-6 max-w-5xl mx-auto">
			{#each packs as pack}
				<div class="relative bg-surface rounded-xl border {pack.popular ? 'border-brand/40 shadow-[0_0_30px_rgba(229,62,62,0.1)]' : 'border-[rgba(255,255,255,0.08)]'} p-6 flex flex-col backdrop-blur-sm bg-[rgba(22,22,32,0.8)]">
					{#if pack.popular}
						<div class="absolute -top-3 left-1/2 -translate-x-1/2">
							<span class="text-xs font-bold uppercase tracking-wider text-white bg-brand px-3 py-1 rounded-full">
								Most Popular
							</span>
						</div>
					{/if}

					<!-- Pack Header -->
					<div class="text-center mb-6 {pack.popular ? 'mt-2' : ''}">
						<h3 class="text-xl font-bold text-white">{pack.name}</h3>
						<div class="mt-3">
							<span class="text-4xl font-bold text-white">{formatPrice(pack.price)}</span>
						</div>
					</div>

					<!-- Divider -->
					<div class="border-t border-[rgba(255,255,255,0.06)] mb-5"></div>

					<!-- Credits Info -->
					<div class="space-y-3 mb-6 flex-1">
						<div class="flex items-center justify-between">
							<span class="text-sm text-text-secondary">Credits</span>
							<span class="text-sm font-semibold text-white">{pack.credits.toLocaleString()}</span>
						</div>
						{#if pack.bonus_credits > 0}
							<div class="flex items-center justify-between">
								<span class="text-sm text-text-secondary">Bonus</span>
								<span class="text-sm font-semibold text-success">+{pack.bonus_credits.toLocaleString()} bonus</span>
							</div>
							<div class="flex items-center justify-between">
								<span class="text-sm text-text-secondary">Total</span>
								<span class="text-sm font-bold text-brand">{(pack.credits + pack.bonus_credits).toLocaleString()}</span>
							</div>
						{/if}
						<div class="flex items-center justify-between">
							<span class="text-sm text-text-secondary">Per credit</span>
							<span class="text-sm text-text-muted">{formatPrice(Math.round(pack.price / (pack.credits + (pack.bonus_credits || 0))))}</span>
						</div>
					</div>

					<!-- CTA -->
					<div class="mt-auto">
						<Button
							variant={pack.popular ? 'primary' : 'secondary'}
							class="w-full"
							loading={purchasingId === pack.id}
							disabled={purchasingId !== null}
							onclick={() => handlePurchase(pack)}
						>
							{purchasingId === pack.id ? 'Processing...' : 'Purchase'}
						</Button>
					</div>
				</div>
			{/each}
		</div>
	{/if}
</div>
