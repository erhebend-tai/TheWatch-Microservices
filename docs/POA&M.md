# TheWatch — Plan of Action & Milestones (POA&M)

> NIST 800-171 CA-5 compliance. Tracks all open security findings with remediation timeline.
> Updated: 2026-02-26

## Summary

| Severity | Total | Remediated | Open | Target Date |
|----------|-------|------------|------|-------------|
| CRITICAL | 9 | 0 | 9 | Sprint 1 (2 weeks) |
| HIGH | 22 | 0 | 22 | Sprint 2-3 (4 weeks) |
| MEDIUM | 15 | 0 | 15 | Sprint 4-5 (6 weeks) |
| LOW | 4 | 0 | 4 | Sprint 6 (8 weeks) |

## Open Findings

| ID | Finding | NIST Control | Severity | Status | Target Date | Owner | Evidence |
|----|---------|-------------|----------|--------|-------------|-------|----------|
| C-01 | .NET not running in FIPS mode | SC-13 | CRITICAL | Open | Sprint 1 | DevSecOps | runtimeconfig.json |
| C-02 | JWT symmetric key shared | SC-12, SC-13 | CRITICAL | Open | Sprint 1 | Backend Lead | WatchAuthExtensions.cs |
| C-04 | No TLS cipher suite restriction | SC-8 | CRITICAL | Open | Sprint 1 | DevSecOps | Kestrel config |
| C-05 | No mTLS between services | SC-8 | CRITICAL | Open | Sprint 1 | Infrastructure | WatchTlsExtensions.cs |
| D-01 | SQL Server TDE not configured | SC-28 | CRITICAL | Open | Sprint 1 | DBA | Terraform/Docker |
| D-02 | No column-level encryption | SC-28 | CRITICAL | Open | Sprint 1 | Backend Lead | FieldEncryptionService.cs |
| I-01 | Containers run as root | CM-7 | HIGH | Open | Sprint 1 | DevSecOps | All Dockerfiles |
| I-03 | Default passwords in compose | IA-5 | HIGH | Open | Sprint 1 | DevSecOps | docker-compose.yml |
| A-01 | Password min length = 8 | IA-5 | HIGH | Open | Sprint 2 | Backend Lead | P5 Program.cs |
| A-02 | No password history | IA-5 | HIGH | Open | Sprint 2 | Backend Lead | PasswordHistory entity |
| A-03 | No password max age | IA-5 | HIGH | Open | Sprint 2 | Backend Lead | WatchUser model |
| A-05 | No CAC/PIV auth | IA-2(12) | HIGH | Open | Sprint 2 | Backend Lead | WatchAuthExtensions.cs |
| A-06 | Lockout threshold = 5 | AC-7 | MEDIUM | Open | Sprint 2 | Backend Lead | P5 Program.cs |
| V-01 | No FluentValidation | SI-10 | HIGH | Open | Sprint 3 | All Teams | Validators/ dirs |
| V-02 | No global ProblemDetails | SI-11 | HIGH | Open | Sprint 3 | Backend Lead | WatchProblemDetailsMiddleware |
| S-01 | No SBOM | SR-4 | HIGH | Open | Sprint 4 | DevSecOps | CI pipeline |
| S-02 | No container signing | SA-10 | HIGH | Open | Sprint 4 | DevSecOps | Cosign |
| S-03 | No DAST | SA-11 | HIGH | Open | Sprint 4 | DevSecOps | ZAP pipeline |
| L-01 | Audit logs not signed | AU-9 | HIGH | Open | Sprint 5 | Backend Lead | LogIntegrityService |
| L-02 | No centralized SIEM | AU-6 | HIGH | Open | Sprint 5 | DevSecOps | Seq/Elastic |
| P-01 | No SSP | CA-1 | HIGH | Open | Sprint 6 | Security Lead | docs/ssp/ |
| P-02 | No POA&M | CA-5 | HIGH | Open | Sprint 6 | Security Lead | This document |

## Risk Acceptances

| ID | Risk | Compensating Control | Accepted By | Review Date |
|----|------|---------------------|-------------|-------------|
| C-03 | Argon2id not FIPS-validated | Argon2id is cryptographically superior; FIPS PBKDF2 fallback available via config toggle | TBD | Quarterly |

## Revision History

| Date | Author | Changes |
|------|--------|---------|
| 2026-02-26 | Automated Analysis | Initial POA&M from DOD_SECURITY_ANALYSIS.md |
