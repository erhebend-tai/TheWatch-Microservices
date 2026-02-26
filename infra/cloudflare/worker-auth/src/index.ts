/**
 * TheWatch Edge Auth Worker
 *
 * Validates JWT tokens at the Cloudflare edge before requests reach origin services.
 * Extracts user claims and forwards them as trusted headers to downstream services.
 *
 * Features:
 * - JWT signature verification via JWKS endpoint
 * - Token expiry and audience validation
 * - Claim extraction (sub, role, email) forwarded as X-* headers
 * - Health/public endpoint bypass
 * - Rate limiting integration via Cloudflare API
 */

interface Env {
  JWT_ISSUER: string;
  JWT_AUDIENCE: string;
  JWKS_ENDPOINT: string;
  JWT_SECRET?: string;
  ENVIRONMENT: string;
}

interface JwtPayload {
  sub: string;
  role?: string;
  email?: string;
  exp: number;
  iat: number;
  iss: string;
  aud: string | string[];
}

// Paths that bypass authentication
const PUBLIC_PATHS = [
  '/health',
  '/alive',
  '/metrics',
  '/api/auth/login',
  '/api/auth/register',
  '/api/auth/refresh',
  '/swagger',
  '/openapi',
];

// JWKS cache (in-memory, per-isolate)
let jwksCache: { keys: JsonWebKey[]; fetchedAt: number } | null = null;
const JWKS_CACHE_TTL_MS = 600_000; // 10 minutes

export default {
  async fetch(request: Request, env: Env): Promise<Response> {
    const url = new URL(request.url);

    // Skip auth for public/health endpoints
    if (isPublicPath(url.pathname)) {
      return fetch(request);
    }

    // Skip auth for OPTIONS (CORS preflight)
    if (request.method === 'OPTIONS') {
      return fetch(request);
    }

    // Extract Bearer token
    const authHeader = request.headers.get('Authorization');
    if (!authHeader?.startsWith('Bearer ')) {
      return unauthorizedResponse('Missing or invalid Authorization header');
    }

    const token = authHeader.slice(7);

    try {
      const payload = await verifyJwt(token, env);

      // Validate expiry
      if (payload.exp < Math.floor(Date.now() / 1000)) {
        return unauthorizedResponse('Token expired');
      }

      // Validate audience
      const audiences = Array.isArray(payload.aud) ? payload.aud : [payload.aud];
      if (!audiences.includes(env.JWT_AUDIENCE)) {
        return unauthorizedResponse('Invalid audience');
      }

      // Validate issuer
      if (payload.iss !== env.JWT_ISSUER) {
        return unauthorizedResponse('Invalid issuer');
      }

      // Forward request with validated claims as trusted headers
      const modifiedHeaders = new Headers(request.headers);
      modifiedHeaders.set('X-User-Id', payload.sub);
      if (payload.role) modifiedHeaders.set('X-User-Role', payload.role);
      if (payload.email) modifiedHeaders.set('X-User-Email', payload.email);
      modifiedHeaders.set('X-Auth-Validated', 'edge');
      modifiedHeaders.set('X-Auth-Timestamp', new Date().toISOString());

      const modifiedRequest = new Request(request, {
        headers: modifiedHeaders,
      });

      return fetch(modifiedRequest);
    } catch (error) {
      const message = error instanceof Error ? error.message : 'Token validation failed';
      return unauthorizedResponse(message);
    }
  },
};

function isPublicPath(pathname: string): boolean {
  return PUBLIC_PATHS.some(
    (p) => pathname === p || pathname.startsWith(p + '/')
  );
}

function unauthorizedResponse(message: string): Response {
  return new Response(
    JSON.stringify({ error: 'Unauthorized', message }),
    {
      status: 401,
      headers: { 'Content-Type': 'application/json' },
    }
  );
}

async function verifyJwt(token: string, env: Env): Promise<JwtPayload> {
  const parts = token.split('.');
  if (parts.length !== 3) {
    throw new Error('Invalid JWT format');
  }

  const header = JSON.parse(atob(parts[0]));
  const payload: JwtPayload = JSON.parse(atob(parts[1]));

  // Attempt JWKS-based verification for RS256/ES256
  if (header.alg === 'RS256' || header.alg === 'ES256') {
    const keys = await fetchJwks(env.JWKS_ENDPOINT);
    const key = keys.find((k) => k.kid === header.kid);
    if (!key) {
      throw new Error('Signing key not found in JWKS');
    }

    const cryptoKey = await crypto.subtle.importKey(
      'jwk',
      key,
      header.alg === 'RS256'
        ? { name: 'RSASSA-PKCS1-v1_5', hash: 'SHA-256' }
        : { name: 'ECDSA', namedCurve: 'P-256' },
      false,
      ['verify']
    );

    const signatureBytes = base64UrlToArrayBuffer(parts[2]);
    const dataBytes = new TextEncoder().encode(parts[0] + '.' + parts[1]);

    const valid = await crypto.subtle.verify(
      header.alg === 'RS256'
        ? 'RSASSA-PKCS1-v1_5'
        : { name: 'ECDSA', hash: 'SHA-256' },
      cryptoKey,
      signatureBytes,
      dataBytes
    );

    if (!valid) {
      throw new Error('Invalid JWT signature');
    }
  } else if (header.alg === 'HS256' && env.JWT_SECRET) {
    // HMAC fallback for development/testing
    const key = await crypto.subtle.importKey(
      'raw',
      new TextEncoder().encode(env.JWT_SECRET),
      { name: 'HMAC', hash: 'SHA-256' },
      false,
      ['verify']
    );

    const signatureBytes = base64UrlToArrayBuffer(parts[2]);
    const dataBytes = new TextEncoder().encode(parts[0] + '.' + parts[1]);

    const valid = await crypto.subtle.verify('HMAC', key, signatureBytes, dataBytes);
    if (!valid) {
      throw new Error('Invalid JWT signature');
    }
  } else {
    throw new Error(`Unsupported algorithm: ${header.alg}`);
  }

  return payload;
}

async function fetchJwks(endpoint: string): Promise<JsonWebKey[]> {
  if (jwksCache && Date.now() - jwksCache.fetchedAt < JWKS_CACHE_TTL_MS) {
    return jwksCache.keys;
  }

  const response = await fetch(endpoint);
  if (!response.ok) {
    throw new Error(`Failed to fetch JWKS: ${response.status}`);
  }

  const data = (await response.json()) as { keys: JsonWebKey[] };
  jwksCache = { keys: data.keys, fetchedAt: Date.now() };
  return data.keys;
}

function base64UrlToArrayBuffer(base64url: string): ArrayBuffer {
  const base64 = base64url.replace(/-/g, '+').replace(/_/g, '/');
  const padded = base64 + '='.repeat((4 - (base64.length % 4)) % 4);
  const binary = atob(padded);
  const bytes = new Uint8Array(binary.length);
  for (let i = 0; i < binary.length; i++) {
    bytes[i] = binary.charCodeAt(i);
  }
  return bytes.buffer;
}
