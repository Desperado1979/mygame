/**
 * Print compact daily risk summary from /metrics/report.
 * Usage:
 *   node tools/daily_risk_report.cjs [days]
 */
const http = require("http");

const days = Number(process.argv[2]) > 0 ? Number(process.argv[2]) : 7;
const url = `http://127.0.0.1:8787/metrics/report?days=${days}&top=10&groupBy=day`;

http
  .get(url, (res) => {
    let data = "";
    res.on("data", (c) => (data += c));
    res.on("end", () => {
      if (res.statusCode !== 200) {
        console.error(`report_failed status=${res.statusCode}`);
        console.error(data);
        process.exit(1);
      }
      let j;
      try {
        j = JSON.parse(data);
      } catch (e) {
        console.error("invalid_json", e.message);
        process.exit(1);
      }
      if (!j.hasData) {
        console.log("no_data", j.reason || "unknown");
        process.exit(0);
      }
      console.log(`window=${j.window} player=${j.playerId}`);
      console.log(
        `overall total=${j.requests.total} acceptRate=${j.requests.acceptRate}% low=${j.warnings.low} high=${j.warnings.high}`
      );
      const rows = Array.isArray(j.byDay) ? j.byDay : [];
      for (const r of rows) {
        const top = (r.warnings.topCodes || []).map((x) => `${x.code}:${x.count}`).join(",");
        console.log(
          `${r.key} total=${r.requests.total} ok=${r.requests.accepted} rej=${r.requests.rejected} low=${r.warnings.low} high=${r.warnings.high} top=[${top}]`
        );
      }
      process.exit(0);
    });
  })
  .on("error", (e) => {
    console.error("daily_risk_report_failed", e.message);
    process.exit(1);
  });
