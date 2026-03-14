<script lang="ts">
	import { navigating } from '$app/state';

	let visible = $state(false);
	let width = $state(0);

	$effect(() => {
		if (navigating.to) {
			visible = true;
			width = 80;
		} else if (visible) {
			width = 100;
			setTimeout(() => {
				visible = false;
				width = 0;
			}, 300);
		}
	});
</script>

{#if visible}
	<div class="fixed top-0 left-0 right-0 z-[60] h-0.5">
		<div
			class="h-full bg-brand transition-all duration-500 ease-out"
			style="width: {width}%"
		></div>
	</div>
{/if}
