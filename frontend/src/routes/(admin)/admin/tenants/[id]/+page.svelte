<script lang="ts">
	import { page } from '$app/state';
	import { adminApi } from '$lib/api/client';
	import { Button, Alert } from '$lib/components/ui';
	import { Modal, StatusBadge, SkeletonLoader } from '$lib/components/shared';
	import type { ApiError } from '$lib/types/auth';

	let tenant = $state<any | null>(null);
	let auditLog = $state<any[]>([]);
	let isLoading = $state(true);
	let error = $state<string | null>(null);
	let actionLoading = $state<string | null>(null);
	let actionSuccess = $state<string | null>(null);

	// Modal states
	let showSuspendModal = $state(false);
	let showImpersonateModal = $state(false);
	let suspendReason = $state('');

	async function loadTenant() {
		isLoading = true;
		error = null;
		try {
			tenant = await adminApi.getTenant(page.params.id);
			try {
				// Audit log endpoint may not exist yet — gracefully handle
			auditLog = [];
			} catch {
				auditLog = [];
			}
		} catch (err) {
			const apiErr = err as ApiError;
			error = apiErr.message || 'Failed to load tenant.';
		} finally {
			isLoading = false;
		}
	}

	$effect(() => {
		loadTenant();
	});

	async function handleSuspend() {
		if (!tenant || !suspendReason.trim()) return;
		actionLoading = 'suspend';
		actionSuccess = null;
		error = null;
		try {
			await adminApi.suspendTenant(tenant.id, { reason: suspendReason.trim() });
			actionSuccess = 'Tenant has been suspended.';
			showSuspendModal = false;
			suspendReason = '';
			await loadTenant();
		} catch (err) {
			const apiErr = err as ApiError;
			error = apiErr.message || 'Failed to suspend tenant.';
		} finally {
			actionLoading = null;
		}
	}

	async function handleReactivate() {
		if (!tenant) return;
		actionLoading = 'reactivate';
		actionSuccess = null;
		error = null;
		try {
			await adminApi.reactivateTenant(tenant.id);
			actionSuccess = 'Tenant has been reactivated.';
			await loadTenant();
		} catch (err) {
			const apiErr = err as ApiError;
			error = apiErr.message || 'Failed to reactivate tenant.';
		} finally {
			actionLoading = null;
		}
	}

	async function handleImpersonate() {
		if (!tenant) return;
		actionLoading = 'impersonate';
		error = null;
		try {
			await adminApi.impersonateTenant(tenant.id);
			showImpersonateModal = false;
			window.location.href = '/dashboard';
		} catch (err) {
			const apiErr = err as ApiError;
			error = apiErr.message || 'Failed to start impersonation.';
		} finally {
			actionLoading = null;
		}
	}

	async function handleApproveDeletion() {
		if (!tenant) return;
		actionLoading = 'approve-deletion';
		actionSuccess = null;
		error = null;
		try {
			await adminApi.approveDeletion(tenant.id);
			actionSuccess = 'Tenant deletion approved.';
			await loadTenant();
		} catch (err) {
			const apiErr = err as ApiError;
			error = apiErr.message || 'Failed to approve deletion.';
		} finally {
			actionLoading = null;
		}
	}

	async function handleDenyDeletion() {
		if (!tenant) return;
		actionLoading = 'deny-deletion';
		actionSuccess = null;
		error = null;
		try {
			await adminApi.denyDeletion(tenant.id);
			actionSuccess = 'Tenant deletion denied.';
			await loadTenant();
		} catch (err) {
			const apiErr = err as ApiError;
			error = apiErr.message || 'Failed to deny deletion.';
		} finally {
			actionLoading = null;
		}
	}
</script>

<svelte:head>
	<title>{tenant?.name ?? 'Tenant Detail'} — Reconova Admin</title>
</svelte:head>

<div class="max-w-4xl">
	<!-- Back Link -->
	<a href="/admin/tenants" class="text-brand text-sm font-medium hover:text-brand-dark transition-colors inline-flex items-center gap-1 mb-6">
		<svg class="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
			<path stroke-linecap="round" stroke-linejoin="round" d="M10.5 19.5 3 12m0 0 7.5-7.5M3 12h18" />
		</svg>
		Back to Tenants
	</a>

	{#if error}
		<Alert variant="error">{error}</Alert>
		<div class="mt-4"></div>
	{/if}

	{#if actionSuccess}
		<Alert variant="success">{actionSuccess}</Alert>
		<div class="mt-4"></div>
	{/if}

	{#if isLoading}
		<div class="bg-surface/60 backdrop-blur-sm rounded-xl border border-[rgba(255,255,255,0.08)] p-6">
			<SkeletonLoader lines={8} />
		</div>
	{:else if tenant}
		<!-- Tenant Info Card -->
		<div class="bg-surface/60 backdrop-blur-sm rounded-xl border border-[rgba(255,255,255,0.08)] p-6 shadow-lg shadow-black/10">
			<div class="flex items-start justify-between mb-6">
				<h1 class="text-xl font-bold text-white">{tenant.name}</h1>
				<StatusBadge status={tenant.status} size="md" />
			</div>

			<div class="grid grid-cols-2 gap-4">
				<div>
					<p class="text-xs text-text-muted uppercase tracking-wider mb-1">Slug</p>
					<p class="text-sm text-text font-mono">{tenant.slug}</p>
				</div>
				<div>
					<p class="text-xs text-text-muted uppercase tracking-wider mb-1">Plan</p>
					<p class="text-sm text-text">{tenant.plan ?? '—'}</p>
				</div>
				<div>
					<p class="text-xs text-text-muted uppercase tracking-wider mb-1">Created</p>
					<p class="text-sm text-text">{new Date(tenant.createdAt).toLocaleString()}</p>
				</div>
				<div>
					<p class="text-xs text-text-muted uppercase tracking-wider mb-1">Last Active</p>
					<p class="text-sm text-text">{tenant.lastActiveAt ? new Date(tenant.lastActiveAt).toLocaleString() : '—'}</p>
				</div>
				<div>
					<p class="text-xs text-text-muted uppercase tracking-wider mb-1">Owner Email</p>
					<p class="text-sm text-text">{tenant.ownerEmail ?? '—'}</p>
				</div>
				<div>
					<p class="text-xs text-text-muted uppercase tracking-wider mb-1">Domains</p>
					<p class="text-sm text-text">{tenant.domains?.length ? tenant.domains.join(', ') : '—'}</p>
				</div>
				<div>
					<p class="text-xs text-text-muted uppercase tracking-wider mb-1">Credits</p>
					<p class="text-sm text-text">{tenant.credits ?? '—'}</p>
				</div>
			</div>
		</div>

		<!-- Actions -->
		<div class="mt-6 bg-surface/60 backdrop-blur-sm rounded-xl border border-[rgba(255,255,255,0.08)] p-6 shadow-lg shadow-black/10">
			<h2 class="text-lg font-semibold text-white mb-4">Actions</h2>
			<div class="flex flex-wrap gap-3">
				{#if tenant.status === 'ACTIVE'}
					<Button
						variant="destructive"
						onclick={() => showSuspendModal = true}
					>
						Suspend
					</Button>
					<Button
						variant="secondary"
						onclick={() => showImpersonateModal = true}
					>
						Impersonate
					</Button>
				{/if}

				{#if tenant.status === 'SUSPENDED'}
					<Button
						variant="secondary"
						loading={actionLoading === 'reactivate'}
						onclick={handleReactivate}
					>
						Reactivate
					</Button>
				{/if}

				{#if tenant.pendingDeletion}
					<Button
						variant="destructive"
						loading={actionLoading === 'approve-deletion'}
						onclick={handleApproveDeletion}
					>
						Approve Deletion
					</Button>
					<Button
						variant="secondary"
						loading={actionLoading === 'deny-deletion'}
						onclick={handleDenyDeletion}
					>
						Deny Deletion
					</Button>
				{/if}

				{#if tenant.status !== 'ACTIVE' && tenant.status !== 'SUSPENDED' && !tenant.pendingDeletion}
					<p class="text-sm text-text-muted">No actions available for this tenant status.</p>
				{/if}
			</div>
		</div>

		<!-- Audit Log -->
		<div class="mt-6 bg-surface/60 backdrop-blur-sm rounded-xl border border-[rgba(255,255,255,0.08)] p-6 shadow-lg shadow-black/10">
			<h2 class="text-lg font-semibold text-white mb-4">Audit Log</h2>
			{#if auditLog.length === 0}
				<p class="text-sm text-text-muted py-4 text-center">No audit log entries found.</p>
			{:else}
				<div class="overflow-x-auto">
					<table class="w-full">
						<thead>
							<tr class="border-b border-[rgba(255,255,255,0.08)]">
								<th class="text-left px-4 py-3 text-xs font-semibold uppercase tracking-wider text-text-muted">Date</th>
								<th class="text-left px-4 py-3 text-xs font-semibold uppercase tracking-wider text-text-muted">Actor</th>
								<th class="text-left px-4 py-3 text-xs font-semibold uppercase tracking-wider text-text-muted">Action</th>
								<th class="text-left px-4 py-3 text-xs font-semibold uppercase tracking-wider text-text-muted">Details</th>
							</tr>
						</thead>
						<tbody>
							{#each auditLog as entry}
								<tr class="border-b border-[rgba(255,255,255,0.04)] hover:bg-[rgba(255,255,255,0.02)] transition-colors">
									<td class="px-4 py-3 text-sm text-text-secondary whitespace-nowrap">{new Date(entry.timestamp).toLocaleString()}</td>
									<td class="px-4 py-3 text-sm text-text">{entry.actor ?? '—'}</td>
									<td class="px-4 py-3 text-sm text-text font-medium">{entry.action}</td>
									<td class="px-4 py-3 text-sm text-text-secondary">{entry.details ?? '—'}</td>
								</tr>
							{/each}
						</tbody>
					</table>
				</div>
			{/if}
		</div>
	{/if}
</div>

<!-- Suspend Modal -->
<Modal title="Suspend Tenant" open={showSuspendModal} onclose={() => showSuspendModal = false}>
	<div class="space-y-4">
		<p class="text-sm text-text-secondary">
			Provide a reason for suspending <span class="font-semibold text-white">{tenant?.name}</span>. The tenant owner will be notified.
		</p>
		<div>
			<label for="suspend-reason" class="block text-sm font-medium text-text-secondary mb-1">Reason</label>
			<textarea
				id="suspend-reason"
				bind:value={suspendReason}
				rows="3"
				placeholder="Enter suspension reason..."
				class="w-full bg-surface border border-[rgba(255,255,255,0.08)] rounded-lg px-3 py-2 text-sm text-text placeholder:text-text-muted focus:outline-none focus:border-brand/50 transition-colors resize-none"
			></textarea>
		</div>
		<div class="flex justify-end gap-3 pt-2">
			<Button variant="ghost" onclick={() => showSuspendModal = false}>Cancel</Button>
			<Button
				variant="destructive"
				loading={actionLoading === 'suspend'}
				disabled={!suspendReason.trim()}
				onclick={handleSuspend}
			>
				Suspend Tenant
			</Button>
		</div>
	</div>
</Modal>

<!-- Impersonate Modal -->
<Modal title="Impersonate Tenant" open={showImpersonateModal} onclose={() => showImpersonateModal = false}>
	<div class="space-y-4">
		<p class="text-sm text-text-secondary">
			You are about to impersonate <span class="font-semibold text-white">{tenant?.name}</span>. You will be redirected to the tenant dashboard and can act on their behalf. Your actions will be logged in the audit trail.
		</p>
		<div class="flex justify-end gap-3 pt-2">
			<Button variant="ghost" onclick={() => showImpersonateModal = false}>Cancel</Button>
			<Button
				variant="primary"
				loading={actionLoading === 'impersonate'}
				onclick={handleImpersonate}
			>
				Start Impersonation
			</Button>
		</div>
	</div>
</Modal>
