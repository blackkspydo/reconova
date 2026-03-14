<script lang="ts">
	import { adminAuditApi } from '$lib/api/client';
	import { Button, Alert } from '$lib/components/ui';
	import { Pagination, EmptyState, SkeletonLoader } from '$lib/components/shared';
	import type { AuditLogEntry } from '$lib/types/admin';
	import type { ApiError } from '$lib/types/auth';

	let entries = $state<AuditLogEntry[]>([]);
	let totalCount = $state(0);
	let currentPage = $state(1);
	let totalPages = $state(1);
	let pageSize = 20;
	let isLoading = $state(true);
	let error = $state<string | null>(null);

	// Filters
	let actorEmail = $state('');
	let actionFilter = $state('');
	let resourceTypeFilter = $state('');
	let dateFrom = $state('');
	let dateTo = $state('');

	// Expanded row
	let expandedId = $state<string | null>(null);

	const ACTION_TYPES = [
		'CREATE', 'UPDATE', 'DELETE', 'LOGIN', 'LOGOUT',
		'ENABLE', 'DISABLE', 'INVITE', 'REVOKE', 'EXPORT',
	];

	const RESOURCE_TYPES = [
		'USER', 'TENANT', 'DOMAIN', 'SCAN', 'FEATURE_FLAG',
		'SUBSCRIPTION', 'API_KEY', 'CONFIGURATION',
	];

	async function loadAudit() {
		isLoading = true;
		error = null;
		try {
			const res = (await adminAuditApi.list({
				page: currentPage,
				page_size: pageSize,
				actor_email: actorEmail || undefined,
				action: actionFilter || undefined,
				resource_type: resourceTypeFilter || undefined,
				date_from: dateFrom || undefined,
				date_to: dateTo || undefined,
			})) as { entries: AuditLogEntry[]; total_count: number; total_pages: number };
			entries = res.entries || [];
			totalCount = res.total_count || 0;
			totalPages = res.total_pages || 1;
		} catch (err) {
			const apiErr = err as ApiError;
			error = apiErr.message || 'Failed to load audit log.';
		} finally {
			isLoading = false;
		}
	}

	$effect(() => {
		loadAudit();
	});

	function handleSearch() {
		currentPage = 1;
		loadAudit();
	}

	function handlePageChange(page: number) {
		currentPage = page;
		loadAudit();
	}

	function handleClearFilters() {
		actorEmail = '';
		actionFilter = '';
		resourceTypeFilter = '';
		dateFrom = '';
		dateTo = '';
		currentPage = 1;
		loadAudit();
	}

	function toggleExpand(id: string) {
		expandedId = expandedId === id ? null : id;
	}

	function formatTimestamp(ts: string): string {
		const d = new Date(ts);
		return d.toLocaleDateString('en-US', {
			year: 'numeric',
			month: 'short',
			day: 'numeric',
			hour: '2-digit',
			minute: '2-digit',
			second: '2-digit',
		});
	}

	function formatAction(action: string): string {
		return action.replace(/_/g, ' ').replace(/\b\w/g, (c) => c.toUpperCase());
	}

	let hasActiveFilters = $derived(
		actorEmail !== '' || actionFilter !== '' || resourceTypeFilter !== '' || dateFrom !== '' || dateTo !== ''
	);
</script>

<svelte:head>
	<title>Audit Log — Reconova Admin</title>
</svelte:head>

<div>
	<div class="flex items-center justify-between mb-6">
		<div>
			<h1 class="text-2xl font-bold text-white">Audit Log</h1>
			<p class="text-text-secondary text-sm mt-1">{totalCount} total entries</p>
		</div>
	</div>

	<!-- Filters -->
	<div class="bg-surface/60 backdrop-blur-sm rounded-xl border border-[rgba(255,255,255,0.08)] p-4 mb-6">
		<div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-5 gap-3">
			<div>
				<label for="filter-email" class="block text-xs font-medium text-text-muted mb-1">Actor Email</label>
				<input
					id="filter-email"
					type="text"
					bind:value={actorEmail}
					placeholder="Search by email..."
					class="w-full bg-surface border border-[rgba(255,255,255,0.08)] rounded-lg px-3 py-2 text-sm text-text placeholder:text-text-muted focus:outline-none focus:border-brand/50 transition-colors"
					onkeydown={(e) => { if (e.key === 'Enter') handleSearch(); }}
				/>
			</div>
			<div>
				<label for="filter-action" class="block text-xs font-medium text-text-muted mb-1">Action</label>
				<select
					id="filter-action"
					bind:value={actionFilter}
					class="w-full bg-surface border border-[rgba(255,255,255,0.08)] rounded-lg px-3 py-2 text-sm text-text focus:outline-none focus:border-brand/50 transition-colors"
					onchange={handleSearch}
				>
					<option value="">All actions</option>
					{#each ACTION_TYPES as action}
						<option value={action}>{formatAction(action)}</option>
					{/each}
				</select>
			</div>
			<div>
				<label for="filter-resource" class="block text-xs font-medium text-text-muted mb-1">Resource Type</label>
				<select
					id="filter-resource"
					bind:value={resourceTypeFilter}
					class="w-full bg-surface border border-[rgba(255,255,255,0.08)] rounded-lg px-3 py-2 text-sm text-text focus:outline-none focus:border-brand/50 transition-colors"
					onchange={handleSearch}
				>
					<option value="">All types</option>
					{#each RESOURCE_TYPES as type}
						<option value={type}>{formatAction(type)}</option>
					{/each}
				</select>
			</div>
			<div>
				<label for="filter-from" class="block text-xs font-medium text-text-muted mb-1">Date From</label>
				<input
					id="filter-from"
					type="date"
					bind:value={dateFrom}
					class="w-full bg-surface border border-[rgba(255,255,255,0.08)] rounded-lg px-3 py-2 text-sm text-text focus:outline-none focus:border-brand/50 transition-colors"
					onchange={handleSearch}
				/>
			</div>
			<div>
				<label for="filter-to" class="block text-xs font-medium text-text-muted mb-1">Date To</label>
				<input
					id="filter-to"
					type="date"
					bind:value={dateTo}
					class="w-full bg-surface border border-[rgba(255,255,255,0.08)] rounded-lg px-3 py-2 text-sm text-text focus:outline-none focus:border-brand/50 transition-colors"
					onchange={handleSearch}
				/>
			</div>
		</div>
		<div class="flex items-center gap-3 mt-3">
			<Button variant="secondary" onclick={handleSearch}>Search</Button>
			{#if hasActiveFilters}
				<button onclick={handleClearFilters} class="text-sm text-text-muted hover:text-white transition-colors">Clear filters</button>
			{/if}
		</div>
	</div>

	{#if error}
		<div class="mb-4">
			<Alert variant="error">{error}</Alert>
		</div>
	{/if}

	{#if isLoading}
		<div class="bg-surface/60 backdrop-blur-sm rounded-xl border border-[rgba(255,255,255,0.08)] p-6">
			<SkeletonLoader lines={12} />
		</div>
	{:else if entries.length === 0}
		<div class="bg-surface/60 backdrop-blur-sm rounded-xl border border-[rgba(255,255,255,0.08)]">
			<EmptyState title="No audit log entries" description="No entries match your current filters." />
		</div>
	{:else}
		<div class="bg-surface rounded-xl border border-[rgba(255,255,255,0.08)] overflow-hidden">
			<table class="w-full">
				<thead>
					<tr class="border-b border-[rgba(255,255,255,0.08)]">
						<th class="text-left px-4 py-3 text-xs font-semibold uppercase tracking-wider text-text-muted w-5"></th>
						<th class="text-left px-4 py-3 text-xs font-semibold uppercase tracking-wider text-text-muted">Timestamp</th>
						<th class="text-left px-4 py-3 text-xs font-semibold uppercase tracking-wider text-text-muted">Actor Email</th>
						<th class="text-left px-4 py-3 text-xs font-semibold uppercase tracking-wider text-text-muted">Action</th>
						<th class="text-left px-4 py-3 text-xs font-semibold uppercase tracking-wider text-text-muted">Resource Type</th>
						<th class="text-left px-4 py-3 text-xs font-semibold uppercase tracking-wider text-text-muted">Resource ID</th>
						<th class="text-left px-4 py-3 text-xs font-semibold uppercase tracking-wider text-text-muted">IP Address</th>
					</tr>
				</thead>
				<tbody>
					{#each entries as entry}
						<tr
							class="border-b border-[rgba(255,255,255,0.04)] hover:bg-[rgba(255,255,255,0.02)] transition-colors {entry.details ? 'cursor-pointer' : ''}"
							onclick={() => { if (entry.details) toggleExpand(entry.id); }}
						>
							<td class="px-4 py-3 text-text-muted">
								{#if entry.details}
									<svg class="w-4 h-4 transition-transform {expandedId === entry.id ? 'rotate-90' : ''}" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
										<path stroke-linecap="round" stroke-linejoin="round" d="m8.25 4.5 7.5 7.5-7.5 7.5" />
									</svg>
								{/if}
							</td>
							<td class="px-4 py-3 text-sm text-text-secondary whitespace-nowrap">{formatTimestamp(entry.timestamp)}</td>
							<td class="px-4 py-3 text-sm text-text">{entry.actor_email}</td>
							<td class="px-4 py-3">
								<span class="text-xs font-medium px-2 py-0.5 rounded-full bg-brand/10 text-brand">
									{formatAction(entry.action)}
								</span>
							</td>
							<td class="px-4 py-3 text-sm text-text-secondary">{formatAction(entry.resource_type)}</td>
							<td class="px-4 py-3">
								{#if entry.resource_id}
									<code class="text-xs font-mono text-text-muted bg-[rgba(255,255,255,0.05)] px-1.5 py-0.5 rounded">{entry.resource_id}</code>
								{:else}
									<span class="text-text-muted text-sm">—</span>
								{/if}
							</td>
							<td class="px-4 py-3 text-sm text-text-muted">{entry.ip_address || '—'}</td>
						</tr>
						{#if expandedId === entry.id && entry.details}
							<tr class="border-b border-[rgba(255,255,255,0.04)]">
								<td colspan="7" class="px-4 py-3">
									<div class="bg-[rgba(255,255,255,0.03)] rounded-lg p-4 ml-8">
										<p class="text-xs font-semibold uppercase tracking-wider text-text-muted mb-2">Details</p>
										<pre class="text-xs text-text-secondary font-mono whitespace-pre-wrap break-all">{JSON.stringify(entry.details, null, 2)}</pre>
									</div>
								</td>
							</tr>
						{/if}
					{/each}
				</tbody>
			</table>
		</div>

		<Pagination {currentPage} {totalPages} onPageChange={handlePageChange} />
	{/if}
</div>
