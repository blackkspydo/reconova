<script lang="ts">
	import { domainApi } from '$lib/api/client';
	import type { Domain } from '$lib/types/domains';
	import type { ApiError } from '$lib/types/auth';
	import { Button, Alert, TextInput } from '$lib/components/ui';
	import { Modal, EmptyState, SkeletonLoader } from '$lib/components/shared';

	let domains = $state<Domain[]>([]);
	let isLoading = $state(true);
	let error = $state<string | null>(null);

	// Add domain form
	let newDomain = $state('');
	let addError = $state<string | null>(null);
	let isAdding = $state(false);

	// Delete
	let deleteTarget = $state<Domain | null>(null);
	let isDeleting = $state(false);
	let deleteError = $state<string | null>(null);

	let domainCount = $derived(domains.length);

	async function loadDomains() {
		isLoading = true;
		error = null;
		try {
			domains = (await domainApi.list()) as Domain[];
		} catch (err) {
			const apiErr = err as ApiError;
			error = apiErr.message || 'Failed to load domains.';
		} finally {
			isLoading = false;
		}
	}

	$effect(() => {
		loadDomains();
	});

	async function addDomain() {
		const trimmed = newDomain.trim();
		if (!trimmed) return;

		isAdding = true;
		addError = null;
		try {
			await domainApi.create({ domain: trimmed });
			newDomain = '';
			await loadDomains();
		} catch (err) {
			const apiErr = err as ApiError;
			addError = apiErr.message || 'Failed to add domain.';
		} finally {
			isAdding = false;
		}
	}

	function confirmDelete(domain: Domain) {
		deleteTarget = domain;
		deleteError = null;
	}

	async function deleteDomain() {
		if (!deleteTarget) return;
		isDeleting = true;
		deleteError = null;
		try {
			await domainApi.delete(deleteTarget.id);
			deleteTarget = null;
			await loadDomains();
		} catch (err) {
			const apiErr = err as ApiError;
			deleteError = apiErr.message || 'Failed to delete domain.';
		} finally {
			isDeleting = false;
		}
	}

	function formatDate(dateStr: string | null): string {
		if (!dateStr) return '—';
		return new Date(dateStr).toLocaleDateString();
	}
</script>

<svelte:head>
	<title>Domains — Reconova</title>
</svelte:head>

<div>
	<!-- Header -->
	<div class="flex items-center justify-between mb-6">
		<div class="flex items-center gap-3">
			<h1 class="text-2xl font-bold text-white">Domains</h1>
			{#if !isLoading}
				<span class="text-xs font-medium px-2.5 py-1 rounded-full bg-brand/10 text-brand">
					{domainCount} / 20
				</span>
			{/if}
		</div>
	</div>

	<!-- Add Domain Form -->
	<div class="bg-surface rounded-xl border border-[rgba(255,255,255,0.08)] p-4 mb-6">
		<form class="flex items-end gap-3" onsubmit={(e) => { e.preventDefault(); addDomain(); }}>
			<div class="flex-1">
				<TextInput
					id="new-domain"
					label="Add a domain"
					bind:value={newDomain}
					placeholder="example.com"
					error={addError}
					disabled={isAdding}
				/>
			</div>
			<div class="pb-0.5">
				<Button variant="primary" type="submit" loading={isAdding} disabled={!newDomain.trim()}>
					Add Domain
				</Button>
			</div>
		</form>
	</div>

	{#if error}
		<div class="mb-4">
			<Alert variant="error">{error}</Alert>
		</div>
	{/if}

	{#if isLoading}
		<div class="bg-surface rounded-xl border border-[rgba(255,255,255,0.08)] p-6">
			<SkeletonLoader lines={5} />
		</div>
	{:else if domains.length === 0}
		<div class="bg-surface rounded-xl border border-[rgba(255,255,255,0.08)]">
			<EmptyState
				title="No domains yet"
				description="Add your first domain above to start scanning."
			/>
		</div>
	{:else}
		<!-- Domain Table -->
		<div class="bg-surface rounded-xl border border-[rgba(255,255,255,0.08)] overflow-hidden">
			<table class="w-full">
				<thead>
					<tr class="border-b border-[rgba(255,255,255,0.08)]">
						<th class="text-left px-4 py-3 text-xs font-semibold uppercase tracking-wider text-text-muted">Domain</th>
						<th class="text-left px-4 py-3 text-xs font-semibold uppercase tracking-wider text-text-muted">Status</th>
						<th class="text-left px-4 py-3 text-xs font-semibold uppercase tracking-wider text-text-muted">Added</th>
						<th class="text-left px-4 py-3 text-xs font-semibold uppercase tracking-wider text-text-muted">Last Scanned</th>
						<th class="text-right px-4 py-3 text-xs font-semibold uppercase tracking-wider text-text-muted">Actions</th>
					</tr>
				</thead>
				<tbody>
					{#each domains as domain}
						<tr class="border-b border-[rgba(255,255,255,0.04)] hover:bg-[rgba(255,255,255,0.02)] transition-colors">
							<td class="px-4 py-3 text-sm text-text font-medium">{domain.domain}</td>
							<td class="px-4 py-3">
								{#if domain.status === 'ACTIVE'}
									<span class="text-xs font-medium px-2 py-0.5 rounded-full text-success bg-success/10">ACTIVE</span>
								{:else if domain.status === 'VERIFIED'}
									<span class="text-xs font-medium px-2 py-0.5 rounded-full text-success bg-success/10">VERIFIED</span>
								{:else}
									<span class="text-xs font-medium px-2 py-0.5 rounded-full text-warning bg-warning/10">PENDING</span>
								{/if}
							</td>
							<td class="px-4 py-3 text-sm text-text-secondary">{formatDate(domain.created_at)}</td>
							<td class="px-4 py-3 text-sm text-text-secondary">{formatDate(domain.last_scanned_at)}</td>
							<td class="px-4 py-3 text-right">
								<div class="flex items-center justify-end gap-3">
									<a href="/domains/{domain.id}" class="text-brand text-sm font-medium hover:text-brand-dark transition-colors">View</a>
									<button
										class="text-danger text-sm font-medium hover:text-red-400 transition-colors"
										onclick={() => confirmDelete(domain)}
									>
										Delete
									</button>
								</div>
							</td>
						</tr>
					{/each}
				</tbody>
			</table>
		</div>
	{/if}
</div>

<!-- Delete Confirmation Modal -->
<Modal title="Delete Domain" open={deleteTarget !== null} onclose={() => { deleteTarget = null; }}>
	<div>
		<p class="text-text-secondary text-sm mb-1">
			Are you sure you want to delete this domain?
		</p>
		<p class="text-white text-sm font-medium mb-4">{deleteTarget?.domain}</p>
		<p class="text-text-muted text-xs mb-6">
			This will permanently remove the domain and all associated data including subdomains, ports, technologies, and scan history.
		</p>

		{#if deleteError}
			<div class="mb-4">
				<Alert variant="error">{deleteError}</Alert>
			</div>
		{/if}

		<div class="flex justify-end gap-3">
			<Button variant="ghost" onclick={() => { deleteTarget = null; }} disabled={isDeleting}>Cancel</Button>
			<Button variant="destructive" onclick={deleteDomain} loading={isDeleting}>Delete Domain</Button>
		</div>
	</div>
</Modal>
