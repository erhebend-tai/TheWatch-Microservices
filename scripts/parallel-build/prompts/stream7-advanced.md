# Stream 7: ADVANCED — Stage 12 (TODO Items 141-150)

You are working in a git worktree of TheWatch microservices solution. This is a Wave 2 stream — Wave 1 has already been merged, so auth, Docker, cloud infrastructure, and mobile features are in place.

## YOUR ASSIGNED TODO ITEMS

### 12A. ML/AI (141-144)
141. Train gunshot detection audio classifier for P2 active shooter scenarios
142. Implement fall detection from wearable accelerometer data stream (P4)
143. Implement vital sign anomaly detection with configurable thresholds (P7)
144. Implement responder dispatch optimization (minimize response time across geography)

### 12B. Compliance (145-148)
145. Implement HIPAA-compliant data handling for P7/P9 health records (encryption at rest + transit, access logging, BAA requirements)
146. Implement COPPA compliance for P7 child data (parental consent, data minimization)
147. Implement GDPR right-to-erasure across all services (cascade delete with audit)
148. Implement SOX-expanded audit framework from memorandum (quarterly reporting, signed attestations)

### 12C. Graph & Observability (149-150)
149. Deploy Watch-GraphDB.sql graph tables (node/edge) for social graph and incident correlation
150. Wire 10 monitoring agents into CI/CD with Prometheus metrics, Grafana dashboards, and PagerDuty alerting

## FILES YOU MAY CREATE/MODIFY

### ML/AI Services:
- `TheWatch.P2.VoiceEmergency/Services/GunshotDetectionService.cs` (new)
- `TheWatch.P4.Wearable/Services/FallDetectionService.cs` (new)
- `TheWatch.P7.FamilyHealth/Services/VitalAnomalyService.cs` (new)
- `TheWatch.Geospatial/Services/DispatchOptimizationService.cs` (new)
- `TheWatch.Shared/ML/` (new directory) — shared ML model types, feature extraction

### Compliance:
- `TheWatch.Shared/Compliance/HipaaComplianceService.cs`
- `TheWatch.Shared/Compliance/CoppaComplianceService.cs`
- `TheWatch.Shared/Compliance/GdprErasureService.cs`
- `TheWatch.Shared/Compliance/SoxAuditService.cs`
- `TheWatch.Shared/Compliance/IComplianceService.cs`

### Graph DB:
- `docker/sql/init/Watch-GraphDB.sql` (graph tables)

### Observability:
- `infra/monitoring/prometheus.yml`
- `infra/monitoring/grafana/dashboards/` (JSON dashboard definitions)
- `infra/monitoring/alertmanager.yml`
- `infra/monitoring/docker-compose.monitoring.yml`

## FILES YOU MUST NOT TOUCH

- `TheWatch.P5.AuthSecurity/` (owned by auth stream)
- `TheWatch.Mobile/` (owned by mobile stream)
- `TheWatch.Dashboard/`
- `TheWatch.Aspire.AppHost/`
- `infra/bicep/` (owned by Azure stream)
- `infra/cloudflare/` (owned by GCP/CF stream)
- `.github/workflows/` (owned by Docker/K8s stream)
- Existing `helm/` directory

## IMPLEMENTATION PATTERNS

### Gunshot Detection (item 141):
Create a service that processes audio frames and classifies them. Use ML.NET or ONNX Runtime:
```csharp
public interface IGunshotDetectionService
{
    Task<GunshotDetectionResult> AnalyzeAudioFrameAsync(float[] audioSamples, int sampleRate);
}

public class GunshotDetectionResult
{
    public bool IsGunshot { get; set; }
    public float Confidence { get; set; }
    public string Classification { get; set; } = string.Empty; // "gunshot", "firework", "car_backfire", "ambient"
    public DateTime DetectedAt { get; set; }
}
```

### Fall Detection (item 142):
```csharp
public interface IFallDetectionService
{
    FallDetectionResult AnalyzeAccelerometerData(AccelerometerReading[] readings);
}

public class AccelerometerReading
{
    public float X { get; set; }
    public float Y { get; set; }
    public float Z { get; set; }
    public DateTime Timestamp { get; set; }
}
```
Detect falls using: sudden acceleration spike (>3g) followed by orientation change and stillness.

### Vital Anomaly Detection (item 143):
Extend existing VitalService in P7. Add configurable thresholds with sliding window:
```csharp
public class VitalAnomalyDetector
{
    // Sliding window of last N readings per vital type
    // Detect: sudden change (delta > threshold), sustained abnormal, trend deviation
    // Configurable per user (age, conditions, medications)
}
```

### HIPAA Compliance (item 145):
```csharp
public interface IHipaaComplianceService
{
    Task<byte[]> EncryptPhi(byte[] data); // PHI = Protected Health Information
    Task<byte[]> DecryptPhi(byte[] encryptedData);
    Task LogPhiAccess(string userId, string resourceType, string resourceId, string action);
    Task<bool> ValidateMinimumNecessary(string userId, string requestedScope);
}
```

### GDPR Erasure (item 147):
```csharp
public interface IGdprErasureService
{
    Task<ErasureResult> ProcessErasureRequest(Guid userId);
    // Must cascade across: P1 (profile), P5 (auth), P6 (responder data),
    // P7 (family), P9 (medical), P10 (gamification)
    // Audit log the erasure itself (retained for legal compliance)
}
```

### SOX Audit Framework (item 148):
Per memorandum: quarterly reporting with signed attestations.
```csharp
public interface ISoxAuditService
{
    Task<QuarterlyReport> GenerateQuarterlyReport(int year, int quarter);
    Task<SignedAttestation> CreateAttestation(Guid auditorId, QuarterlyReport report);
    Task<bool> ValidateAttestationSignature(SignedAttestation attestation);
}
```

### Graph DB (item 149):
```sql
-- Node tables
CREATE TABLE dbo.Person AS NODE;
CREATE TABLE dbo.Incident AS NODE;
CREATE TABLE dbo.Location AS NODE;

-- Edge tables
CREATE TABLE dbo.RespondedTo AS EDGE;
CREATE TABLE dbo.FamilyOf AS EDGE;
CREATE TABLE dbo.NearTo AS EDGE;
CREATE TABLE dbo.CorrelatedWith AS EDGE;
```

### Prometheus + Grafana (item 150):
```yaml
# infra/monitoring/docker-compose.monitoring.yml
services:
  prometheus:
    image: prom/prometheus:latest
    volumes:
      - ./prometheus.yml:/etc/prometheus/prometheus.yml
    ports:
      - "9090:9090"

  grafana:
    image: grafana/grafana:latest
    volumes:
      - ./grafana/dashboards:/var/lib/grafana/dashboards
    ports:
      - "3000:3000"

  alertmanager:
    image: prom/alertmanager:latest
    volumes:
      - ./alertmanager.yml:/etc/alertmanager/alertmanager.yml
```

## WHEN DONE

Commit all changes with message:
```
feat(advanced): add ML services, compliance framework, graph DB, monitoring stack

Items 141-150: Gunshot detection, fall detection, vital anomaly detection,
dispatch optimization, HIPAA/COPPA/GDPR/SOX compliance, graph DB schema,
Prometheus/Grafana observability with PagerDuty alerting
```
