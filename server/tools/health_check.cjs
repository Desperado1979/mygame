/**
 * Quick health check for local persist_sync server.
 * Usage: node tools/health_check.cjs [baseUrl]
 */
const http = require("http");

const baseUrl = process.argv[2] || "http://127.0.0.1:8787";
const healthUrl = baseUrl.replace(/\/$/, "") + "/health";

http
  .get(healthUrl, (res) => {
    let data = "";
    res.on("data", (c) => (data += c));
    res.on("end", () => {
      let body = null;
      try {
        body = JSON.parse(data);
      } catch (_) {}
      const ok = res.statusCode === 200 && body && body.ok === true;
      console.log(`status=${res.statusCode} ok=${ok ? "yes" : "no"}`);
      if (body) {
        console.log(`service=${body.service || "?"} ts=${body.ts || "?"}`);
        if (body.metrics)
          console.log(`metrics.exists=${body.metrics.exists} sizeBytes=${body.metrics.sizeBytes}`);
      } else {
        console.log(data);
      }
      process.exit(ok ? 0 : 1);
    });
  })
  .on("error", (e) => {
    console.error("health_check_failed", e.message);
    process.exit(1);
  });
