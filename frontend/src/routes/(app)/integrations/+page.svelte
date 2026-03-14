<script lang="ts">
	import { integrationsApi } from '$lib/api/client';
	import type { Integration, IntegrationType } from '$lib/types/integrations';
	import type { ApiError } from '$lib/types/auth';
	import { Button, Alert } from '$lib/components/ui';
	import { Modal, EmptyState, SkeletonLoader } from '$lib/components/shared';

	let integrations = $state<Integration[]>([]);
	let isLoading = $state(true);
	let error = $state<string | null>(null);

	// Delete
	let deleteTarget = $state<Integration | null>(null);
	let isDeleting = $state(false);
	let deleteError = $state<string | null>(null);

	// Test
	let testingId = $state<string | null>(null);
	let testResult = $state<{ id: string; success: boolean } | null>(null);

	// Toggle
	let togglingId = $state<string | null>(null);

	const typeConfig: Record<IntegrationType, { label: string; color: string; bg: string; icon: string }> = {
		SLACK: { label: 'Slack', color: 'text-purple-400', bg: 'bg-purple-500/10', icon: 'M20.7 5.3a1 1 0 0 0-1.4 0L12 12.58 4.7 5.3a1 1 0 0 0-1.4 1.4l8 8a1 1 0 0 0 1.4 0l8-8a1 1 0 0 0 0-1.4Z' },
		EMAIL: { label: 'Email', color: 'text-blue-400', bg: 'bg-blue-500/10', icon: 'M21.75 6.75v10.5a2.25 2.25 0 0 1-2.25 2.25h-15a2.25 2.25 0 0 1-2.25-2.25V6.75m19.5 0A2.25 2.25 0 0 0 19.5 4.5h-15a2.25 2.25 0 0 0-2.25 2.25m19.5 0v.243a2.25 2.25 0 0 1-1.07 1.916l-7.5 4.615a2.25 2.25 0 0 1-2.36 0L3.32 8.91a2.25 2.25 0 0 1-1.07-1.916V6.75' },
		JIRA: { label: 'Jira', color: 'text-cyan-400', bg: 'bg-cyan-500/10', icon: 'M9 12h3.75M9 15h3.75M9 18h3.75m3 .75H18a2.25 2.25 0 0 0 2.25-2.25V6.108c0-1.135-.845-2.098-1.976-2.192a48.424 48.424 0 0 0-1.123-.08m-5.801 0c-.065.21-.1.433-.1.664 0 .414.336.75.75.75h4.5a.75.75 0 0 0 .75-.75 2.25 2.25 0 0 0-.1-.664m-5.8 0A2.251 2.251 0 0 1 13.5 2.25H15c1.012 0 1.867.668 2.15 1.586m-5.8 0c-.376.023-.75.05-1.124.08C9.095 4.01 8.25 4.973 8.25 6.108V8.25m0 0H4.875c-.621 0-1.125.504-1.125 1.125v11.25c0 .621.504 1.125 1.125 1.125h9.75c.621 0 1.125-.504 1.125-1.125V9.375c0-.621-.504-1.125-1.125-1.125H8.25Z' },
		WEBHOOK: { label: 'Webhook', color: 'text-orange-400', bg: 'bg-orange-500/10', icon: 'M13.19 8.688a4.5 4.5 0 0 1 1.242 7.244l-4.5 4.5a4.5 4.5 0 0 1-6.364-6.364l1.757-1.757m13.35-.622 1.757-1.757a4.5 4.5 0 0 0-6.364-6.364l-4.5 4.5a4.5 4.5 0 0 0 1.242 7.244' },
		SIEM: { label: 'SIEM', color: 'text-green-400', bg: 'bg-green-500/10', icon: 'M3.75 3v11.25A2.25 2.25 0 0 0 6 16.5h2.25M3.75 3h-1.5m1.5 0h16.5m0 0h1.5m-1.5 0v11.25A2.25 2.25 0 0 1 18 16.5h-2.25m-7.5 0h7.5m-7.5 0-1 3m8.5-3 1 3m0 0 .5 1.5m-.5-1.5h-9.5m0 0-.5 1.5' },
	};

	async function loadIntegrations() {
		isLoading = true;
		error = null;
		try {
			integrations = (await integrationsApi.list()) as Integration[];
		} catch (err) {
			const apiErr = err as ApiError;
			error = apiErr.message || 'Failed to load integrations.';
		} finally {
			isLoading = false;
		}
	}

	$effect(() => {
		loadIntegrations();
	});

	function confirmDelete(integration: Integration) {
		deleteTarget = integration;
		deleteError = null;
	}

	async function deleteIntegration() {
		if (!deleteTarget) return;
		isDeleting = true;
		deleteError = null;
		try {
			await integrationsApi.delete(deleteTarget.id);
			deleteTarget = null;
			await loadIntegrations();
		} catch (err) {
			const apiErr = err as ApiError;
			deleteError = apiErr.message || 'Failed to delete integration.';
		} finally {
			isDeleting = false;
		}
	}

	async function testIntegration(id: string) {
		testingId = id;
		testResult = null;
		try {
			await integrationsApi.test(id);
			testResult = { id, success: true };
			await loadIntegrations();
		} catch {
			testResult = { id, success: false };
		} finally {
			testingId = null;
		}
	}

	async function toggleIntegration(integration: Integration) {
		togglingId = integration.id;
		try {
			await integrationsApi.update(integration.id, { enabled: !integration.enabled });
			await loadIntegrations();
		} catch (err) {
			const apiErr = err as ApiError;
			error = apiErr.message || 'Failed to update integration.';
		} finally {
			togglingId = null;
		}
	}

	function formatDateTime(dateStr: string | null): string {
		if (!dateStr) return 'Never';
		return new Date(dateStr).toLocaleString();
	}
</script>

<svelte:head>
	<title>Integrations — Reconova</title>
</svelte:head>

<div>
	<!-- Header -->
	<div class="flex items-center justify-between mb-6">
		<div class="flex items-center gap-3">
			<h1 class="text-2xl font-bold text-white">Integrations</h1>
			{#if !isLoading}
				<span class="text-xs font-medium px-2.5 py-1 rounded-full bg-brand/10 text-brand">
					{integrations.length}
				</span>
			{/if}
		</div>
		<a href="/integrations/new">
			<Button variant="primary">Add Integration</Button>
		</a>
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
	{:else if integrations.length === 0}
		<div class="bg-surface rounded-xl border border-[rgba(255,255,255,0.08)]">
			<EmptyState
				title="No integrations configured"
				description="Connect external services to receive notifications and forward findings."
			>
				<a href="/integrations/new">
					<Button variant="primary">Add Integration</Button>
				</a>
			</EmptyState>
		</div>
	{:else}
		<!-- Integration Cards Grid -->
		<div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
			{#each integrations as integration}
				{@const cfg = typeConfig[integration.type]}
				<div class="bg-surface/60 backdrop-blur-sm rounded-xl border border-[rgba(255,255,255,0.08)] p-5 flex flex-col">
					<!-- Card Header -->
					<div class="flex items-start justify-between mb-4">
						<div class="flex items-center gap-3">
							<div class="w-10 h-10 rounded-lg {cfg.bg} flex items-center justify-center">
								<svg class="w-5 h-5 {cfg.color}" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="1.5">
									<path stroke-linecap="round" stroke-linejoin="round" d={cfg.icon} />
								</svg>
							</div>
							<div>
								<h3 class="text-sm font-semibold text-white">{integration.name}</h3>
								<span class="text-xs {cfg.color}">{cfg.label}</span>
							</div>
						</div>
						<!-- Toggle -->
						<button
							class="relative inline-flex h-6 w-11 items-center rounded-full transition-colors {integration.enabled ? 'bg-brand' : 'bg-[rgba(255,255,255,0.1)]'}"
							onclick={() => toggleIntegration(integration)}
							disabled={togglingId === integration.id}
							aria-label="{integration.enabled ? 'Disable' : 'Enable'} integration"
						>
							<span
								class="inline-block h-4 w-4 rounded-full bg-white transition-transform {integration.enabled ? 'translate-x-6' : 'translate-x-1'}"
							></span>
						</button>
					</div>

					<!-- Test Status -->
					<div class="flex items-center gap-2 mb-4 text-xs">
						{#if integration.last_tested_at}
							{#if integration.last_test_success}
								<svg class="w-4 h-4 text-success" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
									<path stroke-linecap="round" stroke-linejoin="round" d="M9 12.75 11.25 15 15 9.75M21 12a9 9 0 1 1-18 0 9 9 0 0 1 18 0Z" />
								</svg>
								<span class="text-success">Passed</span>
							{:else}
								<svg class="w-4 h-4 text-danger" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
									<path stroke-linecap="round" stroke-linejoin="round" d="m9.75 9.75 4.5 4.5m0-4.5-4.5 4.5M21 12a9 9 0 1 1-18 0 9 9 0 0 1 18 0Z" />
								</svg>
								<span class="text-danger">Failed</span>
							{/if}
							<span class="text-text-muted">{formatDateTime(integration.last_tested_at)}</span>
						{:else}
							<span class="text-text-muted">Not tested yet</span>
						{/if}
						{#if testResult && testResult.id === integration.id}
							<span class="ml-auto text-xs font-medium {testResult.success ? 'text-success' : 'text-danger'}">
								{testResult.success ? 'Test passed' : 'Test failed'}
							</span>
						{/if}
					</div>

					<!-- Actions -->
					<div class="flex items-center gap-2 mt-auto pt-3 border-t border-[rgba(255,255,255,0.06)]">
						<button
							class="text-xs font-medium text-text-secondary hover:text-white transition-colors px-2 py-1.5 rounded-md hover:bg-[rgba(255,255,255,0.05)] flex items-center gap-1.5"
							onclick={() => testIntegration(integration.id)}
							disabled={testingId === integration.id}
						>
							{#if testingId === integration.id}
								<svg class="w-3.5 h-3.5 animate-spin" fill="none" viewBox="0 0 24 24">
									<circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
									<path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
								</svg>
								Testing...
							{:else}
								<svg class="w-3.5 h-3.5" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
									<path stroke-linecap="round" stroke-linejoin="round" d="M5.636 18.364a9 9 0 0 1 0-12.728m12.728 0a9 9 0 0 1 0 12.728M9.172 15.828a4.5 4.5 0 0 1 0-6.364m5.656 0a4.5 4.5 0 0 1 0 6.364" />
								</svg>
								Test
							{/if}
						</button>
						<a
							href="/integrations/{integration.id}"
							class="text-xs font-medium text-text-secondary hover:text-white transition-colors px-2 py-1.5 rounded-md hover:bg-[rgba(255,255,255,0.05)] flex items-center gap-1.5"
						>
							<svg class="w-3.5 h-3.5" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
								<path stroke-linecap="round" stroke-linejoin="round" d="m16.862 4.487 1.687-1.688a1.875 1.875 0 1 1 2.652 2.652L10.582 16.07a4.5 4.5 0 0 1-1.897 1.13L6 18l.8-2.685a4.5 4.5 0 0 1 1.13-1.897l8.932-8.931Z" />
							</svg>
							Edit
						</a>
						<button
							class="text-xs font-medium text-danger hover:text-red-400 transition-colors px-2 py-1.5 rounded-md hover:bg-danger/5 flex items-center gap-1.5 ml-auto"
							onclick={() => confirmDelete(integration)}
						>
							<svg class="w-3.5 h-3.5" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
								<path stroke-linecap="round" stroke-linejoin="round" d="m14.74 9-.346 9m-4.788 0L9.26 9m9.968-3.21c.342.052.682.107 1.022.166m-1.022-.165L18.16 19.673a2.25 2.25 0 0 1-2.244 2.077H8.084a2.25 2.25 0 0 1-2.244-2.077L4.772 5.79m14.456 0a48.108 48.108 0 0 0-3.478-.397m-12 .562c.34-.059.68-.114 1.022-.165m0 0a48.11 48.11 0 0 1 3.478-.397m7.5 0v-.916c0-1.18-.91-2.164-2.09-2.201a51.964 51.964 0 0 0-3.32 0c-1.18.037-2.09 1.022-2.09 2.201v.916m7.5 0a48.667 48.667 0 0 0-7.5 0" />
							</svg>
							Delete
						</button>
					</div>
				</div>
			{/each}
		</div>
	{/if}
</div>

<!-- Delete Confirmation Modal -->
<Modal title="Delete Integration" open={deleteTarget !== null} onclose={() => { deleteTarget = null; }}>
	<div>
		<p class="text-text-secondary text-sm mb-1">
			Are you sure you want to delete this integration?
		</p>
		<p class="text-white text-sm font-medium mb-4">{deleteTarget?.name}</p>
		<p class="text-text-muted text-xs mb-6">
			This will permanently remove the integration and all associated notification rules.
		</p>

		{#if deleteError}
			<div class="mb-4">
				<Alert variant="error">{deleteError}</Alert>
			</div>
		{/if}

		<div class="flex justify-end gap-3">
			<Button variant="ghost" onclick={() => { deleteTarget = null; }} disabled={isDeleting}>Cancel</Button>
			<Button variant="destructive" onclick={deleteIntegration} loading={isDeleting}>Delete Integration</Button>
		</div>
	</div>
</Modal>
