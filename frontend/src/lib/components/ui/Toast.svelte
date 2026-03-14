<script lang="ts">
	import { getToastStore } from '$lib/stores/toast';

	const store = getToastStore();

	function variantClasses(variant: string): string {
		switch (variant) {
			case 'success': return 'bg-success/90 text-white';
			case 'error': return 'bg-danger/90 text-white';
			case 'warning': return 'bg-warning/90 text-white';
			case 'info': return 'bg-info/90 text-white';
			default: return 'bg-surface-2 text-white';
		}
	}
</script>

{#if store.toasts.length > 0}
	<div class="fixed top-4 right-4 z-50 flex flex-col gap-2 max-w-sm">
		{#each store.toasts as toast (toast.id)}
			<div class="rounded-lg px-4 py-3 shadow-lg text-sm font-medium flex items-center gap-3 {variantClasses(toast.variant)} animate-in">
				<span class="flex-1">{toast.message}</span>
				<button
					class="opacity-70 hover:opacity-100 transition-opacity"
					onclick={() => store.remove(toast.id)}
					aria-label="Dismiss"
				>
					<svg class="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
						<path stroke-linecap="round" stroke-linejoin="round" d="M6 18 18 6M6 6l12 12" />
					</svg>
				</button>
			</div>
		{/each}
	</div>
{/if}
