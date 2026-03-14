<script lang="ts">
	import { goto } from '$app/navigation';
	import { getAuthStore } from '$lib/stores/auth';

	let { children } = $props();

	const auth = getAuthStore();

	let hasLoadedUser = false;

	$effect(() => {
		if (!hasLoadedUser) {
			hasLoadedUser = true;
			auth.loadUser();
		}
	});

	$effect(() => {
		if (!auth.isLoading && auth.isAuthenticated) {
			goto('/dashboard');
		}
	});
</script>

{#if auth.isLoading}
	<div class="bg-bg min-h-screen flex items-center justify-center">
		<svg class="w-8 h-8 animate-spin text-brand" fill="none" viewBox="0 0 24 24">
			<circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
			<path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
		</svg>
	</div>
{:else}
	<div class="bg-bg min-h-screen flex flex-col items-center justify-center p-4 relative overflow-hidden text-white">
		{@render children()}

		<footer class="text-center text-xs text-text-muted mt-8">
			&copy; 2026 Reconova. All rights reserved.
		</footer>
	</div>
{/if}
