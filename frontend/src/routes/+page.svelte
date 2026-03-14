<script lang="ts">
	import { goto } from '$app/navigation';
	import { getAuthStore } from '$lib/stores/auth';

	const auth = getAuthStore();

	$effect(() => {
		auth.loadUser().then(() => {
			if (auth.isAuthenticated) {
				goto('/dashboard');
			} else {
				goto('/auth/login');
			}
		});
	});
</script>
