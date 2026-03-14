<script lang="ts">
	import { scheduleApi, domainApi } from '$lib/api/client';
	import { Button, Alert } from '$lib/components/ui';
	import { Modal, Pagination, EmptyState, SkeletonLoader } from '$lib/components/shared';
	import { goto } from '$app/navigation';
	import type { ScanSchedule } from '$lib/types/scans';
	import type { Domain } from '$lib/types/domains';
	import type { ApiError } from '$lib/types/auth';

	let schedules = $state<ScanSchedule[]>([]);
	let domains = $state<Domain[]>([]);
	let currentPage = $state(1);
	let totalPages = $state(1);
	let totalCount = $state(0);
	let pageSize = 10;
	let isLoading = $state(true);
	let error = $state<string | null>(null);
	let togglingId = $state<string | null>(null);

	// Filters
	let domainFilter = $state('');
	let enabledFilter = $state('');

	// Delete modal
	let deleteModalOpen = $state(false);
	let deleteTarget = $state<ScanSchedule | null>(null);
	let isDeleting = $state(false);

	let filteredSchedules = $derived(() => {
		let result = schedules;
		if (domainFilter) {
			result = result.filter((s) => s.domain_id === domainFilter);
		}
		if (enabledFilter === 'enabled') {
			result = result.filter((s) => s.enabled);
		} else if (enabledFilter === 'disabled') {
			result = result.filter((s) => !s.enabled);
		}
		return result;
	});

	let displayedSchedules = $derived(() => {
		const all = filteredSchedules();
		totalCount = all.length;
		totalPages = Math.max(1, Math.ceil(all.length / pageSize));
		const start = (currentPage - 1) * pageSize;
		return all.slice(start, start + pageSize);
	});

	async function loadDomains() {
		try {
			const res = await domainApi.list() as Domain[];
			domains = res;
		} catch {
			// silent
		}
	}

	async function loadSchedules() {
		isLoading = true;
		error = null;
		try {
			const res = await scheduleApi.list() as ScanSchedule[];
			schedules = res;
		} catch (err) {
			const apiErr = err as ApiError;
			error = apiErr.message || 'Failed to load schedules.';
		} finally {
			isLoading = false;
		}
	}

	async function toggleEnabled(schedule: ScanSchedule) {
		togglingId = schedule.id;
		try {
			if (schedule.enabled) {
				await scheduleApi.disable(schedule.id);
			} else {
				await scheduleApi.enable(schedule.id);
			}
			await loadSchedules();
		} catch (err) {
			const apiErr = err as ApiError;
			error = apiErr.message || 'Failed to update schedule.';
		} finally {
			togglingId = null;
		}
	}

	function openDeleteModal(schedule: ScanSchedule) {
		deleteTarget = schedule;
		deleteModalOpen = true;
	}

	async function confirmDelete() {
		if (!deleteTarget) return;
		isDeleting = true;
		try {
			await scheduleApi.delete(deleteTarget.id);
			deleteModalOpen = false;
			deleteTarget = null;
			await loadSchedules();
		} catch (err) {
			const apiErr = err as ApiError;
			error = apiErr.message || 'Failed to delete schedule.';
		} finally {
			isDeleting = false;
		}
	}

	function handleFilterChange() {
		currentPage = 1;
	}

	function goToPage(page: number) {
		currentPage = page;
	}

	function formatDate(dateStr: string | null): string {
		if (!dateStr) return '\u2014';
		return new Date(dateStr).toLocaleString(undefined, {
			month: 'short',
			day: 'numeric',
			hour: '2-digit',
			minute: '2-digit',
		});
	}

	$effect(() => {
		loadDomains();
		loadSchedules();
	});
</script>

<svelte:head>
	<title>Schedules — Reconova</title>
</svelte:head>

<div>
	<!-- Header -->
	<div class="flex items-center justify-between mb-6">
		<div>
			<h1 class="text-2xl font-bold text-white">Schedules</h1>
			<p class="text-text-secondary text-sm mt-1">{totalCount} total schedules</p>
		</div>
		<Button variant="primary" onclick={() => goto('/scans/schedules/new')}>New Schedule</Button>
	</div>

	<!-- Filters -->
	<div class="flex gap-3 mb-6">
		<select
			bind:value={domainFilter}
			class="bg-surface border border-[rgba(255,255,255,0.08)] rounded-lg px-3 py-2 text-sm text-text focus:outline-none focus:border-brand/50 transition-colors"
			onchange={handleFilterChange}
		>
			<option value="">All domains</option>
			{#each domains as domain}
				<option value={domain.id}>{domain.domain}</option>
			{/each}
		</select>

		<select
			bind:value={enabledFilter}
			class="bg-surface border border-[rgba(255,255,255,0.08)] rounded-lg px-3 py-2 text-sm text-text focus:outline-none focus:border-brand/50 transition-colors"
			onchange={handleFilterChange}
		>
			<option value="">All statuses</option>
			<option value="enabled">Enabled</option>
			<option value="disabled">Disabled</option>
		</select>
	</div>

	{#if error}
		<Alert variant="error">{error}</Alert>
		<div class="mt-4"></div>
	{/if}

	{#if isLoading}
		<div class="bg-surface/60 backdrop-blur-sm rounded-xl border border-[rgba(255,255,255,0.08)] p-6">
			<SkeletonLoader lines={6} />
		</div>
	{:else if displayedSchedules().length === 0}
		<div class="bg-surface/60 backdrop-blur-sm rounded-xl border border-[rgba(255,255,255,0.08)]">
			<EmptyState
				title="No schedules found"
				description="Create a schedule to automatically run scans on a recurring basis."
			>
				<Button variant="primary" onclick={() => goto('/scans/schedules/new')}>New Schedule</Button>
			</EmptyState>
		</div>
	{:else}
		<!-- Schedule Table -->
		<div class="bg-surface/60 backdrop-blur-sm rounded-xl border border-[rgba(255,255,255,0.08)] overflow-hidden">
			<table class="w-full">
				<thead>
					<tr class="border-b border-[rgba(255,255,255,0.08)]">
						<th class="text-left px-4 py-3 text-xs font-semibold uppercase tracking-wider text-text-muted">Domain</th>
						<th class="text-left px-4 py-3 text-xs font-semibold uppercase tracking-wider text-text-muted">Workflow</th>
						<th class="text-left px-4 py-3 text-xs font-semibold uppercase tracking-wider text-text-muted">Frequency</th>
						<th class="text-left px-4 py-3 text-xs font-semibold uppercase tracking-wider text-text-muted">Next Run</th>
						<th class="text-left px-4 py-3 text-xs font-semibold uppercase tracking-wider text-text-muted">Last Run</th>
						<th class="text-left px-4 py-3 text-xs font-semibold uppercase tracking-wider text-text-muted">Status</th>
						<th class="text-right px-4 py-3 text-xs font-semibold uppercase tracking-wider text-text-muted"></th>
					</tr>
				</thead>
				<tbody>
					{#each displayedSchedules() as schedule}
						<tr
							class="border-b border-[rgba(255,255,255,0.04)] hover:bg-[rgba(255,255,255,0.02)] transition-colors cursor-pointer"
							onclick={() => goto(`/scans/schedules/${schedule.id}`)}
						>
							<td class="px-4 py-3 text-sm text-text font-medium">{schedule.domain_name}</td>
							<td class="px-4 py-3 text-sm text-text-secondary">{schedule.workflow_name}</td>
							<td class="px-4 py-3 text-sm text-text-secondary">{schedule.cron_human}</td>
							<td class="px-4 py-3 text-sm text-text-secondary">{formatDate(schedule.next_run_at)}</td>
							<td class="px-4 py-3 text-sm text-text-secondary">{formatDate(schedule.last_run_at)}</td>
							<td class="px-4 py-3">
								<!-- svelte-ignore a11y_click_events_have_key_events -->
								<button
									class="relative inline-flex h-5 w-9 items-center rounded-full transition-colors {schedule.enabled ? 'bg-brand' : 'bg-[rgba(255,255,255,0.12)]'} {togglingId === schedule.id ? 'opacity-50' : ''}"
									disabled={togglingId === schedule.id}
									onclick={(e: MouseEvent) => { e.stopPropagation(); toggleEnabled(schedule); }}
									aria-label="{schedule.enabled ? 'Disable' : 'Enable'} schedule"
								>
									<span
										class="inline-block h-3.5 w-3.5 rounded-full bg-white transition-transform {schedule.enabled ? 'translate-x-4.5' : 'translate-x-1'}"
									></span>
								</button>
							</td>
							<td class="px-4 py-3 text-right">
								<div class="flex items-center justify-end gap-3">
									<a
										href="/scans/schedules/{schedule.id}"
										class="text-brand text-sm font-medium hover:text-brand-dark transition-colors"
										onclick={(e: MouseEvent) => e.stopPropagation()}
									>Edit</a>
									<!-- svelte-ignore a11y_click_events_have_key_events -->
									<button
										class="text-xs text-danger hover:text-red-400 font-medium transition-colors"
										onclick={(e: MouseEvent) => { e.stopPropagation(); openDeleteModal(schedule); }}
									>Delete</button>
								</div>
							</td>
						</tr>
					{/each}
				</tbody>
			</table>
		</div>

		<Pagination {currentPage} {totalPages} onPageChange={goToPage} />
	{/if}
</div>

<!-- Delete Confirmation Modal -->
<Modal title="Delete Schedule" open={deleteModalOpen} onclose={() => { deleteModalOpen = false; deleteTarget = null; }}>
	<p class="text-text-secondary text-sm mb-4">
		Are you sure you want to delete the schedule for <span class="text-white font-medium">{deleteTarget?.domain_name}</span> using <span class="text-white font-medium">{deleteTarget?.workflow_name}</span>? This action cannot be undone.
	</p>
	<div class="flex items-center justify-end gap-3">
		<Button variant="secondary" onclick={() => { deleteModalOpen = false; deleteTarget = null; }}>Cancel</Button>
		<Button variant="destructive" onclick={confirmDelete} loading={isDeleting}>
			{isDeleting ? 'Deleting...' : 'Delete Schedule'}
		</Button>
	</div>
</Modal>
