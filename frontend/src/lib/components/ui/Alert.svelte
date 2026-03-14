<!--
  Inline alert banner for error/warning/info/success states.
-->
<script lang="ts">
	import type { Snippet } from 'svelte';

	type Variant = 'error' | 'warning' | 'info' | 'success';

	interface Props {
		variant?: Variant;
		children: Snippet;
	}

	let { variant = 'error', children }: Props = $props();

	const variantClasses: Record<Variant, string> = {
		error: 'bg-danger/10 border-danger/20 text-danger',
		warning: 'bg-warning/10 border-warning/20 text-warning',
		info: 'bg-info/10 border-info/20 text-info',
		success: 'bg-success/10 border-success/20 text-success',
	};

	const icons: Record<Variant, string> = {
		error: 'M12 9v3.75m9-.75a9 9 0 1 1-18 0 9 9 0 0 1 18 0Zm-9 3.75h.008v.008H12v-.008Z',
		warning: 'M12 9v3.75m-9.303 3.376c-.866 1.5.217 3.374 1.948 3.374h14.71c1.73 0 2.813-1.874 1.948-3.374L13.949 3.378c-.866-1.5-3.032-1.5-3.898 0L2.697 16.126ZM12 15.75h.007v.008H12v-.008Z',
		info: 'm11.25 11.25.041-.02a.75.75 0 0 1 1.063.852l-.708 2.836a.75.75 0 0 0 1.063.853l.041-.021M21 12a9 9 0 1 1-18 0 9 9 0 0 1 18 0Zm-9-3.75h.008v.008H12V8.25Z',
		success: 'M9 12.75 11.25 15 15 9.75M21 12a9 9 0 1 1-18 0 9 9 0 0 1 18 0Z',
	};
</script>

<div class="flex items-start gap-3 p-3 rounded-lg border text-sm {variantClasses[variant]}">
	<svg class="w-5 h-5 shrink-0 mt-0.5" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="1.5">
		<path stroke-linecap="round" stroke-linejoin="round" d={icons[variant]} />
	</svg>
	<div>{@render children()}</div>
</div>
