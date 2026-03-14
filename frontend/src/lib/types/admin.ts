export interface ConfigEntry {
	key: string;
	value: string;
	is_sensitive: boolean;
	category: string;
	description: string;
	updated_at: string;
	updated_by: string | null;
}

export interface ConfigHistory {
	id: string;
	key: string;
	old_value: string | null;
	new_value: string;
	changed_by: string;
	changed_at: string;
	reason: string | null;
}

export interface ConfigChangeRequest {
	id: string;
	key: string;
	proposed_value: string;
	reason: string;
	status: 'PENDING' | 'APPROVED' | 'REJECTED';
	requested_by: string;
	reviewed_by: string | null;
	created_at: string;
	reviewed_at: string | null;
}

export interface AuditLogEntry {
	id: string;
	actor_id: string;
	actor_email: string;
	action: string;
	resource_type: string;
	resource_id: string | null;
	details: Record<string, unknown> | null;
	ip_address: string | null;
	timestamp: string;
}

export interface FeatureFlag {
	id: string;
	key: string;
	name: string;
	description: string | null;
	enabled: boolean;
	tier_gated: boolean;
	min_tier: string | null;
	created_at: string;
	updated_at: string;
}

export interface FeatureFlagOverride {
	id: string;
	tenant_id: string;
	feature_flag_id: string;
	feature_flag_key: string;
	enabled: boolean;
	created_at: string;
}
