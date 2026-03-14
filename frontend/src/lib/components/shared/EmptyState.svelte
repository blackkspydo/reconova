<script lang="ts">
	import type { Snippet } from 'svelte';

	let { title, description, children }: {
		title: string;
		description?: string;
		children?: Snippet;
	} = $props();
</script>

<div class="flex flex-col items-center justify-center py-12 text-center">
	<p class="text-text-secondary text-sm font-medium">{title}</p>
	{#if description}
		<p class="text-text-muted text-xs mt-1">{description}</p>
	{/if}
	{#if children}
		<div class="mt-4">
			{@render children()}
		</div>
	{/if}
</div>
