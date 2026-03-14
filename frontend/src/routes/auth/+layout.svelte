<script lang="ts">
	let { children } = $props();
</script>

<div class="bg-bg min-h-screen flex flex-col items-center justify-center p-4 relative overflow-hidden text-white">
	{@render children()}

	<footer class="text-center text-xs text-text-muted mt-8">
		&copy; 2026 Reconova. All rights reserved.
	</footer>
</div>
