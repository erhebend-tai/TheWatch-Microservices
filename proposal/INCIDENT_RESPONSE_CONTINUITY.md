# Incident Response & Business Continuity — TheWatch Platform

> **Classification:** CUI // SP-PRIV // SP-HLTH // SP-LEI  
> **Reference Standards:** NIST SP 800-171 Rev 2 (IR family), NIST SP 800-61 Rev 2,
> DFARS 252.204-7012, NIST SP 800-53 Rev 5 (CP, IR families)  
> **Document ID:** IRBC-001  
> **Version:** 1.0

---

## 1. Incident Response Plan

### 1.1 Incident Categories

TheWatch classifies security incidents according to DoD/US-CERT categories:

| Category | Description | Examples | Severity | Response Time |
|----------|-------------|---------|----------|---------------|
| **CAT-1** | Unauthorized Access | Successful system compromise, privilege escalation, unauthorized CUI access | P1 — Critical | 15 minutes |
| **CAT-2** | Denial of Service | DDoS attacks, resource exhaustion, service degradation | P2 — High | 30 minutes |
| **CAT-3** | Malicious Code | Malware detection, ransomware, supply chain compromise | P1 — Critical | 15 minutes |
| **CAT-4** | Improper Usage | Policy violations, unauthorized software, data handling violations | P3 — Medium | 4 hours |
| **CAT-5** | Scans/Probes/Reconnaissance | Port scanning, vulnerability scanning, social engineering attempts | P4 — Low | 24 hours |

### 1.2 Severity Levels

| Level | Definition | Notification Chain | Response Time |
|-------|-----------|-------------------|---------------|
| **P1 — Critical** | Active compromise of CUI; system integrity loss; data breach | Security Lead → CISO → DoD (72h) → DC3 | 15 minutes initial; 72h DoD report |
| **P2 — High** | Service disruption affecting emergency operations; auth system compromise | Security Lead → CISO → Operations | 30 minutes |
| **P3 — Medium** | Policy violation; minor vulnerability exploitation; non-CUI data exposure | Security Lead → Team Lead | 4 hours |
| **P4 — Low** | Reconnaissance activity; failed attack attempts; informational findings | Security Team | 24 hours |

### 1.3 Detection Mechanisms

| Mechanism | Implementation | Detection Capability | Standard |
|-----------|---------------|---------------------|----------|
| **STRIDE Threat Scanning** | Hangfire job every 15 minutes | Systematic threat analysis across all services | NIST 800-53 SI-4 |
| **MITRE ATT&CK Rules** | 5 seeded detection rules | T1078 (Valid Accounts), T1110 (Brute Force), T1528 (Steal App Token), T1621 (MFA Request Gen), T1556 (Modify Auth) | NIST 800-53 SI-4 |
| **Rate Limiting Alerts** | Global: 100/min, Auth: 10/min | DDoS and brute force detection | NIST 800-53 SC-5 |
| **Brute Force Detection** | 3-attempt lockout with escalation | Credential stuffing, password spraying | NIST 800-53 AC-7 |
| **SIEM Correlation** | Planned (centralized log aggregation) | Cross-service attack pattern detection | NIST 800-53 SI-4(4) |
| **Container Scanning** | Trivy in CI/CD | Supply chain and runtime vulnerability detection | NIST 800-53 RA-5 |
| **Secret Scanning** | Gitleaks on every push | Hardcoded credential detection | NIST 800-53 SC-28 |
| **Dependency Monitoring** | VulnerabilityMonitorService (continuous) | Known vulnerability alerting (CISA KEV, NuGet, GitHub) | NIST 800-53 SI-5 |

### 1.4 Incident Response Procedures

#### Phase 1: Detection & Analysis

| Step | Action | Responsible | Tools |
|------|--------|-------------|-------|
| 1.1 | Receive alert from monitoring system | Security Team (on-call) | Prometheus, AlertManager |
| 1.2 | Classify incident (CAT-1 through CAT-5) | Incident Commander | IR category matrix |
| 1.3 | Assign severity level (P1-P4) | Incident Commander | Severity criteria |
| 1.4 | Activate IR team per severity | Incident Commander | Notification chain |
| 1.5 | Document initial findings | IR Team | Incident log |
| 1.6 | Preserve forensic evidence (90-day minimum) | IR Team | Log archives, database snapshots |

#### Phase 2: Containment

| Step | Action | Responsible | Approach |
|------|--------|-------------|----------|
| 2.1 | Short-term containment | IR Team | Isolate affected service/container; block source IP |
| 2.2 | Evidence preservation | IR Team | Snapshot containers; export audit logs; preserve chain-of-custody |
| 2.3 | Long-term containment | IR Team | Deploy patched containers; rotate compromised credentials |
| 2.4 | Communication | Incident Commander | Notify stakeholders per severity |

#### Phase 3: Eradication

| Step | Action | Responsible |
|------|--------|-------------|
| 3.1 | Identify root cause | IR Team |
| 3.2 | Remove malicious artifacts | IR Team |
| 3.3 | Patch vulnerabilities | Development Team |
| 3.4 | Update detection rules | Security Team |

#### Phase 4: Recovery

| Step | Action | Responsible | Verification |
|------|--------|-------------|-------------|
| 4.1 | Rebuild affected containers from clean images | DevOps Team | Image hash verification |
| 4.2 | Restore from verified backups (if needed) | DBA Team | Backup integrity check |
| 4.3 | Re-deploy affected services | DevOps Team | Health check verification |
| 4.4 | Monitor for recurrence (72-hour watch) | Security Team | Enhanced monitoring |
| 4.5 | Validate security controls | Security Team | Control verification checklist |

#### Phase 5: Post-Incident Activity

| Step | Action | Responsible | Timeline |
|------|--------|-------------|----------|
| 5.1 | Conduct post-incident review | IR Team + Management | Within 5 business days |
| 5.2 | Document lessons learned | IR Team | Within 10 business days |
| 5.3 | Update IR plan with improvements | Security Team | Within 15 business days |
| 5.4 | Update threat model (STRIDE/ATT&CK) | Security Team | Within 15 business days |
| 5.5 | Submit DoD report (if applicable) | CISO | Within 72 hours of discovery |

**Evidence:** `docs/incident-response-plan.md`

---

## 2. DoD Incident Reporting

### 2.1 DFARS 252.204-7012 Requirements

| Requirement | Implementation | Timeline |
|-------------|---------------|----------|
| **Report cyber incidents** | IR plan includes DoD reporting procedures | Within 72 hours |
| **Report to DC3** | DoD Cyber Crime Center (DC3) notification procedure documented | Within 72 hours |
| **Preserve images** | Container snapshots and log archives preserved | 90 days minimum |
| **Provide malware samples** | Isolated container images with malicious artifacts | As requested |
| **Submit to DIBNet** | Defense Industrial Base Cybersecurity reporting | Within 72 hours |

### 2.2 Incident Data Preservation

| Data Type | Preservation Method | Retention | Standard |
|-----------|-------------------|-----------|----------|
| **Container Images** | Snapshot of running containers | 90 days | DFARS 7012(c) |
| **Audit Logs** | Exported and archived with integrity hashes | 1 year | NIST 800-171 AU-11 |
| **Network Logs** | Packet captures if available | 90 days | DFARS 7012(c) |
| **Database Snapshots** | Point-in-time backup of affected databases | 90 days | DFARS 7012(c) |
| **Correlation IDs** | Cross-service request traces | 90 days | NIST 800-53 AU-12 |

---

## 3. Business Continuity Plan

### 3.1 Critical Services Priority

| Priority | Service | Impact if Unavailable | RTO | RPO |
|----------|---------|----------------------|-----|-----|
| **P1** | P2 VoiceEmergency | Emergency calls cannot be processed | 5 min | 0 |
| **P1** | P5 AuthSecurity | No authentication; all services inaccessible | 5 min | 0 |
| **P1** | P1 CoreGateway | No API access; entire system down | 5 min | 0 |
| **P2** | P6 FirstResponder | Dispatch delayed; responder tracking lost | 15 min | 5 min |
| **P2** | Geospatial | No location services; routing unavailable | 15 min | 5 min |
| **P2** | P3 MeshNetwork | Offline communications unavailable | 15 min | 5 min |
| **P3** | P7 FamilyHealth | Health monitoring interrupted | 30 min | 15 min |
| **P3** | P4 Wearable | Device telemetry interrupted | 30 min | 15 min |
| **P3** | P9 DoctorServices | Telehealth appointments disrupted | 30 min | 15 min |
| **P3** | P8 DisasterRelief | Disaster coordination delayed | 30 min | 15 min |
| **P4** | P11 Surveillance | Camera monitoring paused | 1 hour | 30 min |
| **P4** | P10 Gamification | Engagement features unavailable | 4 hours | 1 hour |
| **P4** | Dashboard/Admin | Admin operations delayed | 1 hour | 30 min |

### 3.2 Continuity Strategies

| Strategy | Implementation | Services | Standard |
|----------|---------------|----------|----------|
| **Multi-Replica Deployment** | Kubernetes replicas (1-20 per service) with PDB | All services | NIST 800-53 CP-7 |
| **Auto-Scaling** | KEDA event-driven scaling + Karpenter node provisioning | All services | NIST 800-53 CP-2 |
| **Database Geo-Replication** | Azure SQL geo-replication; Cosmos DB multi-region write | SQL, Cosmos | NIST 800-53 CP-9 |
| **Offline-First Mobile** | SQLite local database + sync engine + BLE mesh fallback | Mobile App | NIST 800-53 CP-8 |
| **Event Bus Durability** | Kafka partition replication; message persistence | Event-driven services | NIST 800-53 CP-9 |
| **Container Registry DR** | Geo-replicated Azure Container Registry | Container pulls | NIST 800-53 CP-9 |
| **Multi-Cloud Readiness** | Terraform modules for Azure, AWS, GCP | Full stack | NIST 800-53 CP-6 |

### 3.3 Offline Operations

TheWatch mobile application maintains emergency operations capability during network outages:

| Capability | Mechanism | Duration | Standard |
|------------|----------|----------|----------|
| **Request Queuing** | FIFO SQLite queue with priority ordering | Indefinite | NIST 800-53 CP-2 |
| **Mesh Communication** | BLE device-to-device relay | Range-limited | NIST 800-53 CP-8 |
| **Local Data Access** | SQLite cached data | Last sync + queue | NIST 800-53 CP-9 |
| **Auto-Sync** | ConnectivityMonitor triggers SyncEngine on reconnect | Automatic | NIST 800-53 CP-10 |
| **Retry Logic** | Exponential backoff (5 retries maximum) | Per-request | NIST 800-53 SC-5 |

**Evidence:** `TheWatch.Mobile/Services/OfflineQueueService.cs`, `TheWatch.Mobile/Services/SyncEngine.cs`, `TheWatch.Mobile/Services/ConnectivityMonitorService.cs`

---

## 4. Disaster Recovery

### 4.1 Recovery Procedures

| Scenario | Recovery Action | RTO | Standard |
|----------|----------------|-----|----------|
| **Single Service Failure** | Kubernetes auto-restart; health check recovery | < 5 min | NIST 800-53 CP-10 |
| **Database Failure** | Failover to geo-replica; promote read replica | < 15 min | NIST 800-53 CP-9 |
| **Cloud Region Outage** | Multi-region deployment; DNS failover | < 30 min | NIST 800-53 CP-7 |
| **Full Cloud Provider Outage** | Multi-cloud deployment (Azure → AWS/GCP) | < 4 hours | NIST 800-53 CP-6 |
| **Data Corruption** | Point-in-time restore from backups | < 1 hour | NIST 800-53 CP-9 |
| **Supply Chain Compromise** | Rebuild from verified source; SBOM analysis | < 8 hours | NIST 800-53 SR-3 |
| **Ransomware** | Clean rebuild from immutable infrastructure; backup restore | < 4 hours | NIST 800-53 CP-10 |

### 4.2 Backup and Recovery Testing

| Test Type | Frequency | Scope | Standard |
|-----------|-----------|-------|----------|
| **Backup Verification** | Monthly | Verify backup integrity and completeness | NIST 800-53 CP-9(1) |
| **Recovery Exercise** | Quarterly | Single service failover and recovery | NIST 800-53 CP-4 |
| **DR Tabletop** | Semi-annually | Full disaster scenario walkthrough | NIST 800-53 CP-4(1) |
| **Full DR Test** | Annually | Multi-cloud failover exercise | NIST 800-53 CP-4 |

---

## 5. IR Testing & Exercises

### 5.1 Exercise Schedule

| Exercise Type | Frequency | Participants | Scenarios |
|--------------|-----------|-------------|-----------|
| **Tabletop** | Quarterly | IR team, management | CUI data breach, DDoS, supply chain compromise |
| **Simulation** | Semi-annually | IR team, DevOps | Simulated incidents with real tooling |
| **Full-Scale** | Annually | All teams, management | Live incident with containment and recovery |
| **Red Team** | Annually | External testers | Adversarial testing per pentest program |

### 5.2 Communication Plan

| Audience | Channel | Timeline | Content |
|----------|---------|----------|---------|
| **IR Team** | Direct notification (on-call) | Immediate | Technical details, severity, actions |
| **Management** | Email + meeting | Within 1 hour (P1/P2) | Impact assessment, resource needs |
| **DoD/DC3** | DIBNet portal | Within 72 hours (CUI breach) | DFARS-compliant report |
| **Users** | In-app notification | As needed | Service status, protective actions |
| **Legal/Compliance** | Direct briefing | Within 24 hours (P1) | Regulatory implications, evidence preservation |

---

## 6. Standards Mapping

| IR Requirement | Standard Reference | TheWatch Implementation |
|---------------|-------------------|------------------------|
| Incident handling capability | NIST 800-171 IR-1 / 3.6.1 | 5-category IR plan with 4 severity levels |
| Incident tracking & reporting | NIST 800-171 IR-8 / 3.6.2 | Post-incident review; 72h DoD reporting |
| IR plan testing | NIST 800-171 IR-3 / 3.6.3 | Quarterly tabletop; annual full-scale |
| IR plan documentation | NIST 800-53 IR-1 | `docs/incident-response-plan.md` |
| Incident monitoring | NIST 800-53 IR-4 | STRIDE + ATT&CK + rate limiting + brute force detection |
| Incident reporting | NIST 800-53 IR-6 | DoD reporting procedure; DIBNet |
| Incident response assistance | NIST 800-53 IR-7 | External pentest partners; DC3 coordination |
| IR plan updates | NIST 800-53 IR-8 | Post-incident review updates |
| Continuity planning | NIST 800-53 CP-2 | Multi-replica; auto-scaling; offline mobile |
| Backup & recovery | NIST 800-53 CP-9 | Geo-replicated databases; automated backups |
| Recovery & reconstitution | NIST 800-53 CP-10 | Container rebuild; backup restore; health checks |
| Cyber incident reporting | DFARS 252.204-7012(c) | 72-hour reporting to DC3 |
| Image preservation | DFARS 252.204-7012(c) | 90-day forensic evidence preservation |

---

*This Incident Response & Business Continuity Plan ensures TheWatch can detect, respond to,
and recover from security incidents while meeting DoD reporting requirements and maintaining
emergency operations capability.*
