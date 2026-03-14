<script lang="ts">
	import { workflowApi } from '$lib/api/client';
	import type { Workflow, WorkflowTemplate, WorkflowStepDefinition } from '$lib/types/scans';
	import { Button, Alert } from '$lib/components/ui';
	import { EmptyState, SkeletonLoader } from '$lib/components/shared';
	import { Modal } from '$lib/components/shared';
	import { goto } from '$app/navigation';
	import type { ApiError } from '$lib/types/auth';

	let templates = $state<WorkflowTemplate[]>([]);
	let customWorkflows = $state<Workflow[]>([]);
	let isLoading = $state(true);
	let error = $state<string | null>(null);

	let deleteTarget = $state<Workflow | null>(null);
	let isDeleting = $state(false);
	let deleteError = $state<string | null>(null);

	const MAX_CUSTOM_WORKFLOWS = 20;

	async function loadWorkflows() {
		isLoading = true;
		error = null;
		try {
			const res = (await workflowApi.list()) as { templates: WorkflowTemplate[]; workflows: Workflow[] };
			templates = res.templates ?? [];
			customWorkflows = res.workflows ?? [];
		} catch (err) {
			const apiErr = err as ApiError;
			error = apiErr.message || 'Failed to load workflows.';
		} finally {
			isLoading = false;
		}
	}

	$effect(() => {
		loadWorkflows();
	});

	function formatStepName(checkType: string): string {
		return checkType
			.replace(/_/g, ' ')
			.replace(/\b\w/g, (c) => c.toUpperCase());
	}

	function formatStepPipeline(steps: WorkflowStepDefinition[]): string {
		return steps.map((s) => s.check_type).join(' → ');
	}

	async function confirmDelete() {
		if (!deleteTarget) return;
		isDeleting = true;
		deleteError = null;
		try {
			await workflowApi.delete(deleteTarget.id);
			customWorkflows = customWorkflows.filter((w) => w.id !== deleteTarget!.id);
			deleteTarget = null;
		} catch (err) {
			const apiErr = err as ApiError;
			deleteError = apiErr.message || 'Failed to delete workflow.';
		} finally {
			isDeleting = false;
		}
	}
</script>

<svelte:head>
	<title>Workflows — Reconova</title>
</svelte:head>

<div>
	<div class="flex items-center justify-between mb-8">
		<div>
			<h1 class="text-2xl font-bold text-white">Workflows</h1>
			<p class="text-text-secondary text-sm mt-1">Manage scan workflow templates and custom pipelines</p>
		</div>
	</div>

	{#if error}
		<Alert variant="error">{error}</Alert>
		<div class="mt-6"></div>
	{/if}

	{#if isLoading}
		<div class="space-y-6">
			<div class="bg-surface/60 backdrop-blur-xl rounded-xl border border-[rgba(255,255,255,0.08)] p-6">
				<SkeletonLoader lines={4} />
			</div>
			<div class="bg-surface/60 backdrop-blur-xl rounded-xl border border-[rgba(255,255,255,0.08)] p-6">
				<SkeletonLoader lines={5} />
			</div>
		</div>
	{:else}
		<!-- System Templates -->
		<section class="mb-10">
			<h2 class="text-lg font-semibold text-white mb-4">System Templates</h2>
			{#if templates.length === 0}
				<div class="bg-surface/60 backdrop-blur-xl rounded-xl border border-[rgba(255,255,255,0.08)] p-6">
					<EmptyState title="No system templates available" description="System templates will appear here once configured." />
				</div>
			{:else}
				<div class="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
					{#each templates as template}
						<div class="bg-surface/60 backdrop-blur-xl rounded-xl border border-[rgba(255,255,255,0.08)] p-5 flex flex-col justify-between hover:border-[rgba(255,255,255,0.16)] transition-colors">
							<div>
								<h3 class="text-white font-semibold text-sm">{template.name}</h3>
								{#if template.description}
									<p class="text-text-muted text-xs mt-1 line-clamp-2">{template.description}</p>
								{/if}
								<div class="mt-3 flex flex-wrap items-center gap-1">
									{#each template.steps_json as step, i}
										<span class="text-[11px] text-text-secondary bg-[rgba(255,255,255,0.06)] px-2 py-0.5 rounded-md font-mono whitespace-nowrap">
											{step.check_type}
										</span>
										{#if i < template.steps_json.length - 1}
											<span class="text-text-muted text-[10px]">→</span>
										{/if}
									{/each}
								</div>
							</div>
							<div class="mt-4">
								<Button variant="secondary" onclick={() => goto(`/scans/new?workflow=${template.id}`)}>
									Use This
								</Button>
							</div>
						</div>
					{/each}
				</div>
			{/if}
		</section>

		<!-- Custom Workflows -->
		<section>
			<div class="flex items-center justify-between mb-4">
				<h2 class="text-lg font-semibold text-white">
					Custom Workflows
					<span class="text-text-muted text-sm font-normal ml-1">({customWorkflows.length} / {MAX_CUSTOM_WORKFLOWS})</span>
				</h2>
				<Button
					variant="primary"
					onclick={() => goto('/scans/workflows/new')}
					disabled={customWorkflows.length >= MAX_CUSTOM_WORKFLOWS}
				>
					Create Workflow
				</Button>
			</div>

			{#if customWorkflows.length === 0}
				<div class="bg-surface/60 backdrop-blur-xl rounded-xl border border-[rgba(255,255,255,0.08)] p-6">
					<EmptyState
						title="No custom workflows yet"
						description="Create your own scan workflow to define exactly which steps to run."
					>
						<Button variant="secondary" onclick={() => goto('/scans/workflows/new')}>
							Create Your First Workflow
						</Button>
					</EmptyState>
				</div>
			{:else}
				<div class="bg-surface/60 backdrop-blur-xl rounded-xl border border-[rgba(255,255,255,0.08)] overflow-hidden">
					<table class="w-full">
						<thead>
							<tr class="border-b border-[rgba(255,255,255,0.08)]">
								<th class="text-left px-4 py-3 text-xs font-semibold uppercase tracking-wider text-text-muted">Name</th>
								<th class="text-left px-4 py-3 text-xs font-semibold uppercase tracking-wider text-text-muted">Steps</th>
								<th class="text-left px-4 py-3 text-xs font-semibold uppercase tracking-wider text-text-muted">Created</th>
								<th class="text-right px-4 py-3 text-xs font-semibold uppercase tracking-wider text-text-muted">Actions</th>
							</tr>
						</thead>
						<tbody>
							{#each customWorkflows as workflow}
								<tr class="border-b border-[rgba(255,255,255,0.04)] hover:bg-[rgba(255,255,255,0.02)] transition-colors">
									<td class="px-4 py-3 text-sm text-text font-medium">{workflow.name}</td>
									<td class="px-4 py-3 text-sm text-text-secondary">{workflow.steps_json.length} step{workflow.steps_json.length !== 1 ? 's' : ''}</td>
									<td class="px-4 py-3 text-sm text-text-secondary">{new Date(workflow.created_at).toLocaleDateString()}</td>
									<td class="px-4 py-3 text-right">
										<div class="flex items-center justify-end gap-2">
											<button
												class="text-brand text-sm font-medium hover:text-brand-dark transition-colors"
												onclick={() => goto(`/scans/workflows/${workflow.id}/edit`)}
											>
												Edit
											</button>
											<button
												class="text-danger text-sm font-medium hover:text-red-400 transition-colors"
												onclick={() => { deleteTarget = workflow; deleteError = null; }}
											>
												Delete
											</button>
										</div>
									</td>
								</tr>
							{/each}
						</tbody>
					</table>
				</div>
			{/if}
		</section>
	{/if}
</div>

<!-- Delete Confirmation Modal -->
<Modal title="Delete Workflow" open={deleteTarget !== null} onclose={() => { deleteTarget = null; deleteError = null; }}>
	<div class="space-y-4">
		<p class="text-text-secondary text-sm">
			Are you sure you want to delete <span class="text-white font-medium">{deleteTarget?.name}</span>? This action cannot be undone.
		</p>

		{#if deleteError}
			<Alert variant="error">{deleteError}</Alert>
		{/if}

		<div class="flex justify-end gap-3">
			<Button variant="secondary" onclick={() => { deleteTarget = null; deleteError = null; }}>
				Cancel
			</Button>
			<Button variant="destructive" loading={isDeleting} onclick={confirmDelete}>
				{isDeleting ? 'Deleting...' : 'Delete'}
			</Button>
		</div>
	</div>
</Modal>
