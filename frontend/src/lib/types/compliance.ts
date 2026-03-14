export interface ComplianceFramework {
	id: string;
	name: string;
	description: string;
	version: string;
	control_count: number;
	status: 'ACTIVE' | 'DRAFT';
}

export interface ComplianceControl {
	id: string;
	framework_id: string;
	control_id: string;
	title: string;
	description: string;
	category: string;
}

export interface ComplianceAssessment {
	id: string;
	tenant_id: string;
	framework_id: string;
	framework_name: string;
	status: 'RUNNING' | 'COMPLETED' | 'FAILED';
	total_controls: number;
	passed_controls: number;
	failed_controls: number;
	score: number;
	started_at: string;
	completed_at: string | null;
	created_by: string;
}
