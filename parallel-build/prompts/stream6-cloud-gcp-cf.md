# Stream 6: CLOUD-GCP-CF — Stage 11C + 11D (TODO Items 132-140)

You are working in a git worktree of TheWatch microservices solution. Your task is to create Google Cloud service integrations and Cloudflare edge configurations.

## YOUR ASSIGNED TODO ITEMS

### 11C. GCP Services (132-135)
132. Integrate Google Speech-to-Text API for P2 voice recognition (server-side processing)
133. Integrate Google Vision API for evidence analysis and content moderation
134. Integrate Firebase Cloud Messaging for push notifications
135. Integrate Google Healthcare API (FHIR) for P7/P9 health data interoperability

### 11D. Cloudflare Edge (136-140)
136. Configure Cloudflare CDN for static assets (MAUI WebView, Dashboard)
137. Deploy Cloudflare Workers for edge authentication validation
138. Configure Cloudflare WAF rules for API protection
139. Set up Cloudflare Zero Trust for admin/dashboard access
140. Configure Argo Tunnels for secure service exposure without public IPs

## FILES YOU MAY CREATE/MODIFY

### Google Cloud integrations (NEW directory):
- `TheWatch.Shared/Integrations/GoogleSpeechService.cs`
- `TheWatch.Shared/Integrations/GoogleVisionService.cs`
- `TheWatch.Shared/Integrations/GoogleHealthcareService.cs`
- `TheWatch.Shared/Integrations/IGoogleSpeechService.cs`
- `TheWatch.Shared/Integrations/IGoogleVisionService.cs`
- `TheWatch.Shared/Integrations/IGoogleHealthcareService.cs`
- `TheWatch.Shared/TheWatch.Shared.csproj` — add Google Cloud NuGet packages ONLY

### Cloudflare configs (NEW directory):
- `infra/cloudflare/worker-auth/wrangler.toml`
- `infra/cloudflare/worker-auth/src/index.ts`
- `infra/cloudflare/worker-auth/package.json`
- `infra/cloudflare/waf-rules.json`
- `infra/cloudflare/zero-trust-config.json`
- `infra/cloudflare/argo-tunnels.yaml`
- `infra/cloudflare/cdn-config.json`
- `infra/cloudflare/README.md`

## FILES YOU MUST NOT TOUCH

- `TheWatch.Shared/Auth/` (owned by Stream 2)
- `TheWatch.Shared/Events/`, `TheWatch.Shared/Notifications/`, `TheWatch.Shared/Contracts/`
- Any `TheWatch.P*/` directory
- `TheWatch.Mobile/`
- `TheWatch.Dashboard/`
- `TheWatch.Aspire.AppHost/`
- `infra/bicep/` (owned by Stream 5)
- `docker/`, `helm/`, `.github/`

## PACKAGES TO ADD TO TheWatch.Shared.csproj

```xml
<PackageReference Include="Google.Cloud.Speech.V2" Version="1.*" />
<PackageReference Include="Google.Cloud.Vision.V1" Version="3.*" />
<PackageReference Include="Google.Cloud.Healthcare.V1" Version="3.*" />
```

Note: Firebase Admin SDK is already in TheWatch.Shared.csproj (added in Session 9).

## IMPLEMENTATION PATTERNS

### Google Speech-to-Text Service:
```csharp
namespace TheWatch.Shared.Integrations;

public interface IGoogleSpeechService
{
    Task<SpeechTranscription> TranscribeAudioAsync(byte[] audioData, string languageCode = "en-US");
    Task<SpeechTranscription> TranscribeStreamAsync(Stream audioStream, string languageCode = "en-US");
}

public class GoogleSpeechService : IGoogleSpeechService
{
    private readonly SpeechClient _client;

    public GoogleSpeechService()
    {
        _client = SpeechClient.Create(); // Uses GOOGLE_APPLICATION_CREDENTIALS
    }

    public async Task<SpeechTranscription> TranscribeAudioAsync(byte[] audioData, string languageCode)
    {
        var response = await _client.RecognizeAsync(new RecognitionConfig
        {
            Encoding = RecognitionConfig.Types.AudioEncoding.Linear16,
            SampleRateHertz = 16000,
            LanguageCode = languageCode,
            Model = "latest_long",
            EnableAutomaticPunctuation = true,
        }, RecognitionAudio.FromBytes(audioData));

        return new SpeechTranscription
        {
            Text = string.Join(" ", response.Results.Select(r => r.Alternatives[0].Transcript)),
            Confidence = response.Results.Average(r => r.Alternatives[0].Confidence),
            LanguageCode = languageCode
        };
    }
}

public record SpeechTranscription
{
    public string Text { get; init; } = string.Empty;
    public float Confidence { get; init; }
    public string LanguageCode { get; init; } = "en-US";
}
```

### Google Vision Service (content moderation):
```csharp
public interface IGoogleVisionService
{
    Task<ContentModerationResult> ModerateImageAsync(byte[] imageData);
    Task<List<string>> DetectLabelsAsync(byte[] imageData);
    Task<string> ExtractTextAsync(byte[] imageData);
}

public class GoogleVisionService : IGoogleVisionService
{
    private readonly ImageAnnotatorClient _client;

    public async Task<ContentModerationResult> ModerateImageAsync(byte[] imageData)
    {
        var image = Image.FromBytes(imageData);
        var response = await _client.DetectSafeSearchAsync(image);
        return new ContentModerationResult
        {
            IsAdult = response.Adult >= Likelihood.Likely,
            IsViolence = response.Violence >= Likelihood.Likely,
            IsMedical = response.Medical >= Likelihood.Likely,
            AdultLikelihood = response.Adult.ToString(),
            ViolenceLikelihood = response.Violence.ToString()
        };
    }
}
```

### Google Healthcare API (FHIR):
```csharp
public interface IGoogleHealthcareService
{
    Task<string> CreatePatientResourceAsync(string datasetId, string fhirStoreId, object patientData);
    Task<string> GetPatientResourceAsync(string datasetId, string fhirStoreId, string patientId);
    Task<string> SearchResourcesAsync(string datasetId, string fhirStoreId, string resourceType, string query);
}
```

### Cloudflare Worker for Edge Auth:
```typescript
// infra/cloudflare/worker-auth/src/index.ts
export default {
  async fetch(request: Request): Promise<Response> {
    const url = new URL(request.url);

    // Skip auth for health endpoints
    if (url.pathname.endsWith('/health') || url.pathname.endsWith('/alive')) {
      return fetch(request);
    }

    // Validate JWT at the edge
    const authHeader = request.headers.get('Authorization');
    if (!authHeader?.startsWith('Bearer ')) {
      return new Response('Unauthorized', { status: 401 });
    }

    const token = authHeader.slice(7);
    try {
      const payload = await verifyJwt(token);
      // Add validated claims as headers for downstream services
      const modifiedRequest = new Request(request, {
        headers: new Headers({
          ...Object.fromEntries(request.headers),
          'X-User-Id': payload.sub,
          'X-User-Role': payload.role,
        })
      });
      return fetch(modifiedRequest);
    } catch {
      return new Response('Invalid token', { status: 401 });
    }
  }
};
```

### Cloudflare WAF Rules:
```json
{
  "rules": [
    {
      "description": "Block SQL injection attempts",
      "expression": "http.request.uri.query contains \"UNION SELECT\" or http.request.uri.query contains \"DROP TABLE\"",
      "action": "block"
    },
    {
      "description": "Rate limit auth endpoints",
      "expression": "http.request.uri.path contains \"/api/auth/\"",
      "action": "rate_limit",
      "rateLimit": { "requestsPerPeriod": 10, "period": 60 }
    },
    {
      "description": "Block non-US traffic to admin endpoints",
      "expression": "http.request.uri.path contains \"/admin\" and ip.geoip.country ne \"US\"",
      "action": "block"
    }
  ]
}
```

## WHEN DONE

Commit all changes with message:
```
feat(cloud): add Google Cloud integrations and Cloudflare edge configurations

Items 132-140: Google Speech-to-Text, Vision API, Healthcare FHIR,
Cloudflare CDN, edge auth Worker, WAF rules, Zero Trust, Argo Tunnels
```
