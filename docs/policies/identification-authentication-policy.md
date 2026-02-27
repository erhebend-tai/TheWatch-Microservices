# TheWatch — Identification & Authentication Policy

> NIST 800-171 IA-1 | CMMC Level 2 | DISA STIG V-222524-546 | Effective: 2026-02-26

## 1. Purpose
Establishes requirements for user identification and authentication to protect CUI.

## 2. Password Requirements (STIG V-222536-546)

| Requirement | Value | STIG Reference |
|------------|-------|----------------|
| Minimum length | 15 characters | V-222536 |
| Uppercase required | Yes | V-222537 |
| Lowercase required | Yes | V-222538 |
| Digit required | Yes | V-222539 |
| Special character required | Yes | V-222540 |
| Maximum age | 60 days | V-222545 |
| Minimum age | 24 hours | V-222544 |
| Password history | 5 generations | V-222546 |
| Character change delta | 8 positions | V-222541 |
| Max failed attempts | 3 | V-222432 |
| Lockout duration | Progressive (15m, 1h, 24h, admin unlock) | V-222432 |

## 3. Multi-Factor Authentication (NIST IA-2)

| Role | MFA Requirement | Methods |
|------|----------------|---------|
| Admin | Mandatory | TOTP, FIDO2/Passkey, CAC/PIV |
| Responder | Mandatory | TOTP, SMS, FIDO2/Passkey |
| Doctor | Mandatory | TOTP, FIDO2/Passkey |
| FamilyMember | Encouraged | TOTP, SMS, Magic Link |
| Patient | Encouraged | TOTP, SMS, Magic Link |

## 4. Service Account Management
- API keys rotated every 90 days
- mTLS certificates for inter-service communication
- No interactive login for service accounts
- Separate credentials per environment (dev/staging/production)

## 5. CAC/PIV Authentication (STIG V-222524)
- x.509 certificate authentication supported
- Validated against DoD PKI certificate chain
- Certificate subject mapped to user identity
- Certificate revocation checked via OCSP/CRL
