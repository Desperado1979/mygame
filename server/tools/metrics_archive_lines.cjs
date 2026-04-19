/**
 * Copy metrics lines older than N days into metrics-archive/part-YYYY-MM-DD.ndjson (append, non-destructive).
 * Usage: node tools/metrics_archive_lines.cjs [olderThanDays] [metricsPath]
 * Default: 30 days, server/data/metrics.ndjson
 */
const fs = require("fs");
const path = require("path");

const days = process.argv[2] && /^\d+$/.test(process.argv[2]) ? parseInt(process.argv[2], 10) : 30;
const metricsPath = process.argv[3]
  ? path.resolve(process.argv[3])
  : path.join(__dirname, "..", "data", "metrics.ndjson");
const archiveDir = path.join(__dirname, "..", "data", "metrics-archive");
const cutoff = Date.now() - days * 24 * 60 * 60 * 1000;

if (!fs.existsSync(metricsPath)) {
  console.error("metrics_not_found", metricsPath);
  process.exit(2);
}
if (!fs.existsSync(archiveDir)) fs.mkdirSync(archiveDir, { recursive: true });

const text = fs.readFileSync(metricsPath, "utf8");
const lines = text.split(/\r?\n/).filter(Boolean);
let moved = 0;
const buckets = Object.create(null);

for (let i = 0; i < lines.length; i++) {
  let row;
  try {
    row = JSON.parse(lines[i]);
  } catch (_) {
    continue;
  }
  const t = Date.parse(String(row && row.ts));
  if (!Number.isFinite(t) || t >= cutoff) continue;
  const dk = new Date(t).toISOString().slice(0, 10);
  if (!buckets[dk]) buckets[dk] = [];
  buckets[dk].push(lines[i]);
  moved++;
}

for (const dk of Object.keys(buckets).sort()) {
  const fp = path.join(archiveDir, `part-${dk}.ndjson`);
  fs.appendFileSync(fp, buckets[dk].join("\n") + "\n", "utf8");
}

console.log(JSON.stringify({ ok: true, olderThanDays: days, linesArchived: moved, archiveDir }, null, 2));
