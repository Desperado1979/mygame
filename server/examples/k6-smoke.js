/**
 * Optional smoke: k6 run examples/k6-smoke.js
 * Requires persist_sync on 127.0.0.1:8787 (npm run persist).
 */
import http from "k6/http";
import { check } from "k6";

export const options = {
  vus: 1,
  duration: "5s",
};

export default function () {
  const res = http.get("http://127.0.0.1:8787/health");
  check(res, { "health 200": (r) => r.status === 200 });
}
