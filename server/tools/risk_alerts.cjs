const fs = require("fs");
const http = require("http");

function parseArgs(argv) {
  const out = {
    base: process.env.SYNC_BASE_URL || "http://127.0.0.1:8787",
    days: "7",
    top: "5",
    hours: "24",
    compareHours: "24",
    minAcceptRate: "95",
    maxHighWarnings: "0",
    maxRejected: "0",
    minSpikeDelta: "2",
    csv: "",
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

function csvCell(v) {
  const s = String(v ?? "");
  if (/[",\n]/.test(s)) return `"${s.replace(/"/g, '""')}"`;
  return s;
}

function alertsToCsv(rows) {
  const lines = ["severity,code,message,value,threshold"];
  for (const a of rows || []) {
    lines.push([a.severity, csvCell(a.code), csvCell(a.message), csvCell(a.value), csvCell(a.threshold)].join(","));
  }
  return lines.join("\n") + "\n";
}

async function main() {
  const args = parseArgs(process.argv);
  const base = String(args.base || "").replace(/\/+$/, "");
  const q = new URLSearchParams({
    days: String(args.days),
    top: String(args.top),
    hours: String(args.hours),
    compareHours: String(args.compareHours),
    minAcceptRate: String(args.minAcceptRate),
    maxHighWarnings: String(args.maxHighWarnings),
    maxRejected: String(args.maxRejected),
    minSpikeDelta: String(args.minSpikeDelta),
  });
  const url = `${base}/metrics/alerts?${q.toString()}`;
  const r = await getJson(url);
  if (r.status < 200 || r.status >= 300 || !r.body || !r.body.ok) {
    console.error("request failed", "status=" + r.status, url);
    process.exit(1);
  }
  const body = r.body;
  if (!body.hasData) {
    console.log("no alert data");
    return;
  }
  const rows = Array.isArray(body.alerts) ? body.alerts : [];
  console.log(`alerts=${rows.length} window=${body.window}`);
  if (rows.length === 0) {
    console.log("status=GREEN (all thresholds within limits)");
  } else {
    console.log("status=RED (threshold exceeded)");
    for (const a of rows) {
      console.log(`- [${a.severity}] ${a.code}: ${a.message}`);
    }
  }
  if (args.csv && String(args.csv).trim()) {
    const content = alertsToCsv(rows);
    fs.writeFileSync(String(args.csv), content, "utf8");
    console.log(`csv_saved=${args.csv}`);
  }
}

main().catch((e) => {
  console.error("risk alerts failed:", e.message);
  process.exit(1);
});
