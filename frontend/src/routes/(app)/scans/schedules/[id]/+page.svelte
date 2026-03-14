<script lang="ts">
	import { scheduleApi } from '$lib/api/client';
	import { Button, Alert } from '$lib/components/ui';
	import { Modal, SkeletonLoader } from '$lib/components/shared';
	import { page } from '$app/state';
	import { goto } from '$app/navigation';
	import type { ScanSchedule } from '$lib/types/scans';
	import type { ApiError } from '$lib/types/auth';

	let schedule = $state<ScanSchedule | null>(null);
	let isLoading = $state(true);
	let error = $state<string | null>(null);
	let successMessage = $state<string | null>(null);
	let isUpdating = $state(false);
	let isToggling = $state(false);
	let isDeleting = $state(false);
	let deleteModalOpen = $state(false);

	// Edit state
	let selectedPreset = $state('');
	let customCron = $state('');
	let isEditing = $state(false);

	let scheduleId = $derived(page.params.id);

	// Preset options
	const presets = [
		{ label: 'Daily', value: '0 0 * * *', description: 'Every day at midnight' },
		{ label: 'Weekly on Monday', value: '0 0 * * 1', description: 'Every Monday at midnight' },
		{ label: 'Weekly on Friday', value: '0 0 * * 5', description: 'Every Friday at midnight' },
		{ label: 'Bi-weekly', value: '0 0 1,15 * *', description: '1st and 15th of each month' },
		{ label: 'Monthly', value: '0 0 1 * *', description: '1st of each month at midnight' },
		{ label: 'Custom', value: 'custom', description: 'Enter a custom cron expression' },
	];

	let cronExpression = $derived(selectedPreset === 'custom' ? customCron : selectedPreset);
	let hasChanges = $derived(cronExpression !== '' && cronExpression !== schedule?.cron_expression);

	function initEditState() {
		if (!schedule) return;
		const matchingPreset = presets.find((p) => p.value === schedule!.cron_expression);
		if (matchingPreset) {
			selectedPreset = matchingPreset.value;
			customCron = '';
		} else {
			selectedPreset = 'custom';
			customCron = schedule.cron_expression;
		}
	}

	async function loadSchedule() {
		isLoading = true;
		error = null;
		try {
			schedule = await scheduleApi.get(scheduleId) as ScanSchedule;
			initEditState();
		} catch (err) {
			const apiErr = err as ApiError;
			error = apiErr.message || 'Failed to load schedule.';
		} finally {
			isLoading = false;
		}
	}

	async function handleUpdate() {
		if (!schedule || !cronExpression || !hasChanges) return;
		isUpdating = true;
		error = null;
		successMessage = null;
		try {
			await scheduleApi.update(schedule.id, { cron_expression: cronExpression });
			successMessage = 'Schedule updated successfully.';
			isEditing = false;
			await loadSchedule();
		} catch (err) {
			const apiErr = err as ApiError;
			error = apiErr.message || 'Failed to update schedule.';
		} finally {
			isUpdating = false;
		}
	}

	async function toggleEnabled() {
		if (!schedule) return;
		isToggling = true;
		error = null;
		successMessage = null;
		try {
			if (schedule.enabled) {
				await scheduleApi.disable(schedule.id);
				successMessage = 'Schedule disabled.';
			} else {
				await scheduleApi.enable(schedule.id);
				successMessage = 'Schedule enabled.';
			}
			await loadSchedule();
		} catch (err) {
			const apiErr = err as ApiError;
			error = apiErr.message || 'Failed to update schedule.';
		} finally {
			isToggling = false;
		}
	}

	async function confirmDelete() {
		if (!schedule) return;
		isDeleting = true;
		try {
			await scheduleApi.delete(schedule.id);
			goto('/scans/schedules');
		} catch (err) {
			const apiErr = err as ApiError;
			error = apiErr.message || 'Failed to delete schedule.';
			deleteModalOpen = false;
		} finally {
			isDeleting = false;
		}
	}

	function cancelEdit() {
		isEditing = false;
		initEditState();
	}

	function formatDate(dateStr: string | null): string {
		if (!dateStr) return '\u2014';
		return new Date(dateStr).toLocaleString(undefined, {
			month: 'short',
			day: 'numeric',
			year: 'numeric',
			hour: '2-digit',
			minute: '2-digit',
		});
	}

	$effect(() => {
		loadSchedule();
	});
</script>

<svelte:head>
	<title>{schedule ? `${schedule.domain_name} Schedule` : 'Schedule Details'} — Reconova</title>
</svelte:head>

<div class="max-w-3xl">
	{#if isLoading}
		<div class="bg-surface/60 backdrop-blur-sm rounded-xl border border-[rgba(255,255,255,0.08)] p-6">
			<SkeletonLoader lines={5} />
		</div>
	{:else if error && !schedule}
		<Alert variant="error">{error}</Alert>
		<div class="mt-4">
			<Button variant="secondary" onclick={() => goto('/scans/schedules')}>Back to Schedules</Button>
		</div>
	{:else if schedule}
		<!-- Header -->
		<div class="flex items-start justify-between mb-6">
			<div>
				<div class="flex items-center gap-2 mb-1">
					<button onclick={() => goto('/scans/schedules')} class="text-text-muted hover:text-white transition-colors">
						<svg class="w-5 h-5" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
							<path stroke-linecap="round" stroke-linejoin="round" d="M15 19l-7-7 7-7" />
						</svg>
					</button>
					<h1 class="text-2xl font-bold text-white">{schedule.domain_name}</h1>
					<span class="text-text-muted text-lg">/</span>
					<span class="text-lg text-text-secondary">{schedule.workflow_name}</span>
				</div>
				<div class="flex items-center gap-4 mt-2">
					<span class="inline-flex items-center gap-1.5 text-xs font-medium px-2.5 py-1 rounded-full {schedule.enabled ? 'bg-green-500/10 text-green-400 border border-green-500/20' : 'bg-[rgba(255,255,255,0.05)] text-text-muted border border-[rgba(255,255,255,0.08)]'}">
						{schedule.enabled ? 'Enabled' : 'Disabled'}
					</span>
					<span class="text-sm text-text-muted">{schedule.cron_human}</span>
				</div>
			</div>
			<div class="flex items-center gap-2">
				<Button variant="secondary" onclick={toggleEnabled} loading={isToggling}>
					{isToggling ? 'Updating...' : schedule.enabled ? 'Disable' : 'Enable'}
				</Button>
				<Button variant="destructive" onclick={() => { deleteModalOpen = true; }}>Delete</Button>
			</div>
		</div>

		{#if error}
			<Alert variant="error">{error}</Alert>
			<div class="mt-4"></div>
		{/if}

		{#if successMessage}
			<Alert variant="success">{successMessage}</Alert>
			<div class="mt-4"></div>
		{/if}

		<!-- Schedule Info Card -->
		<div class="bg-surface/60 backdrop-blur-sm rounded-xl border border-[rgba(255,255,255,0.08)] p-6 mb-6">
			<h2 class="text-sm font-semibold uppercase tracking-wider text-text-muted mb-4">Schedule Details</h2>
			<div class="space-y-3">
				<div class="flex items-center justify-between">
					<span class="text-sm text-text-muted">Domain</span>
					<span class="text-sm text-text font-medium">{schedule.domain_name}</span>
				</div>
				<div class="flex items-center justify-between">
					<span class="text-sm text-text-muted">Workflow</span>
					<span class="text-sm text-text font-medium">{schedule.workflow_name}</span>
				</div>
				<div class="flex items-center justify-between">
					<span class="text-sm text-text-muted">Frequency</span>
					<span class="text-sm text-text font-medium">{schedule.cron_human}</span>
				</div>
				<div class="flex items-center justify-between">
					<span class="text-sm text-text-muted">Cron Expression</span>
					<span class="text-sm text-text font-mono">{schedule.cron_expression}</span>
				</div>
				<div class="flex items-center justify-between">
					<span class="text-sm text-text-muted">Status</span>
					<span class="text-sm font-medium {schedule.enabled ? 'text-green-400' : 'text-text-muted'}">{schedule.enabled ? 'Enabled' : 'Disabled'}</span>
				</div>
				{#if schedule.disabled_reason}
					<div class="flex items-center justify-between">
						<span class="text-sm text-text-muted">Disabled Reason</span>
						<span class="text-sm text-text-secondary">{schedule.disabled_reason}</span>
					</div>
				{/if}
				<div class="flex items-center justify-between">
					<span class="text-sm text-text-muted">Next Run</span>
					<span class="text-sm text-text-secondary">{formatDate(schedule.next_run_at)}</span>
				</div>
				<div class="flex items-center justify-between">
					<span class="text-sm text-text-muted">Last Run</span>
					<span class="text-sm text-text-secondary">{formatDate(schedule.last_run_at)}</span>
				</div>
				<div class="flex items-center justify-between">
					<span class="text-sm text-text-muted">Estimated Credits</span>
					<span class="text-sm text-text-secondary">{schedule.estimated_credits} credits</span>
				</div>
				<div class="flex items-center justify-between">
					<span class="text-sm text-text-muted">Created</span>
					<span class="text-sm text-text-secondary">{formatDate(schedule.created_at)}</span>
				</div>
			</div>
		</div>

		<!-- Edit Frequency -->
		<div class="bg-surface/60 backdrop-blur-sm rounded-xl border border-[rgba(255,255,255,0.08)] p-6 mb-6">
			<div class="flex items-center justify-between mb-4">
				<h2 class="text-sm font-semibold uppercase tracking-wider text-text-muted">Edit Frequency</h2>
				{#if !isEditing}
					<button
						class="text-brand text-sm font-medium hover:text-brand-dark transition-colors"
						onclick={() => { isEditing = true; }}
					>Edit</button>
				{/if}
			</div>

			{#if isEditing}
				<div class="space-y-2 mb-4">
					{#each presets as preset}
						<label
							class="flex items-center gap-3 p-3 rounded-lg border cursor-pointer transition-all {selectedPreset === preset.value ? 'border-brand/50 bg-brand/5' : 'border-[rgba(255,255,255,0.08)] hover:border-[rgba(255,255,255,0.16)]'}"
						>
							<input
								type="radio"
								name="schedule"
								value={preset.value}
								bind:group={selectedPreset}
								class="accent-brand"
							/>
							<div class="flex-1">
								<p class="text-sm font-medium text-white">{preset.label}</p>
								<p class="text-xs text-text-muted mt-0.5">{preset.description}</p>
							</div>
							{#if preset.value !== 'custom'}
								<span class="text-xs font-mono text-text-muted">{preset.value}</span>
							{/if}
						</label>
					{/each}
				</div>

				{#if selectedPreset === 'custom'}
					<div class="mb-4">
						<input
							type="text"
							bind:value={customCron}
							placeholder="* * * * *"
							class="w-full bg-surface border border-[rgba(255,255,255,0.08)] rounded-lg px-3 py-3 text-sm text-text font-mono focus:outline-none focus:border-brand/50 transition-colors"
						/>
						<p class="text-xs text-text-muted mt-2">
							Format: minute hour day-of-month month day-of-week. Example: <span class="font-mono text-text-secondary">0 2 * * 1</span> runs at 2:00 AM every Monday.
						</p>
					</div>
				{/if}

				<div class="flex items-center justify-end gap-3">
					<Button variant="secondary" onclick={cancelEdit}>Cancel</Button>
					<Button
						variant="primary"
						onclick={handleUpdate}
						loading={isUpdating}
						disabled={!hasChanges || (selectedPreset === 'custom' && !customCron)}
					>
						{isUpdating ? 'Saving...' : 'Save Changes'}
					</Button>
				</div>
			{:else}
				<p class="text-sm text-text-secondary">
					Currently set to <span class="text-white font-medium">{schedule.cron_human}</span>
					<span class="text-text-muted font-mono ml-2">({schedule.cron_expression})</span>
				</p>
			{/if}
		</div>
	{/if}
</div>

<!-- Delete Confirmation Modal -->
<Modal title="Delete Schedule" open={deleteModalOpen} onclose={() => { deleteModalOpen = false; }}>
	<p class="text-text-secondary text-sm mb-4">
		Are you sure you want to delete the schedule for <span class="text-white font-medium">{schedule?.domain_name}</span> using <span class="text-white font-medium">{schedule?.workflow_name}</span>? This action cannot be undone.
	</p>
	<div class="flex items-center justify-end gap-3">
		<Button variant="secondary" onclick={() => { deleteModalOpen = false; }}>Cancel</Button>
		<Button variant="destructive" onclick={confirmDelete} loading={isDeleting}>
			{isDeleting ? 'Deleting...' : 'Delete Schedule'}
		</Button>
	</div>
</Modal>
