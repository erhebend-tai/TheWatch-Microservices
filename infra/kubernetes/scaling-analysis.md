# TheWatch Kubernetes Scaling Analysis

> Generated: 2026-02-26 | Platform: TheWatch Microservices (12 services, .NET 10)

---

## 1. Current State

### Existing Infrastructure
- **Helm chart**: `helm/thewatch/` with HPA templates for P2 and P6
- **Docker images**: Multi-stage .NET 10 builds for all 12 services
- **Terraform**: Full IaC for Azure, AWS (20 modules including ECS Fargate), GCP (Cloud Run)
- **HPA**: autoscaling/v2 on CPU/memory for P2-VoiceEmergency and P6-FirstResponder

### Service Scaling Profiles

| Service | Base Replicas | HPA | Max Replicas | Criticality | Burst Pattern |
|---------|:---:|:---:|:---:|:---:|---|
| P1 CoreGateway | 2 | No | - | High | Steady, proportional to total traffic |
| P2 VoiceEmergency | 3 | **Yes** | 20 | **Critical** | Massive spikes during incidents |
| P3 MeshNetwork | 2 | No | - | Medium | Proportional to connected devices |
| P4 Wearable | 2 | No | - | Medium | Peaks at health-check intervals |
| P5 AuthSecurity | 2 | No | - | **Critical** | Spikes at login/registration surges |
| P6 FirstResponder | 3 | **Yes** | 15 | **Critical** | Dispatches during emergencies |
| P7 FamilyHealth | 2 | No | - | High | Scheduled check-in patterns |
| P8 DisasterRelief | 2 | No | - | High | Extreme spikes during disasters |
| P9 DoctorServices | 2 | No | - | Medium | Appointment scheduling patterns |
| P10 Gamification | 1 | No | - | Low | Proportional to active users |
| P11 Surveillance | 2 | No | - | High | Continuous (CCTV stream processing) |
| Geospatial | 2 | No | - | High | Query-driven, burst on map loads |
| Dashboard | 2 | No | - | Medium | Admin traffic patterns |

---

## 2. Scaling Gaps & Recommendations

### Services That Need HPA/KEDA Added

**P1 CoreGateway** — Gateway fan-out means it scales with total platform traffic.
- Recommendation: HPA on CPU (70%) + request rate (KEDA HTTP trigger)
- Min: 2, Max: 10

**P5 AuthSecurity** — Token issuance is CPU-intensive (JWT signing).
- Recommendation: HPA on CPU (60%) + KEDA Prometheus trigger on `auth_requests_per_second`
- Min: 2, Max: 12

**P8 DisasterRelief** — Dormant normally, extreme spike during events.
- Recommendation: KEDA with Kafka trigger (message backlog) + scheduled scaler for hurricane season
- Min: 1 (idle), Max: 25 (disaster)

**P11 Surveillance** — GPU-bound for CCTV object recognition.
- Recommendation: HPA on GPU utilization (custom metrics) + KEDA Kafka trigger on frame backlog
- Min: 2, Max: 8 (limited by GPU availability)

**Geospatial** — PostGIS queries are memory-intensive.
- Recommendation: HPA on memory (70%) + CPU (65%)
- Min: 2, Max: 8

---

## 3. Standalone Kubernetes (Self-Managed)

### Architecture
```
┌─────────────────────────────────────────────────────┐
│                  Bare-Metal / VM Cluster             │
│                                                     │
│  ┌───────────┐  ┌───────────┐  ┌───────────┐       │
│  │  Master 1  │  │  Master 2  │  │  Master 3  │  HA  │
│  └─────┬─────┘  └─────┬─────┘  └─────┬─────┘       │
│        └───────────────┼───────────────┘             │
│                        │                             │
│  ┌─────────┐ ┌─────────┐ ┌─────────┐ ┌─────────┐   │
│  │Worker 1 │ │Worker 2 │ │Worker 3 │ │Worker N │   │
│  │(CPU)    │ │(CPU)    │ │(CPU)    │ │(GPU)    │   │
│  └─────────┘ └─────────┘ └─────────┘ └─────────┘   │
│                                                     │
│  MetalLB (LoadBalancer) │ Longhorn (Storage)        │
│  Calico (CNI)           │ cert-manager (TLS)        │
│  KEDA (Autoscaling)     │ Prometheus (Metrics)      │
└─────────────────────────────────────────────────────┘
```

### Minimum Node Requirements
| Role | Count | CPU | RAM | Disk | Notes |
|------|:---:|:---:|:---:|:---:|---|
| Control Plane | 3 | 4 vCPU | 8 GB | 100 GB SSD | HA etcd, stacked topology |
| Worker (CPU) | 4 | 8 vCPU | 32 GB | 200 GB SSD | P1-P10, Dashboard, Geospatial |
| Worker (GPU) | 1 | 8 vCPU | 32 GB | 200 GB SSD | P11 Surveillance (NVIDIA T4/A10) |
| Infra (DB) | 2 | 4 vCPU | 16 GB | 500 GB NVMe | SQL Server, PostgreSQL, Redis |

### Pros
- Full control over hardware and configuration
- No cloud vendor lock-in
- Lowest per-unit compute cost at scale
- Can place clusters at the network edge (field deployment)

### Cons
- Full ops burden: upgrades, patching, etcd backups, certificate rotation
- Must self-manage storage (Longhorn/Rook-Ceph), networking (Calico/Cilium), load balancing (MetalLB)
- No managed node auto-provisioning — Cluster Autoscaler requires cloud integration
- GPU driver management and NVIDIA device plugin maintenance

### Scaling Strategy
- **HPA + KEDA** for pod-level scaling (see Section 6)
- **Cluster Autoscaler**: Not available without cloud — must manually add nodes or use Rancher/kubeadm
- **VPA (Vertical Pod Autoscaler)**: Useful for right-sizing over time
- **PDB (Pod Disruption Budgets)**: Ensure min availability during node maintenance

---

## 4. Azure Kubernetes Service (AKS)

### Architecture
```
┌──────────────────────────────────────────────────────┐
│              Azure Resource Group                     │
│                                                      │
│  ┌─────────────────────────────────────────┐         │
│  │            AKS Cluster                  │         │
│  │  ┌─────────────┐  ┌──────────────────┐  │         │
│  │  │ System Pool  │  │  App Pool (CPU)  │  │         │
│  │  │ 3x D4s_v5    │  │  4x D8s_v5       │  │         │
│  │  │ (tainted)    │  │  (auto-scale 2-8)│  │         │
│  │  └─────────────┘  └──────────────────┘  │         │
│  │  ┌──────────────────┐  ┌────────────┐   │         │
│  │  │ GPU Pool         │  │ Spot Pool  │   │         │
│  │  │ 1x NC6s_v3       │  │ D4s_v5     │   │         │
│  │  │ (P11 Surveillance)│  │ (burst)    │   │         │
│  │  └──────────────────┘  └────────────┘   │         │
│  └─────────────────────────────────────────┘         │
│                                                      │
│  Azure SQL Managed Instance  │  Azure Cache Redis    │
│  Azure Cosmos DB (Postgres)  │  Azure Service Bus    │
│  Azure Key Vault             │  Azure Monitor        │
│  Cloudflare Tunnel (egress)  │  Azure Front Door     │
└──────────────────────────────────────────────────────┘
```

### Node Pool Strategy
| Pool | VM SKU | Min | Max | Purpose | Cost/mo (est.) |
|------|--------|:---:|:---:|---------|---:|
| system | Standard_D4s_v5 | 3 | 3 | Core K8s components, monitoring | $438 |
| apppool | Standard_D8s_v5 | 2 | 8 | P1-P10, Dashboard, Geospatial | $584-$2,336 |
| gpupool | Standard_NC6s_v3 | 0 | 2 | P11 Surveillance | $0-$1,752 |
| spotpool | Standard_D4s_v5 | 0 | 10 | Burst capacity (75% discount) | $0-$365 |

### AKS-Specific Features
- **KEDA add-on**: First-class AKS integration (managed KEDA operator)
- **Virtual Nodes (ACI)**: Serverless burst to Azure Container Instances — instant scale without node provisioning
- **Cluster Autoscaler**: Native node-level auto-scaling per pool
- **Workload Identity**: Azure AD pod identity for secret-free Azure service access
- **Azure CNI Overlay**: Efficient IP management for large clusters
- **Azure Monitor + Container Insights**: Built-in Prometheus-compatible monitoring

### Estimated Monthly Cost (Production)
| Component | Cost |
|-----------|-----:|
| AKS cluster (free tier control plane) | $0 |
| Node pools (steady state) | ~$1,500 |
| Azure SQL MI (General Purpose 8 vCore) | ~$800 |
| Azure Cache Redis (C2) | ~$160 |
| Azure Service Bus (Standard) | ~$10 |
| Azure Key Vault | ~$5 |
| Networking / LB | ~$50 |
| **Total (steady)** | **~$2,525/mo** |
| **Total (burst peak)** | **~$4,800/mo** |

---

## 5. Amazon Elastic Kubernetes Service (EKS)

### Architecture
```
┌──────────────────────────────────────────────────────┐
│              AWS VPC (3 AZ)                          │
│                                                      │
│  ┌─────────────────────────────────────────┐         │
│  │              EKS Cluster                │         │
│  │  ┌──────────────┐  ┌────────────────┐   │         │
│  │  │ Managed NG    │  │ Karpenter Pool │   │         │
│  │  │ 4x m6i.2xl   │  │ (auto-provision)│   │         │
│  │  │ (base fleet)  │  │ spot + on-demand│   │         │
│  │  └──────────────┘  └────────────────┘   │         │
│  │  ┌──────────────────┐                    │         │
│  │  │ GPU NG           │                    │         │
│  │  │ g5.xlarge        │                    │         │
│  │  │ (P11 Surveillance)│                    │         │
│  │  └──────────────────┘                    │         │
│  └─────────────────────────────────────────┘         │
│                                                      │
│  RDS SQL Server  │  Aurora PostgreSQL (PostGIS)      │
│  ElastiCache     │  MSK (Kafka)                      │
│  Secrets Manager │  CloudWatch + X-Ray               │
│  ALB Ingress     │  Cloudflare Tunnel (egress)       │
│  Karpenter       │  KEDA (self-managed)              │
└──────────────────────────────────────────────────────┘
```

### EKS-Specific Features
- **Karpenter**: AWS-native node provisioner — far more responsive than Cluster Autoscaler. Provisions right-sized nodes in ~60s
- **Fargate profiles**: Serverless pods (no node management) for batch/cron workloads
- **EKS Pod Identity**: IAM roles per pod for secret-free AWS service access
- **AWS App Mesh**: Service mesh with Envoy sidecar for mTLS and observability
- **Spot Instances**: Up to 90% savings for fault-tolerant services (P10, Dashboard)
- **Graviton (ARM)**: arm64 nodes at 20% cost savings — .NET 10 supports ARM natively

### Karpenter Node Provisioner
```yaml
# Karpenter replaces Cluster Autoscaler — provisions optimal nodes on-demand
apiVersion: karpenter.sh/v1beta1
kind: NodePool
metadata:
  name: thewatch-general
spec:
  template:
    spec:
      requirements:
        - key: karpenter.sh/capacity-type
          operator: In
          values: ["spot", "on-demand"]
        - key: node.kubernetes.io/instance-type
          operator: In
          values: ["m6i.xlarge", "m6i.2xlarge", "m7g.xlarge", "m7g.2xlarge"]
        - key: topology.kubernetes.io/zone
          operator: In
          values: ["us-east-1a", "us-east-1b", "us-east-1c"]
  limits:
    cpu: "128"
    memory: 512Gi
  disruption:
    consolidationPolicy: WhenUnderutilized
    expireAfter: 720h
```

### Estimated Monthly Cost (Production)
| Component | Cost |
|-----------|-----:|
| EKS control plane | $73 |
| EC2 nodes (m6i.2xlarge x4 on-demand) | ~$1,120 |
| Spot burst capacity | ~$200 |
| RDS SQL Server (db.m6i.xlarge) | ~$540 |
| Aurora PostgreSQL (db.r6g.large) | ~$330 |
| ElastiCache Redis (r6g.large) | ~$200 |
| MSK (kafka.m5.large x3) | ~$450 |
| ALB + data transfer | ~$100 |
| **Total (steady)** | **~$3,013/mo** |
| **Total (burst peak)** | **~$4,500/mo** |

---

## 6. Google Kubernetes Engine (GKE)

### Architecture
```
┌──────────────────────────────────────────────────────┐
│              GCP Project                             │
│                                                      │
│  ┌─────────────────────────────────────────┐         │
│  │           GKE Autopilot                 │         │
│  │    (fully managed node provisioning)    │         │
│  │                                         │         │
│  │    Pay-per-pod, no node management      │         │
│  │    Auto bin-packing, auto-scaling       │         │
│  │    GPU pods provisioned on demand       │         │
│  └─────────────────────────────────────────┘         │
│                                                      │
│  Cloud SQL (SQL Server) │  AlloyDB (PostgreSQL)      │
│  Memorystore Redis      │  Pub/Sub                   │
│  Secret Manager         │  Cloud Monitoring          │
│  Cloud Armor (WAF)      │  Cloudflare Tunnel         │
│  GKE Gateway API        │  KEDA (managed via GMP)    │
└──────────────────────────────────────────────────────┘
```

### GKE Modes Comparison
| Feature | GKE Standard | GKE Autopilot |
|---------|:---:|:---:|
| Node management | Manual | Fully managed |
| Cluster Autoscaler | Self-configure | Built-in |
| Pod-level billing | No (node-level) | **Yes** |
| GPU support | Full control | On-demand provisioning |
| Spot/preemptible | Manual config | Automatic selection |
| Security posture | Self-harden | Hardened by default |
| **Recommendation** | Staging/dev | **Production** |

### GKE Autopilot Advantages for TheWatch
- **No node pool management**: Autopilot provisions exact resources per pod
- **Built-in security**: Workload Identity, shielded nodes, Binary Authorization
- **Cost optimization**: Pay per pod resource request, not per node
- **Automatic bin-packing**: No wasted capacity
- **SLA**: 99.95% for regional, 99.5% for zonal

### Estimated Monthly Cost (Production — Autopilot)
| Component | Cost |
|-----------|-----:|
| GKE Autopilot (pod resources) | ~$800 |
| Cloud SQL (SQL Server, 4 vCPU) | ~$480 |
| AlloyDB (PostgreSQL, 4 vCPU) | ~$350 |
| Memorystore Redis (6 GB) | ~$180 |
| Pub/Sub | ~$30 |
| Secret Manager | ~$5 |
| Networking | ~$60 |
| **Total (steady)** | **~$1,905/mo** |
| **Total (burst peak)** | **~$3,200/mo** |

---

## 7. KEDA Event-Driven Autoscaling

KEDA (Kubernetes Event-Driven Autoscaler) extends HPA with event-source triggers. This is the recommended scaling strategy for TheWatch because emergency services have unpredictable burst patterns.

### KEDA Scaler Configuration

```yaml
# infra/kubernetes/keda-scalers.yaml
# P2 VoiceEmergency — scale on Kafka message backlog + HTTP request rate
apiVersion: keda.sh/v1alpha1
kind: ScaledObject
metadata:
  name: p2-voiceemergency-scaler
  namespace: thewatch
spec:
  scaleTargetRef:
    name: p2-voiceemergency
  pollingInterval: 10
  cooldownPeriod: 120
  minReplicaCount: 3
  maxReplicaCount: 30
  fallback:
    failureThreshold: 3
    replicas: 5
  triggers:
    - type: kafka
      metadata:
        bootstrapServers: thewatch-kafka:9092
        consumerGroup: p2-voiceemergency
        topic: emergency-events
        lagThreshold: "50"
    - type: prometheus
      metadata:
        serverAddress: http://prometheus:9090
        metricName: http_requests_per_second
        query: sum(rate(http_requests_total{service="p2-voiceemergency"}[1m]))
        threshold: "100"
    - type: cpu
      metricType: Utilization
      metadata:
        value: "60"
---
# P5 AuthSecurity — scale on auth request throughput
apiVersion: keda.sh/v1alpha1
kind: ScaledObject
metadata:
  name: p5-authsecurity-scaler
  namespace: thewatch
spec:
  scaleTargetRef:
    name: p5-authsecurity
  pollingInterval: 15
  cooldownPeriod: 180
  minReplicaCount: 2
  maxReplicaCount: 12
  triggers:
    - type: prometheus
      metadata:
        serverAddress: http://prometheus:9090
        metricName: auth_token_requests
        query: sum(rate(auth_token_issued_total[2m]))
        threshold: "50"
    - type: cpu
      metricType: Utilization
      metadata:
        value: "60"
---
# P6 FirstResponder — scale on dispatch queue + Kafka events
apiVersion: keda.sh/v1alpha1
kind: ScaledObject
metadata:
  name: p6-firstresponder-scaler
  namespace: thewatch
spec:
  scaleTargetRef:
    name: p6-firstresponder
  pollingInterval: 10
  cooldownPeriod: 120
  minReplicaCount: 3
  maxReplicaCount: 20
  fallback:
    failureThreshold: 3
    replicas: 5
  triggers:
    - type: kafka
      metadata:
        bootstrapServers: thewatch-kafka:9092
        consumerGroup: p6-firstresponder
        topic: dispatch-events
        lagThreshold: "30"
    - type: prometheus
      metadata:
        serverAddress: http://prometheus:9090
        metricName: active_dispatches
        query: thewatch_active_dispatches
        threshold: "20"
    - type: cpu
      metricType: Utilization
      metadata:
        value: "65"
---
# P8 DisasterRelief — scale from idle to massive during events
apiVersion: keda.sh/v1alpha1
kind: ScaledObject
metadata:
  name: p8-disasterrelief-scaler
  namespace: thewatch
spec:
  scaleTargetRef:
    name: p8-disasterrelief
  pollingInterval: 15
  cooldownPeriod: 300
  idleReplicaCount: 1
  minReplicaCount: 2
  maxReplicaCount: 25
  triggers:
    - type: prometheus
      metadata:
        serverAddress: http://prometheus:9090
        metricName: disaster_active_events
        query: thewatch_disaster_active_events
        threshold: "1"
    - type: cron
      metadata:
        timezone: America/New_York
        start: 0 6 1 6 *
        end: 0 6 1 12 *
        desiredReplicas: "4"
---
# P1 CoreGateway — scale with total platform throughput
apiVersion: keda.sh/v1alpha1
kind: ScaledObject
metadata:
  name: p1-coregateway-scaler
  namespace: thewatch
spec:
  scaleTargetRef:
    name: p1-coregateway
  pollingInterval: 15
  cooldownPeriod: 180
  minReplicaCount: 2
  maxReplicaCount: 10
  triggers:
    - type: prometheus
      metadata:
        serverAddress: http://prometheus:9090
        metricName: gateway_requests_per_second
        query: sum(rate(http_requests_total{service="p1-coregateway"}[2m]))
        threshold: "200"
    - type: cpu
      metricType: Utilization
      metadata:
        value: "70"
---
# P11 Surveillance — scale on CCTV frame processing backlog
apiVersion: keda.sh/v1alpha1
kind: ScaledObject
metadata:
  name: p11-surveillance-scaler
  namespace: thewatch
spec:
  scaleTargetRef:
    name: p11-surveillance
  pollingInterval: 30
  cooldownPeriod: 300
  minReplicaCount: 2
  maxReplicaCount: 8
  triggers:
    - type: prometheus
      metadata:
        serverAddress: http://prometheus:9090
        metricName: frame_processing_backlog
        query: thewatch_surveillance_frame_queue_depth
        threshold: "500"
    - type: cpu
      metricType: Utilization
      metadata:
        value: "75"
---
# Geospatial — scale on memory pressure (PostGIS is memory-heavy)
apiVersion: keda.sh/v1alpha1
kind: ScaledObject
metadata:
  name: geospatial-scaler
  namespace: thewatch
spec:
  scaleTargetRef:
    name: geospatial
  pollingInterval: 30
  cooldownPeriod: 300
  minReplicaCount: 2
  maxReplicaCount: 8
  triggers:
    - type: memory
      metricType: Utilization
      metadata:
        value: "70"
    - type: cpu
      metricType: Utilization
      metadata:
        value: "65"
```

---

## 8. Multi-Cloud Comparison Matrix

| Dimension | Standalone | AKS | EKS | GKE Autopilot |
|-----------|:---:|:---:|:---:|:---:|
| **Monthly cost (steady)** | ~$2,000* | ~$2,525 | ~$3,013 | **~$1,905** |
| **Monthly cost (burst)** | Fixed | ~$4,800 | ~$4,500 | **~$3,200** |
| **Node provisioning speed** | Manual | ~2 min | ~60s (Karpenter) | **Instant (pod-level)** |
| **Node auto-scaling** | None | Cluster Autoscaler | Karpenter | **Built-in** |
| **KEDA integration** | Self-install | AKS add-on | Self-install | Self-install (GMP) |
| **GPU support** | Manual drivers | NC-series pools | g5/p4 instances | On-demand |
| **Spot/preemptible** | N/A | Spot pools | Spot + Karpenter | **Automatic** |
| **Serverless burst** | None | Virtual Nodes (ACI) | Fargate profiles | **Native** |
| **Managed databases** | Self-hosted | Azure SQL MI | RDS/Aurora | Cloud SQL/AlloyDB |
| **Multi-region** | Complex | Azure Front Door | Global Accelerator | Multi-cluster |
| **Control plane SLA** | Self-managed | 99.95% | 99.95% | **99.95%** |
| **Ops burden** | **Heavy** | Medium | Medium | **Low** |
| **Vendor lock-in** | None | Medium | Medium | Medium |
| **Edge deployment** | **Yes** | No | Outposts ($$) | Anthos ($$) |

*Standalone cost is hardware amortized over 36 months, excluding labor.

---

## 9. Recommendations

### Primary: GKE Autopilot
**Best for**: Production workloads where cost efficiency and minimal ops matter.
- Lowest steady-state cost at ~$1,905/mo
- Pod-level billing eliminates wasted node capacity
- Automatic security hardening (shielded nodes, workload identity)
- KEDA works well with Google Managed Prometheus

### Secondary: AKS with KEDA Add-on
**Best for**: Teams already on Azure, enterprises needing Microsoft support.
- Native KEDA integration (managed operator)
- Virtual Nodes for instant burst to ACI
- Strong .NET ecosystem integration
- Azure Front Door for global traffic management

### Tertiary: EKS with Karpenter
**Best for**: AWS-native organizations, complex multi-service architectures.
- Karpenter is the most advanced node provisioner available
- Best Spot instance integration
- Graviton ARM nodes for 20% cost savings on .NET 10
- AWS App Mesh for zero-trust service mesh

### For Field/Edge Deployment: Standalone K3s
**Best for**: Disconnected or austere environments (disaster relief, field ops).
- Use k3s (lightweight K8s) instead of full kubeadm
- Pre-bake images for offline operation
- P3 MeshNetwork enables peer-to-peer even without cloud
- Minimal footprint: 3 nodes can run the full stack

---

## 10. Implementation Priority

### Phase 1: KEDA Installation (Week 1)
1. Install KEDA operator in existing cluster
2. Deploy ScaledObjects for P2, P5, P6 (highest priority)
3. Configure Prometheus triggers for request-rate scaling
4. Test with load generation

### Phase 2: Expanded Autoscaling (Week 2)
1. Add KEDA scalers for P1, P8, P11, Geospatial
2. Configure Kafka triggers for event-driven services
3. Add PodDisruptionBudgets for all critical services
4. Tune cooldown periods and thresholds with real traffic

### Phase 3: Multi-Cloud Readiness (Week 3-4)
1. Create cloud-specific Helm value overrides: `values-aks.yaml`, `values-eks.yaml`, `values-gke.yaml`
2. Add Karpenter NodePool manifests for EKS
3. Configure GKE Autopilot resource requests
4. Set up Cloudflare Tunnel as ingress for all environments

### Phase 4: Production Hardening (Week 5-6)
1. Pod Security Standards (restricted)
2. Network Policies (Calico/Cilium)
3. mTLS via service mesh or Cloudflare Zero Trust
4. Chaos engineering tests (pod kill, node drain, zone failure)
