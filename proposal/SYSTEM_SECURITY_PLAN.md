# System Security Plan — TheWatch Emergency Response Platform

> **Classification:** CUI // SP-PRIV // SP-HLTH // SP-LEI  
> **Reference Standards:** NIST SP 800-171 Rev 2, NIST SP 800-53 Rev 5, CMMC Level 2  
> **Document ID:** SSP-001  
> **Version:** 1.0

---

## 1. System Identification

| Attribute | Value |
|-----------|-------|
| **System Name** | TheWatch Emergency Response & Public Safety Platform |
| **System Type** | Microservices-based Web/Mobile Application |
| **Impact Level** | Moderate (CUI) |
| **Information Types** | CUI SP-PRIV, SP-HLTH, SP-LEI, SP-GEO, SP-BASIC |
| **Operational Status** | Under Development (Pre-Production) |
| **Authorization Boundary** | 11 microservices + shared infrastructure + mobile app + admin interfaces |
| **Environment** | Multi-cloud (Azure primary, AWS/GCP secondary) with Kubernetes orchestration |

### 1.1 System Description

TheWatch processes Controlled Unclassified Information (CUI) in support of emergency
response operations for DoD personnel and their families. The system handles personally
identifiable information (PII), protected health information (PHI), law enforcement
sensitive data, and geolocation data across 11 microservices with mobile and web interfaces.

### 1.2 System Boundary

The authorization boundary encompasses:

- **11 Microservices** (P1–P11) with dedicated SQL Server 2022 databases
- **Geospatial Engine** with PostgreSQL/PostGIS
- **Event Bus** (Apache Kafka / Azure Service Bus)
- **Cache Layer** (Redis 7)
- **Mobile Application** (.NET MAUI Hybrid Blazor)
- **Web Dashboards** (Blazor Server)
- **Admin REST API** (13 controllers)
- **Admin CLI** (45 PowerShell cmdlets)
- **CI/CD Pipeline** (GitHub Actions, 11 workflows)
- **Container Infrastructure** (Docker, Kubernetes/Helm)

---

## 2. Control Family Implementation

### 2.1 Access Control (AC) — NIST 800-171 §3.1

| Control | Requirement | Implementation | Status | Evidence |
|---------|-------------|----------------|--------|----------|
| **3.1.1** | Limit system access to authorized users | JWT bearer authentication required on all endpoints; anonymous access prohibited except `/login`, `/register`, `/refresh` | **[IMPLEMENTED]** | `TheWatch.Admin.RestAPI/Program.cs`, `TheWatch.Shared/Auth/WatchAuthExtensions.cs` |
| **3.1.2** | Limit system access to authorized functions | RBAC with 6 policies: AdminOnly, ResponderAccess, DoctorAccess, FamilyAccess, PatientAccess, Authenticated | **[IMPLEMENTED]** | `TheWatch.Admin.RestAPI/Program.cs` (authorization policies) |
| **3.1.3** | Control CUI flow per authorizations | CUI marking middleware applies classification headers per route; data classification matrix defines per-entity CUI categories | **[IMPLEMENTED]** | `TheWatch.Shared/Security/CuiMarkingMiddleware.cs`, `docs/data-classification-matrix.md` |
| **3.1.4** | Separate duties of individuals | Role-based separation: Admin, Responder, Doctor, FamilyMember, Patient, ServiceAccount; no single role spans all functions | **[IMPLEMENTED]** | `docs/policies/access-control-policy.md` |
| **3.1.5** | Employ least privilege | Service accounts scoped per microservice; user tokens contain only assigned roles; IDOR prevention via `CallerCanAccessUser()` | **[IMPLEMENTED]** | `TheWatch.Admin.RestAPI/Controllers/AuthSecurityController.cs` |
| **3.1.6** | Use non-privileged accounts for non-security functions | Containers run as non-root user `appuser` (UID 1001); service accounts have minimal database permissions | **[IMPLEMENTED]** | `TheWatch.P1.CoreGateway/Dockerfile` (DISA STIG V-222425) |
| **3.1.7** | Prevent non-privileged users from executing privileged functions | `[Authorize(Policy = "AdminOnly")]` enforced on administrative endpoints; role escalation requires admin approval | **[IMPLEMENTED]** | `TheWatch.Admin.RestAPI/Controllers/` |
| **3.1.8** | Limit unsuccessful login attempts | 3-attempt lockout (15 min); 10 attempts (60 min); 15+ attempts trigger account deactivation | **[IMPLEMENTED]** | `TheWatch.P5.AuthSecurity/` |
| **3.1.9** | Provide privacy and security notices | EULA versioned acceptance tracking with IP logging; privacy notice displayed before data collection | **[IMPLEMENTED]** | `TheWatch.P5.AuthSecurity/` (EULA endpoints) |
| **3.1.10** | Use session lock after inactivity | JWT tokens expire after 30 minutes (sliding window); refresh tokens valid 8 hours; automatic session termination | **[IMPLEMENTED]** | `TheWatch.Shared/Auth/WatchAuthExtensions.cs` |
| **3.1.11** | Terminate sessions after defined conditions | Token revocation on logout; session invalidation on password change; device trust score recalculation | **[IMPLEMENTED]** | `TheWatch.P5.AuthSecurity/` |
| **3.1.12** | Monitor and control remote access | TLS 1.2+ enforced on all connections; rate limiting (100/min global, 10/min auth); CORS whitelist | **[IMPLEMENTED]** | `TheWatch.Shared/Security/WatchKestrelExtensions.cs` |
| **3.1.13** | Employ cryptographic mechanisms for remote access | TLS 1.2/1.3 only; no TLS 1.0/1.1; HSTS with 365-day max-age and preload | **[IMPLEMENTED]** | `TheWatch.Shared/Security/WatchKestrelExtensions.cs` |
| **3.1.14** | Route remote access via managed access points | All traffic routes through P1 CoreGateway; no direct microservice access from external networks | **[IMPLEMENTED]** | `TheWatch.P1.CoreGateway/`, `docker-compose.yml` |
| **3.1.15** | Authorize remote execution of privileged commands | Admin CLI requires authentication; all admin operations logged with user identity | **[IMPLEMENTED]** | `TheWatch.Admin.CLI/` |
| **3.1.16** | Authorize wireless access | Mobile app authenticates via JWT; BLE mesh requires device registration | **[IMPLEMENTED]** | `TheWatch.Mobile/`, `TheWatch.P3.MeshNetwork/` |
| **3.1.17** | Protect wireless access using authentication and encryption | TLS 1.2+ for all wireless communications; WPA3 recommended in deployment guide | **[IMPLEMENTED]** | `TheWatch.Shared/Security/WatchKestrelExtensions.cs` |
| **3.1.18** | Control connection of mobile devices | Device trust scoring (fingerprint + IP + geolocation); biometric gate for sensitive operations | **[IMPLEMENTED]** | `TheWatch.P5.AuthSecurity/`, `TheWatch.Mobile/Services/BiometricGateService.cs` |
| **3.1.19** | Encrypt CUI on mobile devices | SQLite local database with encryption; offline queue encrypted at rest | **[PARTIAL]** | `TheWatch.Mobile/Data/WatchLocalDbContext.cs` |
| **3.1.20** | Verify and control connections to external systems | Typed HTTP clients with explicit endpoint configuration; no dynamic service discovery from untrusted sources | **[IMPLEMENTED]** | `TheWatch.Contracts.*/` |
| **3.1.21** | Limit use of portable storage | Mobile evidence collection requires chain-of-custody logging; no arbitrary file export | **[IMPLEMENTED]** | `TheWatch.Mobile/Services/ChainOfCustodyService.cs` |
| **3.1.22** | Control CUI posted to public systems | No public-facing CUI endpoints; all CUI-classified routes require authentication | **[IMPLEMENTED]** | `TheWatch.Shared/Security/CuiMarkingMiddleware.cs` |

### 2.2 Awareness and Training (AT) — NIST 800-171 §3.2

| Control | Requirement | Implementation | Status | Evidence |
|---------|-------------|----------------|--------|----------|
| **3.2.1** | Ensure personnel are aware of security risks | Security policies documented; developer setup guide includes security requirements | **[PARTIAL]** | `docs/developer-setup.md`, `docs/policies/` |
| **3.2.2** | Ensure personnel are trained in duties | Documented onboarding process; signed commit requirements; code review mandated | **[PARTIAL]** | `docs/developer-setup.md` |
| **3.2.3** | Provide security awareness training on threats | STRIDE threat model documented; MITRE ATT&CK detection rules seeded; threat awareness in IR plan | **[PARTIAL]** | `docs/incident-response-plan.md` |

### 2.3 Audit and Accountability (AU) — NIST 800-171 §3.3

| Control | Requirement | Implementation | Status | Evidence |
|---------|-------------|----------------|--------|----------|
| **3.3.1** | Create and retain system audit logs | Serilog structured logging across all 11 services; correlation ID tracking; Hangfire job logging | **[IMPLEMENTED]** | `TheWatch.Shared/` (Serilog configuration) |
| **3.3.2** | Ensure actions can be traced to individual users | JWT claims include user ID, roles, device fingerprint; all API calls logged with identity | **[IMPLEMENTED]** | `TheWatch.Admin.RestAPI/Program.cs` |
| **3.3.3** | Review and analyze audit logs | Audit log page in Admin Portal; weekly/monthly/quarterly review schedule defined | **[IMPLEMENTED]** | `TheWatch.Admin/`, `docs/policies/audit-accountability-policy.md` |
| **3.3.4** | Alert on audit process failure | Health check endpoints monitor logging pipeline; Hangfire monitors background job execution | **[PARTIAL]** | `TheWatch.Shared/` (health checks) |
| **3.3.5** | Correlate audit review and analysis | Correlation IDs propagated across service boundaries; structured logging enables cross-service tracing | **[IMPLEMENTED]** | `TheWatch.Shared/Security/` (Correlation ID middleware) |
| **3.3.6** | Provide audit record reduction and report generation | Structured JSON logging enables filtering/aggregation; admin dashboard provides summary views | **[IMPLEMENTED]** | `TheWatch.Admin/Pages/AuditLog.razor` |
| **3.3.7** | Provide system clock synchronization | Container orchestration provides NTP synchronization; UTC timestamps in all audit records | **[IMPLEMENTED]** | Kubernetes/Docker NTP |
| **3.3.8** | Protect audit information from unauthorized access | Audit logs stored in service databases with RBAC; `AdminOnly` policy on audit endpoints | **[IMPLEMENTED]** | `docs/policies/audit-accountability-policy.md` |
| **3.3.9** | Limit management of audit functionality | Only Admin role can configure logging levels and retention; no user-accessible audit config | **[IMPLEMENTED]** | `TheWatch.Admin/Pages/Settings.razor` |

### 2.4 Configuration Management (CM) — NIST 800-171 §3.4

| Control | Requirement | Implementation | Status | Evidence |
|---------|-------------|----------------|--------|----------|
| **3.4.1** | Establish and maintain baseline configurations | `Directory.Build.props` defines central build settings; `Directory.Packages.props` for NuGet versioning; Docker base images pinned | **[IMPLEMENTED]** | `Directory.Build.props`, `Directory.Packages.props` |
| **3.4.2** | Establish and enforce security configuration settings | Kestrel hardening extensions enforce TLS, header limits, timeout settings; DISA STIG-compliant container config | **[IMPLEMENTED]** | `TheWatch.Shared/Security/WatchKestrelExtensions.cs` |
| **3.4.3** | Track, review, approve, and audit changes | GitHub branch protection (2 approvals, signed commits, linear history); CODEOWNERS for security paths | **[IMPLEMENTED]** | `docs/developer-setup.md` |
| **3.4.4** | Analyze security impact of changes | CodeQL SAST in CI pipeline; dependency review on pull requests; security workflow validates changes | **[IMPLEMENTED]** | `.github/workflows/security.yml` |
| **3.4.5** | Define, document, approve physical/logical access restrictions | `docs/policies/access-control-policy.md` defines access restrictions; RBAC enforced at application layer | **[IMPLEMENTED]** | `docs/policies/access-control-policy.md` |
| **3.4.6** | Employ least functionality | Microservices follow single-responsibility principle; Docker containers include only required packages; non-root execution | **[IMPLEMENTED]** | Dockerfiles (multi-stage builds) |
| **3.4.7** | Restrict, disable, or prevent nonessential programs | Docker containers use multi-stage builds with `aspnet` runtime (no SDK); no shell in production images | **[IMPLEMENTED]** | `TheWatch.P1.CoreGateway/Dockerfile` |
| **3.4.8** | Apply deny-by-exception (blacklisting) for unauthorized software | `NuGetAudit` enabled with low-severity threshold; vulnerable packages fail builds | **[IMPLEMENTED]** | `Directory.Build.props` (NuGetAudit) |
| **3.4.9** | Control and monitor user-installed software | Container images are immutable; no user-installable software in production; renovate.json manages updates | **[IMPLEMENTED]** | `renovate.json` |

### 2.5 Identification and Authentication (IA) — NIST 800-171 §3.5

| Control | Requirement | Implementation | Status | Evidence |
|---------|-------------|----------------|--------|----------|
| **3.5.1** | Identify system users and processes | Unique user IDs via registration; service accounts per microservice; JWT claims identify actors | **[IMPLEMENTED]** | `TheWatch.P5.AuthSecurity/` |
| **3.5.2** | Authenticate users and devices | JWT bearer tokens; 4-method MFA (TOTP, FIDO2, SMS, Magic Link); device trust scoring | **[IMPLEMENTED]** | `TheWatch.P5.AuthSecurity/` |
| **3.5.3** | Use multi-factor authentication | MFA mandatory for Admin, Responder, Doctor roles; 4 methods available (TOTP, FIDO2, SMS, Magic Link) | **[IMPLEMENTED]** | `docs/policies/identification-authentication-policy.md` |
| **3.5.4** | Employ replay-resistant authentication | JWT tokens include `jti` (unique token ID), `iat` (issued at), `exp` (expiration); TOTP time-based with window | **[IMPLEMENTED]** | `TheWatch.Shared/Auth/WatchAuthExtensions.cs` |
| **3.5.5** | Prevent reuse of identifiers | Unique user IDs enforced at database level; email uniqueness constraint | **[IMPLEMENTED]** | EF Core entity configuration |
| **3.5.6** | Disable identifiers after inactivity | Account deactivation after 15+ failed attempts; inactive account monitoring | **[PARTIAL]** | `TheWatch.P5.AuthSecurity/` |
| **3.5.7** | Enforce minimum password complexity | Currently 8-character minimum with complexity (uppercase, lowercase, digit, special) | **[PARTIAL]** | POA&M: Upgrade to 15-character minimum per DISA STIG V-222524 |
| **3.5.8** | Prohibit password reuse | **[PLANNED]** | POA&M item: Implement 24-generation password history per DISA STIG V-222546 |
| **3.5.9** | Allow temporary passwords for system login | Initial registration generates temporary credentials; forced password change on first login | **[IMPLEMENTED]** | `TheWatch.P5.AuthSecurity/` |
| **3.5.10** | Store and transmit only cryptographically protected passwords | Argon2id hashing (default); PBKDF2-SHA512 FIPS fallback (600K iterations); TLS for transmission | **[IMPLEMENTED]** | `TheWatch.P5.AuthSecurity/Security/Argon2PasswordHasher.cs`, `TheWatch.Shared/Security/FipsPbkdf2PasswordHasher.cs` |
| **3.5.11** | Obscure feedback of authentication information | Password fields masked; error messages do not reveal account existence | **[IMPLEMENTED]** | `TheWatch.P5.AuthSecurity/` |

### 2.6 Incident Response (IR) — NIST 800-171 §3.6

| Control | Requirement | Implementation | Status | Evidence |
|---------|-------------|----------------|--------|----------|
| **3.6.1** | Establish incident handling capability | Incident Response Plan with 5 categories (CAT-1 through CAT-5) and 4 severity levels | **[IMPLEMENTED]** | `docs/incident-response-plan.md` |
| **3.6.2** | Track, document, and report incidents | 72-hour DoD reporting to DC3 (DFARS 252.204-7012); post-incident review process | **[IMPLEMENTED]** | `docs/incident-response-plan.md` |
| **3.6.3** | Test incident response capability | IR plan includes quarterly tabletop exercises and annual full-scale exercises | **[PARTIAL]** | `docs/incident-response-plan.md` |

### 2.7 Maintenance (MA) — NIST 800-171 §3.7

| Control | Requirement | Implementation | Status | Evidence |
|---------|-------------|----------------|--------|----------|
| **3.7.1** | Perform maintenance on systems | Automated dependency updates via Renovate; NuGet audit on every build; Trivy container scanning | **[IMPLEMENTED]** | `renovate.json`, `.github/workflows/ci.yml` |
| **3.7.2** | Provide controls for maintenance tools | GitHub Actions runners are ephemeral; no persistent maintenance access; tools pinned to versions | **[IMPLEMENTED]** | `.github/workflows/` (action version pinning) |
| **3.7.3** | Ensure equipment removed for maintenance is sanitized | Container images rebuilt from scratch on each deployment; no persistent state in containers | **[IMPLEMENTED]** | Dockerfiles (multi-stage builds) |
| **3.7.4** | Check media containing diagnostic programs | SBOM generation tracks all dependencies; NuGet signature verification available | **[PARTIAL]** | `generate-sbom.sh` |
| **3.7.5** | Require multi-factor auth for nonlocal maintenance sessions | Admin CLI and REST API require JWT authentication; MFA mandatory for admin roles | **[IMPLEMENTED]** | `TheWatch.Admin.RestAPI/Program.cs` |
| **3.7.6** | Supervise maintenance activities | GitHub audit log tracks all repository changes; PR reviews required for all changes | **[IMPLEMENTED]** | `docs/developer-setup.md` |

### 2.8 Media Protection (MP) — NIST 800-171 §3.8

| Control | Requirement | Implementation | Status | Evidence |
|---------|-------------|----------------|--------|----------|
| **3.8.1** | Protect system media (digital and physical) | Database encryption (TDE planned); AES-256-GCM field encryption for CUI columns | **[PARTIAL]** | `TheWatch.Shared/Security/FieldEncryptionService.cs` |
| **3.8.2** | Limit access to CUI on system media | RBAC enforced on all data access; CUI marking middleware classifies routes | **[IMPLEMENTED]** | `TheWatch.Shared/Security/CuiMarkingMiddleware.cs` |
| **3.8.3** | Sanitize or destroy system media before disposal or reuse | Containers are stateless and destroyed on termination; database backups encrypted | **[PARTIAL]** | Container architecture |
| **3.8.4** | Mark media with CUI markings and distribution limitations | CUI marking middleware adds classification headers to HTTP responses | **[IMPLEMENTED]** | `TheWatch.Shared/Security/CuiMarkingMiddleware.cs` |
| **3.8.5** | Control access to media containing CUI with controlled areas | Cloud provider physical security; Kubernetes namespace isolation | **[IMPLEMENTED]** | `terraform/`, `helm/` |
| **3.8.6** | Implement cryptographic mechanisms during transport | TLS 1.2+ enforced; evidence files hashed (SHA-256) during transport | **[IMPLEMENTED]** | `TheWatch.Shared/Security/WatchKestrelExtensions.cs` |
| **3.8.7** | Control removable media | Mobile evidence collection requires chain-of-custody; no arbitrary media access | **[IMPLEMENTED]** | `TheWatch.Mobile/Services/ChainOfCustodyService.cs` |
| **3.8.8** | Prohibit use of portable storage without an identified owner | Evidence files tagged with user ID, device fingerprint, and GPS coordinates | **[IMPLEMENTED]** | `TheWatch.Mobile/Services/ChainOfCustodyService.cs` |
| **3.8.9** | Protect backup CUI at storage locations | Encrypted backup configurations in Terraform; geo-replicated storage | **[PARTIAL]** | `terraform/azure/main.tf` |

### 2.9 Personnel Security (PS) — NIST 800-171 §3.9

| Control | Requirement | Implementation | Status | Evidence |
|---------|-------------|----------------|--------|----------|
| **3.9.1** | Screen individuals before authorizing access | Account registration requires identity verification; admin approval for privileged roles | **[IMPLEMENTED]** | `TheWatch.P5.AuthSecurity/` |
| **3.9.2** | Protect CUI during personnel actions | Account deactivation capability; role revocation; token invalidation on status change | **[IMPLEMENTED]** | `TheWatch.P5.AuthSecurity/` |

### 2.10 Physical Protection (PE) — NIST 800-171 §3.10

| Control | Requirement | Implementation | Status | Evidence |
|---------|-------------|----------------|--------|----------|
| **3.10.1** | Limit physical access to systems | Cloud provider facilities (Azure/AWS/GCP) provide physical security controls | **[IMPLEMENTED]** | Cloud provider compliance (inherited) |
| **3.10.2** | Protect and monitor the physical facility | Cloud provider monitoring; Kubernetes node security groups | **[IMPLEMENTED]** | `terraform/` (security groups) |
| **3.10.3** | Escort visitors and monitor activity | Cloud provider responsibility (inherited) | **[IMPLEMENTED]** | Cloud provider compliance |
| **3.10.4** | Maintain audit logs of physical access | Cloud provider audit logs; Kubernetes audit logging | **[IMPLEMENTED]** | Cloud provider + Kubernetes |
| **3.10.5** | Control and manage physical access devices | Managed Identity for Azure resources; no physical keys in application layer | **[IMPLEMENTED]** | `terraform/azure/main.tf` |
| **3.10.6** | Enforce safeguarding measures at alternate work sites | Mobile app enforces TLS; biometric gate for sensitive operations; device trust scoring | **[IMPLEMENTED]** | `TheWatch.Mobile/Services/BiometricGateService.cs` |

### 2.11 Risk Assessment (RA) — NIST 800-171 §3.11

| Control | Requirement | Implementation | Status | Evidence |
|---------|-------------|----------------|--------|----------|
| **3.11.1** | Periodically assess risk | STRIDE threat modeling; MITRE ATT&CK detection rules; DoD Security Analysis with risk register | **[IMPLEMENTED]** | `DOD_SECURITY_ANALYSIS.md` |
| **3.11.2** | Scan for vulnerabilities | CodeQL SAST, Trivy container scanning, Gitleaks secret scanning, NuGet audit, GitHub Dependency Review; VulnerabilityMonitorService (Hangfire, continuous) | **[IMPLEMENTED]** | `.github/workflows/security.yml`, `docs/vulnerability-management-policy.md` |
| **3.11.3** | Remediate vulnerabilities per assessments | SLA-based remediation: Critical 7d, High 30d, Medium 90d, Low 180d; POA&M tracking | **[IMPLEMENTED]** | `docs/vulnerability-management-policy.md`, `docs/POA&M.md` |

### 2.12 Security Assessment (CA) — NIST 800-171 §3.12

| Control | Requirement | Implementation | Status | Evidence |
|---------|-------------|----------------|--------|----------|
| **3.12.1** | Periodically assess security controls | DoD readiness workflow validates compliance; penetration testing program established | **[IMPLEMENTED]** | `.github/workflows/dod-readiness.yml`, `docs/pentest-program.md` |
| **3.12.2** | Develop and implement plans of action | POA&M with 50 findings: 9 Critical, 22 High, 15 Medium, 4 Low; sprint-based remediation | **[IMPLEMENTED]** | `docs/POA&M.md` |
| **3.12.3** | Monitor security controls on an ongoing basis | Continuous vulnerability monitoring (CISA KEV, NuGet Advisory, GitHub Advisory); security workflow on every push | **[IMPLEMENTED]** | `.github/workflows/security.yml`, `docs/vulnerability-management-policy.md` |
| **3.12.4** | Develop, document, update SSP | This document serves as the System Security Plan; maintained in version control | **[IMPLEMENTED]** | `proposal/SYSTEM_SECURITY_PLAN.md` |

### 2.13 System and Communications Protection (SC) — NIST 800-171 §3.13

| Control | Requirement | Implementation | Status | Evidence |
|---------|-------------|----------------|--------|----------|
| **3.13.1** | Monitor, control, protect communications at boundaries | P1 CoreGateway serves as boundary protection; rate limiting; CORS whitelist; security headers | **[IMPLEMENTED]** | `TheWatch.P1.CoreGateway/` |
| **3.13.2** | Employ architectural designs to promote security | Microservices isolation with dedicated databases; event-driven loose coupling; defense in depth | **[IMPLEMENTED]** | Architecture design |
| **3.13.3** | Separate user functionality from system management | Admin portal and CLI separated from user-facing applications; role-based access | **[IMPLEMENTED]** | `TheWatch.Admin/`, `TheWatch.Admin.CLI/` |
| **3.13.4** | Prevent unauthorized/unintended information transfer | CUI marking middleware; PII redaction middleware; data classification enforcement | **[IMPLEMENTED]** | `TheWatch.Shared/Security/` |
| **3.13.5** | Implement subnetworks for publicly accessible components | Kubernetes namespace isolation; Docker network segregation; security groups | **[IMPLEMENTED]** | `helm/`, `terraform/` |
| **3.13.6** | Deny network traffic by default | Kubernetes NetworkPolicy (planned); Docker network isolation | **[PARTIAL]** | POA&M: Implement network policies |
| **3.13.7** | Prevent split tunneling for remote devices | Mobile app routes all traffic through CoreGateway; no direct service access | **[IMPLEMENTED]** | `TheWatch.Mobile/` |
| **3.13.8** | Implement cryptographic mechanisms for CUI in transit | TLS 1.2/1.3 only; HSTS 365-day max-age; certificate pinning for mobile | **[IMPLEMENTED]** | `TheWatch.Shared/Security/WatchKestrelExtensions.cs` |
| **3.13.9** | Terminate network connections at end of sessions | JWT expiration enforced; connection timeout configured; health check-based termination | **[IMPLEMENTED]** | JWT configuration |
| **3.13.10** | Establish and manage cryptographic keys | RSA-2048 for JWT (production); AES-256 for field encryption; PBKDF2-SHA512 key derivation; Azure Key Vault for production | **[PARTIAL]** | POA&M: Implement HSM-backed key management |
| **3.13.11** | Employ FIPS-validated cryptography for CUI | PBKDF2-SHA512 (FIPS 140-2 fallback) available; AES-256-GCM for field encryption; .NET FIPS mode configuration available | **[PARTIAL]** | POA&M: Enable FIPS mode, transition from Argon2id |
| **3.13.12** | Prohibit remote activation of collaborative computing devices | Not applicable — system does not include collaborative computing devices (cameras are opt-in with user consent) | **N/A** | |
| **3.13.13** | Control and protect mobile code | Mobile app distributed through managed app stores; MAUI code signed; no arbitrary code execution | **[IMPLEMENTED]** | `TheWatch.Mobile/` |
| **3.13.14** | Control VoIP | Not applicable — voice emergency ingests voice data but does not provide VoIP services | **N/A** | |
| **3.13.15** | Protect authenticity of communications sessions | JWT token binding; device trust scoring; correlation ID tracking; HMAC-signed audit records | **[IMPLEMENTED]** | `TheWatch.Shared/Security/` |
| **3.13.16** | Protect CUI at rest | AES-256-GCM field-level encryption for sensitive columns; SQL Server TDE planned; mobile SQLite encryption | **[PARTIAL]** | `TheWatch.Shared/Security/FieldEncryptionService.cs`, POA&M |

### 2.14 System and Information Integrity (SI) — NIST 800-171 §3.14

| Control | Requirement | Implementation | Status | Evidence |
|---------|-------------|----------------|--------|----------|
| **3.14.1** | Identify, report, and correct flaws in a timely manner | VulnerabilityMonitorService (continuous); SLA-based remediation; NuGet audit in CI; Renovate automated updates | **[IMPLEMENTED]** | `docs/vulnerability-management-policy.md`, `.github/workflows/ci.yml` |
| **3.14.2** | Provide protection from malicious code | CodeQL SAST; Trivy container scanning; Gitleaks secret scanning; .NET analyzers (latest-recommended) | **[IMPLEMENTED]** | `.github/workflows/security.yml` |
| **3.14.3** | Monitor security alerts and advisories | VulnerabilityMonitorService monitors CISA KEV, NuGet Advisory DB, GitHub Advisory DB | **[IMPLEMENTED]** | `docs/vulnerability-management-policy.md` |
| **3.14.4** | Update malicious code protection | Automated dependency updates via Renovate; container base image updates; CodeQL rule updates | **[IMPLEMENTED]** | `renovate.json` |
| **3.14.5** | Perform periodic scans and real-time scans | CodeQL on every push; Trivy on container builds; security workflow on PR; SBOM generation | **[IMPLEMENTED]** | `.github/workflows/security.yml`, `.github/workflows/sbom-aggregate.yml` |
| **3.14.6** | Monitor systems including inbound/outbound traffic | Rate limiting with logging; CORS violation logging; auth failure logging; health endpoint monitoring | **[IMPLEMENTED]** | `TheWatch.Shared/` |
| **3.14.7** | Identify unauthorized use | STRIDE threat scanning (15-min intervals); MITRE ATT&CK detection rules; brute force detection; anomaly alerts | **[IMPLEMENTED]** | `TheWatch.P5.AuthSecurity/` |

---

## 3. POA&M Summary

The Plan of Action & Milestones tracks 50 open findings with remediation timelines:

| Severity | Count | Target Remediation |
|----------|-------|--------------------|
| **CRITICAL** | 9 | Sprint 1–2 (immediate) |
| **HIGH** | 22 | Sprint 2–4 (30 days) |
| **MEDIUM** | 15 | Sprint 4–6 (90 days) |
| **LOW** | 4 | Sprint 6–8 (180 days) |

**Key POA&M Items:**

| ID | Finding | Target | NIST Control |
|----|---------|--------|-------------|
| C-01 | Enable FIPS cryptographic mode | Sprint 1 | SC-13 |
| C-02 | Transition JWT to RSA asymmetric keys | Sprint 1 | SC-13 |
| C-04 | Implement inter-service mTLS | Sprint 2 | SC-8(1) |
| D-01 | Enable SQL Server TDE | Sprint 2 | SC-28 |
| A-01 | Increase password minimum to 15 characters | Sprint 3 | IA-5(1) |
| A-02 | Implement 24-generation password history | Sprint 3 | IA-5(1) |
| A-05 | Implement CAC/PIV certificate authentication | Sprint 4 | IA-2(12) |
| V-01 | Deploy FluentValidation on all DTOs | Sprint 3 | SI-10 |
| S-01 | Generate and sign SBOMs | Sprint 4 | SA-17 |

Full details: `docs/POA&M.md`

---

## 4. Risk Acceptance Decisions

| Risk | Compensating Control | Rationale |
|------|---------------------|-----------|
| Argon2id not FIPS-validated | PBKDF2-SHA512 (FIPS 140-2) fallback available; Argon2id cryptographically superior | Risk accepted pending FIPS validation of Argon2id; PBKDF2 migration path documented |

---

*This System Security Plan is maintained in version control and updated with each
security-relevant change. All control implementations reference specific files and
configurations within the TheWatch repository.*
