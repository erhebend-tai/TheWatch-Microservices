# TheWatch — Emergency Response Microservices Platform

[![CI — Build & Test](https://github.com/erhebend-tai/TheWatch-Microservices/actions/workflows/ci.yml/badge.svg)](https://github.com/erhebend-tai/TheWatch-Microservices/actions/workflows/ci.yml)
[![Security — CodeQL & Dependency Review](https://github.com/erhebend-tai/TheWatch-Microservices/actions/workflows/security.yml/badge.svg)](https://github.com/erhebend-tai/TheWatch-Microservices/actions/workflows/security.yml)
[![DoD Compliance](https://github.com/erhebend-tai/TheWatch-Microservices/actions/workflows/dod-compliance.yml/badge.svg)](https://github.com/erhebend-tai/TheWatch-Microservices/actions/workflows/dod-compliance.yml)
[![SBOM Generation](https://github.com/erhebend-tai/TheWatch-Microservices/actions/workflows/sbom-aggregate.yml/badge.svg)](https://github.com/erhebend-tai/TheWatch-Microservices/actions/workflows/sbom-aggregate.yml)

> **Classification: UNCLASSIFIED**
>
> A DoD-memorandum-driven emergency response platform featuring 54+ .NET 10
> microservices, real-time incident management, MAUI mobile application, and
> multi-cloud deployment across Azure, AWS, and GCP.

---

## Table of Contents

- [Mission Overview](#mission-overview)
- [Architecture](#architecture)
- [Technology Stack](#technology-stack)
- [Repository Structure](#repository-structure)
- [Getting Started](#getting-started)
- [Compliance & Security](#compliance--security)
- [Operations & Deployment](#operations--deployment)
- [Documentation](#documentation)
- [Contributing](#contributing)
- [Security Policy](#security-policy)

---

## Mission Overview

TheWatch provides a unified emergency response platform encompassing SOS
dispatch, voice emergency detection, mesh networking for offline resilience,
wearable device integration, family health monitoring, first responder
coordination, disaster relief management, and medical services — all built to
DoD security standards.

### Core Capabilities

| Capability | Service | Description |
|---|---|---|
| Incident Routing | P1 — CoreGateway | Central SOS handling and incident dispatch |
| Voice Emergency | P2 — VoiceEmergency | Voice recognition and active shooter detection |
| Mesh Networking | P3 — MeshNetwork | Offline BLE/mesh fallback for connectivity gaps |
| Wearable Integration | P4 — Wearable | Vitals streaming and device management via Kafka |
| Auth & Security | P5 — AuthSecurity | JWT, MFA (7 methods), RBAC, threat monitoring |
| First Responder | P6 — FirstResponder | Responder dispatch and real-time location tracking |
| Family Health | P7 — FamilyHealth | Family check-ins, health monitoring, geofencing |
| Disaster Relief | P8 — DisasterRelief | Shelter management, evacuation routing |
| Doctor Services | P9 — DoctorServices | Appointments, medical records, FHIR integration |
| Gamification | P10 — Gamification | Badges, points, leaderboards for engagement |
| Surveillance | P11 — Surveillance | ONNX object detection and video stream alerts |
| Geospatial | Geospatial | PostGIS spatial queries and proximity lookups |

---

## Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                      Client Surfaces                            │
│  ┌──────────┐  ┌──────────────┐  ┌──────────┐  ┌─────────────┐ │
│  │  Mobile   │  │  Dashboard   │  │  Admin   │  │  Admin CLI  │ │
│  │  (MAUI)   │  │  (Blazor)    │  │  Portal  │  │ (PowerShell)│ │
│  └─────┬─────┘  └──────┬───────┘  └─────┬────┘  └──────┬──────┘ │
└────────┼───────────────┼────────────────┼──────────────┼────────┘
         │               │                │              │
    ┌────▼───────────────▼────────────────▼──────────────▼────┐
    │                  Admin REST API Gateway                  │
    └────┬──────┬──────┬──────┬──────┬──────┬──────┬──────┬───┘
         │      │      │      │      │      │      │      │
    ┌────▼─┐ ┌──▼──┐ ┌─▼──┐ ┌▼───┐ ┌▼───┐ ┌▼───┐ ┌▼───┐ ┌▼────┐
    │  P1  │ │ P2  │ │ P3 │ │ P4 │ │ P5 │ │ P6 │ │ P7 │ │P8-11│
    └──┬───┘ └──┬──┘ └─┬──┘ └─┬──┘ └─┬──┘ └─┬──┘ └─┬──┘ └──┬──┘
       │        │      │      │      │      │      │       │
    ┌──▼────────▼──────▼──────▼──────▼──────▼──────▼───────▼──┐
    │           Shared Infrastructure Layer                    │
    │  ┌────────┐ ┌───────┐ ┌────────┐ ┌──────────┐           │
    │  │ Kafka  │ │ Redis │ │SQL Svr │ │ PostGIS  │           │
    │  │Events  │ │ Cache │ │  DBs   │ │ Spatial  │           │
    │  └────────┘ └───────┘ └────────┘ └──────────┘           │
    └─────────────────────────────────────────────────────────┘
```

### Supporting Components

- **TheWatch.Shared** — Cross-cutting concerns: auth extensions, security middleware, Serilog defaults
- **TheWatch.Contracts.\*** — 12 typed client libraries using `ServiceClientBase` pattern
- **TheWatch.Generators** — 10 Roslyn source generators (endpoints, models, services, Hangfire jobs)
- **TheWatch.Aspire.AppHost** — Local orchestration via .NET Aspire

---

## Technology Stack

| Layer | Technology |
|---|---|
| Runtime | .NET 10 (preview) |
| Microservices | ASP.NET Core Minimal APIs |
| Mobile | .NET MAUI Blazor Hybrid (offline SQLite, BLE mesh) |
| Web UIs | Blazor Server + Radzen |
| Eventing | Apache Kafka |
| Real-Time | SignalR |
| Databases | SQL Server 2022, PostgreSQL + PostGIS |
| Caching | Redis |
| Auth | JWT + Argon2id + MFA (7 methods) + RBAC |
| ORM | Entity Framework Core |
| Code Gen | Roslyn Source Generators |
| Containers | Docker (multi-stage, non-root per DISA STIG V-222425) |
| Orchestration | Kubernetes (Helm), .NET Aspire (local) |
| IaC | Terraform (Azure, AWS, GCP) |
| CI/CD | GitHub Actions |
| Security Scanning | CodeQL, Trivy, Gitleaks, Grype |
| SBOM | CycloneDX + SPDX |

---

## Repository Structure

```
TheWatch-Microservices/
├── .github/workflows/          # CI/CD, security, compliance workflows
├── docs/                       # Security policies & compliance documentation
│   ├── policies/               #   NIST 800-171 policy documents
│   ├── POA&M.md                #   Plan of Action & Milestones
│   ├── ssdf-attestation.md     #   SSDF v1.1 self-attestation
│   └── ...                     #   IRP, vulnerability mgmt, data classification
├── documentation/              # Architecture & developer documentation
│   ├── README.md               #   Documentation hub
│   └── microservices.md        #   Service map & architecture details
├── TheWatch.P1–P11.*/          # Microservice projects (12 services)
├── TheWatch.P*.Tests/          # Per-service unit & integration tests
├── TheWatch.Contracts.*/       # Typed client contract libraries (12)
├── TheWatch.Shared/            # Cross-cutting shared library
├── TheWatch.Generators/        # Roslyn source generators
├── TheWatch.Admin.RestAPI/     # Aggregated REST API gateway
├── TheWatch.Admin/             # Blazor Server admin portal
├── TheWatch.Admin.CLI/         # PowerShell CLI module
├── TheWatch.Dashboard/         # Blazor Server ops dashboard
├── TheWatch.Mobile/            # MAUI Blazor Hybrid mobile app
├── TheWatch.Aspire.*/          # .NET Aspire orchestration
├── docker/                     # Per-service Dockerfiles
├── helm/                       # Kubernetes Helm charts
├── terraform/                  # Multi-cloud IaC (Azure, AWS, GCP)
├── infra/                      # Cloudflare, K8s manifests, monitoring
├── scripts/                    # Build & development utility scripts
├── docker-compose*.yml         # Local, staging, production compose files
├── TheWatch.sln                # Solution file (54+ projects)
├── Directory.Build.props       # Shared MSBuild properties
└── Directory.Packages.props    # Central package version management
```

---

## Related Repositories

- [`watch-two`](https://github.com/erhebend-tai/watch-two) — placeholder repo; currently contains only a README and no assets to import.

---

## Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/) (preview)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/)
- SQL Server 2022 (via Docker or local)

### Build & Run

```bash
# 1. Clone the repository
git clone https://github.com/erhebend-tai/TheWatch-Microservices.git
cd TheWatch-Microservices

# 2. Restore dependencies
dotnet restore TheWatch.sln

# 3. Build a specific service
dotnet build TheWatch.P1.CoreGateway/TheWatch.P1.CoreGateway.csproj -c Release

# 4. Run tests for a service
dotnet test TheWatch.P1.CoreGateway.Tests/TheWatch.P1.CoreGateway.Tests.csproj

# 5. Launch local stack via Aspire
dotnet run --project TheWatch.Aspire.AppHost/TheWatch.Aspire.AppHost.csproj

# 6. Or use Docker Compose for the full environment
docker compose up -d
```

For detailed developer setup including commit signing and branch protection, see
[`docs/developer-setup.md`](docs/developer-setup.md).

---

## Compliance & Security

TheWatch is designed to meet DoD security requirements. The following standards
and frameworks guide development and operations:

| Standard | Status | Reference |
|---|---|---|
| CMMC 2.0 Level 2 | In Progress | [`DOD_SECURITY_ANALYSIS.md`](DOD_SECURITY_ANALYSIS.md) |
| NIST SP 800-171 Rev 2 | In Progress | [`docs/policies/`](docs/policies/) |
| NIST SP 800-218 (SSDF) | Attested | [`docs/ssdf-attestation.md`](docs/ssdf-attestation.md) |
| NIST SP 800-53 Rev 5 | In Progress | [`docs/POA&M.md`](docs/POA%26M.md) |
| DISA STIG | Partial | Container hardening implemented |
| EO 14028 | Partial | SBOM generation, supply chain signing |
| DFARS 252.204-7012 | In Progress | CUI protection controls |

### Key Security Controls

- **Authentication**: JWT + Argon2id password hashing + MFA (7 methods) + RBAC
- **Container Security**: Non-root user (UID 1001) per DISA STIG V-222425
- **Supply Chain**: CycloneDX/SPDX SBOM generation, Cosign image signing, SLSA provenance
- **Scanning**: CodeQL SAST, Trivy container scanning, Gitleaks secret detection, Grype vulnerability scanning
- **Policies**: Access control, audit/accountability, identification/authentication per NIST 800-171

For the full security gap analysis and remediation plan, see
[`DOD_SECURITY_ANALYSIS.md`](DOD_SECURITY_ANALYSIS.md) and
[`docs/POA&M.md`](docs/POA%26M.md).

---

## Operations & Deployment

| Environment | Method | Configuration |
|---|---|---|
| Local | Docker Compose / Aspire | `docker-compose.yml` |
| Staging | Helm → AKS | `.github/workflows/deploy-staging.yml` |
| Production | Helm → AKS (approval gates) | `.github/workflows/deploy-production.yml` |

### CI/CD Pipelines

| Workflow | Purpose |
|---|---|
| `ci.yml` | Per-project build & test on main/develop |
| `security.yml` | CodeQL, dependency review, secret detection |
| `docker-publish.yml` | Container builds with Trivy + Cosign |
| `dod-compliance.yml` | Automated DoD compliance checks |
| `sbom-aggregate.yml` | SBOM generation & vulnerability scanning |
| `slsa-provenance.yml` | SLSA provenance attestation |

---

## Documentation

| Document | Location | Description |
|---|---|---|
| Architecture & Service Map | [`documentation/microservices.md`](documentation/microservices.md) | Detailed service architecture |
| Documentation Hub | [`documentation/README.md`](documentation/README.md) | Developer documentation index |
| Security Analysis | [`DOD_SECURITY_ANALYSIS.md`](DOD_SECURITY_ANALYSIS.md) | CMMC/NIST gap analysis |
| SSDF Attestation | [`docs/ssdf-attestation.md`](docs/ssdf-attestation.md) | Supply chain security attestation |
| Plan of Action & Milestones | [`docs/POA&M.md`](docs/POA%26M.md) | Security finding remediation tracker |
| Data Classification | [`docs/data-classification-matrix.md`](docs/data-classification-matrix.md) | CUI/HIPAA data handling |
| Incident Response Plan | [`docs/incident-response-plan.md`](docs/incident-response-plan.md) | Security incident procedures |
| Security Policies | [`docs/policies/`](docs/policies/) | NIST 800-171 policy documents |
| Developer Setup | [`docs/developer-setup.md`](docs/developer-setup.md) | Environment setup & signing |
| Vulnerability Management | [`docs/vulnerability-management-policy.md`](docs/vulnerability-management-policy.md) | Vulnerability handling procedures |
| Penetration Testing | [`docs/pentest-program.md`](docs/pentest-program.md) | Pentest program & scope |
| Government Data Sources | [`docs/government-data-sources.md`](docs/government-data-sources.md) | Cited U.S. government datasets: crimes, injuries, disasters |
| Project Roadmap | [`ROADMAP.md`](ROADMAP.md) | Development status & milestones |

---

## Contributing

Please read [`CONTRIBUTING.md`](CONTRIBUTING.md) for contribution guidelines,
coding standards, and the review process. All contributions must comply with the
security and compliance requirements documented in this repository.

---

## Security Policy

For vulnerability reporting procedures, see [`SECURITY.md`](SECURITY.md).

---

## License

See [`LICENSE`](LICENSE) for terms.
