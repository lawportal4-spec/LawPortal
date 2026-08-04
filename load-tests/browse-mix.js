import http from "k6/http";
import { check, sleep } from "k6";
import { Rate, Trend } from "k6/metrics";

// A real load test against the live local API — not a synthetic benchmark. Models the
// read-heavy anonymous-browse path (the traffic shape a marketing-site visitor or a directory
// browser actually produces): health, catalog lookups, lawyer search, and a single lawyer
// profile fetch. Deliberately excludes money-moving/auth endpoints — those are rate-limited by
// design (see Program.cs's "auth" policy) and would just report artificial 429s here rather than
// real capacity numbers.
const BASE_URL = __ENV.BASE_URL || "http://localhost:5280";
const LAWYER_SLUG = __ENV.LAWYER_SLUG || "خالدسعدالمطيري-cb44534d";

export const errorRate = new Rate("errors");
export const catalogTrend = new Trend("catalog_duration", true);
export const searchTrend = new Trend("lawyer_search_duration", true);
export const profileTrend = new Trend("lawyer_profile_duration", true);

export const options = {
  scenarios: {
    browse: {
      executor: "ramping-vus",
      startVUs: 0,
      stages: [
        { duration: "10s", target: 20 },
        { duration: "20s", target: 50 },
        { duration: "10s", target: 0 },
      ],
    },
  },
  thresholds: {
    http_req_duration: ["p(95)<500", "p(99)<1000"],
    errors: ["rate<0.01"],
  },
};

export default function () {
  const health = http.get(`${BASE_URL}/health`);
  check(health, { "health 200": (r) => r.status === 200 }) || errorRate.add(1);

  const categories = http.get(`${BASE_URL}/api/v1/catalog/categories`);
  catalogTrend.add(categories.timings.duration);
  check(categories, { "categories 200": (r) => r.status === 200 }) || errorRate.add(1);

  const search = http.get(`${BASE_URL}/api/v1/lawyers?sort=Rating&page=1&pageSize=10`);
  searchTrend.add(search.timings.duration);
  check(search, { "lawyer search 200": (r) => r.status === 200 }) || errorRate.add(1);

  const profile = http.get(`${BASE_URL}/api/v1/lawyers/${encodeURIComponent(LAWYER_SLUG)}`);
  profileTrend.add(profile.timings.duration);
  check(profile, { "lawyer profile 200": (r) => r.status === 200 }) || errorRate.add(1);

  sleep(1);
}
