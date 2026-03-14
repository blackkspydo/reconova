<script lang="ts">
	let { lines = 3, width = 'full' }: { lines?: number; width?: 'full' | 'half' | 'third' } = $props();

	const widthClass: Record<string, string> = {
		full: 'w-full',
		half: 'w-1/2',
		third: 'w-1/3',
	};
</script>

<div class="animate-pulse space-y-3">
	{#each Array(lines) as _, i}
		<div class="h-4 bg-[rgba(255,255,255,0.06)] rounded {i === lines - 1 ? widthClass[width] : 'w-full'}"></div>
	{/each}
</div>
