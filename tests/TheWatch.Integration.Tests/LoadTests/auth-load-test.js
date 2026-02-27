// Item 257: k6 load test for P5 auth (login with MFA)
// Target: 500 concurrent logins
// Run: k6 run --vus 50 --duration 60s auth-load-test.js

import http from 'k6/http';
import { check, sleep } from 'k6';
import { Counter, Rate, Trend } from 'k6/metrics';

const loginSuccess = new Counter('login_success_total');
const loginFailRate = new Rate('login_failure_rate');
const loginLatency = new Trend('login_latency', true);

export const options = {
    scenarios: {
        login_load: {
            executor: 'ramping-vus',
            startVUs: 0,
            stages: [
                { duration: '10s', target: 50 },
                { duration: '30s', target: 500 },
                { duration: '30s', target: 500 },
                { duration: '10s', target: 0 },
            ],
        },
    },
    thresholds: {
        http_req_duration: ['p(95)<3000'],
        http_req_failed: ['rate<0.05'],     // Auth may rate-limit, allow 5%
        login_latency: ['p(95)<2500'],
    },
};

const AUTH_URL = __ENV.WATCH_AUTH_URL || 'http://localhost:5050';

export function setup() {
    // Pre-register test users (10 users, round-robin across VUs)
    const users = [];
    for (let i = 0; i < 10; i++) {
        const email = `loadtest-${i}@thewatch.test`;
        const password = 'LoadTest!2024Pass';

        const regRes = http.post(`${AUTH_URL}/api/v1/auth/register`, JSON.stringify({
            email,
            password,
            displayName: `Load Test User ${i}`,
        }), { headers: { 'Content-Type': 'application/json' } });

        users.push({ email, password });
    }
    return { users };
}

export default function (data) {
    const user = data.users[__VU % data.users.length];

    const payload = JSON.stringify({
        email: user.email,
        password: user.password,
    });

    const res = http.post(`${AUTH_URL}/api/v1/auth/login`, payload, {
        headers: {
            'Content-Type': 'application/json',
            'X-Correlation-Id': `k6-auth-${__VU}-${__ITER}`,
        },
    });

    const success = check(res, {
        'login succeeded (200)': (r) => r.status === 200,
        'has access token': (r) => {
            try {
                const body = JSON.parse(r.body);
                return !!(body.data?.accessToken || body.accessToken);
            } catch {
                return false;
            }
        },
    });

    loginLatency.add(res.timings.duration);
    if (success) loginSuccess.add(1);
    loginFailRate.add(!success);

    // Token refresh test (10% of iterations)
    if (__ITER % 10 === 0 && res.status === 200) {
        try {
            const body = JSON.parse(res.body);
            const refreshToken = body.data?.refreshToken || body.refreshToken;
            if (refreshToken) {
                const refreshRes = http.post(`${AUTH_URL}/api/v1/auth/refresh`, JSON.stringify({
                    refreshToken,
                }), { headers: { 'Content-Type': 'application/json' } });

                check(refreshRes, {
                    'refresh succeeded': (r) => r.status === 200,
                });
            }
        } catch { /* ignore parse errors */ }
    }

    sleep(0.5 + Math.random());
}
