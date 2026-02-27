# Continuous Monitoring Strategy — TheWatch Platform

> **Classification:** CUI // SP-BASIC  
> **Reference Standards:** NIST SP 800-137, NIST SP 800-53 Rev 5 (CA, RA, SI families),
> CISA KEV, CMMC Level 2  
> **Document ID:** CM-001  
> **Version:** 1.0

---

## 1. Continuous Monitoring Framework

TheWatch implements continuous monitoring (ConMon) aligned with NIST SP 800-137 to
maintain ongoing awareness of the security posture, vulnerabilities, and threats
across all 11 microservices and supporting infrastructure.

### 1.1 ConMon Objectives

| Objective | Approach | Standard |
|-----------|---------|----------|
| **Maintain authorization** | Continuous security assessment; POA&M tracking; SSP updates | NIST 800-137 §3.1 |
| **Detect vulnerabilities** | Automated scanning (SAST/SCA/DAST); advisory monitoring | NIST 800-53 RA-5 |
| **Assess threats** | STRIDE modeling; MITRE ATT&CK detection; CISA KEV monitoring | NIST 800-53 RA-3 |
| **Monitor controls** | Automated compliance workflows; periodic manual reviews | NIST 800-53 CA-7 |
| **Report status** | Dashboard metrics; periodic reports; POA&M updates | NIST 800-53 CA-7(4) |

---

## 2. Automated Security Monitoring

### 2.1 VulnerabilityMonitorService

TheWatch includes a dedicated `VulnerabilityMonitorService` implemented as a Hangfire
background job for continuous vulnerability discovery:

| Data Source | Monitoring Frequency | Scope | Action |
|-------------|---------------------|-------|--------|
| **CISA KEV Catalog** | Continuous (Hangfire scheduled) | Known exploited vulnerabilities | Immediate triage (6-hour SLA for Critical) |
| **NuGet Advisory Database** | Every build + continuous monitoring | .NET dependency vulnerabilities | Auto-fail builds; POA&M if existing |
| **GitHub Advisory Database** | Continuous + PR dependency review | All dependency advisories | PR annotations; vulnerability alerts |
| **CodeQL Analysis** | Every push and pull request | Source code vulnerabilities | Block merge on HIGH/CRITICAL |
| **Trivy Container Scanning** | Every container build | OS + application layer vulnerabilities | Build failure on HIGH/CRITICAL |
| **Gitleaks Secret Scanning** | Every push | Hardcoded secrets and credentials | Immediate alert; commit blocked |

**Evidence:** `docs/vulnerability-management-policy.md`

### 2.2 Threat Detection

| Detector | Implementation | Interval | Threats Detected | Standard |
|----------|---------------|----------|-----------------|----------|
| **STRIDE Threat Scanner** | Hangfire background job | Every 15 minutes | Spoofing, Tampering, Repudiation, Information Disclosure, Denial of Service, Elevation of Privilege | NIST 800-53 RA-3 |
| **MITRE ATT&CK Rules** | Seeded detection rules | Real-time event matching | T1078 (Valid Accounts), T1110 (Brute Force), T1528 (Steal Application Access Token), T1621 (MFA Request Generation), T1556 (Modify Authentication Process) | NIST 800-53 SI-4 |
| **Brute Force Detection** | Auth service built-in | Real-time | Credential stuffing, password spraying | NIST 800-53 AC-7 |
| **Rate Limit Monitoring** | Middleware logging | Real-time | DDoS, API abuse | NIST 800-53 SC-5 |
| **Device Trust Scoring** | Auth service | Per-authentication | Compromised devices, unusual login locations | NIST 800-53 IA-3 |
| **CORS Violation Logging** | Middleware | Real-time | Cross-origin attacks | NIST 800-53 SC-7 |

### 2.3 Application Health Monitoring

| Component | Monitor | Interval | Alert Threshold | Standard |
|-----------|---------|----------|----------------|----------|
| **Service Health** | `/healthz` endpoint | 30 seconds | 3 consecutive failures | NIST 800-53 SI-4 |
| **Service Readiness** | `/ready` endpoint | 30 seconds | Not ready > 60 seconds | NIST 800-53 SI-4 |
| **Database Connectivity** | EF Core health check | 60 seconds | Connection failure | NIST 800-53 SI-4 |
| **Kafka Connectivity** | Producer/consumer health | 60 seconds | Connection failure | NIST 800-53 SI-4 |
| **Redis Connectivity** | Cache health check | 60 seconds | Connection failure | NIST 800-53 SI-4 |
| **Certificate Expiry** | TLS certificate monitoring | Daily | < 30 days to expiry | NIST 800-53 SC-17 |

---

## 3. Monitoring Infrastructure

### 3.1 Monitoring Stack

| Component | Tool | Purpose | Configuration |
|-----------|------|---------|--------------|
| **Metrics Collection** | Prometheus | Service metrics, custom counters, histograms | `infra/monitoring/prometheus/` |
| **Visualization** | Grafana | Dashboards, trend analysis, alerting | `infra/monitoring/grafana/` |
| **Alert Management** | AlertManager | Alert routing, deduplication, notification | `infra/monitoring/alertmanager/` |
| **Alert Rules** | Prometheus Rules | Custom alert definitions per service | `infra/monitoring/prometheus/alert-rules/` |
| **Logging** | Serilog → Structured JSON | Application logs with correlation IDs | All services |
| **Log Aggregation** | Planned (ELK/Loki) | Centralized log search and analysis | POA&M item |

### 3.2 Monitoring Agents

TheWatch deploys 10 specialized monitoring agents for continuous assessment:

| Agent | Purpose | Monitoring Scope | Standard |
|-------|---------|-----------------|----------|
| **Schema Monitor** | Detect database schema drift from expected configuration | All 11 SQL Server databases + PostgreSQL | NIST 800-53 CM-3 |
| **Security Monitor** | Validate security configuration compliance | TLS, auth, headers, rate limiting | NIST 800-53 CA-7 |
| **API Quality Monitor** | Track API response times, error rates, SLA compliance | All REST endpoints | NIST 800-53 SI-4 |
| **Compliance Monitor** | Verify regulatory control implementation | CMMC, NIST, DISA STIG checks | CMMC Level 2 |
| **Performance Monitor** | Track latency, throughput, resource utilization | All services and databases | NIST 800-53 SI-4 |
| **Dependency Monitor** | Track dependency health and version currency | NuGet packages, base images | NIST 800-53 RA-5 |
| **Certificate Monitor** | Track TLS certificate validity and rotation | All TLS endpoints | NIST 800-53 SC-17 |
| **Backup Monitor** | Verify backup completion and integrity | All databases and storage | NIST 800-53 CP-9 |
| **Access Monitor** | Track access patterns and anomalies | Auth service, admin endpoints | NIST 800-53 AC-2(4) |
| **Event Monitor** | Track event bus health and message delivery | Kafka topics and consumers | NIST 800-53 SI-4 |

---

## 4. Audit and Accountability Monitoring

### 4.1 Audit Log Management

| Aspect | Configuration | Standard |
|--------|--------------|----------|
| **Format** | Structured JSON (Serilog) | NIST 800-53 AU-3 |
| **Content** | Timestamp, user ID, action, resource, result, correlation ID, IP address | NIST 800-53 AU-3(1) |
| **Integrity** | HMAC-SHA256 signed records with hash chaining | NIST 800-53 AU-9 |
| **Storage** | Service database (current); centralized SIEM (planned) | NIST 800-53 AU-4 |
| **Retention — Security** | 1 year | NIST 800-53 AU-11 |
| **Retention — General** | 90 days | NIST 800-53 AU-11 |
| **Access Control** | AdminOnly policy on audit endpoints | NIST 800-53 AU-9 |
| **PII Redaction** | SSN, phone, email, credit card automatically masked | NIST 800-53 AU-3 |

### 4.2 Auditable Events

| Event Category | Examples | Log Level | Standard |
|----------------|---------|-----------|----------|
| **Authentication** | Login success/failure, MFA challenge, token refresh, session termination | Information/Warning | NIST 800-53 AU-2(3) |
| **Authorization** | Role assignment, permission check, IDOR attempt, policy evaluation | Information/Warning | NIST 800-53 AU-2(3) |
| **Account Management** | User creation, role change, account lockout, account deactivation | Information | NIST 800-53 AU-2(3) |
| **CUI Access** | PHI read/write, law enforcement data access, geolocation queries | Information | NIST 800-53 AU-2(3) |
| **Admin Actions** | Configuration change, system setting update, service restart | Warning | NIST 800-53 AU-2(3) |
| **Security Events** | Rate limit exceeded, CORS violation, brute force detected, threat scan result | Warning/Error | NIST 800-53 AU-2(3) |
| **Data Modification** | CUI record create/update/delete; evidence submission | Information | NIST 800-53 AU-2(3) |

### 4.3 Log Review Schedule

| Review Type | Frequency | Reviewer | Scope | Standard |
|-------------|-----------|---------|-------|----------|
| **Automated Alerts** | Real-time | Security Team (on-call) | Critical security events | NIST 800-53 AU-6 |
| **Security Log Review** | Weekly | Security Lead | Auth failures, admin actions, anomalies | NIST 800-53 AU-6(1) |
| **Compliance Review** | Monthly | Compliance Officer | CUI access, policy compliance, POA&M progress | NIST 800-53 AU-6(3) |
| **Executive Review** | Quarterly | CISO / Management | Trend analysis, risk posture, remediation status | NIST 800-53 AU-6(5) |

---

## 5. Vulnerability Management Lifecycle

### 5.1 Discovery → Remediation Flow

```
┌──────────────────────────────────────────────────────────────────┐
│                    DISCOVERY SOURCES                              │
│  ┌─────────┐ ┌─────────┐ ┌──────────┐ ┌─────────┐ ┌─────────┐ │
│  │ CodeQL  │ │ Trivy   │ │ Gitleaks │ │ NuGet   │ │ CISA    │ │
│  │ (SAST)  │ │ (SCA)   │ │ (Secrets)│ │ Audit   │ │ KEV     │ │
│  └────┬────┘ └────┬────┘ └────┬─────┘ └────┬────┘ └────┬────┘ │
│       └──────┬─────┴──────┬────┴──────┬─────┘           │      │
│              ▼            ▼           ▼                  ▼      │
│         ┌─────────────────────────────────────────────────┐     │
│         │          TRIAGE (SLA-Based)                      │     │
│         │  Critical: 6 hours  │  High: 24 hours           │     │
│         │  Medium: 3 days     │  Low: 5 days              │     │
│         └───────────┬─────────────────────────────────────┘     │
│                     ▼                                            │
│         ┌─────────────────────────────────────────────────┐     │
│         │          ASSIGNMENT                               │     │
│         │  Owner assigned │ Sprint planned │ POA&M updated  │     │
│         └───────────┬─────────────────────────────────────┘     │
│                     ▼                                            │
│         ┌─────────────────────────────────────────────────┐     │
│         │          REMEDIATION (SLA-Based)                  │     │
│         │  Critical: 7 days   │  High: 30 days             │     │
│         │  Medium: 90 days    │  Low: 180 days             │     │
│         └───────────┬─────────────────────────────────────┘     │
│                     ▼                                            │
│         ┌─────────────────────────────────────────────────┐     │
│         │          VERIFICATION                             │     │
│         │  Fix validated │ Regression tested │ Deployed     │     │
│         └───────────┬─────────────────────────────────────┘     │
│                     ▼                                            │
│         ┌─────────────────────────────────────────────────┐     │
│         │          CLOSURE                                  │     │
│         │  POA&M updated │ Metrics recorded │ Case closed   │     │
│         └─────────────────────────────────────────────────┘     │
└──────────────────────────────────────────────────────────────────┘
```

### 5.2 False Positive Handling

| Step | Action | Documentation |
|------|--------|--------------|
| 1 | Analyst investigates finding | Initial analysis documented |
| 2 | Determine not exploitable / not applicable | Technical justification |
| 3 | Peer review of determination | Second analyst confirms |
| 4 | Document risk acceptance | POA&M entry with rationale |
| 5 | Periodic re-evaluation | Quarterly review of false positives |

### 5.3 Risk Acceptance Process

| Criterion | Requirement | Approver |
|-----------|-------------|---------|
| **Compensating Control** | Must exist and be documented | Security Lead |
| **Business Justification** | Clear rationale for acceptance | Project Manager |
| **Time-Bounded** | Maximum 1-year acceptance period | CISO |
| **Re-Evaluation** | Quarterly review required | Security Team |

**Current Risk Acceptances:**

| Risk | Compensating Control | Expiry |
|------|---------------------|--------|
| Argon2id not FIPS-validated | PBKDF2-SHA512 (FIPS 140-2) fallback available; Argon2id cryptographically superior | Pending FIPS validation |

---

## 6. Compliance Monitoring

### 6.1 Automated Compliance Checks

| Workflow | Checks Performed | Frequency | Standard |
|----------|-----------------|-----------|----------|
| **dod-compliance.yml** | Container non-root, TLS config, auth enforcement, security headers | Every push to main | CMMC Level 2 |
| **dod-readiness.yml** | CMMC practice coverage, NIST alignment, policy completeness, POA&M currency | Every push to main | CMMC Level 2 |
| **security.yml** | CodeQL SAST, dependency review, secret scanning | Every push and PR | NIST 800-53 SA-11 |
| **sbom-aggregate.yml** | SBOM generation, vulnerability cross-reference | Release builds | EO 14028 |
| **slsa-provenance.yml** | Build provenance attestation | Release builds | SLSA v1.0 |

### 6.2 POA&M Monitoring

| Metric | Current Value | Target | Review Frequency |
|--------|--------------|--------|-----------------|
| **Total Open Findings** | 50 | 0 | Weekly |
| **Critical Findings** | 9 | 0 | Daily |
| **High Findings** | 22 | 0 | Weekly |
| **Overdue Findings** | TBD | 0 | Weekly |
| **Average Time to Remediate** | TBD | < SLA | Monthly |
| **False Positive Rate** | TBD | < 10% | Quarterly |

**Evidence:** `docs/POA&M.md`

---

## 7. Reporting

### 7.1 Report Types

| Report | Audience | Frequency | Content | Standard |
|--------|---------|-----------|---------|----------|
| **Security Dashboard** | Security Team | Real-time | Active alerts, scan results, threat indicators | NIST 800-137 |
| **Vulnerability Report** | Management | Monthly | New/closed findings, SLA compliance, trend analysis | NIST 800-53 RA-5(5) |
| **POA&M Status** | CISO / AO | Monthly | Finding status, remediation progress, risk acceptances | NIST 800-53 CA-5 |
| **Compliance Status** | Management | Quarterly | CMMC/NIST control implementation status, gap analysis | CMMC Level 2 |
| **Executive Summary** | Senior Leadership | Quarterly | Risk posture, key metrics, strategic recommendations | NIST 800-137 |
| **Annual Security Review** | All Stakeholders | Annually | Year-in-review, penetration test results, roadmap | NIST 800-53 CA-2 |

### 7.2 Key Performance Indicators

| KPI | Metric | Target | Standard |
|-----|--------|--------|----------|
| **Mean Time to Detect** | Discovery → triage | < 24 hours | NIST 800-53 SI-4 |
| **Mean Time to Remediate** | Discovery → closure | < SLA per severity | NIST 800-53 SI-2 |
| **Scan Coverage** | % of codebase scanned | 100% | NIST 800-53 RA-5 |
| **Control Compliance** | % of NIST 800-171 controls met | > 90% | CMMC Level 2 |
| **Patch Currency** | % of dependencies at latest secure version | > 95% | NIST 800-53 SI-2 |
| **False Positive Rate** | % of findings determined false positive | < 10% | Quality metric |

---

*This Continuous Monitoring Strategy ensures TheWatch maintains ongoing security awareness,
timely vulnerability remediation, and continuous compliance with DoD standards throughout
the system lifecycle.*
