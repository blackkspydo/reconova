<script lang="ts">
	import { scheduleApi, domainApi, workflowApi, billingApi } from '$lib/api/client';
	import { Button, Alert } from '$lib/components/ui';
	import { SkeletonLoader } from '$lib/components/shared';
	import { goto } from '$app/navigation';
	import type { Workflow, CreditEstimate, ScanSchedule } from '$lib/types/scans';
	import type { Domain } from '$lib/types/domains';
	import type { ApiError } from '$lib/types/auth';

	let domains = $state<Domain[]>([]);
	let workflows = $state<Workflow[]>([]);
	let isLoadingData = $state(true);
	let error = $state<string | null>(null);
	let isSubmitting = $state(false);

	// Form state
	let selectedDomainId = $state('');
	let selectedWorkflowId = $state('');
	let selectedPreset = $state('');
	let customCron = $state('');
	let estimate = $state<CreditEstimate | null>(null);
	let isEstimating = $state(false);

	// Preset options
	const presets = [
		{ label: 'Daily', value: '0 0 * * *', description: 'Every day at midnight' },
		{ label: 'Weekly on Monday', value: '0 0 * * 1', description: 'Every Monday at midnight' },
		{ label: 'Weekly on Friday', value: '0 0 * * 5', description: 'Every Friday at midnight' },
		{ label: 'Bi-weekly', value: '0 0 1,15 * *', description: '1st and 15th of each month' },
		{ label: 'Monthly', value: '0 0 1 * *', description: '1st of each month at midnight' },
		{ label: 'Custom', value: 'custom', description: 'Enter a custom cron expression' },
	];

	// Derived
	let selectedDomain = $derived(domains.find((d) => d.id === selectedDomainId));
	let selectedWorkflow = $derived(workflows.find((w) => w.id === selectedWorkflowId));
	let systemWorkflows = $derived(workflows.filter((w) => w.is_system));
	let customWorkflows = $derived(workflows.filter((w) => !w.is_system));
	let cronExpression = $derived(selectedPreset === 'custom' ? customCron : selectedPreset);
	let cronHuman = $derived(() => {
		const preset = presets.find((p) => p.value === selectedPreset);
		if (!preset) return '';
		if (selectedPreset === 'custom') return customCron || 'Custom expression';
		return preset.description;
	});
	let isFormValid = $derived(
		!!selectedDomainId && !!selectedWorkflowId && !!cronExpression && (selectedPreset !== 'custom' || !!customCron)
	);

	async function loadData() {
		isLoadingData = true;
		error = null;
		try {
			const [domainRes, workflowRes] = await Promise.all([
				domainApi.list() as Promise<Domain[]>,
				workflowApi.list() as Promise<Workflow[]>,
			]);
			domains = domainRes;
			workflows = workflowRes;
		} catch (err) {
			const apiErr = err as ApiError;
			error = apiErr.message || 'Failed to load data.';
		} finally {
			isLoadingData = false;
		}
	}

	async function loadEstimate() {
		if (!selectedWorkflow) {
			estimate = null;
			return;
		}
		isEstimating = true;
		try {
			const steps = selectedWorkflow.steps_json.map((s) => s.check_type);
			estimate = await billingApi.estimateCost({
				workflow_steps: steps,
				domain_count: 1,
			}) as CreditEstimate;
		} catch {
			estimate = null;
		} finally {
			isEstimating = false;
		}
	}

	$effect(() => {
		if (selectedWorkflowId) {
			loadEstimate();
		} else {
			estimate = null;
		}
	});

	async function handleSubmit() {
		if (!isFormValid) return;

		isSubmitting = true;
		error = null;
		try {
			await scheduleApi.create({
				domain_id: selectedDomainId,
				workflow_id: selectedWorkflowId,
				cron_expression: cronExpression,
			}) as ScanSchedule;
			goto('/scans/schedules');
		} catch (err) {
			const apiErr = err as ApiError;
			error = apiErr.message || 'Failed to create schedule.';
		} finally {
			isSubmitting = false;
		}
	}

	function formatCheckType(checkType: string): string {
		return checkType
			.replace(/_/g, ' ')
			.replace(/\b\w/g, (c) => c.toUpperCase());
	}

	$effect(() => {
		loadData();
	});
</script>

<svelte:head>
	<title>New Schedule — Reconova</title>
</svelte:head>

<div class="max-w-3xl">
	<!-- Header -->
	<div class="mb-8">
		<div class="flex items-center gap-2 mb-1">
			<button onclick={() => goto('/scans/schedules')} class="text-text-muted hover:text-white transition-colors">
				<svg class="w-5 h-5" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
					<path stroke-linecap="round" stroke-linejoin="round" d="M15 19l-7-7 7-7" />
				</svg>
			</button>
			<h1 class="text-2xl font-bold text-white">New Schedule</h1>
		</div>
		<p class="text-text-secondary text-sm mt-1">Configure a recurring scan schedule</p>
	</div>

	{#if error}
		<Alert variant="error">{error}</Alert>
		<div class="mt-4"></div>
	{/if}

	{#if isLoadingData}
		<div class="bg-surface/60 backdrop-blur-sm rounded-xl border border-[rgba(255,255,255,0.08)] p-6">
			<SkeletonLoader lines={5} />
		</div>
	{:else}
		<!-- Step 1: Select Domain -->
		<div class="bg-surface/60 backdrop-blur-sm rounded-xl border border-[rgba(255,255,255,0.08)] p-6 mb-4">
			<div class="flex items-center gap-3 mb-4">
				<div class="w-8 h-8 rounded-full bg-brand/20 text-brand flex items-center justify-center text-sm font-bold">1</div>
				<h2 class="text-lg font-semibold text-white">Select Domain</h2>
			</div>

			{#if domains.length === 0}
				<p class="text-text-muted text-sm">No domains available. <a href="/domains" class="text-brand hover:text-brand-dark transition-colors">Add a domain first.</a></p>
			{:else}
				<select
					bind:value={selectedDomainId}
					class="w-full bg-surface border border-[rgba(255,255,255,0.08)] rounded-lg px-3 py-3 text-sm text-text focus:outline-none focus:border-brand/50 transition-colors"
				>
					<option value="">Choose a domain...</option>
					{#each domains as domain}
						<option value={domain.id}>{domain.domain}</option>
					{/each}
				</select>
			{/if}
		</div>

		<!-- Step 2: Select Workflow -->
		<div class="bg-surface/60 backdrop-blur-sm rounded-xl border border-[rgba(255,255,255,0.08)] p-6 mb-4 {!selectedDomainId ? 'opacity-50 pointer-events-none' : ''}">
			<div class="flex items-center gap-3 mb-4">
				<div class="w-8 h-8 rounded-full bg-brand/20 text-brand flex items-center justify-center text-sm font-bold">2</div>
				<h2 class="text-lg font-semibold text-white">Select Workflow</h2>
			</div>

			{#if workflows.length === 0}
				<p class="text-text-muted text-sm">No workflows available.</p>
			{:else}
				{#if systemWorkflows.length > 0}
					<div class="mb-4">
						<p class="text-xs font-semibold uppercase tracking-wider text-text-muted mb-3">System Workflows</p>
						<div class="space-y-2">
							{#each systemWorkflows as wf}
								<label
									class="flex items-start gap-3 p-3 rounded-lg border cursor-pointer transition-all {selectedWorkflowId === wf.id ? 'border-brand/50 bg-brand/5' : 'border-[rgba(255,255,255,0.08)] hover:border-[rgba(255,255,255,0.16)]'}"
								>
									<input
										type="radio"
										name="workflow"
										value={wf.id}
										bind:group={selectedWorkflowId}
										class="mt-0.5 accent-brand"
									/>
									<div class="flex-1">
										<p class="text-sm font-medium text-white">{wf.name}</p>
										{#if wf.description}
											<p class="text-xs text-text-muted mt-0.5">{wf.description}</p>
										{/if}
										<div class="flex flex-wrap gap-1.5 mt-2">
											{#each wf.steps_json as step}
												<span class="text-xs px-2 py-0.5 rounded bg-[rgba(255,255,255,0.05)] text-text-secondary">
													{formatCheckType(step.check_type)}
												</span>
											{/each}
										</div>
									</div>
								</label>
							{/each}
						</div>
					</div>
				{/if}

				{#if customWorkflows.length > 0}
					<div>
						<p class="text-xs font-semibold uppercase tracking-wider text-text-muted mb-3">Custom Workflows</p>
						<div class="space-y-2">
							{#each customWorkflows as wf}
								<label
									class="flex items-start gap-3 p-3 rounded-lg border cursor-pointer transition-all {selectedWorkflowId === wf.id ? 'border-brand/50 bg-brand/5' : 'border-[rgba(255,255,255,0.08)] hover:border-[rgba(255,255,255,0.16)]'}"
								>
									<input
										type="radio"
										name="workflow"
										value={wf.id}
										bind:group={selectedWorkflowId}
										class="mt-0.5 accent-brand"
									/>
									<div class="flex-1">
										<p class="text-sm font-medium text-white">{wf.name}</p>
										{#if wf.description}
											<p class="text-xs text-text-muted mt-0.5">{wf.description}</p>
										{/if}
										<div class="flex flex-wrap gap-1.5 mt-2">
											{#each wf.steps_json as step}
												<span class="text-xs px-2 py-0.5 rounded bg-[rgba(255,255,255,0.05)] text-text-secondary">
													{formatCheckType(step.check_type)}
												</span>
											{/each}
										</div>
									</div>
								</label>
							{/each}
						</div>
					</div>
				{/if}
			{/if}
		</div>

		<!-- Step 3: Configure Schedule -->
		<div class="bg-surface/60 backdrop-blur-sm rounded-xl border border-[rgba(255,255,255,0.08)] p-6 mb-4 {!selectedWorkflowId ? 'opacity-50 pointer-events-none' : ''}">
			<div class="flex items-center gap-3 mb-4">
				<div class="w-8 h-8 rounded-full bg-brand/20 text-brand flex items-center justify-center text-sm font-bold">3</div>
				<h2 class="text-lg font-semibold text-white">Configure Schedule</h2>
			</div>

			<div class="space-y-2">
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
				<div class="mt-4">
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
		</div>

		<!-- Step 4: Review -->
		<div class="bg-surface/60 backdrop-blur-sm rounded-xl border border-[rgba(255,255,255,0.08)] p-6 mb-6 {!isFormValid ? 'opacity-50 pointer-events-none' : ''}">
			<div class="flex items-center gap-3 mb-4">
				<div class="w-8 h-8 rounded-full bg-brand/20 text-brand flex items-center justify-center text-sm font-bold">4</div>
				<h2 class="text-lg font-semibold text-white">Review</h2>
			</div>

			{#if selectedDomain && selectedWorkflow && cronExpression}
				<div class="space-y-3 mb-4">
					<div class="flex items-center justify-between">
						<span class="text-sm text-text-muted">Domain</span>
						<span class="text-sm text-text font-medium">{selectedDomain.domain}</span>
					</div>
					<div class="flex items-center justify-between">
						<span class="text-sm text-text-muted">Workflow</span>
						<span class="text-sm text-text font-medium">{selectedWorkflow.name}</span>
					</div>
					<div class="flex items-center justify-between">
						<span class="text-sm text-text-muted">Schedule</span>
						<span class="text-sm text-text font-medium">{cronHuman()}</span>
					</div>
					<div class="flex items-center justify-between">
						<span class="text-sm text-text-muted">Cron Expression</span>
						<span class="text-sm text-text font-mono">{cronExpression}</span>
					</div>
				</div>

				{#if isEstimating}
					<SkeletonLoader lines={2} />
				{:else if estimate}
					<div class="border-t border-[rgba(255,255,255,0.08)] pt-4">
						<div class="flex items-center justify-between text-sm font-semibold">
							<span class="text-white">Estimated Credits per Run</span>
							<span class="text-white">{estimate.estimated_cost} credits</span>
						</div>
					</div>
				{/if}
			{:else}
				<p class="text-text-muted text-sm">Complete all steps above to review your schedule.</p>
			{/if}
		</div>

		<!-- Actions -->
		<div class="flex items-center justify-end gap-3">
			<Button variant="secondary" onclick={() => goto('/scans/schedules')}>Cancel</Button>
			<Button
				variant="primary"
				onclick={handleSubmit}
				loading={isSubmitting}
				disabled={!isFormValid}
			>
				{isSubmitting ? 'Creating...' : 'Create Schedule'}
			</Button>
		</div>
	{/if}
</div>
