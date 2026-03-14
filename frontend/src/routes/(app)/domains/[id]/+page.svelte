<script lang="ts">
	import { page } from '$app/state';
	import { domainApi } from '$lib/api/client';
	import type { DomainDetails, Subdomain, Port, Technology } from '$lib/types/domains';
	import type { ScanJob, Paginated } from '$lib/types/scans';
	import type { ApiError } from '$lib/types/auth';
	import { Button, Alert } from '$lib/components/ui';
	import { Pagination, StatusBadge, EmptyState, SkeletonLoader } from '$lib/components/shared';

	// Domain state
	let domain = $state<DomainDetails | null>(null);
	let isLoading = $state(true);
	let error = $state<string | null>(null);

	// Tab state
	let activeTab = $state<'overview' | 'scans'>('overview');

	// Subdomains pagination
	let subdomains = $state<Subdomain[]>([]);
	let subdomainsPage = $state(1);
	let subdomainsTotalPages = $state(1);
	let subdomainsLoading = $state(false);

	// Ports pagination
	let ports = $state<Port[]>([]);
	let portsPage = $state(1);
	let portsTotalPages = $state(1);
	let portsLoading = $state(false);

	// Technologies pagination
	let technologies = $state<Technology[]>([]);
	let techPage = $state(1);
	let techTotalPages = $state(1);
	let techLoading = $state(false);

	// Scans pagination
	let scans = $state<ScanJob[]>([]);
	let scansPage = $state(1);
	let scansTotalPages = $state(1);
	let scansLoading = $state(false);

	const pageSize = 10;

	async function loadDomain() {
		isLoading = true;
		error = null;
		try {
			domain = (await domainApi.get(page.params.id)) as DomainDetails;
		} catch (err) {
			const apiErr = err as ApiError;
			error = apiErr.message || 'Failed to load domain.';
		} finally {
			isLoading = false;
		}
	}

	async function loadSubdomains() {
		subdomainsLoading = true;
		try {
			const res = (await domainApi.getSubdomains(page.params.id, { page: subdomainsPage, size: pageSize })) as Paginated<Subdomain>;
			subdomains = res.data;
			subdomainsTotalPages = res.total_pages;
		} catch {
			subdomains = [];
		} finally {
			subdomainsLoading = false;
		}
	}

	async function loadPorts() {
		portsLoading = true;
		try {
			const res = (await domainApi.getPorts(page.params.id, { page: portsPage, size: pageSize })) as Paginated<Port>;
			ports = res.data;
			portsTotalPages = res.total_pages;
		} catch {
			ports = [];
		} finally {
			portsLoading = false;
		}
	}

	async function loadTechnologies() {
		techLoading = true;
		try {
			const res = (await domainApi.getTechnologies(page.params.id, { page: techPage, size: pageSize })) as Paginated<Technology>;
			technologies = res.data;
			techTotalPages = res.total_pages;
		} catch {
			technologies = [];
		} finally {
			techLoading = false;
		}
	}

	async function loadScans() {
		scansLoading = true;
		try {
			const res = (await domainApi.getScans(page.params.id, { page: scansPage, size: pageSize })) as Paginated<ScanJob>;
			scans = res.data;
			scansTotalPages = res.total_pages;
		} catch {
			scans = [];
		} finally {
			scansLoading = false;
		}
	}

	$effect(() => {
		loadDomain();
		loadSubdomains();
		loadPorts();
		loadTechnologies();
		loadScans();
	});

	function formatDate(dateStr: string | null): string {
		if (!dateStr) return '—';
		return new Date(dateStr).toLocaleDateString();
	}

	function formatDateTime(dateStr: string | null): string {
		if (!dateStr) return '—';
		return new Date(dateStr).toLocaleString();
	}

	function formatDuration(startedAt: string | null, completedAt: string | null): string {
		if (!startedAt || !completedAt) return '—';
		const ms = new Date(completedAt).getTime() - new Date(startedAt).getTime();
		if (ms < 1000) return `${ms}ms`;
		const seconds = Math.floor(ms / 1000);
		if (seconds < 60) return `${seconds}s`;
		const minutes = Math.floor(seconds / 60);
		const remainSec = seconds % 60;
		return `${minutes}m ${remainSec}s`;
	}

	function handleSubdomainsPage(p: number) {
		subdomainsPage = p;
		loadSubdomains();
	}

	function handlePortsPage(p: number) {
		portsPage = p;
		loadPorts();
	}

	function handleTechPage(p: number) {
		techPage = p;
		loadTechnologies();
	}

	function handleScansPage(p: number) {
		scansPage = p;
		loadScans();
	}
</script>

<svelte:head>
	<title>{domain?.domain ?? 'Domain Detail'} — Reconova</title>
</svelte:head>

<div>
	<!-- Back link -->
	<a href="/domains" class="text-brand text-sm font-medium hover:text-brand-dark transition-colors inline-flex items-center gap-1 mb-6">
		<svg class="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
			<path stroke-linecap="round" stroke-linejoin="round" d="M10.5 19.5 3 12m0 0 7.5-7.5M3 12h18" />
		</svg>
		Back to Domains
	</a>

	{#if error}
		<div class="mb-4">
			<Alert variant="error">{error}</Alert>
		</div>
	{/if}

	{#if isLoading}
		<div class="bg-surface rounded-xl border border-[rgba(255,255,255,0.08)] p-6">
			<SkeletonLoader lines={4} />
		</div>
	{:else if domain}
		<!-- Domain Header -->
		<div class="flex items-center gap-3 mb-6">
			<h1 class="text-2xl font-bold text-white">{domain.domain}</h1>
			<StatusBadge status={domain.status} size="md" />
		</div>

		<!-- Stats Summary -->
		<div class="grid grid-cols-3 gap-4 mb-6">
			<div class="bg-surface rounded-xl border border-[rgba(255,255,255,0.08)] p-4">
				<p class="text-xs text-text-muted uppercase tracking-wider mb-1">Subdomains</p>
				<p class="text-xl font-bold text-white">{domain.subdomain_count}</p>
			</div>
			<div class="bg-surface rounded-xl border border-[rgba(255,255,255,0.08)] p-4">
				<p class="text-xs text-text-muted uppercase tracking-wider mb-1">Open Ports</p>
				<p class="text-xl font-bold text-white">{domain.port_count}</p>
			</div>
			<div class="bg-surface rounded-xl border border-[rgba(255,255,255,0.08)] p-4">
				<p class="text-xs text-text-muted uppercase tracking-wider mb-1">Technologies</p>
				<p class="text-xl font-bold text-white">{domain.technology_count}</p>
			</div>
		</div>

		<!-- Tabs -->
		<div class="flex gap-1 mb-6 bg-surface rounded-lg border border-[rgba(255,255,255,0.08)] p-1 w-fit">
			<button
				class="px-4 py-2 text-sm font-medium rounded-md transition-colors {activeTab === 'overview' ? 'bg-brand/20 text-brand' : 'text-text-secondary hover:text-white'}"
				onclick={() => { activeTab = 'overview'; }}
			>
				Overview
			</button>
			<button
				class="px-4 py-2 text-sm font-medium rounded-md transition-colors {activeTab === 'scans' ? 'bg-brand/20 text-brand' : 'text-text-secondary hover:text-white'}"
				onclick={() => { activeTab = 'scans'; }}
			>
				Scan History
			</button>
		</div>

		{#if activeTab === 'overview'}
			<!-- Subdomains Section -->
			<div class="mb-6">
				<h2 class="text-lg font-semibold text-white mb-3">Subdomains</h2>
				{#if subdomainsLoading}
					<div class="bg-surface rounded-xl border border-[rgba(255,255,255,0.08)] p-6">
						<SkeletonLoader lines={3} />
					</div>
				{:else if subdomains.length === 0}
					<div class="bg-surface rounded-xl border border-[rgba(255,255,255,0.08)]">
						<EmptyState title="No subdomains discovered" description="Run a scan to discover subdomains." />
					</div>
				{:else}
					<div class="bg-surface rounded-xl border border-[rgba(255,255,255,0.08)] overflow-hidden">
						<table class="w-full">
							<thead>
								<tr class="border-b border-[rgba(255,255,255,0.08)]">
									<th class="text-left px-4 py-3 text-xs font-semibold uppercase tracking-wider text-text-muted">Subdomain</th>
									<th class="text-left px-4 py-3 text-xs font-semibold uppercase tracking-wider text-text-muted">Source</th>
									<th class="text-left px-4 py-3 text-xs font-semibold uppercase tracking-wider text-text-muted">First Seen</th>
									<th class="text-left px-4 py-3 text-xs font-semibold uppercase tracking-wider text-text-muted">Last Seen</th>
								</tr>
							</thead>
							<tbody>
								{#each subdomains as sub}
									<tr class="border-b border-[rgba(255,255,255,0.04)] hover:bg-[rgba(255,255,255,0.02)] transition-colors">
										<td class="px-4 py-3 text-sm text-text font-mono">{sub.subdomain}</td>
										<td class="px-4 py-3 text-sm text-text-secondary">{sub.source}</td>
										<td class="px-4 py-3 text-sm text-text-secondary">{formatDate(sub.first_seen)}</td>
										<td class="px-4 py-3 text-sm text-text-secondary">{formatDate(sub.last_seen)}</td>
									</tr>
								{/each}
							</tbody>
						</table>
					</div>
					<Pagination currentPage={subdomainsPage} totalPages={subdomainsTotalPages} onPageChange={handleSubdomainsPage} />
				{/if}
			</div>

			<!-- Ports Section -->
			<div class="mb-6">
				<h2 class="text-lg font-semibold text-white mb-3">Ports</h2>
				{#if portsLoading}
					<div class="bg-surface rounded-xl border border-[rgba(255,255,255,0.08)] p-6">
						<SkeletonLoader lines={3} />
					</div>
				{:else if ports.length === 0}
					<div class="bg-surface rounded-xl border border-[rgba(255,255,255,0.08)]">
						<EmptyState title="No ports discovered" description="Run a scan to discover open ports." />
					</div>
				{:else}
					<div class="bg-surface rounded-xl border border-[rgba(255,255,255,0.08)] overflow-hidden">
						<table class="w-full">
							<thead>
								<tr class="border-b border-[rgba(255,255,255,0.08)]">
									<th class="text-left px-4 py-3 text-xs font-semibold uppercase tracking-wider text-text-muted">Port</th>
									<th class="text-left px-4 py-3 text-xs font-semibold uppercase tracking-wider text-text-muted">Protocol</th>
									<th class="text-left px-4 py-3 text-xs font-semibold uppercase tracking-wider text-text-muted">Service</th>
									<th class="text-left px-4 py-3 text-xs font-semibold uppercase tracking-wider text-text-muted">Subdomain</th>
									<th class="text-left px-4 py-3 text-xs font-semibold uppercase tracking-wider text-text-muted">Discovered</th>
								</tr>
							</thead>
							<tbody>
								{#each ports as port}
									<tr class="border-b border-[rgba(255,255,255,0.04)] hover:bg-[rgba(255,255,255,0.02)] transition-colors">
										<td class="px-4 py-3 text-sm text-text font-mono font-medium">{port.port}</td>
										<td class="px-4 py-3 text-sm text-text-secondary uppercase">{port.protocol}</td>
										<td class="px-4 py-3 text-sm text-text-secondary">{port.service ?? '—'}</td>
										<td class="px-4 py-3 text-sm text-text-secondary font-mono">{port.subdomain_name}</td>
										<td class="px-4 py-3 text-sm text-text-secondary">{formatDate(port.discovered_at)}</td>
									</tr>
								{/each}
							</tbody>
						</table>
					</div>
					<Pagination currentPage={portsPage} totalPages={portsTotalPages} onPageChange={handlePortsPage} />
				{/if}
			</div>

			<!-- Technologies Section -->
			<div class="mb-6">
				<h2 class="text-lg font-semibold text-white mb-3">Technologies</h2>
				{#if techLoading}
					<div class="bg-surface rounded-xl border border-[rgba(255,255,255,0.08)] p-6">
						<SkeletonLoader lines={3} />
					</div>
				{:else if technologies.length === 0}
					<div class="bg-surface rounded-xl border border-[rgba(255,255,255,0.08)]">
						<EmptyState title="No technologies detected" description="Run a scan to detect technologies." />
					</div>
				{:else}
					<div class="bg-surface rounded-xl border border-[rgba(255,255,255,0.08)] overflow-hidden">
						<table class="w-full">
							<thead>
								<tr class="border-b border-[rgba(255,255,255,0.08)]">
									<th class="text-left px-4 py-3 text-xs font-semibold uppercase tracking-wider text-text-muted">Technology</th>
									<th class="text-left px-4 py-3 text-xs font-semibold uppercase tracking-wider text-text-muted">Version</th>
									<th class="text-left px-4 py-3 text-xs font-semibold uppercase tracking-wider text-text-muted">Category</th>
									<th class="text-left px-4 py-3 text-xs font-semibold uppercase tracking-wider text-text-muted">Subdomain</th>
									<th class="text-left px-4 py-3 text-xs font-semibold uppercase tracking-wider text-text-muted">Detected</th>
								</tr>
							</thead>
							<tbody>
								{#each technologies as tech}
									<tr class="border-b border-[rgba(255,255,255,0.04)] hover:bg-[rgba(255,255,255,0.02)] transition-colors">
										<td class="px-4 py-3 text-sm text-text font-medium">{tech.tech_name}</td>
										<td class="px-4 py-3 text-sm text-text-secondary font-mono">{tech.version ?? '—'}</td>
										<td class="px-4 py-3 text-sm text-text-secondary">{tech.category}</td>
										<td class="px-4 py-3 text-sm text-text-secondary font-mono">{tech.subdomain_name}</td>
										<td class="px-4 py-3 text-sm text-text-secondary">{formatDate(tech.detected_at)}</td>
									</tr>
								{/each}
							</tbody>
						</table>
					</div>
					<Pagination currentPage={techPage} totalPages={techTotalPages} onPageChange={handleTechPage} />
				{/if}
			</div>
		{:else}
			<!-- Scan History Tab -->
			<div>
				<h2 class="text-lg font-semibold text-white mb-3">Scan History</h2>
				{#if scansLoading}
					<div class="bg-surface rounded-xl border border-[rgba(255,255,255,0.08)] p-6">
						<SkeletonLoader lines={4} />
					</div>
				{:else if scans.length === 0}
					<div class="bg-surface rounded-xl border border-[rgba(255,255,255,0.08)]">
						<EmptyState title="No scans yet" description="Start a scan to see results here." />
					</div>
				{:else}
					<div class="bg-surface rounded-xl border border-[rgba(255,255,255,0.08)] overflow-hidden">
						<table class="w-full">
							<thead>
								<tr class="border-b border-[rgba(255,255,255,0.08)]">
									<th class="text-left px-4 py-3 text-xs font-semibold uppercase tracking-wider text-text-muted">Status</th>
									<th class="text-left px-4 py-3 text-xs font-semibold uppercase tracking-wider text-text-muted">Workflow</th>
									<th class="text-left px-4 py-3 text-xs font-semibold uppercase tracking-wider text-text-muted">Started</th>
									<th class="text-left px-4 py-3 text-xs font-semibold uppercase tracking-wider text-text-muted">Duration</th>
									<th class="text-left px-4 py-3 text-xs font-semibold uppercase tracking-wider text-text-muted">Credits</th>
								</tr>
							</thead>
							<tbody>
								{#each scans as scan}
									<tr class="border-b border-[rgba(255,255,255,0.04)] hover:bg-[rgba(255,255,255,0.02)] transition-colors">
										<td class="px-4 py-3">
											<StatusBadge status={scan.status} />
										</td>
										<td class="px-4 py-3 text-sm text-text">{scan.workflow_name}</td>
										<td class="px-4 py-3 text-sm text-text-secondary">{formatDateTime(scan.started_at ?? scan.created_at)}</td>
										<td class="px-4 py-3 text-sm text-text-secondary">{formatDuration(scan.started_at, scan.completed_at)}</td>
										<td class="px-4 py-3 text-sm text-text-secondary">{scan.total_credits}</td>
									</tr>
								{/each}
							</tbody>
						</table>
					</div>
					<Pagination currentPage={scansPage} totalPages={scansTotalPages} onPageChange={handleScansPage} />
				{/if}
			</div>
		{/if}
	{/if}
</div>
