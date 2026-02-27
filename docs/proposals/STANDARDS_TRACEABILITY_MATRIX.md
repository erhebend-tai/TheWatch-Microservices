# Standards Traceability Matrix — TheWatch Platform

> **Classification:** CUI // SP-BASIC  
> **Purpose:** Complete mapping of ALL applicable standards, controls, and requirements
> to system evidence within the TheWatch repository  
> **Document ID:** STM-001  
> **Version:** 1.0

---

## 1. Matrix Overview

This Standards Traceability Matrix (STM) provides a single, comprehensive reference
that maps every applicable DoD and federal standard to specific implementation evidence
within the TheWatch codebase, CI/CD pipeline, and documentation.

### 1.1 Standards Covered

| # | Standard | Controls/Practices | Primary Proposal Document |
|---|----------|-------------------|--------------------------|
| 1 | NIST SP 800-171 Rev 2 | 110 requirements (14 families) | [System Security Plan](SYSTEM_SECURITY_PLAN.md) |
| 2 | CMMC 2.0 Level 2 | 110 practices (14 domains) | [CMMC Compliance Matrix](CMMC_LEVEL2_COMPLIANCE.md) |
| 3 | NIST SP 800-53 Rev 5 | Selected controls (20 families) | [System Security Plan](SYSTEM_SECURITY_PLAN.md) |
| 4 | NIST SP 800-218 (SSDF v1.1) | 21 tasks (4 groups) | [Supply Chain Risk Mgmt](SUPPLY_CHAIN_RISK_MANAGEMENT.md) |
| 5 | DISA STIG | 15+ key findings | [CMMC Compliance Matrix](CMMC_LEVEL2_COMPLIANCE.md) |
| 6 | DFARS 252.204-7012 | CUI safeguarding + incident reporting | [Data Protection Plan](DATA_PROTECTION_PLAN.md) |
| 7 | HIPAA Security Rule | Technical safeguards (§164.312) | [Data Protection Plan](DATA_PROTECTION_PLAN.md) |
| 8 | Executive Order 14028 | Software supply chain requirements | [Supply Chain Risk Mgmt](SUPPLY_CHAIN_RISK_MANAGEMENT.md) |
| 9 | OMB M-22-18 | SSDF self-attestation | [Supply Chain Risk Mgmt](SUPPLY_CHAIN_RISK_MANAGEMENT.md) |
| 10 | FIPS 140-2/3 | Cryptographic module validation | [System Security Plan](SYSTEM_SECURITY_PLAN.md) |
| 11 | OWASP Top 10:2021 | 10 web application risks | [Testing & QA](TESTING_QUALITY_ASSURANCE.md) |
| 12 | OWASP ASVS v4.0 | 14 verification chapters | [Testing & QA](TESTING_QUALITY_ASSURANCE.md) |
| 13 | SLSA v1.0 | Build integrity levels | [Supply Chain Risk Mgmt](SUPPLY_CHAIN_RISK_MANAGEMENT.md) |
| 14 | NIST SP 800-137 | Continuous monitoring | [Continuous Monitoring](CONTINUOUS_MONITORING_STRATEGY.md) |
| 15 | NIST SP 800-61 Rev 2 | Incident handling | [Incident Response](INCIDENT_RESPONSE_CONTINUITY.md) |
| 16 | NIST SP 800-161 Rev 1 | Supply chain risk management | [Supply Chain Risk Mgmt](SUPPLY_CHAIN_RISK_MANAGEMENT.md) |
| 17 | NIST SP 800-115 | Security testing | [Testing & QA](TESTING_QUALITY_ASSURANCE.md) |
| 18 | NIST SP 800-207 | Zero Trust Architecture | [Technical Volume](TECHNICAL_VOLUME.md) |

---

## 2. NIST SP 800-171 Rev 2 — Full Traceability

### 2.1 Access Control (AC) — 22 Requirements

| Req | Description | Status | Primary Evidence | Secondary Evidence |
|-----|-------------|--------|------------------|-------------------|
| 3.1.1 | Authorized access | **[MET]** | `TheWatch.Admin.RestAPI/Program.cs` (JWT config) | `TheWatch.Shared/Auth/WatchAuthExtensions.cs` |
| 3.1.2 | Transaction control | **[MET]** | `TheWatch.Admin.RestAPI/Program.cs` (6 RBAC policies) | `docs/policies/access-control-policy.md` |
| 3.1.3 | CUI flow | **[MET]** | `TheWatch.Shared/Security/CuiMarkingMiddleware.cs` | `docs/data-classification-matrix.md` |
| 3.1.4 | Separation of duties | **[MET]** | `docs/policies/access-control-policy.md` | 6 defined roles |
| 3.1.5 | Least privilege | **[MET]** | `TheWatch.Admin.RestAPI/Controllers/` (IDOR checks) | Dockerfiles (non-root UID 1001) |
| 3.1.6 | Non-privileged accounts | **[MET]** | Dockerfiles (`USER appuser`) | DISA STIG V-222425 |
| 3.1.7 | Privileged function prevention | **[MET]** | `[Authorize(Policy = "AdminOnly")]` | Controller attributes |
| 3.1.8 | Unsuccessful logon attempts | **[MET]** | `TheWatch.P5.AuthSecurity/` (3-attempt lockout) | DISA STIG V-222534 |
| 3.1.9 | Privacy/security notices | **[MET]** | `TheWatch.P5.AuthSecurity/` (EULA endpoints) | IP logging |
| 3.1.10 | Session lock | **[MET]** | JWT 30-min expiration | Refresh token 8-hour max |
| 3.1.11 | Session termination | **[MET]** | Token revocation on logout | Password change invalidation |
| 3.1.12 | Remote access control | **[MET]** | `TheWatch.Shared/Security/WatchKestrelExtensions.cs` | Rate limiting middleware |
| 3.1.13 | Remote access encryption | **[MET]** | TLS 1.2/1.3 only | HSTS 365d preload |
| 3.1.14 | Managed access points | **[MET]** | P1 CoreGateway routing | `docker-compose.yml` |
| 3.1.15 | Remote privileged commands | **[MET]** | `TheWatch.Admin.CLI/` (JWT required) | Admin REST API auth |
| 3.1.16 | Wireless access auth | **[MET]** | Mobile JWT auth | BLE mesh device registration |
| 3.1.17 | Wireless access protection | **[MET]** | TLS enforcement | Mobile certificate pinning |
| 3.1.18 | Mobile device control | **[MET]** | Device trust scoring | `TheWatch.Mobile/Services/BiometricGateService.cs` |
| 3.1.19 | Encrypt CUI on mobile | **[PARTIAL]** | `TheWatch.Mobile/Data/WatchLocalDbContext.cs` | SQLite encryption planned |
| 3.1.20 | External connections | **[MET]** | `TheWatch.Contracts.*/` (typed clients) | Explicit endpoint config |
| 3.1.21 | Portable storage | **[MET]** | `TheWatch.Mobile/Services/ChainOfCustodyService.cs` | Evidence hash chain |
| 3.1.22 | Public CUI control | **[MET]** | CUI marking middleware | Auth required on CUI routes |

### 2.2 Awareness and Training (AT) — 3 Requirements

| Req | Description | Status | Evidence |
|-----|-------------|--------|----------|
| 3.2.1 | Risk awareness | **[PARTIAL]** | `docs/developer-setup.md`, `docs/policies/` |
| 3.2.2 | Role-based training | **[PARTIAL]** | `docs/developer-setup.md` (signed commit training) |
| 3.2.3 | Insider threat awareness | **[PARTIAL]** | `docs/incident-response-plan.md` (threat categories) |

### 2.3 Audit and Accountability (AU) — 9 Requirements

| Req | Description | Status | Evidence |
|-----|-------------|--------|----------|
| 3.3.1 | Audit logs | **[MET]** | Serilog JSON across all services |
| 3.3.2 | User accountability | **[MET]** | JWT claims in audit records |
| 3.3.3 | Log review | **[MET]** | `TheWatch.Admin/Pages/AuditLog.razor`, `docs/policies/audit-accountability-policy.md` |
| 3.3.4 | Audit failure alerts | **[PARTIAL]** | Health checks; alerting integration planned |
| 3.3.5 | Correlation | **[MET]** | Correlation ID middleware |
| 3.3.6 | Reduction & reporting | **[MET]** | Structured JSON; admin dashboard |
| 3.3.7 | Clock sync | **[MET]** | Container NTP; UTC timestamps |
| 3.3.8 | Protect audit info | **[MET]** | AdminOnly policy on audit endpoints |
| 3.3.9 | Audit management | **[MET]** | Admin-only configuration |

### 2.4 Configuration Management (CM) — 9 Requirements

| Req | Description | Status | Evidence |
|-----|-------------|--------|----------|
| 3.4.1 | Baselines | **[MET]** | `Directory.Build.props`, `Directory.Packages.props` |
| 3.4.2 | Security settings | **[MET]** | `TheWatch.Shared/Security/WatchKestrelExtensions.cs` |
| 3.4.3 | Change tracking | **[MET]** | `docs/developer-setup.md` (branch protection) |
| 3.4.4 | Security impact | **[MET]** | `.github/workflows/security.yml` |
| 3.4.5 | Access restrictions | **[MET]** | `docs/policies/access-control-policy.md` |
| 3.4.6 | Least functionality | **[MET]** | Dockerfiles (multi-stage, minimal) |
| 3.4.7 | Nonessential programs | **[MET]** | Runtime-only containers |
| 3.4.8 | Deny-by-exception | **[MET]** | NuGetAudit fail-on-low |
| 3.4.9 | User software control | **[MET]** | Immutable containers; `renovate.json` |

### 2.5 Identification and Authentication (IA) — 11 Requirements

| Req | Description | Status | Evidence |
|-----|-------------|--------|----------|
| 3.5.1 | Identify users | **[MET]** | `TheWatch.P5.AuthSecurity/` |
| 3.5.2 | Authenticate users/devices | **[MET]** | JWT + MFA (4 methods) + device trust |
| 3.5.3 | Multi-factor auth | **[MET]** | TOTP, FIDO2, SMS, Magic Link |
| 3.5.4 | Replay-resistant auth | **[MET]** | JWT jti/iat/exp claims |
| 3.5.5 | Identifier reuse prevention | **[MET]** | Database unique constraints |
| 3.5.6 | Identifier inactivity | **[PARTIAL]** | Account deactivation; inactivity monitoring planned |
| 3.5.7 | Password complexity | **[PARTIAL]** | 8-char min → POA&M: 15-char (STIG V-222524) |
| 3.5.8 | Password reuse | **[PLANNED]** | POA&M: 24-gen history (STIG V-222546) |
| 3.5.9 | Temporary passwords | **[MET]** | Forced change on first login |
| 3.5.10 | Crypto password protection | **[MET]** | Argon2id + PBKDF2-SHA512 |
| 3.5.11 | Auth feedback | **[MET]** | Masked fields; generic errors |

### 2.6 Incident Response (IR) — 3 Requirements

| Req | Description | Status | Evidence |
|-----|-------------|--------|----------|
| 3.6.1 | Incident handling | **[MET]** | `docs/incident-response-plan.md` |
| 3.6.2 | Incident tracking/reporting | **[MET]** | 72h DoD reporting; post-incident review |
| 3.6.3 | IR testing | **[PARTIAL]** | Tabletop exercises planned |

### 2.7 Maintenance (MA) — 6 Requirements

| Req | Description | Status | Evidence |
|-----|-------------|--------|----------|
| 3.7.1 | System maintenance | **[MET]** | `renovate.json`; NuGet audit |
| 3.7.2 | Maintenance tool controls | **[MET]** | Ephemeral CI runners; version pinning |
| 3.7.3 | Equipment sanitization | **[MET]** | Stateless containers rebuilt per deploy |
| 3.7.4 | Diagnostic media check | **[PARTIAL]** | SBOM generation; NuGet signing planned |
| 3.7.5 | Nonlocal maintenance MFA | **[MET]** | Admin JWT + MFA |
| 3.7.6 | Maintenance supervision | **[MET]** | GitHub audit; PR reviews |

### 2.8 Media Protection (MP) — 9 Requirements

| Req | Description | Status | Evidence |
|-----|-------------|--------|----------|
| 3.8.1 | Media protection | **[PARTIAL]** | `TheWatch.Shared/Security/FieldEncryptionService.cs`; TDE planned |
| 3.8.2 | CUI media access | **[MET]** | RBAC + CUI marking |
| 3.8.3 | Media sanitization | **[PARTIAL]** | Stateless containers; backup sanitization planned |
| 3.8.4 | CUI media marking | **[MET]** | CUI headers in HTTP responses |
| 3.8.5 | Controlled area access | **[MET]** | Cloud provider security; K8s namespaces |
| 3.8.6 | Portable media encryption | **[MET]** | TLS transport; SHA-256 evidence hashing |
| 3.8.7 | Removable media control | **[MET]** | Chain-of-custody required |
| 3.8.8 | Shared media prohibition | **[MET]** | Evidence tagged (user, device, GPS) |
| 3.8.9 | Backup CUI protection | **[PARTIAL]** | Terraform encrypted backups |

### 2.9–2.14 Remaining Families

| Family | Reqs | Met | Partial | Planned | Evidence Reference |
|--------|------|-----|---------|---------|-------------------|
| **PS** Personnel Security | 2 | 2 | 0 | 0 | `TheWatch.P5.AuthSecurity/` |
| **PE** Physical & Environmental | 6 | 6 | 0 | 0 | Cloud provider (inherited) |
| **RA** Risk Assessment | 3 | 3 | 0 | 0 | `DOD_SECURITY_ANALYSIS.md`, `.github/workflows/security.yml` |
| **CA** Security Assessment | 4 | 4 | 0 | 0 | `docs/POA&M.md`, `docs/pentest-program.md` |
| **SC** System & Communications | 16 | 10 | 4 | 0 | `TheWatch.Shared/`, Kestrel hardening |
| **SI** System & Info Integrity | 7 | 7 | 0 | 0 | `.github/workflows/security.yml`, VulnerabilityMonitorService |

---

## 3. NIST SP 800-53 Rev 5 — Key Controls

| Control | Family | Description | Implementation | Evidence |
|---------|--------|-------------|----------------|----------|
| AC-2 | Access Control | Account Management | User registration, role assignment, account deactivation | `TheWatch.P5.AuthSecurity/` |
| AC-3 | Access Control | Access Enforcement | RBAC with 6 policies | `TheWatch.Admin.RestAPI/Program.cs` |
| AC-6 | Access Control | Least Privilege | Non-root containers; scoped service accounts | Dockerfiles |
| AC-7 | Access Control | Unsuccessful Logon | 3-attempt lockout → escalation | `TheWatch.P5.AuthSecurity/` |
| AU-2 | Audit | Auditable Events | 7 event categories logged | `docs/policies/audit-accountability-policy.md` |
| AU-3 | Audit | Content of Audit Records | Timestamp, user, action, result, correlation ID | Serilog configuration |
| AU-6 | Audit | Audit Review & Analysis | Weekly/monthly/quarterly reviews | Admin Portal |
| AU-9 | Audit | Audit Record Protection | HMAC-SHA256 signing; hash chaining | Audit policy |
| AU-11 | Audit | Audit Retention | 1 year security; 90 days general | Retention policies |
| CA-2 | Assessment | Security Assessments | DoD compliance workflows; pentest program | `.github/workflows/dod-*.yml` |
| CA-5 | Assessment | POA&M | 50 findings tracked with sprints | `docs/POA&M.md` |
| CA-7 | Assessment | Continuous Monitoring | VulnerabilityMonitorService + automated workflows | ConMon strategy |
| CA-8 | Assessment | Penetration Testing | Annual pentest program with ROE | `docs/pentest-program.md` |
| CM-2 | Config Mgmt | Baseline Configuration | `Directory.Build.props`, Docker image pinning | Build configuration |
| CM-3 | Config Mgmt | Configuration Change Control | Branch protection; signed commits; 2 approvals | `docs/developer-setup.md` |
| CM-7 | Config Mgmt | Least Functionality | Multi-stage Docker; runtime-only images | Dockerfiles |
| CP-2 | Contingency | Contingency Planning | Multi-replica; auto-scaling; offline mobile | K8s + KEDA + mobile |
| CP-7 | Contingency | Alternate Processing | Multi-cloud (Azure/AWS/GCP) | `terraform/` |
| CP-9 | Contingency | Information System Backup | Geo-replicated databases; automated backups | `terraform/azure/main.tf` |
| IA-2 | Auth | Identification & Auth | JWT + MFA (4 methods) + device trust | `TheWatch.P5.AuthSecurity/` |
| IA-5 | Auth | Authenticator Management | Argon2id/PBKDF2; brute force protection | `TheWatch.Shared/Security/` |
| IR-1 | Incident Response | IR Policy & Procedures | 5-category IR plan; 4 severity levels | `docs/incident-response-plan.md` |
| IR-4 | Incident Response | Incident Handling | Detection, containment, eradication, recovery | IR plan |
| IR-6 | Incident Response | Incident Reporting | 72h DoD/DC3 reporting | IR plan |
| PM-17 | Program Mgmt | IoT Security | Wearable device provisioning; device trust | `TheWatch.P4.Wearable/` |
| RA-3 | Risk Assessment | Risk Assessment | STRIDE + MITRE ATT&CK | `DOD_SECURITY_ANALYSIS.md` |
| RA-5 | Risk Assessment | Vulnerability Monitoring | CodeQL, Trivy, NuGet, CISA KEV monitoring | `.github/workflows/security.yml` |
| SA-11 | System & Services | Developer Testing | 12 test projects; CodeQL; Trivy | Test projects + CI workflows |
| SA-15 | System & Services | Development Process | Roslyn generators; .NET analyzers | `TheWatch.Generators/` |
| SC-7 | System & Comm | Boundary Protection | CoreGateway; rate limiting; CORS | `TheWatch.P1.CoreGateway/` |
| SC-8 | System & Comm | Transmission Confidentiality | TLS 1.2/1.3; HSTS; certificate pinning | Kestrel hardening |
| SC-12 | System & Comm | Key Management | RSA-2048 JWT; AES-256; Key Vault planned | Auth configuration |
| SC-13 | System & Comm | Cryptographic Protection | AES-256-GCM; Argon2id/PBKDF2; TLS | `TheWatch.Shared/Security/` |
| SC-28 | System & Comm | Protection at Rest | Field encryption; TDE planned | `FieldEncryptionService.cs` |
| SI-2 | System & Info | Flaw Remediation | Renovate; SLA remediation; NuGet audit | `renovate.json` |
| SI-4 | System & Info | System Monitoring | Prometheus + Grafana + AlertManager | `infra/monitoring/` |
| SI-7 | System & Info | Software Integrity | SLSA provenance; signed commits | `.github/workflows/slsa-provenance.yml` |
| SI-10 | System & Info | Information Input Validation | FluentValidation planned; EF Core parameterized | POA&M |
| SR-3 | Supply Chain | Supply Chain Controls | NuGet audit; dependency review | `Directory.Build.props` |
| SR-4 | Supply Chain | Provenance | SLSA; SBOM; signed commits | CI workflows |

---

## 4. DISA STIG — Key Findings Traceability

| STIG ID | CAT | Requirement | Status | Evidence |
|---------|-----|-------------|--------|----------|
| V-222425 | I | Non-root container execution | **[MET]** | Dockerfiles: `USER appuser` UID 1001 |
| V-222427 | I | Least privilege enforcement | **[MET]** | RBAC + IDOR prevention |
| V-222524 | II | Password min 15 chars | **[PARTIAL]** | Currently 8; POA&M Sprint 3 |
| V-222530 | II | Multi-factor authentication | **[MET]** | 4 MFA methods |
| V-222534 | II | Login attempt limiting (3) | **[MET]** | 3-attempt lockout |
| V-222535 | II | Session timeout (30 min) | **[MET]** | JWT 30-min expiration |
| V-222536 | II | Remote access control | **[MET]** | TLS + rate limiting |
| V-222545 | II | Password max age (60d) | **[PLANNED]** | POA&M Sprint 3 |
| V-222546 | II | Password history (24 gen) | **[PLANNED]** | POA&M Sprint 3 |
| V-222457 | II | Audit logging | **[MET]** | Serilog all services |
| V-222458 | II | User accountability in logs | **[MET]** | JWT claims in records |
| V-222602 | I | Boundary protection | **[MET]** | CoreGateway + rate limiting |
| V-222603 | I | Transmission encryption | **[MET]** | TLS 1.2/1.3 only |
| V-222604 | I | FIPS cryptography | **[PARTIAL]** | PBKDF2 fallback available |
| V-222605 | I | CUI at rest encryption | **[PARTIAL]** | Field encryption; TDE planned |

---

## 5. DFARS 252.204-7012 — Traceability

| Requirement | Section | Implementation | Evidence |
|-------------|---------|----------------|----------|
| Adequate security for CUI | (b) | NIST 800-171 controls implemented (see §2) | SSP + CMMC Matrix |
| Cyber incident reporting | (c)(1) | 72h reporting to DC3 | `docs/incident-response-plan.md` |
| Malicious software | (c)(2) | CodeQL + Trivy + Gitleaks | `.github/workflows/security.yml` |
| Media preservation | (c)(3) | 90-day forensic preservation | IR plan |
| Cyber incident damage assessment | (c)(4) | Post-incident review process | IR plan |
| Subcontractor flow-down | (m) | CUI marking on all data flows | `CuiMarkingMiddleware.cs` |

---

## 6. HIPAA Security Rule — Technical Safeguards

| Section | Requirement | Implementation | Evidence |
|---------|-------------|----------------|----------|
| §164.312(a)(1) | Access Control | RBAC with DoctorAccess policy; MFA mandatory | RBAC configuration |
| §164.312(a)(2)(i) | Unique User ID | Unique IDs in all PHI access logs | JWT claims |
| §164.312(a)(2)(ii) | Emergency Access | Emergency override with audit trail | P2 VoiceEmergency |
| §164.312(a)(2)(iii) | Automatic Logoff | JWT 30-min expiration | JWT configuration |
| §164.312(a)(2)(iv) | Encryption/Decryption | AES-256-GCM for PHI fields | `FieldEncryptionService.cs` |
| §164.312(b) | Audit Controls | PHI access logging via `HipaaComplianceService` | `HipaaComplianceService.cs` |
| §164.312(c)(1) | Integrity Controls | HMAC-signed audit records | Audit policy |
| §164.312(d) | Person/Entity Auth | MFA for PHI roles | Auth policies |
| §164.312(e)(1) | Transmission Security | TLS 1.2+ enforced | Kestrel config |
| §164.312(e)(2)(ii) | Encryption | TLS 1.2/1.3 for all PHI transmission | Kestrel config |
| §164.514(b) | Safe Harbor De-ID | 18-identifier redaction | `HipaaComplianceService.cs` |

---

## 7. Executive Order 14028 & OMB M-22-18

| Requirement | Section | Implementation | Evidence |
|-------------|---------|----------------|----------|
| SBOM for federal software | EO §4(e) | CycloneDX + SPDX per-project SBOMs | `generate-sbom.sh`, `.github/workflows/sbom-aggregate.yml` |
| SSDF self-attestation | M-22-18 | SSDF attestation covering PO, PS, PW, RV | `docs/ssdf-attestation.md` |
| Zero trust architecture | EO §3 | JWT auth everywhere; no implicit trust | `TheWatch.Shared/Security/` |
| Secure software development | EO §4(e) | SSDF practices implemented | `docs/ssdf-attestation.md` |
| Supply chain security | EO §4 | SLSA provenance; signed commits; SBOM | CI workflows |
| Encryption of data at rest | EO §3 | AES-256-GCM field encryption | `FieldEncryptionService.cs` |
| MFA deployment | EO §3 | 4 MFA methods implemented | `TheWatch.P5.AuthSecurity/` |

---

## 8. OWASP Top 10:2021

| Risk | Controls | Evidence |
|------|----------|----------|
| **A01** Broken Access Control | RBAC (6 policies); IDOR checks; JWT auth | `TheWatch.Admin.RestAPI/Program.cs` |
| **A02** Cryptographic Failures | AES-256-GCM; TLS 1.2+; Argon2id/PBKDF2 | `TheWatch.Shared/Security/` |
| **A03** Injection | EF Core parameterized queries; CodeQL | EF Core usage |
| **A04** Insecure Design | STRIDE; MITRE ATT&CK; microservices isolation | Threat model |
| **A05** Security Misconfiguration | Kestrel hardening; no Server header; CORS whitelist | Kestrel config |
| **A06** Vulnerable Components | NuGet Audit; Trivy; Renovate; SBOM | CI pipeline |
| **A07** Auth Failures | MFA; brute force protection; device trust | `TheWatch.P5.AuthSecurity/` |
| **A08** Software & Data Integrity | Signed commits; SLSA provenance; SBOM | CI workflows |
| **A09** Logging & Monitoring | Serilog; correlation IDs; STRIDE scanning | All services |
| **A10** SSRF | Typed HTTP clients; no dynamic URL from user input | `TheWatch.Contracts.*/` |

---

## 9. SLSA v1.0 & SSDF v1.1

### SLSA Levels

| Requirement | Status | Evidence |
|-------------|--------|----------|
| Source — Version controlled | **[MET]** | Git repository |
| Source — Verified history | **[MET]** | Signed commits; branch protection |
| Build — Scripted build | **[MET]** | GitHub Actions workflows |
| Build — Build service | **[MET]** | GitHub-hosted ephemeral runners |
| Build — Provenance | **[MET]** | `.github/workflows/slsa-provenance.yml` |

### SSDF Practice Groups

| Group | Practices | Status | Evidence |
|-------|-----------|--------|----------|
| **PO** Prepare | PO.1–PO.5 | **[MET]** | Threat modeling, toolchain, secure environments |
| **PS** Protect | PS.1–PS.3 | **[MET]** | Signed commits, SBOM, release archives |
| **PW** Produce | PW.1–PW.9 | **[MET]** | Zero trust, analyzers, testing, secure defaults |
| **RV** Respond | RV.1–RV.3 | **[MET]** | Vulnerability monitoring, SLA remediation, RCA |

---

## 10. Evidence Artifact Index

### 10.1 Repository Source Code Evidence

| Evidence | Path | Standards Served |
|----------|------|-----------------|
| JWT Authentication | `TheWatch.Shared/Auth/WatchAuthExtensions.cs` | NIST 800-171 IA, AC; CMMC IA, AC |
| Argon2id Password Hashing | `TheWatch.P5.AuthSecurity/Security/Argon2PasswordHasher.cs` | NIST 800-53 IA-5; OWASP A07 |
| PBKDF2-SHA512 FIPS Hashing | `TheWatch.Shared/Security/FipsPbkdf2PasswordHasher.cs` | FIPS 140-2; NIST 800-171 SC-13 |
| AES-256-GCM Field Encryption | `TheWatch.Shared/Security/FieldEncryptionService.cs` | NIST 800-171 SC-28; HIPAA §164.312(a)(2)(iv) |
| Kestrel TLS Hardening | `TheWatch.Shared/Security/WatchKestrelExtensions.cs` | NIST 800-171 SC-8; DISA STIG |
| CUI Marking Middleware | `TheWatch.Shared/Security/CuiMarkingMiddleware.cs` | 32 CFR Part 2002; NIST 800-171 MP |
| PII Redaction Middleware | `TheWatch.Shared/Security/PiiRedactionMiddleware.cs` | NIST 800-53 AU-3; HIPAA §164.514 |
| HIPAA Compliance Service | `TheWatch.Shared/Compliance/HipaaComplianceService.cs` | HIPAA Security Rule |
| Chain of Custody | `TheWatch.Mobile/Services/ChainOfCustodyService.cs` | NIST 800-53 AU-10 |
| Biometric Gate | `TheWatch.Mobile/Services/BiometricGateService.cs` | NIST 800-53 IA-2(12) |
| Non-root Dockerfile | `TheWatch.P1.CoreGateway/Dockerfile` (pattern) | DISA STIG V-222425 |
| Build Configuration | `Directory.Build.props` | NIST 800-53 SR-4; SLSA |
| Package Management | `Directory.Packages.props` | NIST 800-53 CM-2 |
| Secret Scanning Config | `.gitleaks.toml` | NIST 800-53 SC-28 |

### 10.2 CI/CD Pipeline Evidence

| Evidence | Path | Standards Served |
|----------|------|-----------------|
| Build & Test Pipeline | `.github/workflows/ci.yml` | SSDF PW.7; NIST 800-53 SA-11 |
| Security Scanning | `.github/workflows/security.yml` | NIST 800-53 RA-5, SA-11 |
| DoD Compliance Checks | `.github/workflows/dod-compliance.yml` | CMMC Level 2 |
| DoD Readiness Assessment | `.github/workflows/dod-readiness.yml` | CMMC Level 2 |
| SBOM Generation | `.github/workflows/sbom-aggregate.yml` | EO 14028; NTIA |
| SLSA Provenance | `.github/workflows/slsa-provenance.yml` | SLSA v1.0 |
| Container Publishing | `.github/workflows/docker-publish.yml` | DISA STIG |
| Staging Deployment | `.github/workflows/deploy-staging.yml` | NIST 800-53 CM-3 |
| Production Deployment | `.github/workflows/deploy-production.yml` | NIST 800-53 CM-3 |
| SBOM Script | `generate-sbom.sh` | EO 14028 |

### 10.3 Policy & Documentation Evidence

| Evidence | Path | Standards Served |
|----------|------|-----------------|
| SSDF Self-Attestation | `docs/ssdf-attestation.md` | OMB M-22-18; SSDF v1.1 |
| Vulnerability Management | `docs/vulnerability-management-policy.md` | NIST 800-53 SI-2, RA-5 |
| Incident Response Plan | `docs/incident-response-plan.md` | NIST 800-171 IR; DFARS 7012 |
| Data Classification Matrix | `docs/data-classification-matrix.md` | NIST 800-171 MP; 32 CFR 2002 |
| Penetration Testing Program | `docs/pentest-program.md` | NIST 800-53 CA-8; NIST 800-115 |
| Plan of Action & Milestones | `docs/POA&M.md` | NIST 800-171 CA-5 |
| Access Control Policy | `docs/policies/access-control-policy.md` | NIST 800-171 AC-1 |
| Audit & Accountability Policy | `docs/policies/audit-accountability-policy.md` | NIST 800-171 AU-1 |
| ID & Authentication Policy | `docs/policies/identification-authentication-policy.md` | NIST 800-171 IA-1 |
| Developer Setup Guide | `docs/developer-setup.md` | SSDF PS.1 |
| DoD Security Analysis | `DOD_SECURITY_ANALYSIS.md` | CMMC; NIST 800-171; DISA STIG |
| System Roadmap | `ROADMAP.md` | Program management |

### 10.4 Infrastructure Evidence

| Evidence | Path | Standards Served |
|----------|------|-----------------|
| Azure Terraform | `terraform/azure/main.tf` | NIST 800-53 CM, SC; FedRAMP |
| AWS Terraform | `terraform/aws/` | Multi-cloud DR |
| GCP Terraform | `terraform/gcp/` | Multi-cloud DR |
| Helm Charts | `helm/thewatch/` | NIST 800-53 CM-2 |
| Docker Compose | `docker-compose.yml` | Development environment |
| Kubernetes Scaling | `infra/kubernetes/` | NIST 800-53 CP-2 |
| Monitoring Stack | `infra/monitoring/` | NIST 800-53 SI-4 |
| Cloudflare Edge Security | `infra/cloudflare/` | NIST 800-53 SC-7 |
| Aspire Orchestration | `TheWatch.Aspire.AppHost/` | Development orchestration |
| Dependency Updates | `renovate.json` | NIST 800-53 SI-2 |

---

## 11. Compliance Gap Summary

| Standard | Total Requirements | Fully Met | Partial | Planned | Not Applicable |
|----------|-------------------|-----------|---------|---------|---------------|
| **NIST 800-171** | 110 | 90 | 16 | 2 | 2 |
| **CMMC Level 2** | 110 | 90 | 16 | 2 | 2 |
| **DISA STIG** (key findings) | 15 | 10 | 3 | 2 | 0 |
| **HIPAA §164.312** | 10 | 10 | 0 | 0 | 0 |
| **SSDF v1.1** | 21 tasks | 19 | 2 | 0 | 0 |
| **OWASP Top 10** | 10 | 8 | 2 | 0 | 0 |
| **SLSA v1.0** | 10 | 7 | 3 | 0 | 0 |
| **DFARS 7012** | 6 | 6 | 0 | 0 | 0 |
| **EO 14028** | 7 | 7 | 0 | 0 | 0 |

### Top Gaps Requiring Remediation

| Priority | Gap | Standards Affected | POA&M Sprint |
|----------|-----|-------------------|-------------|
| 1 | FIPS cryptographic mode not enabled | NIST 800-171 SC-13; DISA STIG V-222604; FIPS 140-2 | Sprint 1 |
| 2 | SQL Server TDE not enabled | NIST 800-171 SC-28; DISA STIG V-222605 | Sprint 2 |
| 3 | Password minimum 8 chars (needs 15) | NIST 800-171 IA-5; DISA STIG V-222524 | Sprint 3 |
| 4 | No password history (needs 24 gen) | NIST 800-171 IA-5; DISA STIG V-222546 | Sprint 3 |
| 5 | Inter-service mTLS not implemented | NIST 800-171 SC-8(1) | Sprint 2 |
| 6 | Kubernetes NetworkPolicy missing | NIST 800-53 SC-7(5) | Sprint 5 |
| 7 | Centralized SIEM not deployed | NIST 800-53 SI-4(4) | Sprint 5 |
| 8 | Mobile CUI encryption incomplete | NIST 800-171 AC-19 | Sprint 4 |
| 9 | Training program not formalized | NIST 800-171 AT-1 through AT-3 | Sprint 6 |
| 10 | Container signing not implemented | NIST 800-53 SI-7; OWASP A08 | Sprint 4 |

---

*This Standards Traceability Matrix provides complete visibility into TheWatch's
compliance posture across all applicable DoD and federal standards. It serves as
the authoritative cross-reference for assessors, auditors, and program managers.*
