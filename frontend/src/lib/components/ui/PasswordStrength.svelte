<script lang="ts">
	interface Props {
		password: string;
	}

	let { password }: Props = $props();

	const checks = $derived({
		length: password.length >= 12,
		uppercase: /[A-Z]/.test(password),
		lowercase: /[a-z]/.test(password),
		number: /[0-9]/.test(password),
		special: /[^A-Za-z0-9]/.test(password),
	});

	const passed = $derived(Object.values(checks).filter(Boolean).length);

	const strength = $derived(
		password.length === 0 ? 'none' :
		passed <= 2 ? 'weak' :
		passed <= 4 ? 'good' :
		'strong'
	);

	const strengthLabel = $derived(
		strength === 'none' ? '' :
		strength === 'weak' ? 'Weak' :
		strength === 'good' ? 'Good' :
		'Strong'
	);

	const barWidth = $derived(
		strength === 'none' ? 'w-0' :
		strength === 'weak' ? 'w-1/3' :
		strength === 'good' ? 'w-2/3' :
		'w-full'
	);

	const barColor = $derived(
		strength === 'weak' ? 'bg-danger' :
		strength === 'good' ? 'bg-warning' :
		strength === 'strong' ? 'bg-success' :
		''
	);
</script>

{#if password.length > 0}
	<div class="flex flex-col gap-3 mt-1">
		<div class="flex items-center gap-2">
			<div class="flex-1 h-1 rounded-full bg-surface-2 overflow-hidden">
				<div class="h-full rounded-full transition-all duration-300 {barWidth} {barColor}"></div>
			</div>
			<span class="text-xs font-medium {strength === 'weak' ? 'text-danger' : strength === 'good' ? 'text-warning' : 'text-success'}">
				{strengthLabel}
			</span>
		</div>

		<div class="grid grid-cols-2 gap-x-4 gap-y-1.5">
			<div class="flex items-center gap-1.5 text-xs {checks.length ? 'text-success' : 'text-text-muted'}">
				<svg class="w-3.5 h-3.5 shrink-0" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
					{#if checks.length}
						<path stroke-linecap="round" stroke-linejoin="round" d="m4.5 12.75 6 6 9-13.5" />
					{:else}
						<path stroke-linecap="round" stroke-linejoin="round" d="M6 18 18 6M6 6l12 12" />
					{/if}
				</svg>
				12+ characters
			</div>
			<div class="flex items-center gap-1.5 text-xs {checks.uppercase ? 'text-success' : 'text-text-muted'}">
				<svg class="w-3.5 h-3.5 shrink-0" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
					{#if checks.uppercase}
						<path stroke-linecap="round" stroke-linejoin="round" d="m4.5 12.75 6 6 9-13.5" />
					{:else}
						<path stroke-linecap="round" stroke-linejoin="round" d="M6 18 18 6M6 6l12 12" />
					{/if}
				</svg>
				Uppercase
			</div>
			<div class="flex items-center gap-1.5 text-xs {checks.lowercase ? 'text-success' : 'text-text-muted'}">
				<svg class="w-3.5 h-3.5 shrink-0" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
					{#if checks.lowercase}
						<path stroke-linecap="round" stroke-linejoin="round" d="m4.5 12.75 6 6 9-13.5" />
					{:else}
						<path stroke-linecap="round" stroke-linejoin="round" d="M6 18 18 6M6 6l12 12" />
					{/if}
				</svg>
				Lowercase
			</div>
			<div class="flex items-center gap-1.5 text-xs {checks.number ? 'text-success' : 'text-text-muted'}">
				<svg class="w-3.5 h-3.5 shrink-0" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
					{#if checks.number}
						<path stroke-linecap="round" stroke-linejoin="round" d="m4.5 12.75 6 6 9-13.5" />
					{:else}
						<path stroke-linecap="round" stroke-linejoin="round" d="M6 18 18 6M6 6l12 12" />
					{/if}
				</svg>
				Number
			</div>
			<div class="flex items-center gap-1.5 text-xs {checks.special ? 'text-success' : 'text-text-muted'}">
				<svg class="w-3.5 h-3.5 shrink-0" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
					{#if checks.special}
						<path stroke-linecap="round" stroke-linejoin="round" d="m4.5 12.75 6 6 9-13.5" />
					{:else}
						<path stroke-linecap="round" stroke-linejoin="round" d="M6 18 18 6M6 6l12 12" />
					{/if}
				</svg>
				Special character
			</div>
		</div>
	</div>
{/if}
