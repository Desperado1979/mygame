const fs = require("fs");
const http = require("http");

function parseArgs(argv) {
  const out = {
    base: process.env.SYNC_BASE_URL || "http://127.0.0.1:8787",
    days: "7",
    top: "5",
    hours: "24",
    compareHours: "24",
    out: "",
    csvOut: "",
    markdownOut: "",
    csvMode: "flat",
    markdownMode: "full",
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

function toCsv(j) {
  return toCsvFlat(j);
}

function toCsvFlat(j) {
  const lines = [];
  lines.push("section,key,value");
  lines.push(["summary", "requests_total", j.summary.requests.total].join(","));
  lines.push(["summary", "accept_rate", j.summary.requests.acceptRate].join(","));
  lines.push(["summary", "warnings_low", j.summary.warnings.low].join(","));
  lines.push(["summary", "warnings_high", j.summary.warnings.high].join(","));
  for (const p of j.topPlayers || []) {
    lines.push(["top_player", csvCell(p.playerId), csvCell(`risk=${p.riskScore}|req=${p.requests}|rej=${p.rejected}`)].join(","));
  }
  for (const c of j.topCodes || []) lines.push(["top_code", csvCell(c.code), c.count].join(","));
  for (const r of j.rejectionReasons || []) lines.push(["rejection", csvCell(r.reason), r.count].join(","));
  for (const a of j.alerts || []) lines.push(["alert", csvCell(a.code), csvCell(a.message)].join(","));
  return lines.join("\n") + "\n";
}

function toCsvSectioned(j) {
  const lines = [];
  lines.push("section,key,value");
  lines.push("summary,requests_total," + j.summary.requests.total);
  lines.push("summary,accept_rate," + j.summary.requests.acceptRate);
  lines.push("summary,warnings_low," + j.summary.warnings.low);
  lines.push("summary,warnings_high," + j.summary.warnings.high);
  lines.push("");
  lines.push("section,player,risk_meta");
  for (const p of j.topPlayers || []) {
    lines.push(["players", csvCell(p.playerId), csvCell(`risk=${p.riskScore}|req=${p.requests}|rej=${p.rejected}`)].join(","));
  }
  lines.push("");
  lines.push("section,code,count");
  for (const c of j.topCodes || []) lines.push(["codes", csvCell(c.code), c.count].join(","));
  lines.push("");
  lines.push("section,reason,count");
  for (const r of j.rejectionReasons || []) lines.push(["rejections", csvCell(r.reason), r.count].join(","));
  lines.push("");
  lines.push("section,alert,message");
  for (const a of j.alerts || []) lines.push(["alerts", csvCell(a.code), csvCell(a.message)].join(","));
  return lines.join("\n") + "\n";
}

function toMarkdown(j, mode) {
  const lines = [];
  lines.push("# Risk Dashboard Snapshot");
  lines.push("");
  lines.push(`- Window: ${j.window || "unknown"}`);
  lines.push(
    `- Requests: ${j.summary.requests.total} (accepted ${j.summary.requests.accepted}, rejected ${j.summary.requests.rejected})`
  );
  lines.push(`- Accept Rate: ${j.summary.requests.acceptRate}%`);
  lines.push(`- Warnings: low=${j.summary.warnings.low}, high=${j.summary.warnings.high}`);
  lines.push("");
  lines.push("## Top Players");
  if (!j.topPlayers || j.topPlayers.length === 0) lines.push("- none");
  else for (const p of j.topPlayers) lines.push(`- ${p.playerId}: risk=${p.riskScore}, rejected=${p.rejected}`);
  lines.push("");
  lines.push("## Top Codes");
  if (!j.topCodes || j.topCodes.length === 0) lines.push("- none");
  else for (const c of j.topCodes) lines.push(`- ${c.code}: ${c.count}`);
  lines.push("");
  lines.push("## Alerts");
  if (!j.alerts || j.alerts.length === 0) lines.push("- none");
  else for (const a of j.alerts) lines.push(`- [${a.severity || "unknown"}] ${a.code}: ${a.message}`);
  if (String(mode || "full").toLowerCase() !== "compact") {
    lines.push("");
    lines.push("## Rejection Reasons");
    if (!j.rejectionReasons || j.rejectionReasons.length === 0) lines.push("- none");
    else for (const r of j.rejectionReasons) lines.push(`- ${r.reason}: ${r.count}`);
    lines.push("");
    lines.push("## Anomaly Spikes");
    if (!j.anomalySpikes || j.anomalySpikes.length === 0) lines.push("- none");
    else
      for (const s of j.anomalySpikes) {
        const ratio = s.ratio == null ? "new" : `${s.ratio}x`;
        lines.push(`- ${s.code}: delta=${s.delta}, current=${s.current}, previous=${s.previous}, ratio=${ratio}`);
      }
  }
  lines.push("");
  return lines.join("\n");
}

async function main() {
  const args = parseArgs(process.argv);
  const base = String(args.base || "").replace(/\/+$/, "");
  const q = new URLSearchParams({
    days: String(args.days),
    top: String(args.top),
    hours: String(args.hours),
    compareHours: String(args.compareHours),
  });
  const url = `${base}/metrics/dashboard?${q.toString()}`;
  const r = await getJson(url);
  if (r.status < 200 || r.status >= 300 || !r.body || !r.body.ok) {
    console.error("request failed", "status=" + r.status, url);
    process.exit(1);
  }
  const j = r.body;
  if (!j.hasData) {
    console.log("dashboard: no data");
    return;
  }
  console.log("=== risk dashboard ===");
  console.log(
    `requests=${j.summary.requests.total} acceptRate=${j.summary.requests.acceptRate}% low=${j.summary.warnings.low} high=${j.summary.warnings.high}`
  );
  const tp = Array.isArray(j.topPlayers) && j.topPlayers[0] ? j.topPlayers[0] : null;
  const tc = Array.isArray(j.topCodes) && j.topCodes[0] ? j.topCodes[0] : null;
  const tr = Array.isArray(j.rejectionReasons) && j.rejectionReasons[0] ? j.rejectionReasons[0] : null;
  const ta = Array.isArray(j.alerts) ? j.alerts.length : 0;
  console.log(`top_player=${tp ? tp.playerId : "none"} top_code=${tc ? tc.code : "none"} top_reject=${tr ? tr.reason : "none"} alerts=${ta}`);
  if (args.out && String(args.out).trim()) {
    fs.writeFileSync(String(args.out), JSON.stringify(j, null, 2) + "\n", "utf8");
    console.log(`dashboard_saved=${args.out}`);
  }
  if (args.csvOut && String(args.csvOut).trim()) {
    const mode = String(args.csvMode || "flat").trim().toLowerCase();
    const content = mode === "sectioned" ? toCsvSectioned(j) : toCsv(j);
    fs.writeFileSync(String(args.csvOut), content, "utf8");
    console.log(`dashboard_csv_saved=${args.csvOut}`);
  }
  if (args.markdownOut && String(args.markdownOut).trim()) {
    fs.writeFileSync(String(args.markdownOut), toMarkdown(j, args.markdownMode) + "\n", "utf8");
    console.log(`dashboard_markdown_saved=${args.markdownOut}`);
  }
}

main().catch((e) => {
  console.error("risk dashboard failed:", e.message);
  process.exit(1);
});
