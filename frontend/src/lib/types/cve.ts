export interface CveEntry {
	cve_id: string;
	description: string;
	severity: 'LOW' | 'MEDIUM' | 'HIGH' | 'CRITICAL';
	cvss_score: number | null;
	published_at: string;
	modified_at: string;
	references: string[];
	affected_products: string[];
}

export interface VulnerabilityAlert {
	id: string;
	tenant_id: string;
	cve_id: string;
	subdomain_name: string;
	domain_name: string;
	severity: 'LOW' | 'MEDIUM' | 'HIGH' | 'CRITICAL';
	description: string;
	status: 'NEW' | 'ACKNOWLEDGED' | 'RESOLVED';
	acknowledged_by: string | null;
	acknowledged_at: string | null;
	resolved_by: string | null;
	resolved_at: string | null;
	created_at: string;
}
