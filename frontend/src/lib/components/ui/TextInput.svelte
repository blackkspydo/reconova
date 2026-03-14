<!--
  Styled text input matching the dark glassmorphism design.
-->
<script lang="ts">
	import type { HTMLInputAttributes } from 'svelte/elements';

	interface Props extends HTMLInputAttributes {
		label: string;
		id: string;
		value?: string;
		error?: string | null;
	}

	let { label, id, value = $bindable(''), error = null, ...rest }: Props = $props();
</script>

<div class="flex flex-col gap-2">
	<label class="text-white text-sm font-medium" for={id}>{label}</label>
	<input
		{id}
		bind:value
		class="w-full rounded-lg border border-[rgba(255,255,255,0.08)] bg-surface text-white placeholder-text-muted focus:ring-1 focus:ring-brand focus:border-brand h-12 px-4 text-sm transition-colors outline-none"
		class:border-danger={error}
		{...rest}
	/>
	{#if error}
		<p class="text-danger text-xs mt-0.5">{error}</p>
	{/if}
</div>
