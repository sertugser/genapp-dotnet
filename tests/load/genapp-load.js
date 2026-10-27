// Load test of the performance requirements in docs/srs.md section 4.1 (#84). Results: tests/results/load-test.md.
//
// 20 virtual users run at the same time for 2 minutes. Each one owns a customer (created in setup, not measured) and
// repeats: motor policy add, motor policy inquire, customer inquire, motor policy update, motor policy delete. There
// is no think time between requests, so the load is heavier than 20 real users at a screen.
//
//   docker compose up -d && docker compose --profile load run --rm k6     (docker-compose.yml, service k6)
//   k6 run -e BASE_URL=http://localhost:5236 tests/load/genapp-load.js    (k6 installed, API started with dotnet run)

import http from 'k6/http';
import { check, fail, sleep } from 'k6';
import { Rate } from 'k6/metrics';

const BASE_URL = __ENV.BASE_URL || 'http://localhost:8080';
const SUMMARY_FILE = __ENV.SUMMARY_FILE || 'tests/results/load-test-summary.json';
const USERS = 20;
const OPERATIONS = ['motor-ekle', 'motor-sorgula', 'musteri-sorgula', 'motor-guncelle', 'motor-sil'];
const JSON_HEADERS = { 'Content-Type': 'application/json' };

// NFR-03: share of measured requests answered with a 5xx status.
const serverErrors = new Rate('server_errors');

export const options = {
  scenarios: {
    users: { executor: 'constant-vus', vus: USERS, duration: '2m' },
  },
  // k6 exits with an error when a threshold fails. Requests are tagged kind:read or kind:write; setup requests are
  // tagged kind:setup and take no part in the thresholds.
  thresholds: {
    'http_req_duration{kind:read}': ['p(95)<500'], // NFR-01
    'http_req_duration{kind:write}': ['p(95)<1000'], // NFR-02
    server_errors: ['rate==0'], // NFR-03
    checks: ['rate==1'], // every request got the status the operation documents (docs/api-design.md)
    // Never fail; they make k6 keep the durations of each operation for the summary.
    ...Object.fromEntries(OPERATIONS.map((name) => [`http_req_duration{name:${name}}`, ['max>=0']])),
  },
  summaryTrendStats: ['avg', 'min', 'med', 'p(90)', 'p(95)', 'max'],
};

const customer = {
  firstName: 'Yuk',
  lastName: 'Testi',
  dateOfBirth: '1990-01-01',
  houseName: 'Test Evi',
  houseNumber: '1',
  postcode: 'LT1 1AA',
  phoneMobile: '05550000000',
  phoneHome: '02120000000',
  email: 'yuk.testi@example.com',
};

function motorPolicy(payment) {
  return {
    issueDate: '2026-01-01',
    expiryDate: '2027-01-01',
    brokerId: 1,
    brokersReference: 'YUKTEST',
    payment,
    make: 'Ford',
    model: 'Focus',
    value: 20000,
    registrationNumber: 'AB12CDE',
    colour: 'Mavi',
    cc: 1600,
    manufactureDate: '2020-05-01',
    premium: 500,
    accidents: 0,
  };
}

// Waits for the API (docker compose run starts it together with k6), then creates one customer per virtual user.
export function setup() {
  for (let i = 0; ; i++) {
    const res = http.get(`${BASE_URL}/api/customers/0`, { tags: { kind: 'setup' } });
    if (res.status !== 0) break;
    if (i === 60) fail(`API is not answering at ${BASE_URL}`);
    sleep(1);
  }

  const customerNumbers = [];
  for (let i = 0; i < USERS; i++) {
    const res = http.post(`${BASE_URL}/api/customers`, JSON.stringify(customer), {
      headers: JSON_HEADERS,
      tags: { kind: 'setup' },
    });
    if (res.status !== 201) fail(`customer add returned ${res.status}`);
    customerNumbers.push(res.json('customerNumber'));
  }
  return { customerNumbers };
}

function measured(res, expectedStatus) {
  serverErrors.add(res.status >= 500);
  return check(res, { [`${res.request.method} ${expectedStatus}`]: (r) => r.status === expectedStatus });
}

export default function ({ customerNumbers }) {
  const customerNumber = customerNumbers[(__VU - 1) % customerNumbers.length];
  const policies = `${BASE_URL}/api/customers/${customerNumber}/motor-policies`;
  const body = motorPolicy(__ITER % 1000);

  const added = http.post(policies, JSON.stringify(body), {
    headers: JSON_HEADERS,
    tags: { name: 'motor-ekle', kind: 'write' },
  });
  if (!measured(added, 201)) return;
  const policyNumber = added.json('policyNumber');

  const inquired = http.get(`${policies}/${policyNumber}`, { tags: { name: 'motor-sorgula', kind: 'read' } });
  measured(inquired, 200);

  measured(
    http.get(`${BASE_URL}/api/customers/${customerNumber}`, { tags: { name: 'musteri-sorgula', kind: 'read' } }),
    200,
  );

  if (inquired.status === 200) {
    // The lastChanged value of the inquiry is sent back unchanged (docs/api-design.md section 5).
    const changes = { ...body, premium: body.premium + 1, lastChanged: inquired.json('lastChanged') };
    measured(
      http.put(`${policies}/${policyNumber}`, JSON.stringify(changes), {
        headers: JSON_HEADERS,
        tags: { name: 'motor-guncelle', kind: 'write' },
      }),
      200,
    );
  }

  measured(http.del(`${policies}/${policyNumber}`, null, { tags: { name: 'motor-sil', kind: 'write' } }), 204);
}

// Writes the measured values next to the report, so the numbers in load-test.md can be checked.
export function handleSummary(data) {
  const stats = (name) => data.metrics[name] && data.metrics[name].values;
  const summary = {
    baseUrl: BASE_URL,
    virtualUsers: USERS,
    duration: '2m',
    durationsMs: {
      read: stats('http_req_duration{kind:read}'),
      write: stats('http_req_duration{kind:write}'),
      ...Object.fromEntries(OPERATIONS.map((name) => [name, stats(`http_req_duration{name:${name}}`)])),
    },
    requests: stats('http_reqs'),
    iterations: stats('iterations'),
    serverErrors: stats('server_errors'),
    checks: stats('checks'),
    thresholds: Object.fromEntries(
      Object.entries(data.metrics)
        .filter(([name, metric]) => metric.thresholds && !name.includes('{name:'))
        .map(([name, metric]) => [name, metric.thresholds]),
    ),
  };
  return {
    [SUMMARY_FILE]: JSON.stringify(summary, null, 2),
    stdout: JSON.stringify(summary, null, 2) + '\n',
  };
}
