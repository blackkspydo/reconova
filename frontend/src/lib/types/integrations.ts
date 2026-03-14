export type IntegrationType = 'EMAIL' | 'SLACK' | 'JIRA' | 'WEBHOOK' | 'SIEM';

export interface Integration {
	id: string;
	tenant_id: string;
	type: IntegrationType;
	name: string;
	config: Record<string, unknown>;
	enabled: boolean;
	last_tested_at: string | null;
	last_test_success: boolean | null;
	created_at: string;
	updated_at: string;
}

export interface NotificationRule {
	id: string;
	tenant_id: string;
	integration_id: string;
	integration_name: string;
	event_type: string;
	severity_filter: ('LOW' | 'MEDIUM' | 'HIGH' | 'CRITICAL')[];
	enabled: boolean;
	created_at: string;
}

export type EventType =
	| 'SCAN_COMPLETED'
	| 'SCAN_FAILED'
	| 'VULNERABILITY_FOUND'
	| 'CVE_ALERT'
	| 'DOMAIN_VERIFIED'
	| 'CREDITS_LOW'
	| 'SCHEDULE_FAILED';
