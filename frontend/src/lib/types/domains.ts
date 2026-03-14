export interface Domain {
	id: string;
	domain: string;
	status: 'ACTIVE' | 'PENDING_VERIFICATION' | 'VERIFIED';
	added_by: string;
	verified_at: string | null;
	created_at: string;
	last_scanned_at: string | null;
}

export interface DomainDetails extends Domain {
	subdomain_count: number;
	port_count: number;
	technology_count: number;
	latest_scan: import('./scans').ScanJob | null;
}

export interface Subdomain {
	id: string;
	domain_id: string;
	subdomain: string;
	source: string;
	first_seen: string;
	last_seen: string;
}

export interface Port {
	id: string;
	subdomain_id: string;
	subdomain_name: string;
	port: number;
	protocol: string;
	service: string | null;
	banner: string | null;
	discovered_at: string;
}

export interface Technology {
	id: string;
	subdomain_id: string;
	subdomain_name: string;
	tech_name: string;
	version: string | null;
	category: string;
	detected_at: string;
}
