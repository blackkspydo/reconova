<script lang="ts">
	import '../app.css';

	let { children } = $props();
</script>

<svelte:head>
	<title>Reconova</title>
</svelte:head>

{@render children()}
