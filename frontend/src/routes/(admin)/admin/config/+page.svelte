<script lang="ts">
	import { adminConfigApi } from '$lib/api/client';
	import { Button, Alert } from '$lib/components/ui';
	import { SkeletonLoader } from '$lib/components/shared';
	import { getToastStore } from '$lib/stores/toast';
	import type { ConfigEntry } from '$lib/types/admin';
	import type { ApiError } from '$lib/types/auth';

	const toast = getToastStore();

	let configs = $state<ConfigEntry[]>([]);
	let isLoading = $state(true);
	let error = $state<string | null>(null);
	let search = $state('');
	let categoryFilter = $state('');

	// Cache status
	let cacheHits = $state(0);
	let cacheMisses = $state(0);
	let cacheSize = $state(0);
	let isInvalidating = $state(false);

	// Inline edit state
	let editingKey = $state<string | null>(null);
	let editValue = $state('');
	let editReason = $state('');
	let isSaving = $state(false);

	// Reveal state
	let revealedKeys = $state<Record<string, string>>({});
	let revealingKey = $state<string | null>(null);

	// Collapsible sections
	let collapsedCategories = $state<Record<string, boolean>>({});

	let categories = $derived<string[]>(
		[...new Set(configs.map(c => c.category))].sort()
	);

	let filteredConfigs = $derived(
		configs.filter((c: ConfigEntry) => {
			const matchesSearch = !search || c.key.toLowerCase().includes(search.toLowerCase());
			const matchesCategory = !categoryFilter || c.category === categoryFilter;
			return matchesSearch && matchesCategory;
		})
	);

	let groupedConfigs = $derived.by(() => {
		const groups = new Map<string, ConfigEntry[]>();
		for (const entry of filteredConfigs) {
			const existing = groups.get(entry.category);
			if (existing) {
				existing.push(entry);
			} else {
				groups.set(entry.category, [entry]);
			}
		}
		return Array.from(groups.entries())
			.map(([category, entries]) => ({ category, entries }))
			.sort((a, b) => a.category.localeCompare(b.category));
	});

	async function loadConfigs() {
		isLoading = true;
		error = null;
		try {
			configs = await adminConfigApi.list() as ConfigEntry[];
		} catch (err) {
			const apiErr = err as ApiError;
			error = apiErr.message || 'Failed to load configuration.';
		} finally {
			isLoading = false;
		}
	}

	async function loadCacheStatus() {
		try {
			const status = await adminConfigApi.getCacheStatus() as { hits: number; misses: number; size: number };
			cacheHits = status.hits;
			cacheMisses = status.misses;
			cacheSize = status.size;
		} catch {
			// Silently fail for cache status
		}
	}

	$effect(() => {
		loadConfigs();
		loadCacheStatus();
	});

	async function handleInvalidateCache() {
		isInvalidating = true;
		try {
			await adminConfigApi.invalidateCache();
			toast.success('Cache invalidated successfully.');
			await loadCacheStatus();
		} catch (err) {
			const apiErr = err as ApiError;
			toast.error(apiErr.message || 'Failed to invalidate cache.');
		} finally {
			isInvalidating = false;
		}
	}

	function startEdit(entry: ConfigEntry) {
		editingKey = entry.key;
		editValue = entry.is_sensitive ? '' : entry.value;
		editReason = '';
	}

	function cancelEdit() {
		editingKey = null;
		editValue = '';
		editReason = '';
	}

	async function saveEdit(key: string) {
		isSaving = true;
		try {
			await adminConfigApi.update(key, {
				value: editValue,
				reason: editReason || undefined,
			});
			toast.success(`Configuration "${key}" updated.`);
			editingKey = null;
			editValue = '';
			editReason = '';
			// Remove revealed value since it may have changed
			delete revealedKeys[key];
			await loadConfigs();
		} catch (err) {
			const apiErr = err as ApiError;
			toast.error(apiErr.message || 'Failed to update configuration.');
		} finally {
			isSaving = false;
		}
	}

	async function revealValue(key: string) {
		revealingKey = key;
		try {
			const result = await adminConfigApi.reveal(key) as { value: string };
			revealedKeys[key] = result.value;
		} catch (err) {
			const apiErr = err as ApiError;
			toast.error(apiErr.message || 'Failed to reveal value.');
		} finally {
			revealingKey = null;
		}
	}

	function hideValue(key: string) {
		delete revealedKeys[key];
		revealedKeys = { ...revealedKeys };
	}

	function toggleCategory(category: string) {
		collapsedCategories[category] = !collapsedCategories[category];
		collapsedCategories = { ...collapsedCategories };
	}

	function formatCategory(cat: string): string {
		return cat.replace(/_/g, ' ').replace(/\b\w/g, c => c.toUpperCase());
	}

	function displayValue(entry: ConfigEntry): string {
		if (entry.is_sensitive) {
			if (revealedKeys[entry.key] !== undefined) {
				return revealedKeys[entry.key];
			}
			return '\u2022\u2022\u2022\u2022\u2022\u2022\u2022\u2022';
		}
		return entry.value;
	}
</script>

<svelte:head>
	<title>System Configuration — Reconova Admin</title>
</svelte:head>

<div>
	<div class="flex items-center justify-between mb-6">
		<div>
			<h1 class="text-2xl font-bold text-white">System Configuration</h1>
			<p class="text-text-secondary text-sm mt-1">Manage application configuration values</p>
		</div>
		<div class="flex items-center gap-3">
			<!-- Cache Status -->
			<div class="flex items-center gap-2 bg-surface/60 backdrop-blur-sm border border-[rgba(255,255,255,0.08)] rounded-lg px-3 py-2">
				<div class="w-2 h-2 rounded-full bg-success"></div>
				<span class="text-xs text-text-secondary">
					Cache: {cacheSize} entries | {cacheHits}H / {cacheMisses}M
				</span>
			</div>
			<Button variant="secondary" loading={isInvalidating} onclick={handleInvalidateCache}>
				Invalidate Cache
			</Button>
			<a href="/admin/config/history" class="text-sm text-brand hover:text-brand-dark transition-colors font-medium">History</a>
			<a href="/admin/config/requests" class="text-sm text-brand hover:text-brand-dark transition-colors font-medium">Requests</a>
		</div>
	</div>

	<!-- Filters -->
	<div class="flex gap-3 mb-6">
		<div class="flex-1">
			<input
				type="text"
				bind:value={search}
				placeholder="Search by key..."
				class="w-full bg-surface border border-[rgba(255,255,255,0.08)] rounded-lg px-3 py-2 text-sm text-text placeholder:text-text-muted focus:outline-none focus:border-brand/50 transition-colors"
			/>
		</div>
		<select
			bind:value={categoryFilter}
			class="bg-surface border border-[rgba(255,255,255,0.08)] rounded-lg px-3 py-2 text-sm text-text focus:outline-none focus:border-brand/50 transition-colors"
		>
			<option value="">All categories</option>
			{#each categories as cat}
				<option value={cat}>{formatCategory(cat)}</option>
			{/each}
		</select>
	</div>

	{#if error}
		<div class="mb-4">
			<Alert variant="error">{error}</Alert>
		</div>
	{/if}

	{#if isLoading}
		<div class="bg-surface/60 backdrop-blur-sm rounded-xl border border-[rgba(255,255,255,0.08)] p-6">
			<SkeletonLoader lines={12} />
		</div>
	{:else if filteredConfigs.length === 0}
		<div class="bg-surface/60 backdrop-blur-sm rounded-xl border border-[rgba(255,255,255,0.08)] p-8 text-center">
			<p class="text-text-muted text-sm">No configuration entries found.</p>
		</div>
	{:else}
		<div class="space-y-4">
			{#each groupedConfigs as group}
				<div class="bg-surface rounded-xl border border-[rgba(255,255,255,0.08)] overflow-hidden">
					<!-- Category Header -->
					<button
						class="w-full flex items-center justify-between px-4 py-3 bg-[rgba(255,255,255,0.02)] hover:bg-[rgba(255,255,255,0.04)] transition-colors"
						onclick={() => toggleCategory(group.category)}
					>
						<div class="flex items-center gap-2">
							<svg
								class="w-4 h-4 text-text-muted transition-transform {collapsedCategories[group.category] ? '' : 'rotate-90'}"
								fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2"
							>
								<path stroke-linecap="round" stroke-linejoin="round" d="m8.25 4.5 7.5 7.5-7.5 7.5" />
							</svg>
							<span class="text-sm font-semibold text-white">{formatCategory(group.category)}</span>
						</div>
						<span class="text-xs text-text-muted">{group.entries.length} {group.entries.length === 1 ? 'entry' : 'entries'}</span>
					</button>

					{#if !collapsedCategories[group.category]}
						<div class="divide-y divide-[rgba(255,255,255,0.04)]">
							{#each group.entries as entry}
								<div class="px-4 py-3 hover:bg-[rgba(255,255,255,0.02)] transition-colors">
									<div class="flex items-start justify-between gap-4">
										<div class="flex-1 min-w-0">
											<div class="flex items-center gap-2 mb-1">
												<code class="text-sm font-mono text-brand">{entry.key}</code>
												{#if entry.is_sensitive}
													<span class="text-[10px] font-medium uppercase tracking-wider px-1.5 py-0.5 rounded bg-warning/10 text-warning">Sensitive</span>
												{/if}
											</div>
											{#if entry.description}
												<p class="text-xs text-text-muted mb-2">{entry.description}</p>
											{/if}

											{#if editingKey === entry.key}
												<!-- Inline Edit Mode -->
												<div class="space-y-2 mt-2">
													<input
														type="text"
														bind:value={editValue}
														placeholder="New value"
														class="w-full bg-surface border border-[rgba(255,255,255,0.08)] rounded-lg px-3 py-2 text-sm text-text placeholder:text-text-muted focus:outline-none focus:border-brand/50 transition-colors"
													/>
													<input
														type="text"
														bind:value={editReason}
														placeholder="Reason for change (optional)"
														class="w-full bg-surface border border-[rgba(255,255,255,0.08)] rounded-lg px-3 py-2 text-sm text-text placeholder:text-text-muted focus:outline-none focus:border-brand/50 transition-colors"
													/>
													<div class="flex gap-2">
														<Button variant="primary" loading={isSaving} onclick={() => saveEdit(entry.key)}>Save</Button>
														<Button variant="secondary" onclick={cancelEdit}>Cancel</Button>
													</div>
												</div>
											{:else}
												<!-- Display Value -->
												<button
													class="text-sm text-text-secondary font-mono bg-[rgba(255,255,255,0.03)] rounded px-2 py-1 hover:bg-[rgba(255,255,255,0.06)] transition-colors text-left max-w-full truncate"
													onclick={() => startEdit(entry)}
													title="Click to edit"
												>
													{displayValue(entry)}
												</button>
											{/if}
										</div>

										<div class="flex items-center gap-2 shrink-0">
											{#if entry.is_sensitive && editingKey !== entry.key}
												{#if revealedKeys[entry.key] !== undefined}
													<button
														class="text-xs text-text-muted hover:text-white transition-colors"
														onclick={() => hideValue(entry.key)}
													>
														Hide
													</button>
												{:else}
													<button
														class="text-xs text-brand hover:text-brand-dark transition-colors font-medium"
														disabled={revealingKey === entry.key}
														onclick={() => revealValue(entry.key)}
													>
														{revealingKey === entry.key ? 'Revealing...' : 'Reveal'}
													</button>
												{/if}
											{/if}
											<span class="text-xs text-text-muted whitespace-nowrap">
												{new Date(entry.updated_at).toLocaleDateString()}
											</span>
										</div>
									</div>
								</div>
							{/each}
						</div>
					{/if}
				</div>
			{/each}
		</div>
	{/if}
</div>
