/**
 * Sort metrics.ndjson lines by JSON.ts ascending (stable). Dedup not applied.
 * Usage: node tools/merge_metrics_ndjson.cjs [path/to/metrics.ndjson]
 *   omit path → read stdin
 */
const fs = require("fs");

const fp = process.argv[2];
const raw = fp ? fs.readFileSync(fp, "utf8") : fs.readFileSync(0, "utf8");
const lines = raw.split(/\r?\n/).filter(Boolean);
const rows = [];
for (let i = 0; i < lines.length; i++) {
  try {
    rows.push(JSON.parse(lines[i]));
  } catch (_) {
    /* skip bad line */
  }
}
rows.sort((a, b) => {
  const ta = Date.parse(String((a && a.ts) || ""));
  const tb = Date.parse(String((b && b.ts) || ""));
  return (Number.isFinite(ta) ? ta : 0) - (Number.isFinite(tb) ? tb : 0);
});
for (let i = 0; i < rows.length; i++) console.log(JSON.stringify(rows[i]));
