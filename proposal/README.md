# TheWatch — DoD Contract Proposal Package

> **Classification:** CUI // SP-PRIV // SP-HLTH // SP-LEI  
> **Program:** TheWatch Emergency Response & Public Safety Platform  
> **Solicitation Reference:** *(Insert solicitation number)*  
> **Prepared by:** *(Insert contractor name)*  
> **Date:** February 2026  
> **Version:** 1.0

---

## Executive Summary

TheWatch is a microservices-based emergency response and public safety platform designed
to deliver **sub-2-second citizen emergency response** capability across voice, wearable,
mobile, and mesh-network channels. The system encompasses 11 purpose-built microservices,
a MAUI hybrid mobile application, administrative interfaces, and multi-cloud deployment
infrastructure — all engineered to meet Department of Defense cybersecurity requirements.

### Mission Capability

| Capability | Description | Services |
|------------|-------------|----------|
| **Emergency Voice Response** | SOS ingestion, incident creation, automated dispatch | P2 VoiceEmergency, P6 FirstResponder |
| **Wearable Integration** | Device provisioning, vitals telemetry, fall detection | P4 Wearable, P7 FamilyHealth |
| **Mesh Network Communications** | Offline/degraded-network Bluetooth relay mesh | P3 MeshNetwork |
| **First Responder Dispatch** | Real-time location tracking, automated assignment | P6 FirstResponder, Geospatial |
| **Family Health Monitoring** | Vital signs tracking, geofencing, health alerts | P7 FamilyHealth, P9 DoctorServices |
| **Disaster Coordination** | Shelter management, evacuation routing, resource allocation | P8 DisasterRelief, Geospatial |
| **Telehealth Services** | Remote medical consultations, appointment scheduling | P9 DoctorServices |
| **Community Surveillance** | Camera registration, footage analysis, crime reporting | P11 Surveillance |
| **Identity & Access Management** | MFA, RBAC, zero-trust enforcement, CAC/PIV support | P5 AuthSecurity |
| **Community Engagement** | Gamification, safety challenges, leaderboards | P10 Gamification |

### Technology Stack

- **Runtime:** .NET 10 (ASP.NET Core, Entity Framework Core, SignalR, MAUI)
- **Databases:** SQL Server 2022 (11 service databases), PostgreSQL 16 + PostGIS, MongoDB Atlas
- **Messaging:** Apache Kafka (event streaming), Azure Service Bus (cloud)
- **Infrastructure:** Docker, Kubernetes (Helm), Terraform (Azure/AWS/GCP)
- **Orchestration:** .NET Aspire (local development), Azure Container Apps (production)
- **Security:** JWT (RSA-2048), Argon2id/PBKDF2-SHA512, AES-256-GCM, MFA (TOTP/FIDO2/SMS/Magic Link)
- **CI/CD:** GitHub Actions (11 workflows), CodeQL, Trivy, Gitleaks, CycloneDX SBOM
- **ML/AI:** ML.NET with ONNX models (object detection in surveillance)

### Compliance Posture

TheWatch is designed to meet the following DoD and federal standards:

| Standard | Status | Evidence |
|----------|--------|----------|
| **CMMC Level 2** | 74% controls met (full + partial) | [CMMC Compliance Matrix](CMMC_LEVEL2_COMPLIANCE.md) |
| **NIST SP 800-171 Rev 2** | 47/110 fully met, 34 partial | [System Security Plan](SYSTEM_SECURITY_PLAN.md) |
| **NIST SP 800-53 Rev 5** | Mapped across 14 families | [System Security Plan](SYSTEM_SECURITY_PLAN.md) |
| **NIST SP 800-218 SSDF v1.1** | All 4 practice groups addressed | [Supply Chain Risk Management](SUPPLY_CHAIN_RISK_MANAGEMENT.md) |
| **DISA STIG** | 286 findings mapped | [CMMC Compliance Matrix](CMMC_LEVEL2_COMPLIANCE.md) |
| **DFARS 252.204-7012** | CUI safeguarding implemented | [Data Protection Plan](DATA_PROTECTION_PLAN.md) |
| **HIPAA Security Rule** | PHI controls for health services | [Data Protection Plan](DATA_PROTECTION_PLAN.md) |
| **Executive Order 14028** | Supply chain security measures | [Supply Chain Risk Management](SUPPLY_CHAIN_RISK_MANAGEMENT.md) |
| **OMB M-22-18** | SSDF self-attestation | [Supply Chain Risk Management](SUPPLY_CHAIN_RISK_MANAGEMENT.md) |
| **FIPS 140-2/3** | PBKDF2-SHA512 fallback available | [System Security Plan](SYSTEM_SECURITY_PLAN.md) |
| **OWASP Top 10:2021** | Application security coverage | [Testing & QA](TESTING_QUALITY_ASSURANCE.md) |
| **OWASP ASVS v4.0** | Verification standard alignment | [Testing & QA](TESTING_QUALITY_ASSURANCE.md) |
| **SLSA v1.0** | Supply chain integrity levels | [Supply Chain Risk Management](SUPPLY_CHAIN_RISK_MANAGEMENT.md) |

---

## Proposal Document Index

| # | Document | Description | Primary Standards |
|---|----------|-------------|-------------------|
| 1 | **[Technical Volume](TECHNICAL_VOLUME.md)** | System architecture, microservices design, capabilities, and integration approach | NIST 800-53 SA, CMMC Level 2 |
| 2 | **[System Security Plan](SYSTEM_SECURITY_PLAN.md)** | Complete SSP covering all 14 NIST 800-171 control families with implementation details | NIST 800-171, NIST 800-53, CMMC Level 2 |
| 3 | **[CMMC Level 2 Compliance Matrix](CMMC_LEVEL2_COMPLIANCE.md)** | Practice-by-practice compliance status mapped to CMMC 2.0 Level 2 | CMMC 2.0, NIST 800-171, DISA STIG |
| 4 | **[Supply Chain Risk Management](SUPPLY_CHAIN_RISK_MANAGEMENT.md)** | Software supply chain security, SBOM, provenance, SSDF compliance | SSDF v1.1, EO 14028, OMB M-22-18, SLSA, NIST 800-161 |
| 5 | **[Data Protection Plan](DATA_PROTECTION_PLAN.md)** | CUI/PHI/PII data classification, encryption, access controls, handling procedures | NIST 800-171 MP/SC, DFARS 252.204-7012, HIPAA |
| 6 | **[Infrastructure & Deployment](INFRASTRUCTURE_DEPLOYMENT.md)** | Multi-cloud architecture, container security, Kubernetes, IaC | NIST 800-53 CM/SC, DISA STIG, FedRAMP |
| 7 | **[Testing & Quality Assurance](TESTING_QUALITY_ASSURANCE.md)** | Testing strategy, CI/CD pipeline, code quality, security scanning | NIST 800-53 SA/SI, OWASP Top 10, OWASP ASVS |
| 8 | **[Incident Response & Continuity](INCIDENT_RESPONSE_CONTINUITY.md)** | Incident response plan, disaster recovery, business continuity | NIST 800-171 IR, DFARS 252.204-7012, NIST 800-61 |
| 9 | **[Continuous Monitoring Strategy](CONTINUOUS_MONITORING_STRATEGY.md)** | Ongoing authorization, vulnerability management, threat detection | NIST 800-137, NIST 800-53 CA/RA/SI, CISA KEV |
| 10 | **[Standards Traceability Matrix](STANDARDS_TRACEABILITY_MATRIX.md)** | Complete mapping of ALL standards, controls, and requirements to system evidence | All applicable standards |

---

## Supporting Documentation (Existing)

The following documents are maintained within the repository and provide additional
evidence of compliance:

| Document | Location | Purpose |
|----------|----------|---------|
| SSDF Self-Attestation | `docs/ssdf-attestation.md` | OMB M-22-18 compliance attestation |
| Vulnerability Management Policy | `docs/vulnerability-management-policy.md` | NIST 800-53 SI-2/RA-5 |
| Incident Response Plan | `docs/incident-response-plan.md` | NIST 800-171 IR-1/IR-8 |
| Data Classification Matrix | `docs/data-classification-matrix.md` | CUI category mapping per entity |
| Penetration Testing Program | `docs/pentest-program.md` | NIST 800-53 CA-8 |
| Plan of Action & Milestones | `docs/POA&M.md` | NIST 800-171 CA-5 |
| Access Control Policy | `docs/policies/access-control-policy.md` | NIST 800-171 AC-1 |
| Audit & Accountability Policy | `docs/policies/audit-accountability-policy.md` | NIST 800-171 AU-1 |
| Identification & Auth Policy | `docs/policies/identification-authentication-policy.md` | NIST 800-171 IA-1 |
| Government Data Sources | `docs/government-data-sources.md` | 25 cited U.S. government datasets: crimes, injuries, disasters |
| Developer Setup Guide | `docs/developer-setup.md` | SSDF PS.1 signed commits |
| DoD Security Analysis | `DOD_SECURITY_ANALYSIS.md` | Gap analysis & remediation roadmap |
| System Roadmap | `ROADMAP.md` | 265-item delivery tracking |
| Microservices Map | `documentation/microservices.md` | Service architecture reference |

---

## Applicable Regulations & Standards

### Primary (Contractual Requirements)

| Regulation/Standard | Full Title | Applicability |
|---------------------|-----------|---------------|
| **DFARS 252.204-7012** | Safeguarding Covered Defense Information and Cyber Incident Reporting | CUI protection in DoD contracts |
| **CMMC 2.0 Level 2** | Cybersecurity Maturity Model Certification | 110 practices from NIST 800-171 |
| **NIST SP 800-171 Rev 2** | Protecting CUI in Nonfederal Systems | 110 security requirements across 14 families |
| **NIST SP 800-53 Rev 5** | Security and Privacy Controls for Information Systems | 1,189 controls across 20 families |
| **Executive Order 14028** | Improving the Nation's Cybersecurity | Software supply chain and zero trust |
| **OMB M-22-18** | Enhancing the Security of the Software Supply Chain | SSDF attestation requirement |

### Security & Development Standards

| Standard | Full Title | Applicability |
|----------|-----------|---------------|
| **NIST SP 800-218 (SSDF v1.1)** | Secure Software Development Framework | Secure development lifecycle |
| **NIST SP 800-161 Rev 1** | Cybersecurity Supply Chain Risk Management | C-SCRM practices |
| **NIST SP 800-137** | Information Security Continuous Monitoring | ConMon strategy |
| **NIST SP 800-61 Rev 2** | Computer Security Incident Handling Guide | IR procedures |
| **NIST SP 800-115** | Technical Guide to Information Security Testing | Penetration testing |
| **NIST SP 800-37 Rev 2** | Risk Management Framework | System authorization |
| **FIPS 140-2/3** | Security Requirements for Cryptographic Modules | Cryptographic validation |
| **DISA STIG** | Security Technical Implementation Guides | Technical configuration |

### Application & Data Standards

| Standard | Full Title | Applicability |
|----------|-----------|---------------|
| **OWASP Top 10:2021** | Open Worldwide Application Security Project | Web application security |
| **OWASP ASVS v4.0** | Application Security Verification Standard | Security verification |
| **OWASP MSTG** | Mobile Security Testing Guide | Mobile application security |
| **HIPAA Security Rule** | Health Insurance Portability and Accountability Act | Protected Health Information |
| **SLSA v1.0** | Supply-chain Levels for Software Artifacts | Build integrity |
| **CycloneDX / SPDX** | Software Bill of Materials Formats | Supply chain transparency |
| **CISA KEV** | Known Exploited Vulnerabilities Catalog | Active threat mitigation |

---

## Document Conventions

Throughout this proposal package:

- **[IMPLEMENTED]** — Control is fully implemented with evidence in the codebase
- **[PARTIAL]** — Control is partially implemented; remediation plan exists in POA&M
- **[PLANNED]** — Control is planned for implementation per the roadmap
- **Evidence references** point to specific files, configurations, or workflows in the repository
- **NIST control identifiers** use the format `XX-N` (family abbreviation-number)
- **CMMC practice identifiers** use the format `XX.L2-N.N.N`
- **DISA STIG identifiers** use the format `V-NNNNNN`

---

*This proposal package demonstrates TheWatch's commitment to meeting and exceeding
DoD cybersecurity requirements. All claims are substantiated by evidence within the
source repository, CI/CD pipeline artifacts, and policy documentation.*
