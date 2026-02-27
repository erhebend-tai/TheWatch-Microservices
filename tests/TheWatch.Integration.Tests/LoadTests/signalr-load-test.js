// Item 258: k6 load test for SignalR hub connections
// Target: 10,000 concurrent WebSocket connections
// Run: k6 run --vus 100 --duration 120s signalr-load-test.js
// Note: Requires k6 with xk6-websockets extension

import http from 'k6/http';
import ws from 'k6/ws';
import { check, sleep } from 'k6';
import { Counter, Trend } from 'k6/metrics';

const wsConnected = new Counter('ws_connections_established');
const wsMessages = new Counter('ws_messages_received');
const wsConnectLatency = new Trend('ws_connect_latency', true);

export const options = {
    scenarios: {
        signalr_connections: {
            executor: 'ramping-vus',
            startVUs: 0,
            stages: [
                { duration: '20s', target: 1000 },
                { duration: '30s', target: 5000 },
                { duration: '30s', target: 10000 },
                { duration: '30s', target: 10000 },
                { duration: '10s', target: 0 },
            ],
        },
    },
    thresholds: {
        ws_connect_latency: ['p(95)<5000'],
        ws_connections_established: ['count>5000'],
    },
};

const VOICE_URL = __ENV.WATCH_VOICE_URL || 'http://localhost:5020';
const AUTH_URL = __ENV.WATCH_AUTH_URL || 'http://localhost:5050';

export function setup() {
    // Get a token for WebSocket connections
    const loginRes = http.post(`${AUTH_URL}/api/v1/auth/login`, JSON.stringify({
        email: __ENV.TEST_EMAIL || 'loadtest@thewatch.test',
        password: __ENV.TEST_PASSWORD || 'LoadTest!2024Pass',
    }), { headers: { 'Content-Type': 'application/json' } });

    const body = JSON.parse(loginRes.body);
    return { token: body.data?.accessToken || body.accessToken || '' };
}

export default function (data) {
    // SignalR negotiate endpoint
    const wsBaseUrl = VOICE_URL.replace('http://', 'ws://').replace('https://', 'wss://');
    const hubUrl = `${wsBaseUrl}/hubs/incidents?access_token=${data.token}`;

    const startTime = Date.now();

    const res = ws.connect(hubUrl, {}, function (socket) {
        wsConnectLatency.add(Date.now() - startTime);
        wsConnected.add(1);

        // SignalR handshake
        socket.send(JSON.stringify({ protocol: 'json', version: 1 }) + '\x1e');

        socket.on('message', function (msg) {
            wsMessages.add(1);
        });

        socket.on('error', function (e) {
            console.error(`WebSocket error: ${e}`);
        });

        // Keep connection alive for test duration
        sleep(30 + Math.random() * 30);

        socket.close();
    });

    check(res, {
        'WebSocket connected': (r) => r && r.status === 101,
    });

    sleep(1);
}
