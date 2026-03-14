<script lang="ts">
	import '../app.css';
	import Toast from '$lib/components/ui/Toast.svelte';
	import NavigationProgress from '$lib/components/ui/NavigationProgress.svelte';

	let { children } = $props();
</script>

<svelte:head>
	<title>Reconova</title>
</svelte:head>

<NavigationProgress />
{@render children()}
<Toast />
