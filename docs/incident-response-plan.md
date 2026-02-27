# TheWatch — Incident Response Plan (IRP)

> NIST 800-171 IR-1, IR-8 | CMMC Level 2 | Effective: 2026-02-26

## 1. Incident Categories

| Category | Description | Example |
|----------|-------------|---------|
| CAT-1 | Unauthorized access to CUI | Database breach, credential theft |
| CAT-2 | Denial of service | DDoS attack, resource exhaustion |
| CAT-3 | Malicious code | Malware, ransomware, supply chain compromise |
| CAT-4 | Improper usage | Policy violation, unauthorized configuration change |
| CAT-5 | Reconnaissance | Port scanning, vulnerability scanning |

## 2. Severity Levels

| Level | Response Time | Notification | Description |
|-------|--------------|-------------|-------------|
| P1 — Critical | 15 minutes | Immediate | Active breach, CUI exposure, system compromise |
| P2 — High | 1 hour | Within 2 hours | Attempted breach detected, credential stuffing |
| P3 — Medium | 4 hours | Within 24 hours | Vulnerability discovered, suspicious activity |
| P4 — Low | 24 hours | Weekly report | Policy violation, minor misconfiguration |

## 3. Detection Mechanisms
- STRIDE threat modeling (automated scan every 15 minutes)
- MITRE ATT&CK detection rules (automated scan every 15 minutes)
- Rate limiting alerts (threshold breaches)
- SIEM correlation rules (multi-service attack patterns)
- Brute force detection service
- Container security scanning (Trivy)

## 4. Response Procedures

### 4.1 Detection & Analysis
1. Alert received via SIEM, PagerDuty, or manual report
2. On-call engineer confirms incident severity
3. Gather initial evidence (logs, timestamps, affected systems)
4. Classify incident category and severity level

### 4.2 Containment
1. Isolate affected service(s) — scale to zero or network policy block
2. Revoke compromised credentials immediately
3. Block attacking IP addresses at WAF level
4. Preserve forensic evidence (container snapshots, log exports)

### 4.3 Eradication
1. Identify root cause
2. Apply patches or configuration fixes
3. Rotate all potentially compromised secrets
4. Rebuild affected containers from verified images

### 4.4 Recovery
1. Restore services from verified clean state
2. Monitor for recurrence (enhanced alerting for 72 hours)
3. Verify data integrity (audit log chain, database checksums)
4. Gradually restore normal operations

### 4.5 Post-Incident
1. Conduct post-incident review within 72 hours
2. Update POA&M with findings
3. Update detection rules to prevent recurrence
4. Document lessons learned

## 5. DoD Reporting Requirements
- Report cyber incidents to DC3 (DoD Cyber Crime Center) within 72 hours
- Preserve forensic images for 90 days minimum
- Provide incident report per DFARS 252.204-7012
