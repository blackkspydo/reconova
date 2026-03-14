<script lang="ts">
	import { page } from '$app/state';
	import { adminApi } from '$lib/api/client';
	import { Button, Alert } from '$lib/components/ui';
	import type { AdminUserDetail, ApiError } from '$lib/types/auth';

	let user = $state<AdminUserDetail | null>(null);
	let isLoading = $state(true);
	let error = $state<string | null>(null);
	let actionLoading = $state<string | null>(null);
	let actionSuccess = $state<string | null>(null);
	let showConfirm = $state<string | null>(null);

	async function loadUser() {
		isLoading = true;
		error = null;
		try {
			user = await adminApi.getUser(page.params.id) as AdminUserDetail;
		} catch (err) {
			const apiErr = err as ApiError;
			error = apiErr.message || 'Failed to load user.';
		} finally {
			isLoading = false;
		}
	}

	$effect(() => {
		loadUser();
	});

	async function performAction(action: string) {
		if (!user) return;
		actionLoading = action;
		actionSuccess = null;
		error = null;

		try {
			switch (action) {
				case 'deactivate':
					await adminApi.deactivateUser(user.id);
					actionSuccess = 'User deactivated.';
					break;
				case 'enable':
					await adminApi.enableUser(user.id);
					actionSuccess = 'User re-enabled.';
					break;
				case 'unlock':
					await adminApi.unlockUser(user.id);
					actionSuccess = 'User unlocked.';
					break;
				case 'reset-2fa':
					await adminApi.reset2fa(user.id);
					actionSuccess = '2FA has been reset.';
					break;
			}
			showConfirm = null;
			await loadUser();
		} catch (err) {
			const apiErr = err as ApiError;
			error = apiErr.message || 'Action failed.';
		} finally {
			actionLoading = null;
		}
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
	<title>{user?.email ?? 'User Detail'} — Reconova Admin</title>
</svelte:head>

<div class="max-w-3xl">
	<a href="/users" class="text-brand text-sm font-medium hover:text-brand-dark transition-colors inline-flex items-center gap-1 mb-6">
		<svg class="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
			<path stroke-linecap="round" stroke-linejoin="round" d="M10.5 19.5 3 12m0 0 7.5-7.5M3 12h18" />
		</svg>
		Back to Users
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
		<div class="flex justify-center py-12">
			<svg class="w-8 h-8 animate-spin text-brand" fill="none" viewBox="0 0 24 24">
				<circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
				<path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
			</svg>
		</div>
	{:else if user}
		<!-- User Info Card -->
		<div class="bg-surface rounded-xl border border-[rgba(255,255,255,0.08)] p-6">
			<div class="flex items-start justify-between mb-6">
				<div>
					<h1 class="text-xl font-bold text-white">{user.email}</h1>
					<p class="text-text-secondary text-sm mt-1">{user.tenantName}</p>
				</div>
				<span class="text-xs font-medium px-3 py-1 rounded-full {statusColor(user.status)}">
					{user.status.replace('_', ' ')}
				</span>
			</div>

			<div class="grid grid-cols-2 gap-4">
				<div>
					<p class="text-xs text-text-muted uppercase tracking-wider mb-1">Role</p>
					<p class="text-sm text-text">{user.role === 'SUPER_ADMIN' ? 'Super Admin' : 'Tenant Owner'}</p>
				</div>
				<div>
					<p class="text-xs text-text-muted uppercase tracking-wider mb-1">Tenant ID</p>
					<p class="text-sm text-text font-mono">{user.tenantId}</p>
				</div>
				<div>
					<p class="text-xs text-text-muted uppercase tracking-wider mb-1">2FA</p>
					<p class="text-sm {user.twoFactorEnabled ? 'text-success' : 'text-warning'}">{user.twoFactorEnabled ? 'Enabled' : 'Disabled'}</p>
				</div>
				<div>
					<p class="text-xs text-text-muted uppercase tracking-wider mb-1">Created</p>
					<p class="text-sm text-text">{new Date(user.createdAt).toLocaleString()}</p>
				</div>
				<div>
					<p class="text-xs text-text-muted uppercase tracking-wider mb-1">Last Login</p>
					<p class="text-sm text-text">{user.lastLoginAt ? new Date(user.lastLoginAt).toLocaleString() : 'Never'}</p>
				</div>
				<div>
					<p class="text-xs text-text-muted uppercase tracking-wider mb-1">Last Login IP</p>
					<p class="text-sm text-text font-mono">{user.lastLoginIp ?? '—'}</p>
				</div>
			</div>
		</div>

		<!-- Actions -->
		<div class="mt-6 bg-surface rounded-xl border border-[rgba(255,255,255,0.08)] p-6">
			<h2 class="text-lg font-semibold text-white mb-4">Actions</h2>
			<div class="flex flex-wrap gap-3">
				{#if user.status === 'LOCKED'}
					<Button
						variant="secondary"
						loading={actionLoading === 'unlock'}
						onclick={() => performAction('unlock')}
					>
						Unlock User
					</Button>
				{/if}

				{#if user.status === 'DEACTIVATED'}
					<Button
						variant="secondary"
						loading={actionLoading === 'enable'}
						onclick={() => performAction('enable')}
					>
						Re-enable User
					</Button>
				{/if}

				{#if user.twoFactorEnabled}
					{#if showConfirm === 'reset-2fa'}
						<div class="flex items-center gap-2 bg-danger/10 rounded-lg px-3 py-2">
							<span class="text-sm text-danger">Reset 2FA?</span>
							<Button variant="destructive" loading={actionLoading === 'reset-2fa'} onclick={() => performAction('reset-2fa')}>
								Confirm
							</Button>
							<Button variant="ghost" onclick={() => showConfirm = null}>Cancel</Button>
						</div>
					{:else}
						<Button variant="secondary" onclick={() => showConfirm = 'reset-2fa'}>
							Reset 2FA
						</Button>
					{/if}
				{/if}

				{#if user.status === 'ACTIVE' || user.status === 'LOCKED'}
					{#if showConfirm === 'deactivate'}
						<div class="flex items-center gap-2 bg-danger/10 rounded-lg px-3 py-2">
							<span class="text-sm text-danger">Deactivate?</span>
							<Button variant="destructive" loading={actionLoading === 'deactivate'} onclick={() => performAction('deactivate')}>
								Confirm
							</Button>
							<Button variant="ghost" onclick={() => showConfirm = null}>Cancel</Button>
						</div>
					{:else}
						<Button variant="destructive" onclick={() => showConfirm = 'deactivate'}>
							Deactivate User
						</Button>
					{/if}
				{/if}
			</div>
		</div>
	{/if}
</div>
