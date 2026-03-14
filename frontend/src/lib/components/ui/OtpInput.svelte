<script lang="ts">
	interface Props {
		value?: string;
		length?: number;
		error?: string | null;
	}

	let { value = $bindable(''), length = 6, error = null }: Props = $props();

	let inputs: HTMLInputElement[] = $state([]);

	function handleInput(index: number, e: Event) {
		const target = e.target as HTMLInputElement;
		const val = target.value.replace(/\D/g, '');
		target.value = val.slice(0, 1);

		// Build value from all inputs
		const chars = inputs.map(el => el?.value || '');
		value = chars.join('');

		// Auto-advance
		if (val && index < length - 1) {
			inputs[index + 1]?.focus();
		}
	}

	function handleKeydown(index: number, e: KeyboardEvent) {
		const target = e.target as HTMLInputElement;
		if (e.key === 'Backspace' && !target.value && index > 0) {
			inputs[index - 1]?.focus();
		}
	}

	function handlePaste(e: ClipboardEvent) {
		e.preventDefault();
		const pasted = (e.clipboardData?.getData('text') || '').replace(/\D/g, '').slice(0, length);
		for (let i = 0; i < length; i++) {
			if (inputs[i]) {
				inputs[i].value = pasted[i] || '';
			}
		}
		value = pasted;
		const focusIndex = Math.min(pasted.length, length - 1);
		inputs[focusIndex]?.focus();
	}
</script>

<div class="flex flex-col items-center gap-3">
	<div class="flex items-center gap-2">
		{#each Array(length) as _, i}
			{#if i === Math.floor(length / 2)}
				<div class="w-2"></div>
			{/if}
			<input
				bind:this={inputs[i]}
				type="text"
				inputmode="numeric"
				maxlength="1"
				class="w-12 h-14 text-center font-mono text-2xl font-semibold rounded-lg border transition-colors outline-none
					{error
						? 'border-danger/50 bg-danger/5'
						: 'border-[rgba(255,255,255,0.08)] bg-surface'
					}
					text-white focus:ring-1 focus:ring-brand focus:border-brand"
				oninput={(e) => handleInput(i, e)}
				onkeydown={(e) => handleKeydown(i, e)}
				onpaste={handlePaste}
				autocomplete="one-time-code"
			/>
		{/each}
	</div>
	{#if error}
		<p class="text-danger text-xs flex items-center gap-1.5">
			<svg class="w-3.5 h-3.5 shrink-0" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
				<path stroke-linecap="round" stroke-linejoin="round" d="M12 9v3.75m9-.75a9 9 0 1 1-18 0 9 9 0 0 1 18 0Zm-9 3.75h.008v.008H12v-.008Z" />
			</svg>
			{error}
		</p>
	{/if}
</div>
