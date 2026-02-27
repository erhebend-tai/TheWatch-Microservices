# TheWatch — Access Control Policy

> NIST 800-171 AC-1 | CMMC Level 2 | Effective: 2026-02-26

## 1. Purpose
This policy establishes access control requirements for the TheWatch platform to protect Controlled Unclassified Information (CUI) in accordance with NIST SP 800-171 Rev 2.

## 2. Scope
All TheWatch microservices, databases, infrastructure, and user-facing applications.

## 3. Roles and Responsibilities

| Role | Access Level | Description |
|------|-------------|-------------|
| Admin | Full | System administrators; manage users, roles, configuration |
| Responder | Elevated | First responders; incident management, dispatch, location tracking |
| Doctor | Elevated | Medical professionals; patient records, telehealth, prescriptions |
| FamilyMember | Standard | Family group management, health check-ins, vital monitoring |
| Patient | Standard | Personal health data, appointments, emergency activation |
| ServiceAccount | Inter-service | Automated service-to-service communication (API key auth) |

## 4. Access Control Requirements

### 4.1 Authentication (NIST IA-2)
- All users must authenticate via JWT Bearer tokens issued by P5 AuthSecurity
- Multi-factor authentication required for Admin and Responder roles
- CAC/PIV smart card authentication supported for DoD environments
- Password requirements: 15-character minimum, complexity enforced, 60-day rotation

### 4.2 Authorization (NIST AC-3)
- Role-Based Access Control (RBAC) enforced via ASP.NET authorization policies
- Principle of least privilege: users receive minimum permissions needed
- Service-to-service communication requires API key or mTLS certificate

### 4.3 Account Management (NIST AC-2)
- New accounts created by Admin only via `/api/auth/register`
- Account lockout after 3 consecutive failed login attempts
- Disabled accounts within 24 hours of personnel termination
- Quarterly access review by Admin to verify all active accounts

### 4.4 Session Management (NIST AC-12)
- Access token lifetime: 30 minutes maximum
- Refresh token lifetime: 8 hours maximum
- Idle timeout: 15 minutes
- Maximum 5 concurrent sessions per user

### 4.5 Remote Access (NIST AC-17)
- All remote access via TLS 1.2 or TLS 1.3 only
- VPN or Zero Trust network required for administrative access
- MFA required for all remote sessions

## 5. Review Schedule
This policy is reviewed annually and updated as needed when security requirements change.

## 6. Enforcement
Violations of this policy may result in immediate access revocation and disciplinary action.
