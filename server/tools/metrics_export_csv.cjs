/**
 * Save /metrics/report CSV to a local file.
 * Usage:
 *   node tools/metrics_export_csv.cjs [output.csv] [queryString]
 * Example:
 *   node tools/metrics_export_csv.cjs risk_report.csv "days=7&top=10&groupBy=day"
 */
const fs = require("fs");
const http = require("http");
const path = require("path");

const out = process.argv[2] || "metrics_report.csv";
const qs = process.argv[3] || "days=7&top=10&groupBy=day";
const url = `http://127.0.0.1:8787/metrics/report?${qs}&format=csv`;

http
  .get(url, (res) => {
    let data = "";
    res.on("data", (c) => (data += c));
    res.on("end", () => {
      if (res.statusCode !== 200) {
        console.error(`export_failed status=${res.statusCode}`);
        console.error(data);
        process.exit(1);
      }
      const fp = path.resolve(out);
      fs.writeFileSync(fp, data, "utf8");
      console.log("saved_csv", fp);
      process.exit(0);
    });
  })
  .on("error", (e) => {
    console.error("metrics_export_failed", e.message);
    process.exit(1);
  });
