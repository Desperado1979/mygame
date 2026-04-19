const http = require("http");

function parseArgs(argv) {
  const out = {
    base: process.env.SYNC_BASE_URL || "http://127.0.0.1:8787",
    days: "7",
    top: "5",
    hours: "24",
    compareHours: "24",
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

async function main() {
  const args = parseArgs(process.argv);
  const base = String(args.base || "").replace(/\/+$/, "");
  const days = String(args.days);
  const top = String(args.top);
  const hours = String(args.hours);
  const compareHours = String(args.compareHours);

  const [report, players, rejections, anomalies] = await Promise.all([
    mustGet(`${base}/metrics/report?days=${encodeURIComponent(days)}&top=${encodeURIComponent(top)}`),
    mustGet(`${base}/metrics/players?days=${encodeURIComponent(days)}&top=${encodeURIComponent(top)}`),
    mustGet(`${base}/metrics/rejections?days=${encodeURIComponent(days)}`),
    mustGet(
      `${base}/metrics/anomalies?hours=${encodeURIComponent(hours)}&compareHours=${encodeURIComponent(compareHours)}&top=${encodeURIComponent(
        top
      )}&minCount=1`
    ),
  ]);

  if (!report.hasData) {
    console.log("risk brief: no data");
    return;
  }

  console.log("=== risk brief ===");
  console.log(`window=${report.window} player=${report.playerId}`);
  console.log(
    `requests=${report.requests.total} accepted=${report.requests.accepted} rejected=${report.requests.rejected} acceptRate=${report.requests.acceptRate}%`
  );
  console.log(`warnings low=${report.warnings.low} high=${report.warnings.high}`);

  const topPlayer = Array.isArray(players.players) && players.players.length > 0 ? players.players[0] : null;
  if (topPlayer) {
    console.log(
      `top player=${topPlayer.playerId} risk=${topPlayer.riskScore} req=${topPlayer.requests} rej=${topPlayer.rejected}`
    );
  }

  const topReject =
    Array.isArray(rejections.reasons) && rejections.reasons.length > 0 ? rejections.reasons[0] : null;
  if (topReject) console.log(`top rejection=${topReject.reason}:${topReject.count}`);
  else console.log("top rejection=(none)");

  const topSpike =
    Array.isArray(anomalies.anomalies) && anomalies.anomalies.length > 0 ? anomalies.anomalies[0] : null;
  if (topSpike) {
    const ratio = topSpike.ratio == null ? "new" : `${topSpike.ratio}x`;
    console.log(`top spike=${topSpike.code} delta=${topSpike.delta} ratio=${ratio}`);
  } else {
    console.log("top spike=(none)");
  }
}

main().catch((e) => {
  console.error("risk brief failed:", e.message);
  process.exit(1);
});
