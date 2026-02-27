// Item 256: k6 load test for P2 incident creation
// Target: 1,000 concurrent SOS activations
// Run: k6 run --vus 100 --duration 60s sos-load-test.js

import http from 'k6/http';
import { check, sleep } from 'k6';
import { Counter, Rate, Trend } from 'k6/metrics';

// Custom metrics
const sosCreated = new Counter('sos_incidents_created');
const sosFailRate = new Rate('sos_failure_rate');
const sosLatency = new Trend('sos_creation_latency', true);

export const options = {
    scenarios: {
        // Ramp up to 1,000 concurrent SOS activations
        sos_spike: {
            executor: 'ramping-vus',
            startVUs: 0,
            stages: [
                { duration: '10s', target: 100 },
                { duration: '30s', target: 1000 },
                { duration: '20s', target: 1000 },
                { duration: '10s', target: 0 },
            ],
        },
    },
    thresholds: {
        http_req_duration: ['p(95)<2000'],   // 95th percentile under 2s (SOS SLA)
        http_req_failed: ['rate<0.01'],       // Less than 1% failure rate
        sos_creation_latency: ['p(99)<3000'], // 99th percentile under 3s
    },
};

const BASE_URL = __ENV.WATCH_VOICE_URL || 'http://localhost:5020';
const AUTH_URL = __ENV.WATCH_AUTH_URL || 'http://localhost:5050';

// Pre-test: login and get token
export function setup() {
    const loginPayload = JSON.stringify({
        email: __ENV.TEST_EMAIL || 'loadtest@thewatch.test',
        password: __ENV.TEST_PASSWORD || 'LoadTest!2024Pass',
    });

    const loginRes = http.post(`${AUTH_URL}/api/v1/auth/login`, loginPayload, {
        headers: { 'Content-Type': 'application/json' },
    });

    if (loginRes.status !== 200) {
        console.error(`Login failed: ${loginRes.status} ${loginRes.body}`);
        return { token: '' };
    }

    const body = JSON.parse(loginRes.body);
    return { token: body.data?.accessToken || body.accessToken || '' };
}

export default function (data) {
    const lat = 40.7128 + (Math.random() - 0.5) * 0.1;
    const lng = -74.0060 + (Math.random() - 0.5) * 0.1;

    const payload = JSON.stringify({
        reporterUserId: '00000000-0000-0000-0000-000000000000',
        type: 'SOS',
        latitude: lat,
        longitude: lng,
        description: `k6 load test SOS - VU ${__VU} iter ${__ITER}`,
        reporterPhone: '+15551234567',
    });

    const params = {
        headers: {
            'Content-Type': 'application/json',
            Authorization: `Bearer ${data.token}`,
            'X-Correlation-Id': `k6-${__VU}-${__ITER}-${Date.now()}`,
        },
    };

    const res = http.post(`${BASE_URL}/api/v1/incidents`, payload, params);
    const success = check(res, {
        'SOS created (2xx)': (r) => r.status >= 200 && r.status < 300,
        'response has incident id': (r) => {
            try {
                const body = JSON.parse(r.body);
                return !!(body.data?.id || body.id);
            } catch {
                return false;
            }
        },
    });

    sosLatency.add(res.timings.duration);
    if (success) {
        sosCreated.add(1);
    }
    sosFailRate.add(!success);

    sleep(0.1 + Math.random() * 0.5);
}
