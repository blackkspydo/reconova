<script lang="ts">
	import { billingApi } from '$lib/api/client';
	import type {
		TenantSubscription,
		CreditBalance,
		CreditTransaction,
	} from '$lib/types/billing';
	import type { Paginated } from '$lib/types/scans';
	import type { ApiError } from '$lib/types/auth';
	import { Button, Alert } from '$lib/components/ui';
	import { Pagination, StatusBadge, EmptyState, SkeletonLoader } from '$lib/components/shared';

	let subscription = $state<TenantSubscription | null>(null);
	let credits = $state<CreditBalance | null>(null);
	let transactions = $state<CreditTransaction[]>([]);
	let totalPages = $state(1);
	let currentPage = $state(1);
	let pageSize = 10;

	let isLoading = $state(true);
	let error = $state<string | null>(null);

	// Filters
	let typeFilter = $state('');
	let dateFilter = $state('');

	async function loadData() {
		isLoading = true;
		error = null;
		try {
			const [sub, cred, txns] = await Promise.all([
				billingApi.getSubscription() as Promise<TenantSubscription>,
				billingApi.getCredits() as Promise<CreditBalance>,
				billingApi.getTransactions({
					page: currentPage,
					page_size: pageSize,
					type: typeFilter || undefined,
					date: dateFilter || undefined,
				}) as Promise<Paginated<CreditTransaction>>,
			]);
			subscription = sub;
			credits = cred;
			transactions = txns.data;
			totalPages = txns.total_pages;
		} catch (err) {
			const apiErr = err as ApiError;
			error = apiErr.message || 'Failed to load billing data.';
		} finally {
			isLoading = false;
		}
	}

	$effect(() => {
		loadData();
	});

	function handleFilterChange() {
		currentPage = 1;
		loadData();
	}

	function goToPage(page: number) {
		currentPage = page;
		loadData();
	}

	function formatDate(dateStr: string | null): string {
		if (!dateStr) return '--';
		return new Date(dateStr).toLocaleDateString('en-US', {
			year: 'numeric',
			month: 'short',
			day: 'numeric',
		});
	}

	function formatDateTime(dateStr: string): string {
		return new Date(dateStr).toLocaleDateString('en-US', {
			year: 'numeric',
			month: 'short',
			day: 'numeric',
			hour: '2-digit',
			minute: '2-digit',
		});
	}

	function formatPrice(cents: number): string {
		return `$${(cents / 100).toFixed(2)}`;
	}

	let usagePercent = $derived(
		credits ? Math.round((credits.used_this_period / (credits.used_this_period + credits.allotment_remaining)) * 100) || 0 : 0
	);

	function typeColor(type: string): string {
		switch (type) {
			case 'ALLOTMENT': return 'text-info bg-info/10';
			case 'PURCHASE': return 'text-success bg-success/10';
			case 'CONSUMPTION': return 'text-warning bg-warning/10';
			case 'REFUND': return 'text-brand bg-brand/10';
			case 'ADJUSTMENT': return 'text-text-secondary bg-[rgba(255,255,255,0.05)]';
			default: return 'text-text-muted bg-[rgba(255,255,255,0.05)]';
		}
	}
</script>

<svelte:head>
	<title>Billing — Reconova</title>
</svelte:head>

<div>
	<div class="mb-6">
		<h1 class="text-2xl font-bold text-white">Billing</h1>
		<p class="text-text-secondary text-sm mt-1">Manage your subscription, credits, and billing history</p>
	</div>

	{#if error}
		<Alert variant="error">{error}</Alert>
		<div class="mt-4"></div>
	{/if}

	{#if isLoading}
		<div class="grid grid-cols-1 lg:grid-cols-2 gap-6 mb-6">
			<div class="bg-surface rounded-xl border border-[rgba(255,255,255,0.08)] p-6">
				<SkeletonLoader lines={4} />
			</div>
			<div class="bg-surface rounded-xl border border-[rgba(255,255,255,0.08)] p-6">
				<SkeletonLoader lines={4} />
			</div>
		</div>
		<div class="bg-surface rounded-xl border border-[rgba(255,255,255,0.08)] p-6">
			<SkeletonLoader lines={6} />
		</div>
	{:else}
		<!-- Current Plan & Credit Balance -->
		<div class="grid grid-cols-1 lg:grid-cols-2 gap-6 mb-6">
			<!-- Current Plan Card -->
			{#if subscription}
				<div class="bg-surface rounded-xl border border-[rgba(255,255,255,0.08)] p-6 backdrop-blur-sm bg-[rgba(22,22,32,0.8)]">
					<div class="flex items-center justify-between mb-4">
						<h2 class="text-lg font-semibold text-white">Current Plan</h2>
						<StatusBadge status={subscription.status} size="md" />
					</div>

					<div class="space-y-3">
						<div class="flex items-center justify-between">
							<span class="text-sm text-text-muted">Plan</span>
							<span class="text-sm font-medium text-white">{subscription.plan.name}</span>
						</div>
						<div class="flex items-center justify-between">
							<span class="text-sm text-text-muted">Price</span>
							<span class="text-sm text-text">
								{formatPrice(subscription.billing_interval === 'MONTHLY' ? subscription.plan.price_monthly : subscription.plan.price_annual)}
								<span class="text-text-muted">/{subscription.billing_interval === 'MONTHLY' ? 'mo' : 'yr'}</span>
							</span>
						</div>
						<div class="flex items-center justify-between">
							<span class="text-sm text-text-muted">Billing Interval</span>
							<span class="text-sm text-text">{subscription.billing_interval === 'MONTHLY' ? 'Monthly' : 'Annual'}</span>
						</div>
						<div class="flex items-center justify-between">
							<span class="text-sm text-text-muted">Next Billing Date</span>
							<span class="text-sm text-text">{formatDate(subscription.current_period_end)}</span>
						</div>
					</div>

					{#if subscription.pending_plan}
						<div class="mt-4 p-3 rounded-lg bg-warning/10 border border-warning/20">
							<p class="text-xs text-warning">
								Pending change to <span class="font-semibold">{subscription.pending_plan.name}</span> at end of billing period.
							</p>
						</div>
					{/if}

					<div class="mt-5">
						<a href="/billing/plans">
							<Button variant="secondary">Change Plan</Button>
						</a>
					</div>
				</div>
			{/if}

			<!-- Credit Balance Card -->
			{#if credits}
				<div class="bg-surface rounded-xl border border-[rgba(255,255,255,0.08)] p-6 backdrop-blur-sm bg-[rgba(22,22,32,0.8)]">
					<div class="flex items-center justify-between mb-4">
						<h2 class="text-lg font-semibold text-white">Credit Balance</h2>
						{#if credits.resets_at}
							<span class="text-xs text-text-muted">Resets {formatDate(credits.resets_at)}</span>
						{/if}
					</div>

					<!-- Progress Bar -->
					<div class="mb-4">
						<div class="flex items-center justify-between mb-2">
							<span class="text-sm text-text-secondary">Usage This Period</span>
							<span class="text-sm font-medium text-white">{usagePercent}%</span>
						</div>
						<div class="w-full h-2.5 bg-[rgba(255,255,255,0.06)] rounded-full overflow-hidden">
							<div
								class="h-full rounded-full transition-all duration-500 {usagePercent > 80 ? 'bg-danger' : usagePercent > 50 ? 'bg-warning' : 'bg-brand'}"
								style="width: {usagePercent}%"
							></div>
						</div>
						<div class="flex justify-between mt-1">
							<span class="text-xs text-text-muted">{credits.used_this_period} used</span>
							<span class="text-xs text-text-muted">{credits.allotment_remaining + credits.used_this_period} allotment</span>
						</div>
					</div>

					<!-- Stats -->
					<div class="grid grid-cols-3 gap-3">
						<div class="bg-[rgba(255,255,255,0.03)] rounded-lg p-3 text-center">
							<p class="text-lg font-bold text-white">{credits.allotment_remaining}</p>
							<p class="text-xs text-text-muted mt-0.5">Allotment</p>
						</div>
						<div class="bg-[rgba(255,255,255,0.03)] rounded-lg p-3 text-center">
							<p class="text-lg font-bold text-white">{credits.purchased_balance}</p>
							<p class="text-xs text-text-muted mt-0.5">Purchased</p>
						</div>
						<div class="bg-[rgba(255,255,255,0.03)] rounded-lg p-3 text-center">
							<p class="text-lg font-bold text-brand">{credits.total_available}</p>
							<p class="text-xs text-text-muted mt-0.5">Total Available</p>
						</div>
					</div>

					<div class="mt-5">
						<a href="/billing/credits">
							<Button variant="primary">Purchase Credits</Button>
						</a>
					</div>
				</div>
			{/if}
		</div>

		<!-- Transaction History -->
		<div class="bg-surface rounded-xl border border-[rgba(255,255,255,0.08)] backdrop-blur-sm bg-[rgba(22,22,32,0.8)]">
			<div class="p-6 pb-4">
				<h2 class="text-lg font-semibold text-white mb-4">Transaction History</h2>

				<!-- Filters -->
				<div class="flex gap-3">
					<select
						bind:value={typeFilter}
						class="bg-surface-2 border border-[rgba(255,255,255,0.08)] rounded-lg px-3 py-2 text-sm text-text focus:outline-none focus:border-brand/50 transition-colors"
						onchange={handleFilterChange}
					>
						<option value="">All types</option>
						<option value="ALLOTMENT">Allotment</option>
						<option value="CONSUMPTION">Consumption</option>
						<option value="PURCHASE">Purchase</option>
						<option value="REFUND">Refund</option>
						<option value="ADJUSTMENT">Adjustment</option>
					</select>
					<input
						type="date"
						bind:value={dateFilter}
						class="bg-surface-2 border border-[rgba(255,255,255,0.08)] rounded-lg px-3 py-2 text-sm text-text focus:outline-none focus:border-brand/50 transition-colors"
						onchange={handleFilterChange}
					/>
				</div>
			</div>

			<div class="overflow-hidden">
				<table class="w-full">
					<thead>
						<tr class="border-b border-[rgba(255,255,255,0.08)]">
							<th class="text-left px-6 py-3 text-xs font-semibold uppercase tracking-wider text-text-muted">Date</th>
							<th class="text-left px-6 py-3 text-xs font-semibold uppercase tracking-wider text-text-muted">Description</th>
							<th class="text-right px-6 py-3 text-xs font-semibold uppercase tracking-wider text-text-muted">Amount</th>
							<th class="text-left px-6 py-3 text-xs font-semibold uppercase tracking-wider text-text-muted">Type</th>
						</tr>
					</thead>
					<tbody>
						{#each transactions as txn}
							<tr class="border-b border-[rgba(255,255,255,0.04)] hover:bg-[rgba(255,255,255,0.02)] transition-colors">
								<td class="px-6 py-3 text-sm text-text-secondary">{formatDateTime(txn.created_at)}</td>
								<td class="px-6 py-3 text-sm text-text">{txn.description || '--'}</td>
								<td class="px-6 py-3 text-sm text-right font-mono font-medium {txn.amount > 0 ? 'text-success' : 'text-danger'}">
									{txn.amount > 0 ? '+' : ''}{txn.amount}
								</td>
								<td class="px-6 py-3">
									<span class="text-xs font-medium px-2 py-0.5 rounded-full {typeColor(txn.type)}">
										{txn.type}
									</span>
								</td>
							</tr>
						{:else}
							<tr>
								<td colspan="4">
									<EmptyState title="No transactions found" description="Your credit transaction history will appear here." />
								</td>
							</tr>
						{/each}
					</tbody>
				</table>
			</div>

			<div class="px-6 pb-4">
				<Pagination {currentPage} {totalPages} onPageChange={goToPage} />
			</div>
		</div>
	{/if}
</div>
