<script lang="ts">
	import { workflowApi } from '$lib/api/client';
	import type { WorkflowStepDefinition } from '$lib/types/scans';
	import { Button, Alert, TextInput } from '$lib/components/ui';
	import { goto } from '$app/navigation';
	import type { ApiError } from '$lib/types/auth';

	const MAX_STEPS = 15;

	interface StepOption {
		check_type: string;
		label: string;
		tierGated: boolean;
	}

	const AVAILABLE_STEPS: StepOption[] = [
		{ check_type: 'subdomain_enum', label: 'Subdomain Enumeration', tierGated: false },
		{ check_type: 'port_scan', label: 'Port Scan', tierGated: false },
		{ check_type: 'tech_detect', label: 'Technology Detection', tierGated: false },
		{ check_type: 'screenshot', label: 'Screenshot', tierGated: false },
		{ check_type: 'vuln_scan', label: 'Vulnerability Scan', tierGated: false },
		{ check_type: 'compliance_check', label: 'Compliance Check', tierGated: true },
		{ check_type: 'shodan_lookup', label: 'Shodan Lookup', tierGated: true },
		{ check_type: 'securitytrails', label: 'SecurityTrails', tierGated: true },
		{ check_type: 'censys_lookup', label: 'Censys Lookup', tierGated: true },
		{ check_type: 'custom_connector', label: 'Custom Connector', tierGated: true },
	];

	let workflowName = $state('');
	let selectedSteps = $state<WorkflowStepDefinition[]>([]);
	let isSubmitting = $state(false);
	let error = $state<string | null>(null);
	let nameError = $state<string | null>(null);

	let dragIndex = $state<number | null>(null);
	let dragOverIndex = $state<number | null>(null);

	function isSelected(checkType: string): boolean {
		return selectedSteps.some((s) => s.check_type === checkType);
	}

	function toggleStep(step: StepOption) {
		if (isSelected(step.check_type)) {
			selectedSteps = selectedSteps.filter((s) => s.check_type !== step.check_type);
		} else {
			if (selectedSteps.length >= MAX_STEPS) return;
			selectedSteps = [...selectedSteps, { check_type: step.check_type }];
		}
	}

	function removeStep(index: number) {
		selectedSteps = selectedSteps.filter((_, i) => i !== index);
	}

	function handleDragStart(index: number) {
		dragIndex = index;
	}

	function handleDragOver(e: DragEvent, index: number) {
		e.preventDefault();
		dragOverIndex = index;
	}

	function handleDrop(index: number) {
		if (dragIndex === null || dragIndex === index) {
			dragIndex = null;
			dragOverIndex = null;
			return;
		}
		const reordered = [...selectedSteps];
		const [moved] = reordered.splice(dragIndex, 1);
		reordered.splice(index, 0, moved);
		selectedSteps = reordered;
		dragIndex = null;
		dragOverIndex = null;
	}

	function handleDragEnd() {
		dragIndex = null;
		dragOverIndex = null;
	}

	function getStepLabel(checkType: string): string {
		return AVAILABLE_STEPS.find((s) => s.check_type === checkType)?.label ?? checkType;
	}

	async function handleCreate(e: Event) {
		e.preventDefault();
		nameError = null;
		error = null;

		const trimmed = workflowName.trim();
		if (!trimmed) {
			nameError = 'Workflow name is required.';
			return;
		}
		if (trimmed.length < 3) {
			nameError = 'Name must be at least 3 characters.';
			return;
		}
		if (selectedSteps.length === 0) {
			error = 'Select at least one step for the workflow.';
			return;
		}

		isSubmitting = true;
		try {
			await workflowApi.create({
				name: trimmed,
				steps_json: selectedSteps,
			});
			goto('/scans/workflows');
		} catch (err) {
			const apiErr = err as ApiError;
			error = apiErr.message || 'Failed to create workflow.';
		} finally {
			isSubmitting = false;
		}
	}
</script>

<svelte:head>
	<title>New Workflow — Reconova</title>
</svelte:head>

<div class="max-w-3xl">
	<div class="mb-8">
		<h1 class="text-2xl font-bold text-white">New Workflow</h1>
		<p class="text-text-secondary text-sm mt-1">Define a custom scan pipeline with the steps you need</p>
	</div>

	{#if error}
		<Alert variant="error">{error}</Alert>
		<div class="mt-6"></div>
	{/if}

	<form onsubmit={handleCreate} class="space-y-8">
		<!-- Name Input -->
		<div class="bg-surface/60 backdrop-blur-xl rounded-xl border border-[rgba(255,255,255,0.08)] p-6">
			<TextInput
				label="Workflow Name"
				id="workflow-name"
				bind:value={workflowName}
				placeholder="e.g. Full Recon Pipeline"
				error={nameError}
			/>
		</div>

		<!-- Available Steps -->
		<div class="bg-surface/60 backdrop-blur-xl rounded-xl border border-[rgba(255,255,255,0.08)] p-6">
			<h2 class="text-lg font-semibold text-white mb-4">Available Steps</h2>
			<p class="text-text-muted text-xs mb-4">Select the scan steps to include in your workflow. Tier-gated steps require a higher plan.</p>

			<div class="grid gap-2 sm:grid-cols-2">
				{#each AVAILABLE_STEPS as step}
					<label
						class="flex items-center gap-3 p-3 rounded-lg border cursor-pointer transition-all
							{isSelected(step.check_type)
								? 'border-brand/50 bg-brand/5'
								: 'border-[rgba(255,255,255,0.06)] hover:border-[rgba(255,255,255,0.14)] bg-[rgba(255,255,255,0.02)]'}
							{selectedSteps.length >= MAX_STEPS && !isSelected(step.check_type) ? 'opacity-50 cursor-not-allowed' : ''}"
					>
						<input
							type="checkbox"
							checked={isSelected(step.check_type)}
							onchange={() => toggleStep(step)}
							disabled={selectedSteps.length >= MAX_STEPS && !isSelected(step.check_type)}
							class="w-4 h-4 rounded border-[rgba(255,255,255,0.2)] bg-transparent text-brand focus:ring-brand focus:ring-offset-0 accent-[#e53e3e]"
						/>
						<span class="text-sm text-text flex-1">{step.label}</span>
						{#if step.tierGated}
							<svg class="w-4 h-4 text-text-muted flex-shrink-0" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2" aria-label="Tier-gated">
								<path stroke-linecap="round" stroke-linejoin="round" d="M16.5 10.5V6.75a4.5 4.5 0 1 0-9 0v3.75m-.75 11.25h10.5a2.25 2.25 0 0 0 2.25-2.25v-6.75a2.25 2.25 0 0 0-2.25-2.25H6.75a2.25 2.25 0 0 0-2.25 2.25v6.75a2.25 2.25 0 0 0 2.25 2.25Z" />
							</svg>
						{/if}
					</label>
				{/each}
			</div>
		</div>

		<!-- Selected Steps (Ordered) -->
		<div class="bg-surface/60 backdrop-blur-xl rounded-xl border border-[rgba(255,255,255,0.08)] p-6">
			<div class="flex items-center justify-between mb-4">
				<h2 class="text-lg font-semibold text-white">Selected Steps</h2>
				<span class="text-xs text-text-muted font-mono">{selectedSteps.length} / {MAX_STEPS} max</span>
			</div>

			{#if selectedSteps.length === 0}
				<p class="text-text-muted text-sm py-4 text-center">No steps selected. Check steps above to add them here.</p>
			{:else}
				<div class="space-y-2">
					{#each selectedSteps as step, i}
						<!-- svelte-ignore a11y_no_static_element_interactions -->
						<div
							class="flex items-center gap-3 p-3 rounded-lg border transition-all
								{dragOverIndex === i ? 'border-brand/50 bg-brand/5' : 'border-[rgba(255,255,255,0.06)] bg-[rgba(255,255,255,0.02)]'}"
							draggable="true"
							ondragstart={() => handleDragStart(i)}
							ondragover={(e) => handleDragOver(e, i)}
							ondrop={() => handleDrop(i)}
							ondragend={handleDragEnd}
						>
							<span
								class="text-text-muted cursor-grab active:cursor-grabbing select-none text-lg leading-none"
								aria-label="Drag to reorder"
							>
								&#8801;
							</span>
							<span class="text-xs text-text-muted font-mono w-6 text-center">{i + 1}</span>
							<span class="text-sm text-text flex-1">{getStepLabel(step.check_type)}</span>
							<span class="text-[11px] text-text-muted font-mono bg-[rgba(255,255,255,0.06)] px-2 py-0.5 rounded">{step.check_type}</span>
							<button
								type="button"
								class="text-text-muted hover:text-danger transition-colors text-lg leading-none"
								onclick={() => removeStep(i)}
								aria-label="Remove step"
							>
								&#10005;
							</button>
						</div>
					{/each}
				</div>
			{/if}
		</div>

		<!-- Actions -->
		<div class="flex items-center justify-end gap-3">
			<Button variant="secondary" type="button" onclick={() => goto('/scans/workflows')}>
				Cancel
			</Button>
			<Button
				variant="primary"
				type="submit"
				loading={isSubmitting}
				disabled={selectedSteps.length === 0 || !workflowName.trim()}
			>
				{isSubmitting ? 'Creating...' : 'Create Workflow'}
			</Button>
		</div>
	</form>
</div>
