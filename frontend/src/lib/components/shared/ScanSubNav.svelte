<script lang="ts">
	import { page } from '$app/state';

	const tabs = [
		{ href: '/scans', label: 'Scans', exact: true },
		{ href: '/scans/schedules', label: 'Schedules', exact: false },
		{ href: '/scans/workflows', label: 'Workflows', exact: false },
	];

	function isActive(tab: { href: string; exact: boolean }): boolean {
		if (tab.exact) {
			return page.url.pathname === tab.href;
		}
		return page.url.pathname.startsWith(tab.href);
	}
</script>

<nav class="flex gap-1 mb-6 bg-surface rounded-lg border border-[rgba(255,255,255,0.08)] p-1 w-fit">
	{#each tabs as tab}
		<a
			href={tab.href}
			class="px-4 py-2 text-sm font-medium rounded-md transition-colors {isActive(tab) ? 'bg-brand/20 text-brand' : 'text-text-secondary hover:text-white'}"
		>
			{tab.label}
		</a>
	{/each}
</nav>
