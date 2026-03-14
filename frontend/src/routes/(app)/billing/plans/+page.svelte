<script lang="ts">
	import { billingApi } from '$lib/api/client';
	import type {
		TenantSubscription,
		SubscriptionPlan,
	} from '$lib/types/billing';
	import type { ApiError } from '$lib/types/auth';
	import { Button, Alert } from '$lib/components/ui';
	import { SkeletonLoader } from '$lib/components/shared';
	import { Modal } from '$lib/components/shared';

	let plans = $state<SubscriptionPlan[]>([]);
	let subscription = $state<TenantSubscription | null>(null);
	let isLoading = $state(true);
	let error = $state<string | null>(null);
	let billingInterval = $state<'MONTHLY' | 'ANNUAL'>('MONTHLY');

	// Downgrade modal
	let showDowngradeModal = $state(false);
	let downgradePlan = $state<SubscriptionPlan | null>(null);
	let isProcessing = $state(false);

	async function loadData() {
		isLoading = true;
		error = null;
		try {
			const [p, sub] = await Promise.all([
				billingApi.getPlans() as Promise<SubscriptionPlan[]>,
				billingApi.getSubscription() as Promise<TenantSubscription>,
			]);
			plans = p.filter((pl) => pl.status === 'ACTIVE');
			subscription = sub;
			billingInterval = sub.billing_interval;
		} catch (err) {
			const apiErr = err as ApiError;
			error = apiErr.message || 'Failed to load plans.';
		} finally {
			isLoading = false;
		}
	}

	$effect(() => {
		loadData();
	});

	function isCurrentPlan(plan: SubscriptionPlan): boolean {
		return subscription?.plan_id === plan.id;
	}

	function isUpgrade(plan: SubscriptionPlan): boolean {
		if (!subscription) return false;
		return plan.price_monthly > subscription.plan.price_monthly;
	}

	function getPrice(plan: SubscriptionPlan): number {
		return billingInterval === 'MONTHLY' ? plan.price_monthly : plan.price_annual;
	}

	function formatPrice(cents: number): string {
		return `$${(cents / 100).toFixed(0)}`;
	}

	function monthlyEquivalent(plan: SubscriptionPlan): string {
		if (billingInterval === 'ANNUAL') {
			return `$${(plan.price_annual / 100 / 12).toFixed(0)}`;
		}
		return formatPrice(plan.price_monthly);
	}

	function annualSavings(plan: SubscriptionPlan): number {
		const monthlyTotal = (plan.price_monthly * 12) / 100;
		const annualTotal = plan.price_annual / 100;
		return Math.round(monthlyTotal - annualTotal);
	}

	async function handleUpgrade(plan: SubscriptionPlan) {
		isProcessing = true;
		error = null;
		try {
			const result = await billingApi.checkout({
				plan_id: plan.id,
				billing_interval: billingInterval,
			}) as { checkout_url: string };
			window.location.href = result.checkout_url;
		} catch (err) {
			const apiErr = err as ApiError;
			error = apiErr.message || 'Failed to start checkout.';
			isProcessing = false;
		}
	}

	function openDowngradeModal(plan: SubscriptionPlan) {
		downgradePlan = plan;
		showDowngradeModal = true;
	}

	async function confirmDowngrade() {
		if (!downgradePlan) return;
		isProcessing = true;
		error = null;
		try {
			await billingApi.downgrade({ plan_id: downgradePlan.id });
			showDowngradeModal = false;
			downgradePlan = null;
			await loadData();
		} catch (err) {
			const apiErr = err as ApiError;
			error = apiErr.message || 'Failed to downgrade plan.';
		} finally {
			isProcessing = false;
		}
	}

	function getFeatureList(plan: SubscriptionPlan): { name: string; enabled: boolean }[] {
		const features = plan.features_json || {};
		return Object.entries(features).map(([name, enabled]) => ({
			name: name.replace(/_/g, ' ').replace(/\b\w/g, (c) => c.toUpperCase()),
			enabled,
		}));
	}

	// Determine card accent: highlight the middle / most popular plan
	function cardAccent(index: number): string {
		if (plans.length === 3 && index === 1) {
			return 'border-brand/40 shadow-[0_0_30px_rgba(229,62,62,0.1)]';
		}
		return 'border-[rgba(255,255,255,0.08)]';
	}

	function isPopular(index: number): boolean {
		return plans.length === 3 && index === 1;
	}
</script>

<svelte:head>
	<title>Choose Your Plan — Reconova</title>
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
		<h1 class="text-3xl font-bold text-white">Choose Your Plan</h1>
		<p class="text-text-secondary mt-2">Select the plan that best fits your security needs</p>
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
					<SkeletonLoader lines={8} />
				</div>
			{/each}
		</div>
	{:else}
		<!-- Billing Toggle -->
		<div class="flex items-center justify-center gap-3 mb-8">
			<span class="text-sm {billingInterval === 'MONTHLY' ? 'text-white font-medium' : 'text-text-muted'}">Monthly</span>
			<button
				class="relative w-14 h-7 rounded-full transition-colors {billingInterval === 'ANNUAL' ? 'bg-brand' : 'bg-surface-3'}"
				onclick={() => billingInterval = billingInterval === 'MONTHLY' ? 'ANNUAL' : 'MONTHLY'}
				aria-label="Toggle billing interval"
			>
				<span
					class="absolute top-0.5 left-0.5 w-6 h-6 bg-white rounded-full transition-transform {billingInterval === 'ANNUAL' ? 'translate-x-7' : 'translate-x-0'}"
				></span>
			</button>
			<span class="text-sm {billingInterval === 'ANNUAL' ? 'text-white font-medium' : 'text-text-muted'}">
				Annual
			</span>
			{#if billingInterval === 'ANNUAL'}
				<span class="text-xs font-medium text-success bg-success/10 px-2 py-0.5 rounded-full">Save up to 20%</span>
			{/if}
		</div>

		<!-- Plan Cards -->
		<div class="grid grid-cols-1 md:grid-cols-3 gap-6 max-w-5xl mx-auto">
			{#each plans as plan, i}
				<div class="relative bg-surface rounded-xl border {cardAccent(i)} p-6 flex flex-col backdrop-blur-sm bg-[rgba(22,22,32,0.8)]">
					{#if isPopular(i)}
						<div class="absolute -top-3 left-1/2 -translate-x-1/2">
							<span class="text-xs font-bold uppercase tracking-wider text-white bg-brand px-3 py-1 rounded-full">
								Most Popular
							</span>
						</div>
					{/if}

					<!-- Plan Header -->
					<div class="text-center mb-6 {isPopular(i) ? 'mt-2' : ''}">
						<h3 class="text-xl font-bold text-white">{plan.name}</h3>
						<div class="mt-3">
							<span class="text-4xl font-bold text-white">{monthlyEquivalent(plan)}</span>
							<span class="text-text-muted text-sm">/mo</span>
						</div>
						{#if billingInterval === 'ANNUAL' && annualSavings(plan) > 0}
							<p class="text-xs text-success mt-1">Save ${annualSavings(plan)}/year</p>
						{/if}
						{#if billingInterval === 'ANNUAL'}
							<p class="text-xs text-text-muted mt-1">Billed {formatPrice(getPrice(plan))}/year</p>
						{/if}
					</div>

					<!-- Divider -->
					<div class="border-t border-[rgba(255,255,255,0.06)] mb-5"></div>

					<!-- Credits & Domains -->
					<div class="space-y-3 mb-5">
						<div class="flex items-center justify-between">
							<span class="text-sm text-text-secondary">Credits / month</span>
							<span class="text-sm font-semibold text-white">{plan.monthly_credits.toLocaleString()}</span>
						</div>
						<div class="flex items-center justify-between">
							<span class="text-sm text-text-secondary">Max domains</span>
							<span class="text-sm font-semibold text-white">{plan.max_domains === -1 ? 'Unlimited' : plan.max_domains}</span>
						</div>
					</div>

					<!-- Features -->
					<ul class="space-y-2.5 mb-6 flex-1">
						{#each getFeatureList(plan) as feature}
							<li class="flex items-center gap-2.5 text-sm">
								{#if feature.enabled}
									<svg class="w-4 h-4 text-success flex-shrink-0" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2.5">
										<path stroke-linecap="round" stroke-linejoin="round" d="M5 13l4 4L19 7" />
									</svg>
									<span class="text-text">{feature.name}</span>
								{:else}
									<svg class="w-4 h-4 text-text-muted flex-shrink-0" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2.5">
										<path stroke-linecap="round" stroke-linejoin="round" d="M6 18L18 6M6 6l12 12" />
									</svg>
									<span class="text-text-muted">{feature.name}</span>
								{/if}
							</li>
						{/each}
					</ul>

					<!-- CTA -->
					<div class="mt-auto">
						{#if isCurrentPlan(plan)}
							<Button variant="secondary" disabled class="w-full">Current Plan</Button>
						{:else if isUpgrade(plan)}
							<Button
								variant="primary"
								class="w-full"
								loading={isProcessing}
								onclick={() => handleUpgrade(plan)}
							>
								Upgrade
							</Button>
						{:else}
							<Button
								variant="secondary"
								class="w-full"
								loading={isProcessing}
								onclick={() => openDowngradeModal(plan)}
							>
								Downgrade
							</Button>
						{/if}
					</div>
				</div>
			{/each}
		</div>
	{/if}
</div>

<!-- Downgrade Confirmation Modal -->
<Modal title="Confirm Downgrade" open={showDowngradeModal} onclose={() => { showDowngradeModal = false; downgradePlan = null; }}>
	<div class="space-y-4">
		<p class="text-sm text-text-secondary">
			Are you sure you want to downgrade to <span class="font-semibold text-white">{downgradePlan?.name}</span>?
		</p>
		<div class="bg-warning/10 border border-warning/20 rounded-lg p-3">
			<p class="text-xs text-warning">
				The downgrade will take effect at the end of your current billing period. You will retain access to your current plan features until then.
			</p>
		</div>
		{#if downgradePlan}
			<div class="text-sm text-text-muted space-y-1">
				<p>New monthly credits: <span class="text-white">{downgradePlan.monthly_credits.toLocaleString()}</span></p>
				<p>New max domains: <span class="text-white">{downgradePlan.max_domains === -1 ? 'Unlimited' : downgradePlan.max_domains}</span></p>
			</div>
		{/if}
		<div class="flex gap-3 justify-end pt-2">
			<Button variant="secondary" onclick={() => { showDowngradeModal = false; downgradePlan = null; }}>
				Cancel
			</Button>
			<Button variant="primary" loading={isProcessing} onclick={confirmDowngrade}>
				Confirm Downgrade
			</Button>
		</div>
	</div>
</Modal>
