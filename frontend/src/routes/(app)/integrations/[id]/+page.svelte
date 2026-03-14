<script lang="ts">
	import { page } from '$app/state';
	import { goto } from '$app/navigation';
	import { integrationsApi } from '$lib/api/client';
	import type { Integration, IntegrationType, NotificationRule, EventType } from '$lib/types/integrations';
	import type { ApiError } from '$lib/types/auth';
	import { Button, Alert, TextInput } from '$lib/components/ui';
	import { Modal, EmptyState, SkeletonLoader } from '$lib/components/shared';

	// Integration state
	let integration = $state<Integration | null>(null);
	let isLoading = $state(true);
	let error = $state<string | null>(null);
	let isSaving = $state(false);
	let saveSuccess = $state(false);

	// Form fields
	let integrationName = $state('');
	let integrationEnabled = $state(true);

	// Config fields
	let emailRecipients = $state('');
	let slackWebhookUrl = $state('');
	let jiraBaseUrl = $state('');
	let jiraProjectKey = $state('');
	let jiraApiToken = $state('');
	let webhookUrl = $state('');
	let webhookSecret = $state('');
	let siemEndpoint = $state('');
	let siemApiKey = $state('');
	let siemFormat = $state<'CEF' | 'JSON'>('JSON');

	// Test state
	let isTesting = $state(false);
	let testResult = $state<boolean | null>(null);

	// Delete state
	let showDeleteModal = $state(false);
	let isDeleting = $state(false);
	let deleteError = $state<string | null>(null);

	// Rules state
	let rules = $state<NotificationRule[]>([]);
	let rulesLoading = $state(false);

	// Add rule modal
	let showAddRuleModal = $state(false);
	let newRuleEventType = $state<EventType>('VULNERABILITY_FOUND');
	let newRuleSeverities = $state<Record<string, boolean>>({ LOW: false, MEDIUM: false, HIGH: true, CRITICAL: true });
	let isAddingRule = $state(false);
	let addRuleError = $state<string | null>(null);

	// Deleting rule
	let deletingRuleId = $state<string | null>(null);

	const typeConfig: Record<IntegrationType, { label: string; color: string; bg: string; icon: string }> = {
		SLACK: { label: 'Slack', color: 'text-purple-400', bg: 'bg-purple-500/10', icon: 'M20.7 5.3a1 1 0 0 0-1.4 0L12 12.58 4.7 5.3a1 1 0 0 0-1.4 1.4l8 8a1 1 0 0 0 1.4 0l8-8a1 1 0 0 0 0-1.4Z' },
		EMAIL: { label: 'Email', color: 'text-blue-400', bg: 'bg-blue-500/10', icon: 'M21.75 6.75v10.5a2.25 2.25 0 0 1-2.25 2.25h-15a2.25 2.25 0 0 1-2.25-2.25V6.75m19.5 0A2.25 2.25 0 0 0 19.5 4.5h-15a2.25 2.25 0 0 0-2.25 2.25m19.5 0v.243a2.25 2.25 0 0 1-1.07 1.916l-7.5 4.615a2.25 2.25 0 0 1-2.36 0L3.32 8.91a2.25 2.25 0 0 1-1.07-1.916V6.75' },
		JIRA: { label: 'Jira', color: 'text-cyan-400', bg: 'bg-cyan-500/10', icon: 'M9 12h3.75M9 15h3.75M9 18h3.75m3 .75H18a2.25 2.25 0 0 0 2.25-2.25V6.108c0-1.135-.845-2.098-1.976-2.192a48.424 48.424 0 0 0-1.123-.08m-5.801 0c-.065.21-.1.433-.1.664 0 .414.336.75.75.75h4.5a.75.75 0 0 0 .75-.75 2.25 2.25 0 0 0-.1-.664m-5.8 0A2.251 2.251 0 0 1 13.5 2.25H15c1.012 0 1.867.668 2.15 1.586m-5.8 0c-.376.023-.75.05-1.124.08C9.095 4.01 8.25 4.973 8.25 6.108V8.25m0 0H4.875c-.621 0-1.125.504-1.125 1.125v11.25c0 .621.504 1.125 1.125 1.125h9.75c.621 0 1.125-.504 1.125-1.125V9.375c0-.621-.504-1.125-1.125-1.125H8.25Z' },
		WEBHOOK: { label: 'Webhook', color: 'text-orange-400', bg: 'bg-orange-500/10', icon: 'M13.19 8.688a4.5 4.5 0 0 1 1.242 7.244l-4.5 4.5a4.5 4.5 0 0 1-6.364-6.364l1.757-1.757m13.35-.622 1.757-1.757a4.5 4.5 0 0 0-6.364-6.364l-4.5 4.5a4.5 4.5 0 0 0 1.242 7.244' },
		SIEM: { label: 'SIEM', color: 'text-green-400', bg: 'bg-green-500/10', icon: 'M3.75 3v11.25A2.25 2.25 0 0 0 6 16.5h2.25M3.75 3h-1.5m1.5 0h16.5m0 0h1.5m-1.5 0v11.25A2.25 2.25 0 0 1 18 16.5h-2.25m-7.5 0h7.5m-7.5 0-1 3m8.5-3 1 3m0 0 .5 1.5m-.5-1.5h-9.5m0 0-.5 1.5' },
	};

	const eventTypes: { value: EventType; label: string }[] = [
		{ value: 'SCAN_COMPLETED', label: 'Scan Completed' },
		{ value: 'SCAN_FAILED', label: 'Scan Failed' },
		{ value: 'VULNERABILITY_FOUND', label: 'Vulnerability Found' },
		{ value: 'CVE_ALERT', label: 'CVE Alert' },
		{ value: 'DOMAIN_VERIFIED', label: 'Domain Verified' },
		{ value: 'CREDITS_LOW', label: 'Credits Low' },
		{ value: 'SCHEDULE_FAILED', label: 'Schedule Failed' },
	];

	function populateForm(i: Integration) {
		integrationName = i.name;
		integrationEnabled = i.enabled;
		const cfg = i.config;
		switch (i.type) {
			case 'EMAIL':
				emailRecipients = Array.isArray(cfg.recipients) ? (cfg.recipients as string[]).join(', ') : String(cfg.recipients ?? '');
				break;
			case 'SLACK':
				slackWebhookUrl = String(cfg.webhook_url ?? '');
				break;
			case 'JIRA':
				jiraBaseUrl = String(cfg.base_url ?? '');
				jiraProjectKey = String(cfg.project_key ?? '');
				jiraApiToken = String(cfg.api_token ?? '');
				break;
			case 'WEBHOOK':
				webhookUrl = String(cfg.url ?? '');
				webhookSecret = String(cfg.secret ?? '');
				break;
			case 'SIEM':
				siemEndpoint = String(cfg.endpoint ?? '');
				siemApiKey = String(cfg.api_key ?? '');
				siemFormat = (cfg.format === 'CEF' ? 'CEF' : 'JSON');
				break;
		}
	}

	function buildConfig(): Record<string, unknown> {
		if (!integration) return {};
		switch (integration.type) {
			case 'EMAIL':
				return { recipients: emailRecipients.split(',').map(e => e.trim()).filter(Boolean) };
			case 'SLACK':
				return { webhook_url: slackWebhookUrl };
			case 'JIRA':
				return { base_url: jiraBaseUrl, project_key: jiraProjectKey, api_token: jiraApiToken };
			case 'WEBHOOK':
				return { url: webhookUrl, secret: webhookSecret };
			case 'SIEM':
				return { endpoint: siemEndpoint, api_key: siemApiKey, format: siemFormat };
			default:
				return {};
		}
	}

	async function loadIntegration() {
		isLoading = true;
		error = null;
		try {
			integration = (await integrationsApi.get(page.params.id)) as Integration;
			populateForm(integration);
		} catch (err) {
			const apiErr = err as ApiError;
			error = apiErr.message || 'Failed to load integration.';
		} finally {
			isLoading = false;
		}
	}

	async function loadRules() {
		rulesLoading = true;
		try {
			const allRules = (await integrationsApi.getRules()) as NotificationRule[];
			rules = allRules.filter(r => r.integration_id === page.params.id);
		} catch {
			rules = [];
		} finally {
			rulesLoading = false;
		}
	}

	$effect(() => {
		loadIntegration();
		loadRules();
	});

	async function handleSave() {
		if (!integration) return;
		isSaving = true;
		error = null;
		saveSuccess = false;
		try {
			await integrationsApi.update(integration.id, {
				name: integrationName.trim(),
				config: buildConfig(),
				enabled: integrationEnabled,
			});
			saveSuccess = true;
			await loadIntegration();
			setTimeout(() => { saveSuccess = false; }, 3000);
		} catch (err) {
			const apiErr = err as ApiError;
			error = apiErr.message || 'Failed to save integration.';
		} finally {
			isSaving = false;
		}
	}

	async function handleTest() {
		if (!integration) return;
		isTesting = true;
		testResult = null;
		try {
			await integrationsApi.test(integration.id);
			testResult = true;
			await loadIntegration();
		} catch {
			testResult = false;
		} finally {
			isTesting = false;
		}
	}

	async function handleDelete() {
		if (!integration) return;
		isDeleting = true;
		deleteError = null;
		try {
			await integrationsApi.delete(integration.id);
			goto('/integrations');
		} catch (err) {
			const apiErr = err as ApiError;
			deleteError = apiErr.message || 'Failed to delete integration.';
		} finally {
			isDeleting = false;
		}
	}

	async function handleAddRule() {
		if (!integration) return;
		isAddingRule = true;
		addRuleError = null;
		const selectedSeverities = Object.entries(newRuleSeverities)
			.filter(([, v]) => v)
			.map(([k]) => k) as ('LOW' | 'MEDIUM' | 'HIGH' | 'CRITICAL')[];
		try {
			await integrationsApi.createRule({
				integration_id: integration.id,
				event_type: newRuleEventType,
				severity_filter: selectedSeverities,
			});
			showAddRuleModal = false;
			newRuleEventType = 'VULNERABILITY_FOUND';
			newRuleSeverities = { LOW: false, MEDIUM: false, HIGH: true, CRITICAL: true };
			await loadRules();
		} catch (err) {
			const apiErr = err as ApiError;
			addRuleError = apiErr.message || 'Failed to create rule.';
		} finally {
			isAddingRule = false;
		}
	}

	async function deleteRule(ruleId: string) {
		deletingRuleId = ruleId;
		try {
			await integrationsApi.deleteRule(ruleId);
			await loadRules();
		} catch (err) {
			const apiErr = err as ApiError;
			error = apiErr.message || 'Failed to delete rule.';
		} finally {
			deletingRuleId = null;
		}
	}

	function formatDateTime(dateStr: string | null): string {
		if (!dateStr) return 'Never';
		return new Date(dateStr).toLocaleString();
	}

	function formatEventType(et: string): string {
		return et.replace(/_/g, ' ').replace(/\b\w/g, c => c.toUpperCase());
	}
</script>

<svelte:head>
	<title>{integration?.name ?? 'Integration'} — Reconova</title>
</svelte:head>

<div class="max-w-3xl">
	<!-- Back link -->
	<a href="/integrations" class="text-brand text-sm font-medium hover:text-brand-dark transition-colors inline-flex items-center gap-1 mb-6">
		<svg class="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
			<path stroke-linecap="round" stroke-linejoin="round" d="M10.5 19.5 3 12m0 0 7.5-7.5M3 12h18" />
		</svg>
		Back to Integrations
	</a>

	{#if error}
		<div class="mb-4">
			<Alert variant="error">{error}</Alert>
		</div>
	{/if}

	{#if saveSuccess}
		<div class="mb-4">
			<Alert variant="success">Integration saved successfully.</Alert>
		</div>
	{/if}

	{#if isLoading}
		<div class="bg-surface rounded-xl border border-[rgba(255,255,255,0.08)] p-6">
			<SkeletonLoader lines={6} />
		</div>
	{:else if integration}
		{@const cfg = typeConfig[integration.type]}

		<!-- Header -->
		<div class="flex items-center gap-3 mb-6">
			<div class="w-10 h-10 rounded-lg {cfg.bg} flex items-center justify-center">
				<svg class="w-5 h-5 {cfg.color}" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="1.5">
					<path stroke-linecap="round" stroke-linejoin="round" d={cfg.icon} />
				</svg>
			</div>
			<div class="flex-1">
				<h1 class="text-2xl font-bold text-white">{integration.name}</h1>
				<span class="text-xs {cfg.color}">{cfg.label}</span>
			</div>
		</div>

		<!-- Edit Form -->
		<div class="bg-surface/60 backdrop-blur-sm rounded-xl border border-[rgba(255,255,255,0.08)] p-6 mb-4">
			<h2 class="text-lg font-semibold text-white mb-5">Configuration</h2>

			<!-- Name -->
			<div class="mb-5">
				<TextInput
					id="integration-name"
					label="Integration Name"
					bind:value={integrationName}
					placeholder="My Integration"
				/>
			</div>

			<!-- Enable/Disable Toggle -->
			<div class="flex items-center justify-between mb-5 py-3 border-y border-[rgba(255,255,255,0.06)]">
				<div>
					<p class="text-sm font-medium text-white">Enabled</p>
					<p class="text-xs text-text-muted">Receive notifications from this integration</p>
				</div>
				<button
					class="relative inline-flex h-6 w-11 items-center rounded-full transition-colors {integrationEnabled ? 'bg-brand' : 'bg-[rgba(255,255,255,0.1)]'}"
					onclick={() => { integrationEnabled = !integrationEnabled; }}
					aria-label="{integrationEnabled ? 'Disable' : 'Enable'} integration"
				>
					<span
						class="inline-block h-4 w-4 rounded-full bg-white transition-transform {integrationEnabled ? 'translate-x-6' : 'translate-x-1'}"
					></span>
				</button>
			</div>

			<!-- Dynamic config fields -->
			{#if integration.type === 'EMAIL'}
				<div class="flex flex-col gap-2">
					<label class="text-white text-sm font-medium" for="email-recipients">Recipients</label>
					<textarea
						id="email-recipients"
						bind:value={emailRecipients}
						placeholder="user@example.com, team@example.com"
						rows="3"
						class="w-full rounded-lg border border-[rgba(255,255,255,0.08)] bg-surface text-white placeholder-text-muted focus:ring-1 focus:ring-brand focus:border-brand px-4 py-3 text-sm transition-colors outline-none resize-none"
					></textarea>
					<p class="text-text-muted text-xs">Separate multiple email addresses with commas</p>
				</div>
			{:else if integration.type === 'SLACK'}
				<TextInput
					id="slack-webhook-url"
					label="Webhook URL"
					bind:value={slackWebhookUrl}
					placeholder="https://hooks.slack.com/services/..."
				/>
			{:else if integration.type === 'JIRA'}
				<div class="space-y-4">
					<TextInput
						id="jira-base-url"
						label="Jira Base URL"
						bind:value={jiraBaseUrl}
						placeholder="https://yourcompany.atlassian.net"
					/>
					<TextInput
						id="jira-project-key"
						label="Project Key"
						bind:value={jiraProjectKey}
						placeholder="SEC"
					/>
					<TextInput
						id="jira-api-token"
						label="API Token"
						bind:value={jiraApiToken}
						placeholder="Your Jira API token"
						type="password"
					/>
				</div>
			{:else if integration.type === 'WEBHOOK'}
				<div class="space-y-4">
					<TextInput
						id="webhook-url"
						label="Webhook URL"
						bind:value={webhookUrl}
						placeholder="https://api.example.com/webhook"
					/>
					<TextInput
						id="webhook-secret"
						label="Secret (optional)"
						bind:value={webhookSecret}
						placeholder="Signing secret for payload verification"
						type="password"
					/>
				</div>
			{:else if integration.type === 'SIEM'}
				<div class="space-y-4">
					<TextInput
						id="siem-endpoint"
						label="SIEM Endpoint"
						bind:value={siemEndpoint}
						placeholder="https://siem.example.com/api/events"
					/>
					<TextInput
						id="siem-api-key"
						label="API Key"
						bind:value={siemApiKey}
						placeholder="Your SIEM API key"
						type="password"
					/>
					<div class="flex flex-col gap-2">
						<label class="text-white text-sm font-medium" for="siem-format">Format</label>
						<select
							id="siem-format"
							bind:value={siemFormat}
							class="w-full bg-surface border border-[rgba(255,255,255,0.08)] rounded-lg px-3 py-3 text-sm text-text focus:outline-none focus:border-brand/50 transition-colors"
						>
							<option value="JSON">JSON</option>
							<option value="CEF">CEF</option>
						</select>
					</div>
				</div>
			{/if}
		</div>

		<!-- Test Connection -->
		<div class="bg-surface/60 backdrop-blur-sm rounded-xl border border-[rgba(255,255,255,0.08)] p-6 mb-4">
			<h2 class="text-lg font-semibold text-white mb-3">Test Connection</h2>
			<p class="text-text-muted text-xs mb-4">Send a test notification to verify the integration is working correctly.</p>

			<div class="flex items-center gap-4">
				<Button variant="secondary" onclick={handleTest} loading={isTesting}>
					{#if isTesting}
						Testing...
					{:else}
						Test Connection
					{/if}
				</Button>

				{#if testResult !== null}
					<div class="flex items-center gap-2 text-sm">
						{#if testResult}
							<svg class="w-5 h-5 text-success" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
								<path stroke-linecap="round" stroke-linejoin="round" d="M9 12.75 11.25 15 15 9.75M21 12a9 9 0 1 1-18 0 9 9 0 0 1 18 0Z" />
							</svg>
							<span class="text-success">Connection successful</span>
						{:else}
							<svg class="w-5 h-5 text-danger" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
								<path stroke-linecap="round" stroke-linejoin="round" d="m9.75 9.75 4.5 4.5m0-4.5-4.5 4.5M21 12a9 9 0 1 1-18 0 9 9 0 0 1 18 0Z" />
							</svg>
							<span class="text-danger">Connection failed</span>
						{/if}
					</div>
				{/if}

				{#if integration.last_tested_at}
					<span class="text-text-muted text-xs ml-auto">Last tested: {formatDateTime(integration.last_tested_at)}</span>
				{/if}
			</div>
		</div>

		<!-- Notification Rules -->
		<div class="bg-surface/60 backdrop-blur-sm rounded-xl border border-[rgba(255,255,255,0.08)] p-6 mb-4">
			<div class="flex items-center justify-between mb-4">
				<h2 class="text-lg font-semibold text-white">Notification Rules</h2>
				<Button variant="secondary" onclick={() => { showAddRuleModal = true; addRuleError = null; }}>
					Add Rule
				</Button>
			</div>

			{#if rulesLoading}
				<SkeletonLoader lines={3} />
			{:else if rules.length === 0}
				<EmptyState
					title="No notification rules"
					description="Add rules to control which events trigger notifications for this integration."
				/>
			{:else}
				<div class="space-y-2">
					{#each rules as rule}
						<div class="flex items-center justify-between p-3 rounded-lg border border-[rgba(255,255,255,0.06)] hover:border-[rgba(255,255,255,0.12)] transition-colors">
							<div>
								<p class="text-sm font-medium text-white">{formatEventType(rule.event_type)}</p>
								<div class="flex items-center gap-1.5 mt-1">
									{#each rule.severity_filter as sev}
										<span class="text-xs px-2 py-0.5 rounded-full {
											sev === 'CRITICAL' ? 'bg-danger/10 text-danger' :
											sev === 'HIGH' ? 'bg-orange-500/10 text-orange-400' :
											sev === 'MEDIUM' ? 'bg-warning/10 text-warning' :
											'bg-[rgba(255,255,255,0.05)] text-text-secondary'
										}">{sev}</span>
									{/each}
								</div>
							</div>
							<button
								class="text-xs font-medium text-danger hover:text-red-400 transition-colors px-2 py-1.5 rounded-md hover:bg-danger/5"
								onclick={() => deleteRule(rule.id)}
								disabled={deletingRuleId === rule.id}
							>
								{#if deletingRuleId === rule.id}
									<svg class="w-4 h-4 animate-spin" fill="none" viewBox="0 0 24 24">
										<circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
										<path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
									</svg>
								{:else}
									Delete
								{/if}
							</button>
						</div>
					{/each}
				</div>
			{/if}
		</div>

		<!-- Actions -->
		<div class="flex items-center justify-between">
			<Button variant="ghost" onclick={() => { showDeleteModal = true; deleteError = null; }}>
				<span class="text-danger">Delete Integration</span>
			</Button>
			<div class="flex items-center gap-3">
				<Button variant="secondary" onclick={() => goto('/integrations')}>Cancel</Button>
				<Button variant="primary" onclick={handleSave} loading={isSaving}>
					Save Changes
				</Button>
			</div>
		</div>
	{/if}
</div>

<!-- Delete Confirmation Modal -->
<Modal title="Delete Integration" open={showDeleteModal} onclose={() => { showDeleteModal = false; }}>
	<div>
		<p class="text-text-secondary text-sm mb-1">
			Are you sure you want to delete this integration?
		</p>
		<p class="text-white text-sm font-medium mb-4">{integration?.name}</p>
		<p class="text-text-muted text-xs mb-6">
			This will permanently remove the integration and all associated notification rules.
		</p>

		{#if deleteError}
			<div class="mb-4">
				<Alert variant="error">{deleteError}</Alert>
			</div>
		{/if}

		<div class="flex justify-end gap-3">
			<Button variant="ghost" onclick={() => { showDeleteModal = false; }} disabled={isDeleting}>Cancel</Button>
			<Button variant="destructive" onclick={handleDelete} loading={isDeleting}>Delete Integration</Button>
		</div>
	</div>
</Modal>

<!-- Add Rule Modal -->
<Modal title="Add Notification Rule" open={showAddRuleModal} onclose={() => { showAddRuleModal = false; }}>
	<div>
		<div class="mb-4">
			<label class="text-white text-sm font-medium mb-2 block" for="rule-event-type">Event Type</label>
			<select
				id="rule-event-type"
				bind:value={newRuleEventType}
				class="w-full bg-surface border border-[rgba(255,255,255,0.08)] rounded-lg px-3 py-3 text-sm text-text focus:outline-none focus:border-brand/50 transition-colors"
			>
				{#each eventTypes as et}
					<option value={et.value}>{et.label}</option>
				{/each}
			</select>
		</div>

		<div class="mb-6">
			<p class="text-white text-sm font-medium mb-2">Severity Filter</p>
			<div class="space-y-2">
				{#each ['CRITICAL', 'HIGH', 'MEDIUM', 'LOW'] as sev}
					<label class="flex items-center gap-3 cursor-pointer">
						<input
							type="checkbox"
							bind:checked={newRuleSeverities[sev]}
							class="w-4 h-4 rounded border-[rgba(255,255,255,0.2)] bg-surface accent-brand"
						/>
						<span class="text-sm {
							sev === 'CRITICAL' ? 'text-danger' :
							sev === 'HIGH' ? 'text-orange-400' :
							sev === 'MEDIUM' ? 'text-warning' :
							'text-text-secondary'
						}">{sev}</span>
					</label>
				{/each}
			</div>
		</div>

		{#if addRuleError}
			<div class="mb-4">
				<Alert variant="error">{addRuleError}</Alert>
			</div>
		{/if}

		<div class="flex justify-end gap-3">
			<Button variant="ghost" onclick={() => { showAddRuleModal = false; }} disabled={isAddingRule}>Cancel</Button>
			<Button variant="primary" onclick={handleAddRule} loading={isAddingRule}>Add Rule</Button>
		</div>
	</div>
</Modal>
