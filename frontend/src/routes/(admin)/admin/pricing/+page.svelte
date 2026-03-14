<script lang="ts">
	import { adminApi } from '$lib/api/client';
	import { Button, Alert } from '$lib/components/ui';
	import { SkeletonLoader } from '$lib/components/shared';
	import { getToastStore } from '$lib/stores/toast';
	import type { ApiError } from '$lib/types/auth';

	const toast = getToastStore();

	interface PricingRow {
		check_type: string;
		tier_id: string;
		credits_per_domain: number;
	}

	let pricing = $state<PricingRow[]>([]);
	let originalPricing = $state<PricingRow[]>([]);
	let isLoading = $state(true);
	let isSaving = $state(false);
	let error = $state<string | null>(null);

	let isDirty = $derived(
		JSON.stringify(pricing.map(r => r.credits_per_domain)) !==
		JSON.stringify(originalPricing.map(r => r.credits_per_domain))
	);

	async function loadPricing() {
		isLoading = true;
		error = null;
		try {
			const res = await adminApi.getPricing() as PricingRow[];
			pricing = res.map(r => ({ ...r }));
			originalPricing = res.map(r => ({ ...r }));
		} catch (err) {
			const apiErr = err as ApiError;
			error = apiErr.message || 'Failed to load pricing configuration.';
		} finally {
			isLoading = false;
		}
	}

	$effect(() => {
		loadPricing();
	});

	async function handleSave() {
		isSaving = true;
		error = null;
		try {
			const updates = pricing
				.filter((row, i) => row.credits_per_domain !== originalPricing[i].credits_per_domain)
				.map(row => ({
					check_type: row.check_type,
					tier_id: row.tier_id,
					credits_per_domain: row.credits_per_domain,
				}));
			await adminApi.updatePricing({ updates });
			originalPricing = pricing.map(r => ({ ...r }));
			toast.success('Pricing configuration saved successfully.');
		} catch (err) {
			const apiErr = err as ApiError;
			error = apiErr.message || 'Failed to save pricing changes.';
		} finally {
			isSaving = false;
		}
	}

	function formatCheckType(type: string): string {
		return type
			.replace(/_/g, ' ')
			.replace(/\b\w/g, c => c.toUpperCase());
	}

	function groupedPricing(): { checkType: string; rows: { index: number; row: PricingRow }[] }[] {
		const groups = new Map<string, { index: number; row: PricingRow }[]>();
		pricing.forEach((row, index) => {
			const existing = groups.get(row.check_type);
			if (existing) {
				existing.push({ index, row });
			} else {
				groups.set(row.check_type, [{ index, row }]);
			}
		});
		return Array.from(groups.entries()).map(([checkType, rows]) => ({ checkType, rows }));
	}
</script>

<svelte:head>
	<title>Pricing Configuration — Reconova Admin</title>
</svelte:head>

<div>
	<div class="flex items-center justify-between mb-6">
		<div>
			<h1 class="text-2xl font-bold text-white">Pricing Configuration</h1>
			<p class="text-text-secondary text-sm mt-1">Manage credit costs per check type and tier</p>
		</div>
		<Button
			variant="primary"
			disabled={!isDirty}
			loading={isSaving}
			onclick={handleSave}
		>
			Save Changes
		</Button>
	</div>

	{#if error}
		<div class="mb-4">
			<Alert variant="error">{error}</Alert>
		</div>
	{/if}

	{#if isLoading}
		<div class="bg-surface/60 backdrop-blur-sm rounded-xl border border-[rgba(255,255,255,0.08)] p-6">
			<SkeletonLoader lines={10} />
		</div>
	{:else if pricing.length === 0}
		<div class="bg-surface/60 backdrop-blur-sm rounded-xl border border-[rgba(255,255,255,0.08)] p-8 text-center">
			<p class="text-text-muted text-sm">No pricing data found.</p>
		</div>
	{:else}
		<div class="bg-surface rounded-xl border border-[rgba(255,255,255,0.08)] overflow-hidden">
			<table class="w-full">
				<thead>
					<tr class="border-b border-[rgba(255,255,255,0.08)]">
						<th class="text-left px-4 py-3 text-xs font-semibold uppercase tracking-wider text-text-muted">Check Type</th>
						<th class="text-left px-4 py-3 text-xs font-semibold uppercase tracking-wider text-text-muted">Tier</th>
						<th class="text-left px-4 py-3 text-xs font-semibold uppercase tracking-wider text-text-muted">Credits Per Domain</th>
					</tr>
				</thead>
				<tbody>
					{#each groupedPricing() as group, groupIdx}
						{#each group.rows as { index, row }, rowIdx}
							<tr class="border-b border-[rgba(255,255,255,0.04)] hover:bg-[rgba(255,255,255,0.02)] transition-colors">
								<td class="px-4 py-3 text-sm text-text font-medium">
									{#if rowIdx === 0}
										{formatCheckType(group.checkType)}
									{/if}
								</td>
								<td class="px-4 py-3 text-sm text-text-secondary">{row.tier_id}</td>
								<td class="px-4 py-3">
									<input
										type="number"
										min="0"
										step="1"
										bind:value={pricing[index].credits_per_domain}
										class="w-32 bg-surface border border-[rgba(255,255,255,0.08)] rounded-lg px-3 py-1.5 text-sm text-text focus:outline-none focus:border-brand/50 transition-colors [appearance:textfield] [&::-webkit-outer-spin-button]:appearance-none [&::-webkit-inner-spin-button]:appearance-none {pricing[index].credits_per_domain !== originalPricing[index].credits_per_domain ? 'border-brand/50 bg-brand/5' : ''}"
									/>
								</td>
							</tr>
						{/each}
						{#if groupIdx < groupedPricing().length - 1}
							<tr>
								<td colspan="3" class="py-0">
									<div class="border-b border-[rgba(255,255,255,0.08)]"></div>
								</td>
							</tr>
						{/if}
					{/each}
				</tbody>
			</table>
		</div>

		{#if isDirty}
			<div class="mt-4 flex justify-end">
				<Button
					variant="primary"
					loading={isSaving}
					onclick={handleSave}
				>
					Save Changes
				</Button>
			</div>
		{/if}
	{/if}
</div>
