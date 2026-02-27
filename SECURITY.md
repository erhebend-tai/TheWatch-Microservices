# Security Policy

> **Classification: UNCLASSIFIED**

## Reporting a Vulnerability

The TheWatch team takes security vulnerabilities seriously. We appreciate your
efforts to responsibly disclose any issues you find.

### How to Report

**Do NOT open a public GitHub issue for security vulnerabilities.**

Instead, please report vulnerabilities through one of the following channels:

1. **GitHub Security Advisories** (preferred): Use the
   [private vulnerability reporting](https://github.com/erhebend-tai/TheWatch-Microservices/security/advisories/new)
   feature on this repository.
2. **Email**: Contact the security team directly with details of the
   vulnerability.

### What to Include

When reporting a vulnerability, please include:

- **Description** of the vulnerability and its potential impact
- **Steps to reproduce** or a proof-of-concept
- **Affected component(s)** (service name, file path, endpoint)
- **Severity assessment** (Critical / High / Medium / Low)
- **Suggested remediation** (if any)

### Response Timeline

| Action | Target |
|---|---|
| Acknowledgment of report | 2 business days |
| Initial triage & severity assessment | 5 business days |
| Remediation plan communicated | 10 business days |
| Fix deployed (Critical/High) | 30 calendar days |
| Fix deployed (Medium/Low) | 90 calendar days |

### Scope

The following components are in scope for vulnerability reports:

- All microservices (P1–P11)
- Admin REST API and Admin Portal
- Mobile application (MAUI)
- Dashboard application
- Container images and Dockerfiles
- Infrastructure as Code (Terraform, Helm, Kubernetes manifests)
- CI/CD pipeline configurations
- Authentication and authorization mechanisms

### Out of Scope

- Third-party dependencies (report directly to the upstream maintainer)
- Denial of service attacks against development/staging environments
- Social engineering attacks

## Supported Versions

| Version | Supported |
|---|---|
| main branch (latest) | ✅ |
| develop branch | ✅ |
| Feature branches | ❌ |

## Security Standards

This project is developed in accordance with:

- CMMC 2.0 Level 2
- NIST SP 800-171 Rev 2
- NIST SP 800-218 (SSDF v1.1)
- DISA STIG (Application Security & Development)
- EO 14028 (Supply Chain Security)

For the full security posture and compliance status, see:

- [`DOD_SECURITY_ANALYSIS.md`](DOD_SECURITY_ANALYSIS.md) — Gap analysis and
  remediation plan
- [`docs/POA&M.md`](docs/POA%26M.md) — Plan of Action & Milestones
- [`docs/vulnerability-management-policy.md`](docs/vulnerability-management-policy.md) — Vulnerability management procedures
- [`docs/incident-response-plan.md`](docs/incident-response-plan.md) — Incident
  response procedures

## Automated Security Controls

| Control | Tool | Frequency |
|---|---|---|
| Static Analysis (SAST) | CodeQL | Every PR and push to main |
| Dependency Scanning | GitHub Dependency Review, Grype | Every PR |
| Secret Detection | Gitleaks | Every PR and push |
| Container Scanning | Trivy | Every container build |
| Image Signing | Cosign | Every container publish |
| SBOM Generation | CycloneDX, SPDX | Weekly and on release |
| SLSA Provenance | SLSA GitHub Generator | On release |
