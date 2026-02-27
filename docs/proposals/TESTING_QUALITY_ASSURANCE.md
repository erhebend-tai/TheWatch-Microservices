# Testing & Quality Assurance — TheWatch Platform

> **Classification:** CUI // SP-BASIC  
> **Reference Standards:** NIST SP 800-53 Rev 5 (SA, SI families), OWASP Top 10:2021,
> OWASP ASVS v4.0, NIST SP 800-115  
> **Document ID:** TQA-001  
> **Version:** 1.0

---

## 1. Testing Strategy Overview

TheWatch employs a multi-layered testing strategy that integrates security testing,
functional verification, and compliance validation throughout the development lifecycle.

### 1.1 Testing Pyramid

| Layer | Type | Tools | Scope | Standard |
|-------|------|-------|-------|----------|
| **Unit** | Component isolation | xUnit, FluentAssertions | Business logic, services, utilities | SSDF PW.7 |
| **Integration** | Service-level | xUnit, TestContainers | API endpoints, database operations, auth flows | SSDF PW.7 |
| **Security (SAST)** | Static analysis | CodeQL, .NET Analyzers | Source code vulnerability detection | NIST 800-53 SA-11(1) |
| **Security (SCA)** | Composition analysis | NuGet Audit, Trivy, Dependency Review | Dependency vulnerabilities | NIST 800-53 RA-5 |
| **Security (Secret)** | Secret detection | Gitleaks | Hardcoded credentials, API keys | NIST 800-53 SC-28 |
| **Compliance** | Standards validation | DoD compliance/readiness workflows | CMMC, NIST, DISA STIG checks | CMMC Level 2 |
| **Container** | Image scanning | Trivy | OS-level vulnerabilities in containers | NIST 800-53 RA-5 |
| **Penetration** | Active testing | Burp Suite, Nessus, OWASP ZAP, Metasploit | Application and infrastructure | NIST 800-53 CA-8 |

### 1.2 Test Project Inventory

| Test Project | Service Under Test | Framework | Test Count | Focus Areas |
|-------------|-------------------|-----------|------------|-------------|
| TheWatch.P1.CoreGateway.Tests | P1 CoreGateway | xUnit | Integration | Gateway routing, health checks |
| TheWatch.P2.VoiceEmergency.Tests | P2 VoiceEmergency | xUnit | Integration | SOS ingestion, dispatch |
| TheWatch.P3.MeshNetwork.Tests | P3 MeshNetwork | xUnit | Integration | Mesh relay, offline comms |
| TheWatch.P4.Wearable.Tests | P4 Wearable | xUnit | Integration | Device provisioning, telemetry |
| TheWatch.P5.AuthSecurity.Tests | P5 AuthSecurity | xUnit | Integration | Auth, MFA, RBAC, brute force |
| TheWatch.P6.FirstResponder.Tests | P6 FirstResponder | xUnit | Integration | Dispatch, location tracking |
| TheWatch.P7.FamilyHealth.Tests | P7 FamilyHealth | xUnit | Integration | Vitals, geofencing, alerts |
| TheWatch.P8.DisasterRelief.Tests | P8 DisasterRelief | xUnit | Integration | Shelters, evacuation |
| TheWatch.P9.DoctorServices.Tests | P9 DoctorServices | xUnit | Integration | Appointments, telehealth |
| TheWatch.P10.Gamification.Tests | P10 Gamification | xUnit | Integration | Badges, challenges |
| TheWatch.P11.Surveillance.Tests | P11 Surveillance | xUnit | Integration | Camera, footage, detection |
| TheWatch.Mobile.Tests | Mobile App | xUnit, FluentAssertions | Unit | Content moderation, offline queue, sync |

---

## 2. Security Testing

### 2.1 Static Application Security Testing (SAST)

| Tool | Scope | Configuration | Trigger | Standard |
|------|-------|--------------|---------|----------|
| **CodeQL** | Full source code | Default security queries + extended queries | Every push and PR | NIST 800-53 SA-11(1) |
| **.NET Analyzers** | C# source code | `latest-recommended` ruleset; treat warnings as errors in CI | Every build | SSDF PW.5 |
| **Roslyn Analyzers** | C# source code | 10 custom source generators enforce patterns | Compile-time | SSDF PW.5 |

### 2.2 Software Composition Analysis (SCA)

| Tool | Scope | Configuration | Standard |
|------|-------|--------------|----------|
| **NuGet Audit** | .NET NuGet packages | `audit` mode; low severity threshold; fail build on vulnerable | NIST 800-53 RA-5 |
| **GitHub Dependency Review** | All dependencies | PR diff analysis; security advisory cross-reference | NIST 800-53 RA-5 |
| **Trivy** | Container images | OS packages + application dependencies; HIGH/CRITICAL fail | NIST 800-53 RA-5 |
| **Renovate** | Dependency updates | Automated PR creation for outdated packages | NIST 800-53 SI-2 |

### 2.3 Secret Detection

| Tool | Rules | Allowlisted Paths | Standard |
|------|-------|-------------------|----------|
| **Gitleaks** | JWT keys (16+ chars), SQL connection strings, API keys | `.env.example`, `docker-compose*.yml`, `docs/*.md`, `appsettings.*.template.json` | NIST 800-53 SC-28 |

**Evidence:** `.gitleaks.toml`

### 2.4 Penetration Testing Program

| Aspect | Detail | Standard |
|--------|--------|----------|
| **Frequency** | Annual minimum + pre-release major versions + targeted assessments | NIST 800-53 CA-8 |
| **Scope** | 13 microservices, mobile app, API gateway, admin CLI, infrastructure | NIST 800-115 |
| **Categories** | External network, internal network, web apps, APIs, mobile, social engineering | OWASP Testing Guide v4.2 |
| **Tooling** | Burp Suite, Nessus, Metasploit, OWASP ZAP, MobSF, sqlmap | Industry standard |
| **Classification** | CVSS-based: CRITICAL (9-10), HIGH (7-8.9), MEDIUM (4-6.9), LOW (0.1-3.9) | DISA CAT I/II/III |
| **Remediation SLA** | Critical 7d, High 30d, Medium 90d, Low 180d | Organization policy |
| **ROE Template** | Testing hours, out-of-scope, emergency contacts, stop procedures | NIST 800-115 |

**Evidence:** `docs/pentest-program.md`

---

## 3. OWASP Top 10:2021 Coverage

| # | Risk | TheWatch Controls | Status | Standard |
|---|------|-------------------|--------|----------|
| **A01** | Broken Access Control | RBAC (6 policies); IDOR prevention (`CallerCanAccessUser`); JWT auth required; rate limiting | **[COVERED]** | OWASP A01 |
| **A02** | Cryptographic Failures | AES-256-GCM field encryption; TLS 1.2+; Argon2id/PBKDF2 hashing; HSTS | **[PARTIAL]** | OWASP A02 — POA&M: FIPS mode, TDE |
| **A03** | Injection | Entity Framework Core (parameterized queries); Roslyn analyzers; CodeQL scanning | **[COVERED]** | OWASP A03 |
| **A04** | Insecure Design | STRIDE threat modeling; MITRE ATT&CK rules; microservices isolation; defense in depth | **[COVERED]** | OWASP A04 |
| **A05** | Security Misconfiguration | Kestrel hardening; security headers; no Server header; CORS whitelist; non-root containers | **[COVERED]** | OWASP A05 |
| **A06** | Vulnerable Components | NuGet Audit; Trivy; GitHub Dependency Review; Renovate; SBOM generation | **[COVERED]** | OWASP A06 |
| **A07** | Auth Failures | MFA (4 methods); brute force protection (3-attempt lockout); device trust scoring; session management | **[COVERED]** | OWASP A07 |
| **A08** | Software & Data Integrity | Signed commits; SLSA provenance; SBOM generation; branch protection | **[PARTIAL]** | OWASP A08 — POA&M: container signing |
| **A09** | Logging & Monitoring | Serilog structured logging; correlation IDs; STRIDE scanning; security event logging | **[COVERED]** | OWASP A09 |
| **A10** | Server-Side Request Forgery | Typed HTTP clients with explicit endpoints; no dynamic URL construction from user input | **[COVERED]** | OWASP A10 |

### 3.1 OWASP ASVS v4.0 Alignment

| ASVS Chapter | Coverage Area | TheWatch Implementation | Level |
|-------------|--------------|------------------------|-------|
| **V1** | Architecture & Threat Modeling | STRIDE + MITRE ATT&CK; microservices design | L2 |
| **V2** | Authentication | MFA, password hashing, brute force protection | L2 |
| **V3** | Session Management | JWT with expiration, refresh tokens, device binding | L2 |
| **V4** | Access Control | RBAC, IDOR checks, policy-based authorization | L2 |
| **V5** | Validation, Sanitization | EF Core parameterized queries; FluentValidation planned | L1 |
| **V6** | Cryptography | AES-256-GCM, Argon2id/PBKDF2, RSA-2048 JWT | L2 |
| **V7** | Error Handling & Logging | Serilog, PII redaction, correlation IDs | L2 |
| **V8** | Data Protection | CUI marking, field encryption, TLS enforcement | L2 |
| **V9** | Communication | TLS 1.2+, HSTS, certificate pinning (mobile) | L2 |
| **V10** | Malicious Code | CodeQL, dependency review, signed commits | L2 |
| **V11** | Business Logic | Domain-driven design; bounded contexts | L1 |
| **V12** | Files & Resources | Evidence chain-of-custody; SHA-256 hashing | L2 |
| **V13** | API Security | Rate limiting, JWT, CORS, input validation (planned) | L1 |
| **V14** | Configuration | Kestrel hardening, security headers, non-root | L2 |

---

## 4. CI/CD Quality Gates

### 4.1 Pre-Merge Gates

| Gate | Tool | Criteria | Blocking |
|------|------|---------|----------|
| **Build Success** | `dotnet build` | All 54+ projects compile without errors | Yes |
| **Test Pass** | `dotnet test` | All test projects pass | Yes |
| **SAST Clean** | CodeQL | No HIGH/CRITICAL findings | Yes |
| **SCA Clean** | NuGet Audit | No vulnerabilities above threshold | Yes |
| **Secret Scan** | Gitleaks | No secrets detected | Yes |
| **Code Review** | GitHub PR | 2 approvals required | Yes |
| **Signed Commits** | GitHub | All commits GPG/SSH signed | Yes |
| **Dependency Review** | GitHub | No known vulnerable dependencies introduced | Advisory |

### 4.2 Pre-Deploy Gates

| Gate | Tool | Criteria | Blocking |
|------|------|---------|----------|
| **Container Scan** | Trivy | No HIGH/CRITICAL OS vulnerabilities | Yes |
| **SBOM Generation** | CycloneDX | SBOM generated and archived | Yes |
| **Provenance** | SLSA workflow | Provenance attestation generated | Yes |
| **Staging Smoke Test** | `deploy-staging.yml` | Health checks pass | Yes |
| **Production Approval** | GitHub Environments | Manual approval required | Yes |

### 4.3 Build Configuration Quality

| Setting | Value | Purpose | Standard |
|---------|-------|---------|----------|
| **AnalysisLevel** | `latest-recommended` | Maximum .NET analyzer coverage | SSDF PW.5 |
| **NuGetAudit** | `true` | Vulnerability scanning on restore | NIST 800-53 SR-4 |
| **NuGetAuditLevel** | `low` | Fail on any vulnerability | NIST 800-53 SR-4 |
| **TreatWarningsAsErrors** | `true` (CI) | No warnings in production builds | SSDF PW.6 |
| **Deterministic** | `true` | Reproducible builds | SLSA v1.0 |

**Evidence:** `Directory.Build.props`

---

## 5. Code Quality Practices

### 5.1 Code Generation

TheWatch uses 10 Roslyn source generators to enforce consistent patterns and reduce
human error across the codebase:

| Generator | Generated Artifact | Quality Impact |
|-----------|-------------------|---------------|
| **EndpointGenerator** | API endpoint boilerplate | Consistent routing and auth patterns |
| **ModelGenerator** | Domain entity classes | Consistent property naming and validation |
| **ServiceGenerator** | Service layer implementation | Consistent dependency injection and error handling |
| **DbContextGenerator** | EF Core database contexts | Consistent data access patterns |
| **HangfireJobGenerator** | Background job templates | Consistent job scheduling and retry logic |
| **TestGenerator** | Test class scaffolding | Consistent test structure and naming |
| **SerilogConfigGenerator** | Logging configuration | Consistent logging format and levels |
| **OpenApiSchemaGenerator** | API documentation | Consistent OpenAPI/Scalar docs |
| **SignalRHubGenerator** | Real-time hub scaffolding | Consistent WebSocket patterns |
| **ValidationGenerator** | Input validation rules | Consistent request validation |

### 5.2 Coding Standards

| Practice | Implementation | Standard |
|----------|---------------|----------|
| **Repository Pattern** | `IWatchRepository<T>` across all services | Design consistency |
| **Typed Clients** | 12 `ServiceClientBase<T>` implementations | Inter-service communication |
| **Structured Logging** | Serilog with JSON output + correlation IDs | NIST 800-53 AU-3 |
| **Health Checks** | `/healthz` and `/ready` on every service | Operational monitoring |
| **OpenAPI Documentation** | Scalar API docs on every service | API discoverability |
| **Branch Protection** | Main branch: 2 approvals, signed commits, linear history | NIST 800-53 CM-3 |

---

## 6. Mobile Application Testing

### 6.1 Mobile Test Architecture

TheWatch.Mobile.Tests uses a mirror-type pattern for testing pure business logic
independently of the MAUI platform:

| Test Category | Approach | Tools |
|--------------|---------|-------|
| **Content Moderation** | Mirror types re-implementing pure logic | xUnit + FluentAssertions |
| **Offline Queue** | Queue behavior verification (FIFO, retry, priority) | xUnit + FluentAssertions |
| **Sync Engine** | Reconciliation logic testing | xUnit + FluentAssertions |
| **Chain of Custody** | Evidence integrity verification | xUnit + FluentAssertions |

### 6.2 Mobile Security Testing

| Test Type | Tool | Coverage | Standard |
|-----------|------|---------|----------|
| **SAST** | CodeQL | Blazor Razor components and C# services | OWASP MSTG-CODE |
| **Dependency Scan** | NuGet Audit | MAUI NuGet packages | OWASP MSTG-CODE |
| **Logic Testing** | xUnit | Auth flows, offline queue, sync engine | OWASP MSTG-AUTH |
| **Dynamic Testing** | MobSF (planned) | Runtime behavior analysis | OWASP MSTG |

**Evidence:** `TheWatch.Mobile.Tests/`

---

## 7. Compliance Testing

### 7.1 DoD Compliance Workflow

The `dod-compliance.yml` workflow validates:

| Check | Validation | Standard |
|-------|-----------|----------|
| Container non-root | Verify `USER appuser` in all Dockerfiles | DISA STIG V-222425 |
| TLS configuration | Verify TLS 1.2+ enforcement | NIST 800-171 SC-8 |
| Security headers | Verify HSTS, no Server header | DISA STIG |
| Auth enforcement | Verify `[Authorize]` on controllers | NIST 800-171 AC-3 |
| SBOM presence | Verify SBOM generation | EO 14028 |

### 7.2 DoD Readiness Workflow

The `dod-readiness.yml` workflow assesses:

| Check | Validation | Standard |
|-------|-----------|----------|
| CMMC practice coverage | Control implementation evidence | CMMC Level 2 |
| NIST 800-171 alignment | Security requirement mapping | NIST 800-171 |
| Policy documentation | Existence and completeness of security policies | NIST 800-53 PL-1 |
| POA&M currency | Open findings tracked with remediation dates | NIST 800-171 CA-5 |

---

## 8. Vulnerability Remediation SLAs

| Severity | CVSS Score | Triage Time | Remediation Time | Standard |
|----------|-----------|-------------|------------------|----------|
| **CRITICAL** | 9.0–10.0 | 6 hours | 7 calendar days | DISA CAT I |
| **HIGH** | 7.0–8.9 | 24 hours | 30 calendar days | DISA CAT I/II |
| **MEDIUM** | 4.0–6.9 | 3 business days | 90 calendar days | DISA CAT II |
| **LOW** | 0.1–3.9 | 5 business days | 180 calendar days | DISA CAT III |

**Evidence:** `docs/vulnerability-management-policy.md`

---

*This Testing & Quality Assurance document demonstrates TheWatch's comprehensive
approach to software quality, security testing, and compliance validation aligned
with DoD standards.*
