export type ScanJobStatus = 'QUEUED' | 'RUNNING' | 'COMPLETED' | 'PARTIAL' | 'FAILED' | 'CANCELLED';
export type StepStatus = 'PENDING' | 'RUNNING' | 'RETRYING' | 'COMPLETED' | 'FAILED' | 'SKIPPED' | 'CANCELLED';

export interface ScanJob {
	id: string;
	domain_id: string;
	domain_name: string;
	workflow_id: string;
	workflow_name: string;
	status: ScanJobStatus;
	steps: ScanStep[];
	total_credits: number;
	current_step: number | null;
	started_at: string | null;
	completed_at: string | null;
	created_at: string;
	created_by: string;
	cancelled_by: string | null;
	cancellation_reason: string | null;
}

export interface ScanStep {
	index: number;
	check_type: string;
	status: StepStatus;
	credits: number;
	attempt: number;
	max_attempts: number;
	duration_seconds: number | null;
	error: string | null;
}

export interface ScanResults {
	scan_job_id: string;
	subdomains: import('./domains').Subdomain[];
	ports: import('./domains').Port[];
	technologies: import('./domains').Technology[];
	vulnerabilities: Vulnerability[];
	screenshots: Screenshot[];
}

export interface Vulnerability {
	id: string;
	scan_result_id: string;
	subdomain_name: string;
	cve: string | null;
	severity: 'LOW' | 'MEDIUM' | 'HIGH' | 'CRITICAL';
	description: string;
	remediation: string | null;
	created_at: string;
}

export interface Screenshot {
	id: string;
	subdomain_id: string;
	subdomain_name: string;
	url: string;
	storage_path: string;
	image_url: string;
	taken_at: string;
}

export interface ScanSchedule {
	id: string;
	domain_id: string;
	domain_name: string;
	workflow_id: string;
	workflow_name: string;
	cron_expression: string;
	cron_human: string;
	enabled: boolean;
	disabled_reason: string | null;
	estimated_credits: number;
	last_run_at: string | null;
	next_run_at: string | null;
	created_at: string;
	created_by: string;
}

export interface Workflow {
	id: string;
	name: string;
	template_id: string | null;
	steps_json: WorkflowStepDefinition[];
	is_system: boolean;
	description: string | null;
	created_by: string;
	created_at: string;
	updated_at: string;
}

export interface WorkflowStepDefinition {
	check_type: string;
	config?: Record<string, unknown>;
}

export interface WorkflowTemplate {
	id: string;
	name: string;
	description: string | null;
	steps_json: WorkflowStepDefinition[];
	is_system: boolean;
}

export interface CreditEstimate {
	estimated_cost: number;
	available_credits: number;
	sufficient: boolean;
	shortfall: number;
	breakdown: CreditEstimateStep[];
}

export interface CreditEstimateStep {
	check_type: string;
	credits_per_domain: number;
	domain_count: number;
	subtotal: number;
}

export interface Paginated<T> {
	data: T[];
	total: number;
	page: number;
	page_size: number;
	total_pages: number;
}
