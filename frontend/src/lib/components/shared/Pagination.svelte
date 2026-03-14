<script lang="ts">
	import { Button } from '$lib/components/ui';

	let { currentPage, totalPages, onPageChange }: {
		currentPage: number;
		totalPages: number;
		onPageChange: (page: number) => void;
	} = $props();
</script>

{#if totalPages > 1}
	<div class="flex items-center justify-between mt-4">
		<p class="text-sm text-text-muted">Page {currentPage} of {totalPages}</p>
		<div class="flex gap-2">
			<Button variant="secondary" disabled={currentPage <= 1} onclick={() => onPageChange(currentPage - 1)}>
				Previous
			</Button>
			<Button variant="secondary" disabled={currentPage >= totalPages} onclick={() => onPageChange(currentPage + 1)}>
				Next
			</Button>
		</div>
	</div>
{/if}
