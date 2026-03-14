<script lang="ts">
	import { adminApi } from '$lib/api/client';
	import { Button, Alert, TextInput } from '$lib/components/ui';
	import { Pagination, StatusBadge, EmptyState, SkeletonLoader } from '$lib/components/shared';
	import type { ApiError } from '$lib/types/auth';

	let tenants = $state<any[]>([]);
	let totalCount = $state(0);
	let currentPage = $state(1);
	let totalPages = $state(1);
	let pageSize = 10;
	let isLoading = $state(true);
	let error = $state<string | null>(null);
	let search = $state('');
	let statusFilter = $state('');

	const statusTabs = ['', 'ACTIVE', 'SUSPENDED', 'PROVISIONING', 'DEACTIVATED'] as const;
	const statusLabels: Record<string, string> = {
		'': 'All',
		ACTIVE: 'Active',
		SUSPENDED: 'Suspended',
		PROVISIONING: 'Provisioning',
		DEACTIVATED: 'Deactivated',
	};

	async function loadTenants() {
		isLoading = true;
		error = null;
		try {
			const res = await adminApi.getTenants({
				page: currentPage,
				pageSize,
				search: search || undefined,
				status: statusFilter || undefined,
			}) as { tenants: any[]; totalCount: number; totalPages: number };
			tenants = res.tenants;
			totalCount = res.totalCount;
			totalPages = res.totalPages;
		} catch (err) {
			const apiErr = err as ApiError;
			error = apiErr.message || 'Failed to load tenants.';
		} finally {
			isLoading = false;
		}
	}

	$effect(() => {
		loadTenants();
	});

	function handleSearch() {
		currentPage = 1;
		loadTenants();
	}

	function selectStatus(status: string) {
		statusFilter = status;
		currentPage = 1;
		loadTenants();
	}

	function handlePageChange(page: number) {
		currentPage = page;
		loadTenants();
	}
</script>

<svelte:head>
	<title>Tenants — Reconova Admin</title>
</svelte:head>

<div>
	<div class="flex items-center justify-between mb-6">
		<div>
			<h1 class="text-2xl font-bold text-white">Tenants</h1>
			<p class="text-text-secondary text-sm mt-1">{totalCount} total tenants</p>
		</div>
	</div>

	<!-- Search -->
	<div class="flex gap-3 mb-4">
		<div class="flex-1">
			<input
				type="text"
				bind:value={search}
				placeholder="Search by name or slug..."
				class="w-full bg-surface border border-[rgba(255,255,255,0.08)] rounded-lg px-3 py-2 text-sm text-text placeholder:text-text-muted focus:outline-none focus:border-brand/50 transition-colors"
				onkeydown={(e) => { if (e.key === 'Enter') handleSearch(); }}
			/>
		</div>
		<Button variant="secondary" onclick={handleSearch}>Search</Button>
	</div>

	<!-- Status Filter Tabs -->
	<div class="flex gap-1 mb-6 bg-surface/50 border border-[rgba(255,255,255,0.06)] rounded-lg p-1 w-fit">
		{#each statusTabs as tab}
			<button
				class="px-3 py-1.5 text-sm rounded-md font-medium transition-all {statusFilter === tab
					? 'bg-brand/20 text-brand shadow-sm'
					: 'text-text-muted hover:text-text hover:bg-[rgba(255,255,255,0.04)]'}"
				onclick={() => selectStatus(tab)}
			>
				{statusLabels[tab]}
			</button>
		{/each}
	</div>

	{#if error}
		<Alert variant="error">{error}</Alert>
		<div class="mt-4"></div>
	{/if}

	{#if isLoading}
		<div class="bg-surface/60 backdrop-blur-sm rounded-xl border border-[rgba(255,255,255,0.08)] p-6">
			<SkeletonLoader lines={6} />
		</div>
	{:else if tenants.length === 0}
		<div class="bg-surface/60 backdrop-blur-sm rounded-xl border border-[rgba(255,255,255,0.08)]">
			<EmptyState
				title="No tenants found"
				description={search || statusFilter ? 'Try adjusting your search or filter criteria.' : 'No tenants have been created yet.'}
			/>
		</div>
	{:else}
		<!-- Table -->
		<div class="bg-surface/60 backdrop-blur-sm rounded-xl border border-[rgba(255,255,255,0.08)] overflow-hidden shadow-lg shadow-black/10">
			<table class="w-full">
				<thead>
					<tr class="border-b border-[rgba(255,255,255,0.08)]">
						<th class="text-left px-4 py-3 text-xs font-semibold uppercase tracking-wider text-text-muted">Tenant Name</th>
						<th class="text-left px-4 py-3 text-xs font-semibold uppercase tracking-wider text-text-muted">Slug</th>
						<th class="text-left px-4 py-3 text-xs font-semibold uppercase tracking-wider text-text-muted">Status</th>
						<th class="text-left px-4 py-3 text-xs font-semibold uppercase tracking-wider text-text-muted">Plan</th>
						<th class="text-left px-4 py-3 text-xs font-semibold uppercase tracking-wider text-text-muted">Created</th>
						<th class="text-right px-4 py-3 text-xs font-semibold uppercase tracking-wider text-text-muted"></th>
					</tr>
				</thead>
				<tbody>
					{#each tenants as tenant}
						<tr class="border-b border-[rgba(255,255,255,0.04)] hover:bg-[rgba(255,255,255,0.02)] transition-colors">
							<td class="px-4 py-3 text-sm text-text font-medium">{tenant.name}</td>
							<td class="px-4 py-3 text-sm text-text-secondary font-mono">{tenant.slug}</td>
							<td class="px-4 py-3">
								<StatusBadge status={tenant.status} />
							</td>
							<td class="px-4 py-3 text-sm text-text-secondary">{tenant.plan ?? '—'}</td>
							<td class="px-4 py-3 text-sm text-text-secondary">{new Date(tenant.createdAt).toLocaleDateString()}</td>
							<td class="px-4 py-3 text-right">
								<a href="/admin/tenants/{tenant.id}" class="text-brand text-sm font-medium hover:text-brand-dark transition-colors">View</a>
							</td>
						</tr>
					{/each}
				</tbody>
			</table>
		</div>

		<Pagination {currentPage} {totalPages} onPageChange={handlePageChange} />
	{/if}
</div>
