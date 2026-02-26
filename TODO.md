# TheWatch — Master TODO List

> 150 items organized by stage. Status: `[ ]` pending, `[x]` done, `[~]` in progress.
> See `ROADMAP.md` for full stage descriptions.

---

## Stage 5: Database Layer (Items 1–25)

### 5A. Entity Framework Core Integration
- [x] 1. Add `Microsoft.EntityFrameworkCore.SqlServer` to all 10 service .csproj files
- [x] 2. Add `Microsoft.EntityFrameworkCore.Tools` for migrations CLI
- [x] 3. Create `WatchCoreDbContext` for P1 CoreGateway with UserProfile + PlatformConfig entities
- [x] 4. Create `VoiceEmergencyDbContext` for P2 with Incident + Dispatch entities
- [x] 5. Create `MeshNetworkDbContext` for P3 with MeshNode + Message + Channel entities
- [x] 6. Create `WearableDbContext` for P4 with Device + HeartbeatRecord entities
- [x] 7. Create `AuthSecurityDbContext` for P5 with User + RefreshToken + Role entities
- [x] 8. Create `FirstResponderDbContext` for P6 with Responder + CheckIn entities
- [x] 9. Create `FamilyHealthDbContext` for P7 with FamilyGroup + Member + CheckIn + Vital + Alert entities
- [x] 10. Create `DisasterReliefDbContext` for P8 with DisasterEvent + Shelter + Resource + EvacRoute entities
- [x] 11. Create `DoctorServicesDbContext` for P9 with DoctorProfile + Appointment + TelehealthSession entities
- [x] 12. Create `GamificationDbContext` for P10 with Player + Badge + Challenge + Leaderboard entities
- [x] 13. Add `IEntityTypeConfiguration<T>` for all entities (indexes, constraints, value converters)
- [x] 14. Create initial EF migration per service (`dotnet ef migrations add InitialCreate`)

### 5B. SQL Server Schema
- [ ] 15. Map P0 `UniversalMeasurementsDB` tables from `00_Schema.sql`–`10_Devices.sql` to EF seed data
- [ ] 16. Adapt P2 DDL from `Watch_Program_02_VoiceEmergency.md` to EF entity configs
- [ ] 17. Adapt P3 DDL from `Watch-MeshNetwork.md` (19 tables) to EF entity configs
- [ ] 18. Adapt P4 DDL from `Program4_Watch_Wearable.sql` to EF entity configs
- [ ] 19. Adapt P5 DDL from `Watch-AuthSecurity/` (4 parts) to EF entity configs
- [ ] 20. Adapt P8 DDL from `Watch-DisasterRelief/` to EF entity configs (18 tables)
- [ ] 21. Create seed data scripts for development (test users, sample incidents, demo families)

### 5C. Repository Pattern + Aspire
- [x] 22. Extract `IRepository<T>` generic interface from existing service interfaces
- [x] 23. Implement EF-backed repository classes for all 10 services
- [x] 24. Add SQL Server container resource to Aspire AppHost (`AddSqlServer`)
- [x] 25. Configure connection strings via Aspire service discovery for all services

---

## Stage 6: Real-Time & Events (Items 26–45)

### 6A. SignalR
- [x] 26. Add `Microsoft.AspNetCore.SignalR` to P2, P6, P7 services (built into ASP.NET Core; CORS updated for SignalR)
- [x] 27. Create `IncidentHub` in P2 — real-time incident status feed (via SignalRGenerator)
- [x] 28. Create `DispatchHub` in P2 — responder dispatch status streaming (via SignalRGenerator)
- [x] 29. Create `ResponderHub` in P6 — live responder GPS streaming (via SignalRGenerator)
- [x] 30. Create `CheckInHub` in P7 — check-in notifications to family members (via SignalRGenerator)
- [x] 31. Add SignalR client to MAUI app (`HubConnectionBuilder`)
- [x] 32. Add SignalR client to Dashboard for live incident/health updates
- [x] 33. Implement reconnection logic with exponential backoff in both clients

### 6B. Kafka Event Bus
- [x] 34. Add Kafka container to Aspire AppHost
- [x] 35. Create `IEventPublisher` interface in TheWatch.Shared
- [x] 36. Implement Kafka publisher with `Confluent.Kafka` producer
- [x] 37. Publish `IncidentCreated` event from P2 on incident creation
- [x] 38. Publish `DispatchRequested` event from P2 on dispatch creation
- [x] 39. Consume `IncidentCreated` in P6 to auto-query nearby responders
- [x] 40. Consume `DispatchRequested` in P3 to broadcast mesh alert
- [x] 41. Implement dead letter queue for failed event processing
- [x] 42. Add event sourcing audit log for all domain events

### 6C. Push Notifications
- [x] 43. Add Firebase Admin SDK to P2 for FCM push notifications
- [x] 44. Create `NotificationService` in TheWatch.Shared for cross-service notifications
- [x] 45. Implement MAUI push notification handlers (Android `FirebaseMessagingService`, iOS delegate)

---

## Stage 7: Geospatial Engine (Items 46–60)

### 7A. PostGIS
- [x] 46. Add PostgreSQL + PostGIS container to Aspire AppHost
- [x] 47. Deploy `11_Geospatial.sql` schema (28 tables, 5 schemas, 12 spatial functions)
- [x] 48. Create `GeospatialDbContext` with Npgsql + NetTopologySuite for geometry types
- [x] 49. Create `IGeospatialService` interface (nearest-N, within-radius, route calculation)
- [x] 50. Implement `PostGisGeospatialService` with `ST_DWithin`, `ST_Distance`, `ST_MakePoint`

### 7B. Mapping Service
- [x] 51. Create `TheWatch.Geospatial` microservice project (new project in solution)
- [x] 52. Implement nearest-N responder query with expanding radius algorithm
- [x] 53. Implement incident zone polygon creation and querying
- [x] 54. Implement evacuation route calculation for P8 disaster scenarios
- [x] 55. Implement geofencing for P7 family check-in zones
- [x] 56. Add geospatial service to Aspire AppHost orchestration

### 7C. Client Maps
- [x] 57. Add Leaflet.js map library to MAUI project (via JS interop — more suitable for Blazor Hybrid than native Maps control)
- [x] 58. Create `MapPage.razor` with responder placement pins and incident zones
- [x] 59. Implement real-time responder movement on map (SignalR + map updates)
- [x] 60. Add Leaflet map component to Dashboard for incident overview

---

## Stage 8: Auth & Security Hardening (Items 61–80)

### 8A. Identity Provider
- [ ] 61. Replace P5 in-memory user store with EF-backed `AspNetCore.Identity`
- [ ] 62. Implement Argon2id password hashing (replace PBKDF2)
- [ ] 63. Implement TOTP-based MFA (Google Authenticator compatible)
- [ ] 64. Implement SMS MFA via Azure Communication Services
- [ ] 65. Implement email magic link authentication
- [ ] 66. Implement biometric passkey authentication (WebAuthn/FIDO2)
- [ ] 67. Implement JWT sliding window expiration with configurable lifetime
- [ ] 68. Implement refresh token rotation with automatic revocation of old tokens
- [ ] 69. Add EULA versioning and acceptance tracking (from P5 spec)
- [ ] 70. Add onboarding tutorial progress tracking (from P5 spec)

### 8B. Authorization
- [ ] 71. Implement RBAC: Admin, Responder, FamilyMember, Doctor, Patient roles
- [ ] 72. Add claims-based authorization policies to all service endpoints
- [ ] 73. Create API key authentication for inter-service communication
- [ ] 74. Implement rate limiting middleware (`Microsoft.AspNetCore.RateLimiting`)
- [ ] 75. Add IP-based throttling for login/register endpoints

### 8C. Security Monitoring
- [ ] 76. Implement audit logging for all authentication events (login, logout, MFA, token refresh)
- [ ] 77. Implement brute force detection with progressive account lockout
- [ ] 78. Implement device trust scoring based on login history and location
- [ ] 79. Add STRIDE threat model checks to security monitoring agent
- [ ] 80. Add MITRE ATT&CK technique detection rules

---

## Stage 9: MAUI Mobile Production (Items 81–105)

### 9A. Platform Speech Recognition
- [ ] 81. Implement Android `SpeechRecognizer` with continuous recognition in `SpeechListenerService`
- [ ] 82. Implement iOS `SFSpeechRecognizer` with on-device model for privacy
- [ ] 83. Implement Windows `SpeechRecognizer` for desktop testing
- [ ] 84. Add microphone + speech recognition runtime permission requests
- [ ] 85. Implement battery-aware throttling (reduce polling when battery < 20%)
- [ ] 86. Implement background service for perpetual listening (Android foreground service, iOS background task)

### 9B. Offline Resilience
- [ ] 87. Add SQLite local database to MAUI project for offline storage
- [ ] 88. Implement offline queue: buffer API requests when no connectivity
- [ ] 89. Implement sync engine: reconcile local SQLite with server on reconnect
- [ ] 90. Implement conflict resolution: server-wins with local notification
- [ ] 91. Cache user profile, family data, and recent incidents locally
- [ ] 92. Offline emergency mode: activate P3 mesh network fallback when no internet

### 9C. Native Features
- [ ] 93. Implement camera integration for evidence photo/video capture
- [ ] 94. Implement high-accuracy GPS mode during active emergencies
- [ ] 95. Implement haptic feedback patterns (SOS confirm, alert received, check-in reminder)
- [ ] 96. Implement biometric authentication gate (fingerprint/face before app access)
- [ ] 97. Implement background location tracking with user consent flow
- [ ] 98. Add deep link handling for push notification tap → specific page

### 9D. Evidence Collection
- [ ] 99. Create `EvidencePage.razor` for video recording with GPS/timestamp overlay
- [ ] 100. Implement photo capture with automatic metadata (GPS, time, device ID)
- [ ] 101. Implement speech-to-text incident reporting in SITREP format
- [ ] 102. Implement chain-of-custody: SHA-256 hash + timestamp + device signature per artifact
- [ ] 103. Implement client-side content moderation (nudity detection before upload)
- [ ] 104. Create evidence upload queue with retry and progress tracking
- [ ] 105. Create `IncidentReportPage.razor` — post-incident questionnaire (SITREP framework)

---

## Stage 10: Containerization & CI/CD (Items 106–120)

### 10A. Docker
- [ ] 106. Create multi-stage `Dockerfile` for each of the 10 microservices
- [ ] 107. Create `Dockerfile` for Dashboard (Blazor Server)
- [ ] 108. Create `docker-compose.yml` for full local development stack (services + SQL + Redis + Kafka)
- [ ] 109. Create `docker-compose.override.yml` for development-specific config (ports, volumes)
- [ ] 110. Add `.dockerignore` files to prevent including bin/obj/node_modules

### 10B. Kubernetes / Helm
- [ ] 111. Create Helm chart template with deployment, service, ingress per microservice
- [ ] 112. Configure HPA for P2 VoiceEmergency and P6 FirstResponder (emergency surge scaling)
- [ ] 113. Create ConfigMaps for service configuration (feature flags, endpoints)
- [ ] 114. Create Kubernetes Secrets for database credentials and JWT signing keys
- [ ] 115. Configure Ingress Controller with TLS termination and path-based routing

### 10C. CI/CD
- [ ] 116. Create GitHub Actions workflow: build + test on every PR
- [ ] 117. Create GitHub Actions workflow: build Docker images and push to Azure Container Registry
- [ ] 118. Create GitHub Actions workflow: deploy to staging on merge to `develop`
- [ ] 119. Create GitHub Actions workflow: deploy to production on merge to `main` (manual approval gate)
- [ ] 120. Add CodeQL security scanning and dependency review to PR workflow

---

## Stage 11: Cloud Deployment (Items 121–140)

### 11A. Azure Infrastructure
- [ ] 121. Create Terraform/Bicep module for Azure SQL Database (10 databases, geo-replicated)
- [ ] 122. Create Terraform/Bicep module for Azure Cosmos DB (MongoDB API, multi-region writes)
- [ ] 123. Create Terraform/Bicep module for Azure Redis Cache (session store, rate limiting)
- [ ] 124. Create Terraform/Bicep module for Azure Service Bus (event queues)
- [ ] 125. Create Terraform/Bicep module for Azure Key Vault (secrets, certificates, JWT signing keys)
- [ ] 126. Create Terraform/Bicep module for Azure Container Apps or AKS cluster
- [ ] 127. Create Terraform/Bicep module for Azure Storage (evidence blobs)

### 11B. Azure Service Integration
- [ ] 128. Integrate Azure SignalR Service (managed, replaces self-hosted SignalR)
- [ ] 129. Integrate Azure Maps for geospatial (cloud alternative to self-hosted PostGIS)
- [ ] 130. Integrate Azure Communication Services for SMS/email notifications
- [ ] 131. Integrate Application Insights for distributed tracing and APM

### 11C. GCP Services
- [ ] 132. Integrate Google Speech-to-Text API for P2 voice recognition (server-side processing)
- [ ] 133. Integrate Google Vision API for evidence analysis and content moderation
- [ ] 134. Integrate Firebase Cloud Messaging for push notifications
- [ ] 135. Integrate Google Healthcare API (FHIR) for P7/P9 health data interoperability

### 11D. Cloudflare Edge
- [ ] 136. Configure Cloudflare CDN for static assets (MAUI WebView, Dashboard)
- [ ] 137. Deploy Cloudflare Workers for edge authentication validation
- [ ] 138. Configure Cloudflare WAF rules for API protection
- [ ] 139. Set up Cloudflare Zero Trust for admin/dashboard access
- [ ] 140. Configure Argo Tunnels for secure service exposure without public IPs

---

## Stage 12: Advanced Features & Compliance (Items 141–150)

### 12A. ML/AI
- [ ] 141. Train gunshot detection audio classifier for P2 active shooter scenarios
- [ ] 142. Implement fall detection from wearable accelerometer data stream (P4)
- [ ] 143. Implement vital sign anomaly detection with configurable thresholds (P7)
- [ ] 144. Implement responder dispatch optimization (minimize response time across geography)

### 12B. Compliance
- [ ] 145. Implement HIPAA-compliant data handling for P7/P9 health records (encryption at rest + transit, access logging, BAA requirements)
- [ ] 146. Implement COPPA compliance for P7 child data (parental consent, data minimization)
- [ ] 147. Implement GDPR right-to-erasure across all services (cascade delete with audit)
- [ ] 148. Implement SOX-expanded audit framework from memorandum (quarterly reporting, signed attestations)

### 12C. Graph & Observability
- [ ] 149. Deploy `Watch-GraphDB.sql` graph tables (node/edge) for social graph and incident correlation
- [ ] 150. Wire 10 monitoring agents into CI/CD with Prometheus metrics, Grafana dashboards, and PagerDuty alerting

---

## Quick Reference

| Range | Stage | Theme |
|-------|-------|-------|
| 1–25 | 5 | Database (EF Core + SQL Server) |
| 26–45 | 6 | Real-Time (SignalR + Kafka + Push) |
| 46–60 | 7 | Geospatial (PostGIS + Maps) |
| 61–80 | 8 | Auth & Security (P5 Full + RBAC) |
| 81–105 | 9 | MAUI Production (Speech, Offline, Evidence) |
| 106–120 | 10 | DevOps (Docker + K8s + CI/CD) |
| 121–140 | 11 | Cloud (Azure + GCP + Cloudflare) |
| 141–150 | 12 | Advanced (ML, Compliance, Graph, Observability) |

---

*Last updated: 2026-02-26 (Session 13)*
