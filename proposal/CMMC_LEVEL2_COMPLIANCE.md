# CMMC Level 2 Compliance Matrix — TheWatch Platform

> **Classification:** CUI // SP-PRIV // SP-HLTH  
> **Reference Standards:** CMMC 2.0 Level 2 (Final Rule Oct 2024), NIST SP 800-171 Rev 2, DISA STIG  
> **Document ID:** CMMC-001  
> **Version:** 1.0

---

## 1. Compliance Summary

CMMC Level 2 requires implementation of all 110 practices derived from NIST SP 800-171 Rev 2.
This matrix documents TheWatch's implementation status for each CMMC domain.

| CMMC Domain | Practices | Fully Met | Partial | Planned | Compliance Rate |
|-------------|-----------|-----------|---------|---------|-----------------|
| **AC** — Access Control | 22 | 20 | 1 | 1 | 91% |
| **AT** — Awareness & Training | 3 | 0 | 3 | 0 | 0% (partial) |
| **AU** — Audit & Accountability | 9 | 8 | 1 | 0 | 89% |
| **CM** — Configuration Management | 9 | 9 | 0 | 0 | 100% |
| **IA** — Identification & Authentication | 11 | 8 | 2 | 1 | 73% |
| **IR** — Incident Response | 3 | 2 | 1 | 0 | 67% |
| **MA** — Maintenance | 6 | 5 | 1 | 0 | 83% |
| **MP** — Media Protection | 9 | 6 | 3 | 0 | 67% |
| **PS** — Personnel Security | 2 | 2 | 0 | 0 | 100% |
| **PE** — Physical & Environmental | 6 | 6 | 0 | 0 | 100% |
| **RA** — Risk Assessment | 3 | 3 | 0 | 0 | 100% |
| **CA** — Security Assessment | 4 | 4 | 0 | 0 | 100% |
| **SC** — System & Communications | 16 | 10 | 4 | 0 | 63% |
| **SI** — System & Information Integrity | 7 | 7 | 0 | 0 | 100% |
| **TOTAL** | **110** | **90** | **16** | **2** | **82%** |

---

## 2. Domain-by-Domain Practice Mapping

### 2.1 Access Control (AC)

| CMMC Practice | NIST 800-171 | DISA STIG | Description | Status | Implementation Evidence |
|---------------|-------------|-----------|-------------|--------|------------------------|
| AC.L2-3.1.1 | 3.1.1 | V-222425 | Authorized access control | **[MET]** | JWT bearer auth on all endpoints; `[Authorize]` attribute default |
| AC.L2-3.1.2 | 3.1.2 | V-222426 | Transaction & function control | **[MET]** | 6 RBAC policies: AdminOnly, ResponderAccess, DoctorAccess, FamilyAccess, PatientAccess, Authenticated |
| AC.L2-3.1.3 | 3.1.3 | — | CUI flow enforcement | **[MET]** | `CuiMarkingMiddleware` applies SP-PRIV/SP-HLTH/SP-LEI/SP-GEO headers per route |
| AC.L2-3.1.4 | 3.1.4 | — | Separation of duties | **[MET]** | 6 distinct roles; Admin cannot self-assign roles without separate admin approval |
| AC.L2-3.1.5 | 3.1.5 | V-222427 | Least privilege | **[MET]** | `CallerCanAccessUser()` IDOR checks; service accounts scoped per service |
| AC.L2-3.1.6 | 3.1.6 | V-222425 | Non-privileged account use | **[MET]** | Containers: non-root `appuser` UID 1001; service accounts: minimal DB permissions |
| AC.L2-3.1.7 | 3.1.7 | — | Privileged function prevention | **[MET]** | `[Authorize(Policy = "AdminOnly")]` on admin endpoints |
| AC.L2-3.1.8 | 3.1.8 | V-222534 | Unsuccessful logon attempts | **[MET]** | 3-attempt lockout (15 min); 10 attempts (60 min); 15+ deactivation |
| AC.L2-3.1.9 | 3.1.9 | — | Privacy & security notices | **[MET]** | EULA versioned acceptance; IP logging; consent tracking |
| AC.L2-3.1.10 | 3.1.10 | V-222535 | Session lock | **[MET]** | JWT 30-min expiration; refresh token 8-hour max; automatic timeout |
| AC.L2-3.1.11 | 3.1.11 | — | Session termination | **[MET]** | Token revocation on logout; invalidation on password change |
| AC.L2-3.1.12 | 3.1.12 | V-222536 | Remote access control | **[MET]** | TLS 1.2+ enforced; rate limiting; CORS whitelist |
| AC.L2-3.1.13 | 3.1.13 | — | Remote access encryption | **[MET]** | TLS 1.2/1.3 only; HSTS 365d with preload |
| AC.L2-3.1.14 | 3.1.14 | — | Route through managed access points | **[MET]** | All traffic through P1 CoreGateway; no direct service exposure |
| AC.L2-3.1.15 | 3.1.15 | — | Authorize privileged remote commands | **[MET]** | Admin CLI requires JWT auth; all admin actions logged |
| AC.L2-3.1.16 | 3.1.16 | — | Wireless access authorization | **[MET]** | Mobile JWT auth; BLE mesh requires device registration |
| AC.L2-3.1.17 | 3.1.17 | — | Wireless access protection | **[MET]** | TLS 1.2+ for all wireless; WPA3 deployment guidance |
| AC.L2-3.1.18 | 3.1.18 | — | Mobile device control | **[MET]** | Device trust scoring; biometric gate; fingerprint+IP+geo |
| AC.L2-3.1.19 | 3.1.19 | — | Encrypt CUI on mobile | **[PARTIAL]** | SQLite with encryption planned; chain-of-custody for evidence |
| AC.L2-3.1.20 | 3.1.20 | — | External system connections | **[MET]** | Typed HTTP clients; explicit endpoint configuration |
| AC.L2-3.1.21 | 3.1.21 | — | Portable storage use | **[MET]** | Evidence requires chain-of-custody logging; no arbitrary export |
| AC.L2-3.1.22 | 3.1.22 | — | Public CUI control | **[MET]** | No public CUI endpoints; authentication required |

### 2.2 Awareness and Training (AT)

| CMMC Practice | NIST 800-171 | Description | Status | Implementation Evidence |
|---------------|-------------|-------------|--------|------------------------|
| AT.L2-3.2.1 | 3.2.1 | Role-based risk awareness | **[PARTIAL]** | Security policies documented; developer guide includes security requirements; formal training program planned |
| AT.L2-3.2.2 | 3.2.2 | Role-based training | **[PARTIAL]** | Developer setup guide with security practices; signed commit enforcement as training |
| AT.L2-3.2.3 | 3.2.3 | Insider threat awareness | **[PARTIAL]** | STRIDE threat model; MITRE ATT&CK rules; IR plan with insider threat category |

### 2.3 Audit and Accountability (AU)

| CMMC Practice | NIST 800-171 | DISA STIG | Description | Status | Implementation Evidence |
|---------------|-------------|-----------|-------------|--------|------------------------|
| AU.L2-3.3.1 | 3.3.1 | V-222457 | System audit logs | **[MET]** | Serilog structured logging; all 11 services; JSON format |
| AU.L2-3.3.2 | 3.3.2 | V-222458 | User accountability | **[MET]** | JWT claims in logs; user ID + role + device tracking |
| AU.L2-3.3.3 | 3.3.3 | — | Log review and analysis | **[MET]** | Admin audit log page; review schedule (weekly/monthly/quarterly) |
| AU.L2-3.3.4 | 3.3.4 | — | Audit process failure alerts | **[PARTIAL]** | Health checks monitor logging; alerting integration planned |
| AU.L2-3.3.5 | 3.3.5 | — | Correlate audit records | **[MET]** | Correlation IDs across service boundaries; structured tracing |
| AU.L2-3.3.6 | 3.3.6 | — | Audit reduction & reporting | **[MET]** | Structured JSON enables filtering; admin dashboard views |
| AU.L2-3.3.7 | 3.3.7 | — | Authoritative time source | **[MET]** | Container NTP sync; UTC timestamps |
| AU.L2-3.3.8 | 3.3.8 | — | Protect audit information | **[MET]** | RBAC on audit endpoints; AdminOnly access |
| AU.L2-3.3.9 | 3.3.9 | — | Audit management restriction | **[MET]** | Admin-only logging configuration |

### 2.4 Configuration Management (CM)

| CMMC Practice | NIST 800-171 | Description | Status | Implementation Evidence |
|---------------|-------------|-------------|--------|------------------------|
| CM.L2-3.4.1 | 3.4.1 | Baseline configurations | **[MET]** | `Directory.Build.props`, `Directory.Packages.props`, pinned Docker images |
| CM.L2-3.4.2 | 3.4.2 | Security configuration enforcement | **[MET]** | Kestrel hardening; DISA STIG container config; TLS enforcement |
| CM.L2-3.4.3 | 3.4.3 | System change tracking | **[MET]** | GitHub branch protection; 2 approvals; signed commits; CODEOWNERS |
| CM.L2-3.4.4 | 3.4.4 | Security impact analysis | **[MET]** | CodeQL SAST; dependency review; security workflow on every push |
| CM.L2-3.4.5 | 3.4.5 | Access restrictions for change | **[MET]** | Access control policy; PR-based change management |
| CM.L2-3.4.6 | 3.4.6 | Least functionality | **[MET]** | Single-responsibility microservices; minimal Docker images |
| CM.L2-3.4.7 | 3.4.7 | Nonessential functionality restriction | **[MET]** | Multi-stage Docker builds; runtime-only containers |
| CM.L2-3.4.8 | 3.4.8 | Unauthorized software policy | **[MET]** | NuGetAudit deny-by-default; vulnerability-as-error in CI |
| CM.L2-3.4.9 | 3.4.9 | User-installed software control | **[MET]** | Immutable containers; no user-installable software |

### 2.5 Identification and Authentication (IA)

| CMMC Practice | NIST 800-171 | DISA STIG | Description | Status | Implementation Evidence |
|---------------|-------------|-----------|-------------|--------|------------------------|
| IA.L2-3.5.1 | 3.5.1 | — | User identification | **[MET]** | Unique user IDs; JWT claims; service account IDs |
| IA.L2-3.5.2 | 3.5.2 | — | Authentication of users & devices | **[MET]** | JWT bearer; MFA (4 methods); device trust scoring |
| IA.L2-3.5.3 | 3.5.3 | V-222530 | Multi-factor authentication | **[MET]** | TOTP, FIDO2, SMS, Magic Link; mandatory for privileged roles |
| IA.L2-3.5.4 | 3.5.4 | — | Replay-resistant authentication | **[MET]** | JWT jti + iat + exp claims; TOTP time windowing |
| IA.L2-3.5.5 | 3.5.5 | — | Identifier reuse prevention | **[MET]** | Database unique constraints on user IDs and emails |
| IA.L2-3.5.6 | 3.5.6 | — | Identifier inactivity disable | **[PARTIAL]** | Account deactivation after 15+ failures; inactivity monitoring planned |
| IA.L2-3.5.7 | 3.5.7 | V-222524 | Password complexity | **[PARTIAL]** | 8-char minimum with complexity; POA&M: upgrade to 15-char per STIG |
| IA.L2-3.5.8 | 3.5.8 | V-222546 | Password reuse prohibition | **[PLANNED]** | POA&M: implement 24-generation history |
| IA.L2-3.5.9 | 3.5.9 | — | Temporary password change | **[MET]** | Forced password change on first login |
| IA.L2-3.5.10 | 3.5.10 | — | Cryptographic password protection | **[MET]** | Argon2id default; PBKDF2-SHA512 FIPS fallback; TLS transport |
| IA.L2-3.5.11 | 3.5.11 | — | Authentication feedback obscuration | **[MET]** | Masked password fields; generic error messages |

### 2.6 Incident Response (IR)

| CMMC Practice | NIST 800-171 | Description | Status | Implementation Evidence |
|---------------|-------------|-------------|--------|------------------------|
| IR.L2-3.6.1 | 3.6.1 | Incident handling capability | **[MET]** | 5-category IR plan; 4 severity levels; detection through recovery |
| IR.L2-3.6.2 | 3.6.2 | Incident tracking & reporting | **[MET]** | 72-hour DoD/DC3 reporting; post-incident review; forensic preservation |
| IR.L2-3.6.3 | 3.6.3 | Incident response testing | **[PARTIAL]** | Quarterly tabletop exercises planned; annual full-scale planned |

### 2.7 Maintenance (MA)

| CMMC Practice | NIST 800-171 | Description | Status | Implementation Evidence |
|---------------|-------------|-------------|--------|------------------------|
| MA.L2-3.7.1 | 3.7.1 | System maintenance | **[MET]** | Renovate automated updates; NuGet audit; Trivy scanning |
| MA.L2-3.7.2 | 3.7.2 | Maintenance tool controls | **[MET]** | Ephemeral CI runners; version-pinned tools |
| MA.L2-3.7.3 | 3.7.3 | Equipment sanitization | **[MET]** | Stateless containers rebuilt each deployment |
| MA.L2-3.7.4 | 3.7.4 | Diagnostic program media check | **[PARTIAL]** | SBOM generation; NuGet signature verification planned |
| MA.L2-3.7.5 | 3.7.5 | Nonlocal maintenance MFA | **[MET]** | Admin CLI/API require JWT + MFA for admin roles |
| MA.L2-3.7.6 | 3.7.6 | Maintenance activity supervision | **[MET]** | GitHub audit log; PR review mandated |

### 2.8 Media Protection (MP)

| CMMC Practice | NIST 800-171 | Description | Status | Implementation Evidence |
|---------------|-------------|-------------|--------|------------------------|
| MP.L2-3.8.1 | 3.8.1 | Media protection | **[PARTIAL]** | AES-256-GCM field encryption; TDE planned |
| MP.L2-3.8.2 | 3.8.2 | CUI media access limitation | **[MET]** | RBAC on data access; CUI marking middleware |
| MP.L2-3.8.3 | 3.8.3 | Media sanitization | **[PARTIAL]** | Stateless containers destroyed; backup sanitization planned |
| MP.L2-3.8.4 | 3.8.4 | CUI media marking | **[MET]** | CUI classification headers in HTTP responses |
| MP.L2-3.8.5 | 3.8.5 | Controlled area media access | **[MET]** | Cloud provider physical security; namespace isolation |
| MP.L2-3.8.6 | 3.8.6 | Portable media encryption | **[MET]** | TLS transport; SHA-256 evidence hashing |
| MP.L2-3.8.7 | 3.8.7 | Removable media control | **[MET]** | Chain-of-custody required for evidence collection |
| MP.L2-3.8.8 | 3.8.8 | Shared media prohibition | **[MET]** | Evidence tagged with user ID, device, GPS |
| MP.L2-3.8.9 | 3.8.9 | Backup CUI protection | **[PARTIAL]** | Encrypted backup configs in Terraform; implementation in progress |

### 2.9 Personnel Security (PS)

| CMMC Practice | NIST 800-171 | Description | Status | Implementation Evidence |
|---------------|-------------|-------------|--------|------------------------|
| PS.L2-3.9.1 | 3.9.1 | Personnel screening | **[MET]** | Identity verification on registration; admin approval for roles |
| PS.L2-3.9.2 | 3.9.2 | CUI during personnel actions | **[MET]** | Account deactivation; role revocation; token invalidation |

### 2.10 Physical and Environmental Protection (PE)

| CMMC Practice | NIST 800-171 | Description | Status | Implementation Evidence |
|---------------|-------------|-------------|--------|------------------------|
| PE.L2-3.10.1 | 3.10.1 | Physical access limitation | **[MET]** | Cloud provider physical security (inherited) |
| PE.L2-3.10.2 | 3.10.2 | Facility monitoring | **[MET]** | Cloud provider + Kubernetes node security |
| PE.L2-3.10.3 | 3.10.3 | Visitor escort | **[MET]** | Cloud provider responsibility (inherited) |
| PE.L2-3.10.4 | 3.10.4 | Physical access audit logs | **[MET]** | Cloud provider + Kubernetes audit logging |
| PE.L2-3.10.5 | 3.10.5 | Physical access device management | **[MET]** | Azure Managed Identity; no physical keys |
| PE.L2-3.10.6 | 3.10.6 | Alternate work site safeguarding | **[MET]** | Mobile TLS; biometric gate; device trust |

### 2.11 Risk Assessment (RA)

| CMMC Practice | NIST 800-171 | Description | Status | Implementation Evidence |
|---------------|-------------|-------------|--------|------------------------|
| RA.L2-3.11.1 | 3.11.1 | Risk assessments | **[MET]** | STRIDE + MITRE ATT&CK; DoD Security Analysis; risk register |
| RA.L2-3.11.2 | 3.11.2 | Vulnerability scanning | **[MET]** | CodeQL, Trivy, Gitleaks, NuGet Audit, GitHub Dependency Review, VulnerabilityMonitorService |
| RA.L2-3.11.3 | 3.11.3 | Vulnerability remediation | **[MET]** | SLA-based: Critical 7d, High 30d, Medium 90d, Low 180d; POA&M |

### 2.12 Security Assessment (CA)

| CMMC Practice | NIST 800-171 | Description | Status | Implementation Evidence |
|---------------|-------------|-------------|--------|------------------------|
| CA.L2-3.12.1 | 3.12.1 | Security control assessment | **[MET]** | DoD readiness workflow; penetration testing program |
| CA.L2-3.12.2 | 3.12.2 | Plan of action | **[MET]** | POA&M: 50 findings with sprint-based remediation |
| CA.L2-3.12.3 | 3.12.3 | Continuous monitoring | **[MET]** | CISA KEV, NuGet Advisory, GitHub Advisory monitoring |
| CA.L2-3.12.4 | 3.12.4 | System security plan | **[MET]** | `proposal/SYSTEM_SECURITY_PLAN.md` |

### 2.13 System and Communications Protection (SC)

| CMMC Practice | NIST 800-171 | DISA STIG | Description | Status | Implementation Evidence |
|---------------|-------------|-----------|-------------|--------|------------------------|
| SC.L2-3.13.1 | 3.13.1 | V-222602 | Boundary protection | **[MET]** | CoreGateway; rate limiting; CORS; security headers |
| SC.L2-3.13.2 | 3.13.2 | — | Security architecture | **[MET]** | Microservices isolation; event-driven; defense in depth |
| SC.L2-3.13.3 | 3.13.3 | — | User/management separation | **[MET]** | Admin portal separate from user apps |
| SC.L2-3.13.4 | 3.13.4 | — | Shared resource control | **[MET]** | CUI marking; PII redaction; data classification |
| SC.L2-3.13.5 | 3.13.5 | — | Public access subnetworks | **[MET]** | Kubernetes namespaces; Docker networks |
| SC.L2-3.13.6 | 3.13.6 | — | Network deny by default | **[PARTIAL]** | Docker network isolation; K8s NetworkPolicy planned |
| SC.L2-3.13.7 | 3.13.7 | — | Split tunneling prevention | **[MET]** | Mobile routes through CoreGateway |
| SC.L2-3.13.8 | 3.13.8 | V-222603 | CUI transmission encryption | **[MET]** | TLS 1.2/1.3; HSTS 365d; certificate pinning |
| SC.L2-3.13.9 | 3.13.9 | — | Connection termination | **[MET]** | JWT expiration; connection timeouts |
| SC.L2-3.13.10 | 3.13.10 | — | Key management | **[PARTIAL]** | RSA-2048 JWT; AES-256 field encryption; Key Vault planned |
| SC.L2-3.13.11 | 3.13.11 | V-222604 | FIPS-validated cryptography | **[PARTIAL]** | PBKDF2-SHA512 fallback; FIPS mode configuration planned |
| SC.L2-3.13.12 | 3.13.12 | — | Collaborative computing | **N/A** | No collaborative computing devices |
| SC.L2-3.13.13 | 3.13.13 | — | Mobile code control | **[MET]** | Managed app distribution; signed MAUI code |
| SC.L2-3.13.14 | 3.13.14 | — | VoIP protection | **N/A** | No VoIP services |
| SC.L2-3.13.15 | 3.13.15 | — | Communication authenticity | **[MET]** | JWT binding; device trust; HMAC audit records |
| SC.L2-3.13.16 | 3.13.16 | V-222605 | CUI at rest protection | **[PARTIAL]** | AES-256-GCM fields; TDE and full disk encryption planned |

### 2.14 System and Information Integrity (SI)

| CMMC Practice | NIST 800-171 | Description | Status | Implementation Evidence |
|---------------|-------------|-------------|--------|------------------------|
| SI.L2-3.14.1 | 3.14.1 | Flaw remediation | **[MET]** | VulnerabilityMonitorService; SLA remediation; Renovate updates |
| SI.L2-3.14.2 | 3.14.2 | Malicious code protection | **[MET]** | CodeQL; Trivy; Gitleaks; .NET analyzers |
| SI.L2-3.14.3 | 3.14.3 | Security alert monitoring | **[MET]** | CISA KEV, NuGet Advisory, GitHub Advisory monitoring |
| SI.L2-3.14.4 | 3.14.4 | Update malicious code mechanisms | **[MET]** | Renovate; container base image updates; CodeQL rule updates |
| SI.L2-3.14.5 | 3.14.5 | System & file scanning | **[MET]** | CodeQL SAST every push; Trivy containers; SBOM generation |
| SI.L2-3.14.6 | 3.14.6 | Inbound/outbound monitoring | **[MET]** | Rate limiting logging; CORS violations; auth failures |
| SI.L2-3.14.7 | 3.14.7 | Unauthorized use identification | **[MET]** | STRIDE 15-min scans; MITRE ATT&CK rules; brute force detection |

---

## 3. DISA STIG Cross-Reference

| DISA STIG Finding | Category | Requirement | TheWatch Status | CMMC Practice |
|-------------------|----------|-------------|-----------------|---------------|
| V-222425 | CAT I | Non-root container execution | **[MET]** — `appuser` UID 1001 | AC.L2-3.1.6 |
| V-222427 | CAT I | Least privilege enforcement | **[MET]** — RBAC + IDOR prevention | AC.L2-3.1.5 |
| V-222524 | CAT II | Password minimum 15 characters | **[PARTIAL]** — Currently 8, POA&M Sprint 3 | IA.L2-3.5.7 |
| V-222530 | CAT II | Multi-factor authentication | **[MET]** — 4 MFA methods | IA.L2-3.5.3 |
| V-222534 | CAT II | Login attempt limiting (3 max) | **[MET]** — 3-attempt lockout | AC.L2-3.1.8 |
| V-222535 | CAT II | Session timeout (30 min) | **[MET]** — JWT 30-min expiration | AC.L2-3.1.10 |
| V-222536 | CAT II | Remote access control | **[MET]** — TLS + rate limiting | AC.L2-3.1.12 |
| V-222545 | CAT II | Password max age (60 days) | **[PLANNED]** — POA&M Sprint 3 | IA.L2-3.5.7 |
| V-222546 | CAT II | Password history (24 gen) | **[PLANNED]** — POA&M Sprint 3 | IA.L2-3.5.8 |
| V-222457 | CAT II | Audit logging enabled | **[MET]** — Serilog across all services | AU.L2-3.3.1 |
| V-222458 | CAT II | User accountability in logs | **[MET]** — JWT claims in audit records | AU.L2-3.3.2 |
| V-222602 | CAT I | Boundary protection | **[MET]** — CoreGateway + rate limiting | SC.L2-3.13.1 |
| V-222603 | CAT I | Transmission encryption | **[MET]** — TLS 1.2/1.3 only | SC.L2-3.13.8 |
| V-222604 | CAT I | FIPS cryptography | **[PARTIAL]** — PBKDF2 fallback | SC.L2-3.13.11 |
| V-222605 | CAT I | CUI at rest encryption | **[PARTIAL]** — Field encryption; TDE planned | SC.L2-3.13.16 |

---

## 4. Remediation Roadmap

| Sprint | Timeline | Focus Area | Key Practices Addressed |
|--------|----------|-----------|------------------------|
| **Sprint 1** | Weeks 1–2 | Cryptographic compliance | SC.L2-3.13.11 (FIPS mode), SC.L2-3.13.10 (key management) |
| **Sprint 2** | Weeks 3–4 | Data encryption at rest | SC.L2-3.13.16 (TDE), MP.L2-3.8.1 (media protection) |
| **Sprint 3** | Weeks 5–6 | Authentication hardening | IA.L2-3.5.7 (15-char password), IA.L2-3.5.8 (password history) |
| **Sprint 4** | Weeks 7–8 | Supply chain security | MA.L2-3.7.4 (SBOM signing), additional SSDF practices |
| **Sprint 5** | Weeks 9–10 | Monitoring & alerting | AU.L2-3.3.4 (audit failure alerts), SC.L2-3.13.6 (network policies) |
| **Sprint 6** | Weeks 11–12 | Training & documentation | AT.L2-3.2.1–3.2.3 (training program), IR.L2-3.6.3 (IR testing) |

---

*This CMMC Level 2 Compliance Matrix is maintained in version control and updated
as remediation progresses. Full POA&M details are in `docs/POA&M.md`.*
