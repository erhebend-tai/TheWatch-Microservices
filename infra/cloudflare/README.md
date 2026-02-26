# Cloudflare Edge Configuration

Infrastructure-as-code for TheWatch Cloudflare edge services (Stage 11D, Items 136-140).

## Components

| File | Item | Description |
|------|------|-------------|
| `cdn-config.json` | 136 | CDN cache rules, page rules, security headers |
| `worker-auth/` | 137 | Edge JWT authentication Worker (TypeScript) |
| `waf-rules.json` | 138 | WAF custom rules, rate limiting, managed rulesets |
| `zero-trust-config.json` | 139 | Zero Trust access policies, identity providers, service tokens |
| `argo-tunnels.yaml` | 140 | Cloudflare Tunnel ingress rules for secure origin exposure |

## Prerequisites

1. Cloudflare account with the `thewatch.app` domain configured
2. API token with Zone:Read, Zone:Edit, WAF:Edit permissions
3. `cloudflared` CLI installed for tunnel management
4. `wrangler` CLI installed for Worker deployment

## Deployment

### CDN & WAF (via Cloudflare Dashboard or API)

```bash
# Apply WAF rules via API
curl -X PUT "https://api.cloudflare.com/client/v4/zones/{zone_id}/rulesets" \
  -H "Authorization: Bearer {api_token}" \
  -H "Content-Type: application/json" \
  -d @waf-rules.json
```

### Edge Auth Worker

```bash
cd worker-auth
npm install
npm run dev        # Local development
npm run deploy     # Deploy to Cloudflare
```

### Argo Tunnel

```bash
# Create tunnel
cloudflared tunnel create thewatch-tunnel

# Route DNS
cloudflared tunnel route dns thewatch-tunnel api.thewatch.app
cloudflared tunnel route dns thewatch-tunnel dashboard.thewatch.app

# Run tunnel
cloudflared tunnel --config argo-tunnels.yaml run thewatch-tunnel
```

### Zero Trust

Configure via Cloudflare dashboard (Teams > Access > Applications) using `zero-trust-config.json` as reference.

## .NET Integration

Services register Cloudflare providers via `AddCloudflareServicesIfConfigured()`:

```csharp
builder.Services.AddCloudflareServicesIfConfigured(builder.Configuration);
```

Configuration in `appsettings.json`:

```json
{
  "Cloudflare": {
    "UseCdn": true,
    "UseWaf": true,
    "ApiToken": "{token}",
    "ZoneId": "{zone_id}"
  }
}
```

When toggles are `false`, NoOp providers are registered (safe for development).
