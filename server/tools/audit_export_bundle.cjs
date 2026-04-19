/**
 * D10: Build a local audit/metrics bundle with an S3-style manifest (sha256 per part) for upload to object storage.
 * Does not call AWS; writes under data/audit-export/export-<iso>/.
 *
 * Usage: node tools/audit_export_bundle.cjs [--days=7] [--dataDir=../data]
 */
const fs = require("fs");
const path = require("path");
const crypto = require("crypto");

function sha256Buf(buf) {
  return crypto.createHash("sha256").update(buf).digest("hex");
}

function parseArgs() {
  const out = { days: 7, dataDir: path.join(__dirname, "..", "data") };
  for (let i = 2; i < process.argv.length; i++) {
    const a = process.argv[i];
    const m = /^--days=(\d+)$/.exec(a);
    if (m) out.days = parseInt(m[1], 10);
    const md = /^--dataDir=(.+)$/.exec(a);
    if (md) out.dataDir = path.resolve(md[1]);
  }
  return out;
}

function readMetricsWindow(metricsPath, days) {
  const cutoff = Date.now() - days * 24 * 60 * 60 * 1000;
  if (!fs.existsSync(metricsPath)) return { lines: [], lineCount: 0 };
  const text = fs.readFileSync(metricsPath, "utf8");
  const lines = text.split(/\r?\n/).filter(Boolean);
  const kept = [];
  for (let i = 0; i < lines.length; i++) {
    let row;
    try {
      row = JSON.parse(lines[i]);
    } catch (_) {
      continue;
    }
    const t = Date.parse(String(row && row.ts));
    if (!Number.isFinite(t) || t < cutoff) continue;
    kept.push(lines[i]);
  }
  return { lines: kept, lineCount: kept.length };
}

function listPlayerFiles(dataDir, metricsName) {
  const names = fs.readdirSync(dataDir);
  const parts = [];
  for (const n of names) {
    if (!n.endsWith(".json")) continue;
    if (n === "metrics.ndjson") continue;
    const fp = path.join(dataDir, n);
    let st;
    try {
      st = fs.statSync(fp);
    } catch (_) {
      continue;
    }
    if (!st.isFile()) continue;
    parts.push({ fileName: n, bytes: st.size });
  }
  parts.sort((a, b) => a.fileName.localeCompare(b.fileName));
  return parts;
}

function main() {
  const { days, dataDir } = parseArgs();
  const metricsPath = path.join(dataDir, "metrics.ndjson");
  const { lines, lineCount } = readMetricsWindow(metricsPath, days);
  const metricsBody = lines.length ? lines.join("\n") + "\n" : "";
  const metricsBuf = Buffer.from(metricsBody, "utf8");

  const playersIndex = {
    ok: true,
    dataDir: path.basename(dataDir),
    generatedAt: new Date().toISOString(),
    playerJsonFiles: listPlayerFiles(dataDir),
  };
  const indexBuf = Buffer.from(JSON.stringify(playersIndex, null, 2) + "\n", "utf8");

  const stamp = new Date().toISOString().replace(/[:.]/g, "-");
  const root = path.join(dataDir, "audit-export", `export-${stamp}`);
  fs.mkdirSync(root, { recursive: true });

  const metricsKey = "metrics-window.ndjson";
  const indexKey = "players-index.json";
  fs.writeFileSync(path.join(root, metricsKey), metricsBuf, "utf8");
  fs.writeFileSync(path.join(root, indexKey), indexBuf, "utf8");

  const bucketPrefix =
    String(process.env.AUDIT_EXPORT_BUCKET_PREFIX || "persist-sync-local").trim() || "persist-sync-local";
  const manifest = {
    ok: true,
    format: "persist_sync_audit_export_v1",
    s3Compatible: true,
    bucketPrefix,
    createdAt: new Date().toISOString(),
    windowDays: days,
    metricsSource: path.basename(metricsPath),
    metricsLinesInWindow: lineCount,
    parts: [
      { key: metricsKey, bytes: metricsBuf.length, sha256: sha256Buf(metricsBuf) },
      { key: indexKey, bytes: indexBuf.length, sha256: sha256Buf(indexBuf) },
    ],
    uploadHint:
      "aws s3 cp with same keys under {bucket}/{prefix}/export-.../ ; verify sha256 after download.",
  };
  fs.writeFileSync(path.join(root, "manifest.json"), JSON.stringify(manifest, null, 2) + "\n", "utf8");

  console.log(JSON.stringify({ ok: true, outDir: root, manifest: path.join(root, "manifest.json") }, null, 2));
}

main();
