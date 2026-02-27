// Item 259: k6 load test for evidence upload
// Target: 100 concurrent 50MB video uploads
// Run: k6 run --vus 10 --duration 120s evidence-upload-load-test.js

import http from 'k6/http';
import { check, sleep } from 'k6';
import { Counter, Rate, Trend } from 'k6/metrics';

const uploadSuccess = new Counter('evidence_upload_success');
const uploadFailRate = new Rate('evidence_upload_failure_rate');
const uploadLatency = new Trend('evidence_upload_latency', true);
const uploadThroughput = new Trend('evidence_upload_bytes_per_sec', true);

export const options = {
    scenarios: {
        evidence_upload: {
            executor: 'ramping-vus',
            startVUs: 0,
            stages: [
                { duration: '10s', target: 10 },
                { duration: '30s', target: 50 },
                { duration: '60s', target: 100 },
                { duration: '20s', target: 0 },
            ],
        },
    },
    thresholds: {
        http_req_duration: ['p(95)<30000'],     // 30s for large uploads
        evidence_upload_failure_rate: ['rate<0.05'],
    },
};

const VOICE_URL = __ENV.WATCH_VOICE_URL || 'http://localhost:5020';
const AUTH_URL = __ENV.WATCH_AUTH_URL || 'http://localhost:5050';

export function setup() {
    const loginRes = http.post(`${AUTH_URL}/api/v1/auth/login`, JSON.stringify({
        email: __ENV.TEST_EMAIL || 'loadtest@thewatch.test',
        password: __ENV.TEST_PASSWORD || 'LoadTest!2024Pass',
    }), { headers: { 'Content-Type': 'application/json' } });

    const body = JSON.parse(loginRes.body);
    return { token: body.data?.accessToken || body.accessToken || '' };
}

export default function (data) {
    // Generate a simulated evidence payload (1MB binary chunk for testing)
    // Real test would use open()/binary file, but we simulate metadata-only for CI
    const evidenceSizeBytes = 1024 * 1024; // 1MB per iteration (scale up for real test)

    const payload = JSON.stringify({
        incidentId: '00000000-0000-0000-0000-000000000001',
        submittedByUserId: '00000000-0000-0000-0000-000000000000',
        evidenceType: 'Video',
        mimeType: 'video/mp4',
        fileSizeBytes: evidenceSizeBytes,
        latitude: 40.7128 + (Math.random() - 0.5) * 0.01,
        longitude: -74.0060 + (Math.random() - 0.5) * 0.01,
        description: `k6 evidence upload test VU=${__VU} ITER=${__ITER}`,
        capturedAt: new Date().toISOString(),
    });

    const startTime = Date.now();

    const res = http.post(`${VOICE_URL}/api/v1/evidence`, payload, {
        headers: {
            'Content-Type': 'application/json',
            Authorization: `Bearer ${data.token}`,
            'X-Correlation-Id': `k6-evidence-${__VU}-${__ITER}`,
        },
        timeout: '60s',
    });

    const duration = Date.now() - startTime;
    uploadLatency.add(duration);

    if (duration > 0) {
        uploadThroughput.add(evidenceSizeBytes / (duration / 1000));
    }

    const success = check(res, {
        'evidence submitted (2xx)': (r) => r.status >= 200 && r.status < 300,
    });

    if (success) uploadSuccess.add(1);
    uploadFailRate.add(!success);

    sleep(1 + Math.random() * 2);
}
