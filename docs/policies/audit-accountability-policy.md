# TheWatch — Audit & Accountability Policy

> NIST 800-171 AU-1 | CMMC Level 2 | Effective: 2026-02-26

## 1. Auditable Events
All of the following events are logged with: timestamp (UTC), user identity, source IP, event type, success/failure, and relevant details.

- Authentication events (login, logout, failed login, lockout, MFA)
- Authorization failures (403 responses)
- Account management (create, modify, disable, delete)
- Role changes
- Password changes
- Configuration changes
- CUI data access (read, create, update, delete)
- Administrative actions
- System errors (5xx responses)

## 2. Log Retention

| Log Type | Retention Period | Storage |
|----------|-----------------|---------|
| Security audit events | 1 year | Immutable storage (S3 Object Lock / Azure Immutable Blob) |
| Authentication events | 1 year | SIEM + immutable storage |
| General application logs | 90 days | SIEM |
| Health check logs | 7 days | Local |

## 3. Log Integrity (NIST AU-9)
- Audit logs protected by HMAC-SHA256 signing
- Each log entry chained to previous entry (tamper detection)
- Logs forwarded to centralized SIEM within 60 seconds
- Write access to audit logs restricted to system service accounts only

## 4. Log Review
- Automated: Real-time correlation rules in SIEM for attack patterns
- Weekly: DevSecOps team reviews SIEM alerts and trends
- Monthly: Security lead reviews audit summary report
- Quarterly: Formal audit log review documented in POA&M
