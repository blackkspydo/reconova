<!--
  Rotating red glow background for auth pages.
  The glow orbs shift position per page to create a sense of progression.

  Variants:
    login         — top-left + top-right + bottom-center
    register-1    — bottom-left + top-right + top-center
    register-2    — right + left + bottom-center
    2fa-setup     — top-right + bottom-left + bottom-center
    2fa-verify    — top-center + bottom-left + bottom-right
    change-password — bottom-right + top-left + center
    forgot-password — left + right + bottom-center
-->
<script lang="ts">
	type GlowVariant = 'login' | 'register-1' | 'register-2' | '2fa-setup' | '2fa-verify' | 'change-password' | 'forgot-password';

	interface Props {
		variant?: GlowVariant;
	}

	let { variant = 'login' }: Props = $props();

	const positions: Record<GlowVariant, { orb1: string; orb2: string; orb3: string }> = {
		'login': {
			orb1: 'top-1/4 -left-48',
			orb2: 'top-1/4 -right-48',
			orb3: '-bottom-48 left-1/2 -translate-x-1/2',
		},
		'register-1': {
			orb1: 'bottom-1/4 -left-48',
			orb2: 'top-1/4 -right-48',
			orb3: '-top-48 left-1/2 -translate-x-1/2',
		},
		'register-2': {
			orb1: 'top-1/2 -right-48 -translate-y-1/2',
			orb2: 'top-1/2 -left-48 -translate-y-1/2',
			orb3: '-bottom-48 left-1/3',
		},
		'2fa-setup': {
			orb1: '-top-24 -right-48',
			orb2: 'bottom-1/4 -left-48',
			orb3: '-bottom-48 right-1/3',
		},
		'2fa-verify': {
			orb1: '-top-48 left-1/2 -translate-x-1/2',
			orb2: 'bottom-1/4 -left-48',
			orb3: 'bottom-1/4 -right-48',
		},
		'change-password': {
			orb1: 'bottom-1/4 -right-48',
			orb2: 'top-1/4 -left-48',
			orb3: 'top-1/2 left-1/2 -translate-x-1/2 -translate-y-1/2',
		},
		'forgot-password': {
			orb1: 'top-1/2 -left-48 -translate-y-1/2',
			orb2: 'top-1/2 -right-48 -translate-y-1/2',
			orb3: '-bottom-48 left-1/2 -translate-x-1/2',
		},
	};

	let pos = $derived(positions[variant]);
</script>

<div class="fixed {pos.orb1} w-[500px] h-[500px] bg-red-600/10 rounded-full blur-[120px] pointer-events-none" aria-hidden="true"></div>
<div class="fixed {pos.orb2} w-[500px] h-[500px] bg-red-600/10 rounded-full blur-[120px] pointer-events-none" aria-hidden="true"></div>
<div class="fixed {pos.orb3} w-[600px] h-[400px] bg-blue-600/10 rounded-full blur-[120px] pointer-events-none" aria-hidden="true"></div>
