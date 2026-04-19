const fs = require("fs");
const path = require("path");

function parseArgs(argv) {
  const out = {
    file: path.join(__dirname, "..", "data", "risk_smoke_history.ndjson"),
    days: "7,30",
    jsonOut: "",
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

function parseDays(raw) {
  return String(raw || "7,30")
    .split(",")
    .map((s) => Number(s.trim()))
    .filter((n) => Number.isFinite(n) && n > 0)
    .map((n) => Math.floor(n));
}

function summarize(rows, days) {
  const cutoff = Date.now() - days * 24 * 60 * 60 * 1000;
  const xs = rows.filter((r) => {
    const t = Date.parse(String(r.ts || ""));
    return Number.isFinite(t) && t >= cutoff;
  });
  if (xs.length === 0) return { days, count: 0, pass: 0, fail: 0, failRate: 0, avgFailed: 0 };
  const pass = xs.filter((x) => x.ok === true).length;
  const fail = xs.length - pass;
  const totalFailedChecks = xs.reduce((acc, x) => acc + (Number(x.failed) || 0), 0);
  return {
    days,
    count: xs.length,
    pass,
    fail,
    failRate: Number(((fail / xs.length) * 100).toFixed(1)),
    avgFailed: Number((totalFailedChecks / xs.length).toFixed(2)),
  };
}

function main() {
  const args = parseArgs(process.argv);
  const fp = path.resolve(String(args.file));
  if (!fs.existsSync(fp)) {
    console.log("history file not found:", fp);
    process.exit(0);
  }
  const rows = fs
    .readFileSync(fp, "utf8")
    .split(/\r?\n/)
    .filter(Boolean)
    .map((line) => {
      try {
        return JSON.parse(line);
      } catch (_) {
        return null;
      }
    })
    .filter(Boolean);
  const windows = parseDays(args.days);
  const report = windows.map((d) => summarize(rows, d));
  for (const r of report) {
    console.log(
      `window=${r.days}d samples=${r.count} pass=${r.pass} fail=${r.fail} failRate=${r.failRate}% avgFailed=${r.avgFailed}`
    );
  }
  if (args.jsonOut && String(args.jsonOut).trim()) {
    const payload = {
      generatedAt: new Date().toISOString(),
      file: fp,
      windows: report,
    };
    fs.writeFileSync(String(args.jsonOut), JSON.stringify(payload, null, 2) + "\n", "utf8");
    console.log(`risk_smoke_history_report_saved=${args.jsonOut}`);
  }
}

main();
