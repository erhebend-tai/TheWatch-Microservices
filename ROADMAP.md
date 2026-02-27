# TheWatch — Project Roadmap

> **Origin**: 2022-08-11 DoD Memorandum — Sub-2-second citizen emergency response & deterrence.
> **Constraint**: The core mission remains the center of gravity. Everything serves it.
> **Current Status**: 180/265 TODO items complete. Stages 5-13 done. Stages 14-18 pending for launch.

---

## What Exists Today (Sessions 1–35)

### Solution: 54+ .NET 10 Projects

| Layer | Projects | Status |
|-------|----------|--------|
| **Microservices** | P1-P10 + P11 Surveillance + Geospatial (12 services) | Real endpoints, EF Core + IWatchRepository, SignalR hubs, Kafka events, Hangfire Pro jobs, JWT auth on all endpoints |
| **Contracts** | 12 typed client libraries (TheWatch.Contracts.*) + Abstractions | ServiceClientBase pattern, Aspire service discovery, per-service DTOs |
| **REST API Gateway** | TheWatch.Admin.RestAPI (13 controllers) | Zero Trust auth, Scalar API docs, aggregated health checks, routes to all 12 services |
| **Admin Portal** | TheWatch.Admin (8 Blazor Server pages) | Dashboard, Users, Roles, Security, Audit Log, Services, Settings, Login |
| **Dashboard** | TheWatch.Dashboard (Blazor Server + Radzen) | Login, service health, per-program tabs, API explorer, mapping coverage, incident map, response demo |
| **Mobile** | TheWatch.Mobile (MAUI Blazor Hybrid, 12 pages, 31 services) | Auth, SOS, Map, Evidence, SITREP, Health, Profile; platform speech; offline SQLite; BLE mesh fallback; chain-of-custody |
| **Generators** | TheWatch.Generators (10 Roslyn generators) | Endpoint, Model, Service, Hangfire, Test, Serilog, OpenApi, MauiPage, DbContext, SignalR, Security, Notification, Kafka, RestApiController |
| **Shared** | TheWatch.Shared | Auth extensions, security middleware, Serilog defaults, mobile DTOs, Azure/GCP/Cloudflare provider stubs, Kafka events, notification contracts |
| **Orchestration** | Aspire AppHost + ServiceDefaults | 12 services + SQL Server (11 DBs) + PostgreSQL/PostGIS + Kafka + Redis orchestrated |
| **Infrastructure** | Docker (12 Dockerfiles), K8s (Helm), Terraform (Azure 7 modules, AWS 20 modules, GCP 9 modules), 5 GitHub Actions workflows | Full multi-cloud IaC |
| **CLI** | TheWatch.Admin.CLI (PowerShell module, 45 cmdlets) | API ops, infra management, Terraform plan/apply |
| **Tests** | 12 test projects | P1-P10 integration tests, Geospatial tests, Mobile unit tests (battery, crypto, sync, auth) |

### Data Assets

| Asset | Location | Status |
|-------|----------|--------|
| 4,365 consolidated entities | `_Consolidated/json_output/` (7 JSON sheets) | Complete |
| Labeled JSONL | `_Consolidated/jsonl_output/` (7 files) | Complete |
| 251 OpenAPI specs | `E:\json_output\APIS\` (48 domain files + catalog) | Extracted, cataloged |
| API-to-code mapping | `E:\json_output\Microservices\_mapping.json` | 256 matched ops, 2,739 unmatched |
| MongoDB Atlas | `TheWatch` DB on ClusterOne | 8 collections, 4,365 docs + 251 API docs |
| SQL schemas | `Common_Measurement_Infrastructure/` | P0–P11 DDL, PostGIS geospatial |
| EF Core Migrations | Per-service `Migrations/` directories | 10 services migrated (P5 uses EnsureCreated, P11 missing) |
| Seed Data | Per-service `Data/Seeders/` classes | 9 seeders registered via IWatchDataSeeder (P5 seeds inline, P11 missing) |
| 10 monitoring agents | `Agents/` | PowerShell orchestrated, schema/security/API/quality/compliance |

### Key Gaps (Launch Blockers)

1. **P11 Surveillance not in deployment infrastructure** — missing from docker-compose, Terraform (Azure/AWS/GCP), Helm, health checks, CI/CD matrix, and has no test project or EF migration
2. **Cloud provider stubs throw NotImplementedException** — GCP (Speech, Vision, Healthcare) and Cloudflare (CDN, Workers, WAF, Tunnels) providers are interface-complete but implementation bodies throw
3. **No end-to-end integration tests** — individual service tests exist but no test exercises the full SOS→dispatch→evidence→SITREP lifecycle across services
4. **SyncEngine.SendAsync() not implemented** — MAUI offline sync engine has a critical gap at line 401
5. **No FluentValidation** on request DTOs — endpoints accept malformed input without structured validation
6. **No global exception handler** — 5xx errors return raw stack traces, not ProblemDetails
7. **Sub-2-second SOS latency not benchmarked** — core memorandum requirement never measured
8. **Inter-service typed clients not wired** — Contract libraries exist but services still use ad-hoc HTTP calls for cross-service communication

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

## Stage 14: P11 Surveillance Integration

P11.Surveillance was added (Session 34) with 16 endpoints, SignalR hubs, Kafka events, and an ONNX object detector. However it was never wired into docker-compose, Terraform, Helm, health checks, CI/CD, or testing.

- Add P11 to all deployment manifests (Docker, Helm, Terraform x3)
- Add P11 to health check aggregation and dashboard
- Create test project with integration tests
- Create EF migration and seed data
- Wire Kafka topics into event infrastructure

---

## Stage 15: Cloud Provider Implementation

GCP and Cloudflare provider classes implement the correct interfaces but every method body throws `NotImplementedException("...not yet implemented. Implement in batch.")`. 32 stub methods across 5 files.

### GCP (3 providers, 14 stubs)
- `GoogleSpeechToTextProvider` — `TranscribeAsync`, `StartStreamingAsync`
- `GoogleVisionProvider` — `AnalyzeImageAsync`, `AnalyzeImageUrlAsync`, `ExtractTextAsync`, `DetectLabelsAsync`
- `GoogleHealthcareProvider` — `UpsertPatientAsync`, `GetPatientAsync`, `SearchPatientsAsync`, `CreateObservationAsync`, `GetObservationsAsync`, `CreateEncounterAsync`, `ExportPatientDataAsync`

### Cloudflare (4 providers, 15 stubs)
- `CloudflareCdnService` — cache purge, analytics
- `CloudflareWorkerAuthService` — access token validation, identity
- `CloudflareWafService` — rule deployment, rate limits, events, IP blocking
- `CloudflareTunnelService` — tunnel status, connections, zero trust validation

### Azure
- `AzureMapsGeospatialService` — uses ConcurrentDictionary cache, should use Redis

---

## Stage 16: Contract Client Wiring

13 typed client libraries exist under `TheWatch.Contracts.*` but no service currently injects them for cross-service calls. Wire typed clients into consuming services to replace ad-hoc HTTP calls and enable Polly resilience policies, distributed tracing, and API key authentication.

---

## Stage 17: Production Hardening

Close the gap between "works in development" and "survives production traffic":

### Database
- P5 EF migration (currently EnsureCreated), concurrency tokens, connection resilience, ConcurrentDictionary→Redis migration

### API Quality
- FluentValidation, global ProblemDetails exception handler, PII-redacted logging, API versioning, response compression, ETags

### Mobile
- Fix SyncEngine.SendAsync(), bundle ONNX model, crash reporting, certificate pinning, accessibility audit

### Security
- Security headers, evidence request signing, JWT key rotation, OWASP ZAP scan, anti-forgery, secrets rotation runbook

### Observability
- Custom Prometheus metrics, Grafana dashboards, dependency health checks, distributed tracing enrichment, alerting rules, canary endpoints, runbook documentation

---

## Stage 18: Integration & End-to-End Testing

No test currently exercises the full system. Build confidence that the emergency lifecycle works end-to-end.

### End-to-End Tests
- Docker-compose test harness spinning up all services
- Full SOS lifecycle: incident → dispatch → evidence → SITREP
- Family health flow: family → vitals → alert → doctor
- Disaster relief flow: disaster → shelters → mesh → evacuees

### Load Tests (k6 / NBomber)
- 1,000 concurrent SOS activations
- 500 concurrent auth+MFA flows
- 10,000 concurrent SignalR connections
- 100 concurrent 50MB evidence uploads
- **Sub-2-second SOS benchmark** (core memorandum requirement)

### Chaos & Resilience
- SQL Server failover, Kafka broker failure, Redis failure
- Inter-service failure (P6 down during dispatch)
- MAUI offline→mesh→reconnect→sync cycle

---

## Milestone Summary

| Stage | Focus | Key Deliverable | Status |
|-------|-------|-----------------|--------|
| 5 | Database | EF Core + SQL Server replacing in-memory stores | **DONE** |
| 6 | Real-Time | SignalR hubs + Kafka events + push notifications | **DONE** |
| 7 | Geospatial | PostGIS + maps for responder proximity | **DONE** |
| 8 | Security | Full P5 auth with MFA, RBAC, threat monitoring | **DONE** |
| 9 | Mobile | Production MAUI with speech, offline, evidence | **DONE** |
| 10 | DevOps | Docker + Kubernetes + CI/CD pipelines | **DONE** |
| 11 | Cloud | Azure + GCP + Cloudflare deployment | **DONE** |
| 12 | Advanced | ML, compliance, graph DB, observability | **DONE** |
| 13 | AWS | Compute, data, security, DevOps Terraform | **DONE** |
| 14 | P11 Integration | Surveillance in deployment + tests | Pending |
| 15 | Cloud Providers | Replace 32 NotImplementedException stubs | Pending |
| 16 | Contracts | Wire typed clients for inter-service calls | Pending |
| 17 | Hardening | Database, API, mobile, security, observability | Pending |
| 18 | E2E Testing | Integration, load, chaos testing | Pending |

---

## Launch Readiness: Vertical Assessment

### READY (Green)
- **Authentication**: JWT + Argon2 + MFA (TOTP/SMS/magic link/FIDO2) + refresh rotation + sliding window + RBAC across all services
- **Real-Time**: SignalR generated hubs wired into business logic (incident broadcast, dispatch, responder tracking) + Kafka event bus with DLQ
- **Database**: EF Core with Roslyn-generated DbContext, IWatchRepository, migrations, seed data for 10/12 services
- **Mobile Core**: 12 pages, 31 services, offline SQLite, BLE mesh fallback, chain-of-custody cryptography, evidence collection
- **Infrastructure as Code**: Azure (7 modules), AWS (20 modules), GCP (9 modules), all with staging+production tfvars
- **CI/CD**: 5 GitHub Actions workflows (build, Docker publish, staging deploy, production deploy with approval gate, security scanning)

### NOT READY (Red)
- **P11 Surveillance**: Fully coded but invisible to deployment, monitoring, and tests
- **Cloud Provider Bodies**: 32 methods throw NotImplementedException if feature-flagged on
- **End-to-End Proof**: Zero tests that the SOS lifecycle works across services
- **SOS Latency**: Core <2s requirement from memorandum — never measured
- **Input Validation**: No FluentValidation; endpoints accept arbitrary payloads
- **Exception Handling**: No ProblemDetails; 5xx returns raw stack traces
- **SyncEngine.SendAsync()**: Mobile offline sync broken for non-standard HTTP methods

---

*Last updated: 2026-02-26 — Vertical launch assessment (Session 35). 180/265 items complete.*
