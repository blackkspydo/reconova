<script lang="ts">
	let { status, size = 'sm' }: { status: string; size?: 'sm' | 'md' } = $props();

	const colorMap: Record<string, string> = {
		ACTIVE: 'text-success bg-success/10',
		COMPLETED: 'text-success bg-success/10',
		VERIFIED: 'text-success bg-success/10',
		RUNNING: 'text-info bg-info/10',
		QUEUED: 'text-info bg-info/10',
		PROVISIONING: 'text-info bg-info/10',
		PENDING: 'text-text-muted bg-[rgba(255,255,255,0.05)]',
		PENDING_VERIFICATION: 'text-warning bg-warning/10',
		PARTIAL: 'text-warning bg-warning/10',
		RETRYING: 'text-warning bg-warning/10',
		PASSWORD_EXPIRED: 'text-warning bg-warning/10',
		PAST_DUE: 'text-warning bg-warning/10',
		FAILED: 'text-danger bg-danger/10',
		LOCKED: 'text-danger bg-danger/10',
		CANCELLED: 'text-text-muted bg-[rgba(255,255,255,0.05)]',
		DEACTIVATED: 'text-text-muted bg-[rgba(255,255,255,0.05)]',
		SUSPENDED: 'text-danger bg-danger/10',
		SKIPPED: 'text-text-muted bg-[rgba(255,255,255,0.05)]',
	};

	let classes = $derived(colorMap[status] ?? 'text-text-muted bg-[rgba(255,255,255,0.05)]');
	let sizeClasses = $derived(size === 'md' ? 'text-sm px-3 py-1' : 'text-xs px-2 py-0.5');
</script>

<span class="font-medium rounded-full {classes} {sizeClasses}">
	{status.replace(/_/g, ' ')}
</span>
