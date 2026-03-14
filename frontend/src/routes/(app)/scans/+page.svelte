<script lang="ts">
	import { scanApi, domainApi } from '$lib/api/client';
	import { Button, Alert } from '$lib/components/ui';
	import { Pagination, StatusBadge, EmptyState, SkeletonLoader } from '$lib/components/shared';
	import { goto } from '$app/navigation';
	import type { ScanJob, Paginated } from '$lib/types/scans';
	import type { Domain } from '$lib/types/domains';
	import type { ApiError } from '$lib/types/auth';

	let scans = $state<ScanJob[]>([]);
	let domains = $state<Domain[]>([]);
	let currentPage = $state(1);
	let totalPages = $state(1);
	let totalCount = $state(0);
	let pageSize = 10;
	let isLoading = $state(true);
	let error = $state<string | null>(null);
	let cancellingId = $state<string | null>(null);

	// Filters
	let statusFilter = $state('');
	let domainFilter = $state('');
	let dateFilter = $state('');

	async function loadDomains() {
		try {
			const res = await domainApi.list() as Domain[];
			domains = res;
		} catch {
			// silent — domains dropdown just won't populate
		}
	}

	async function loadScans() {
		isLoading = true;
		error = null;
		try {
			const params: Record<string, string | number | undefined> = {
				page: currentPage,
				page_size: pageSize,
			};
			if (statusFilter) params.status = statusFilter;
			if (domainFilter) params.domain_id = domainFilter;
			if (dateFilter) params.date_range = dateFilter;

			const res = await scanApi.list(params) as Paginated<ScanJob>;
			scans = res.data;
			totalPages = res.total_pages;
			totalCount = res.total;
		} catch (err) {
			const apiErr = err as ApiError;
			error = apiErr.message || 'Failed to load scans.';
		} finally {
			isLoading = false;
		}
	}

	async function cancelScan(id: string) {
		cancellingId = id;
		try {
			await scanApi.cancel(id);
			await loadScans();
		} catch (err) {
			const apiErr = err as ApiError;
			error = apiErr.message || 'Failed to cancel scan.';
		} finally {
			cancellingId = null;
		}
	}

	function handleFilterChange() {
		currentPage = 1;
		loadScans();
	}

	function goToPage(page: number) {
		currentPage = page;
		loadScans();
	}

	function formatDuration(scan: ScanJob): string {
		if (!scan.started_at) return '—';
		const start = new Date(scan.started_at).getTime();
		const end = scan.completed_at ? new Date(scan.completed_at).getTime() : Date.now();
		const seconds = Math.floor((end - start) / 1000);
		if (seconds < 60) return `${seconds}s`;
		const minutes = Math.floor(seconds / 60);
		const remainSeconds = seconds % 60;
		if (minutes < 60) return `${minutes}m ${remainSeconds}s`;
		const hours = Math.floor(minutes / 60);
		return `${hours}h ${minutes % 60}m`;
	}

	function formatDate(dateStr: string | null): string {
		if (!dateStr) return '—';
		return new Date(dateStr).toLocaleString(undefined, {
			month: 'short',
			day: 'numeric',
			hour: '2-digit',
			minute: '2-digit',
		});
	}

	function getCurrentStepLabel(scan: ScanJob): string | null {
		if (scan.status !== 'RUNNING' || scan.current_step === null) return null;
		const step = scan.steps[scan.current_step];
		if (!step) return null;
		return step.check_type.replace(/_/g, ' ');
	}

	$effect(() => {
		loadDomains();
		loadScans();
	});
</script>

<svelte:head>
	<title>Scans — Reconova</title>
</svelte:head>

<div>
	<!-- Header -->
	<div class="flex items-center justify-between mb-6">
		<div>
			<h1 class="text-2xl font-bold text-white">Scans</h1>
			<p class="text-text-secondary text-sm mt-1">{totalCount} total scans</p>
		</div>
		<Button variant="primary" onclick={() => goto('/scans/new')}>New Scan</Button>
	</div>

	<!-- Filters -->
	<div class="flex gap-3 mb-6">
		<select
			bind:value={statusFilter}
			class="bg-surface border border-[rgba(255,255,255,0.08)] rounded-lg px-3 py-2 text-sm text-text focus:outline-none focus:border-brand/50 transition-colors"
			onchange={handleFilterChange}
		>
			<option value="">All statuses</option>
			<option value="QUEUED">Queued</option>
			<option value="RUNNING">Running</option>
			<option value="COMPLETED">Completed</option>
			<option value="PARTIAL">Partial</option>
			<option value="FAILED">Failed</option>
			<option value="CANCELLED">Cancelled</option>
		</select>

		<select
			bind:value={domainFilter}
			class="bg-surface border border-[rgba(255,255,255,0.08)] rounded-lg px-3 py-2 text-sm text-text focus:outline-none focus:border-brand/50 transition-colors"
			onchange={handleFilterChange}
		>
			<option value="">All domains</option>
			{#each domains as domain}
				<option value={domain.id}>{domain.domain}</option>
			{/each}
		</select>

		<select
			bind:value={dateFilter}
			class="bg-surface border border-[rgba(255,255,255,0.08)] rounded-lg px-3 py-2 text-sm text-text focus:outline-none focus:border-brand/50 transition-colors"
			onchange={handleFilterChange}
		>
			<option value="">All time</option>
			<option value="24h">Last 24 hours</option>
			<option value="7d">Last 7 days</option>
			<option value="30d">Last 30 days</option>
			<option value="90d">Last 90 days</option>
		</select>
	</div>

	{#if error}
		<Alert variant="error">{error}</Alert>
		<div class="mt-4"></div>
	{/if}

	{#if isLoading}
		<div class="bg-surface/60 backdrop-blur-sm rounded-xl border border-[rgba(255,255,255,0.08)] p-6">
			<SkeletonLoader lines={6} />
		</div>
	{:else if scans.length === 0}
		<div class="bg-surface/60 backdrop-blur-sm rounded-xl border border-[rgba(255,255,255,0.08)]">
			<EmptyState
				title="No scans found"
				description="Start a new scan to discover subdomains, ports, technologies, and vulnerabilities."
			>
				<Button variant="primary" onclick={() => goto('/scans/new')}>New Scan</Button>
			</EmptyState>
		</div>
	{:else}
		<!-- Scan Table -->
		<div class="bg-surface/60 backdrop-blur-sm rounded-xl border border-[rgba(255,255,255,0.08)] overflow-hidden">
			<table class="w-full">
				<thead>
					<tr class="border-b border-[rgba(255,255,255,0.08)]">
						<th class="text-left px-4 py-3 text-xs font-semibold uppercase tracking-wider text-text-muted">Domain</th>
						<th class="text-left px-4 py-3 text-xs font-semibold uppercase tracking-wider text-text-muted">Workflow</th>
						<th class="text-left px-4 py-3 text-xs font-semibold uppercase tracking-wider text-text-muted">Status</th>
						<th class="text-left px-4 py-3 text-xs font-semibold uppercase tracking-wider text-text-muted">Started</th>
						<th class="text-left px-4 py-3 text-xs font-semibold uppercase tracking-wider text-text-muted">Duration</th>
						<th class="text-right px-4 py-3 text-xs font-semibold uppercase tracking-wider text-text-muted">Credits</th>
						<th class="text-right px-4 py-3 text-xs font-semibold uppercase tracking-wider text-text-muted"></th>
					</tr>
				</thead>
				<tbody>
					{#each scans as scan}
						<tr
							class="border-b border-[rgba(255,255,255,0.04)] hover:bg-[rgba(255,255,255,0.02)] transition-colors cursor-pointer"
							onclick={() => goto(`/scans/${scan.id}`)}
						>
							<td class="px-4 py-3 text-sm text-text font-medium">{scan.domain_name}</td>
							<td class="px-4 py-3 text-sm text-text-secondary">{scan.workflow_name}</td>
							<td class="px-4 py-3">
								<div class="flex flex-col gap-1">
									<StatusBadge status={scan.status} />
									{#if getCurrentStepLabel(scan)}
										<span class="text-xs text-info flex items-center gap-1">
											<svg class="w-3 h-3 animate-spin" fill="none" viewBox="0 0 24 24">
												<circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
												<path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
											</svg>
											{getCurrentStepLabel(scan)}
										</span>
									{/if}
								</div>
							</td>
							<td class="px-4 py-3 text-sm text-text-secondary">{formatDate(scan.started_at || scan.created_at)}</td>
							<td class="px-4 py-3 text-sm text-text-secondary">{formatDuration(scan)}</td>
							<td class="px-4 py-3 text-sm text-text-secondary text-right">{scan.total_credits}</td>
							<td class="px-4 py-3 text-right">
								{#if scan.status === 'QUEUED' || scan.status === 'RUNNING'}
									<!-- svelte-ignore a11y_click_events_have_key_events -->
									<button
										class="text-xs text-danger hover:text-red-400 font-medium transition-colors disabled:opacity-50"
										disabled={cancellingId === scan.id}
										onclick={(e: MouseEvent) => { e.stopPropagation(); cancelScan(scan.id); }}
									>
										{cancellingId === scan.id ? 'Cancelling...' : 'Cancel'}
									</button>
								{:else}
									<a
										href="/scans/{scan.id}"
										class="text-brand text-sm font-medium hover:text-brand-dark transition-colors"
										onclick={(e: MouseEvent) => e.stopPropagation()}
									>View</a>
								{/if}
							</td>
						</tr>
					{/each}
				</tbody>
			</table>
		</div>

		<Pagination {currentPage} {totalPages} onPageChange={goToPage} />
	{/if}
</div>
