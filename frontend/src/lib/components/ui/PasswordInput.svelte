<!--
  Password input with show/hide toggle.
-->
<script lang="ts">
	interface Props {
		label: string;
		id: string;
		value?: string;
		placeholder?: string;
		error?: string | null;
		labelRight?: import('svelte').Snippet;
		onblur?: (e: FocusEvent) => void;
	}

	let { label, id, value = $bindable(''), placeholder = '••••••••', error = null, labelRight, onblur }: Props = $props();

	let showPassword = $state(false);
</script>

<div class="flex flex-col gap-2">
	<div class="flex justify-between items-center">
		<label class="text-white text-sm font-medium" for={id}>{label}</label>
		{#if labelRight}
			{@render labelRight()}
		{/if}
	</div>
	<div class="relative flex items-center">
		<input
			{id}
			type={showPassword ? 'text' : 'password'}
			bind:value
			{placeholder}
			{onblur}
			class="w-full rounded-lg border border-[rgba(255,255,255,0.08)] bg-surface text-white placeholder-text-muted focus:ring-1 focus:ring-brand focus:border-brand h-12 px-4 pr-10 text-sm transition-colors outline-none"
			class:border-danger={error}
		/>
		<button
			type="button"
			onclick={() => showPassword = !showPassword}
			class="absolute right-3 text-text-muted hover:text-white focus:outline-none flex items-center justify-center transition-colors"
			aria-label={showPassword ? 'Hide password' : 'Show password'}
		>
			{#if showPassword}
				<svg class="w-5 h-5" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
					<path stroke-linecap="round" stroke-linejoin="round" d="M3.98 8.223A10.477 10.477 0 0 0 1.934 12c1.292 4.338 5.31 7.5 10.066 7.5.993 0 1.953-.138 2.863-.395M6.228 6.228A10.451 10.451 0 0 1 12 4.5c4.756 0 8.773 3.162 10.065 7.498a10.522 10.522 0 0 1-4.293 5.774M6.228 6.228 3 3m3.228 3.228 3.65 3.65m7.894 7.894L21 21m-3.228-3.228-3.65-3.65m0 0a3 3 0 1 0-4.243-4.243m4.242 4.242L9.88 9.88"/>
				</svg>
			{:else}
				<svg class="w-5 h-5" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
					<path stroke-linecap="round" stroke-linejoin="round" d="M2.036 12.322a1.012 1.012 0 0 1 0-.639C3.423 7.51 7.36 4.5 12 4.5c4.638 0 8.573 3.007 9.963 7.178.07.207.07.431 0 .639C20.577 16.49 16.64 19.5 12 19.5c-4.638 0-8.573-3.007-9.963-7.178Z"/>
					<path stroke-linecap="round" stroke-linejoin="round" d="M15 12a3 3 0 1 1-6 0 3 3 0 0 1 6 0Z"/>
				</svg>
			{/if}
		</button>
	</div>
	{#if error}
		<p class="text-danger text-xs mt-0.5">{error}</p>
	{/if}
</div>
