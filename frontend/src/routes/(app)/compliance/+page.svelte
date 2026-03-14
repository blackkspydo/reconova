<script lang="ts">
	import { complianceApi } from '$lib/api/client';
	import { Button, Alert } from '$lib/components/ui';
	import { Pagination, StatusBadge, EmptyState, SkeletonLoader } from '$lib/components/shared';
	import { goto } from '$app/navigation';
	import type { ComplianceFramework, ComplianceAssessment } from '$lib/types/compliance';
	import type { Paginated } from '$lib/types/scans';
	import type { ApiError } from '$lib/types/auth';

	let frameworks = $state<ComplianceFramework[]>([]);
	let assessments = $state<ComplianceAssessment[]>([]);
	let currentPage = $state(1);
	let totalPages = $state(1);
	let totalCount = $state(0);
	let pageSize = 10;
	let isLoadingFrameworks = $state(true);
	let isLoadingAssessments = $state(true);
	let error = $state<string | null>(null);
	let runningFrameworkId = $state<string | null>(null);

	async function loadFrameworks() {
		isLoadingFrameworks = true;
		try {
			frameworks = await complianceApi.getFrameworks() as ComplianceFramework[];
		} catch (err) {
			const apiErr = err as ApiError;
			error = apiErr.message || 'Failed to load frameworks.';
		} finally {
			isLoadingFrameworks = false;
		}
	}

	async function loadAssessments() {
		isLoadingAssessments = true;
		try {
			const res = await complianceApi.getAssessments({
				page: currentPage,
				page_size: pageSize,
			}) as Paginated<ComplianceAssessment>;
			assessments = res.data;
			totalPages = res.total_pages;
			totalCount = res.total;
		} catch (err) {
			const apiErr = err as ApiError;
			error = apiErr.message || 'Failed to load assessments.';
		} finally {
			isLoadingAssessments = false;
		}
	}

	async function runAssessment(frameworkId: string) {
		runningFrameworkId = frameworkId;
		error = null;
		try {
			const result = await complianceApi.runAssessment({ framework_id: frameworkId }) as ComplianceAssessment;
			goto(`/compliance/${result.id}`);
		} catch (err) {
			const apiErr = err as ApiError;
			error = apiErr.message || 'Failed to start assessment.';
		} finally {
			runningFrameworkId = null;
		}
	}

	function goToPage(page: number) {
		currentPage = page;
		loadAssessments();
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

	function scoreColor(score: number): string {
		if (score > 80) return 'text-green-400';
		if (score > 50) return 'text-yellow-400';
		return 'text-red-400';
	}

	$effect(() => {
		loadFrameworks();
		loadAssessments();
	});
</script>

<svelte:head>
	<title>Compliance — Reconova</title>
</svelte:head>

<div>
	<!-- Header -->
	<div class="mb-6">
		<h1 class="text-2xl font-bold text-white">Compliance</h1>
		<p class="text-text-secondary text-sm mt-1">Assess your infrastructure against compliance frameworks</p>
	</div>

	{#if error}
		<Alert variant="error">{error}</Alert>
		<div class="mt-4"></div>
	{/if}

	<!-- Frameworks Section -->
	<div class="mb-8">
		<h2 class="text-lg font-semibold text-white mb-4">Frameworks</h2>

		{#if isLoadingFrameworks}
			<div class="bg-surface/60 backdrop-blur-sm rounded-xl border border-[rgba(255,255,255,0.08)] p-6">
				<SkeletonLoader lines={3} />
			</div>
		{:else if frameworks.length === 0}
			<div class="bg-surface/60 backdrop-blur-sm rounded-xl border border-[rgba(255,255,255,0.08)]">
				<EmptyState
					title="No frameworks available"
					description="Compliance frameworks will appear here once configured."
				/>
			</div>
		{:else}
			<div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
				{#each frameworks as framework}
					<div class="bg-surface/60 backdrop-blur-sm rounded-xl border border-[rgba(255,255,255,0.08)] p-5 flex flex-col">
						<div class="flex items-start justify-between mb-2">
							<h3 class="text-base font-semibold text-white">{framework.name}</h3>
							<span class="text-xs px-2 py-0.5 rounded-full bg-[rgba(255,255,255,0.06)] border border-[rgba(255,255,255,0.08)] text-text-secondary font-medium shrink-0 ml-2">
								v{framework.version}
							</span>
						</div>
						<p class="text-sm text-text-muted mb-4 flex-1">{framework.description}</p>
						<div class="flex items-center justify-between mt-auto">
							<span class="text-xs text-text-muted">{framework.control_count} controls</span>
							<div class="flex items-center gap-2">
								<a
									href="/compliance"
									class="text-xs text-text-secondary hover:text-white transition-colors"
									onclick={(e: MouseEvent) => {
										e.preventDefault();
										// Could navigate to a dedicated controls page; for now just a visual link
									}}
								>
									View Controls
								</a>
								<Button
									variant="primary"
									size="sm"
									onclick={() => runAssessment(framework.id)}
									loading={runningFrameworkId === framework.id}
								>
									{runningFrameworkId === framework.id ? 'Starting...' : 'Run Assessment'}
								</Button>
							</div>
						</div>
					</div>
				{/each}
			</div>
		{/if}
	</div>

	<!-- Assessment History Section -->
	<div>
		<h2 class="text-lg font-semibold text-white mb-4">Assessment History</h2>

		{#if isLoadingAssessments}
			<div class="bg-surface/60 backdrop-blur-sm rounded-xl border border-[rgba(255,255,255,0.08)] p-6">
				<SkeletonLoader lines={6} />
			</div>
		{:else if assessments.length === 0}
			<div class="bg-surface/60 backdrop-blur-sm rounded-xl border border-[rgba(255,255,255,0.08)]">
				<EmptyState
					title="No assessments yet"
					description="Run an assessment against a framework to see results here."
				/>
			</div>
		{:else}
			<div class="bg-surface/60 backdrop-blur-sm rounded-xl border border-[rgba(255,255,255,0.08)] overflow-hidden">
				<table class="w-full">
					<thead>
						<tr class="border-b border-[rgba(255,255,255,0.08)]">
							<th class="text-left px-4 py-3 text-xs font-semibold uppercase tracking-wider text-text-muted">Framework</th>
							<th class="text-left px-4 py-3 text-xs font-semibold uppercase tracking-wider text-text-muted">Status</th>
							<th class="text-left px-4 py-3 text-xs font-semibold uppercase tracking-wider text-text-muted">Score</th>
							<th class="text-left px-4 py-3 text-xs font-semibold uppercase tracking-wider text-text-muted">Controls</th>
							<th class="text-left px-4 py-3 text-xs font-semibold uppercase tracking-wider text-text-muted">Date</th>
							<th class="text-right px-4 py-3 text-xs font-semibold uppercase tracking-wider text-text-muted"></th>
						</tr>
					</thead>
					<tbody>
						{#each assessments as assessment}
							<tr
								class="border-b border-[rgba(255,255,255,0.04)] hover:bg-[rgba(255,255,255,0.02)] transition-colors cursor-pointer"
								onclick={() => goto(`/compliance/${assessment.id}`)}
							>
								<td class="px-4 py-3 text-sm text-text font-medium">{assessment.framework_name}</td>
								<td class="px-4 py-3">
									<StatusBadge status={assessment.status} />
								</td>
								<td class="px-4 py-3 text-sm font-semibold {scoreColor(assessment.score)}">
									{assessment.status === 'COMPLETED' ? `${Math.round(assessment.score)}%` : '\u2014'}
								</td>
								<td class="px-4 py-3 text-sm text-text-secondary">
									{assessment.status === 'COMPLETED'
										? `${assessment.passed_controls}/${assessment.total_controls} passed`
										: '\u2014'}
								</td>
								<td class="px-4 py-3 text-sm text-text-secondary">{formatDate(assessment.started_at)}</td>
								<td class="px-4 py-3 text-right">
									<a
										href="/compliance/{assessment.id}"
										class="text-brand text-sm font-medium hover:text-brand-dark transition-colors"
										onclick={(e: MouseEvent) => e.stopPropagation()}
									>View</a>
								</td>
							</tr>
						{/each}
					</tbody>
				</table>
			</div>

			<Pagination {currentPage} {totalPages} onPageChange={goToPage} />
		{/if}
	</div>
</div>
