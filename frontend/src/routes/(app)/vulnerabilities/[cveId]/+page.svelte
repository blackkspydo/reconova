<script lang="ts">
	import { page } from '$app/state';
	import { cveApi } from '$lib/api/client';
	import type { CveEntry, VulnerabilityAlert } from '$lib/types/cve';
	import type { Paginated } from '$lib/types/scans';
	import type { ApiError } from '$lib/types/auth';
	import { Button, Alert } from '$lib/components/ui';
	import { SeverityBadge, StatusBadge, SkeletonLoader, EmptyState } from '$lib/components/shared';

	// CVE state
	let cve = $state<CveEntry | null>(null);
	let isLoading = $state(true);
	let error = $state<string | null>(null);

	// Related alerts
	let relatedAlerts = $state<VulnerabilityAlert[]>([]);
	let alertsLoading = $state(false);

	let cveId = $derived(page.params.cveId);

	async function loadCve() {
		isLoading = true;
		error = null;
		try {
			cve = (await cveApi.get(cveId)) as CveEntry;
		} catch (err) {
			const apiErr = err as ApiError;
			error = apiErr.message || 'Failed to load CVE details.';
		} finally {
			isLoading = false;
		}
	}

	async function loadRelatedAlerts() {
		alertsLoading = true;
		try {
			const res = (await cveApi.getAlerts({ cve_id: cveId, page_size: 50 })) as Paginated<VulnerabilityAlert>;
			relatedAlerts = res.data;
		} catch {
			relatedAlerts = [];
		} finally {
			alertsLoading = false;
		}
	}

	$effect(() => {
		loadCve();
		loadRelatedAlerts();
	});

	function formatDate(dateStr: string | null): string {
		if (!dateStr) return '\u2014';
		return new Date(dateStr).toLocaleDateString(undefined, {
			month: 'short',
			day: 'numeric',
			year: 'numeric',
		});
	}

	function formatDateTime(dateStr: string | null): string {
		if (!dateStr) return '\u2014';
		return new Date(dateStr).toLocaleString(undefined, {
			month: 'short',
			day: 'numeric',
			year: 'numeric',
			hour: '2-digit',
			minute: '2-digit',
		});
	}

	function cvssColor(score: number | null): string {
		if (score === null) return 'border-[rgba(255,255,255,0.12)] text-text-muted';
		if (score >= 9.0) return 'border-red-500 text-red-400';
		if (score >= 7.0) return 'border-orange-500 text-orange-400';
		if (score >= 4.0) return 'border-yellow-500 text-yellow-400';
		return 'border-blue-500 text-blue-400';
	}
</script>

<svelte:head>
	<title>{cve?.cve_id ?? 'CVE Detail'} \u2014 Reconova</title>
</svelte:head>

<div>
	<!-- Back link -->
	<a href="/vulnerabilities" class="text-brand text-sm font-medium hover:text-brand-dark transition-colors inline-flex items-center gap-1 mb-6">
		<svg class="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
			<path stroke-linecap="round" stroke-linejoin="round" d="M10.5 19.5 3 12m0 0 7.5-7.5M3 12h18" />
		</svg>
		Back to Vulnerabilities
	</a>

	{#if error}
		<div class="mb-4">
			<Alert variant="error">{error}</Alert>
		</div>
	{/if}

	{#if isLoading}
		<div class="bg-surface rounded-xl border border-[rgba(255,255,255,0.08)] p-6">
			<SkeletonLoader lines={5} />
		</div>
	{:else if cve}
		<!-- CVE Header -->
		<div class="flex items-center gap-3 mb-6">
			<h1 class="text-2xl font-bold text-white font-mono">{cve.cve_id}</h1>
			<SeverityBadge severity={cve.severity} />
		</div>

		<!-- Info Card -->
		<div class="bg-surface/60 backdrop-blur-sm rounded-xl border border-[rgba(255,255,255,0.08)] p-6 mb-6">
			<div class="grid grid-cols-2 md:grid-cols-4 gap-6">
				<!-- CVSS Score -->
				<div class="flex items-center gap-3">
					<div class="w-12 h-12 rounded-full border-2 flex items-center justify-center {cvssColor(cve.cvss_score)}">
						<span class="text-sm font-bold">{cve.cvss_score !== null ? cve.cvss_score.toFixed(1) : 'N/A'}</span>
					</div>
					<div>
						<p class="text-xs text-text-muted uppercase tracking-wider">CVSS Score</p>
						<p class="text-sm text-white font-medium">{cve.severity}</p>
					</div>
				</div>

				<!-- Severity -->
				<div>
					<p class="text-xs text-text-muted uppercase tracking-wider mb-1">Severity</p>
					<SeverityBadge severity={cve.severity} />
				</div>

				<!-- Published -->
				<div>
					<p class="text-xs text-text-muted uppercase tracking-wider mb-1">Published</p>
					<p class="text-sm text-white">{formatDate(cve.published_at)}</p>
				</div>

				<!-- Modified -->
				<div>
					<p class="text-xs text-text-muted uppercase tracking-wider mb-1">Last Modified</p>
					<p class="text-sm text-white">{formatDate(cve.modified_at)}</p>
				</div>
			</div>
		</div>

		<!-- Description -->
		<div class="bg-surface/60 backdrop-blur-sm rounded-xl border border-[rgba(255,255,255,0.08)] p-6 mb-6">
			<h2 class="text-sm font-semibold uppercase tracking-wider text-text-muted mb-3">Description</h2>
			<p class="text-sm text-text-secondary leading-relaxed">{cve.description}</p>
		</div>

		<!-- Affected Products -->
		<div class="bg-surface/60 backdrop-blur-sm rounded-xl border border-[rgba(255,255,255,0.08)] p-6 mb-6">
			<h2 class="text-sm font-semibold uppercase tracking-wider text-text-muted mb-3">Affected Products</h2>
			{#if cve.affected_products.length === 0}
				<p class="text-sm text-text-muted">No affected products listed.</p>
			{:else}
				<div class="flex flex-wrap gap-2">
					{#each cve.affected_products as product}
						<span class="text-xs font-medium px-3 py-1.5 rounded-lg bg-[rgba(255,255,255,0.05)] border border-[rgba(255,255,255,0.08)] text-text-secondary font-mono">
							{product}
						</span>
					{/each}
				</div>
			{/if}
		</div>

		<!-- References -->
		<div class="bg-surface/60 backdrop-blur-sm rounded-xl border border-[rgba(255,255,255,0.08)] p-6 mb-6">
			<h2 class="text-sm font-semibold uppercase tracking-wider text-text-muted mb-3">References</h2>
			{#if cve.references.length === 0}
				<p class="text-sm text-text-muted">No references available.</p>
			{:else}
				<ul class="space-y-2">
					{#each cve.references as ref}
						<li>
							<a
								href={ref}
								target="_blank"
								rel="noopener noreferrer"
								class="text-sm text-brand hover:text-brand-dark transition-colors break-all inline-flex items-center gap-1"
							>
								{ref}
								<svg class="w-3.5 h-3.5 flex-shrink-0" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
									<path stroke-linecap="round" stroke-linejoin="round" d="M13.5 6H5.25A2.25 2.25 0 0 0 3 8.25v10.5A2.25 2.25 0 0 0 5.25 21h10.5A2.25 2.25 0 0 0 18 18.75V10.5m-10.5 6L21 3m0 0h-5.25M21 3v5.25" />
								</svg>
							</a>
						</li>
					{/each}
				</ul>
			{/if}
		</div>

		<!-- Related Alerts -->
		<div class="bg-surface/60 backdrop-blur-sm rounded-xl border border-[rgba(255,255,255,0.08)] p-6">
			<h2 class="text-sm font-semibold uppercase tracking-wider text-text-muted mb-3">
				Related Alerts
				{#if relatedAlerts.length > 0}
					<span class="text-xs font-medium px-2 py-0.5 rounded-full bg-brand/10 text-brand ml-2">
						{relatedAlerts.length}
					</span>
				{/if}
			</h2>
			{#if alertsLoading}
				<SkeletonLoader lines={3} />
			{:else if relatedAlerts.length === 0}
				<p class="text-sm text-text-muted">No alerts associated with this CVE.</p>
			{:else}
				<div class="space-y-2">
					{#each relatedAlerts as alert}
						<div class="flex items-center justify-between py-2.5 px-3 rounded-lg border border-[rgba(255,255,255,0.06)] hover:border-[rgba(255,255,255,0.12)] transition-colors">
							<div class="flex items-center gap-3 min-w-0">
								<SeverityBadge severity={alert.severity} />
								<span class="text-sm text-text-secondary font-mono truncate">{alert.subdomain_name}</span>
								<span class="text-xs text-text-muted">{alert.domain_name}</span>
							</div>
							<div class="flex items-center gap-3 flex-shrink-0">
								<StatusBadge status={alert.status} />
								<span class="text-xs text-text-muted">{formatDateTime(alert.created_at)}</span>
							</div>
						</div>
					{/each}
				</div>
			{/if}
		</div>
	{/if}
</div>
