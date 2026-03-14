<script lang="ts">
	import { adminConfigApi } from '$lib/api/client';
	import { Button, Alert } from '$lib/components/ui';
	import { Modal, Pagination, SkeletonLoader } from '$lib/components/shared';
	import { getToastStore } from '$lib/stores/toast';
	import type { ConfigHistory } from '$lib/types/admin';
	import type { ApiError } from '$lib/types/auth';

	const toast = getToastStore();

	let history = $state<ConfigHistory[]>([]);
	let isLoading = $state(true);
	let error = $state<string | null>(null);
	let keyFilter = $state('');
	let currentPage = $state(1);
	let totalPages = $state(1);
	let pageSize = 20;

	// Rollback modal
	let rollbackTarget = $state<ConfigHistory | null>(null);
	let isRollingBack = $state(false);

	async function loadHistory() {
		isLoading = true;
		error = null;
		try {
			const res = await adminConfigApi.getHistory({
				page: currentPage,
				pageSize,
				key: keyFilter || undefined,
			}) as { items: ConfigHistory[]; totalPages: number };
			history = res.items;
			totalPages = res.totalPages;
		} catch (err) {
			const apiErr = err as ApiError;
			error = apiErr.message || 'Failed to load configuration history.';
		} finally {
			isLoading = false;
		}
	}

	$effect(() => {
		loadHistory();
	});

	function handleFilterChange() {
		currentPage = 1;
		loadHistory();
	}

	function handlePageChange(page: number) {
		currentPage = page;
		loadHistory();
	}

	function openRollbackModal(entry: ConfigHistory) {
		rollbackTarget = entry;
	}

	function closeRollbackModal() {
		rollbackTarget = null;
	}

	async function confirmRollback() {
		if (!rollbackTarget) return;
		isRollingBack = true;
		try {
			await adminConfigApi.rollback(rollbackTarget.id);
			toast.success(`Configuration "${rollbackTarget.key}" rolled back.`);
			closeRollbackModal();
			await loadHistory();
		} catch (err) {
			const apiErr = err as ApiError;
			toast.error(apiErr.message || 'Failed to rollback configuration.');
		} finally {
			isRollingBack = false;
		}
	}

	function truncate(value: string | null, maxLen = 40): string {
		if (!value) return '-';
		return value.length > maxLen ? value.slice(0, maxLen) + '...' : value;
	}
</script>

<svelte:head>
	<title>Configuration History — Reconova Admin</title>
</svelte:head>

<div>
	<div class="flex items-center justify-between mb-6">
		<div>
			<div class="flex items-center gap-2 mb-1">
				<a href="/admin/config" class="text-text-muted hover:text-white transition-colors">
					<svg class="w-5 h-5" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="1.5">
						<path stroke-linecap="round" stroke-linejoin="round" d="M10.5 19.5 3 12m0 0 7.5-7.5M3 12h18" />
					</svg>
				</a>
				<h1 class="text-2xl font-bold text-white">Configuration History</h1>
			</div>
			<p class="text-text-secondary text-sm mt-1">View and rollback configuration changes</p>
		</div>
	</div>

	<!-- Filters -->
	<div class="flex gap-3 mb-6">
		<div class="flex-1">
			<input
				type="text"
				bind:value={keyFilter}
				placeholder="Filter by key..."
				class="w-full bg-surface border border-[rgba(255,255,255,0.08)] rounded-lg px-3 py-2 text-sm text-text placeholder:text-text-muted focus:outline-none focus:border-brand/50 transition-colors"
				onkeydown={(e) => { if (e.key === 'Enter') handleFilterChange(); }}
			/>
		</div>
		<Button variant="secondary" onclick={handleFilterChange}>Filter</Button>
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
	{:else}
		<div class="bg-surface rounded-xl border border-[rgba(255,255,255,0.08)] overflow-hidden">
			<table class="w-full">
				<thead>
					<tr class="border-b border-[rgba(255,255,255,0.08)]">
						<th class="text-left px-4 py-3 text-xs font-semibold uppercase tracking-wider text-text-muted">Date</th>
						<th class="text-left px-4 py-3 text-xs font-semibold uppercase tracking-wider text-text-muted">Key</th>
						<th class="text-left px-4 py-3 text-xs font-semibold uppercase tracking-wider text-text-muted">Old Value</th>
						<th class="text-left px-4 py-3 text-xs font-semibold uppercase tracking-wider text-text-muted">New Value</th>
						<th class="text-left px-4 py-3 text-xs font-semibold uppercase tracking-wider text-text-muted">Changed By</th>
						<th class="text-left px-4 py-3 text-xs font-semibold uppercase tracking-wider text-text-muted">Reason</th>
						<th class="text-right px-4 py-3 text-xs font-semibold uppercase tracking-wider text-text-muted"></th>
					</tr>
				</thead>
				<tbody>
					{#each history as entry}
						<tr class="border-b border-[rgba(255,255,255,0.04)] hover:bg-[rgba(255,255,255,0.02)] transition-colors">
							<td class="px-4 py-3 text-sm text-text-secondary whitespace-nowrap">
								{new Date(entry.changed_at).toLocaleString()}
							</td>
							<td class="px-4 py-3">
								<code class="text-sm font-mono text-brand">{entry.key}</code>
							</td>
							<td class="px-4 py-3 text-sm text-text-muted font-mono" title={entry.old_value || ''}>
								{truncate(entry.old_value)}
							</td>
							<td class="px-4 py-3 text-sm text-text font-mono" title={entry.new_value}>
								{truncate(entry.new_value)}
							</td>
							<td class="px-4 py-3 text-sm text-text-secondary">{entry.changed_by}</td>
							<td class="px-4 py-3 text-sm text-text-muted">{entry.reason || '-'}</td>
							<td class="px-4 py-3 text-right">
								<button
									class="text-sm text-brand hover:text-brand-dark font-medium transition-colors"
									onclick={() => openRollbackModal(entry)}
								>
									Rollback
								</button>
							</td>
						</tr>
					{:else}
						<tr>
							<td colspan="7" class="px-4 py-8 text-center text-text-muted text-sm">No history entries found</td>
						</tr>
					{/each}
				</tbody>
			</table>
		</div>

		<Pagination {currentPage} {totalPages} onPageChange={handlePageChange} />
	{/if}
</div>

<!-- Rollback Confirmation Modal -->
<Modal title="Confirm Rollback" open={rollbackTarget !== null} onclose={closeRollbackModal}>
	{#if rollbackTarget}
		<div class="space-y-4">
			<p class="text-sm text-text-secondary">
				Are you sure you want to rollback <code class="font-mono text-brand">{rollbackTarget.key}</code> to its previous value?
			</p>
			<div class="bg-[rgba(255,255,255,0.03)] rounded-lg p-3 space-y-2">
				<div class="flex justify-between text-sm">
					<span class="text-text-muted">Current value:</span>
					<code class="font-mono text-text">{truncate(rollbackTarget.new_value, 30)}</code>
				</div>
				<div class="flex justify-between text-sm">
					<span class="text-text-muted">Revert to:</span>
					<code class="font-mono text-text">{truncate(rollbackTarget.old_value, 30)}</code>
				</div>
			</div>
			<div class="flex justify-end gap-2">
				<Button variant="secondary" onclick={closeRollbackModal}>Cancel</Button>
				<Button variant="primary" loading={isRollingBack} onclick={confirmRollback}>Rollback</Button>
			</div>
		</div>
	{/if}
</Modal>
