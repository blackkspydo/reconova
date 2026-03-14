<!--
  Button component with variants matching the design system.
-->
<script lang="ts">
	import type { Snippet } from 'svelte';
	import type { HTMLButtonAttributes } from 'svelte/elements';

	type Variant = 'primary' | 'secondary' | 'ghost' | 'destructive';

	interface Props extends HTMLButtonAttributes {
		variant?: Variant;
		loading?: boolean;
		fullWidth?: boolean;
		children: Snippet;
	}

	let { variant = 'primary', loading = false, fullWidth = false, children, disabled, ...rest }: Props = $props();

	const variantClasses: Record<Variant, string> = {
		primary: 'bg-gradient-to-r from-brand to-brand-dark hover:from-brand-dark hover:to-red-800 text-white shadow-[0_4px_20px_rgba(229,62,62,0.3)]',
		secondary: 'bg-[rgba(255,255,255,0.04)] text-white border border-[rgba(255,255,255,0.12)] hover:bg-[rgba(255,255,255,0.08)]',
		ghost: 'bg-transparent text-text-secondary hover:text-white',
		destructive: 'bg-brand text-white hover:bg-brand-light',
	};
</script>

<button
	class="rounded-lg h-12 px-4 text-sm font-semibold transition-all flex items-center justify-center gap-2 disabled:opacity-50 disabled:cursor-not-allowed {variantClasses[variant]}"
	class:w-full={fullWidth}
	disabled={disabled || loading}
	{...rest}
>
	{#if loading}
		<svg class="w-5 h-5 animate-spin" fill="none" viewBox="0 0 24 24">
			<circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
			<path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
		</svg>
	{/if}
	{@render children()}
</button>
