<script lang="ts">
	import { page } from '$app/state';

	interface NavItem {
		label: string;
		href: string;
		icon: string;
	}

	const iconPaths: Record<string, string[]> = {
		dashboard: [
			'M3.75 6A2.25 2.25 0 0 1 6 3.75h2.25A2.25 2.25 0 0 1 10.5 6v2.25a2.25 2.25 0 0 1-2.25 2.25H6a2.25 2.25 0 0 1-2.25-2.25V6ZM3.75 15.75A2.25 2.25 0 0 1 6 13.5h2.25a2.25 2.25 0 0 1 2.25 2.25V18a2.25 2.25 0 0 1-2.25 2.25H6A2.25 2.25 0 0 1 3.75 18v-2.25ZM13.5 6a2.25 2.25 0 0 1 2.25-2.25H18A2.25 2.25 0 0 1 20.25 6v2.25A2.25 2.25 0 0 1 18 10.5h-2.25a2.25 2.25 0 0 1-2.25-2.25V6ZM13.5 15.75a2.25 2.25 0 0 1 2.25-2.25H18a2.25 2.25 0 0 1 2.25 2.25V18A2.25 2.25 0 0 1 18 20.25h-2.25a2.25 2.25 0 0 1-2.25-2.25v-2.25Z',
		],
		globe: [
			'M12 21a9.004 9.004 0 0 0 8.716-6.747M12 21a9.004 9.004 0 0 1-8.716-6.747M12 21c2.485 0 4.5-4.03 4.5-9S14.485 3 12 3m0 18c-2.485 0-4.5-4.03-4.5-9S9.515 3 12 3m0 0a8.997 8.997 0 0 1 7.843 4.582M12 3a8.997 8.997 0 0 0-7.843 4.582m15.686 0A11.953 11.953 0 0 1 12 10.5c-2.998 0-5.74-1.1-7.843-2.918m15.686 0A8.959 8.959 0 0 1 21 12c0 .778-.099 1.533-.284 2.253m0 0A17.919 17.919 0 0 1 12 16.5c-3.162 0-6.133-.815-8.716-2.247m0 0A9.015 9.015 0 0 1 3 12c0-1.605.42-3.113 1.157-4.418',
		],
		scan: [
			'm21 21-5.197-5.197m0 0A7.5 7.5 0 1 0 5.196 5.196a7.5 7.5 0 0 0 10.607 10.607Z',
		],
		'credit-card': [
			'M2.25 8.25h19.5M2.25 9h19.5m-16.5 5.25h6m-6 2.25h3m-3.75 3h15a2.25 2.25 0 0 0 2.25-2.25V6.75A2.25 2.25 0 0 0 19.5 4.5h-15a2.25 2.25 0 0 0-2.25 2.25v10.5A2.25 2.25 0 0 0 4.5 19.5Z',
		],
		settings: [
			'M9.594 3.94c.09-.542.56-.94 1.11-.94h2.593c.55 0 1.02.398 1.11.94l.213 1.281c.063.374.313.686.645.87.074.04.147.083.22.127.325.196.72.257 1.075.124l1.217-.456a1.125 1.125 0 0 1 1.37.49l1.296 2.247a1.125 1.125 0 0 1-.26 1.431l-1.003.827c-.293.241-.438.613-.43.992a7.723 7.723 0 0 1 0 .255c-.008.378.137.75.43.991l1.004.827c.424.35.534.955.26 1.43l-1.298 2.247a1.125 1.125 0 0 1-1.369.491l-1.217-.456c-.355-.133-.75-.072-1.076.124a6.47 6.47 0 0 1-.22.128c-.331.183-.581.495-.644.869l-.213 1.281c-.09.543-.56.94-1.11.94h-2.594c-.55 0-1.019-.398-1.11-.94l-.213-1.281c-.062-.374-.312-.686-.644-.87a6.52 6.52 0 0 1-.22-.127c-.325-.196-.72-.257-1.076-.124l-1.217.456a1.125 1.125 0 0 1-1.369-.49l-1.297-2.247a1.125 1.125 0 0 1 .26-1.431l1.004-.827c.292-.24.437-.613.43-.991a6.932 6.932 0 0 1 0-.255c.007-.38-.138-.751-.43-.992l-1.004-.827a1.125 1.125 0 0 1-.26-1.43l1.297-2.247a1.125 1.125 0 0 1 1.37-.491l1.216.456c.356.133.751.072 1.076-.124.072-.044.146-.086.22-.128.332-.183.582-.495.644-.869l.214-1.28Z',
			'M15 12a3 3 0 1 1-6 0 3 3 0 0 1 6 0Z',
		],
		users: [
			'M15 19.128a9.38 9.38 0 0 0 2.625.372 9.337 9.337 0 0 0 4.121-.952 4.125 4.125 0 0 0-7.533-2.493M15 19.128v-.003c0-1.113-.285-2.16-.786-3.07M15 19.128v.106A12.318 12.318 0 0 1 8.624 21c-2.331 0-4.512-.645-6.374-1.766l-.001-.109a6.375 6.375 0 0 1 11.964-3.07M12 6.375a3.375 3.375 0 1 1-6.75 0 3.375 3.375 0 0 1 6.75 0Zm8.25 2.25a2.625 2.625 0 1 1-5.25 0 2.625 2.625 0 0 1 5.25 0Z',
		],
		building: [
			'M2.25 21h19.5M3.75 3v18m16.5-18v18M5.25 3h13.5M5.25 3v3m13.5-3v3M8.25 21V10.5m0 0h7.5m-7.5 0V6m7.5 4.5V6m0 4.5V21m-3.75-6h.008v.008h-.008V15Zm0 3h.008v.008h-.008V18Z',
		],
	};

	const navItems: NavItem[] = [
		{ label: 'Dashboard', href: '/dashboard', icon: 'dashboard' },
		{ label: 'Domains', href: '/domains', icon: 'globe' },
		{ label: 'Scans', href: '/scans', icon: 'scan' },
		{ label: 'Billing', href: '/billing', icon: 'credit-card' },
		{ label: 'Settings', href: '/settings', icon: 'settings' },
	];

	const { isAdmin = false }: { isAdmin?: boolean } = $props();

	const adminItems: NavItem[] = [
		{ label: 'Users', href: '/admin/users', icon: 'users' },
		{ label: 'Tenants', href: '/admin/tenants', icon: 'building' },
	];

	function isActive(href: string): boolean {
		return page.url.pathname === href || page.url.pathname.startsWith(href + '/');
	}
</script>

{#snippet navIcon(icon: string)}
	{#if iconPaths[icon]}
		<svg class="w-[18px] h-[18px]" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="1.5">
			{#each iconPaths[icon] as d}
				<path stroke-linecap="round" stroke-linejoin="round" {d} />
			{/each}
		</svg>
	{/if}
{/snippet}

<aside class="w-60 bg-surface border-r border-[rgba(255,255,255,0.08)] flex flex-col h-screen sticky top-0">
	<!-- Logo -->
	<div class="px-5 py-5 border-b border-[rgba(255,255,255,0.08)]">
		<div class="flex items-center gap-2.5">
			<div class="size-7 text-brand">
				<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5">
					<path stroke-linecap="round" stroke-linejoin="round" d="M9 12.75 11.25 15 15 9.75m-3-7.036A11.959 11.959 0 0 1 3.598 6 11.99 11.99 0 0 0 3 9.749c0 5.592 3.824 10.29 9 11.623 5.176-1.332 9-6.03 9-11.622 0-1.31-.21-2.571-.598-3.751h-.152c-3.196 0-6.1-1.248-8.25-3.285Z" />
				</svg>
			</div>
			<span class="text-lg font-semibold tracking-wider uppercase text-white">Reconova<span class="text-brand">.</span></span>
		</div>
	</div>

	<!-- Navigation -->
	<nav class="flex-1 px-3 py-4 space-y-1 overflow-y-auto">
		<p class="px-3 text-[10px] font-semibold uppercase tracking-widest text-text-muted mb-2">Main</p>
		{#each navItems as item}
			<a
				href={item.href}
				class="flex items-center gap-3 px-3 py-2 rounded-lg text-sm font-medium transition-colors {isActive(item.href) ? 'bg-brand/10 text-brand' : 'text-text-secondary hover:text-white hover:bg-[rgba(255,255,255,0.04)]'}"
			>
				{@render navIcon(item.icon)}
				{item.label}
			</a>
		{/each}

		{#if isAdmin}
			<p class="px-3 text-[10px] font-semibold uppercase tracking-widest text-text-muted mt-6 mb-2">Admin</p>
			{#each adminItems as item}
				<a
					href={item.href}
					class="flex items-center gap-3 px-3 py-2 rounded-lg text-sm font-medium transition-colors {isActive(item.href) ? 'bg-brand/10 text-brand' : 'text-text-secondary hover:text-white hover:bg-[rgba(255,255,255,0.04)]'}"
				>
					{@render navIcon(item.icon)}
					{item.label}
				</a>
			{/each}
		{/if}
	</nav>
</aside>
