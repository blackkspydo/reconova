<script lang="ts">
	import { cveApi } from '$lib/api/client';
	import type { VulnerabilityAlert } from '$lib/types/cve';
	import type { Paginated } from '$lib/types/scans';
	import type { ApiError } from '$lib/types/auth';
	import { Button, Alert, TextInput } from '$lib/components/ui';
	import { Pagination, StatusBadge, SeverityBadge, EmptyState, SkeletonLoader } from '$lib/components/shared';

	// Alerts state
	let alerts = $state<VulnerabilityAlert[]>([]);
	let isLoading = $state(true);
	let error = $state<string | null>(null);
	let totalAlerts = $state(0);
	let currentPage = $state(1);
	let totalPages = $state(1);
	const pageSize = 12;

	// Filters
	let severityFilter = $state<string>('');
	let statusFilter = $state<string>('');
	let domainFilter = $state<string>('');

	// Action state
	let actionLoading = $state<string | null>(null);
	let actionError = $state<string | null>(null);

	// Summary stats
	let criticalCount = $derived(alerts.filter((a) => a.severity === 'CRITICAL').length);
	let unresolvedCount = $derived(alerts.filter((a) => a.status !== 'RESOLVED').length);

	async function loadAlerts() {
		isLoading = true;
		error = null;
		try {
			const params: Record<string, string | number | undefined> = {
				page: currentPage,
				page_size: pageSize,
			};
			if (severityFilter) params.severity = severityFilter;
			if (statusFilter) params.status = statusFilter;
			if (domainFilter.trim()) params.domain = domainFilter.trim();

			const res = (await cveApi.getAlerts(params)) as Paginated<VulnerabilityAlert>;
			alerts = res.data;
			totalAlerts = res.total;
			totalPages = res.total_pages;
		} catch (err) {
			const apiErr = err as ApiError;
			error = apiErr.message || 'Failed to load vulnerability alerts.';
		} finally {
			isLoading = false;
		}
	}

	$effect(() => {
		loadAlerts();
	});

	function applyFilters() {
		currentPage = 1;
		loadAlerts();
	}

	function clearFilters() {
		severityFilter = '';
		statusFilter = '';
		domainFilter = '';
		currentPage = 1;
		loadAlerts();
	}

	function handlePageChange(p: number) {
		currentPage = p;
		loadAlerts();
	}

	async function acknowledgeAlert(id: string) {
		actionLoading = id;
		actionError = null;
		try {
			await cveApi.acknowledgeAlert(id);
			await loadAlerts();
		} catch (err) {
			const apiErr = err as ApiError;
			actionError = apiErr.message || 'Failed to acknowledge alert.';
		} finally {
			actionLoading = null;
		}
	}

	async function resolveAlert(id: string) {
		actionLoading = id;
		actionError = null;
		try {
			await cveApi.resolveAlert(id);
			await loadAlerts();
		} catch (err) {
			const apiErr = err as ApiError;
			actionError = apiErr.message || 'Failed to resolve alert.';
		} finally {
			actionLoading = null;
		}
	}

	function formatDate(dateStr: string | null): string {
		if (!dateStr) return '\u2014';
		return new Date(dateStr).toLocaleString(undefined, {
			month: 'short',
			day: 'numeric',
			year: 'numeric',
			hour: '2-digit',
			minute: '2-digit',
		});
	}

	function truncate(text: string, maxLen: number): string {
		if (text.length <= maxLen) return text;
		return text.slice(0, maxLen) + '\u2026';
	}
</script>

<svelte:head>
	<title>Vulnerability Alerts \u2014 Reconova</title>
</svelte:head>

<div>
	<!-- Header -->
	<div class="flex items-center justify-between mb-6">
		<div class="flex items-center gap-3">
			<h1 class="text-2xl font-bold text-white">Vulnerability Alerts</h1>
			{#if !isLoading}
				<span class="text-xs font-medium px-2.5 py-1 rounded-full bg-brand/10 text-brand">
					{totalAlerts}
				</span>
			{/if}
		</div>
	</div>

	<!-- Summary Stats -->
	{#if !isLoading && !error}
		<div class="grid grid-cols-3 gap-4 mb-6">
			<div class="bg-surface rounded-xl border border-[rgba(255,255,255,0.08)] p-4">
				<p class="text-xs text-text-muted uppercase tracking-wider mb-1">Total Alerts</p>
				<p class="text-xl font-bold text-white">{totalAlerts}</p>
			</div>
			<div class="bg-surface rounded-xl border border-[rgba(255,255,255,0.08)] p-4">
				<p class="text-xs text-text-muted uppercase tracking-wider mb-1">Critical</p>
				<p class="text-xl font-bold text-red-400">{criticalCount}</p>
			</div>
			<div class="bg-surface rounded-xl border border-[rgba(255,255,255,0.08)] p-4">
				<p class="text-xs text-text-muted uppercase tracking-wider mb-1">Unresolved</p>
				<p class="text-xl font-bold text-orange-400">{unresolvedCount}</p>
			</div>
		</div>
	{/if}

	<!-- Filter Bar -->
	<div class="bg-surface rounded-xl border border-[rgba(255,255,255,0.08)] p-4 mb-6">
		<div class="flex flex-wrap items-end gap-3">
			<div class="w-40">
				<label for="severity-filter" class="block text-xs font-medium text-text-muted mb-1">Severity</label>
				<select
					id="severity-filter"
					class="w-full bg-[rgba(255,255,255,0.05)] border border-[rgba(255,255,255,0.08)] rounded-lg px-3 py-2 text-sm text-white focus:outline-none focus:border-brand/50"
					bind:value={severityFilter}
				>
					<option value="">All</option>
					<option value="CRITICAL">Critical</option>
					<option value="HIGH">High</option>
					<option value="MEDIUM">Medium</option>
					<option value="LOW">Low</option>
				</select>
			</div>
			<div class="w-44">
				<label for="status-filter" class="block text-xs font-medium text-text-muted mb-1">Status</label>
				<select
					id="status-filter"
					class="w-full bg-[rgba(255,255,255,0.05)] border border-[rgba(255,255,255,0.08)] rounded-lg px-3 py-2 text-sm text-white focus:outline-none focus:border-brand/50"
					bind:value={statusFilter}
				>
					<option value="">All</option>
					<option value="NEW">New</option>
					<option value="ACKNOWLEDGED">Acknowledged</option>
					<option value="RESOLVED">Resolved</option>
				</select>
			</div>
			<div class="flex-1 min-w-[180px]">
				<TextInput
					id="domain-filter"
					label="Domain"
					bind:value={domainFilter}
					placeholder="Filter by domain..."
				/>
			</div>
			<div class="flex gap-2 pb-0.5">
				<Button variant="primary" onclick={applyFilters}>Filter</Button>
				<Button variant="ghost" onclick={clearFilters}>Clear</Button>
			</div>
		</div>
	</div>

	{#if error}
		<div class="mb-4">
			<Alert variant="error">{error}</Alert>
		</div>
	{/if}

	{#if actionError}
		<div class="mb-4">
			<Alert variant="error">{actionError}</Alert>
		</div>
	{/if}

	{#if isLoading}
		<div class="bg-surface rounded-xl border border-[rgba(255,255,255,0.08)] p-6">
			<SkeletonLoader lines={6} />
		</div>
	{:else if alerts.length === 0}
		<div class="bg-surface rounded-xl border border-[rgba(255,255,255,0.08)]">
			<EmptyState
				title="No vulnerability alerts"
				description="No alerts match your current filters. Alerts are generated when CVEs are detected against your monitored assets."
			/>
		</div>
	{:else}
		<!-- Alert Cards -->
		<div class="space-y-3">
			{#each alerts as alert}
				<div class="bg-surface/60 backdrop-blur-sm rounded-xl border border-[rgba(255,255,255,0.08)] p-5 hover:border-[rgba(255,255,255,0.14)] transition-colors">
					<div class="flex items-start justify-between gap-4">
						<div class="flex-1 min-w-0">
							<div class="flex items-center gap-2 mb-2">
								<SeverityBadge severity={alert.severity} />
								<a
									href="/vulnerabilities/{alert.cve_id}"
									class="text-sm font-mono font-medium text-brand hover:text-brand-dark transition-colors"
								>
									{alert.cve_id}
								</a>
								<StatusBadge status={alert.status} />
							</div>
							<p class="text-sm text-text mb-2">{truncate(alert.description, 180)}</p>
							<div class="flex items-center gap-4 text-xs text-text-muted">
								<span class="font-mono">{alert.subdomain_name}</span>
								<span class="text-[rgba(255,255,255,0.2)]">/</span>
								<span>{alert.domain_name}</span>
								<span class="text-[rgba(255,255,255,0.2)]">/</span>
								<span>{formatDate(alert.created_at)}</span>
							</div>
						</div>
						<div class="flex items-center gap-2 flex-shrink-0">
							{#if alert.status === 'NEW'}
								<Button
									variant="secondary"
									onclick={() => acknowledgeAlert(alert.id)}
									loading={actionLoading === alert.id}
								>
									Acknowledge
								</Button>
							{/if}
							{#if alert.status === 'ACKNOWLEDGED'}
								<Button
									variant="primary"
									onclick={() => resolveAlert(alert.id)}
									loading={actionLoading === alert.id}
								>
									Resolve
								</Button>
							{/if}
						</div>
					</div>
				</div>
			{/each}
		</div>

		<Pagination currentPage={currentPage} totalPages={totalPages} onPageChange={handlePageChange} />
	{/if}
</div>
