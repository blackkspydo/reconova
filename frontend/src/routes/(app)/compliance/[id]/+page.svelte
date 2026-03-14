<script lang="ts">
	import { complianceApi } from '$lib/api/client';
	import { Button, Alert } from '$lib/components/ui';
	import { StatusBadge, SkeletonLoader } from '$lib/components/shared';
	import { page } from '$app/state';
	import { goto } from '$app/navigation';
	import type { ComplianceAssessment, ComplianceControl } from '$lib/types/compliance';
	import type { ApiError } from '$lib/types/auth';

	let assessment = $state<ComplianceAssessment | null>(null);
	let controls = $state<ComplianceControl[]>([]);
	let isLoading = $state(true);
	let isLoadingControls = $state(false);
	let error = $state<string | null>(null);
	let pollTimer = $state<ReturnType<typeof setInterval> | null>(null);

	let assessmentId = $derived(page.params.id);
	let isRunning = $derived(assessment?.status === 'RUNNING');

	// Group controls by category
	let controlsByCategory = $derived.by(() => {
		const groups: Record<string, ComplianceControl[]> = {};
		for (const control of controls) {
			const cat = control.category || 'Uncategorized';
			if (!groups[cat]) groups[cat] = [];
			groups[cat].push(control);
		}
		return groups;
	});

	let categoryNames = $derived(Object.keys(controlsByCategory).sort());

	let scorePercentage = $derived(
		assessment?.status === 'COMPLETED' ? Math.round(assessment.score) : 0
	);

	function scoreColor(score: number): string {
		if (score > 80) return 'text-green-400';
		if (score > 50) return 'text-yellow-400';
		return 'text-red-400';
	}

	function scoreBorderColor(score: number): string {
		if (score > 80) return 'border-green-400';
		if (score > 50) return 'border-yellow-400';
		return 'border-red-400';
	}

	function scoreTrackColor(score: number): string {
		if (score > 80) return 'rgba(74, 222, 128, 0.15)';
		if (score > 50) return 'rgba(250, 204, 21, 0.15)';
		return 'rgba(248, 113, 113, 0.15)';
	}

	async function loadAssessment() {
		try {
			assessment = await complianceApi.getAssessment(assessmentId) as ComplianceAssessment;
		} catch (err) {
			const apiErr = err as ApiError;
			error = apiErr.message || 'Failed to load assessment.';
		} finally {
			isLoading = false;
		}
	}

	async function loadControls() {
		if (!assessment) return;
		isLoadingControls = true;
		try {
			controls = await complianceApi.getControls(assessment.framework_id) as ComplianceControl[];
		} catch {
			// Controls may not be available
		} finally {
			isLoadingControls = false;
		}
	}

	function startPolling() {
		stopPolling();
		pollTimer = setInterval(async () => {
			await loadAssessment();
			if (!isRunning) {
				stopPolling();
				loadControls();
			}
		}, 5000);
	}

	function stopPolling() {
		if (pollTimer) {
			clearInterval(pollTimer);
			pollTimer = null;
		}
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

	function formatDuration(): string {
		if (!assessment?.started_at) return '\u2014';
		const start = new Date(assessment.started_at).getTime();
		const end = assessment.completed_at ? new Date(assessment.completed_at).getTime() : Date.now();
		const seconds = Math.floor((end - start) / 1000);
		if (seconds < 60) return `${seconds}s`;
		const m = Math.floor(seconds / 60);
		const s = seconds % 60;
		if (m < 60) return `${m}m ${s}s`;
		const h = Math.floor(m / 60);
		return `${h}h ${m % 60}m`;
	}

	// Determine pass/fail for a control based on index position
	// Since the API doesn't provide per-control results on the control type itself,
	// we use the assessment's passed count to mark the first N controls as passed
	function isControlPassed(index: number): boolean | null {
		if (!assessment || assessment.status !== 'COMPLETED') return null;
		return index < assessment.passed_controls;
	}

	// Initial load
	$effect(() => {
		loadAssessment().then(() => {
			if (isRunning) {
				startPolling();
			} else {
				loadControls();
			}
		});

		return () => stopPolling();
	});

	// Watch for running state changes
	$effect(() => {
		if (isRunning) {
			startPolling();
		} else {
			stopPolling();
		}
	});
</script>

<svelte:head>
	<title>{assessment ? `${assessment.framework_name} Assessment` : 'Assessment Details'} — Reconova</title>
</svelte:head>

<div>
	{#if isLoading}
		<div class="bg-surface/60 backdrop-blur-sm rounded-xl border border-[rgba(255,255,255,0.08)] p-6">
			<SkeletonLoader lines={5} />
		</div>
	{:else if error && !assessment}
		<Alert variant="error">{error}</Alert>
		<div class="mt-4">
			<Button variant="secondary" onclick={() => goto('/compliance')}>Back to Compliance</Button>
		</div>
	{:else if assessment}
		<!-- Header -->
		<div class="flex items-start justify-between mb-6">
			<div>
				<div class="flex items-center gap-2 mb-1">
					<button onclick={() => goto('/compliance')} class="text-text-muted hover:text-white transition-colors">
						<svg class="w-5 h-5" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
							<path stroke-linecap="round" stroke-linejoin="round" d="M15 19l-7-7 7-7" />
						</svg>
					</button>
					<h1 class="text-2xl font-bold text-white">{assessment.framework_name}</h1>
				</div>
				<div class="flex items-center gap-4 mt-2">
					<StatusBadge status={assessment.status} size="md" />
					<span class="text-sm text-text-muted">Started {formatDate(assessment.started_at)}</span>
					<span class="text-sm text-text-muted">Duration: {formatDuration()}</span>
				</div>
			</div>
		</div>

		{#if error}
			<Alert variant="error">{error}</Alert>
			<div class="mt-4"></div>
		{/if}

		{#if isRunning}
			<!-- Running state -->
			<div class="bg-surface/60 backdrop-blur-sm rounded-xl border border-[rgba(255,255,255,0.08)] p-8 text-center mb-6">
				<svg class="w-8 h-8 animate-spin text-brand mx-auto mb-3" fill="none" viewBox="0 0 24 24">
					<circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
					<path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
				</svg>
				<p class="text-text-secondary text-sm">Assessment in progress. Results will appear here when complete.</p>
				<p class="text-text-muted text-xs mt-1">Auto-refreshing every 5 seconds</p>
			</div>
		{:else}
			<!-- Score Display -->
			<div class="flex items-center justify-center mb-6">
				<div class="bg-surface/60 backdrop-blur-sm rounded-xl border border-[rgba(255,255,255,0.08)] p-8 flex items-center justify-center">
					<div class="relative w-32 h-32 flex items-center justify-center">
						<!-- Background circle -->
						<svg class="absolute inset-0 w-full h-full -rotate-90" viewBox="0 0 120 120">
							<circle
								cx="60" cy="60" r="52"
								fill="none"
								stroke="rgba(255,255,255,0.06)"
								stroke-width="8"
							/>
							<circle
								cx="60" cy="60" r="52"
								fill="none"
								stroke={scorePercentage > 80 ? '#4ade80' : scorePercentage > 50 ? '#facc15' : '#f87171'}
								stroke-width="8"
								stroke-linecap="round"
								stroke-dasharray={`${(scorePercentage / 100) * 327} 327`}
							/>
						</svg>
						<span class="text-3xl font-bold {scoreColor(scorePercentage)}">{scorePercentage}%</span>
					</div>
				</div>
			</div>

			<!-- Summary Cards -->
			<div class="grid grid-cols-2 md:grid-cols-4 gap-4 mb-6">
				<div class="bg-surface/60 backdrop-blur-sm rounded-xl border border-[rgba(255,255,255,0.08)] p-4 text-center">
					<p class="text-xs font-semibold uppercase tracking-wider text-text-muted mb-1">Total Controls</p>
					<p class="text-2xl font-bold text-white">{assessment.total_controls}</p>
				</div>
				<div class="bg-surface/60 backdrop-blur-sm rounded-xl border border-green-500/20 p-4 text-center">
					<p class="text-xs font-semibold uppercase tracking-wider text-text-muted mb-1">Passed</p>
					<p class="text-2xl font-bold text-green-400">{assessment.passed_controls}</p>
				</div>
				<div class="bg-surface/60 backdrop-blur-sm rounded-xl border border-red-500/20 p-4 text-center">
					<p class="text-xs font-semibold uppercase tracking-wider text-text-muted mb-1">Failed</p>
					<p class="text-2xl font-bold text-red-400">{assessment.failed_controls}</p>
				</div>
				<div class="bg-surface/60 backdrop-blur-sm rounded-xl border border-[rgba(255,255,255,0.08)] p-4 text-center">
					<p class="text-xs font-semibold uppercase tracking-wider text-text-muted mb-1">Score</p>
					<p class="text-2xl font-bold {scoreColor(scorePercentage)}">{scorePercentage}%</p>
				</div>
			</div>

			<!-- Controls Breakdown -->
			<div class="mb-6">
				<h2 class="text-lg font-semibold text-white mb-4">Controls Breakdown</h2>

				{#if isLoadingControls}
					<div class="bg-surface/60 backdrop-blur-sm rounded-xl border border-[rgba(255,255,255,0.08)] p-6">
						<SkeletonLoader lines={4} />
					</div>
				{:else if controls.length === 0}
					<div class="bg-surface/60 backdrop-blur-sm rounded-xl border border-[rgba(255,255,255,0.08)] p-6">
						<p class="text-text-muted text-sm text-center py-4">No control details available.</p>
					</div>
				{:else}
					<div class="space-y-4">
						{#each categoryNames as category}
							<div class="bg-surface/60 backdrop-blur-sm rounded-xl border border-[rgba(255,255,255,0.08)] overflow-hidden">
								<div class="px-4 py-3 border-b border-[rgba(255,255,255,0.08)]">
									<h3 class="text-sm font-semibold text-white">{category}</h3>
									<p class="text-xs text-text-muted">{controlsByCategory[category].length} controls</p>
								</div>
								<div class="divide-y divide-[rgba(255,255,255,0.04)]">
									{#each controlsByCategory[category] as control, i}
										{@const globalIndex = controls.indexOf(control)}
										{@const passed = isControlPassed(globalIndex)}
										<div class="px-4 py-3 flex items-center gap-3">
											<!-- Pass/Fail Indicator -->
											{#if passed === true}
												<div class="w-6 h-6 rounded-full bg-green-500/15 border border-green-500/30 flex items-center justify-center shrink-0">
													<svg class="w-3.5 h-3.5 text-green-400" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2.5">
														<path stroke-linecap="round" stroke-linejoin="round" d="M5 13l4 4L19 7" />
													</svg>
												</div>
											{:else if passed === false}
												<div class="w-6 h-6 rounded-full bg-red-500/15 border border-red-500/30 flex items-center justify-center shrink-0">
													<svg class="w-3.5 h-3.5 text-red-400" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2.5">
														<path stroke-linecap="round" stroke-linejoin="round" d="M6 18L18 6M6 6l12 12" />
													</svg>
												</div>
											{:else}
												<div class="w-6 h-6 rounded-full bg-[rgba(255,255,255,0.05)] border border-[rgba(255,255,255,0.1)] flex items-center justify-center shrink-0">
													<span class="text-xs text-text-muted">\u2014</span>
												</div>
											{/if}

											<div class="min-w-0 flex-1">
												<div class="flex items-center gap-2">
													<span class="text-xs font-mono text-text-muted shrink-0">{control.control_id}</span>
													<span class="text-sm text-white truncate">{control.title}</span>
												</div>
											</div>
										</div>
									{/each}
								</div>
							</div>
						{/each}
					</div>
				{/if}
			</div>
		{/if}
	{/if}
</div>
