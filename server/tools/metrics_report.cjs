/**
 * Summarize server/data/metrics.ndjson for quick warning trend checks.
 *
 * Usage (from EpochOfDawn/server):
 *   node tools/metrics_report.cjs
 *   node tools/metrics_report.cjs --days 1 --top 5
 *   node tools/metrics_report.cjs --days 7 --top 10 --player local_player_001
 *
 * - --days N: only include entries with ts >= now - N days (default: all)
 * - --top N: print top N warning codes by count (default: 10)
 * - --player ID: only include rows for this playerId
 */
const fs = require("fs");
const path = require("path");

const metricsPath = path.join(__dirname, "..", "data", "metrics.ndjson");

function argValue(flag, fallback) {
  const i = process.argv.indexOf(flag);
  if (i < 0 || i + 1 >= process.argv.length) return fallback;
  return process.argv[i + 1];
}

function argPositiveNumber(flag, fallback) {
  const raw = argValue(flag, null);
  if (raw == null) return fallback;
  const n = Number(raw);
  return Number.isFinite(n) && n > 0 ? n : fallback;
}

const days = argPositiveNumber("--days", null);
const topN = argPositiveNumber("--top", 10);
const playerFilterRaw = argValue("--player", null);
const playerFilter = playerFilterRaw && playerFilterRaw.trim() ? playerFilterRaw.trim() : null;
const cutoffMs = days == null ? null : Date.now() - days * 24 * 60 * 60 * 1000;

if (!fs.existsSync(metricsPath)) {
  console.log("No metrics file yet:", metricsPath);
  process.exit(0);
}

const lines = fs.readFileSync(metricsPath, "utf8").split(/\r?\n/).filter(Boolean);
if (lines.length === 0) {
  console.log("metrics.ndjson is empty.");
  process.exit(0);
}

let total = 0;
let accepted = 0;
let rejected = 0;
let lowWarnings = 0;
let highWarnings = 0;
const codeCounts = {};

for (const line of lines) {
  let row;
  try {
    row = JSON.parse(line);
  } catch (_) {
    continue;
  }
  if (!row || typeof row !== "object") continue;

  if (cutoffMs != null) {
    const t = Date.parse(String(row.ts || ""));
    if (!Number.isFinite(t) || t < cutoffMs) continue;
  }
  if (playerFilter && row.playerId !== playerFilter) continue;

  total++;
  if (row.accepted === true) accepted++;
  else if (row.accepted === false) rejected++;

  const ws = row.warningSummary || {};
  const low = Number(ws.low) || 0;
  const high = Number(ws.high) || 0;
  lowWarnings += low;
  highWarnings += high;

  const byCode = row.warningsByCode || {};
  for (const k of Object.keys(byCode)) {
    const c = Number(byCode[k]) || 0;
    if (c <= 0) continue;
    codeCounts[k] = (codeCounts[k] || 0) + c;
  }
}

if (total === 0) {
  console.log("No metrics rows matched the current filter.");
  process.exit(0);
}

const acceptRate = ((accepted / total) * 100).toFixed(1);
const top = Object.entries(codeCounts)
  .sort((a, b) => b[1] - a[1])
  .slice(0, topN);

console.log("=== metrics report ===");
console.log("file:", metricsPath);
console.log("window:", days == null ? "all" : `last ${days} day(s)`);
console.log("player:", playerFilter || "all");
console.log("requests:", total, `accepted=${accepted}`, `rejected=${rejected}`, `acceptRate=${acceptRate}%`);
console.log("warnings:", `low=${lowWarnings}`, `high=${highWarnings}`);

if (top.length === 0) {
  console.log("top warning codes: (none)");
} else {
  console.log(`top warning codes (top ${top.length}):`);
  for (const [code, count] of top)
    console.log(` - ${code}: ${count}`);
}
