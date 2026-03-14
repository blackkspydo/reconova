<script lang="ts">
	import { getAuthStore } from '$lib/stores/auth';
	const auth = getAuthStore();
</script>

<svelte:head>
	<title>Dashboard — Reconova</title>
</svelte:head>

<div>
	<h1 class="text-2xl font-bold text-white">Dashboard</h1>
	<p class="text-text-secondary mt-2">Welcome back, {auth.user?.email}</p>
</div>
