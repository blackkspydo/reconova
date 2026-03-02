# Reconova - Comprehensive Business Rules Document

**Version:** 2.0
**Date:** 2026-03-01
**Status:** In Progress
**Scope:** System behavior rules + operational policies
**Reference:** `docs/plans/2026-03-01-reconova-prd-design.md`

> This document is the single source of truth for all business logic in Reconova. Every rule
> answers: **what** happens, **when**, under **what conditions**, and **who** can trigger it.

---

## Sections

| # | Section | File | Status |
|---|---------|------|--------|
| 1 | [Authentication & Account Security](./01-authentication-account-security.md) | `01-authentication-account-security.md` | Done |
| 2 | Tenant Management | `02-tenant-management.md` | Pending |
| 3 | Billing & Credits | `03-billing-credits.md` | Pending |
| 4 | Scanning & Workflows | `04-scanning-workflows.md` | Pending |
| 5 | Feature Flags & Access Control | `05-feature-flags-access-control.md` | Pending |
| 6 | Compliance Engine | `06-compliance-engine.md` | Pending |
| 7 | CVE Monitoring | `07-cve-monitoring.md` | Pending |
| 8 | Integrations | `08-integrations.md` | Pending |
| 9 | Super Admin & Operations | `09-super-admin-operations.md` | Pending |
| 10 | Data, Audit & Platform Compliance | `10-data-audit-platform-compliance.md` | Pending |
| 11 | System Configuration Reference | `11-system-configuration.md` | Pending |
| 12 | Error Response Schema | `12-error-response-schema.md` | Pending |
| 13 | Version History | `13-version-history.md` | Pending |

---

## Original Rules Reference

Source: `docs/plans/business-rule-archived.md` (135 rules across 10 domains)

| Domain | Rules | Range |
|--------|-------|-------|
| Authentication & Security | 15 | BR-AUTH-001 to BR-AUTH-015 |
| Tenant Management | 14 | BR-TNT-001 to BR-TNT-014 |
| Billing & Credits | 18 | BR-BILL-001 to BR-BILL-018 |
| Scanning & Workflows | 21 | BR-SCAN-001 to BR-SCAN-021 |
| Feature Flags | 7 | BR-FLAG-001 to BR-FLAG-007 |
| Compliance Engine | 11 | BR-COMP-001 to BR-COMP-011 |
| CVE Monitoring | 8 | BR-CVE-001 to BR-CVE-008 |
| Integrations | 12 | BR-INT-001 to BR-INT-012 |
| Super Admin & Operations | 10 | BR-ADM-001 to BR-ADM-010 |
| Data, Audit & Compliance | 19 | BR-DATA-001 to BR-DATA-019 |
| **Total** | **135** | |
