// Item 260: Benchmark sub-2-second SOS activation
// Measures end-to-end latency from SOS button press to first responder notification
// Run: k6 run --vus 1 --iterations 100 sos-latency-benchmark.js

import http from 'k6/http';
import { check, sleep } from 'k6';
import { Trend, Counter } from 'k6/metrics';

// End-to-end latency breakdown
const sosEndToEnd = new Trend('sos_e2e_latency_ms', true);
const sosIncidentCreate = new Trend('sos_incident_create_ms', true);
const sosDispatchCreate = new Trend('sos_dispatch_create_ms', true);
const sosNearbyQuery = new Trend('sos_nearby_query_ms', true);
const sosUnder2s = new Counter('sos_under_2s_total');
const sosOver2s = new Counter('sos_over_2s_total');

export const options = {
    scenarios: {
        sos_benchmark: {
            executor: 'per-vu-iterations',
            vus: 5,
            iterations: 20,
            maxDuration: '5m',
        },
    },
    thresholds: {
        sos_e2e_latency_ms: ['p(95)<2000', 'p(50)<1000'],
        sos_incident_create_ms: ['p(95)<500'],
        sos_dispatch_create_ms: ['p(95)<500'],
        sos_nearby_query_ms: ['p(95)<300'],
    },
};

const VOICE_URL = __ENV.WATCH_VOICE_URL || 'http://localhost:5020';
const RESPONDER_URL = __ENV.WATCH_RESPONDER_URL || 'http://localhost:5060';
const AUTH_URL = __ENV.WATCH_AUTH_URL || 'http://localhost:5050';

export function setup() {
    const loginRes = http.post(`${AUTH_URL}/api/v1/auth/login`, JSON.stringify({
        email: __ENV.TEST_EMAIL || 'loadtest@thewatch.test',
        password: __ENV.TEST_PASSWORD || 'LoadTest!2024Pass',
    }), { headers: { 'Content-Type': 'application/json' } });

    const body = JSON.parse(loginRes.body);
    const token = body.data?.accessToken || body.accessToken || '';

    // Pre-register responders
    const authHeaders = {
        'Content-Type': 'application/json',
        Authorization: `Bearer ${token}`,
    };

    for (let i = 0; i < 5; i++) {
        http.post(`${RESPONDER_URL}/api/v1/responders`, JSON.stringify({
            name: `Benchmark Officer ${i}`,
            badgeNumber: `BM-${i}-${Date.now()}`.substring(0, 12),
            type: 'Police',
            latitude: 40.7128 + (i * 0.001),
            longitude: -74.0060,
        }), { headers: authHeaders });
    }

    return { token, authHeaders };
}

export default function (data) {
    const e2eStart = Date.now();
    const lat = 40.7128 + (Math.random() - 0.5) * 0.005;
    const lng = -74.0060 + (Math.random() - 0.5) * 0.005;

    // Step 1: Create SOS incident
    const t1 = Date.now();
    const incidentRes = http.post(`${VOICE_URL}/api/v1/incidents`, JSON.stringify({
        reporterUserId: '00000000-0000-0000-0000-000000000000',
        type: 'SOS',
        latitude: lat,
        longitude: lng,
        description: `SOS benchmark VU=${__VU} ITER=${__ITER}`,
    }), { headers: data.authHeaders });
    sosIncidentCreate.add(Date.now() - t1);

    check(incidentRes, { 'incident created': (r) => r.status >= 200 && r.status < 300 });

    let incidentId;
    try {
        const body = JSON.parse(incidentRes.body);
        incidentId = body.data?.id || body.id;
    } catch {
        return;
    }

    // Step 2: Find nearby responders
    const t2 = Date.now();
    const nearbyRes = http.get(
        `${RESPONDER_URL}/api/v1/responders/nearby?latitude=${lat}&longitude=${lng}&radiusKm=5&onDutyOnly=true`,
        { headers: data.authHeaders }
    );
    sosNearbyQuery.add(Date.now() - t2);

    check(nearbyRes, { 'nearby query ok': (r) => r.status >= 200 && r.status < 300 });

    let responderId;
    try {
        const body = JSON.parse(nearbyRes.body);
        const items = body.data || body;
        if (Array.isArray(items) && items.length > 0) {
            responderId = items[0].id;
        }
    } catch { /* no responders found */ }

    // Step 3: Create dispatch (if responder found)
    if (responderId && incidentId) {
        const t3 = Date.now();
        const dispatchRes = http.post(`${VOICE_URL}/api/v1/dispatches`, JSON.stringify({
            incidentId,
            responderId,
            priority: 'Critical',
        }), { headers: data.authHeaders });
        sosDispatchCreate.add(Date.now() - t3);

        check(dispatchRes, { 'dispatch created': (r) => r.status >= 200 && r.status < 300 });
    }

    // Record end-to-end latency
    const e2eLatency = Date.now() - e2eStart;
    sosEndToEnd.add(e2eLatency);

    if (e2eLatency < 2000) {
        sosUnder2s.add(1);
    } else {
        sosOver2s.add(1);
    }

    sleep(0.5);
}

export function handleSummary(data) {
    const p50 = data.metrics.sos_e2e_latency_ms?.values?.['p(50)'] || 'N/A';
    const p95 = data.metrics.sos_e2e_latency_ms?.values?.['p(95)'] || 'N/A';
    const p99 = data.metrics.sos_e2e_latency_ms?.values?.['p(99)'] || 'N/A';
    const under2s = data.metrics.sos_under_2s_total?.values?.count || 0;
    const over2s = data.metrics.sos_over_2s_total?.values?.count || 0;
    const total = under2s + over2s;
    const pctUnder = total > 0 ? ((under2s / total) * 100).toFixed(1) : 'N/A';

    return {
        stdout: `
═══════════════════════════════════════════════════
  TheWatch SOS Latency Benchmark Results
═══════════════════════════════════════════════════
  E2E p50:  ${p50}ms
  E2E p95:  ${p95}ms
  E2E p99:  ${p99}ms
  Under 2s: ${under2s}/${total} (${pctUnder}%)
  SLA Met:  ${parseFloat(p95) < 2000 ? 'YES' : 'NO'}
═══════════════════════════════════════════════════
`,
    };
}
