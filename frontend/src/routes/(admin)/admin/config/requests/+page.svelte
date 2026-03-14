<script lang="ts">
	import { adminConfigApi } from '$lib/api/client';
	import { Button, Alert } from '$lib/components/ui';
	import { Modal, Pagination, StatusBadge, SkeletonLoader } from '$lib/components/shared';
	import { getToastStore } from '$lib/stores/toast';
	import type { ConfigChangeRequest } from '$lib/types/admin';
	import type { ApiError } from '$lib/types/auth';

	const toast = getToastStore();

	let requests = $state<ConfigChangeRequest[]>([]);
	let isLoading = $state(true);
	let error = $state<string | null>(null);
	let currentPage = $state(1);
	let totalPages = $state(1);
	let pageSize = 20;
	let activeTab = $state<'pending' | 'recent'>('pending');

	// Action modal
	let actionTarget = $state<ConfigChangeRequest | null>(null);
	let actionType = $state<'approve' | 'reject' | null>(null);
	let isProcessing = $state(false);

	async function loadRequests() {
		isLoading = true;
		error = null;
		try {
			const status = activeTab === 'pending' ? 'PENDING' : undefined;
			const res = await adminConfigApi.getChangeRequests({
				page: currentPage,
				pageSize,
				status,
			}) as { items: ConfigChangeRequest[]; totalPages: number };
			requests = res.items;
			totalPages = res.totalPages;
		} catch (err) {
			const apiErr = err as ApiError;
			error = apiErr.message || 'Failed to load change requests.';
		} finally {
			isLoading = false;
		}
	}

	$effect(() => {
		loadRequests();
	});

	function switchTab(tab: 'pending' | 'recent') {
		activeTab = tab;
		currentPage = 1;
		loadRequests();
	}

	function handlePageChange(page: number) {
		currentPage = page;
		loadRequests();
	}

	function openActionModal(request: ConfigChangeRequest, type: 'approve' | 'reject') {
		actionTarget = request;
		actionType = type;
	}

	function closeActionModal() {
		actionTarget = null;
		actionType = null;
	}

	async function confirmAction() {
		if (!actionTarget || !actionType) return;
		isProcessing = true;
		try {
			if (actionType === 'approve') {
				await adminConfigApi.approveRequest(actionTarget.id);
				toast.success(`Request for "${actionTarget.key}" approved.`);
			} else {
				await adminConfigApi.rejectRequest(actionTarget.id);
				toast.success(`Request for "${actionTarget.key}" rejected.`);
			}
			closeActionModal();
			await loadRequests();
		} catch (err) {
			const apiErr = err as ApiError;
			toast.error(apiErr.message || `Failed to ${actionType} request.`);
		} finally {
			isProcessing = false;
		}
	}

	function statusColor(status: string): string {
		switch (status) {
			case 'PENDING': return 'text-warning bg-warning/10';
			case 'APPROVED': return 'text-success bg-success/10';
			case 'REJECTED': return 'text-danger bg-danger/10';
			default: return 'text-text-muted bg-[rgba(255,255,255,0.05)]';
		}
	}

	function truncate(value: string, maxLen = 40): string {
		return value.length > maxLen ? value.slice(0, maxLen) + '...' : value;
	}

	let filteredRequests = $derived<ConfigChangeRequest[]>(
		activeTab === 'recent'
			? requests.filter(r => r.status !== 'PENDING')
			: requests
	);
</script>

<svelte:head>
	<title>Change Requests — Reconova Admin</title>
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
				<h1 class="text-2xl font-bold text-white">Change Requests</h1>
			</div>
			<p class="text-text-secondary text-sm mt-1">Review and manage configuration change requests</p>
		</div>
	</div>

	<!-- Tabs -->
	<div class="flex gap-1 mb-6 bg-surface/60 backdrop-blur-sm border border-[rgba(255,255,255,0.08)] rounded-lg p-1 w-fit">
		<button
			class="px-4 py-2 rounded-md text-sm font-medium transition-colors {activeTab === 'pending' ? 'bg-brand/10 text-brand' : 'text-text-secondary hover:text-white'}"
			onclick={() => switchTab('pending')}
		>
			Pending
		</button>
		<button
			class="px-4 py-2 rounded-md text-sm font-medium transition-colors {activeTab === 'recent' ? 'bg-brand/10 text-brand' : 'text-text-secondary hover:text-white'}"
			onclick={() => switchTab('recent')}
		>
			Recent
		</button>
	</div>

	{#if error}
		<div class="mb-4">
			<Alert variant="error">{error}</Alert>
		</div>
	{/if}

	{#if isLoading}
		<div class="bg-surface/60 backdrop-blur-sm rounded-xl border border-[rgba(255,255,255,0.08)] p-6">
			<SkeletonLoader lines={8} />
		</div>
	{:else}
		<div class="bg-surface rounded-xl border border-[rgba(255,255,255,0.08)] overflow-hidden">
			<table class="w-full">
				<thead>
					<tr class="border-b border-[rgba(255,255,255,0.08)]">
						<th class="text-left px-4 py-3 text-xs font-semibold uppercase tracking-wider text-text-muted">Date</th>
						<th class="text-left px-4 py-3 text-xs font-semibold uppercase tracking-wider text-text-muted">Key</th>
						<th class="text-left px-4 py-3 text-xs font-semibold uppercase tracking-wider text-text-muted">Proposed Value</th>
						<th class="text-left px-4 py-3 text-xs font-semibold uppercase tracking-wider text-text-muted">Reason</th>
						<th class="text-left px-4 py-3 text-xs font-semibold uppercase tracking-wider text-text-muted">Requested By</th>
						<th class="text-left px-4 py-3 text-xs font-semibold uppercase tracking-wider text-text-muted">Status</th>
						<th class="text-right px-4 py-3 text-xs font-semibold uppercase tracking-wider text-text-muted"></th>
					</tr>
				</thead>
				<tbody>
					{#each filteredRequests as request}
						<tr class="border-b border-[rgba(255,255,255,0.04)] hover:bg-[rgba(255,255,255,0.02)] transition-colors">
							<td class="px-4 py-3 text-sm text-text-secondary whitespace-nowrap">
								{new Date(request.created_at).toLocaleString()}
							</td>
							<td class="px-4 py-3">
								<code class="text-sm font-mono text-brand">{request.key}</code>
							</td>
							<td class="px-4 py-3 text-sm text-text font-mono" title={request.proposed_value}>
								{truncate(request.proposed_value)}
							</td>
							<td class="px-4 py-3 text-sm text-text-muted">{request.reason || '-'}</td>
							<td class="px-4 py-3 text-sm text-text-secondary">{request.requested_by}</td>
							<td class="px-4 py-3">
								<span class="text-xs font-medium px-2 py-0.5 rounded-full {statusColor(request.status)}">
									{request.status}
								</span>
							</td>
							<td class="px-4 py-3 text-right">
								{#if request.status === 'PENDING'}
									<div class="flex gap-2 justify-end">
										<button
											class="text-sm text-success hover:text-success/80 font-medium transition-colors"
											onclick={() => openActionModal(request, 'approve')}
										>
											Approve
										</button>
										<button
											class="text-sm text-danger hover:text-danger/80 font-medium transition-colors"
											onclick={() => openActionModal(request, 'reject')}
										>
											Reject
										</button>
									</div>
								{:else if request.reviewed_by}
									<span class="text-xs text-text-muted">by {request.reviewed_by}</span>
								{/if}
							</td>
						</tr>
					{:else}
						<tr>
							<td colspan="7" class="px-4 py-8 text-center text-text-muted text-sm">
								{activeTab === 'pending' ? 'No pending requests' : 'No recent requests'}
							</td>
						</tr>
					{/each}
				</tbody>
			</table>
		</div>

		<Pagination {currentPage} {totalPages} onPageChange={handlePageChange} />
	{/if}
</div>

<!-- Action Confirmation Modal -->
<Modal
	title={actionType === 'approve' ? 'Confirm Approval' : 'Confirm Rejection'}
	open={actionTarget !== null}
	onclose={closeActionModal}
>
	{#if actionTarget}
		<div class="space-y-4">
			<p class="text-sm text-text-secondary">
				Are you sure you want to {actionType} this change request?
			</p>
			<div class="bg-[rgba(255,255,255,0.03)] rounded-lg p-3 space-y-2">
				<div class="flex justify-between text-sm">
					<span class="text-text-muted">Key:</span>
					<code class="font-mono text-brand">{actionTarget.key}</code>
				</div>
				<div class="flex justify-between text-sm">
					<span class="text-text-muted">Proposed value:</span>
					<code class="font-mono text-text">{truncate(actionTarget.proposed_value, 30)}</code>
				</div>
				<div class="flex justify-between text-sm">
					<span class="text-text-muted">Reason:</span>
					<span class="text-text">{actionTarget.reason || '-'}</span>
				</div>
			</div>
			<div class="flex justify-end gap-2">
				<Button variant="secondary" onclick={closeActionModal}>Cancel</Button>
				<Button
					variant="primary"
					loading={isProcessing}
					onclick={confirmAction}
				>
					{actionType === 'approve' ? 'Approve' : 'Reject'}
				</Button>
			</div>
		</div>
	{/if}
</Modal>
