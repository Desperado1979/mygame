/**
 * CLI: GET /metrics/prometheus (OpenMetrics text).
 * Usage: node tools/fetch_prometheus_metrics.cjs [days] [playerId]
 * Env: SYNC_BASE_URL (default http://127.0.0.1:8787)
 */
const http = require("http");

const base = String(process.env.SYNC_BASE_URL || "http://127.0.0.1:8787").replace(/\/$/, "");
const days = process.argv[2] && /^\d+$/.test(process.argv[2]) ? process.argv[2] : "7";
const playerId = process.argv[3] && String(process.argv[3]).trim() ? String(process.argv[3]).trim() : "";
const u = new URL(base + "/metrics/prometheus");
u.searchParams.set("days", days);
if (playerId) u.searchParams.set("playerId", playerId);

http
  .get(u, (res) => {
    let d = "";
    res.on("data", (c) => {
      d += c;
    });
    res.on("end", () => {
      process.stdout.write(d + (d.endsWith("\n") ? "" : "\n"));
      if (res.statusCode < 200 || res.statusCode >= 300) process.exitCode = 1;
    });
  })
  .on("error", (e) => {
    console.error(e.message || e);
    process.exitCode = 1;
  });
