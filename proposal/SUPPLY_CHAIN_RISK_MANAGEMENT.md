# Supply Chain Risk Management Plan — TheWatch Platform

> **Classification:** CUI // SP-BASIC  
> **Reference Standards:** NIST SP 800-218 (SSDF v1.1), NIST SP 800-161 Rev 1, EO 14028,
> OMB M-22-18, SLSA v1.0, CycloneDX/SPDX  
> **Document ID:** SCRM-001  
> **Version:** 1.0

---

## 1. Overview

This Supply Chain Risk Management (SCRM) Plan documents TheWatch's approach to securing
the software supply chain from development through deployment. It addresses Executive
Order 14028 requirements, OMB M-22-18 self-attestation obligations, and NIST frameworks
for supply chain security.

### 1.1 Applicable Regulations

| Regulation | Requirement | TheWatch Implementation |
|------------|-------------|------------------------|
| **EO 14028 §4(e)** | SBOM for all software sold to federal government | CycloneDX + SPDX SBOMs generated per project |
| **OMB M-22-18** | SSDF self-attestation for federal software | SSDF attestation document maintained |
| **NIST SP 800-218** | Secure Software Development Framework practices | All 4 practice groups implemented |
| **NIST SP 800-161** | C-SCRM practices and controls | Supply chain controls documented below |
| **DFARS 252.204-7012** | CUI safeguarding in supply chain | CUI marking and access controls |
| **NIST SP 800-53 SA-12** | Supply chain protection | Automated scanning and monitoring |
| **NIST SP 800-53 SR-4** | Provenance | Build provenance and artifact signing |

---

## 2. SSDF v1.1 Practice Implementation

### 2.1 Prepare the Organization (PO)

| Practice | Task | Implementation | Status | Evidence |
|----------|------|----------------|--------|----------|
| **PO.1** | Define security requirements for development | STRIDE threat modeling integrated; MITRE ATT&CK detection rules seeded (T1078, T1110, T1528, T1621, T1556); security policies documented | **[IMPLEMENTED]** | `docs/incident-response-plan.md`, `TheWatch.P5.AuthSecurity/` |
| **PO.2** | Implement roles and responsibilities | 6 RBAC roles defined; CODEOWNERS for security-critical paths; Admin approval for privileged access | **[IMPLEMENTED]** | `docs/policies/access-control-policy.md` |
| **PO.3** | Implement supporting toolchain | CodeQL (SAST), Trivy (container scanning), Gitleaks (secrets), OWASP ZAP (DAST planned), CycloneDX (SBOM), .NET analyzers, Renovate (dependency updates) | **[IMPLEMENTED]** | `.github/workflows/security.yml`, `renovate.json` |
| **PO.4** | Define and use criteria for security checks | CI pipeline gates: NuGetAudit (fail on low), CodeQL findings block merge, branch protection (2 approvals), signed commits required | **[IMPLEMENTED]** | `Directory.Build.props`, `.github/workflows/ci.yml` |
| **PO.5** | Implement and maintain secure environments | Ephemeral CI runners; Docker multi-stage builds; non-root containers (UID 1001); TLS enforcement | **[IMPLEMENTED]** | `.github/workflows/`, Dockerfiles |

### 2.2 Protect the Software (PS)

| Practice | Task | Implementation | Status | Evidence |
|----------|------|----------------|--------|----------|
| **PS.1** | Protect all forms of code from unauthorized access and tampering | GPG/SSH signed commits required; branch protection (2 approvals, linear history, no force pushes); CODEOWNERS for security paths | **[IMPLEMENTED]** | `docs/developer-setup.md` |
| **PS.2** | Provide a mechanism for verifying software release integrity | SBOM generation (CycloneDX + SPDX); SLSA provenance workflow; container image signing (Cosign planned); artifact retention policies (1d builds, 30d reports, 90d SBOMs) | **[PARTIAL]** | `.github/workflows/slsa-provenance.yml`, `.github/workflows/sbom-aggregate.yml`, `generate-sbom.sh` |
| **PS.3** | Archive and protect each software release | GitHub Releases with tagged versions; Docker registry with immutable tags; SBOM archived per release | **[IMPLEMENTED]** | `.github/workflows/docker-publish.yml` |

### 2.3 Produce Well-Secured Software (PW)

| Practice | Task | Implementation | Status | Evidence |
|----------|------|----------------|--------|----------|
| **PW.1** | Design software to meet security requirements | Zero-trust architecture; microservices isolation; defense in depth; CUI data classification per entity | **[IMPLEMENTED]** | Architecture design, `docs/data-classification-matrix.md` |
| **PW.2** | Review the software design | Security architecture documented; threat model maintained; DoD Security Analysis with gap analysis | **[IMPLEMENTED]** | `DOD_SECURITY_ANALYSIS.md` |
| **PW.4** | Reuse existing, well-secured software | NuGet packages from trusted sources; central version management (`Directory.Packages.props`); NuGetAudit enabled | **[IMPLEMENTED]** | `Directory.Packages.props`, `nuget.config` |
| **PW.5** | Create source code following secure coding practices | .NET analyzers (latest-recommended); Roslyn source generators (10 generators) for consistent patterns; Argon2id/PBKDF2 password hashing; AES-256-GCM encryption; parameterized queries (EF Core) | **[IMPLEMENTED]** | `Directory.Build.props`, `TheWatch.Generators/` |
| **PW.6** | Configure the compilation, interpreter, and build processes | Deterministic builds enabled; NuGetAudit treats vulnerabilities as errors in CI; central build configuration | **[IMPLEMENTED]** | `Directory.Build.props` |
| **PW.7** | Review and test code for vulnerabilities and verify compliance | CodeQL SAST on every push/PR; Trivy container scanning; 12 test projects with xUnit; penetration testing program | **[IMPLEMENTED]** | `.github/workflows/security.yml`, `docs/pentest-program.md` |
| **PW.8** | Test executable code for vulnerabilities | DoD compliance workflow; DoD readiness workflow; SBOM vulnerability scanning; security scanning pipeline | **[IMPLEMENTED]** | `.github/workflows/dod-compliance.yml`, `.github/workflows/dod-readiness.yml` |
| **PW.9** | Configure software to have secure settings by default | TLS 1.2+ enforced; Kestrel hardened; HSTS 365d; rate limiting enabled; CORS restricted; no Server header; 30s headers timeout | **[IMPLEMENTED]** | `TheWatch.Shared/Security/WatchKestrelExtensions.cs` |

### 2.4 Respond to Vulnerabilities (RV)

| Practice | Task | Implementation | Status | Evidence |
|----------|------|----------------|--------|----------|
| **RV.1** | Identify and confirm vulnerabilities | VulnerabilityMonitorService (Hangfire background job); monitors CISA KEV, NuGet Advisory DB, GitHub Advisory DB; Trivy + CodeQL + Gitleaks in CI | **[IMPLEMENTED]** | `docs/vulnerability-management-policy.md` |
| **RV.2** | Assess, prioritize, and remediate vulnerabilities | SLA-based: Critical 7d, High 30d, Medium 90d, Low 180d; CVSS scoring; POA&M tracking | **[IMPLEMENTED]** | `docs/vulnerability-management-policy.md`, `docs/POA&M.md` |
| **RV.3** | Analyze vulnerabilities to identify root cause | Post-incident review process; root cause analysis template; lessons-learned documentation | **[IMPLEMENTED]** | `docs/incident-response-plan.md` |

---

## 3. Software Bill of Materials (SBOM)

### 3.1 SBOM Generation Process

TheWatch generates comprehensive SBOMs using the `generate-sbom.sh` script and CI/CD workflows:

| Step | Tool | Output | Standard |
|------|------|--------|----------|
| **1. Per-Project SBOM** | `dotnet-CycloneDX` | Individual CycloneDX JSON per project (16 projects) | EO 14028 §4(e) |
| **2. SBOM Aggregation** | `cyclonedx-cli merge` | Merged CycloneDX JSON/XML | NTIA Minimum Elements |
| **3. Format Conversion** | `cyclonedx-cli convert` | SPDX format for broad compatibility | SPDX 2.3 |
| **4. Vulnerability Scan** | CI pipeline scanning | Known vulnerability cross-reference | NIST 800-53 RA-5 |
| **5. Archival** | GitHub Artifacts | 90-day retention with artifact signing | NIST 800-53 SR-4 |

### 3.2 SBOM Coverage

| Project | Included | CUI Level |
|---------|----------|-----------|
| TheWatch.P1.CoreGateway | ✅ | SP-BASIC |
| TheWatch.P2.VoiceEmergency | ✅ | SP-LEI |
| TheWatch.P3.MeshNetwork | ✅ | SP-BASIC |
| TheWatch.P4.Wearable | ✅ | SP-PRIV |
| TheWatch.P5.AuthSecurity | ✅ | SP-PRIV |
| TheWatch.P6.FirstResponder | ✅ | SP-LEI |
| TheWatch.P7.FamilyHealth | ✅ | SP-HLTH |
| TheWatch.P8.DisasterRelief | ✅ | SP-GEO |
| TheWatch.P9.DoctorServices | ✅ | SP-HLTH |
| TheWatch.P10.Gamification | ✅ | SP-BASIC |
| TheWatch.P11.Surveillance | ✅ | SP-LEI |
| TheWatch.Geospatial | ✅ | SP-GEO |
| TheWatch.Dashboard | ✅ | SP-BASIC |
| TheWatch.Admin.RestAPI | ✅ | SP-BASIC |
| TheWatch.Shared | ✅ | SP-BASIC |
| TheWatch.Generators | ✅ | SP-BASIC |

### 3.3 NTIA Minimum Elements Compliance

| Element | Provided | Source |
|---------|----------|--------|
| Supplier Name | ✅ | NuGet package metadata |
| Component Name | ✅ | CycloneDX component name |
| Version | ✅ | Package version string |
| Unique Identifier | ✅ | CycloneDX bom-ref / SPDX SPDXID |
| Dependency Relationship | ✅ | CycloneDX dependency graph |
| Author of SBOM | ✅ | Build pipeline metadata |
| Timestamp | ✅ | CycloneDX creation timestamp |

---

## 4. SLSA Provenance

### 4.1 SLSA Level Achievement

| SLSA Requirement | Level | Implementation | Status |
|------------------|-------|----------------|--------|
| **Source — Version Controlled** | L1+ | Git repository with full history | **[MET]** |
| **Source — Verified History** | L2+ | Signed commits required; branch protection | **[MET]** |
| **Build — Scripted Build** | L1+ | GitHub Actions workflows; declarative CI/CD | **[MET]** |
| **Build — Build Service** | L2+ | GitHub-hosted runners (ephemeral) | **[MET]** |
| **Build — Provenance Generated** | L1+ | SLSA provenance workflow (`slsa-provenance.yml`) | **[MET]** |
| **Build — Isolated** | L3 | Ephemeral CI runners; no shared state | **[PARTIAL]** |
| **Build — Parameterless** | L3 | Workflow inputs from trusted sources only | **[PARTIAL]** |
| **Provenance — Available** | L1+ | Artifacts with provenance metadata | **[MET]** |
| **Provenance — Authenticated** | L2+ | GitHub-signed workflow attestations | **[MET]** |
| **Provenance — Non-falsifiable** | L3 | Provenance generated by build service | **[PARTIAL]** |

### 4.2 Provenance Workflow

```
Source Code (signed commits)
  → GitHub Actions (ephemeral runner)
    → .NET Build (deterministic)
      → Docker Image (multi-stage build)
        → SBOM Generation (CycloneDX)
          → Provenance Attestation (SLSA)
            → Container Registry (signed push)
              → Deployment (verified pull)
```

**Evidence:** `.github/workflows/slsa-provenance.yml`

---

## 5. Dependency Management

### 5.1 Dependency Governance

| Control | Implementation | Standard |
|---------|---------------|----------|
| **Central Version Management** | `Directory.Packages.props` pins all NuGet versions centrally | NIST 800-53 CM-2 |
| **Vulnerability Scanning** | `NuGetAudit` enabled with low-severity threshold; fails build on vulnerable dependencies | NIST 800-53 RA-5 |
| **Automated Updates** | Renovate bot creates PRs for dependency updates | NIST 800-53 SI-2 |
| **License Compliance** | SBOM includes license inventory; MIT license for project | EO 14028 |
| **Source Restrictions** | `nuget.config` restricts NuGet sources to nuget.org and local Hangfire Pro source | NIST 800-53 CM-7 |
| **Signature Verification** | NuGet package signature verification available; enforcement planned | NIST 800-53 SI-7 |

### 5.2 Vulnerability Monitoring Pipeline

```
┌─────────────────────────────────────────────────────┐
│              CONTINUOUS MONITORING                    │
│                                                       │
│  ┌─────────────┐  ┌──────────────┐  ┌─────────────┐ │
│  │ CISA KEV    │  │ NuGet        │  │ GitHub      │ │
│  │ Catalog     │  │ Advisory DB  │  │ Advisory DB │ │
│  └──────┬──────┘  └──────┬───────┘  └──────┬──────┘ │
│         │                │                  │        │
│         └────────┬───────┴──────────────────┘        │
│                  ▼                                    │
│   VulnerabilityMonitorService (Hangfire)              │
│                  │                                    │
│         ┌────────┴────────┐                          │
│         ▼                 ▼                          │
│   Triage (6h-5d)   POA&M Update                     │
│         │                                            │
│   Remediation (7-180d SLA)                           │
│         │                                            │
│   Verification & Close                               │
└─────────────────────────────────────────────────────┘
```

### 5.3 CI/CD Security Gates

| Gate | Tool | Trigger | Action on Failure |
|------|------|---------|-------------------|
| **NuGet Audit** | `dotnet restore` | Every build | Build fails |
| **CodeQL SAST** | GitHub CodeQL | Every push/PR | PR blocked |
| **Dependency Review** | GitHub Dependency Review | Every PR | PR annotated |
| **Secret Scanning** | Gitleaks | Every push | Alert generated |
| **Container Scan** | Trivy | Container build | Build fails |
| **SBOM Generation** | CycloneDX | Release builds | SBOM archived |
| **Provenance** | SLSA workflow | Release builds | Attestation generated |

---

## 6. Container Supply Chain Security

### 6.1 Image Security

| Control | Implementation | Standard |
|---------|---------------|----------|
| **Base Image Pinning** | Specific .NET 10 base image tags in Dockerfiles | NIST 800-53 CM-2 |
| **Multi-Stage Builds** | SDK used only for build; runtime-only in final image | DISA STIG |
| **Non-Root Execution** | `appuser` (UID 1001) in all production images | DISA STIG V-222425 |
| **Image Scanning** | Trivy scanning in CI; vulnerability threshold enforcement | NIST 800-53 RA-5 |
| **Image Signing** | Cosign container signing planned for production | NIST 800-53 SI-7 |
| **Registry Security** | Azure Container Registry with access controls | NIST 800-53 AC-3 |
| **Layer Minimization** | No unnecessary packages in runtime images | DISA STIG |

### 6.2 Dockerfile Security Pattern

Each of the 12 Dockerfiles follows the same hardened pattern:

```dockerfile
# Stage 1: Build (SDK image)
FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS build
WORKDIR /src
COPY . .
RUN dotnet publish -c Release -o /app

# Stage 2: Runtime (minimal image)
FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview AS runtime
RUN groupadd -r appuser && useradd -r -g appuser -u 1001 appuser
WORKDIR /app
COPY --from=build /app .
USER appuser                    # DISA STIG V-222425
EXPOSE 8080
ENTRYPOINT ["dotnet", "TheWatch.*.dll"]
```

---

## 7. Third-Party Risk Assessment

### 7.1 Critical Dependencies

| Dependency | Version | Purpose | Risk Level | Mitigation |
|------------|---------|---------|------------|------------|
| **.NET 10** | Preview | Runtime framework | LOW | Microsoft-supported; LTS planned |
| **Entity Framework Core** | 10.x | ORM/data access | LOW | Parameterized queries; no raw SQL |
| **SignalR** | 10.x | Real-time messaging | LOW | Microsoft-supported; JWT auth |
| **Hangfire Pro** | Latest | Background jobs | MEDIUM | Licensed; local NuGet source |
| **Serilog** | Latest | Structured logging | LOW | Widely adopted; well-audited |
| **Apache Kafka** | 7.7.0 | Event streaming | MEDIUM | Apache Foundation; TLS planned |
| **Redis** | 7.x | Caching/sessions | MEDIUM | Auth + TLS planned |
| **SQL Server** | 2022 | Primary database | LOW | Microsoft-supported; TDE available |
| **PostgreSQL** | 16 | Geospatial database | LOW | PostGIS extension; well-audited |
| **ML.NET/ONNX** | Latest | Object detection | MEDIUM | Local execution; no cloud dependency |
| **Radzen Blazor** | Latest | UI components | LOW | Well-maintained; open-source |

### 7.2 Supply Chain Risk Mitigations

| Risk | Mitigation | NIST 800-161 Control |
|------|-----------|---------------------|
| Compromised dependency | NuGetAudit + GitHub Advisory monitoring | SR-3 |
| Malicious code injection | Signed commits + branch protection + code review | SR-4 |
| Build environment compromise | Ephemeral CI runners + SLSA provenance | SR-5 |
| Container image tampering | Image scanning + signing + registry access control | SR-6 |
| Insider threat | 2-approval PRs + CODEOWNERS + signed commits | SR-7 |
| License compliance | SBOM license inventory + MIT project license | SR-8 |

---

*This Supply Chain Risk Management Plan ensures TheWatch meets federal software
supply chain security requirements across EO 14028, OMB M-22-18, and NIST frameworks.*
