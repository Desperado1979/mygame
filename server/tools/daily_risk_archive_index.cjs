const fs = require("fs");
const path = require("path");

function parseArgs(argv) {
  const out = {
    dir: path.join(__dirname, "..", "data"),
    out: path.join(__dirname, "..", "data", "daily_risk_index.md"),
    limit: "60",
    env: "",
    jsonOut: "",
    since: "",
    until: "",
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

function extractTitle(content) {
  const lines = String(content || "").split(/\r?\n/);
  for (const line of lines) {
    const s = line.trim();
    if (s.startsWith("# ")) return s.slice(2).trim();
  }
  return "Daily Risk Report";
}

function extractMeta(content, key) {
  const re = new RegExp(`^-\\s*${key}:\\s*(.+)$`, "mi");
  const m = String(content || "").match(re);
  return m ? m[1].trim() : "N/A";
}

function toPosInt(v, fallback) {
  const n = Number(v);
  return Number.isFinite(n) && n > 0 ? Math.floor(n) : fallback;
}

function toIsoDateOrNull(v) {
  const s = String(v || "").trim();
  if (!s) return null;
  const t = Date.parse(s);
  if (!Number.isFinite(t)) return null;
  return new Date(t).toISOString().slice(0, 10);
}

function main() {
  const args = parseArgs(process.argv);
  const dir = path.resolve(String(args.dir));
  const outPath = path.resolve(String(args.out));
  const limit = toPosInt(args.limit, 60);
  const outBase = path.basename(outPath).toLowerCase();
  const envFilter = String(args.env || "").trim().toLowerCase();
  const sinceDate = toIsoDateOrNull(args.since);
  const untilDate = toIsoDateOrNull(args.until);

  if (!fs.existsSync(dir)) {
    console.error("archive dir not found:", dir);
    process.exit(1);
  }

  const files = fs
    .readdirSync(dir)
    .filter((name) => /^daily_risk_.*\.md$/i.test(name))
    .filter((name) => !/index/i.test(name))
    .filter((name) => name.toLowerCase() !== outBase)
    .map((name) => {
      const full = path.join(dir, name);
      const stat = fs.statSync(full);
      const content = fs.readFileSync(full, "utf8");
      const rel = path.relative(path.dirname(outPath), full).replace(/\\/g, "/");
      return {
        name,
        full,
        rel,
        mtimeMs: stat.mtimeMs,
        title: extractTitle(content),
        env: extractMeta(content, "Environment"),
        owner: extractMeta(content, "Owner"),
      };
    })
    .filter((item) => (envFilter ? String(item.env || "").toLowerCase() === envFilter : true))
    .filter((item) => {
      const d = new Date(item.mtimeMs).toISOString().slice(0, 10);
      if (sinceDate && d < sinceDate) return false;
      if (untilDate && d > untilDate) return false;
      return true;
    })
    .sort((a, b) => b.mtimeMs - a.mtimeMs)
    .slice(0, limit);

  const lines = [];
  lines.push("# Daily Risk Archive Index");
  lines.push("");
  lines.push(`- Generated: ${new Date().toISOString()}`);
  lines.push(`- Source Dir: ${dir.replace(/\\/g, "/")}`);
  lines.push(`- File Count: ${files.length}`);
  lines.push(`- Env Filter: ${envFilter || "all"}`);
  lines.push(`- Since: ${sinceDate || "none"}`);
  lines.push(`- Until: ${untilDate || "none"}`);
  lines.push("");
  lines.push("| Date | Env | Owner | Report |");
  lines.push("|---|---|---|---|");
  for (const f of files) {
    const date = new Date(f.mtimeMs).toISOString().slice(0, 10);
    lines.push(`| ${date} | ${f.env} | ${f.owner} | [${f.title}](${f.rel}) |`);
  }
  lines.push("");

  fs.writeFileSync(outPath, lines.join("\n"), "utf8");
  console.log(`daily_archive_index_saved=${outPath}`);
  if (args.jsonOut && String(args.jsonOut).trim()) {
    const jsonRows = files.map((f) => ({
      date: new Date(f.mtimeMs).toISOString().slice(0, 10),
      env: f.env,
      owner: f.owner,
      title: f.title,
      path: f.full,
      rel: f.rel,
    }));
    const payload = {
      generatedAt: new Date().toISOString(),
      sourceDir: dir,
      envFilter: envFilter || "all",
      since: sinceDate,
      until: untilDate,
      count: jsonRows.length,
      items: jsonRows,
    };
    fs.writeFileSync(String(args.jsonOut), JSON.stringify(payload, null, 2) + "\n", "utf8");
    console.log(`daily_archive_index_json_saved=${args.jsonOut}`);
  }
}

main();
