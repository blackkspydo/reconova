<script lang="ts">
	import { integrationsApi } from '$lib/api/client';
	import type { IntegrationType } from '$lib/types/integrations';
	import type { ApiError } from '$lib/types/auth';
	import { Button, Alert, TextInput } from '$lib/components/ui';
	import { goto } from '$app/navigation';

	// Step state
	let currentStep = $state(1);
	let selectedType = $state<IntegrationType | ''>('');

	// Form state
	let integrationName = $state('');
	let isSubmitting = $state(false);
	let error = $state<string | null>(null);

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

	const typeOptions: { type: IntegrationType; label: string; description: string; color: string; bg: string; icon: string }[] = [
		{
			type: 'EMAIL',
			label: 'Email',
			description: 'Email notifications to your inbox',
			color: 'text-blue-400',
			bg: 'bg-blue-500/10',
			icon: 'M21.75 6.75v10.5a2.25 2.25 0 0 1-2.25 2.25h-15a2.25 2.25 0 0 1-2.25-2.25V6.75m19.5 0A2.25 2.25 0 0 0 19.5 4.5h-15a2.25 2.25 0 0 0-2.25 2.25m19.5 0v.243a2.25 2.25 0 0 1-1.07 1.916l-7.5 4.615a2.25 2.25 0 0 1-2.36 0L3.32 8.91a2.25 2.25 0 0 1-1.07-1.916V6.75',
		},
		{
			type: 'SLACK',
			label: 'Slack',
			description: 'Send alerts to Slack channels',
			color: 'text-purple-400',
			bg: 'bg-purple-500/10',
			icon: 'M20.7 5.3a1 1 0 0 0-1.4 0L12 12.58 4.7 5.3a1 1 0 0 0-1.4 1.4l8 8a1 1 0 0 0 1.4 0l8-8a1 1 0 0 0 0-1.4Z',
		},
		{
			type: 'JIRA',
			label: 'Jira',
			description: 'Create Jira tickets for findings',
			color: 'text-cyan-400',
			bg: 'bg-cyan-500/10',
			icon: 'M9 12h3.75M9 15h3.75M9 18h3.75m3 .75H18a2.25 2.25 0 0 0 2.25-2.25V6.108c0-1.135-.845-2.098-1.976-2.192a48.424 48.424 0 0 0-1.123-.08m-5.801 0c-.065.21-.1.433-.1.664 0 .414.336.75.75.75h4.5a.75.75 0 0 0 .75-.75 2.25 2.25 0 0 0-.1-.664m-5.8 0A2.251 2.251 0 0 1 13.5 2.25H15c1.012 0 1.867.668 2.15 1.586m-5.8 0c-.376.023-.75.05-1.124.08C9.095 4.01 8.25 4.973 8.25 6.108V8.25m0 0H4.875c-.621 0-1.125.504-1.125 1.125v11.25c0 .621.504 1.125 1.125 1.125h9.75c.621 0 1.125-.504 1.125-1.125V9.375c0-.621-.504-1.125-1.125-1.125H8.25Z',
		},
		{
			type: 'WEBHOOK',
			label: 'Webhook',
			description: 'POST events to a custom URL',
			color: 'text-orange-400',
			bg: 'bg-orange-500/10',
			icon: 'M13.19 8.688a4.5 4.5 0 0 1 1.242 7.244l-4.5 4.5a4.5 4.5 0 0 1-6.364-6.364l1.757-1.757m13.35-.622 1.757-1.757a4.5 4.5 0 0 0-6.364-6.364l-4.5 4.5a4.5 4.5 0 0 0 1.242 7.244',
		},
		{
			type: 'SIEM',
			label: 'SIEM',
			description: 'Forward to your SIEM platform',
			color: 'text-green-400',
			bg: 'bg-green-500/10',
			icon: 'M3.75 3v11.25A2.25 2.25 0 0 0 6 16.5h2.25M3.75 3h-1.5m1.5 0h16.5m0 0h1.5m-1.5 0v11.25A2.25 2.25 0 0 1 18 16.5h-2.25m-7.5 0h7.5m-7.5 0-1 3m8.5-3 1 3m0 0 .5 1.5m-.5-1.5h-9.5m0 0-.5 1.5',
		},
	];

	function selectType(type: IntegrationType) {
		selectedType = type;
		const opt = typeOptions.find(o => o.type === type);
		if (opt && !integrationName) {
			integrationName = `My ${opt.label} Integration`;
		}
		currentStep = 2;
	}

	function buildConfig(): Record<string, unknown> {
		switch (selectedType) {
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

	let canSubmit = $derived(() => {
		if (!selectedType || !integrationName.trim()) return false;
		switch (selectedType) {
			case 'EMAIL': return emailRecipients.trim().length > 0;
			case 'SLACK': return slackWebhookUrl.trim().length > 0;
			case 'JIRA': return jiraBaseUrl.trim().length > 0 && jiraProjectKey.trim().length > 0 && jiraApiToken.trim().length > 0;
			case 'WEBHOOK': return webhookUrl.trim().length > 0;
			case 'SIEM': return siemEndpoint.trim().length > 0 && siemApiKey.trim().length > 0;
			default: return false;
		}
	});

	async function handleSubmit() {
		if (!selectedType || !canSubmit()) return;

		isSubmitting = true;
		error = null;
		try {
			await integrationsApi.create({
				type: selectedType,
				name: integrationName.trim(),
				config: buildConfig(),
			});
			goto('/integrations');
		} catch (err) {
			const apiErr = err as ApiError;
			error = apiErr.message || 'Failed to create integration.';
		} finally {
			isSubmitting = false;
		}
	}
</script>

<svelte:head>
	<title>New Integration — Reconova</title>
</svelte:head>

<div class="max-w-3xl">
	<!-- Header -->
	<div class="mb-8">
		<a href="/integrations" class="text-brand text-sm font-medium hover:text-brand-dark transition-colors inline-flex items-center gap-1 mb-4">
			<svg class="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
				<path stroke-linecap="round" stroke-linejoin="round" d="M10.5 19.5 3 12m0 0 7.5-7.5M3 12h18" />
			</svg>
			Back to Integrations
		</a>
		<h1 class="text-2xl font-bold text-white">New Integration</h1>
		<p class="text-text-secondary text-sm mt-1">Connect an external service to receive alerts and forward findings</p>
	</div>

	{#if error}
		<div class="mb-4">
			<Alert variant="error">{error}</Alert>
		</div>
	{/if}

	<!-- Step indicators -->
	<div class="flex items-center gap-4 mb-6">
		<button
			class="flex items-center gap-2 text-sm font-medium transition-colors {currentStep >= 1 ? 'text-brand' : 'text-text-muted'}"
			onclick={() => { currentStep = 1; }}
		>
			<div class="w-7 h-7 rounded-full flex items-center justify-center text-xs font-bold {currentStep >= 1 ? 'bg-brand/20 text-brand' : 'bg-[rgba(255,255,255,0.05)] text-text-muted'}">1</div>
			Choose Type
		</button>
		<div class="flex-1 h-px bg-[rgba(255,255,255,0.08)]"></div>
		<div
			class="flex items-center gap-2 text-sm font-medium transition-colors {currentStep >= 2 ? 'text-brand' : 'text-text-muted'}"
		>
			<div class="w-7 h-7 rounded-full flex items-center justify-center text-xs font-bold {currentStep >= 2 ? 'bg-brand/20 text-brand' : 'bg-[rgba(255,255,255,0.05)] text-text-muted'}">2</div>
			Configure
		</div>
	</div>

	<!-- Step 1: Choose Type -->
	{#if currentStep === 1}
		<div class="grid grid-cols-1 sm:grid-cols-2 gap-3">
			{#each typeOptions as opt}
				<button
					class="flex items-start gap-4 p-5 rounded-xl border transition-all text-left {selectedType === opt.type ? 'border-brand/50 bg-brand/5' : 'border-[rgba(255,255,255,0.08)] bg-surface/60 backdrop-blur-sm hover:border-[rgba(255,255,255,0.16)]'}"
					onclick={() => selectType(opt.type)}
				>
					<div class="w-11 h-11 rounded-lg {opt.bg} flex items-center justify-center shrink-0">
						<svg class="w-5 h-5 {opt.color}" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="1.5">
							<path stroke-linecap="round" stroke-linejoin="round" d={opt.icon} />
						</svg>
					</div>
					<div>
						<p class="text-sm font-semibold text-white">{opt.label}</p>
						<p class="text-xs text-text-muted mt-0.5">{opt.description}</p>
					</div>
				</button>
			{/each}
		</div>
	{/if}

	<!-- Step 2: Configure -->
	{#if currentStep === 2 && selectedType}
		{@const selectedOpt = typeOptions.find(o => o.type === selectedType)}
		<div class="bg-surface/60 backdrop-blur-sm rounded-xl border border-[rgba(255,255,255,0.08)] p-6 mb-4">
			<!-- Selected type badge -->
			{#if selectedOpt}
				<div class="flex items-center gap-3 mb-6">
					<div class="w-10 h-10 rounded-lg {selectedOpt.bg} flex items-center justify-center">
						<svg class="w-5 h-5 {selectedOpt.color}" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="1.5">
							<path stroke-linecap="round" stroke-linejoin="round" d={selectedOpt.icon} />
						</svg>
					</div>
					<div>
						<p class="text-sm font-semibold text-white">{selectedOpt.label}</p>
						<p class="text-xs text-text-muted">{selectedOpt.description}</p>
					</div>
					<button
						class="ml-auto text-xs text-text-secondary hover:text-white transition-colors"
						onclick={() => { currentStep = 1; }}
					>
						Change
					</button>
				</div>
			{/if}

			<!-- Name -->
			<div class="mb-5">
				<TextInput
					id="integration-name"
					label="Integration Name"
					bind:value={integrationName}
					placeholder="My Integration"
				/>
			</div>

			<!-- Dynamic fields based on type -->
			{#if selectedType === 'EMAIL'}
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
			{:else if selectedType === 'SLACK'}
				<TextInput
					id="slack-webhook-url"
					label="Webhook URL"
					bind:value={slackWebhookUrl}
					placeholder="https://hooks.slack.com/services/..."
				/>
			{:else if selectedType === 'JIRA'}
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
			{:else if selectedType === 'WEBHOOK'}
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
			{:else if selectedType === 'SIEM'}
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

		<!-- Actions -->
		<div class="flex items-center justify-end gap-3">
			<Button variant="secondary" onclick={() => goto('/integrations')}>Cancel</Button>
			<Button
				variant="primary"
				onclick={handleSubmit}
				loading={isSubmitting}
				disabled={!canSubmit()}
			>
				Create Integration
			</Button>
		</div>
	{/if}
</div>
