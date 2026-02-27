# TheWatch Microservices — DoD Contract-Level Security Standards Analysis

**Date:** 2026-02-26
**Analyst:** Claude Opus 4.6 (automated security gap analysis)
**Scope:** Full repository (`E:\json_output\Microservices`) — 12 microservices, 1 REST API gateway, 1 dashboard, 1 admin portal, 1 MAUI mobile app
**Target Compliance:** CMMC 2.0 Level 2 (NIST 800-171 Rev 2), DISA ASD STIG, NIST 800-53 Rev 5, OWASP Top 10:2021, NIST 800-218 SSDF

---

## Source Documents Referenced

| Standard | Version | Authority | Applicability |
|----------|---------|-----------|---------------|
| **NIST SP 800-171 Rev 2** | Rev 2 Update 1, Jan 2021 | NIST (via DFARS 252.204-7012) | CUI protection — mandatory for all DoD contracts handling CUI |
| **CMMC 2.0** | Final Rule, Oct 2024 (effective Nov 2025) | DoD CIO | Contractor cybersecurity maturity — Level 2 = 110 practices from 800-171 |
| **NIST SP 800-53 Rev 5** | Rev 5 Update 1, v5.2.0 | NIST | Federal information systems — superset of 800-171; 1,189 controls across 20 families |
| **OWASP Top 10:2021** | 2021 | OWASP Foundation | Web application security risks — industry standard |
| **DISA ASD STIG** | Version 6 Release 1, Feb 2025 | DISA (DoD) | 286 technical findings (34 CAT I / 230 CAT II / 22 CAT III) |
| **NIST SP 800-218 SSDF** | v1.1, Feb 2022 | NIST (via EO 14028 + OMB M-22-18) | Secure software development — self-attestation required for federal sales |

---

## Executive Summary

TheWatch has **strong foundational security** — JWT authentication, RBAC with 6 policies, MFA (4 methods), Argon2id password hashing, rate limiting, security headers, audit logging, STRIDE/MITRE threat detection, and a CI/CD security pipeline with CodeQL + dependency review + secret scanning. This puts the project significantly ahead of most early-stage systems.

However, **DoD contract-level compliance requires 30+ specific hardening actions** before passing a CMMC Level 2 third-party assessment or DISA STIG audit. The gaps fall into five categories:

| Priority | Category | Gap Count | Effort |
|----------|----------|-----------|--------|
| **CRITICAL** | Cryptographic Compliance (FIPS 140-2) | 5 | High |
| **CRITICAL** | Data Protection (CUI at rest + in transit) | 6 | High |
| **HIGH** | Authentication Hardening | 7 | Medium |
| **HIGH** | Input Validation & Error Handling | 5 | Medium |
| **HIGH** | SDLC & Supply Chain | 8 | Medium |
| **MEDIUM** | Logging, Monitoring & Incident Response | 6 | Medium |
| **MEDIUM** | Infrastructure & Container Security | 7 | Medium |
| **LOW** | Documentation & Process | 6 | Low |

**Bottom line:** The architecture is sound. The gaps are almost entirely **configuration, tooling, and process** — not fundamental redesign. Estimated effort to reach CMMC Level 2 readiness: 4-6 focused sprints.

---

## Detailed Gap Analysis

### SECTION 1: CRYPTOGRAPHIC COMPLIANCE (FIPS 140-2/140-3)

**Why it matters:** NIST 800-171 SC-13, DISA STIG V-222570-572, CMMC Level 2 all require FIPS 140-2 validated cryptographic modules. This is the single most common audit failure for DoD contractors.

| ID | Finding | Standard | Current State | Required State | Severity |
|----|---------|----------|---------------|----------------|----------|
| C-01 | **.NET not running in FIPS mode** | SC-13, V-222570 | No `CryptoConfig.AllowOnlyFipsAlgorithms` enforcement; no OS-level FIPS policy | Enable Windows FIPS mode policy; configure .NET `runtimeconfig.json` with `"System.Security.Cryptography.UseFipsAlgorithms": true`; verify all crypto calls use FIPS-compliant providers | **CRITICAL** |
| C-02 | **JWT symmetric key (HMAC-SHA256) — key management** | SC-12, SC-13, V-222641 | `Jwt:Key` stored in `appsettings.json` as empty string, populated via environment variable. Symmetric key shared across all 12 services | Migrate to asymmetric RSA-2048/ECDSA-256 JWT signing (private key at P5 only, public key at consumers). Store signing key in HSM or Azure Key Vault with FIPS 140-2 Level 2 boundary. Implement key rotation schedule (90-day maximum) | **CRITICAL** |
| C-03 | **Argon2id not FIPS-validated** | SC-13, V-222570 | `Isopoh.Cryptography.Argon2` — open-source, not FIPS-validated | For strict FIPS compliance: use PBKDF2 with HMAC-SHA-512 (600,000+ iterations per OWASP 2024) as the FIPS-approved fallback, OR obtain a FIPS-validated Argon2 implementation. Document the risk acceptance if retaining Argon2 (which is otherwise cryptographically superior) | **HIGH** |
| C-04 | **No TLS cipher suite restriction** | SC-8, V-222596 | Default Kestrel TLS settings — allows TLS 1.0/1.1 and weak cipher suites | Configure Kestrel `SslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13`; restrict cipher suites to FIPS-approved only (AES-256-GCM, CHACHA20-POLY1305); disable CBC mode | **CRITICAL** |
| C-05 | **No certificate pinning for inter-service communication** | SC-8, V-222641, IA-5(2) | Services communicate over HTTP within Docker network; no mTLS | Implement mutual TLS (mTLS) between all microservices. Use service mesh (Istio/Linkerd) or ASP.NET Kestrel client certificates. DoD requires mutual endpoint authentication for key exchange | **CRITICAL** |

### SECTION 2: DATA PROTECTION (CUI AT REST + IN TRANSIT)

**Why it matters:** NIST 800-171 SC-28, DISA STIG V-222588-589 — if any data in the system qualifies as CUI (incident reports, medical records, location data, evidence chain-of-custody), it must be encrypted at rest with approved algorithms.

| ID | Finding | Standard | Current State | Required State | Severity |
|----|---------|----------|---------------|----------------|----------|
| D-01 | **SQL Server Transparent Data Encryption (TDE) not configured** | SC-28, V-222588 | Docker Compose SQL Server uses Developer edition with no TDE | Enable TDE on all 11 databases; use AES-256 encryption; manage DEK with Key Vault; Enterprise or Standard edition required for TDE in production | **CRITICAL** |
| D-02 | **No column-level encryption for PII/CUI fields** | SC-28, V-222589 | User emails, phone numbers, medical data, GPS coordinates stored in plaintext columns | Implement Always Encrypted or application-level encryption for: `WatchUser.Email`, `WatchUser.PhoneNumber`, `VitalReading.*`, `MedicalAlert.*`, `Incident.Location`, `Evidence.*` metadata | **CRITICAL** |
| D-03 | **PostgreSQL/PostGIS data at rest — no encryption** | SC-28, V-222588 | PostGIS container uses default settings; no disk encryption | Enable PostgreSQL `pgcrypto` extension; configure full-disk encryption on the volume; use `pg_tde` or OS-level LUKS encryption | **HIGH** |
| D-04 | **Redis cache — no encryption, no auth** | SC-28, AC-3 | `redis:7-alpine` in Docker Compose with no password, no TLS | Enable Redis AUTH with strong password (32+ chars); enable TLS (`--tls-port 6380`); disable unencrypted port; configure `maxmemory-policy` to prevent data leaks via eviction | **HIGH** |
| D-05 | **Kafka — PLAINTEXT protocol** | SC-8, V-222596 | `KAFKA_LISTENER_SECURITY_PROTOCOL_MAP: CONTROLLER:PLAINTEXT,PLAINTEXT:PLAINTEXT` | Configure SASL_SSL for all Kafka listeners; use TLS for broker-to-broker and client-to-broker communication; enable ACLs for topic-level authorization | **HIGH** |
| D-06 | **Backup encryption not enforced** | MP-4, SC-28 | `docker-compose.sqlbackup.yml` exists but no encryption on backup files | Encrypt all database backups with AES-256; store encryption keys separately from backup media; implement backup integrity verification | **MEDIUM** |

### SECTION 3: AUTHENTICATION HARDENING

**What's good:** MFA (TOTP, SMS, Magic Link, FIDO2), Argon2id hashing, brute force detection, device trust scoring, account lockout (5 attempts / 15 min), JWT with refresh tokens.

| ID | Finding | Standard | Current State | Required State | Severity |
|----|---------|----------|---------------|----------------|----------|
| A-01 | **Password minimum length is 8 characters** | IA-5, V-222536 | `options.Password.RequiredLength = 8` in P5 Program.cs:62 | DISA STIG requires **minimum 15 characters**. Update to `RequiredLength = 15` | **HIGH** |
| A-02 | **No password history enforcement** | IA-5, V-222546 | ASP.NET Identity does not enforce password reuse prevention out of the box | Implement password history tracking (last 5 passwords minimum per STIG V-222546). Create `PasswordHistory` entity with hashed previous passwords; check on change | **HIGH** |
| A-03 | **No password maximum age (60-day rotation)** | IA-5, V-222545 | No password expiration policy | Add `PasswordLastChangedUtc` to `WatchUser`; enforce 60-day maximum password age; prompt user to change on login if expired; allow 24-hour minimum age to prevent rapid cycling (V-222544) | **HIGH** |
| A-04 | **SMS MFA uses in-memory OTP storage** | IA-2, V-222530 | `SmsMfaService` uses `ConcurrentDictionary` — not persistent, not distributed | Move OTP storage to Redis with TTL; use HMAC-based OTP (RFC 6238) instead of random codes; limit OTP verification attempts to 3 per code | **HIGH** |
| A-05 | **No CAC/PIV/x.509 certificate authentication** | IA-2(12), V-222524 | Only JWT Bearer and API Key schemes | DoD environments require CAC/PIV smart card authentication support. Add `AddCertificate()` authentication scheme; validate against DoD PKI chain; map certificate subject to user identity | **HIGH** |
| A-06 | **Account lockout duration too short** | AC-7, V-222432 | 15-minute lockout after 5 failures | STIG requires maximum 3 consecutive failures (not 5). Set `MaxFailedAccessAttempts = 3`. Consider progressive lockout: 15 min → 1 hour → 24 hours → admin unlock | **MEDIUM** |
| A-07 | **Refresh token not bound to device fingerprint** | IA-5, SC-23 | `RefreshTokenRequest` accepts optional `DeviceFingerprint` but binding is not enforced | Bind refresh tokens to device fingerprint; reject refresh attempts from different devices; log anomalous device changes as security events | **MEDIUM** |

### SECTION 4: INPUT VALIDATION & ERROR HANDLING

**Why it matters:** DISA STIG V-222606 requires ALL application inputs validated. OWASP A03 (Injection) ranked #3. This is the biggest code-level gap.

| ID | Finding | Standard | Current State | Required State | Severity |
|----|---------|----------|---------------|----------------|----------|
| V-01 | **No FluentValidation on API DTOs** | SI-10, V-222606, A03 | DTOs use `[Required]`, `[MaxLength]`, `[EmailAddress]` data annotations only — no cross-field validation, no business rule validation | Add FluentValidation to all 12 microservice projects. Create validators for every request DTO. Wire via `AddFluentValidation()`. Use `.AddFluentValidationAutoValidation()` for automatic model state | **HIGH** |
| V-02 | **Global exception handler incomplete** | SI-11, V-222610, A05 | `Admin.RestAPI` has `GlobalExceptionMiddleware`; other 11 services do not | Add standardized `ProblemDetails` exception middleware to all services. Never expose stack traces, internal type names, or connection strings in error responses. Return RFC 7807 Problem Details JSON | **HIGH** |
| V-03 | **No request body size limits on most services** | SC-5, V-222602 | `Admin.RestAPI` has Kestrel limits (10MB body, 32KB headers); P1-P11 services use defaults (30MB body) | Apply consistent Kestrel limits across all services: 10MB max body, 32KB max headers, 8KB max request line. Configure via `ConfigureKestrel()` in each Program.cs or via shared extension | **MEDIUM** |
| V-04 | **`/api/onboarding/complete-step` accepts raw query string** | SI-10, V-222606, A03 | `app.MapPost("/api/onboarding/complete-step", async (string step, ...)` — `step` is a raw query parameter, no validation | Validate `step` against a known enum/allowlist of onboarding steps. Reject unknown values. Use a request body DTO instead of query parameter for POST operations | **MEDIUM** |
| V-05 | **CORS allows all origins in development (SignalR)** | AC-4, V-222602 | D034 decision: `SetIsOriginAllowed(_ => true)` in some services for SignalR | Remove `SetIsOriginAllowed(_ => true)` from all production configurations. Use `WithOrigins()` with explicit origin list only. The `WatchCorsExtensions` is correct but must be used consistently everywhere | **MEDIUM** |

### SECTION 5: SDLC & SUPPLY CHAIN SECURITY

**Why it matters:** NIST 800-218 SSDF is now mandatory for federal software sales (EO 14028 + OMB M-22-18). CMMC Level 3 adds supply chain requirements (SR family).

| ID | Finding | Standard | Current State | Required State | Severity |
|----|---------|----------|---------------|----------------|----------|
| S-01 | **No SBOM (Software Bill of Materials)** | SR-4, PS.2, A08 | No SBOM generated during build | Add `dotnet sbom` (CycloneDX or SPDX format) to CI pipeline. Generate SBOM for every release. Store with release artifacts. EO 14028 requires SBOM for all federal software | **HIGH** |
| S-02 | **No container image signing** | SA-10, PS.2, A08 | Docker images pushed to registry unsigned | Implement Cosign or Docker Content Trust (DCT) for image signing. Verify signatures before deployment. Implement admission controllers in Kubernetes to reject unsigned images | **HIGH** |
| S-03 | **No DAST (Dynamic Application Security Testing) in CI** | SA-11, PW.7 | CI has SAST (CodeQL) but no DAST | Add OWASP ZAP or Burp Suite automated scan stage to CI pipeline. Run against deployed containers in staging. Gate deployments on DAST results (fail on HIGH findings) | **HIGH** |
| S-04 | **No penetration testing program** | CA-8, PW.7 | No evidence of pentest schedule | Establish annual penetration testing by independent assessor. Schedule quarterly automated pentests. Maintain pentest reports and remediation tracking | **HIGH** |
| S-05 | **No signed Git commits** | PS.1, CM-3 | Git commits not GPG/SSH signed | Enable commit signing requirement on repository. Configure `git config commit.gpgsign true`. Enforce via branch protection rules | **MEDIUM** |
| S-06 | **NuGet packages not signature-verified** | SR-4, PS.2 | No `<NuGetAudit>` in .csproj files; no package signing verification | Add `<NuGetAudit>true</NuGetAudit>` and `<NuGetAuditLevel>low</NuGetAuditLevel>` to `Directory.Build.props`. Enable `repositorySignatures` in `nuget.config` | **MEDIUM** |
| S-07 | **Secret scanning is regex-only** | IA-5, PS.1 | `secrets-scan` job in `security.yml` uses grep patterns | Upgrade to dedicated secret scanner (Gitleaks, TruffleHog, or GitHub Advanced Security secret scanning). These detect entropy-based secrets that regex misses | **MEDIUM** |
| S-08 | **No SSDF self-attestation form** | SSDF (OMB M-22-18) | No attestation document | Complete CISA Secure Software Self-Attestation Common Form. Document compliance with all 4 SSDF practice groups (PO, PS, PW, RV). Required before any federal procurement | **MEDIUM** |

### SECTION 6: LOGGING, MONITORING & INCIDENT RESPONSE

**What's good:** Serilog structured logging across all services, correlation IDs, Prometheus + Grafana monitoring, AlertManager, security audit middleware, MITRE ATT&CK detection rules, PagerDuty integration.

| ID | Finding | Standard | Current State | Required State | Severity |
|----|---------|----------|---------------|----------------|----------|
| L-01 | **Audit logs not cryptographically protected** | AU-9, V-222507 | Serilog writes to file and console; no integrity verification | Implement log signing (HMAC on each log entry) or use append-only storage (Azure Immutable Blob, AWS S3 Object Lock). Prevent tampering by admins | **HIGH** |
| L-02 | **No centralized SIEM** | AU-6, V-222481 | Logs stay local to each container. Prometheus monitors metrics but not security events | Deploy centralized SIEM (Elastic Security, Azure Sentinel, or Splunk). Forward all Serilog output via Serilog.Sinks.Elasticsearch or Serilog.Sinks.Seq. Create correlation rules for multi-service attack patterns | **HIGH** |
| L-03 | **Audit log retention period not defined** | AU-11 | No log rotation or retention policy configured in Serilog | Define retention policy: minimum 1 year for security events, 90 days for general logs (per NIST 800-171 AU-11). Configure `rollingInterval` and `retainedFileCountLimit` in Serilog; archive to immutable storage | **MEDIUM** |
| L-04 | **No incident response plan document** | IR-1 through IR-3 | STRIDE + MITRE detection services exist but no documented IR plan | Create formal Incident Response Plan (IRP) covering: detection, analysis, containment, eradication, recovery, post-incident. Define severity levels, escalation paths, communication procedures, and reporting timelines (72 hours for DoD) | **MEDIUM** |
| L-05 | **Health endpoints return service info without auth** | AC-3, V-222522 | `/health` and `/info` endpoints have no authorization | Add IP allowlisting or require authentication for `/info` endpoint (exposes version, program, description). `/health` can remain unauthenticated but should return only status (not internal details) | **LOW** |
| L-06 | **No log sanitization for PII** | SI-4, PT-2 | Log statements may capture user email, IP, phone in plaintext | Implement Serilog destructuring policies to mask PII fields (email → `u***@domain.com`, phone → `***-**-1234`). Use `[LogMasked]` attributes on sensitive properties | **LOW** |

### SECTION 7: INFRASTRUCTURE & CONTAINER SECURITY

| ID | Finding | Standard | Current State | Required State | Severity |
|----|---------|----------|---------------|----------------|----------|
| I-01 | **Containers run as root** | CM-7, V-222425 | Dockerfiles don't specify `USER` directive; containers run as root by default | Add `RUN adduser --disabled-password --no-create-home appuser` and `USER appuser` to all 12 Dockerfiles. .NET 10 base image supports non-root execution | **HIGH** |
| I-02 | **Docker Compose exposes database ports to host** | SC-7, AC-4 | `ports: - "1433:1433"`, `"5432:5432"`, `"6379:6379"`, `"9092:9092"` | Remove host port mappings for databases in production compose. Use Docker networks for inter-service communication only. Databases should never be reachable from outside the container network | **HIGH** |
| I-03 | **Default database passwords in docker-compose.yml** | IA-5, V-222662 | `SQL_SA_PASSWORD:-Watch@Str0ngP4ss!`, `POSTGRES_PASSWORD:-Watch@Geo2024!` | Remove all default passwords from compose files. Use `.env` file (gitignored) or Docker secrets. In production, use managed secrets (Key Vault, Secrets Manager) | **HIGH** |
| I-04 | **No container image scanning** | SI-2, RV.1 | No Trivy, Grype, or Snyk container scanning in CI | Add Trivy or Grype scanning to `docker-publish.yml`. Fail builds on CRITICAL/HIGH CVEs. Scan base images weekly for new vulnerabilities | **HIGH** |
| I-05 | **No network policies between services** | SC-7, AC-4 | Docker Compose uses a flat network; all services can reach all other services | Define network policies (Kubernetes NetworkPolicy or Docker network segmentation). P7 FamilyHealth should not be able to reach P11 Surveillance directly. Implement zero-trust service mesh | **MEDIUM** |
| I-06 | **No resource limits on containers** | SC-5 | No `deploy.resources.limits` in Docker Compose; containers can consume unlimited CPU/RAM | Add `mem_limit`, `cpus` constraints to all services in docker-compose.yml and Kubernetes resource requests/limits. Prevents single service DoS against shared host | **MEDIUM** |
| I-07 | **Base images use `-preview` tags** | CM-2, SI-2 | `FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview` | Pin to specific digest-based image tags in production. Use `-cbl-mariner` or `-alpine` distroless base images for smaller attack surface. Update base images on a defined schedule | **MEDIUM** |

### SECTION 8: DOCUMENTATION & PROCESS (CMMC ASSESSMENT ARTIFACTS)

**Why it matters:** CMMC Level 2 requires documented policies and procedures for all 14 families. A C3PAO assessor will request these artifacts.

| ID | Finding | Standard | Current State | Required State | Severity |
|----|---------|----------|---------------|----------------|----------|
| P-01 | **No System Security Plan (SSP)** | CA-1, CA-5 | No SSP document | Create SSP documenting all 110 NIST 800-171 requirements with implementation status, responsible parties, and evidence. Use NIST SP 800-171A assessment procedures as template | **HIGH** |
| P-02 | **No Plan of Action and Milestones (POA&M)** | CA-5 | No POA&M | Create POA&M tracking all open findings from this analysis with remediation dates, responsible parties, and risk ratings. This document IS the remediation tracker | **HIGH** |
| P-03 | **No security awareness training program** | AT-1, AT-2 | No training documentation | Establish annual security awareness training for all developers/operators. Document completion. Cover CUI handling, phishing, incident reporting, secure coding | **MEDIUM** |
| P-04 | **No access control policy document** | AC-1 | RBAC implemented in code but not documented as policy | Create Access Control Policy defining: role definitions, access provisioning/deprovisioning procedures, principle of least privilege, periodic access reviews (quarterly minimum) | **MEDIUM** |
| P-05 | **No configuration management plan** | CM-1, CM-9 | IaC (Terraform) exists but no CM plan document | Document CM plan: baseline configurations, change control process, configuration deviation handling, authorized software list | **LOW** |
| P-06 | **No data flow diagrams** | PL-2, SC-7 | Architecture exists in code but not documented visually | Create data flow diagrams showing: CUI boundaries, encryption points, authentication boundaries, network zones, external interfaces. Required for SSP and STIG assessment | **LOW** |

---

## Compliance Scorecard

### NIST 800-171 Rev 2 (110 Requirements) — Estimated CMMC Level 2 Readiness

| Control Family | Total Reqs | Fully Met | Partially Met | Not Met | Notes |
|----------------|-----------|-----------|---------------|---------|-------|
| AC — Access Control | 22 | 14 | 5 | 3 | RBAC strong; missing CAC/PIV, network segmentation |
| AT — Awareness & Training | 3 | 0 | 0 | 3 | No training program documented |
| AU — Audit & Accountability | 9 | 5 | 3 | 1 | Serilog strong; missing integrity protection, SIEM |
| CM — Configuration Management | 9 | 5 | 3 | 1 | IaC good; missing CM plan, baseline docs |
| IA — Identification & Authentication | 11 | 6 | 4 | 1 | MFA strong; passwords below STIG minimum |
| IR — Incident Response | 3 | 1 | 1 | 1 | Detection exists; no formal IR plan |
| MA — Maintenance | 6 | 2 | 2 | 2 | CI/CD handles some; needs policy docs |
| MP — Media Protection | 9 | 2 | 3 | 4 | Backup exists; no encryption, sanitization |
| PE — Physical Protection | 6 | 0 | 0 | 6 | Cloud provider responsibility; needs documentation |
| PS — Personnel Security | 2 | 0 | 1 | 1 | Needs screening policy, access agreements |
| RA — Risk Assessment | 3 | 1 | 2 | 0 | STRIDE/MITRE exist; needs formal RA process |
| CA — Security Assessment | 4 | 0 | 2 | 2 | No SSP, no POA&M, no formal assessment |
| SC — System & Comms Protection | 16 | 7 | 6 | 3 | Security headers good; FIPS crypto gap critical |
| SI — System & Information Integrity | 7 | 4 | 2 | 1 | Scanning good; input validation incomplete |
| **TOTALS** | **110** | **47** | **34** | **29** | **43% full / 74% partial+full** |

### OWASP Top 10:2021 Coverage

| Category | Status | Evidence |
|----------|--------|----------|
| A01 — Broken Access Control | **PARTIAL** | RBAC implemented; missing CSRF tokens on state-changing ops, some endpoints lack authorization |
| A02 — Cryptographic Failures | **GAP** | No FIPS mode, no data-at-rest encryption, JWT symmetric key |
| A03 — Injection | **PARTIAL** | EF Core parameterized queries; missing FluentValidation on all DTOs |
| A04 — Insecure Design | **GOOD** | STRIDE threat modeling, defense-in-depth architecture, rate limiting |
| A05 — Security Misconfiguration | **PARTIAL** | Security headers present; default passwords in compose, verbose `/info` endpoints |
| A06 — Vulnerable Components | **GOOD** | NuGet vulnerability audit in CI, Dependabot, CodeQL |
| A07 — Auth Failures | **GOOD** | ASP.NET Identity, Argon2id, MFA (4 methods), lockout, brute force detection |
| A08 — Integrity Failures | **GAP** | No container signing, no SBOM, no NuGet signature verification |
| A09 — Logging Failures | **PARTIAL** | Serilog everywhere; no SIEM, no log integrity, no PII masking |
| A10 — SSRF | **UNKNOWN** | No explicit SSRF protections found; HttpClient usage needs review |

---

## Remediation Roadmap (Priority-Ordered)

### Sprint 1 — CRITICAL Crypto & Data Protection (Highest Impact)

| Task | Files Affected | LOE |
|------|---------------|-----|
| Enable FIPS-compliant TLS (TLS 1.2/1.3 only, approved cipher suites) | All 12 `Program.cs` + Kestrel config | 1 day |
| Migrate JWT from symmetric HMAC to asymmetric RSA-2048/ECDSA | `WatchAuthExtensions.cs`, `SecurityGenerator.cs`, P5 `AuthService`, all `appsettings.json` | 2 days |
| Remove default passwords from `docker-compose.yml` → `.env` file | `docker-compose.yml`, `docker-compose.override.yml` | 0.5 day |
| Add non-root `USER` directive to all 12 Dockerfiles | 12 Dockerfiles | 0.5 day |
| Configure SQL Server TDE + PostgreSQL encryption at rest | Terraform modules, Docker init scripts | 2 days |
| Configure Redis AUTH + TLS | `docker-compose.yml`, all services' Redis config | 1 day |
| Configure Kafka SASL_SSL | `docker-compose.yml`, Kafka producer/consumer config in Shared | 1 day |

### Sprint 2 — Authentication & Password Hardening

| Task | Files Affected | LOE |
|------|---------------|-----|
| Increase minimum password length to 15 characters | P5 `Program.cs` line 62 | 0.1 day |
| Reduce max failed login attempts to 3 | P5 `Program.cs` line 66 | 0.1 day |
| Implement password history (5 generations) | New `PasswordHistory` entity + migration, P5 `AuthService` | 1 day |
| Implement password max age (60 days) + min age (24 hours) | `WatchUser` model, P5 `AuthService`, login flow | 1 day |
| Move SMS OTP storage from ConcurrentDictionary to Redis | P5 `SmsMfaService` | 0.5 day |
| Add CAC/PIV certificate authentication scheme | `WatchAuthExtensions.cs`, P5 `Program.cs` | 2 days |
| Enforce device fingerprint binding on refresh tokens | P5 `AuthService.RefreshAsync()` | 0.5 day |

### Sprint 3 — Input Validation & Error Handling

| Task | Files Affected | LOE |
|------|---------------|-----|
| Add FluentValidation to all 12 microservices | 12 `.csproj` files, 50+ validator classes | 3 days |
| Add global `ProblemDetails` exception middleware to all services | Shared middleware class + 11 `Program.cs` files | 1 day |
| Apply Kestrel request size limits to all services | 11 `Program.cs` files (P1-P11 + Geospatial) | 0.5 day |
| Remove host port mappings from production Docker Compose | `docker-compose.yml` + production override | 0.5 day |
| Validate all query string parameters against allowlists | Review all `MapGet`/`MapPost` handlers | 1 day |

### Sprint 4 — SDLC & Supply Chain

| Task | Files Affected | LOE |
|------|---------------|-----|
| Add SBOM generation (`dotnet CycloneDX`) to CI pipeline | `.github/workflows/docker-publish.yml` | 0.5 day |
| Add container image scanning (Trivy) to CI | `.github/workflows/docker-publish.yml` | 0.5 day |
| Add DAST scanning (OWASP ZAP) to staging pipeline | `.github/workflows/deploy-staging.yml` | 1 day |
| Enable NuGet audit + signature verification | `Directory.Build.props`, `nuget.config` | 0.5 day |
| Replace regex secret scanner with Gitleaks | `.github/workflows/security.yml` | 0.5 day |
| Add Cosign container image signing to publish pipeline | `.github/workflows/docker-publish.yml` | 1 day |
| Enable GPG commit signing requirement | GitHub repo settings, developer setup docs | 0.5 day |

### Sprint 5 — Logging & Monitoring

| Task | Files Affected | LOE |
|------|---------------|-----|
| Deploy centralized SIEM (Seq or Elastic) | New `docker-compose.siem.yml`, Serilog sink config | 2 days |
| Implement audit log integrity (HMAC signing per entry) | `SecurityAuditMiddleware.cs`, new `LogIntegrityService` | 1 day |
| Configure log retention policies (1 year security, 90 days general) | Serilog config in all `appsettings.json` | 0.5 day |
| Add PII masking to Serilog destructuring | Shared Serilog enricher + all services | 1 day |
| Restrict `/info` endpoints to authenticated requests | All 12 `Program.cs` files | 0.5 day |
| Column-level encryption for PII/CUI fields | EF entity configurations, migration | 3 days |

### Sprint 6 — Documentation & Assessment Prep

| Task | Files Affected | LOE |
|------|---------------|-----|
| Write System Security Plan (SSP) mapped to 110 controls | New `docs/ssp/` directory | 3 days |
| Create POA&M from this analysis | New `docs/POA&M.xlsx` | 1 day |
| Write Incident Response Plan (IRP) | New `docs/incident-response-plan.md` | 1 day |
| Write Access Control Policy | New `docs/policies/access-control.md` | 0.5 day |
| Create data flow diagrams (CUI boundaries) | New `docs/diagrams/` | 1 day |
| Complete SSDF self-attestation form | New `docs/ssdf-attestation.pdf` | 0.5 day |
| Establish security training program | New `docs/training/` | 0.5 day |

---

## Risk Register (Top 10)

| Rank | Risk | Likelihood | Impact | Current Mitigation | Residual Risk |
|------|------|-----------|--------|-------------------|---------------|
| 1 | Non-FIPS cryptographic modules fail audit | High | Critical | None | **Critical** |
| 2 | CUI data breach via unencrypted database | Medium | Critical | Network isolation only | **High** |
| 3 | JWT signing key compromise (shared symmetric) | Medium | Critical | Env variable storage | **High** |
| 4 | SQL injection via unvalidated input | Low | High | EF Core parameterization | **Medium** |
| 5 | Container escape via root execution | Low | High | None | **High** |
| 6 | Credential stuffing below lockout threshold | Medium | Medium | 5-attempt lockout | **Medium** |
| 7 | Supply chain attack via unsigned packages | Low | High | NuGet vulnerability scan | **Medium** |
| 8 | Insider threat via unrestricted service mesh | Low | High | RBAC | **Medium** |
| 9 | Log tampering destroys forensic evidence | Low | Medium | File-based logging | **Medium** |
| 10 | CMMC assessment failure due to missing SSP | High | High | None | **High** |

---

## Appendix A: File-Level Reference for Key Findings

| Finding | File Path | Line(s) |
|---------|-----------|---------|
| Password length = 8 | `TheWatch.P5.AuthSecurity/Program.cs` | 62 |
| Lockout attempts = 5 | `TheWatch.P5.AuthSecurity/Program.cs` | 66 |
| JWT symmetric key config | `TheWatch.Shared/Auth/WatchAuthExtensions.cs` | 35 |
| CORS `SetIsOriginAllowed` | Decision D034 (documented pattern) | — |
| Default SQL password | `docker-compose.yml` | 16 |
| Default Postgres password | `docker-compose.yml` | 36 |
| Redis no auth | `docker-compose.yml` | 78-90 |
| Kafka PLAINTEXT | `docker-compose.yml` | 56 |
| Dockerfile no USER | `TheWatch.P5.AuthSecurity/Dockerfile` | (all 12 files) |
| Security headers middleware | `TheWatch.Shared/Security/SecurityHeadersMiddleware.cs` | 1-45 |
| Rate limiting generator | `TheWatch.Generators/SecurityGenerator.cs` | 146-202 |
| CI security pipeline | `.github/workflows/security.yml` | 1-180 |

---

## Appendix B: Standards Quick-Reference Links

1. **NIST SP 800-171 Rev 2** — https://csrc.nist.gov/pubs/sp/800/171/r2/upd1/final
2. **CMMC 2.0 Program** — https://dodcio.defense.gov/CMMC/
3. **NIST SP 800-53 Rev 5** — https://csrc.nist.gov/pubs/sp/800/53/r5/upd1/final
4. **OWASP Top 10:2021** — https://owasp.org/Top10/2021/
5. **DISA ASD STIG v6r1** — https://www.stigviewer.com/stigs/application_security_and_development
6. **NIST SP 800-218 SSDF** — https://csrc.nist.gov/pubs/sp/800/218/final
7. **CISA SSDF Self-Attestation Form** — https://www.cisa.gov/secure-software-attestation-form
8. **DFARS 252.204-7012** — https://www.acquisition.gov/dfars/252.204-7012
9. **Federal Register CMMC Final Rule** — https://www.federalregister.gov/documents/2024/10/15/2024-22905/cybersecurity-maturity-model-certification-cmmc-program
