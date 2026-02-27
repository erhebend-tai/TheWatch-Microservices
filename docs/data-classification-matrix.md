# TheWatch — CUI Data Classification Matrix

> NIST 800-171 SC-28, MP-4 | CMMC Level 2 | Effective: 2026-02-26

## Classification Categories

| Category | CUI Marking | Description |
|----------|------------|-------------|
| CUI//SP-PRIV | Privacy | PII: names, emails, phones, addresses, DOB |
| CUI//SP-HLTH | Health | PHI: vital readings, medical alerts, prescriptions, doctor notes |
| CUI//SP-LEI | Law Enforcement | Incident details, evidence, surveillance footage, dispatch records |
| CUI//SP-GEO | Geolocation | GPS coordinates of incidents, responders, shelters |
| CUI//SP-BASIC | Basic CUI | System configuration, audit logs, user roles |

## Entity-Level Classification

| Service | Entity | Fields | CUI Category | Encryption |
|---------|--------|--------|-------------|------------|
| P5 Auth | WatchUser | Email, PhoneNumber | SP-PRIV | Always Encrypted |
| P5 Auth | WatchUser | PasswordHash | SP-PRIV | Argon2id/PBKDF2 |
| P2 Emergency | Incident | ReporterPhone, Location | SP-LEI + SP-GEO | Always Encrypted |
| P2 Emergency | Evidence | All metadata | SP-LEI | Always Encrypted |
| P6 Responder | Responder | Phone, BadgeNumber | SP-PRIV + SP-LEI | Always Encrypted |
| P6 Responder | CheckIn | Latitude, Longitude | SP-GEO | Field Encryption |
| P7 Family | FamilyMember | DateOfBirth | SP-PRIV | Always Encrypted |
| P7 Family | VitalReading | All fields | SP-HLTH | Always Encrypted |
| P7 Family | MedicalAlert | Description | SP-HLTH | Always Encrypted |
| P8 Disaster | Shelter | Location | SP-GEO | Field Encryption |
| P9 Doctor | DoctorProfile | LicenseNumber | SP-PRIV | Always Encrypted |
| P9 Doctor | Appointment | Notes | SP-HLTH | Always Encrypted |
| P11 Surveillance | Camera | Location, StreamUrl | SP-LEI + SP-GEO | Field Encryption |
| P11 Surveillance | Footage | All metadata | SP-LEI | Always Encrypted |
| Geospatial | All entities | Coordinates | SP-GEO | PostGIS + pgcrypto |

## Protection Requirements by Category

| Category | At Rest | In Transit | Access Control | Audit |
|----------|---------|-----------|----------------|-------|
| SP-PRIV | AES-256 (TDE + column) | TLS 1.2+ | RBAC | All access logged |
| SP-HLTH | AES-256 (TDE + column) | TLS 1.2+ | RBAC + MFA | All access logged |
| SP-LEI | AES-256 (TDE + column) | TLS 1.2+ mTLS | RBAC + MFA | All access logged + signed |
| SP-GEO | AES-256 (TDE + column) | TLS 1.2+ | RBAC | All access logged |
| SP-BASIC | AES-256 (TDE) | TLS 1.2+ | RBAC | All access logged |
