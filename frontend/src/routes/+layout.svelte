<script lang="ts">
	import '../app.css';
	import Toast from '$lib/components/ui/Toast.svelte';

	let { children } = $props();
</script>

<svelte:head>
	<title>Reconova</title>
</svelte:head>

{@render children()}
<Toast />
