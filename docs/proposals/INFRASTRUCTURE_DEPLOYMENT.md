# Infrastructure & Deployment — TheWatch Platform

> **Classification:** CUI // SP-BASIC  
> **Reference Standards:** NIST SP 800-53 Rev 5 (CM, SC, CP families), DISA STIG,
> FedRAMP, CISA Cloud Security  
> **Document ID:** INFRA-001  
> **Version:** 1.0

---

## 1. Deployment Architecture Overview

TheWatch employs a multi-cloud, container-native deployment architecture with
Infrastructure as Code (IaC) across three cloud providers and Kubernetes orchestration.

### 1.1 Environment Summary

| Environment | Purpose | Platform | Automation |
|-------------|---------|----------|-----------|
| **Local Development** | Developer workstation | .NET Aspire AppHost + Docker Compose | `dotnet run` |
| **CI/CD** | Build, test, scan | GitHub Actions (11 workflows) | Automated on push/PR |
| **Staging** | Pre-production validation | Azure Container Apps / Kubernetes | `deploy-staging.yml` |
| **Production** | Live operations | Azure Container Apps / Kubernetes | `deploy-production.yml` (gated) |

### 1.2 Cloud Provider Strategy

| Provider | Role | IaC Modules | Status |
|----------|------|-------------|--------|
| **Azure** | Primary cloud | 7 Terraform modules (SQL, Cosmos, Redis, Service Bus, Key Vault, Container Apps, Storage) | **[IMPLEMENTED]** |
| **AWS** | Secondary / DR | 20 Terraform modules | **[IMPLEMENTED]** |
| **GCP** | Tertiary option | 9 Terraform modules | **[PARTIAL]** |
| **Cloudflare** | Edge security | WAF, Zero Trust, Argo tunnels, CDN, worker auth | **[PARTIAL]** |

---

## 2. Container Security

### 2.1 Docker Image Hardening

All 12 Dockerfiles follow a DISA STIG-compliant hardening pattern:

| Control | Implementation | DISA STIG | NIST 800-53 |
|---------|---------------|-----------|-------------|
| **Non-root execution** | `USER appuser` (UID 1001) | V-222425 | AC-6 |
| **Multi-stage builds** | SDK in build stage; runtime-only in final image | — | CM-7 |
| **Minimal base image** | `aspnet:10.0-preview` runtime (no SDK, no shell tools) | — | CM-7 |
| **No SETUID/SETGID** | Non-root user has no elevated permissions | V-222425 | AC-6(10) |
| **Read-only filesystem** | Planned via Kubernetes `readOnlyRootFilesystem` | — | AC-6 |
| **Health checks** | `/healthz` and `/ready` endpoints | — | SI-4 |
| **Port restriction** | Single port 8080 exposed per container | — | SC-7 |
| **No secrets in image** | Environment variable injection; Key Vault planned | — | SC-28 |

### 2.2 Container Scanning

| Scanner | Trigger | Scope | Action on Finding |
|---------|---------|-------|-------------------|
| **Trivy** | CI/CD pipeline | OS packages + application dependencies | Build fails on HIGH/CRITICAL |
| **NuGet Audit** | Every `dotnet restore` | .NET NuGet packages | Build fails on LOW+ |
| **GitHub Dependency Review** | Pull request | Dependency diff | PR annotated |
| **SBOM Scan** | Release builds | Full dependency graph | Vulnerability report |

### 2.3 Container Registry

| Attribute | Configuration | Standard |
|-----------|--------------|----------|
| **Registry** | Azure Container Registry (ACR) | NIST 800-53 AC-3 |
| **Authentication** | Azure Managed Identity / service principal | NIST 800-53 IA-2 |
| **Image Signing** | Cosign signing planned | NIST 800-53 SI-7 |
| **Image Scanning** | Trivy + ACR built-in scanning | NIST 800-53 RA-5 |
| **Access Control** | Repository-scoped tokens; pull-only for deployments | NIST 800-53 AC-6 |
| **Geo-Replication** | Multi-region replication for DR | NIST 800-53 CP-9 |

---

## 3. Kubernetes Architecture

### 3.1 Cluster Configuration

| Component | Configuration | Standard |
|-----------|--------------|----------|
| **Cluster Provider** | AKS (primary), EKS (secondary), GKE (tertiary) | FedRAMP |
| **Helm Charts** | `helm/thewatch/` with per-cloud values files | NIST 800-53 CM-2 |
| **Namespace Isolation** | Service-specific namespaces (planned) | NIST 800-53 SC-7 |
| **RBAC** | Kubernetes RBAC aligned with application roles | NIST 800-53 AC-3 |
| **Network Policies** | Planned (POA&M item) | NIST 800-53 SC-7(5) |
| **Pod Security** | Non-root containers; security contexts | DISA STIG V-222425 |

### 3.2 Auto-Scaling

| Scaler | Tool | Configuration | Standard |
|--------|------|--------------|----------|
| **Pod Auto-Scaling** | KEDA | Event-driven scaling (1-20 replicas) | NIST 800-53 CP-2 |
| **Node Auto-Scaling** | Karpenter | On-demand node provisioning | NIST 800-53 CP-2 |
| **Pod Disruption Budgets** | Kubernetes PDB | Minimum availability during updates | NIST 800-53 CP-2 |

**Evidence:** `infra/kubernetes/keda-scalers.yaml`, `infra/kubernetes/karpenter-nodepools.yaml`, `infra/kubernetes/pod-disruption-budgets.yaml`

### 3.3 Helm Values per Cloud

| Values File | Cloud | Key Differences |
|-------------|-------|----------------|
| `values.yaml` | Default | Base configuration for all clouds |
| `values-aks.yaml` | Azure AKS | Azure-specific storage classes, load balancers |
| `values-eks.yaml` | AWS EKS | AWS ALB ingress, EBS storage |
| `values-gke.yaml` | GCP GKE | GCP load balancer, persistent disks |

**Evidence:** `helm/thewatch/`

---

## 4. Infrastructure as Code (Terraform)

### 4.1 Azure Infrastructure (Primary)

| Module | Resources | Security Controls | Standard |
|--------|-----------|-------------------|----------|
| **SQL Database** | 11 MSSQL 2022 databases; geo-replication in production; critical/standard tiering | TDE planned; firewall rules; private endpoints | NIST 800-53 SC-28 |
| **Cosmos DB** | MongoDB API; multi-region write | Encryption at rest; RBAC; VNet integration | NIST 800-53 SC-28 |
| **Redis Cache** | Session store & rate limiting | AUTH planned; TLS planned; VNet integration | NIST 800-53 SC-8 |
| **Service Bus** | 10 topics; inter-service messaging | TLS enforced; SAS keys; managed identity | NIST 800-53 SC-8 |
| **Key Vault** | Secrets, certificates, JWT keys | HSM-backed (planned); access policies; audit logging | NIST 800-53 SC-12 |
| **Container Apps** | 14 services; auto-scaling (1-20) | Managed identity; TLS termination; VNet integration | NIST 800-53 CM-7 |
| **Storage Account** | Blob storage for evidence files | Encryption at rest; access tiers; lifecycle policies | NIST 800-53 SC-28 |

### 4.2 AWS Infrastructure (Secondary)

| Module Count | Category | Examples |
|-------------|----------|---------|
| 20 modules | Compute, Network, Storage, Security, Monitoring | ECS/EKS, VPC, S3, IAM, CloudWatch |

### 4.3 GCP Infrastructure (Tertiary)

| Module Count | Category | Examples |
|-------------|----------|---------|
| 9 modules | Compute, Network, Storage | GKE, VPC, Cloud Storage |

### 4.4 IaC Security Controls

| Control | Implementation | Standard |
|---------|---------------|----------|
| **State Encryption** | Terraform state encrypted at rest in backend | NIST 800-53 SC-28 |
| **Secret Injection** | Variables for sensitive values; no hardcoded secrets | NIST 800-53 SC-28 |
| **Change Review** | `terraform plan` reviewed before `apply` | NIST 800-53 CM-3 |
| **Version Pinning** | Provider versions pinned in configuration | NIST 800-53 CM-2 |
| **Least Privilege** | Service principals scoped to required resources | NIST 800-53 AC-6 |
| **Audit Trail** | Terraform state changes tracked in version control | NIST 800-53 AU-3 |

---

## 5. CI/CD Pipeline Security

### 5.1 Workflow Architecture

| Workflow | Trigger | Purpose | Security Controls | Standard |
|----------|---------|---------|-------------------|----------|
| **ci.yml** | Push/PR to main/develop | Per-project build & test | NuGet audit, .NET analyzers, concurrency groups | NIST 800-53 SA-11 |
| **security.yml** | Push/PR | Security scanning | CodeQL SAST, dependency review, secret scanning | NIST 800-53 SA-11 |
| **dod-compliance.yml** | Push to main | DoD compliance checks | STIG validation, container non-root check | CMMC Level 2 |
| **dod-readiness.yml** | Push to main | DoD readiness assessment | CMMC/NIST control validation | CMMC Level 2 |
| **sbom-aggregate.yml** | Release | SBOM generation & scanning | CycloneDX + SPDX; vulnerability cross-reference | EO 14028 |
| **slsa-provenance.yml** | Release | Supply chain provenance | SLSA attestation generation | SLSA v1.0 |
| **docker-publish.yml** | Push to main | Container image builds | Multi-stage builds; non-root; Trivy scan | DISA STIG |
| **deploy-staging.yml** | Manual/merge | Staging deployment | Smoke tests; health checks; rollback | NIST 800-53 CM-3 |
| **deploy-production.yml** | Manual with approval | Production deployment | Approval gate; canary deployment; monitoring | NIST 800-53 CM-3 |
| **nuget-publish.yml** | Tag push | NuGet package publishing | Package signing; version verification | NIST 800-53 SI-7 |
| **docs-pages.yml** | Push to main | Documentation publishing | GitHub Pages from documentation/ folder | — |

### 5.2 Pipeline Security Controls

| Control | Implementation | Standard |
|---------|---------------|----------|
| **Explicit Permissions** | All workflows declare minimal `permissions` blocks | NIST 800-53 AC-6 |
| **Concurrency Groups** | `cancel-in-progress` prevents duplicate runs | NIST 800-53 CM-3 |
| **Action Version Pinning** | `actions/checkout@v4`, `actions/cache@v4`, `actions/upload-artifact@v4` | NIST 800-53 CM-2 |
| **Ephemeral Runners** | GitHub-hosted runners; no persistent state | NIST 800-53 SC-7 |
| **Secret Management** | GitHub Secrets for credentials; no hardcoded values | NIST 800-53 SC-28 |
| **Artifact Retention** | 1d builds, 30d reports, 90d SBOMs | NIST 800-53 AU-11 |
| **Per-Project Strategy** | Build per project to avoid OOM (D017) | Operational reliability |
| **Branch Protection** | 2 approvals, signed commits, linear history, status checks | NIST 800-53 CM-3 |

### 5.3 Deployment Pipeline

```
Developer Commit (signed)
  → PR Review (2 approvals required)
    → CI Pipeline (build + test + scan)
      → Security Pipeline (CodeQL + Trivy + Gitleaks)
        → Docker Build (multi-stage, non-root)
          → Container Scan (Trivy)
            → SBOM Generation (CycloneDX)
              → Registry Push (ACR)
                → Staging Deploy (smoke tests)
                  → Production Deploy (approval gate)
                    → Health Check Verification
                      → Monitoring (Prometheus + Grafana)
```

---

## 6. Monitoring & Observability Infrastructure

### 6.1 Monitoring Stack

| Component | Tool | Purpose | Standard |
|-----------|------|---------|----------|
| **Metrics** | Prometheus | Service metrics collection | NIST 800-53 SI-4 |
| **Dashboards** | Grafana | Visualization and alerting | NIST 800-53 SI-4 |
| **Alerting** | AlertManager | Alert routing and notification | NIST 800-53 SI-4 |
| **Logging** | Serilog (structured JSON) | Application logging | NIST 800-53 AU-3 |
| **Tracing** | Correlation IDs | Cross-service request tracing | NIST 800-53 AU-12 |
| **Health Checks** | ASP.NET Core Health Checks | Service availability | NIST 800-53 SI-4 |

**Evidence:** `infra/monitoring/` (Prometheus, Grafana, AlertManager configurations)

### 6.2 Monitoring Agents

TheWatch includes 10 specialized monitoring agents:

| Agent | Purpose | Standard |
|-------|---------|----------|
| Schema Monitor | Database schema drift detection | NIST 800-53 CM-3 |
| Security Monitor | Security configuration compliance | NIST 800-53 CA-7 |
| API Quality Monitor | API response quality and SLA | NIST 800-53 SI-4 |
| Compliance Monitor | Regulatory compliance checks | CMMC Level 2 |
| Performance Monitor | Latency and throughput tracking | NIST 800-53 SI-4 |

---

## 7. Edge Security (Cloudflare)

| Component | Configuration | Standard |
|-----------|--------------|----------|
| **WAF** | Rule-based web application firewall | NIST 800-53 SC-7 |
| **Zero Trust** | Cloudflare Access for admin interfaces | NIST 800-207 |
| **Argo Tunnels** | Encrypted tunnels to origin servers | NIST 800-53 SC-8 |
| **CDN** | Static asset caching with security headers | NIST 800-53 SC-7 |
| **Worker Auth** | Edge authentication with workers | NIST 800-53 IA-2 |

**Evidence:** `infra/cloudflare/`

---

## 8. Disaster Recovery & High Availability

### 8.1 High Availability Controls

| Component | HA Strategy | RTO | RPO | Standard |
|-----------|-------------|-----|-----|----------|
| **Microservices** | Multi-replica (1-20); PDB | < 5 min | 0 (stateless) | NIST 800-53 CP-7 |
| **SQL Server** | Geo-replication (production) | < 15 min | < 5 min | NIST 800-53 CP-9 |
| **Cosmos DB** | Multi-region write | < 1 min | 0 | NIST 800-53 CP-9 |
| **Kafka** | Multi-broker with replication | < 5 min | < 1 min | NIST 800-53 CP-9 |
| **Redis** | Sentinel HA cluster | < 5 min | < 1 min | NIST 800-53 CP-9 |
| **Container Registry** | Geo-replicated ACR | < 5 min | 0 | NIST 800-53 CP-9 |

### 8.2 Backup Strategy

| Data Store | Backup Method | Frequency | Retention | Encryption | Standard |
|------------|--------------|-----------|-----------|------------|----------|
| **SQL Server** | Azure automated backup | Continuous | 35 days | AES-256 | NIST 800-53 CP-9 |
| **Cosmos DB** | Continuous backup | Continuous | 30 days | Default | NIST 800-53 CP-9 |
| **PostgreSQL** | pg_dump / cloud backup | Daily | 30 days | AES-256 | NIST 800-53 CP-9 |
| **Evidence Files** | Blob storage replication | Continuous | Per policy | AES-256 | NIST 800-53 CP-9 |
| **Configuration** | Git version control | Every commit | Indefinite | N/A | NIST 800-53 CP-9 |

---

## 9. Network Architecture

### 9.1 Network Segmentation

```
┌────────────────────────────────────────────────────────────────┐
│                    INTERNET / EDGE                              │
│  Cloudflare WAF → CDN → Zero Trust → Argo Tunnel              │
├────────────────────────────────────────────────────────────────┤
│                    DMZ / INGRESS                                │
│  Kubernetes Ingress Controller (TLS termination)               │
├────────────────────────────────────────────────────────────────┤
│                    APPLICATION TIER                              │
│  P1 CoreGateway (public-facing)                                │
│  ├── P2-P11 Microservices (internal only)                      │
│  ├── Geospatial Engine (internal only)                         │
│  └── Dashboard / Admin (internal only)                         │
├────────────────────────────────────────────────────────────────┤
│                    DATA TIER                                    │
│  SQL Server │ PostgreSQL │ MongoDB │ Redis │ Kafka             │
│  (private endpoints; no public access)                         │
└────────────────────────────────────────────────────────────────┘
```

### 9.2 Network Security Controls

| Control | Implementation | Standard |
|---------|---------------|----------|
| **Private Endpoints** | Database services on private VNet | NIST 800-53 SC-7 |
| **Security Groups** | NSG/SG rules per tier | NIST 800-53 SC-7(5) |
| **Ingress Control** | Kubernetes ingress with TLS | NIST 800-53 SC-7 |
| **Egress Control** | Planned (NSG egress rules) | NIST 800-53 SC-7 |
| **Service Mesh** | mTLS between services (planned) | NIST 800-53 SC-8(1) |
| **DNS Security** | Cloudflare DNSSEC | NIST 800-53 SC-20 |

---

*This Infrastructure & Deployment document provides a comprehensive view of TheWatch's
multi-cloud, container-native architecture with security controls mapped to DoD standards.*
