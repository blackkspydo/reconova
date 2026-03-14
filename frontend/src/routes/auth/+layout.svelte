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

<div class="bg-bg min-h-screen flex flex-col items-center justify-center p-4 relative overflow-hidden text-white">
	{@render children()}

	<footer class="text-center text-xs text-text-muted mt-8">
		&copy; 2026 Reconova. All rights reserved.
	</footer>
</div>
