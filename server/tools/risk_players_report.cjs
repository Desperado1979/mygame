const http = require("http");

function parseArgs(argv) {
  const out = { base: process.env.SYNC_BASE_URL || "http://127.0.0.1:8787", days: "7", top: "10" };
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
  const q = new URLSearchParams({ days: String(args.days), top: String(args.top) });
  const url = `${base}/metrics/players?${q.toString()}`;
  const r = await getJson(url);
  if (r.status < 200 || r.status >= 300 || !r.body || !r.body.ok) {
    console.error("request failed", "status=" + r.status, url);
    process.exit(1);
  }
  const players = Array.isArray(r.body.players) ? r.body.players : [];
  if (players.length === 0) {
    console.log("no player risk data");
    return;
  }
  console.log(`risk players top=${players.length}`);
  for (const p of players) {
    console.log(
      `${p.playerId} | risk=${p.riskScore} | req=${p.requests} | reject=${p.rejected} | high=${p.high} | low=${p.low} | accept=${p.acceptRate}%`
    );
  }
}

main().catch((e) => {
  console.error("risk players report failed:", e.message);
  process.exit(1);
});
