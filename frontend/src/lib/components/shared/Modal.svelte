<script lang="ts">
	import type { Snippet } from 'svelte';

	let { title, open = false, onclose, children }: {
		title: string;
		open: boolean;
		onclose: () => void;
		children: Snippet;
	} = $props();

	function handleBackdrop(e: MouseEvent) {
		if (e.target === e.currentTarget) onclose();
	}

	function handleKeydown(e: KeyboardEvent) {
		if (e.key === 'Escape') onclose();
	}
</script>

<svelte:window onkeydown={handleKeydown} />

{#if open}
	<!-- svelte-ignore a11y_click_events_have_key_events -->
	<!-- svelte-ignore a11y_no_static_element_interactions -->
	<div class="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/60 backdrop-blur-sm" onclick={handleBackdrop}>
		<div class="bg-surface-2 border border-[rgba(255,255,255,0.08)] rounded-xl shadow-2xl w-full max-w-md p-6">
			<div class="flex items-center justify-between mb-4">
				<h3 class="text-lg font-semibold text-white">{title}</h3>
				<button class="text-text-muted hover:text-white transition-colors" onclick={onclose} aria-label="Close">
					<svg class="w-5 h-5" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
						<path stroke-linecap="round" stroke-linejoin="round" d="M6 18 18 6M6 6l12 12" />
					</svg>
				</button>
			</div>
			{@render children()}
		</div>
	</div>
{/if}
