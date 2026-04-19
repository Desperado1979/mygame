const fs = require("fs");
const http = require("http");
const path = require("path");

function parseArgs(argv) {
  const out = {
    base: process.env.SYNC_BASE_URL || "http://127.0.0.1:8787",
    days: "7",
    top: "5",
    env: process.env.RISK_ENV || "dev",
    owner: process.env.RISK_OWNER || "unassigned",
    incident: "",
    out: "",
    jsonOut: "",
    prevJson: "",
    autoPrev: "0",
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

async function mustGet(url) {
  const r = await getJson(url);
  if (r.status < 200 || r.status >= 300 || !r.body || !r.body.ok) {
    throw new Error(`request_failed status=${r.status} url=${url}`);
  }
  return r.body;
}

function todayStamp() {
  return new Date().toISOString().slice(0, 10);
}

function toBool(v, fallback) {
  if (v == null || v === "") return fallback;
  const s = String(v).trim().toLowerCase();
  if (["1", "true", "yes", "on", "y"].includes(s)) return true;
  if (["0", "false", "no", "off", "n"].includes(s)) return false;
  return fallback;
}

function loadJsonIfExists(fp) {
  try {
    if (!fp) return null;
    if (!fs.existsSync(fp)) return null;
    return JSON.parse(fs.readFileSync(fp, "utf8"));
  } catch (_) {
    return null;
  }
}

function resolveAutoPrevJson(targetJsonOut) {
  if (!targetJsonOut) return null;
  const dir = path.dirname(targetJsonOut);
  const base = path.basename(targetJsonOut);
  if (!fs.existsSync(dir)) return null;
  const list = fs
    .readdirSync(dir)
    .filter((n) => /^daily_risk_.*\.json$/i.test(n))
    .filter((n) => n !== base)
    .map((n) => {
      const fp = path.join(dir, n);
      const st = fs.statSync(fp);
      return { fp, mtimeMs: st.mtimeMs };
    })
    .sort((a, b) => b.mtimeMs - a.mtimeMs);
  return list.length > 0 ? list[0].fp : null;
}

async function main() {
  const args = parseArgs(process.argv);
  const base = String(args.base || "").replace(/\/+$/, "");
  const days = String(args.days);
  const top = String(args.top);
  const envName = String(args.env || "dev").trim() || "dev";
  const owner = String(args.owner || "unassigned").trim() || "unassigned";
  const incident = String(args.incident || "").trim();
  const jsonOutPath = args.jsonOut && String(args.jsonOut).trim() ? String(args.jsonOut) : "";
  const autoPrev = toBool(args.autoPrev, false);
  const prevJsonPath =
    (args.prevJson && String(args.prevJson).trim()) || (autoPrev ? resolveAutoPrevJson(jsonOutPath) : "");
  const prevSnapshot = loadJsonIfExists(prevJsonPath);

  const [report, alerts, pAlerts, codes, rejections, anomalies] = await Promise.all([
    mustGet(`${base}/metrics/report?days=${encodeURIComponent(days)}&top=${encodeURIComponent(top)}`),
    mustGet(
      `${base}/metrics/alerts?days=${encodeURIComponent(days)}&minAcceptRate=95&maxHighWarnings=0&maxRejected=0&minSpikeDelta=2`
    ),
    mustGet(
      `${base}/metrics/alerts/players?days=${encodeURIComponent(days)}&top=${encodeURIComponent(top)}&minAcceptRate=95&maxHighWarnings=0&maxRejected=0`
    ),
    mustGet(`${base}/metrics/codes?days=${encodeURIComponent(days)}&top=${encodeURIComponent(top)}`),
    mustGet(`${base}/metrics/rejections?days=${encodeURIComponent(days)}`),
    mustGet(`${base}/metrics/anomalies?hours=24&compareHours=24&top=${encodeURIComponent(top)}&minCount=1`),
  ]);

  if (!report.hasData) {
    console.log("daily template: no data");
    return;
  }

  const lines = [];
  lines.push(`# Daily Risk Report (${todayStamp()})`);
  lines.push("");
  lines.push(`- Environment: ${envName}`);
  lines.push(`- Owner: ${owner}`);
  lines.push(`- Incident Ticket: ${incident || "N/A"}`);
  lines.push(`- Window: ${report.window}`);
  lines.push(`- Requests: ${report.requests.total} (accepted ${report.requests.accepted}, rejected ${report.requests.rejected})`);
  lines.push(`- Accept Rate: ${report.requests.acceptRate}%`);
  lines.push(`- Warnings: low=${report.warnings.low}, high=${report.warnings.high}`);
  if (prevSnapshot && prevSnapshot.summary && prevSnapshot.summary.requests) {
    const prevReq = prevSnapshot.summary.requests;
    const prevWarn = (prevSnapshot.summary && prevSnapshot.summary.warnings) || {};
    const reqDelta = report.requests.total - (Number(prevReq.total) || 0);
    const rejDelta = report.requests.rejected - (Number(prevReq.rejected) || 0);
    const highDelta = report.warnings.high - (Number(prevWarn.high) || 0);
    lines.push(`- Change vs Previous: reqDelta=${reqDelta}, rejDelta=${rejDelta}, highWarnDelta=${highDelta}`);
  } else {
    lines.push(`- Change vs Previous: N/A`);
  }
  lines.push("");
  lines.push("## Top Summary");
  const topCode = Array.isArray(codes.codes) && codes.codes.length > 0 ? codes.codes[0] : null;
  const topReject = Array.isArray(rejections.reasons) && rejections.reasons.length > 0 ? rejections.reasons[0] : null;
  const topSpike = Array.isArray(anomalies.anomalies) && anomalies.anomalies.length > 0 ? anomalies.anomalies[0] : null;
  lines.push(`- Top Warning Code: ${topCode ? `${topCode.code}:${topCode.count}` : "none"}`);
  lines.push(`- Top Rejection: ${topReject ? `${topReject.reason}:${topReject.count}` : "none"}`);
  lines.push(
    `- Top Spike: ${
      topSpike
        ? `${topSpike.code} delta=${topSpike.delta} ratio=${topSpike.ratio == null ? "new" : topSpike.ratio + "x"}`
        : "none"
    }`
  );
  lines.push("");
  lines.push("## System Alerts");
  if (!alerts.alerts || alerts.alerts.length === 0) {
    lines.push("- GREEN: no system threshold alerts");
  } else {
    for (const a of alerts.alerts) lines.push(`- [${a.severity}] ${a.code}: ${a.message}`);
  }
  lines.push("");
  lines.push("## Player Alerts");
  if (!pAlerts.players || pAlerts.players.length === 0) {
    lines.push("- GREEN: no player exceeded thresholds");
  } else {
    for (const p of pAlerts.players) {
      lines.push(`- ${p.playerId}: risk=${p.riskScore}, rejected=${p.rejected}, alerts=${(p.alerts || []).join("; ")}`);
    }
  }
  lines.push("");
  lines.push("## Action Plan");
  lines.push("- [ ] Investigate top rejection reason and offending payload patterns");
  lines.push("- [ ] Check top warning code trend and confirm if expected");
  lines.push("- [ ] Re-run risk-alerts after fixes");
  lines.push("");

  const defaultOut = path.join(__dirname, "..", "data", `daily_risk_${todayStamp()}.md`);
  const outPath = args.out && String(args.out).trim() ? String(args.out) : defaultOut;
  fs.writeFileSync(outPath, lines.join("\n") + "\n", "utf8");
  console.log(`daily_report_saved=${outPath}`);
  if (args.jsonOut && String(args.jsonOut).trim()) {
    const snapshot = {
      ts: new Date().toISOString(),
      environment: envName,
      owner,
      incident: incident || null,
      window: report.window,
      summary: {
        requests: report.requests,
        warnings: { low: report.warnings.low, high: report.warnings.high },
      },
      change: prevSnapshot && prevSnapshot.summary
        ? {
            prevJson: prevJsonPath || null,
            requestsDelta: report.requests.total - (Number(prevSnapshot.summary.requests && prevSnapshot.summary.requests.total) || 0),
            rejectedDelta:
              report.requests.rejected -
              (Number(prevSnapshot.summary.requests && prevSnapshot.summary.requests.rejected) || 0),
            highWarningsDelta:
              report.warnings.high -
              (Number(prevSnapshot.summary.warnings && prevSnapshot.summary.warnings.high) || 0),
          }
        : null,
      top: {
        warningCode: topCode,
        rejection: topReject,
        spike: topSpike,
      },
      alerts: alerts.alerts || [],
      playerAlerts: pAlerts.players || [],
    };
    fs.writeFileSync(String(args.jsonOut), JSON.stringify(snapshot, null, 2) + "\n", "utf8");
    console.log(`daily_report_json_saved=${args.jsonOut}`);
  }
}

main().catch((e) => {
  console.error("daily risk template failed:", e.message);
  process.exit(1);
});
