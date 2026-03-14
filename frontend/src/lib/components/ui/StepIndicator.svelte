<script lang="ts">
	interface Props {
		currentStep: number;
		totalSteps: number;
	}

	let { currentStep, totalSteps }: Props = $props();
</script>

<div class="flex items-center justify-center gap-2">
	{#each Array(totalSteps) as _, i}
		{#if i > 0}
			<div class="w-8 h-0.5 {i < currentStep ? 'bg-brand' : 'bg-[rgba(255,255,255,0.12)]'}"></div>
		{/if}
		<div
			class="w-3 h-3 rounded-full border-2 transition-colors {i < currentStep ? 'bg-brand border-brand' : 'border-[rgba(255,255,255,0.2)] bg-transparent'}"
		></div>
	{/each}
</div>
