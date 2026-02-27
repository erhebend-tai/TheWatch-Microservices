# Stream 2: AUTH — Stage 8 (TODO Items 61-80)

You are working in a git worktree of TheWatch microservices solution. Your task is to implement full authentication, authorization, and security monitoring for the P5 AuthSecurity service.

## YOUR ASSIGNED TODO ITEMS

### 8A. Identity Provider (61-70)
61. Replace P5 in-memory user store with EF-backed AspNetCore.Identity
62. Implement Argon2id password hashing (replace PBKDF2)
63. Implement TOTP-based MFA (Google Authenticator compatible)
64. Implement SMS MFA via Azure Communication Services
65. Implement email magic link authentication
66. Implement biometric passkey authentication (WebAuthn/FIDO2)
67. Implement JWT sliding window expiration with configurable lifetime
68. Implement refresh token rotation with automatic revocation of old tokens
69. Add EULA versioning and acceptance tracking
70. Add onboarding tutorial progress tracking

### 8B. Authorization (71-75)
71. Implement RBAC: Admin, Responder, FamilyMember, Doctor, Patient roles
72. Add claims-based authorization policies to all service endpoints
73. Create API key authentication for inter-service communication
74. Implement rate limiting middleware (Microsoft.AspNetCore.RateLimiting)
75. Add IP-based throttling for login/register endpoints

### 8C. Security Monitoring (76-80)
76. Implement audit logging for all authentication events
77. Implement brute force detection with progressive account lockout
78. Implement device trust scoring based on login history and location
79. Add STRIDE threat model checks to security monitoring agent
80. Add MITRE ATT&CK technique detection rules

## FILES YOU MAY MODIFY (your exclusive scope)

- `TheWatch.P5.AuthSecurity/Services/` — create/modify service classes
- `TheWatch.P5.AuthSecurity/Middleware/` — create middleware classes
- `TheWatch.P5.AuthSecurity/Security/` — create new directory for security features
- `TheWatch.P5.AuthSecurity/Program.cs` — wire new services and middleware
- `TheWatch.P5.AuthSecurity/TheWatch.P5.AuthSecurity.csproj` — add NuGet packages
- `TheWatch.Shared/Auth/` — create new directory for shared auth constants/policies
- `TheWatch.P5.AuthSecurity.Tests/` — add new tests

## FILES YOU MUST NOT TOUCH

- `TheWatch.P5.AuthSecurity/Auth/AuthModels.cs` — entity models (owned by Stream 1)
- `TheWatch.P5.AuthSecurity/Data/` — database context (owned by Stream 1)
- Any other `TheWatch.P*/` service directory
- `TheWatch.Mobile/`
- `TheWatch.Dashboard/`
- `TheWatch.Aspire.AppHost/`
- `TheWatch.Shared/TheWatch.Shared.csproj` — do NOT add packages here
- `TheWatch.Shared/Events/`, `TheWatch.Shared/Notifications/`, `TheWatch.Shared/Contracts/`
- `docker/`, `helm/`, `.github/`, `infra/`

## CURRENT STATE OF P5

Read `TheWatch.P5.AuthSecurity/Program.cs` and `TheWatch.P5.AuthSecurity/Services/AuthService.cs` first. Currently:
- Uses ConcurrentDictionary in-memory store
- PBKDF2 password hashing
- Basic JWT generation
- Endpoints: POST /api/auth/register, /login, /refresh, GET /me, /users
- No MFA, no rate limiting, no audit logging

## IMPLEMENTATION GUIDANCE

### Packages to add to P5 .csproj:
- `Isopoh.Cryptography.Argon2` (Argon2id)
- `Otp.NET` (TOTP)
- `Fido2.Models` + `Fido2` (WebAuthn/FIDO2 passkeys)
- `Microsoft.AspNetCore.RateLimiting` (built into ASP.NET Core 8+)

### Shared Auth Constants (in TheWatch.Shared/Auth/):
```csharp
// TheWatch.Shared/Auth/WatchRoles.cs
public static class WatchRoles
{
    public const string Admin = "Admin";
    public const string Responder = "Responder";
    public const string FamilyMember = "FamilyMember";
    public const string Doctor = "Doctor";
    public const string Patient = "Patient";
}

// TheWatch.Shared/Auth/WatchPolicies.cs
public static class WatchPolicies
{
    public const string RequireAdmin = "RequireAdmin";
    public const string RequireResponder = "RequireResponder";
    // ...
}
```

### Rate Limiting Pattern:
```csharp
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("auth", o => { o.PermitLimit = 5; o.Window = TimeSpan.FromMinutes(1); });
    options.AddSlidingWindowLimiter("api", o => { o.PermitLimit = 100; o.Window = TimeSpan.FromMinutes(1); o.SegmentsPerWindow = 4; });
});
```

### API Key Auth for Inter-Service:
Create an `ApiKeyAuthenticationHandler` in `TheWatch.Shared/Auth/` that validates X-Api-Key headers.

### STRIDE Threat Model:
Implement as a service that logs and categorizes security events by STRIDE categories: Spoofing, Tampering, Repudiation, Information Disclosure, Denial of Service, Elevation of Privilege.

### MITRE ATT&CK Detection:
Map auth events to technique IDs: T1078 (Valid Accounts), T1110 (Brute Force), T1098 (Account Manipulation), T1556 (Modify Authentication Process).

## WHEN DONE

Commit all changes with message:
```
feat(auth): implement full auth stack — Identity, MFA, RBAC, rate limiting, security monitoring

Items 61-80: Argon2id, TOTP/SMS/passkey MFA, magic link, JWT sliding window,
refresh token rotation, RBAC policies, API key auth, rate limiting,
audit logging, brute force detection, device trust, STRIDE/MITRE
```
