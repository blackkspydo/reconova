<script lang="ts">
	import { page } from '$app/state';
	import Button from '$lib/components/ui/Button.svelte';

	const status = $derived(page.status);
	const message = $derived(page.error?.message ?? 'An error occurred');

	const isNotFound = $derived(status === 404);
	const title = $derived(isNotFound ? 'Page not found' : 'Something went wrong');
	const description = $derived(
		isNotFound
			? "The page you're looking for doesn't exist or has been moved."
			: 'An unexpected error occurred. Please try again.'
	);

	function tryAgain() {
		window.location.reload();
	}
</script>

<div class="flex-1 flex items-center justify-center py-16">
	<div class="max-w-md w-full text-center space-y-8">
		<!-- Shield Icon -->
		<div class="flex justify-center">
			<div class="size-12 text-brand/40">
				<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5">
					<path stroke-linecap="round" stroke-linejoin="round" d="M9 12.75 11.25 15 15 9.75m-3-7.036A11.959 11.959 0 0 1 3.598 6 11.99 11.99 0 0 0 3 9.749c0 5.592 3.824 10.29 9 11.623 5.176-1.332 9-6.03 9-11.622 0-1.31-.21-2.571-.598-3.751h-.152c-3.196 0-6.1-1.248-8.25-3.285Z" />
				</svg>
			</div>
		</div>

		<!-- Error Code -->
		<p class="text-7xl font-bold text-brand/80 tracking-tight">{status}</p>

		<!-- Error Details -->
		<div class="space-y-2">
			<h1 class="text-xl font-semibold text-white">{title}</h1>
			<p class="text-text-secondary text-sm leading-relaxed">{description}</p>
			{#if !isNotFound}
				<p class="text-text-muted text-xs mt-2">{message}</p>
			{/if}
		</div>

		<!-- Actions -->
		<div class="flex items-center justify-center gap-3">
			<a href="/admin/users">
				<Button variant="primary">Go to Admin Dashboard</Button>
			</a>
			<Button variant="secondary" onclick={tryAgain}>Try Again</Button>
		</div>
	</div>
</div>
