# Reconova

A B2B SaaS multi-tenant reconnaissance and compliance platform. Businesses register, add their target domains, run full reconnaissance and vulnerability scans, and receive compliance readiness reports (SOC 2, NIST) — without needing to manage security tooling or API integrations.

## Architecture

**Modular monolith with scan worker separation.**

```
SvelteKit Frontend
        |
.NET API (single process, modular)
├── Control Plane Module (auth, tenants, billing, feature flags, super admin)
├── Tenant Module (domains, scans, results, workflows, compliance, integrations)
        |
Redis Queue
        |
Scan Worker(s) (separate process, horizontally scalable)
├── Internal Modules (subdomain enum, port scan, HTTP probe, etc.)
├── External Tool Adapters (subfinder, amass, nmap, httpx, nuclei)
└── API Integrations (Shodan, SecurityTrails, Censys, VirusTotal)
```

- Single .NET solution with clear module boundaries
- Scan Workers run as separate processes consuming from a Redis queue, scaling horizontally based on queue depth
- Database-per-tenant isolation with PostgreSQL

## Tech Stack

- **Backend:** .NET 9 (ASP.NET Core), EF Core
- **Frontend:** SvelteKit
- **Database:** PostgreSQL (database-per-tenant)
- **Cache/Queue:** Redis
- **Payments:** Stripe
- **Containerization:** Docker

## Project Structure

```
Reconova.sln
├── src/
│   ├── Reconova.Api/                    # ASP.NET Core Web API host
│   ├── Reconova.ControlPlane/           # Control plane services (tenants, auth, billing, flags)
│   ├── Reconova.ControlPlane.Data/      # EF Core DbContext + entities for control DB
│   ├── Reconova.Tenant/                 # Tenant module services (domains, scans, workflows)
│   ├── Reconova.Tenant.Data/            # EF Core DbContext + entities for tenant DBs
│   ├── Reconova.ScanWorker/             # Background worker host (consumes Redis queue)
│   ├── Reconova.ScanEngine/             # Scan pipeline, tool adapters, recon modules
│   └── Reconova.Shared/                 # Shared DTOs, interfaces, enums, constants
├── tests/
│   ├── Reconova.ControlPlane.Tests/
│   ├── Reconova.Tenant.Tests/
│   ├── Reconova.ScanEngine.Tests/
│   └── Reconova.Integration.Tests/
├── frontend/                            # SvelteKit application
├── docker/
│   └── docker-compose.yml
└── docs/plans/                          # Design docs, business rules, and implementation plans
```

## Key Features

- **Multi-Tenant Isolation** — Each tenant gets a dedicated PostgreSQL database cloned from a template
- **Automated Reconnaissance** — Subdomain enumeration, port scanning, HTTP probing, and more
- **Vulnerability Scanning** — Integration with Nuclei, Nmap, and other security tools
- **Compliance Engine** — SOC 2, NIST framework mapping with readiness reports
- **CVE Monitoring** — Track and alert on CVEs relevant to discovered assets
- **Credit-Based Billing** — Stripe-powered subscriptions with credit packs for scan operations
- **Feature Flags** — Subscription-based and operational feature gating per tenant
- **Integrations** — Slack, Jira, webhook notifications for scan results and alerts
- **Super Admin Dashboard** — Platform-wide tenant management, analytics, and operations

## Documentation

- [Product Requirements & Design](docs/plans/2026-03-01-reconova-prd-design.md)
- [Implementation Plan](docs/plans/2026-03-01-reconova-implementation-plan.md)
- [Business Rules Index](docs/plans/business-rules/00-index.md) — 135 rules across 13 sections covering auth, tenants, billing, scanning, compliance, and more

## License

Proprietary. All rights reserved.
