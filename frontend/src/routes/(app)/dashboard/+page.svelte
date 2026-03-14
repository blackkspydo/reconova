<script lang="ts">
	import { getAuthStore } from '$lib/stores/auth';
	import { domainApi, scanApi, billingApi } from '$lib/api/client';
	import type { Domain } from '$lib/types/domains';
	import type { ScanJob } from '$lib/types/scans';
	import type { CreditBalance } from '$lib/types/billing';
	import { Button } from '$lib/components/ui';
	import { StatusBadge, SkeletonLoader } from '$lib/components/shared';

	const auth = getAuthStore();

	let loading = $state(true);
	let domains = $state<Domain[]>([]);
	let scans = $state<ScanJob[]>([]);
	let credits = $state<CreditBalance | null>(null);

	let completedScans = $derived(scans.filter((s) => s.status === 'COMPLETED'));
	let recentScans = $derived(scans.slice(0, 5));

	$effect(() => {
		loadDashboard();
	});

	async function loadDashboard() {
		loading = true;
		try {
			const [domainRes, scanRes, creditRes] = await Promise.all([
				domainApi.list(),
				scanApi.list({ page: '1', size: '20' }),
				billingApi.getCredits()
			]);
			domains = domainRes.data ?? domainRes ?? [];
			const scanData = scanRes.data ?? scanRes ?? [];
			scans = Array.isArray(scanData) ? scanData : [];
			credits = creditRes;
		} catch (e) {
			console.error('Failed to load dashboard data', e);
		} finally {
			loading = false;
		}
	}

	function formatDate(dateStr: string | null): string {
		if (!dateStr) return '-';
		return new Date(dateStr).toLocaleDateString('en-US', {
			month: 'short',
			day: 'numeric',
			year: 'numeric'
		});
	}
</script>

<svelte:head>
	<title>Dashboard — Reconova</title>
</svelte:head>

<div class="space-y-8">
	<!-- Welcome -->
	<div>
		<h1 class="text-2xl font-bold text-white">Dashboard</h1>
		<p class="text-text-secondary mt-1">Welcome back, {auth.user?.email}</p>
	</div>

	<!-- Stat Cards -->
	{#if loading}
		<div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
			{#each Array(4) as _}
				<div class="bg-surface rounded-xl border border-[rgba(255,255,255,0.08)] p-5">
					<SkeletonLoader lines={2} width="half" />
				</div>
			{/each}
		</div>
	{:else}
		<div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
			<!-- Domains -->
			<div class="bg-surface rounded-xl border border-[rgba(255,255,255,0.08)] p-5 space-y-2">
				<p class="text-text-muted text-xs uppercase tracking-wider">Domains</p>
				<p class="text-2xl font-bold text-white">{domains.length}</p>
				<a href="/domains" class="text-brand text-xs hover:underline">View All</a>
			</div>

			<!-- Scans -->
			<div class="bg-surface rounded-xl border border-[rgba(255,255,255,0.08)] p-5 space-y-2">
				<p class="text-text-muted text-xs uppercase tracking-wider">Scans</p>
				<p class="text-2xl font-bold text-white">{completedScans.length}</p>
				<a href="/scans" class="text-brand text-xs hover:underline">View All</a>
			</div>

			<!-- Credits -->
			<div class="bg-surface rounded-xl border border-[rgba(255,255,255,0.08)] p-5 space-y-2">
				<p class="text-text-muted text-xs uppercase tracking-wider">Credits</p>
				<p class="text-2xl font-bold text-white">{credits?.total_available ?? 0}</p>
				<a href="/billing" class="text-brand text-xs hover:underline">View Balance</a>
			</div>

			<!-- Recent Scans Count -->
			<div class="bg-surface rounded-xl border border-[rgba(255,255,255,0.08)] p-5 space-y-2">
				<p class="text-text-muted text-xs uppercase tracking-wider">Recent Scans</p>
				<p class="text-2xl font-bold text-white">{recentScans.length}</p>
			</div>
		</div>
	{/if}

	<!-- Recent Scans Table -->
	<div class="bg-surface rounded-xl border border-[rgba(255,255,255,0.08)] p-6">
		<h2 class="text-lg font-semibold text-white mb-4">Recent Scans</h2>
		{#if loading}
			<SkeletonLoader lines={5} />
		{:else if recentScans.length === 0}
			<p class="text-text-muted text-sm">No scans yet. Start your first scan to see results here.</p>
		{:else}
			<div class="overflow-x-auto">
				<table class="w-full text-sm">
					<thead>
						<tr class="border-b border-[rgba(255,255,255,0.08)]">
							<th class="text-left text-text-muted text-xs uppercase tracking-wider py-3 pr-4">Domain</th>
							<th class="text-left text-text-muted text-xs uppercase tracking-wider py-3 pr-4">Workflow</th>
							<th class="text-left text-text-muted text-xs uppercase tracking-wider py-3 pr-4">Status</th>
							<th class="text-left text-text-muted text-xs uppercase tracking-wider py-3 pr-4">Date</th>
							<th class="text-right text-text-muted text-xs uppercase tracking-wider py-3"></th>
						</tr>
					</thead>
					<tbody>
						{#each recentScans as scan}
							<tr class="border-b border-[rgba(255,255,255,0.04)] hover:bg-[rgba(255,255,255,0.02)]">
								<td class="py-3 pr-4 text-white">{scan.domain_name}</td>
								<td class="py-3 pr-4 text-text-secondary">{scan.workflow_name}</td>
								<td class="py-3 pr-4">
									<StatusBadge status={scan.status} />
								</td>
								<td class="py-3 pr-4 text-text-secondary">{formatDate(scan.created_at)}</td>
								<td class="py-3 text-right">
									<a href="/scans/{scan.id}" class="text-brand text-xs hover:underline">View</a>
								</td>
							</tr>
						{/each}
					</tbody>
				</table>
			</div>
		{/if}
	</div>

	<!-- Quick Actions -->
	<div class="bg-surface rounded-xl border border-[rgba(255,255,255,0.08)] p-6">
		<h2 class="text-lg font-semibold text-white mb-4">Quick Actions</h2>
		<div class="flex flex-wrap gap-3">
			<a href="/scans/new">
				<Button variant="primary">New Scan</Button>
			</a>
			<a href="/domains">
				<Button variant="secondary">Add Domain</Button>
			</a>
			<a href="/billing">
				<Button variant="secondary">View Billing</Button>
			</a>
		</div>
	</div>
</div>
