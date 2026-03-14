<script lang="ts">
	import { adminApi } from '$lib/api/client';
	import { Button, Alert } from '$lib/components/ui';
	import type { AdminUserDetail, PaginatedUsers, ApiError } from '$lib/types/auth';

	let users = $state<AdminUserDetail[]>([]);
	let totalCount = $state(0);
	let currentPage = $state(1);
	let totalPages = $state(1);
	let pageSize = 10;
	let isLoading = $state(true);
	let error = $state<string | null>(null);
	let search = $state('');
	let statusFilter = $state('');

	async function loadUsers() {
		isLoading = true;
		error = null;
		try {
			const res = await adminApi.getUsers({
				page: currentPage,
				pageSize,
				search: search || undefined,
				status: statusFilter || undefined,
			}) as PaginatedUsers;
			users = res.users;
			totalCount = res.totalCount;
			totalPages = res.totalPages;
		} catch (err) {
			const apiErr = err as ApiError;
			error = apiErr.message || 'Failed to load users.';
		} finally {
			isLoading = false;
		}
	}

	$effect(() => {
		loadUsers();
	});

	function handleSearch() {
		currentPage = 1;
		loadUsers();
	}

	function goToPage(page: number) {
		currentPage = page;
		loadUsers();
	}

	function statusColor(status: string): string {
		switch (status) {
			case 'ACTIVE': return 'text-success bg-success/10';
			case 'LOCKED': return 'text-danger bg-danger/10';
			case 'DEACTIVATED': return 'text-text-muted bg-[rgba(255,255,255,0.05)]';
			case 'PASSWORD_EXPIRED': return 'text-warning bg-warning/10';
			case 'PENDING_2FA': return 'text-info bg-info/10';
			default: return 'text-text-muted bg-[rgba(255,255,255,0.05)]';
		}
	}
</script>

<svelte:head>
	<title>Users — Reconova Admin</title>
</svelte:head>

<div>
	<div class="flex items-center justify-between mb-6">
		<div>
			<h1 class="text-2xl font-bold text-white">Users</h1>
			<p class="text-text-secondary text-sm mt-1">{totalCount} total users</p>
		</div>
	</div>

	<!-- Filters -->
	<div class="flex gap-3 mb-6">
		<div class="flex-1">
			<input
				type="text"
				bind:value={search}
				placeholder="Search by email..."
				class="w-full bg-surface border border-[rgba(255,255,255,0.08)] rounded-lg px-3 py-2 text-sm text-text placeholder:text-text-muted focus:outline-none focus:border-brand/50 transition-colors"
				onkeydown={(e) => { if (e.key === 'Enter') handleSearch(); }}
			/>
		</div>
		<select
			bind:value={statusFilter}
			class="bg-surface border border-[rgba(255,255,255,0.08)] rounded-lg px-3 py-2 text-sm text-text focus:outline-none focus:border-brand/50 transition-colors"
			onchange={handleSearch}
		>
			<option value="">All statuses</option>
			<option value="ACTIVE">Active</option>
			<option value="LOCKED">Locked</option>
			<option value="DEACTIVATED">Deactivated</option>
			<option value="PASSWORD_EXPIRED">Password Expired</option>
			<option value="PENDING_2FA">Pending 2FA</option>
		</select>
		<Button variant="secondary" onclick={handleSearch}>Search</Button>
	</div>

	{#if error}
		<Alert variant="error">{error}</Alert>
	{/if}

	{#if isLoading}
		<div class="flex justify-center py-12">
			<svg class="w-8 h-8 animate-spin text-brand" fill="none" viewBox="0 0 24 24">
				<circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
				<path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
			</svg>
		</div>
	{:else}
		<!-- Table -->
		<div class="bg-surface rounded-xl border border-[rgba(255,255,255,0.08)] overflow-hidden">
			<table class="w-full">
				<thead>
					<tr class="border-b border-[rgba(255,255,255,0.08)]">
						<th class="text-left px-4 py-3 text-xs font-semibold uppercase tracking-wider text-text-muted">Email</th>
						<th class="text-left px-4 py-3 text-xs font-semibold uppercase tracking-wider text-text-muted">Role</th>
						<th class="text-left px-4 py-3 text-xs font-semibold uppercase tracking-wider text-text-muted">Status</th>
						<th class="text-left px-4 py-3 text-xs font-semibold uppercase tracking-wider text-text-muted">2FA</th>
						<th class="text-left px-4 py-3 text-xs font-semibold uppercase tracking-wider text-text-muted">Created</th>
						<th class="text-right px-4 py-3 text-xs font-semibold uppercase tracking-wider text-text-muted"></th>
					</tr>
				</thead>
				<tbody>
					{#each users as user}
						<tr class="border-b border-[rgba(255,255,255,0.04)] hover:bg-[rgba(255,255,255,0.02)] transition-colors">
							<td class="px-4 py-3 text-sm text-text">{user.email}</td>
							<td class="px-4 py-3 text-sm text-text-secondary">{user.role === 'SUPER_ADMIN' ? 'Super Admin' : 'Tenant Owner'}</td>
							<td class="px-4 py-3">
								<span class="text-xs font-medium px-2 py-0.5 rounded-full {statusColor(user.status)}">
									{user.status.replace('_', ' ')}
								</span>
							</td>
							<td class="px-4 py-3 text-sm {user.twoFactorEnabled ? 'text-success' : 'text-text-muted'}">{user.twoFactorEnabled ? 'On' : 'Off'}</td>
							<td class="px-4 py-3 text-sm text-text-secondary">{new Date(user.createdAt).toLocaleDateString()}</td>
							<td class="px-4 py-3 text-right">
								<a href="/admin/users/{user.id}" class="text-brand text-sm font-medium hover:text-brand-dark transition-colors">View</a>
							</td>
						</tr>
					{:else}
						<tr>
							<td colspan="6" class="px-4 py-8 text-center text-text-muted text-sm">No users found</td>
						</tr>
					{/each}
				</tbody>
			</table>
		</div>

		<!-- Pagination -->
		{#if totalPages > 1}
			<div class="flex items-center justify-between mt-4">
				<p class="text-sm text-text-muted">
					Page {currentPage} of {totalPages}
				</p>
				<div class="flex gap-2">
					<Button variant="secondary" disabled={currentPage <= 1} onclick={() => goToPage(currentPage - 1)}>
						Previous
					</Button>
					<Button variant="secondary" disabled={currentPage >= totalPages} onclick={() => goToPage(currentPage + 1)}>
						Next
					</Button>
				</div>
			</div>
		{/if}
	{/if}
</div>
