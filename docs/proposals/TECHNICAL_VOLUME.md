# Technical Volume — TheWatch Emergency Response Platform

> **Classification:** CUI // SP-PRIV // SP-HLTH  
> **Reference Standards:** NIST SP 800-53 Rev 5 (SA family), CMMC Level 2, DISA STIG  
> **Document ID:** TV-001  
> **Version:** 1.0

---

## 1. Technical Approach Overview

TheWatch is a distributed microservices platform engineered for emergency response, public
safety, and community health monitoring. The system is designed around a **zero-trust
architecture** with defense-in-depth security, event-driven communication, and resilient
offline-capable operations.

### 1.1 Design Principles

| Principle | Implementation | Standard Alignment |
|-----------|---------------|-------------------|
| **Zero Trust** | JWT bearer authentication on every request; no implicit trust between services | NIST 800-207, EO 14028 |
| **Defense in Depth** | Multi-layer security: network, transport, application, data | NIST 800-53 SC-7 |
| **Least Privilege** | RBAC with 6 granular policies; service accounts scoped per microservice | NIST 800-171 AC-6 |
| **Separation of Concerns** | 11 bounded-context microservices with isolated databases | NIST 800-53 SC-3 |
| **Secure by Default** | TLS 1.2+ enforced, rate limiting enabled, security headers applied | DISA STIG V-222425 |
| **Resilience** | Offline queue, mesh networking, exponential backoff, circuit breakers | NIST 800-53 CP-2 |
| **Auditability** | All operations logged with correlation IDs; HMAC-signed audit trails | NIST 800-171 AU-3 |

### 1.2 System Context

```
┌─────────────────────────────────────────────────────────────────────┐
│                        EXTERNAL ACTORS                              │
│  Citizens │ First Responders │ Medical Staff │ Administrators       │
└─────┬──────────┬───────────────┬──────────────┬─────────────────────┘
      │          │               │              │
      ▼          ▼               ▼              ▼
┌─────────────────────────────────────────────────────────────────────┐
│  P1 CoreGateway (API Gateway + Request Orchestration)               │
│  ► Rate Limiting  ► Auth Validation  ► Request Routing              │
├─────────────────────────────────────────────────────────────────────┤
│                    MICROSERVICES LAYER                               │
│  ┌──────────┐ ┌──────────┐ ┌──────────┐ ┌──────────┐ ┌──────────┐ │
│  │P2 Voice  │ │P3 Mesh   │ │P4 Wear-  │ │P5 Auth   │ │P6 First  │ │
│  │Emergency │ │Network   │ │able      │ │Security  │ │Responder │ │
│  └──────────┘ └──────────┘ └──────────┘ └──────────┘ └──────────┘ │
│  ┌──────────┐ ┌──────────┐ ┌──────────┐ ┌──────────┐ ┌──────────┐ │
│  │P7 Family │ │P8 Dis-   │ │P9 Doctor │ │P10 Gami- │ │P11 Sur-  │ │
│  │Health    │ │aster     │ │Services  │ │fication  │ │veillance │ │
│  └──────────┘ └──────────┘ └──────────┘ └──────────┘ └──────────┘ │
│  ┌──────────────────────┐ ┌────────────────────────────────────┐   │
│  │ Geospatial Engine    │ │ Shared Infrastructure (Logging,    │   │
│  │ (PostGIS / Azure Maps│ │  Messaging, Auth, Encryption)      │   │
│  └──────────────────────┘ └────────────────────────────────────┘   │
├─────────────────────────────────────────────────────────────────────┤
│                      DATA LAYER                                     │
│  SQL Server 2022 (11 DBs) │ PostgreSQL+PostGIS │ MongoDB │ Redis   │
├─────────────────────────────────────────────────────────────────────┤
│                   EVENT BUS (Kafka / Azure Service Bus)             │
└─────────────────────────────────────────────────────────────────────┘
```

---

## 2. Microservices Architecture

### 2.1 Service Catalog

Each microservice follows a consistent internal architecture pattern:

- **ASP.NET Core Minimal API** with Scalar/OpenAPI documentation
- **Entity Framework Core** with `IWatchRepository<T>` data access pattern
- **Hangfire Pro** for background job scheduling
- **Serilog** structured logging with correlation IDs
- **SignalR** hubs for real-time communication
- **Kafka/Service Bus** event publishing and consumption
- **Health checks** with `/healthz` and `/ready` endpoints

#### P1 — Core Gateway

| Attribute | Detail |
|-----------|--------|
| **Purpose** | API gateway, request orchestration, cross-service aggregation |
| **Endpoints** | 13 Admin REST API controllers + service routing |
| **Security** | JWT validation, rate limiting (100 req/min global, 10 req/min auth), CORS whitelist |
| **Standards** | NIST 800-53 SC-7 (Boundary Protection), AC-4 (Information Flow Enforcement) |
| **Evidence** | `TheWatch.Admin.RestAPI/Program.cs`, `TheWatch.P1.CoreGateway/` |

#### P2 — Voice Emergency

| Attribute | Detail |
|-----------|--------|
| **Purpose** | SOS ingestion, incident creation, responder dispatch |
| **Real-time** | SignalR hubs: `/hubs/emergency`, `/hubs/dispatch` |
| **Events** | Kafka producer: `EmergencyCreated`, `DispatchRequested` |
| **SLA Target** | Sub-2-second response from SOS to dispatch notification |
| **Standards** | NIST 800-53 IR-4 (Incident Handling), CP-2 (Contingency Planning) |
| **Evidence** | `TheWatch.P2.VoiceEmergency/` |

#### P3 — Mesh Network

| Attribute | Detail |
|-----------|--------|
| **Purpose** | Offline/degraded-network communications via Bluetooth relay mesh |
| **Capability** | Device-to-device message relay when cellular/Wi-Fi unavailable |
| **Events** | Kafka consumer/producer for network state synchronization |
| **Standards** | NIST 800-53 CP-8 (Telecommunications Services), SC-7 |
| **Evidence** | `TheWatch.P3.MeshNetwork/` |

#### P4 — Wearable Integration

| Attribute | Detail |
|-----------|--------|
| **Purpose** | Device provisioning, heartbeat monitoring, telemetry ingestion |
| **Capability** | Firmware management, fall detection, vitals streaming |
| **Standards** | NIST 800-53 IA-3 (Device Identification), PM-17 (IoT Security) |
| **Evidence** | `TheWatch.P4.Wearable/` |

#### P5 — Authentication & Security

| Attribute | Detail |
|-----------|--------|
| **Purpose** | Identity management, multi-factor authentication, RBAC, token issuance |
| **MFA Methods** | TOTP, FIDO2/Passkey, SMS OTP, Magic Link (4 methods) |
| **Password Hashing** | Argon2id (default) with PBKDF2-SHA512 (FIPS 140-2 fallback) |
| **JWT** | RSA-2048 asymmetric signing (production), HMAC-SHA256 (development) |
| **Brute Force** | 3-attempt lockout → 15 min; 10 attempts → 60 min; 15 → deactivation |
| **Device Trust** | Fingerprint + IP + geolocation scoring (threshold: 30) |
| **Threat Detection** | STRIDE scanning (15-min intervals), MITRE ATT&CK rules (T1078, T1110, T1528, T1621, T1556) |
| **Standards** | NIST 800-171 IA-1 through IA-8, DISA STIG V-222524-V-222546 |
| **Evidence** | `TheWatch.P5.AuthSecurity/`, `TheWatch.Shared/Security/` |

#### P6 — First Responder

| Attribute | Detail |
|-----------|--------|
| **Purpose** | Responder profile management, real-time location streaming, incident assignment |
| **Real-time** | SignalR hub for location updates |
| **Geospatial** | Nearest-responder queries (10km default radius) |
| **Standards** | NIST 800-53 PE-17 (Alternate Work Site), CP-2 |
| **Evidence** | `TheWatch.P6.FirstResponder/` |

#### P7 — Family Health

| Attribute | Detail |
|-----------|--------|
| **Purpose** | Family group management, vital signs tracking, check-in system, health alerts |
| **Data Classification** | CUI // SP-HLTH (Protected Health Information) |
| **Geofencing** | Family member location monitoring with configurable alert zones |
| **Standards** | HIPAA Security Rule §164.312, NIST 800-53 SC-28 |
| **Evidence** | `TheWatch.P7.FamilyHealth/` |

#### P8 — Disaster Relief

| Attribute | Detail |
|-----------|--------|
| **Purpose** | Disaster event coordination, shelter management, evacuation routing, resource allocation |
| **Geospatial** | Polygon-based disaster zones, evacuation waypoint routing |
| **Standards** | NIST 800-53 CP-2 (Contingency Planning), CP-4 (Testing) |
| **Evidence** | `TheWatch.P8.DisasterRelief/` |

#### P9 — Doctor Services

| Attribute | Detail |
|-----------|--------|
| **Purpose** | Telehealth consultations, appointment scheduling, medical record access |
| **Data Classification** | CUI // SP-HLTH (Protected Health Information) |
| **Standards** | HIPAA Security Rule, NIST 800-53 SC-28, AC-3 |
| **Evidence** | `TheWatch.P9.DoctorServices/` |

#### P10 — Gamification

| Attribute | Detail |
|-----------|--------|
| **Purpose** | Community engagement through badges, safety challenges, leaderboards |
| **Standards** | NIST 800-53 AC-3 (Access Enforcement) |
| **Evidence** | `TheWatch.P10.Gamification/` |

#### P11 — Surveillance

| Attribute | Detail |
|-----------|--------|
| **Purpose** | Camera registration, footage analysis, crime location reporting |
| **ML/AI** | ONNX object detection model via ML.NET (1-minute analysis cycle) |
| **Real-time** | SignalR hubs: `/hubs/cameras`, `/hubs/detections`, `/hubs/footagesubmissions` |
| **Chain of Custody** | SHA-256 hashed evidence with HMAC-SHA256 device signatures |
| **Data Lifecycle** | 30-day stale footage cleanup |
| **Standards** | NIST 800-53 AU-12 (Audit Record Generation), PE-6 (Monitoring) |
| **Evidence** | `TheWatch.P11.Surveillance/` |

#### Geospatial Engine

| Attribute | Detail |
|-----------|--------|
| **Purpose** | Spatial queries, routing, geofencing, zone management |
| **Backend** | PostGIS (default) or Azure Maps (configurable) |
| **Capabilities** | Nearest-N queries, within-radius, incident zones, evacuation routes, family geofences |
| **Data Format** | GeoJSON with NetTopologySuite |
| **Standards** | NIST 800-53 SC-7 |
| **Evidence** | `TheWatch.Geospatial/` |

### 2.2 Shared Infrastructure

| Component | Purpose | Standards Alignment |
|-----------|---------|-------------------|
| **TheWatch.Shared** | Logging, JWT, encryption, CORS, rate limiting, HIPAA compliance, CUI marking, PII redaction | NIST 800-53 SC, AU families |
| **TheWatch.Contracts.\*** | 12 typed HTTP client libraries for inter-service communication | NIST 800-53 SC-8 (Transmission Confidentiality) |
| **TheWatch.Generators** | 10 Roslyn source generators (endpoints, models, DbContext, tests, SignalR, logging, OpenAPI) | NIST 800-53 SA-15 (Development Process) |
| **TheWatch.Aspire.AppHost** | Local development orchestration of all 15 services + data stores | N/A (Development tooling) |

### 2.3 Client Surfaces

| Client | Technology | Capabilities |
|--------|-----------|-------------|
| **Dashboard** | Blazor Server + Radzen | 12-page SPA: incident map, response demo, mapping, analytics |
| **Admin Portal** | Blazor Server | 8 pages: users, roles, security, audit log, services, settings |
| **Admin REST API** | ASP.NET Core | 13 controllers with Scalar API documentation |
| **Admin CLI** | PowerShell | 45 cmdlets for operational management |
| **Mobile App** | .NET MAUI Hybrid Blazor | 12 pages, 31 services, offline SQLite, BLE mesh, chain-of-custody |

---

## 3. Data Architecture

### 3.1 Database Design

| Database | Technology | Services | Data Classification |
|----------|-----------|----------|-------------------|
| P1_CoreGateway | SQL Server 2022 | P1 | CUI // SP-BASIC |
| P2_VoiceEmergency | SQL Server 2022 | P2 | CUI // SP-LEI |
| P3_MeshNetwork | SQL Server 2022 | P3 | CUI // SP-BASIC |
| P4_Wearable | SQL Server 2022 | P4 | CUI // SP-PRIV |
| P5_AuthSecurity | SQL Server 2022 | P5 | CUI // SP-PRIV |
| P6_FirstResponder | SQL Server 2022 | P6 | CUI // SP-LEI |
| P7_FamilyHealth | SQL Server 2022 | P7 | CUI // SP-HLTH |
| P8_DisasterRelief | SQL Server 2022 | P8 | CUI // SP-GEO |
| P9_DoctorServices | SQL Server 2022 | P9 | CUI // SP-HLTH |
| P10_Gamification | SQL Server 2022 | P10 | CUI // SP-BASIC |
| P11_Surveillance | SQL Server 2022 | P11 | CUI // SP-LEI |
| Geospatial | PostgreSQL 16 + PostGIS | Geospatial | CUI // SP-GEO |
| Event Streams | MongoDB Atlas | Cross-service events | CUI // SP-BASIC |
| Cache/Session | Redis 7 | Cross-service | CUI // SP-PRIV |

### 3.2 Data Flow Security

| Flow | Protection | Standard |
|------|-----------|----------|
| Client → Gateway | TLS 1.2+ (HTTPS enforced) | NIST 800-171 SC-8 |
| Gateway → Services | JWT bearer token validation | NIST 800-171 SC-8(1) |
| Services → Database | Connection-level encryption | NIST 800-171 SC-28 |
| Service → Service | Typed HTTP clients with auth headers | NIST 800-53 SC-8 |
| Service → Kafka | Event publishing (TLS planned) | NIST 800-53 SC-8 |
| Mobile → Gateway | TLS 1.2+ with certificate pinning | NIST 800-171 SC-8 |
| Mobile (Offline) | SQLite with local encryption | NIST 800-171 SC-28(1) |
| Field Encryption | AES-256-GCM per column | NIST 800-171 SC-28 |

### 3.3 Entity Scale

- **4,365 consolidated entities** across 7 domain sheets
- **251 OpenAPI specifications** across 48 domain files
- **EF Core migrations** with automated schema management
- **Seed data** for development and testing

---

## 4. Real-Time Communication

### 4.1 SignalR Hubs

| Hub | Service | Purpose |
|-----|---------|---------|
| `/hubs/emergency` | P2 | Emergency incident real-time updates |
| `/hubs/dispatch` | P2 | Responder dispatch notifications |
| `/hubs/mesh` | P3 | Mesh network relay coordination |
| `/hubs/wearable` | P4 | Device telemetry streaming |
| `/hubs/responder` | P6 | Location tracking updates |
| `/hubs/family` | P7 | Family check-in notifications |
| `/hubs/cameras` | P11 | Camera status updates |
| `/hubs/detections` | P11 | Object detection alerts |
| `/hubs/footagesubmissions` | P11 | Evidence submission tracking |

### 4.2 Event Streaming (Kafka)

| Topic | Producer | Consumers | Purpose |
|-------|----------|-----------|---------|
| `emergency-created` | P2 | P6, P11 | New emergency incidents |
| `dispatch-requested` | P2 | P6 | Responder dispatch |
| `mesh-relay` | P3 | P3 | Mesh network message relay |
| `footage-submitted` | P11 | P11 | Video evidence submissions |
| `responder-location` | P6 | P2 | Real-time responder positions |

---

## 5. Mobile Application Architecture

### 5.1 Platform

- **.NET MAUI Hybrid Blazor** — cross-platform (iOS, Android, Windows, macOS)
- **12 pages** covering all major user functions
- **31 services** for offline-capable operations
- **SQLite** local database for offline data persistence

### 5.2 Offline Capabilities

| Capability | Implementation | Standard |
|------------|---------------|----------|
| **Request Queue** | FIFO SQLite queue with priority ordering | NIST 800-53 CP-2 |
| **Sync Engine** | Automatic reconciliation on reconnection | NIST 800-53 CP-9 |
| **Mesh Fallback** | BLE device-to-device relay when network unavailable | NIST 800-53 CP-8 |
| **Local Storage** | Encrypted SQLite database | NIST 800-171 SC-28(1) |
| **Retry Logic** | Exponential backoff (5 retries max) | NIST 800-53 SC-5 |

### 5.3 Evidence Chain of Custody

| Feature | Implementation | Standard |
|---------|---------------|----------|
| **File Hashing** | SHA-256 integrity hash for all evidence files | NIST 800-53 AU-10 |
| **Device Signatures** | HMAC-SHA256 with device fingerprint | NIST 800-53 AU-10(2) |
| **Hash Chain** | Each custody record references previous record hash | NIST 800-53 AU-9 |
| **Location Capture** | GPS coordinates at time of evidence collection | NIST 800-53 AU-3(1) |
| **Biometric Gate** | Fingerprint/face verification before evidence access | NIST 800-53 IA-2(12) |

---

## 6. Integration Architecture

### 6.1 External Integration Points

| Integration | Protocol | Purpose | Security |
|-------------|----------|---------|----------|
| Azure Maps / PostGIS | HTTPS REST | Geospatial queries, routing | API key + TLS |
| CISA KEV Catalog | HTTPS REST | Known vulnerability monitoring | TLS |
| NuGet Advisory DB | HTTPS REST | Dependency vulnerability scanning | TLS |
| GitHub Advisory DB | HTTPS REST | Open-source vulnerability monitoring | TLS |
| ONNX Runtime | Local | ML object detection models | Process isolation |
| Docker Registry | HTTPS | Container image distribution | Registry auth + TLS |
| Azure Key Vault | HTTPS REST | Secrets and certificate management | Managed Identity |

### 6.2 Inter-Service Communication Patterns

| Pattern | Implementation | Use Case |
|---------|---------------|----------|
| **Request/Response** | Typed HTTP clients (`TheWatch.Contracts.*`) | Synchronous queries |
| **Event-Driven** | Kafka/Service Bus pub/sub | Asynchronous notifications |
| **Real-Time** | SignalR WebSocket hubs | Live updates, streaming |
| **Background Jobs** | Hangfire Pro scheduled tasks | Periodic processing |
| **Health Aggregation** | ASP.NET Core health checks | Service availability |

---

## 7. Development Methodology

### 7.1 Secure Development Lifecycle

| Phase | Practice | Standard |
|-------|---------|----------|
| **Requirements** | Threat modeling (STRIDE + MITRE ATT&CK) | SSDF PO.1 |
| **Design** | Security architecture review, data classification | SSDF PW.1 |
| **Implementation** | Roslyn analyzers, secure coding standards | SSDF PW.5 |
| **Build** | Deterministic builds, NuGet audit, SBOM generation | SSDF PS.2, PW.4 |
| **Test** | 12 test projects, CodeQL SAST, Trivy container scanning | SSDF PW.7, PW.8 |
| **Deploy** | Signed commits, container signing, provenance | SSDF PS.1, PS.2 |
| **Monitor** | Continuous vulnerability monitoring, SIEM correlation | SSDF RV.1 |

### 7.2 Code Generation

TheWatch employs 10 Roslyn source generators to ensure consistency and reduce human error:

| Generator | Purpose | Standard |
|-----------|---------|----------|
| EndpointGenerator | API endpoint scaffolding | SSDF PW.5 |
| ModelGenerator | Domain entity generation | SSDF PW.5 |
| ServiceGenerator | Service layer scaffolding | SSDF PW.5 |
| DbContextGenerator | EF Core context generation | SSDF PW.5 |
| HangfireJobGenerator | Background job templates | SSDF PW.5 |
| TestGenerator | Test scaffolding | SSDF PW.7 |
| SerilogConfigGenerator | Logging configuration | NIST 800-53 AU-3 |
| OpenApiSchemaGenerator | API documentation | SSDF PW.5 |
| SignalRHubGenerator | Real-time hub scaffolding | SSDF PW.5 |
| ValidationGenerator | Input validation rules | OWASP A03 |

---

## 8. Performance & Scalability

### 8.1 Performance Targets

| Metric | Target | Measurement |
|--------|--------|-------------|
| SOS-to-Dispatch Latency | < 2 seconds | End-to-end from SOS button press |
| API Response (p95) | < 500ms | Gateway to client response |
| SignalR Message Delivery | < 100ms | Hub to subscriber |
| Database Query (p95) | < 200ms | EF Core query execution |
| Container Startup | < 30s | Cold start to health check pass |

### 8.2 Scalability Architecture

| Component | Scaling Strategy | Range |
|-----------|-----------------|-------|
| Microservices | Horizontal auto-scaling (KEDA) | 1–20 replicas per service |
| Kubernetes Nodes | Node auto-provisioning (Karpenter) | On-demand |
| Databases | Geo-replication (Azure SQL), read replicas | Per-tier |
| Kafka | Partition-based scaling | Per-topic |
| Redis | Clustered with sentinel | HA configuration |
| Pod Disruption | Budgets configured per service | Minimum availability |

---

*This Technical Volume demonstrates TheWatch's comprehensive microservices architecture,
security-first design, and alignment with DoD standards for emergency response operations.*
