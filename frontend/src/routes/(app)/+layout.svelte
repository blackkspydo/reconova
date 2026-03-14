<script lang="ts">
	import { goto } from '$app/navigation';
	import { getAuthStore } from '$lib/stores/auth';
	import { authApi } from '$lib/api/client';
	import Sidebar from '$lib/components/layout/Sidebar.svelte';
	import Header from '$lib/components/layout/Header.svelte';

	let { children } = $props();
	const auth = getAuthStore();

	let initialized = $state(false);

	$effect(() => {
		if (!initialized) {
			initialized = true;
			auth.loadUser();
		}
	});

	$effect(() => {
		if (!auth.isLoading && !auth.isAuthenticated && initialized) {
			goto('/auth/login');
		}
	});

	// Proactive token refresh every 4 minutes
	$effect(() => {
		if (auth.isAuthenticated) {
			const interval = setInterval(() => {
				authApi.refresh().catch(() => {
					// Refresh failed - will be handled by 401 interceptor on next request
				});
			}, 4 * 60 * 1000);
			return () => clearInterval(interval);
		}
	});
</script>

{#if auth.isLoading}
	<div class="min-h-screen bg-bg flex items-center justify-center">
		<svg class="w-8 h-8 animate-spin text-brand" fill="none" viewBox="0 0 24 24">
			<circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
			<path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
		</svg>
	</div>
{:else if auth.isAuthenticated}
	<div class="flex min-h-screen bg-bg text-white">
		<Sidebar isAdmin={auth.user?.role === 'SUPER_ADMIN'} />
		<div class="flex-1 flex flex-col min-h-screen">
			<Header />
			<main class="flex-1 p-6">
				{@render children()}
			</main>
		</div>
	</div>
{/if}
