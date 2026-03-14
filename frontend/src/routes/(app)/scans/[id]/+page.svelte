<script lang="ts">
	import { scanApi } from '$lib/api/client';
	import { Button, Alert } from '$lib/components/ui';
	import { StatusBadge, SeverityBadge, SkeletonLoader } from '$lib/components/shared';
	import { page } from '$app/state';
	import { goto } from '$app/navigation';
	import type { ScanJob, ScanStep, StepStatus, ScanResults, Vulnerability, Screenshot } from '$lib/types/scans';
	import type { Subdomain, Port, Technology } from '$lib/types/domains';
	import type { ApiError } from '$lib/types/auth';

	let scan = $state<ScanJob | null>(null);
	let results = $state<ScanResults | null>(null);
	let isLoading = $state(true);
	let error = $state<string | null>(null);
	let isCancelling = $state(false);
	let pollTimer = $state<ReturnType<typeof setInterval> | null>(null);
	let activeTab = $state<'subdomains' | 'ports' | 'technologies' | 'vulnerabilities' | 'screenshots'>('subdomains');

	let scanId = $derived(page.params.id);
	let isActive = $derived(scan?.status === 'QUEUED' || scan?.status === 'RUNNING');
	let isTerminal = $derived(
		scan?.status === 'COMPLETED' ||
		scan?.status === 'PARTIAL' ||
		scan?.status === 'FAILED' ||
		scan?.status === 'CANCELLED'
	);

	// Result counts
	let subdomainCount = $derived(results?.subdomains?.length ?? 0);
	let portCount = $derived(results?.ports?.length ?? 0);
	let techCount = $derived(results?.technologies?.length ?? 0);
	let vulnCount = $derived(results?.vulnerabilities?.length ?? 0);
	let screenshotCount = $derived(results?.screenshots?.length ?? 0);

	async function loadScan() {
		try {
			scan = await scanApi.get(scanId) as ScanJob;
		} catch (err) {
			const apiErr = err as ApiError;
			error = apiErr.message || 'Failed to load scan.';
		} finally {
			isLoading = false;
		}
	}

	async function loadResults() {
		if (!scan || !isTerminal) return;
		try {
			results = await scanApi.getResults(scanId) as ScanResults;
		} catch {
			// Results may not be available yet
		}
	}

	async function cancelScan() {
		isCancelling = true;
		try {
			await scanApi.cancel(scanId);
			await loadScan();
		} catch (err) {
			const apiErr = err as ApiError;
			error = apiErr.message || 'Failed to cancel scan.';
		} finally {
			isCancelling = false;
		}
	}

	function startPolling() {
		stopPolling();
		pollTimer = setInterval(async () => {
			await loadScan();
			if (isTerminal) {
				stopPolling();
				loadResults();
			}
		}, 10000);
	}

	function stopPolling() {
		if (pollTimer) {
			clearInterval(pollTimer);
			pollTimer = null;
		}
	}

	function formatDuration(seconds: number | null): string {
		if (seconds === null) return '—';
		if (seconds < 60) return `${seconds}s`;
		const m = Math.floor(seconds / 60);
		const s = seconds % 60;
		if (m < 60) return `${m}m ${s}s`;
		const h = Math.floor(m / 60);
		return `${h}h ${m % 60}m`;
	}

	function formatScanDuration(s: ScanJob): string {
		if (!s.started_at) return '—';
		const start = new Date(s.started_at).getTime();
		const end = s.completed_at ? new Date(s.completed_at).getTime() : Date.now();
		const seconds = Math.floor((end - start) / 1000);
		return formatDuration(seconds);
	}

	function formatDate(dateStr: string | null): string {
		if (!dateStr) return '—';
		return new Date(dateStr).toLocaleString(undefined, {
			month: 'short',
			day: 'numeric',
			year: 'numeric',
			hour: '2-digit',
			minute: '2-digit',
		});
	}

	function formatCheckType(checkType: string): string {
		return checkType
			.replace(/_/g, ' ')
			.replace(/\b\w/g, (c) => c.toUpperCase());
	}

	function stepStatusIcon(status: StepStatus): string {
		switch (status) {
			case 'COMPLETED': return '\u2713';
			case 'RUNNING': return '\u25C9';
			case 'PENDING': return '\u25CB';
			case 'FAILED': return '\u2717';
			case 'SKIPPED': return '\u2014';
			case 'RETRYING': return '\u25C9';
			case 'CANCELLED': return '\u2014';
			default: return '\u25CB';
		}
	}

	function stepStatusColor(status: StepStatus): string {
		switch (status) {
			case 'COMPLETED': return 'border-green-500/40 bg-green-500/10 text-green-400';
			case 'RUNNING': return 'border-blue-500/40 bg-blue-500/10 text-blue-400';
			case 'PENDING': return 'border-[rgba(255,255,255,0.12)] bg-[rgba(255,255,255,0.03)] text-text-muted';
			case 'FAILED': return 'border-red-500/40 bg-red-500/10 text-red-400';
			case 'SKIPPED': return 'border-[rgba(255,255,255,0.12)] bg-[rgba(255,255,255,0.03)] text-text-muted';
			case 'RETRYING': return 'border-orange-500/40 bg-orange-500/10 text-orange-400';
			case 'CANCELLED': return 'border-[rgba(255,255,255,0.12)] bg-[rgba(255,255,255,0.03)] text-text-muted';
			default: return 'border-[rgba(255,255,255,0.12)] bg-[rgba(255,255,255,0.03)] text-text-muted';
		}
	}

	function connectorColor(status: StepStatus): string {
		switch (status) {
			case 'COMPLETED': return 'bg-green-500/40';
			case 'RUNNING': return 'bg-blue-500/40';
			case 'FAILED': return 'bg-red-500/40';
			default: return 'bg-[rgba(255,255,255,0.1)]';
		}
	}

	// Initial load
	$effect(() => {
		loadScan().then(() => {
			if (isActive) {
				startPolling();
			}
			if (isTerminal) {
				loadResults();
			}
		});

		return () => stopPolling();
	});

	// Watch for active state changes to manage polling
	$effect(() => {
		if (isActive) {
			startPolling();
		} else {
			stopPolling();
		}
	});
</script>

<svelte:head>
	<title>{scan ? `${scan.domain_name} Scan` : 'Scan Details'} — Reconova</title>
</svelte:head>

<div>
	{#if isLoading}
		<div class="bg-surface/60 backdrop-blur-sm rounded-xl border border-[rgba(255,255,255,0.08)] p-6">
			<SkeletonLoader lines={5} />
		</div>
	{:else if error && !scan}
		<Alert variant="error">{error}</Alert>
		<div class="mt-4">
			<Button variant="secondary" onclick={() => goto('/scans')}>Back to Scans</Button>
		</div>
	{:else if scan}
		<!-- Header -->
		<div class="flex items-start justify-between mb-6">
			<div>
				<div class="flex items-center gap-2 mb-1">
					<button onclick={() => goto('/scans')} class="text-text-muted hover:text-white transition-colors">
						<svg class="w-5 h-5" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
							<path stroke-linecap="round" stroke-linejoin="round" d="M15 19l-7-7 7-7" />
						</svg>
					</button>
					<h1 class="text-2xl font-bold text-white">{scan.domain_name}</h1>
					<span class="text-text-muted text-lg">/</span>
					<span class="text-lg text-text-secondary">{scan.workflow_name}</span>
				</div>
				<div class="flex items-center gap-4 mt-2">
					<StatusBadge status={scan.status} size="md" />
					<span class="text-sm text-text-muted">Started {formatDate(scan.started_at || scan.created_at)}</span>
					<span class="text-sm text-text-muted">Duration: {formatScanDuration(scan)}</span>
					<span class="text-sm text-text-muted">{scan.total_credits} credits</span>
				</div>
			</div>
			{#if isActive}
				<Button variant="destructive" onclick={cancelScan} loading={isCancelling}>
					{isCancelling ? 'Cancelling...' : 'Cancel Scan'}
				</Button>
			{/if}
		</div>

		{#if error}
			<Alert variant="error">{error}</Alert>
			<div class="mt-4"></div>
		{/if}

		<!-- Step Progress Pipeline -->
		<div class="bg-surface/60 backdrop-blur-sm rounded-xl border border-[rgba(255,255,255,0.08)] p-6 mb-6">
			<h2 class="text-sm font-semibold uppercase tracking-wider text-text-muted mb-4">Step Progress</h2>
			<div class="flex items-center overflow-x-auto pb-2 gap-0">
				{#each scan.steps as step, i}
					<!-- Step box -->
					<div class="flex items-center flex-shrink-0">
						<div class="flex flex-col items-center border rounded-lg px-4 py-3 min-w-[130px] {stepStatusColor(step.status)}">
							<div class="flex items-center gap-1.5 mb-1">
								{#if step.status === 'RUNNING'}
									<svg class="w-4 h-4 animate-spin text-blue-400" fill="none" viewBox="0 0 24 24">
										<circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
										<path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
									</svg>
								{:else if step.status === 'RETRYING'}
									<svg class="w-4 h-4 animate-spin text-orange-400" fill="none" viewBox="0 0 24 24">
										<circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
										<path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
									</svg>
								{:else}
									<span class="text-base font-bold">{stepStatusIcon(step.status)}</span>
								{/if}
							</div>
							<p class="text-xs font-medium text-center whitespace-nowrap">{formatCheckType(step.check_type)}</p>
							<p class="text-xs opacity-70 mt-0.5">{formatDuration(step.duration_seconds)}</p>
							{#if step.status === 'RETRYING'}
								<p class="text-xs opacity-70">attempt {step.attempt}/{step.max_attempts}</p>
							{/if}
						</div>

						<!-- Connector arrow -->
						{#if i < scan.steps.length - 1}
							<div class="flex items-center flex-shrink-0 mx-1">
								<div class="w-6 h-0.5 {connectorColor(step.status)}"></div>
								<svg class="w-3 h-3 -ml-1 {step.status === 'COMPLETED' ? 'text-green-500/60' : 'text-[rgba(255,255,255,0.15)]'}" fill="currentColor" viewBox="0 0 20 20">
									<path fill-rule="evenodd" d="M7.293 14.707a1 1 0 010-1.414L10.586 10 7.293 6.707a1 1 0 011.414-1.414l4 4a1 1 0 010 1.414l-4 4a1 1 0 01-1.414 0z" clip-rule="evenodd" />
								</svg>
							</div>
						{/if}
					</div>
				{/each}
			</div>
		</div>

		<!-- Results Tabs (shown when terminal) -->
		{#if isTerminal && results}
			<div class="bg-surface/60 backdrop-blur-sm rounded-xl border border-[rgba(255,255,255,0.08)] overflow-hidden">
				<!-- Tab bar -->
				<div class="flex border-b border-[rgba(255,255,255,0.08)] overflow-x-auto">
					<button
						class="px-4 py-3 text-sm font-medium whitespace-nowrap transition-colors {activeTab === 'subdomains' ? 'text-brand border-b-2 border-brand' : 'text-text-muted hover:text-white'}"
						onclick={() => activeTab = 'subdomains'}
					>Subdomains ({subdomainCount})</button>
					<button
						class="px-4 py-3 text-sm font-medium whitespace-nowrap transition-colors {activeTab === 'ports' ? 'text-brand border-b-2 border-brand' : 'text-text-muted hover:text-white'}"
						onclick={() => activeTab = 'ports'}
					>Ports ({portCount})</button>
					<button
						class="px-4 py-3 text-sm font-medium whitespace-nowrap transition-colors {activeTab === 'technologies' ? 'text-brand border-b-2 border-brand' : 'text-text-muted hover:text-white'}"
						onclick={() => activeTab = 'technologies'}
					>Technologies ({techCount})</button>
					<button
						class="px-4 py-3 text-sm font-medium whitespace-nowrap transition-colors {activeTab === 'vulnerabilities' ? 'text-brand border-b-2 border-brand' : 'text-text-muted hover:text-white'}"
						onclick={() => activeTab = 'vulnerabilities'}
					>Vulnerabilities ({vulnCount})</button>
					<button
						class="px-4 py-3 text-sm font-medium whitespace-nowrap transition-colors {activeTab === 'screenshots' ? 'text-brand border-b-2 border-brand' : 'text-text-muted hover:text-white'}"
						onclick={() => activeTab = 'screenshots'}
					>Screenshots ({screenshotCount})</button>
				</div>

				<!-- Tab content -->
				<div class="p-6">
					<!-- Subdomains -->
					{#if activeTab === 'subdomains'}
						{#if results.subdomains.length === 0}
							<p class="text-text-muted text-sm text-center py-8">No subdomains discovered.</p>
						{:else}
							<table class="w-full">
								<thead>
									<tr class="border-b border-[rgba(255,255,255,0.08)]">
										<th class="text-left pb-3 text-xs font-semibold uppercase tracking-wider text-text-muted">Subdomain</th>
										<th class="text-left pb-3 text-xs font-semibold uppercase tracking-wider text-text-muted">Source</th>
										<th class="text-left pb-3 text-xs font-semibold uppercase tracking-wider text-text-muted">First Seen</th>
									</tr>
								</thead>
								<tbody>
									{#each results.subdomains as sub}
										<tr class="border-b border-[rgba(255,255,255,0.04)]">
											<td class="py-2.5 text-sm text-text font-mono">{sub.subdomain}</td>
											<td class="py-2.5 text-sm text-text-secondary">{sub.source}</td>
											<td class="py-2.5 text-sm text-text-muted">{formatDate(sub.first_seen)}</td>
										</tr>
									{/each}
								</tbody>
							</table>
						{/if}
					{/if}

					<!-- Ports -->
					{#if activeTab === 'ports'}
						{#if results.ports.length === 0}
							<p class="text-text-muted text-sm text-center py-8">No open ports discovered.</p>
						{:else}
							<table class="w-full">
								<thead>
									<tr class="border-b border-[rgba(255,255,255,0.08)]">
										<th class="text-left pb-3 text-xs font-semibold uppercase tracking-wider text-text-muted">Host</th>
										<th class="text-left pb-3 text-xs font-semibold uppercase tracking-wider text-text-muted">Port</th>
										<th class="text-left pb-3 text-xs font-semibold uppercase tracking-wider text-text-muted">Protocol</th>
										<th class="text-left pb-3 text-xs font-semibold uppercase tracking-wider text-text-muted">Service</th>
									</tr>
								</thead>
								<tbody>
									{#each results.ports as port}
										<tr class="border-b border-[rgba(255,255,255,0.04)]">
											<td class="py-2.5 text-sm text-text font-mono">{port.subdomain_name}</td>
											<td class="py-2.5 text-sm text-text font-mono">{port.port}</td>
											<td class="py-2.5 text-sm text-text-secondary uppercase">{port.protocol}</td>
											<td class="py-2.5 text-sm text-text-secondary">{port.service || '—'}</td>
										</tr>
									{/each}
								</tbody>
							</table>
						{/if}
					{/if}

					<!-- Technologies -->
					{#if activeTab === 'technologies'}
						{#if results.technologies.length === 0}
							<p class="text-text-muted text-sm text-center py-8">No technologies detected.</p>
						{:else}
							<table class="w-full">
								<thead>
									<tr class="border-b border-[rgba(255,255,255,0.08)]">
										<th class="text-left pb-3 text-xs font-semibold uppercase tracking-wider text-text-muted">Host</th>
										<th class="text-left pb-3 text-xs font-semibold uppercase tracking-wider text-text-muted">Technology</th>
										<th class="text-left pb-3 text-xs font-semibold uppercase tracking-wider text-text-muted">Version</th>
										<th class="text-left pb-3 text-xs font-semibold uppercase tracking-wider text-text-muted">Category</th>
									</tr>
								</thead>
								<tbody>
									{#each results.technologies as tech}
										<tr class="border-b border-[rgba(255,255,255,0.04)]">
											<td class="py-2.5 text-sm text-text font-mono">{tech.subdomain_name}</td>
											<td class="py-2.5 text-sm text-text font-medium">{tech.tech_name}</td>
											<td class="py-2.5 text-sm text-text-secondary">{tech.version || '—'}</td>
											<td class="py-2.5 text-sm text-text-muted">{tech.category}</td>
										</tr>
									{/each}
								</tbody>
							</table>
						{/if}
					{/if}

					<!-- Vulnerabilities -->
					{#if activeTab === 'vulnerabilities'}
						{#if results.vulnerabilities.length === 0}
							<p class="text-text-muted text-sm text-center py-8">No vulnerabilities found.</p>
						{:else}
							<div class="space-y-3">
								{#each results.vulnerabilities as vuln}
									<div class="border border-[rgba(255,255,255,0.08)] rounded-lg p-4">
										<div class="flex items-start justify-between mb-2">
											<div class="flex items-center gap-2">
												<SeverityBadge severity={vuln.severity} />
												{#if vuln.cve}
													<span class="text-sm font-mono text-text-secondary">{vuln.cve}</span>
												{/if}
											</div>
											<span class="text-xs text-text-muted font-mono">{vuln.subdomain_name}</span>
										</div>
										<p class="text-sm text-text mt-1">{vuln.description}</p>
										{#if vuln.remediation}
											<div class="mt-3 p-3 rounded bg-[rgba(255,255,255,0.03)] border border-[rgba(255,255,255,0.06)]">
												<p class="text-xs font-semibold uppercase tracking-wider text-text-muted mb-1">Remediation</p>
												<p class="text-sm text-text-secondary">{vuln.remediation}</p>
											</div>
										{/if}
									</div>
								{/each}
							</div>
						{/if}
					{/if}

					<!-- Screenshots -->
					{#if activeTab === 'screenshots'}
						{#if results.screenshots.length === 0}
							<p class="text-text-muted text-sm text-center py-8">No screenshots captured.</p>
						{:else}
							<div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4">
								{#each results.screenshots as screenshot}
									<div class="border border-[rgba(255,255,255,0.08)] rounded-lg overflow-hidden group">
										<div class="aspect-video bg-[rgba(255,255,255,0.03)] relative">
											<img
												src={screenshot.image_url}
												alt="Screenshot of {screenshot.subdomain_name}"
												class="w-full h-full object-cover"
												loading="lazy"
											/>
										</div>
										<div class="px-3 py-2">
											<p class="text-xs text-text font-mono truncate">{screenshot.subdomain_name}</p>
											<p class="text-xs text-text-muted mt-0.5">{formatDate(screenshot.taken_at)}</p>
										</div>
									</div>
								{/each}
							</div>
						{/if}
					{/if}
				</div>
			</div>
		{:else if isActive}
			<div class="bg-surface/60 backdrop-blur-sm rounded-xl border border-[rgba(255,255,255,0.08)] p-8 text-center">
				<svg class="w-8 h-8 animate-spin text-brand mx-auto mb-3" fill="none" viewBox="0 0 24 24">
					<circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
					<path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
				</svg>
				<p class="text-text-secondary text-sm">Scan in progress. Results will appear here when complete.</p>
				<p class="text-text-muted text-xs mt-1">Auto-refreshing every 10 seconds</p>
			</div>
		{/if}
	{/if}
</div>
