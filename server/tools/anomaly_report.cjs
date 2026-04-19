const http = require("http");

function parseArgs(argv) {
  const out = {
    base: process.env.SYNC_BASE_URL || "http://127.0.0.1:8787",
    hours: "24",
    compareHours: "24",
    top: "5",
    minCount: "2",
  };
  for (let i = 2; i < argv.length; i++) {
    const a = argv[i];
    if (!a.startsWith("--")) continue;
    const key = a.slice(2);
    const next = argv[i + 1];
    if (next && !next.startsWith("--")) {
      out[key] = next;
      i += 1;
    } else {
      out[key] = "1";
    }
  }
  return out;
}

function getJson(url) {
  return new Promise((resolve, reject) => {
    http
      .get(url, (res) => {
        let data = "";
        res.on("data", (c) => (data += c));
        res.on("end", () => {
          try {
            resolve({ status: res.statusCode || 0, body: JSON.parse(data || "{}") });
          } catch (e) {
            reject(new Error("invalid_json_response: " + e.message));
          }
        });
      })
      .on("error", reject);
  });
}

async function main() {
  const args = parseArgs(process.argv);
  const base = String(args.base || "").replace(/\/+$/, "");
  const q = new URLSearchParams({
    hours: String(args.hours),
    compareHours: String(args.compareHours),
    top: String(args.top),
    minCount: String(args.minCount),
  });
  const url = `${base}/metrics/anomalies?${q.toString()}`;
  const r = await getJson(url);
  if (r.status < 200 || r.status >= 300 || !r.body || !r.body.ok) {
    console.error("request failed", "status=" + r.status, url);
    process.exit(1);
  }
  const body = r.body;
  if (!body.hasData) {
    console.log("no anomaly data");
    return;
  }
  console.log(
    `window current=${body.window.current.since}..${body.window.current.until} previous=${body.window.previous.since}..${body.window.previous.until}`
  );
  console.log(
    `requests delta=${body.summary.requestDelta} rejectionDelta=${body.summary.rejectionDelta} acceptRateDelta=${body.summary.acceptRateDelta}%`
  );
  const rows = Array.isArray(body.anomalies) ? body.anomalies : [];
  if (rows.length === 0) {
    console.log("no warning-code spikes detected");
    return;
  }
  console.log("spikes:");
  for (const a of rows) {
    const ratio = a.ratio == null ? "new" : `${a.ratio}x`;
    console.log(`- ${a.code}: current=${a.current} prev=${a.previous} delta=${a.delta} ratio=${ratio}`);
  }
}

main().catch((e) => {
  console.error("anomaly report failed:", e.message);
  process.exit(1);
});
