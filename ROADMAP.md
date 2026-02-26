# TheWatch — Project Roadmap

> **Origin**: 2022-08-11 DoD Memorandum — Sub-2-second citizen emergency response & deterrence.
> **Constraint**: The core mission remains the center of gravity. Everything serves it.

---

## What Exists Today (Sessions 1–7)

### Solution: 27 .NET 10 Projects

| Layer | Projects | Status |
|-------|----------|--------|
| **Microservices** | P1 CoreGateway, P2 VoiceEmergency, P3 MeshNetwork, P4 Wearable, P5 AuthSecurity, P6 FirstResponder, P7 FamilyHealth, P8 DisasterRelief, P9 DoctorServices, P10 Gamification | Real endpoints, in-memory stores, Hangfire Pro jobs |
| **Tests** | 10 test projects (P1–P10) + Mobile.Tests | 25 Mobile tests pass; ~113 custom integration tests pass across P1–P10 |
| **Dashboard** | TheWatch.Dashboard (Blazor Server + Radzen) | Login, service health, per-program tabs, API explorer, mapping coverage |
| **Mobile** | TheWatch.Mobile (MAUI Blazor Hybrid) | Login, Home, SOS, Phrases, Health, Profile; voice framework; typed API client |
| **Generators** | TheWatch.Generators (8 Roslyn generators) | Endpoint, Model, Service, Hangfire, Test, Serilog, OpenApi, MauiPage generators |
| **Shared** | TheWatch.Shared | Health contracts, Serilog defaults, mobile DTOs |
| **Orchestration** | Aspire AppHost + ServiceDefaults | All 11 services orchestrated with service discovery, OpenTelemetry |

### Data Assets

| Asset | Location | Status |
|-------|----------|--------|
| 4,365 consolidated entities | `_Consolidated/json_output/` (7 JSON sheets) | Complete |
| Labeled JSONL | `_Consolidated/jsonl_output/` (7 files) | Complete |
| 251 OpenAPI specs | `E:\json_output\APIS\` (48 domain files + catalog) | Extracted, cataloged |
| API-to-code mapping | `E:\json_output\Microservices\_mapping.json` | 256 matched ops, 2,739 unmatched |
| MongoDB Atlas | `TheWatch` DB on ClusterOne | 8 collections, 4,365 docs + 251 API docs |
| SQL schemas | `Common_Measurement_Infrastructure/` | P0–P10 DDL, PostGIS geospatial |
| Program specifications | `Watch-AuthSecurity/`, `Watch-DisasterRelief/`, `Watch-Logging/`, etc. | Detailed designs for P2–P10 |
| 10 monitoring agents | `Agents/` | PowerShell orchestrated, schema/security/API/quality/compliance |

### Key Gaps

1. **All services use in-memory ConcurrentDictionary** — no SQL Server, no MongoDB, no Redis
2. **No real database schemas deployed** — SQL DDL exists in `Common_Measurement_Infrastructure/` but isn't wired
3. **No real authentication backend** — P5 uses in-memory user store
4. **MAUI speech recognition is a framework stub** — no platform-specific implementations
5. **No Docker/Kubernetes/Helm** — no containerization
6. **No CI/CD pipeline** — no GitHub Actions wired to the Microservices solution
7. **No event bus** — Kafka/SignalR/Service Bus not integrated
8. **No geospatial engine** — PostGIS schema exists but no service integration
9. **No cloud deployment** — Azure, GCP, Cloudflare not configured
10. **No evidence chain-of-custody** — core memorandum requirement not yet implemented

---

## Stage 5: Database Layer — SQL Server + Entity Framework

Replace all ConcurrentDictionary stores with real database persistence.

### 5A. Entity Framework Core Integration
- Add EF Core packages to all 10 services
- Create DbContext per service with entity configurations
- Map existing in-memory models to EF entities
- Add migration infrastructure
- Connection string management via Aspire service discovery

### 5B. SQL Server Schema Deployment
- Adapt existing DDL from `Common_Measurement_Infrastructure/` to EF migrations
- P0 UniversalMeasurementsDB (foundation data — 12,853 rows)
- P1–P10 databases with proper FK constraints, indexes, ROWVERSION concurrency
- Seed data scripts for development

### 5C. Repository Pattern
- Extract IRepository<T> interfaces from existing services
- Implement EF-backed repositories
- Keep in-memory implementations as test doubles
- Wire via DI (swap based on configuration)

### 5D. Aspire SQL Server Resource
- Add SQL Server container to AppHost
- Configure connection strings via service discovery
- Health checks against real database

---

## Stage 6: Real-Time & Events — SignalR + Kafka

### 6A. SignalR Hub for Live Updates
- Emergency incident real-time feed (P2)
- Responder location streaming (P6)
- Family check-in notifications (P7)
- Dispatch status updates (P2/P6)

### 6B. Kafka Event Bus
- Publish domain events from each service
- Cross-service event consumption (P2 incident → P6 dispatch → P3 mesh broadcast)
- Event sourcing for audit trail
- Dead letter queue handling

### 6C. Push Notifications
- Firebase Cloud Messaging (FCM) for Android
- Apple Push Notification Service (APNS) for iOS
- MAUI notification handlers
- Notification preferences per user (P5)

---

## Stage 7: Geospatial Engine — PostGIS + Maps

### 7A. PostGIS Database
- Deploy `11_Geospatial.sql` schema (28+ tables, 5 schemas, 12 spatial functions)
- Haversine → PostGIS `ST_DWithin` for responder queries
- Spatial indexing (GIST) for sub-millisecond proximity lookups

### 7B. Mapping Service
- Geospatial microservice wrapping PostGIS
- Nearest-N responder queries with expanding radius
- Incident zone polygons
- Evacuation route calculation (P8)
- Geofencing for family check-ins (P7)

### 7C. Client-Side Maps
- MAUI map component (Google Maps / Apple Maps)
- Responder placement visualization
- Incident zone overlay
- Real-time responder movement tracking
- Dashboard map view (Leaflet/OpenLayers)

---

## Stage 8: Authentication & Security Hardening — P5 Full Implementation

### 8A. Identity Provider
- Replace in-memory auth with ASP.NET Identity or Azure AD B2C
- PBKDF2 → Argon2id password hashing
- Multi-factor authentication (7 methods from spec: SMS, TOTP, email, biometric, passkey, SSO, magic link)
- JWT sliding window expiration
- Refresh token rotation with revocation

### 8B. Authorization
- Role-based access control (RBAC) across all services
- Claims-based authorization for program-specific permissions
- API key management for inter-service communication
- Rate limiting per user/IP

### 8C. Security Monitoring
- STRIDE threat model integration (from P5 spec)
- MITRE ATT&CK mapping
- Audit logging for all auth events
- Brute force detection and account lockout
- Device trust scoring

---

## Stage 9: MAUI Mobile — Production Ready

### 9A. Platform Speech Recognition
- Android: `SpeechRecognizer` with continuous recognition
- iOS: `SFSpeechRecognizer` with on-device processing
- Windows: `Windows.Media.SpeechRecognition`
- Background service for perpetual listening
- Battery-aware throttling

### 9B. Offline Resilience
- SQLite local database for offline data
- Queue outbound requests when offline
- Sync engine: local → server reconciliation
- Conflict resolution strategy
- Offline emergency mode (P3 mesh network fallback)

### 9C. Native Features
- Camera integration (evidence collection)
- GPS with high-accuracy mode for emergency
- Haptic feedback patterns (SOS confirmation, alert received)
- Biometric authentication (fingerprint/face)
- Background location tracking (with user consent)

### 9D. Evidence Collection
- Video recording with metadata overlay
- Photo capture with GPS stamping
- Speech-to-text incident reporting (SITREP format)
- Chain-of-custody: hash, timestamp, sign every artifact
- Client-side nudity detection (OpenCV)

---

## Stage 10: Containerization & CI/CD

### 10A. Docker
- Dockerfile per microservice (multi-stage build)
- docker-compose.yml for local development stack
- SQL Server, Redis, Kafka containers
- Aspire integration with container orchestration

### 10B. Kubernetes / Helm
- Helm chart per service
- HPA (Horizontal Pod Autoscaler) for P2/P6 (emergency surge)
- ConfigMaps for service configuration
- Secrets management (Azure Key Vault CSI driver)
- Ingress controller with TLS termination

### 10C. CI/CD Pipeline
- GitHub Actions workflows for build/test/deploy
- Branch protection rules (main requires PR review)
- Automated test execution on PR
- Container image build and push to ACR
- Staged deployment: dev → staging → production
- CodeQL security scanning
- Dependency review

---

## Stage 11: Cloud Deployment — Azure Primary

### 11A. Azure Infrastructure (Terraform/Bicep)
- Azure SQL Database (per-service databases)
- Azure Cosmos DB (MongoDB API for event streams)
- Azure Redis Cache
- Azure Service Bus (complement to Kafka)
- Azure Key Vault (secrets, certificates)
- Azure Container Apps or AKS

### 11B. Azure Services Integration
- Azure SignalR Service (managed real-time)
- Azure Maps (geospatial alternative to PostGIS for cloud)
- Azure Blob Storage (evidence artifacts)
- Azure Communication Services (SMS/email notifications)
- Application Insights (APM)

### 11C. GCP Secondary Services
- Google Speech-to-Text API (P2 voice recognition)
- Google Vision API (evidence analysis, nudity detection)
- Firebase (FCM, Analytics, Crashlytics)
- Google Healthcare API (P7 health data, FHIR)

### 11D. Cloudflare Edge
- CDN for static assets
- Workers for edge compute
- R2 for object storage
- WAF rules
- Zero Trust access for admin surfaces
- Argo Tunnels for service exposure

---

## Stage 12: Advanced Features & Compliance

### 12A. ML/AI Integration
- Gunshot detection audio model (P2 active shooter)
- Fall detection from wearable accelerometer data (P4)
- Anomaly detection for vital signs (P7)
- Responder optimization (P6 dispatch efficiency)
- Natural language SITREP generation

### 12B. Compliance & Privacy
- HIPAA compliance for health data (P7, P9)
- COPPA compliance for child data (P7)
- GDPR right to erasure
- SOX-expanded audit framework (from memorandum)
- Data residency controls (per-country)

### 12C. Graph Database
- Deploy `Watch-GraphDB.sql` for relationship queries
- Social graph for responder networks
- Incident correlation across geography and time
- Family relationship traversal (P7)

### 12D. Monitoring & Observability
- Wire 10 monitoring agents into CI/CD
- Prometheus metrics export
- Grafana dashboards per service
- PagerDuty/OpsGenie alerting
- SLA tracking per endpoint

---

## Milestone Summary

| Stage | Focus | Key Deliverable |
|-------|-------|-----------------|
| 5 | Database | EF Core + SQL Server replacing in-memory stores |
| 6 | Real-Time | SignalR hubs + Kafka events + push notifications |
| 7 | Geospatial | PostGIS + maps for responder proximity |
| 8 | Security | Full P5 auth with MFA, RBAC, threat monitoring |
| 9 | Mobile | Production MAUI with speech, offline, evidence |
| 10 | DevOps | Docker + Kubernetes + CI/CD pipelines |
| 11 | Cloud | Azure + GCP + Cloudflare deployment |
| 12 | Advanced | ML, compliance, graph DB, observability |

---

*Last updated: 2026-02-25 (Session 7)*
