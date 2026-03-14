<script lang="ts">
	import { featureFlagsApi } from '$lib/api/client';
	import { Button, Alert } from '$lib/components/ui';
	import { Modal, EmptyState, SkeletonLoader } from '$lib/components/shared';
	import { getToastStore } from '$lib/stores/toast';
	import type { FeatureFlag } from '$lib/types/admin';
	import type { ApiError } from '$lib/types/auth';

	const toast = getToastStore();

	const TIERS = ['FREE', 'STARTER', 'PROFESSIONAL', 'ENTERPRISE'];

	let flags = $state<FeatureFlag[]>([]);
	let isLoading = $state(true);
	let error = $state<string | null>(null);

	// Modal state
	let showCreateModal = $state(false);
	let showEditModal = $state(false);
	let showDeleteModal = $state(false);
	let isSaving = $state(false);

	// Form state
	let formKey = $state('');
	let formName = $state('');
	let formDescription = $state('');
	let formEnabled = $state(false);
	let formTierGated = $state(false);
	let formMinTier = $state('FREE');
	let editingFlag = $state<FeatureFlag | null>(null);
	let deletingFlag = $state<FeatureFlag | null>(null);

	async function loadFlags() {
		isLoading = true;
		error = null;
		try {
			flags = (await featureFlagsApi.list()) as FeatureFlag[];
		} catch (err) {
			const apiErr = err as ApiError;
			error = apiErr.message || 'Failed to load feature flags.';
		} finally {
			isLoading = false;
		}
	}

	$effect(() => {
		loadFlags();
	});

	function resetForm() {
		formKey = '';
		formName = '';
		formDescription = '';
		formEnabled = false;
		formTierGated = false;
		formMinTier = 'FREE';
	}

	function openCreate() {
		resetForm();
		showCreateModal = true;
	}

	function openEdit(flag: FeatureFlag) {
		editingFlag = flag;
		formKey = flag.key;
		formName = flag.name;
		formDescription = flag.description || '';
		formEnabled = flag.enabled;
		formTierGated = flag.tier_gated;
		formMinTier = flag.min_tier || 'FREE';
		showEditModal = true;
	}

	function openDelete(flag: FeatureFlag) {
		deletingFlag = flag;
		showDeleteModal = true;
	}

	function sanitizeKey(value: string): string {
		return value.replace(/\s+/g, '_').toLowerCase();
	}

	async function handleCreate() {
		isSaving = true;
		error = null;
		try {
			await featureFlagsApi.create({
				key: formKey,
				name: formName,
				description: formDescription || undefined,
				enabled: formEnabled,
				tier_gated: formTierGated,
				min_tier: formTierGated ? formMinTier : undefined,
			});
			toast.success('Feature flag created successfully.');
			showCreateModal = false;
			await loadFlags();
		} catch (err) {
			const apiErr = err as ApiError;
			error = apiErr.message || 'Failed to create feature flag.';
		} finally {
			isSaving = false;
		}
	}

	async function handleUpdate() {
		if (!editingFlag) return;
		isSaving = true;
		error = null;
		try {
			await featureFlagsApi.update(editingFlag.id, {
				name: formName,
				description: formDescription || undefined,
				enabled: formEnabled,
				tier_gated: formTierGated,
				min_tier: formTierGated ? formMinTier : undefined,
			});
			toast.success('Feature flag updated successfully.');
			showEditModal = false;
			editingFlag = null;
			await loadFlags();
		} catch (err) {
			const apiErr = err as ApiError;
			error = apiErr.message || 'Failed to update feature flag.';
		} finally {
			isSaving = false;
		}
	}

	async function handleDelete() {
		if (!deletingFlag) return;
		isSaving = true;
		error = null;
		try {
			await featureFlagsApi.delete(deletingFlag.id);
			toast.success('Feature flag deleted successfully.');
			showDeleteModal = false;
			deletingFlag = null;
			await loadFlags();
		} catch (err) {
			const apiErr = err as ApiError;
			error = apiErr.message || 'Failed to delete feature flag.';
		} finally {
			isSaving = false;
		}
	}

	async function handleToggle(flag: FeatureFlag) {
		try {
			await featureFlagsApi.update(flag.id, { enabled: !flag.enabled });
			flag.enabled = !flag.enabled;
			toast.success(`Flag "${flag.key}" ${flag.enabled ? 'enabled' : 'disabled'}.`);
		} catch (err) {
			const apiErr = err as ApiError;
			toast.error(apiErr.message || 'Failed to toggle flag.');
		}
	}
</script>

<svelte:head>
	<title>Feature Flags — Reconova Admin</title>
</svelte:head>

<div>
	<div class="flex items-center justify-between mb-6">
		<div>
			<h1 class="text-2xl font-bold text-white">Feature Flags</h1>
			<p class="text-text-secondary text-sm mt-1">Manage feature flags and tier gating</p>
		</div>
		<Button variant="primary" onclick={openCreate}>Create Flag</Button>
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
	{:else if flags.length === 0}
		<div class="bg-surface/60 backdrop-blur-sm rounded-xl border border-[rgba(255,255,255,0.08)]">
			<EmptyState title="No feature flags" description="Create your first feature flag to control functionality.">
				<Button variant="primary" onclick={openCreate}>Create Flag</Button>
			</EmptyState>
		</div>
	{:else}
		<div class="bg-surface rounded-xl border border-[rgba(255,255,255,0.08)] overflow-hidden">
			<table class="w-full">
				<thead>
					<tr class="border-b border-[rgba(255,255,255,0.08)]">
						<th class="text-left px-4 py-3 text-xs font-semibold uppercase tracking-wider text-text-muted">Key</th>
						<th class="text-left px-4 py-3 text-xs font-semibold uppercase tracking-wider text-text-muted">Name</th>
						<th class="text-left px-4 py-3 text-xs font-semibold uppercase tracking-wider text-text-muted">Enabled</th>
						<th class="text-left px-4 py-3 text-xs font-semibold uppercase tracking-wider text-text-muted">Tier Gated</th>
						<th class="text-left px-4 py-3 text-xs font-semibold uppercase tracking-wider text-text-muted">Min Tier</th>
						<th class="text-right px-4 py-3 text-xs font-semibold uppercase tracking-wider text-text-muted">Actions</th>
					</tr>
				</thead>
				<tbody>
					{#each flags as flag}
						<tr class="border-b border-[rgba(255,255,255,0.04)] hover:bg-[rgba(255,255,255,0.02)] transition-colors">
							<td class="px-4 py-3">
								<code class="text-sm font-mono text-text bg-[rgba(255,255,255,0.05)] px-1.5 py-0.5 rounded">{flag.key}</code>
							</td>
							<td class="px-4 py-3 text-sm text-text">{flag.name}</td>
							<td class="px-4 py-3">
								<button
									onclick={() => handleToggle(flag)}
									class="relative inline-flex h-5 w-9 items-center rounded-full transition-colors {flag.enabled ? 'bg-brand' : 'bg-[rgba(255,255,255,0.1)]'}"
									aria-label="Toggle {flag.key}"
								>
									<span class="inline-block h-3.5 w-3.5 rounded-full bg-white transition-transform {flag.enabled ? 'translate-x-4.5' : 'translate-x-0.5'}"></span>
								</button>
							</td>
							<td class="px-4 py-3 text-sm {flag.tier_gated ? 'text-brand' : 'text-text-muted'}">{flag.tier_gated ? 'Yes' : 'No'}</td>
							<td class="px-4 py-3 text-sm text-text-secondary">{flag.tier_gated && flag.min_tier ? flag.min_tier : '—'}</td>
							<td class="px-4 py-3 text-right">
								<div class="flex items-center justify-end gap-2">
									<button onclick={() => openEdit(flag)} class="text-brand text-sm font-medium hover:text-brand-dark transition-colors">Edit</button>
									<button onclick={() => openDelete(flag)} class="text-danger text-sm font-medium hover:text-red-400 transition-colors">Delete</button>
								</div>
							</td>
						</tr>
					{/each}
				</tbody>
			</table>
		</div>
	{/if}
</div>

<!-- Create Modal -->
<Modal title="Create Feature Flag" open={showCreateModal} onclose={() => { showCreateModal = false; }}>
	<form onsubmit={(e) => { e.preventDefault(); handleCreate(); }} class="space-y-4">
		<div>
			<label for="create-key" class="block text-sm font-medium text-text-secondary mb-1">Key</label>
			<input
				id="create-key"
				type="text"
				bind:value={formKey}
				oninput={() => { formKey = sanitizeKey(formKey); }}
				placeholder="e.g. enable_dark_mode"
				required
				class="w-full bg-surface border border-[rgba(255,255,255,0.08)] rounded-lg px-3 py-2 text-sm text-text placeholder:text-text-muted focus:outline-none focus:border-brand/50 transition-colors"
			/>
		</div>
		<div>
			<label for="create-name" class="block text-sm font-medium text-text-secondary mb-1">Name</label>
			<input
				id="create-name"
				type="text"
				bind:value={formName}
				placeholder="e.g. Enable Dark Mode"
				required
				class="w-full bg-surface border border-[rgba(255,255,255,0.08)] rounded-lg px-3 py-2 text-sm text-text placeholder:text-text-muted focus:outline-none focus:border-brand/50 transition-colors"
			/>
		</div>
		<div>
			<label for="create-desc" class="block text-sm font-medium text-text-secondary mb-1">Description (optional)</label>
			<textarea
				id="create-desc"
				bind:value={formDescription}
				placeholder="Describe what this flag controls..."
				rows="2"
				class="w-full bg-surface border border-[rgba(255,255,255,0.08)] rounded-lg px-3 py-2 text-sm text-text placeholder:text-text-muted focus:outline-none focus:border-brand/50 transition-colors resize-none"
			></textarea>
		</div>
		<div class="flex items-center justify-between">
			<label for="create-enabled" class="text-sm font-medium text-text-secondary">Enabled</label>
			<button
				id="create-enabled"
				type="button"
				onclick={() => { formEnabled = !formEnabled; }}
				class="relative inline-flex h-5 w-9 items-center rounded-full transition-colors {formEnabled ? 'bg-brand' : 'bg-[rgba(255,255,255,0.1)]'}"
				aria-label="Toggle enabled"
			>
				<span class="inline-block h-3.5 w-3.5 rounded-full bg-white transition-transform {formEnabled ? 'translate-x-4.5' : 'translate-x-0.5'}"></span>
			</button>
		</div>
		<div class="flex items-center justify-between">
			<label for="create-tier-gated" class="text-sm font-medium text-text-secondary">Tier Gated</label>
			<button
				id="create-tier-gated"
				type="button"
				onclick={() => { formTierGated = !formTierGated; }}
				class="relative inline-flex h-5 w-9 items-center rounded-full transition-colors {formTierGated ? 'bg-brand' : 'bg-[rgba(255,255,255,0.1)]'}"
				aria-label="Toggle tier gated"
			>
				<span class="inline-block h-3.5 w-3.5 rounded-full bg-white transition-transform {formTierGated ? 'translate-x-4.5' : 'translate-x-0.5'}"></span>
			</button>
		</div>
		{#if formTierGated}
			<div>
				<label for="create-min-tier" class="block text-sm font-medium text-text-secondary mb-1">Minimum Tier</label>
				<select
					id="create-min-tier"
					bind:value={formMinTier}
					class="w-full bg-surface border border-[rgba(255,255,255,0.08)] rounded-lg px-3 py-2 text-sm text-text focus:outline-none focus:border-brand/50 transition-colors"
				>
					{#each TIERS as tier}
						<option value={tier}>{tier}</option>
					{/each}
				</select>
			</div>
		{/if}
		<div class="flex justify-end gap-3 pt-2">
			<Button variant="secondary" onclick={() => { showCreateModal = false; }}>Cancel</Button>
			<Button variant="primary" type="submit" loading={isSaving}>Create</Button>
		</div>
	</form>
</Modal>

<!-- Edit Modal -->
<Modal title="Edit Feature Flag" open={showEditModal} onclose={() => { showEditModal = false; editingFlag = null; }}>
	<form onsubmit={(e) => { e.preventDefault(); handleUpdate(); }} class="space-y-4">
		<div>
			<label for="edit-key" class="block text-sm font-medium text-text-secondary mb-1">Key</label>
			<input
				id="edit-key"
				type="text"
				value={formKey}
				disabled
				class="w-full bg-surface border border-[rgba(255,255,255,0.08)] rounded-lg px-3 py-2 text-sm text-text-muted cursor-not-allowed"
			/>
		</div>
		<div>
			<label for="edit-name" class="block text-sm font-medium text-text-secondary mb-1">Name</label>
			<input
				id="edit-name"
				type="text"
				bind:value={formName}
				required
				class="w-full bg-surface border border-[rgba(255,255,255,0.08)] rounded-lg px-3 py-2 text-sm text-text placeholder:text-text-muted focus:outline-none focus:border-brand/50 transition-colors"
			/>
		</div>
		<div>
			<label for="edit-desc" class="block text-sm font-medium text-text-secondary mb-1">Description (optional)</label>
			<textarea
				id="edit-desc"
				bind:value={formDescription}
				rows="2"
				class="w-full bg-surface border border-[rgba(255,255,255,0.08)] rounded-lg px-3 py-2 text-sm text-text placeholder:text-text-muted focus:outline-none focus:border-brand/50 transition-colors resize-none"
			></textarea>
		</div>
		<div class="flex items-center justify-between">
			<label for="edit-enabled" class="text-sm font-medium text-text-secondary">Enabled</label>
			<button
				id="edit-enabled"
				type="button"
				onclick={() => { formEnabled = !formEnabled; }}
				class="relative inline-flex h-5 w-9 items-center rounded-full transition-colors {formEnabled ? 'bg-brand' : 'bg-[rgba(255,255,255,0.1)]'}"
				aria-label="Toggle enabled"
			>
				<span class="inline-block h-3.5 w-3.5 rounded-full bg-white transition-transform {formEnabled ? 'translate-x-4.5' : 'translate-x-0.5'}"></span>
			</button>
		</div>
		<div class="flex items-center justify-between">
			<label for="edit-tier-gated" class="text-sm font-medium text-text-secondary">Tier Gated</label>
			<button
				id="edit-tier-gated"
				type="button"
				onclick={() => { formTierGated = !formTierGated; }}
				class="relative inline-flex h-5 w-9 items-center rounded-full transition-colors {formTierGated ? 'bg-brand' : 'bg-[rgba(255,255,255,0.1)]'}"
				aria-label="Toggle tier gated"
			>
				<span class="inline-block h-3.5 w-3.5 rounded-full bg-white transition-transform {formTierGated ? 'translate-x-4.5' : 'translate-x-0.5'}"></span>
			</button>
		</div>
		{#if formTierGated}
			<div>
				<label for="edit-min-tier" class="block text-sm font-medium text-text-secondary mb-1">Minimum Tier</label>
				<select
					id="edit-min-tier"
					bind:value={formMinTier}
					class="w-full bg-surface border border-[rgba(255,255,255,0.08)] rounded-lg px-3 py-2 text-sm text-text focus:outline-none focus:border-brand/50 transition-colors"
				>
					{#each TIERS as tier}
						<option value={tier}>{tier}</option>
					{/each}
				</select>
			</div>
		{/if}
		<div class="flex justify-end gap-3 pt-2">
			<Button variant="secondary" onclick={() => { showEditModal = false; editingFlag = null; }}>Cancel</Button>
			<Button variant="primary" type="submit" loading={isSaving}>Save Changes</Button>
		</div>
	</form>
</Modal>

<!-- Delete Confirmation Modal -->
<Modal title="Delete Feature Flag" open={showDeleteModal} onclose={() => { showDeleteModal = false; deletingFlag = null; }}>
	<div class="space-y-4">
		<p class="text-sm text-text-secondary">
			Are you sure you want to delete the flag <code class="font-mono text-text bg-[rgba(255,255,255,0.05)] px-1.5 py-0.5 rounded">{deletingFlag?.key}</code>? This action cannot be undone.
		</p>
		<div class="flex justify-end gap-3 pt-2">
			<Button variant="secondary" onclick={() => { showDeleteModal = false; deletingFlag = null; }}>Cancel</Button>
			<Button variant="danger" loading={isSaving} onclick={handleDelete}>Delete</Button>
		</div>
	</div>
</Modal>
