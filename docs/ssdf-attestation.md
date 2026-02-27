# TheWatch CISA Secure Software Self-Attestation Common Form

**Document ID:** TW-SEC-341
**Version:** 1.0
**Classification:** CUI // SP-INFOSEC
**Last Reviewed:** 2026-02-26
**Owner:** TheWatch Security Engineering
**Applicable References:** SSDF v1.1 (NIST SP 800-218), OMB M-22-18, Executive Order 14028 (Improving the Nation's Cybersecurity)

---

## 1. Purpose

This document constitutes TheWatch's self-attestation of conformance with the Secure Software Development Framework (SSDF) version 1.1, as published by NIST in SP 800-218 and required by OMB Memorandum M-22-18 for software used by the Federal Government. This attestation covers all four SSDF practice groups and describes the specific measures TheWatch implements to satisfy each practice area.

TheWatch is a DoD-oriented emergency response platform engineered for sub-2-second geospatial response, comprising 13 microservices, a .NET MAUI mobile application, an Admin REST API Gateway, and an Admin CLI. Given its deployment in mission-critical DoD environments handling Controlled Unclassified Information (CUI), TheWatch adheres to security practices that meet or exceed SSDF requirements.

## 2. Attestation Scope

| Component | Description |
|---|---|
| Software Name | TheWatch Emergency Response Platform |
| Version | Current production release |
| Components | 13 microservices (P1-P11, GeospatialService, DashboardService), .NET MAUI mobile app, Admin REST API Gateway, Admin CLI |
| Development Language | C# / .NET 8+ |
| Deployment Model | Containerized microservices (Docker), orchestrated deployment |
| Data Classification | CUI (Controlled Unclassified Information); HIPAA-covered medical data in select services |

## 3. Attestation Authority

| Role | Name | Title | Date |
|---|---|---|---|
| Software Producer Representative | [Name] | [Title] | [Date] |
| Security Engineering Lead | [Name] | [Title] | [Date] |

*By signing this attestation, the above individuals confirm that the software producer follows the secure software development practices described herein, as attested in accordance with OMB M-22-18.*

---

## 4. SSDF Practice Group: PO -- Prepare the Organization

**Objective:** Ensure that the organization's people, processes, and technology are prepared to perform secure software development at the organizational level.

### PO.1 -- Define Security Requirements for Software Development

**TheWatch Implementation:**

TheWatch maintains a comprehensive set of security requirements derived from the following authoritative sources:

- **CMMC Level 2 Practices**: All 110 practices from NIST SP 800-171 Rev. 2 are mapped to TheWatch implementation controls, tracked in the compliance matrix.
- **DISA STIG Requirements**: Application Security and Development STIG (V-222XXX series) requirements are incorporated into development standards. STIG-compliant password policy is enforced (minimum 15 characters, complexity requirements, 60-day maximum lifetime, 24-generation history).
- **HIPAA Security Rule**: Technical safeguards for medical data handled by applicable microservices (PersonnelService, IncidentService) are defined and enforced.
- **NIST SP 800-53 Rev. 5**: Applicable controls (AC, AU, IA, SC, SI families) are implemented and documented in the System Security Plan.

Security requirements are maintained as enforceable policies in code through `Directory.Build.props` (centralized build configuration), Roslyn analyzer rule sets, and CI/CD pipeline gates.

### PO.2 -- Implement Roles and Responsibilities

**TheWatch Implementation:**

- Defined roles for security engineering, service ownership, DevOps, ISSM/ISSO, and Authorizing Official are documented and assigned.
- Security Engineering is responsible for maintaining security tooling, triaging vulnerabilities, and reviewing security-sensitive changes.
- Service owners are accountable for the security posture of their respective microservices.
- All personnel with commit access have completed role-appropriate security training.

### PO.3 -- Implement Supporting Toolchains

**TheWatch Implementation:**

TheWatch employs a comprehensive security toolchain integrated into the development lifecycle:

| Tool | Category | Purpose |
|---|---|---|
| **CodeQL** | SAST | Static analysis of C# source code for injection, authentication, cryptography, and data flow vulnerabilities |
| **Trivy** | Container Security | Scanning of Docker images for OS and library vulnerabilities, misconfigurations |
| **OWASP ZAP** | DAST | Dynamic analysis of running application endpoints in staging |
| **Gitleaks** | Secret Detection | Detection of secrets, API keys, and credentials in source code and git history |
| **Cosign (Sigstore)** | Image Signing | Cryptographic signing and verification of container images to ensure supply chain integrity |
| **CycloneDX** | SBOM Generation | Generation of Software Bill of Materials in CycloneDX format for every release |
| **NuGet Audit** | Dependency Security | Audit of NuGet package dependencies against known vulnerability advisories |
| **GitHub Dependency Review** | Supply Chain | Review of dependency changes in pull requests for known vulnerabilities |
| **Renovate** | Dependency Management | Automated dependency update proposals to keep libraries current |
| **Roslyn Analyzers** | Secure Coding | Compile-time enforcement of security coding standards |

### PO.4 -- Define and Use Criteria for Software Security Checks

**TheWatch Implementation:**

- Security gates are defined at each phase of the SDLC, as documented in the Secure Development Lifecycle (TW-SEC-342).
- CI/CD pipelines enforce pass/fail criteria: builds fail on CRITICAL/HIGH NuGet advisories, Trivy findings, Gitleaks detections, and CodeQL alerts above defined thresholds.
- Pull request merge requirements include passing security scans, code review with security checklist, and dependency review approval.

### PO.5 -- Implement and Maintain Secure Environments for Software Development

**TheWatch Implementation:**

- Development environments use isolated configurations that do not connect to production data or systems.
- Branch protection rules prevent direct pushes to main branches; all changes require pull request review.
- Secrets management uses environment-specific configuration; no secrets are stored in source code (enforced by Gitleaks).
- CI/CD runner environments are ephemeral and rebuilt for each pipeline execution.

### Threat Modeling (PO.1 supplementary)

**TheWatch Implementation:**

- **STRIDE Threat Modeling**: Comprehensive threat modeling using the STRIDE methodology is documented in `DOD_SECURITY_ANALYSIS.md`. Each microservice and inter-service communication channel has been analyzed for Spoofing, Tampering, Repudiation, Information Disclosure, Denial of Service, and Elevation of Privilege threats.
- **MITRE ATT&CK Mapping**: TheWatch security controls are mapped to MITRE ATT&CK techniques relevant to the DoD threat landscape, including Initial Access (T1190, T1078), Lateral Movement (T1021), and Exfiltration (T1041) techniques. This mapping ensures defensive measures address known adversary behaviors.
- **Attack Surface Analysis**: Each microservice's external and internal attack surface is documented, including exposed endpoints, authentication requirements, data classification of inputs/outputs, and trust boundaries.

---

## 5. SSDF Practice Group: PS -- Protect the Software

**Objective:** Protect all components of the software from tampering and unauthorized access.

### PS.1 -- Protect All Forms of Code from Unauthorized Access and Tampering

**TheWatch Implementation:**

- **Branch Protection Rules**: All repositories enforce branch protection on main/release branches. Direct pushes are prohibited. Pull requests require a minimum of one reviewer approval, passing CI checks, and no unresolved conversations.
- **Signed Commits**: Developers are required to sign commits using GPG or SSH keys. Unsigned commits are flagged during code review.
- **Access Control**: Repository access follows the principle of least privilege. Write access is restricted to authorized development team members. Administrative access requires additional approval.

### PS.2 -- Provide a Mechanism for Verifying Software Release Integrity

**TheWatch Implementation:**

- **Container Image Signing (Cosign)**: All production container images are cryptographically signed using Cosign (Sigstore) as part of the CI/CD release pipeline. Deployment configurations verify image signatures before allowing container instantiation, ensuring that only authenticated, untampered images are deployed.
- **SBOM Generation (CycloneDX)**: A Software Bill of Materials in CycloneDX format is generated for every release. The SBOM enumerates all direct and transitive dependencies, their versions, licenses, and known vulnerability status. SBOMs are archived with each release artifact and made available to consumers upon request per EO 14028 requirements.
- **NuGet Package Audit**: `dotnet restore --audit` is executed in every CI build to verify that no dependencies with known CRITICAL or HIGH advisories are included. This check acts as a supply chain integrity gate.

### PS.3 -- Archive and Protect Each Software Release

**TheWatch Implementation:**

- Release artifacts (signed container images, SBOMs, release notes) are stored in access-controlled registries with audit logging.
- Container registries enforce immutable tags for production releases, preventing tag mutation attacks.
- Release history is maintained in version control with signed tags.

### Supply Chain Integrity (PS supplementary)

**TheWatch Implementation:**

- **Directory.Build.props**: Centralized build configuration in `Directory.Build.props` enforces consistent dependency versions, security analyzer references, and build settings across all 13 microservices. This prevents individual services from diverging in their security posture or introducing unauthorized dependencies.
- **Renovate Automated Dependency Updates**: Renovate is configured to automatically propose dependency update pull requests. Each update triggers the full CI security scanning pipeline (NuGet audit, Trivy, CodeQL, Gitleaks), ensuring that dependency updates are vetted before merge.
- **Dependency Review Action**: GitHub's Dependency Review Action evaluates every pull request for newly introduced dependencies with known vulnerabilities, blocking merge when advisory-affected packages are detected.

---

## 6. SSDF Practice Group: PW -- Produce Well-Secured Software

**Objective:** Produce well-secured software with minimal vulnerabilities in its releases.

### PW.1 -- Design Software to Meet Security Requirements and Mitigate Security Risks

**TheWatch Implementation:**

- Security architecture is designed around zero-trust principles with defense-in-depth layering.
- Inter-service communication uses authenticated and encrypted channels.
- Data classification drives encryption and access control decisions at the service level.

### PW.2 -- Review the Software Design to Verify Compliance with Security Requirements

**TheWatch Implementation:**

- Security architecture reviews are conducted for all new services and significant feature additions.
- Threat model reviews (STRIDE) are updated when the attack surface changes.
- Design documents include a security considerations section reviewed by Security Engineering.

### PW.4 -- Reuse Existing, Well-Secured Software Where Possible

**TheWatch Implementation:**

- Centralized security infrastructure is shared across all microservices via common libraries:
  - **FluentValidation**: Input validation framework used consistently across all API endpoints, preventing injection attacks through a standardized validation pattern.
  - **ProblemDetails Error Handling**: RFC 7807-compliant error responses implemented globally, preventing information leakage through verbose error messages while maintaining useful diagnostic information for authorized consumers.

### PW.5 -- Create Source Code by Adhering to Secure Coding Practices

**TheWatch Implementation:**

- **Roslyn Security Analyzers**: Compile-time security analyzers enforce secure coding standards across all C# code. Analyzers detect and flag insecure patterns including SQL injection, path traversal, insecure cryptography, hardcoded credentials, and insecure deserialization. Violations are treated as build errors for CRITICAL rules and warnings for MEDIUM rules.
- **Security Headers**: All HTTP responses include security headers enforced at the API Gateway level: `Strict-Transport-Security`, `Content-Security-Policy`, `X-Content-Type-Options`, `X-Frame-Options`, `Referrer-Policy`, and `Permissions-Policy`.
- **Rate Limiting**: API rate limiting is implemented at the Gateway level and per-service to prevent abuse, brute-force attacks, and denial-of-service conditions. Rate limits are configured per-endpoint based on expected usage patterns and sensitivity.
- **RBAC (Role-Based Access Control)**: Fine-grained role-based access control is enforced across all microservices. Roles are defined per operational function (e.g., Dispatcher, Commander, Administrator, Auditor) with least-privilege assignments. Authorization is enforced at both the API Gateway and individual service levels.
- **Multi-Factor Authentication (MFA)**: TheWatch supports four MFA methods to accommodate diverse operational environments:
  1. TOTP (Time-based One-Time Password) -- RFC 6238 compliant
  2. FIDO2/WebAuthn hardware security keys
  3. Email-based one-time codes
  4. SMS-based one-time codes (available where hardware tokens are not feasible)

  MFA is mandatory for all administrative access and configurable by policy for operational roles.

- **JWT Asymmetric Signing**: JSON Web Tokens are signed using asymmetric cryptography (RSA-256 or ECDSA) rather than symmetric HMAC. This ensures that only the AuthService possesses the private signing key, while consuming services verify tokens using the public key. Key rotation is supported without service disruption.
- **Argon2id Password Hashing**: User passwords are hashed using Argon2id, the winner of the Password Hashing Competition, configured with memory-hard parameters that resist GPU-based and ASIC-based cracking attacks. Parameters are tuned to the deployment environment's computational capacity.
- **DISA STIG Password Policy**: Password policy enforcement complies with DISA Application Security and Development STIG requirements:
  - Minimum 15 characters
  - Complexity requirements (uppercase, lowercase, numeric, special characters)
  - Maximum password age: 60 days
  - Password history: 24 generations
  - Account lockout after failed attempts
  - Session timeout enforcement

### PW.6 -- Configure the Compilation, Interpreter, and Build Processes to Improve Executable Security

**TheWatch Implementation:**

- Build configurations enforce security-relevant compiler options and runtime settings.
- `Directory.Build.props` centralizes build security settings across all projects.
- Container images are built with minimal base images and non-root user execution.
- Unnecessary services and packages are excluded from production container images.

### PW.7 -- Review and/or Analyze Human-Readable Code to Identify Vulnerabilities

**TheWatch Implementation:**

- **Code Review**: All pull requests require at least one reviewer with documented security review responsibilities. A security checklist is integrated into the PR template covering input validation, authentication, authorization, cryptography, error handling, logging, and data protection.
- **CodeQL SAST**: Weekly scheduled CodeQL analysis scans all C# source code for vulnerabilities. Custom queries supplement the default query suite to address TheWatch-specific patterns.
- **Gitleaks**: Per-PR secret scanning ensures no credentials, API keys, tokens, or other secrets are committed to the repository.

### PW.8 -- Test Executable Code to Identify Vulnerabilities and Verify Compliance

**TheWatch Implementation:**

- **OWASP ZAP DAST**: Dynamic application security testing runs against the staging environment after every deployment, testing live endpoints for runtime vulnerabilities including injection, authentication flaws, and misconfiguration.
- **Penetration Testing**: Annual penetration testing (minimum) per the Penetration Testing Program (TW-SEC-339) covers all components with manual expert analysis.
- **Unit and Integration Tests**: Security-relevant test cases validate authentication flows, authorization enforcement, input validation, and cryptographic operations.

### PW.9 -- Configure Software to Have Secure Settings by Default

**TheWatch Implementation:**

- All services deploy with secure default configurations: TLS enforced, debug modes disabled, verbose error messages suppressed, administrative endpoints restricted.
- Default RBAC policies follow least-privilege principles; elevated access requires explicit grant.
- Container images run as non-root users by default.

---

## 7. SSDF Practice Group: RV -- Respond to Vulnerabilities

**Objective:** Identify residual vulnerabilities in software releases and respond appropriately to address those vulnerabilities and prevent similar ones from occurring in the future.

### RV.1 -- Identify and Confirm Vulnerabilities on an Ongoing Basis

**TheWatch Implementation:**

TheWatch employs six automated vulnerability detection tools operating at different stages of the development and deployment lifecycle:

| Tool | Stage | Frequency | Detection Focus |
|---|---|---|---|
| **NuGet Audit** | Build (CI) | Daily | Known vulnerabilities in .NET package dependencies |
| **Trivy** | Build (CI) | Per container build | OS packages, library vulnerabilities, and misconfigurations in container images |
| **CodeQL** | Source Code | Weekly + on-demand | Static analysis for injection, auth, crypto, and data flow vulnerabilities |
| **Gitleaks** | Pre-merge (PR) | Per pull request | Secrets, credentials, and API keys in source code |
| **OWASP ZAP** | Staging | Per deployment | Runtime vulnerabilities in HTTP endpoints (OWASP Top 10) |
| **Dependency Review** | Pre-merge (PR) | Per pull request | Newly introduced vulnerable dependencies |

Additionally:
- **VulnerabilityMonitorService** (Hangfire recurring job) continuously monitors NuGet vulnerability databases, GitHub Security Advisories, and the CISA KEV catalog for new advisories affecting TheWatch dependencies.
- **Annual penetration testing** provides expert manual analysis of the complete attack surface per TW-SEC-339.

### RV.2 -- Assess, Prioritize, and Remediate Vulnerabilities

**TheWatch Implementation:**

- **Remediation SLAs** are defined and enforced per the Vulnerability Management Policy (TW-SEC-340):
  - CRITICAL (DISA CAT I): 7 calendar days
  - HIGH (DISA CAT I): 30 calendar days
  - MEDIUM (DISA CAT II): 90 calendar days
  - LOW (DISA CAT III): 180 calendar days

- **Triage Workflow**: All vulnerabilities follow a standardized workflow: Discovery, Triage, Assignment, Remediation, Verification, and Closure. Triage includes severity validation, applicability assessment, and exploitability analysis within the TheWatch operational context.

- **POA&M Tracking**: Vulnerabilities that cannot be immediately remediated are tracked in the Plan of Action and Milestones (POA&M) with assigned owners, milestones, and target dates. The POA&M is reviewed monthly and overdue items are escalated.

### RV.3 -- Analyze Vulnerabilities to Identify Their Root Causes

**TheWatch Implementation:**

- **Root Cause Analysis**: CRITICAL and HIGH vulnerabilities undergo root cause analysis to determine whether the vulnerability represents a systemic issue (e.g., missing input validation pattern, insecure default configuration) or an isolated defect.
- **Pattern Detection**: When root cause analysis identifies a class of vulnerability, Roslyn analyzer rules or CodeQL queries are updated to detect similar patterns across the codebase.
- **Post-Incident Reviews**: Security incidents and significant vulnerability discoveries trigger post-incident reviews that document findings, root causes, and preventive measures.

### RV.4 -- Respond to Vulnerability Disclosures

**TheWatch Implementation:**

- **Incident Response Plan**: TheWatch maintains an Incident Response Plan (`docs/incident-response-plan.md`) that defines procedures for responding to security incidents, including vulnerability disclosures from external parties.
- **Coordinated Disclosure**: TheWatch supports coordinated vulnerability disclosure and provides a documented process for external researchers to report vulnerabilities.
- **Notification**: When a vulnerability in a TheWatch release is confirmed, affected consumers are notified through established channels with advisory information and remediation guidance.

---

## 8. Continuous Compliance

### 8.1 Attestation Review Cadence

This attestation shall be reviewed and updated:

- Annually, at minimum.
- Upon significant changes to the development process, toolchain, or security architecture.
- When new SSDF guidance or OMB memoranda are published.
- Following a security incident that reveals gaps in SSDF practice implementation.

### 8.2 Evidence Maintenance

Evidence supporting each practice area is maintained in the following locations:

| Practice Group | Evidence Location |
|---|---|
| PO (Prepare) | `DOD_SECURITY_ANALYSIS.md`, CI/CD pipeline configurations, training records |
| PS (Protect) | Repository settings, Cosign signatures, SBOM archives, `Directory.Build.props` |
| PW (Produce) | Roslyn analyzer configurations, code review records, scan results, test reports |
| RV (Respond) | Vulnerability tracking system, POA&M (`docs/POA&M.md`), incident response records, scan dashboards |

### 8.3 Regulatory Alignment

| Regulation / Directive | Alignment |
|---|---|
| **EO 14028** (Improving the Nation's Cybersecurity) | SSDF attestation directly satisfies Section 4 requirements for software supply chain security |
| **OMB M-22-18** | This attestation form conforms to the self-attestation requirement for critical software |
| **CMMC Level 2** | SSDF practices map to CMMC practices in the Security Assessment (CA), System and Information Integrity (SI), and Risk Assessment (RA) families |
| **DISA STIG** | Application Security and Development STIG requirements are incorporated into PW practices |
| **HIPAA Security Rule** | Technical safeguards for medical data are addressed within PW.1 and PW.5 secure development practices |

## 9. References

| Reference | Description |
|---|---|
| NIST SP 800-218 (SSDF v1.1) | Secure Software Development Framework |
| OMB M-22-18 | Enhancing the Security of the Software Supply Chain through Secure Software Development Practices |
| Executive Order 14028 | Improving the Nation's Cybersecurity |
| NIST SP 800-53 Rev. 5 | Security and Privacy Controls for Information Systems and Organizations |
| NIST SP 800-171 Rev. 2 | Protecting Controlled Unclassified Information in Nonfederal Systems |
| CMMC Level 2 Model | Cybersecurity Maturity Model Certification |
| DISA Application Security and Development STIG | Security Technical Implementation Guide for application development |
| OWASP ASVS v4.0 | Application Security Verification Standard |
| TW-SEC-339 | TheWatch Penetration Testing Program |
| TW-SEC-340 | TheWatch Vulnerability Management Policy |
| TW-SEC-342 | TheWatch Secure Development Lifecycle |

## 10. Document Control

| Version | Date | Author | Change Description |
|---|---|---|---|
| 1.0 | 2026-02-26 | TheWatch Security Engineering | Initial release |

---

*This attestation is submitted in accordance with OMB M-22-18 and constitutes the software producer's self-attestation of conformance with SSDF v1.1 practices. This document is part of TheWatch DoD compliance package and is subject to review and approval by the Authorizing Official. Distribution is limited to authorized personnel and Federal agency consumers as required.*
