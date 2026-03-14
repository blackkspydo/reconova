<script lang="ts">
	let { severity }: { severity: 'LOW' | 'MEDIUM' | 'HIGH' | 'CRITICAL' | 'INFO' } = $props();

	const colorMap: Record<string, string> = {
		CRITICAL: 'text-red-400 bg-red-500/10 border-red-500/20',
		HIGH: 'text-orange-400 bg-orange-500/10 border-orange-500/20',
		MEDIUM: 'text-yellow-400 bg-yellow-500/10 border-yellow-500/20',
		LOW: 'text-blue-400 bg-blue-500/10 border-blue-500/20',
		INFO: 'text-text-muted bg-[rgba(255,255,255,0.05)] border-[rgba(255,255,255,0.08)]',
	};
</script>

<span class="text-xs font-bold px-2 py-0.5 rounded border {colorMap[severity]}">
	{severity}
</span>
