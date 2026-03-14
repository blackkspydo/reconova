<!--
  Glass card wrapper for auth pages.
  Provides the frosted glass panel with red ambient glow.
-->
<script lang="ts">
	import type { Snippet } from 'svelte';

	interface Props {
		children: Snippet;
	}

	let { children }: Props = $props();
</script>

<div class="bg-[rgba(255,255,255,0.03)] backdrop-blur-2xl rounded-[16px] border border-[rgba(255,255,255,0.08)] shadow-[0_0_30px_rgba(229,62,62,0.05)] p-8 flex flex-col gap-8">
	{@render children()}
</div>
