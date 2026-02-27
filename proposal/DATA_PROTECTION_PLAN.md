# Data Protection Plan — TheWatch Platform

> **Classification:** CUI // SP-PRIV // SP-HLTH // SP-LEI // SP-GEO  
> **Reference Standards:** NIST SP 800-171 Rev 2 (MP, SC families), DFARS 252.204-7012,
> HIPAA Security Rule §164.312, NIST SP 800-53 Rev 5  
> **Document ID:** DPP-001  
> **Version:** 1.0

---

## 1. Data Classification Framework

### 1.1 CUI Categories

TheWatch processes five categories of Controlled Unclassified Information (CUI) as defined
by the CUI Registry (32 CFR Part 2002):

| CUI Category | Abbreviation | Description | Handling Requirements |
|--------------|-------------|-------------|----------------------|
| **SP-PRIV** | Specified — Privacy | Personally Identifiable Information (PII): names, emails, phone numbers, addresses, dates of birth | Encryption at rest + transit; access logging; PII redaction in logs |
| **SP-HLTH** | Specified — Health | Protected Health Information (PHI): vital signs, medical records, prescriptions, health alerts | HIPAA safeguards; MFA for access; BAA tracking; minimum necessary |
| **SP-LEI** | Specified — Law Enforcement | Law enforcement sensitive: incident reports, dispatch records, crime locations, evidence, surveillance footage | mTLS planned; restricted access; chain-of-custody; evidence integrity |
| **SP-GEO** | Specified — Geolocation | Sensitive geolocation: responder positions, family geofences, evacuation routes, disaster zones | Geofencing; access controls; position data encryption |
| **SP-BASIC** | Basic CUI | General operational data: configurations, gamification, system logs | Standard CUI handling; access controls; audit logging |

### 1.2 Entity-Level Classification

| Service | Entity | CUI Category | Encryption Required | MFA Required |
|---------|--------|-------------|--------------------|----|
| **P5 Auth** | WatchUser.Email | SP-PRIV | AES-256-GCM | Yes (Admin/Responder/Doctor) |
| **P5 Auth** | WatchUser.PhoneNumber | SP-PRIV | AES-256-GCM | Yes |
| **P5 Auth** | WatchUser.PasswordHash | SP-PRIV | Argon2id/PBKDF2 | N/A (derived) |
| **P7 Family** | VitalReading.HeartRate | SP-HLTH | AES-256-GCM | Yes |
| **P7 Family** | VitalReading.BloodPressure | SP-HLTH | AES-256-GCM | Yes |
| **P7 Family** | FamilyMember.DateOfBirth | SP-PRIV | AES-256-GCM | No |
| **P9 Doctor** | MedicalRecord.* | SP-HLTH | AES-256-GCM | Yes |
| **P9 Doctor** | Prescription.* | SP-HLTH | AES-256-GCM | Yes |
| **P2 Voice** | EmergencyIncident.* | SP-LEI | AES-256-GCM | Yes (Responder) |
| **P6 Responder** | ResponderLocation.GPS | SP-GEO | AES-256-GCM | No |
| **P6 Responder** | IncidentReport.* | SP-LEI | AES-256-GCM | Yes |
| **P11 Surveillance** | CrimeLocation.Coordinates | SP-LEI | AES-256-GCM | Yes (Responder) |
| **P11 Surveillance** | FootageSubmission.* | SP-LEI | AES-256-GCM | Yes |
| **P8 Disaster** | EvacuationRoute.Waypoints | SP-GEO | AES-256-GCM | No |
| **P8 Disaster** | Shelter.Location | SP-GEO | Standard | No |

**Full classification matrix:** `docs/data-classification-matrix.md`

---

## 2. Encryption Architecture

### 2.1 Encryption at Rest

| Layer | Mechanism | Algorithm | Key Size | Status | Standard |
|-------|-----------|-----------|----------|--------|----------|
| **Field-Level** | `AesGcmFieldEncryptor` | AES-256-GCM | 256-bit key, 12-byte nonce, 16-byte tag | **[IMPLEMENTED]** | NIST 800-171 SC-28 |
| **Database TDE** | SQL Server Transparent Data Encryption | AES-256 | 256-bit | **[PLANNED]** | NIST 800-171 SC-28 |
| **PostgreSQL** | pgcrypto / full disk encryption | AES-256 | 256-bit | **[PLANNED]** | NIST 800-171 SC-28 |
| **Mobile Local** | SQLite encryption | AES-256 | 256-bit | **[PARTIAL]** | NIST 800-171 SC-28(1) |
| **Backups** | Azure Storage Service Encryption | AES-256 | 256-bit | **[PLANNED]** | NIST 800-171 MP-4 |

#### Field-Level Encryption Implementation

```
Input: plaintext → AES-256-GCM(key, nonce[12], plaintext)
Output: Base64(nonce[12] || ciphertext[N] || authTag[16])

Properties:
- Authenticated encryption (integrity + confidentiality)
- Unique nonce per encryption operation
- 16-byte authentication tag for tamper detection
- Base64 encoding for database storage compatibility
```

**Evidence:** `TheWatch.Shared/Security/FieldEncryptionService.cs`

### 2.2 Encryption in Transit

| Channel | Protocol | Minimum Version | Configuration | Standard |
|---------|----------|----------------|---------------|----------|
| **Client → Gateway** | TLS | 1.2 | HTTPS enforced; HSTS 365d with preload | NIST 800-171 SC-8 |
| **Gateway → Services** | TLS | 1.2 | Internal TLS (mTLS planned) | NIST 800-171 SC-8(1) |
| **Service → Database** | TLS | 1.2 | Connection-level encryption | NIST 800-53 SC-8 |
| **Service → Kafka** | PLAINTEXT | — | TLS planned (POA&M item) | NIST 800-53 SC-8 |
| **Service → Redis** | TCP | — | AUTH + TLS planned (POA&M item) | NIST 800-53 SC-8 |
| **Mobile → Gateway** | TLS | 1.2 | Certificate pinning | NIST 800-171 SC-8 |
| **SignalR WebSocket** | WSS | 1.2 | TLS-encrypted WebSocket | NIST 800-53 SC-8 |

#### Kestrel TLS Hardening

```
Enforced Settings:
- TLS 1.2 and TLS 1.3 only (SslProtocols.Tls12 | SslProtocols.Tls13)
- No TLS 1.0 or TLS 1.1
- Maximum request body: 10 MB
- Maximum request header: 32 KB
- Headers timeout: 30 seconds (Slowloris protection)
- Server header suppressed (AddServerHeader = false)
```

**Evidence:** `TheWatch.Shared/Security/WatchKestrelExtensions.cs`

### 2.3 Password Hashing

| Algorithm | Parameters | FIPS Status | Use Case |
|-----------|-----------|-------------|----------|
| **Argon2id** | Memory: configurable, Iterations: configurable | NOT FIPS-validated | Default password hashing |
| **PBKDF2-SHA512** | 600,000 iterations, 32-byte salt, 64-byte hash | FIPS 140-2 validated | FIPS fallback option |

**Migration Path:** System detects hash format and can transparently migrate from Argon2id to PBKDF2
on next authentication. Format detection via `$PBKDF2-SHA512$` prefix.

**Evidence:** `TheWatch.P5.AuthSecurity/Security/Argon2PasswordHasher.cs`, `TheWatch.Shared/Security/FipsPbkdf2PasswordHasher.cs`

### 2.4 Cryptographic Key Management

| Key Type | Algorithm | Storage | Rotation | Standard |
|----------|-----------|---------|----------|----------|
| **JWT Signing** | RSA-2048 (production) / HMAC-SHA256 (dev) | Environment variable / Key Vault (planned) | Manual (HSM-backed rotation planned) | NIST 800-53 SC-12 |
| **Field Encryption** | AES-256 | Configuration / Key Vault (planned) | Per deployment | NIST 800-53 SC-12 |
| **Password Salt** | Cryptographic random | Stored with hash | Per-password unique | NIST 800-53 SC-12 |
| **HMAC Signing** | HMAC-SHA256 | Device-derived | Per-device | NIST 800-53 SC-12 |

---

## 3. Access Controls for CUI

### 3.1 Role-Based Access Control (RBAC)

| Role | CUI Access | SP-PRIV | SP-HLTH | SP-LEI | SP-GEO |
|------|-----------|---------|---------|--------|--------|
| **Admin** | Full system access | ✅ Read/Write | ✅ Read/Write | ✅ Read/Write | ✅ Read/Write |
| **Responder** | Emergency + dispatch data | ✅ Read own | ❌ | ✅ Read/Write | ✅ Read |
| **Doctor** | Medical + health data | ✅ Read patients | ✅ Read/Write | ❌ | ❌ |
| **FamilyMember** | Family group data | ✅ Read own family | ✅ Read own family | ❌ | ✅ Read own |
| **Patient** | Own data only | ✅ Read own | ✅ Read own | ❌ | ❌ |
| **ServiceAccount** | Service-specific scope | Scoped | Scoped | Scoped | Scoped |

### 3.2 Authorization Policies

| Policy | Enforcement | Applicable Endpoints | Standard |
|--------|------------|---------------------|----------|
| **AdminOnly** | `[Authorize(Policy = "AdminOnly")]` | User management, system config, audit logs | NIST 800-171 AC-3 |
| **ResponderAccess** | `[Authorize(Policy = "ResponderAccess")]` | Emergency dispatch, incident management, surveillance | NIST 800-171 AC-3 |
| **DoctorAccess** | `[Authorize(Policy = "DoctorAccess")]` | Medical records, appointments, health data | NIST 800-171 AC-3, HIPAA |
| **Authenticated** | Fallback policy (any valid JWT) | General user operations | NIST 800-171 AC-3 |

### 3.3 IDOR Prevention

All endpoints that access user-specific data implement `CallerCanAccessUser()` checks:

- JWT subject claim compared against requested resource owner
- Admin role bypasses ownership check
- Failed checks return 403 Forbidden with audit log entry

**Evidence:** `TheWatch.Admin.RestAPI/Controllers/AuthSecurityController.cs`

---

## 4. HIPAA Compliance Controls

### 4.1 Technical Safeguards (§164.312)

| HIPAA Requirement | §Reference | Implementation | Status | Evidence |
|-------------------|------------|----------------|--------|----------|
| **Access Control** | §164.312(a)(1) | RBAC with DoctorAccess policy; MFA mandatory for Doctor role | **[IMPLEMENTED]** | RBAC configuration |
| **Unique User ID** | §164.312(a)(2)(i) | Unique user identifiers in all PHI access logs | **[IMPLEMENTED]** | JWT claims |
| **Emergency Access** | §164.312(a)(2)(ii) | Emergency override for first responders with audit trail | **[IMPLEMENTED]** | P2 VoiceEmergency |
| **Automatic Logoff** | §164.312(a)(2)(iii) | JWT 30-min expiration; session timeout | **[IMPLEMENTED]** | JWT configuration |
| **Encryption & Decryption** | §164.312(a)(2)(iv) | AES-256-GCM for PHI fields | **[IMPLEMENTED]** | `AesGcmFieldEncryptor` |
| **Audit Controls** | §164.312(b) | All PHI access logged with user, timestamp, action, resource | **[IMPLEMENTED]** | `HipaaComplianceService` |
| **Integrity Controls** | §164.312(c)(1) | HMAC-signed audit records; hash chain verification | **[IMPLEMENTED]** | Audit policy |
| **Person/Entity Auth** | §164.312(d) | MFA mandatory for PHI access roles | **[IMPLEMENTED]** | Auth policies |
| **Transmission Security** | §164.312(e)(1) | TLS 1.2+ enforced; HSTS enabled | **[IMPLEMENTED]** | Kestrel config |
| **Encryption** | §164.312(e)(2)(ii) | TLS 1.2/1.3 for all PHI transmission | **[IMPLEMENTED]** | Kestrel config |

### 4.2 PHI-Specific Controls

| Control | Implementation | Standard |
|---------|---------------|----------|
| **Safe Harbor De-identification** | `HipaaComplianceService` implements Safe Harbor 18-identifier redaction | HIPAA §164.514(b) |
| **Minimum Necessary** | API endpoints return only requested fields; no bulk PHI export | HIPAA §164.502(b) |
| **BAA Tracking** | Business Associate Agreement status tracked per service | HIPAA §164.502(e) |
| **PHI Access Logging** | Dedicated PHI access event logging separate from general audit | HIPAA §164.312(b) |
| **PII Redaction in Logs** | `PiiRedactionMiddleware` masks SSN, phone, email, credit card in log output | NIST 800-53 AU-3 |

**Evidence:** `TheWatch.Shared/Compliance/HipaaComplianceService.cs`, `TheWatch.Shared/Security/PiiRedactionMiddleware.cs`

---

## 5. CUI Marking and Handling

### 5.1 CUI Marking Middleware

TheWatch applies CUI markings to HTTP responses via the `CuiMarkingMiddleware`:

| Route Pattern | CUI Marking Applied | Response Header |
|---------------|---------------------|----------------|
| `/api/auth/*` | CUI // SP-PRIV | `X-CUI-Category: SP-PRIV` |
| `/api/family/*`, `/api/health/*` | CUI // SP-HLTH | `X-CUI-Category: SP-HLTH` |
| `/api/emergency/*`, `/api/dispatch/*`, `/api/surveillance/*` | CUI // SP-LEI | `X-CUI-Category: SP-LEI` |
| `/api/geospatial/*`, `/api/location/*` | CUI // SP-GEO | `X-CUI-Category: SP-GEO` |
| All other `/api/*` routes | CUI // SP-BASIC | `X-CUI-Category: SP-BASIC` |

**Evidence:** `TheWatch.Shared/Security/CuiMarkingMiddleware.cs`

### 5.2 CUI Handling Procedures

| Procedure | Implementation | Standard |
|-----------|---------------|----------|
| **Marking** | Automated HTTP header marking per route | 32 CFR Part 2002 |
| **Safeguarding** | Encryption at rest + transit; RBAC | NIST 800-171 MP-4 |
| **Dissemination** | Role-based access; no public CUI endpoints | NIST 800-171 AC-22 |
| **Decontrol** | Data retention policies; automated cleanup (30-day footage) | 32 CFR Part 2002 |
| **Destruction** | Stateless containers; secure database deletion | NIST 800-88 |

---

## 6. Data Lifecycle Management

### 6.1 Retention Policies

| Data Type | Retention Period | Disposal Method | Standard |
|-----------|-----------------|----------------|----------|
| **Audit Logs (Security)** | 1 year | Secure deletion | NIST 800-171 AU-11 |
| **Audit Logs (General)** | 90 days | Secure deletion | NIST 800-171 AU-11 |
| **Surveillance Footage** | 30 days (stale cleanup) | Automated deletion | Organization policy |
| **Build Artifacts** | 1 day | GitHub artifact expiry | Organization policy |
| **Compliance Reports** | 30 days | GitHub artifact expiry | Organization policy |
| **SBOM Archives** | 90 days | GitHub artifact expiry | EO 14028 |
| **Forensic Evidence** | 90 days minimum | Preserved for investigation | NIST 800-171 IR-4 |
| **Medical Records** | Per HIPAA requirements | HIPAA-compliant destruction | HIPAA §164.530(j) |

### 6.2 Evidence Chain of Custody

For surveillance and evidence data, TheWatch implements a complete chain-of-custody system:

| Step | Action | Integrity Control | Standard |
|------|--------|-------------------|----------|
| **1. Collection** | File captured on mobile device | SHA-256 hash of file content | NIST 800-53 AU-10 |
| **2. Registration** | Custody record created | HMAC-SHA256 device signature | NIST 800-53 AU-10(2) |
| **3. Chain Link** | Record linked to previous | Previous record hash included | NIST 800-53 AU-9 |
| **4. Metadata** | Location + user + device captured | GPS + user ID + device fingerprint | NIST 800-53 AU-3(1) |
| **5. Upload** | Evidence submitted to server | TLS transport + hash verification | NIST 800-53 SC-8 |
| **6. Analysis** | ML/ONNX object detection | Automated processing pipeline | NIST 800-53 AU-6 |
| **7. Archival** | Evidence stored with custody trail | Complete audit trail preserved | NIST 800-53 AU-11 |

**Evidence:** `TheWatch.Mobile/Services/ChainOfCustodyService.cs`, `TheWatch.Mobile/Services/EvidenceMetadataService.cs`

---

## 7. Privacy Controls

### 7.1 PII Handling

| Control | Implementation | Standard |
|---------|---------------|----------|
| **Collection Minimization** | Only required fields collected; optional fields clearly marked | NIST 800-53 AP-1 |
| **Consent Management** | EULA acceptance with versioning and IP logging | NIST 800-53 IP-1 |
| **Access Rights** | Users can view their own data via Patient role | NIST 800-53 IP-2 |
| **Log Redaction** | SSN, phone, email, credit card patterns automatically masked | NIST 800-53 AU-3 |
| **Biometric Gate** | Fingerprint/face required before accessing sensitive data on mobile | NIST 800-53 IA-12 |
| **Location Consent** | GPS tracking requires explicit opt-in consent | Organization policy |

### 7.2 PII Redaction Patterns

The `PiiRedactionMiddleware` applies the following redaction rules to log output:

| Pattern | Example | Redacted As |
|---------|---------|-------------|
| Social Security Number | 123-45-6789 | `***-**-****` |
| Phone Number | (555) 123-4567 | `(***) ***-****` |
| Email Address | user@example.com | `***@***.***` |
| Credit Card | 4111-1111-1111-1111 | `****-****-****-****` |

**Evidence:** `TheWatch.Shared/Security/PiiRedactionMiddleware.cs`

---

## 8. Data Flow Diagrams

### 8.1 CUI Data Flow — Emergency Response

```
┌─────────┐    TLS 1.2+     ┌──────────────┐    JWT Auth     ┌─────────────┐
│ Citizen  │ ──────────────► │ P1 CoreGateway│ ─────────────► │ P2 Voice    │
│ (Mobile) │                 │ Rate Limit    │                │ Emergency   │
└─────────┘                  │ CORS Check    │                │ [SP-LEI]    │
                             └──────────────┘                └──────┬──────┘
                                                                    │ Kafka
                                                                    ▼
┌─────────────┐    TLS 1.2+    ┌──────────────┐    SignalR    ┌──────────────┐
│ P11 Surveil-│ ◄──────────── │ Event Bus    │ ─────────────►│ P6 First     │
│ lance       │                │ (Kafka)      │               │ Responder    │
│ [SP-LEI]    │                └──────────────┘               │ [SP-LEI]     │
└─────────────┘                                               └──────────────┘
                                                                    │
                                                                    ▼
                                                              ┌──────────────┐
                                                              │ Geospatial   │
                                                              │ [SP-GEO]     │
                                                              └──────────────┘
```

### 8.2 CUI Data Flow — Health Monitoring

```
┌─────────────┐    TLS 1.2+     ┌──────────────┐    JWT Auth    ┌──────────────┐
│ Wearable    │ ──────────────► │ P1 CoreGateway│ ────────────► │ P4 Wearable  │
│ Device      │                 │               │               │ [SP-PRIV]    │
└─────────────┘                 └──────────────┘               └──────┬───────┘
                                                                     │
                                                                     ▼
┌──────────────┐   AES-256-GCM   ┌──────────────┐    HIPAA      ┌──────────────┐
│ P9 Doctor    │ ◄──────────────  │ SQL Server   │ ◄──────────── │ P7 Family    │
│ Services     │    encrypted     │ [SP-HLTH]    │   compliant   │ Health       │
│ [SP-HLTH]    │    at rest       │              │               │ [SP-HLTH]    │
└──────────────┘                  └──────────────┘               └──────────────┘
```

---

*This Data Protection Plan ensures that all CUI, PHI, and PII processed by TheWatch
is classified, encrypted, access-controlled, and audited in accordance with DoD and
federal requirements.*
