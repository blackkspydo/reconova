<script lang="ts">
	import { scanApi, domainApi, workflowApi, billingApi } from '$lib/api/client';
	import { Button, Alert } from '$lib/components/ui';
	import { SkeletonLoader } from '$lib/components/shared';
	import { goto } from '$app/navigation';
	import type { Workflow, CreditEstimate, ScanJob } from '$lib/types/scans';
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
	let estimate = $state<CreditEstimate | null>(null);
	let isEstimating = $state(false);

	// Derived
	let selectedDomain = $derived(domains.find((d) => d.id === selectedDomainId));
	let selectedWorkflow = $derived(workflows.find((w) => w.id === selectedWorkflowId));
	let systemWorkflows = $derived(workflows.filter((w) => w.is_system));
	let customWorkflows = $derived(workflows.filter((w) => !w.is_system));

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

	// Re-estimate when workflow changes
	$effect(() => {
		if (selectedWorkflowId) {
			loadEstimate();
		} else {
			estimate = null;
		}
	});

	async function handleSubmit() {
		if (!selectedDomainId || !selectedWorkflowId) return;

		isSubmitting = true;
		error = null;
		try {
			const result = await scanApi.create({
				domain_id: selectedDomainId,
				workflow_id: selectedWorkflowId,
			}) as ScanJob;
			goto(`/scans/${result.id}`);
		} catch (err) {
			const apiErr = err as ApiError;
			error = apiErr.message || 'Failed to start scan.';
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
	<title>New Scan — Reconova</title>
</svelte:head>

<div class="max-w-3xl">
	<!-- Header -->
	<div class="mb-8">
		<h1 class="text-2xl font-bold text-white">New Scan</h1>
		<p class="text-text-secondary text-sm mt-1">Configure and launch a security scan</p>
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
		<!-- Section 1: Select Domain -->
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

		<!-- Section 2: Select Workflow -->
		<div class="bg-surface/60 backdrop-blur-sm rounded-xl border border-[rgba(255,255,255,0.08)] p-6 mb-4 {!selectedDomainId ? 'opacity-50 pointer-events-none' : ''}">
			<div class="flex items-center gap-3 mb-4">
				<div class="w-8 h-8 rounded-full bg-brand/20 text-brand flex items-center justify-center text-sm font-bold">2</div>
				<h2 class="text-lg font-semibold text-white">Select Workflow</h2>
			</div>

			{#if workflows.length === 0}
				<p class="text-text-muted text-sm">No workflows available.</p>
			{:else}
				<!-- System Workflows -->
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

				<!-- Custom Workflows -->
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

		<!-- Section 3: Review -->
		<div class="bg-surface/60 backdrop-blur-sm rounded-xl border border-[rgba(255,255,255,0.08)] p-6 mb-6 {!selectedWorkflowId ? 'opacity-50 pointer-events-none' : ''}">
			<div class="flex items-center gap-3 mb-4">
				<div class="w-8 h-8 rounded-full bg-brand/20 text-brand flex items-center justify-center text-sm font-bold">3</div>
				<h2 class="text-lg font-semibold text-white">Review</h2>
			</div>

			{#if selectedDomain && selectedWorkflow}
				<div class="space-y-3 mb-4">
					<div class="flex items-center justify-between">
						<span class="text-sm text-text-muted">Domain</span>
						<span class="text-sm text-text font-medium">{selectedDomain.domain}</span>
					</div>
					<div class="flex items-center justify-between">
						<span class="text-sm text-text-muted">Workflow</span>
						<span class="text-sm text-text font-medium">{selectedWorkflow.name}</span>
					</div>
				</div>

				<!-- Step Breakdown -->
				{#if isEstimating}
					<SkeletonLoader lines={3} />
				{:else if estimate}
					<div class="border-t border-[rgba(255,255,255,0.08)] pt-4 mb-4">
						<p class="text-xs font-semibold uppercase tracking-wider text-text-muted mb-3">Step Breakdown</p>
						<div class="space-y-2">
							{#each estimate.breakdown as step}
								<div class="flex items-center justify-between text-sm">
									<span class="text-text-secondary">{formatCheckType(step.check_type)}</span>
									<span class="text-text-muted">{step.subtotal} credits</span>
								</div>
							{/each}
						</div>
					</div>

					<div class="border-t border-[rgba(255,255,255,0.08)] pt-4 space-y-2">
						<div class="flex items-center justify-between text-sm font-semibold">
							<span class="text-white">Total Cost</span>
							<span class="text-white">{estimate.estimated_cost} credits</span>
						</div>
						<div class="flex items-center justify-between text-sm">
							<span class="text-text-muted">Available Credits</span>
							<span class="text-text-secondary">{estimate.available_credits}</span>
						</div>
					</div>

					{#if !estimate.sufficient}
						<div class="mt-4">
							<Alert variant="warning">
								Insufficient credits. You need {estimate.shortfall} more credits to run this scan.
								<a href="/billing" class="underline font-medium">Purchase credits</a>
							</Alert>
						</div>
					{/if}
				{/if}
			{:else}
				<p class="text-text-muted text-sm">Select a domain and workflow to see the cost estimate.</p>
			{/if}
		</div>

		<!-- Actions -->
		<div class="flex items-center justify-end gap-3">
			<Button variant="secondary" onclick={() => goto('/scans')}>Cancel</Button>
			<Button
				variant="primary"
				onclick={handleSubmit}
				loading={isSubmitting}
				disabled={!selectedDomainId || !selectedWorkflowId || (estimate !== null && !estimate.sufficient)}
			>
				{#if isSubmitting}
					Starting...
				{:else if estimate}
					Start Scan — {estimate.estimated_cost} credits
				{:else}
					Start Scan
				{/if}
			</Button>
		</div>
	{/if}
</div>
