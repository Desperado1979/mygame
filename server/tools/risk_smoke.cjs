const { spawnSync } = require("child_process");
const fs = require("fs");
const path = require("path");

function parseArgs(argv) {
  const out = {
    base: process.env.SYNC_BASE_URL || "http://127.0.0.1:8787",
    jsonOut: "",
    markdownOut: "",
    compareWith: "",
    saveAsBaseline: "",
    historyOut: "",
    maxFailed: "",
    maxDeltaFailed: "",
    requireOk: "",
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

function run(label, cmd, args) {
  const r = spawnSync(cmd, args, { stdio: "pipe", encoding: "utf8", shell: false });
  const out = (r.stdout || "").trim();
  const err = (r.stderr || "").trim();
  const ok = r.status === 0;
  console.log(`${ok ? "OK" : "FAIL"} ${label} exit=${r.status}`);
  if (out) console.log(out);
  if (err) console.log(err);
  return { label, ok, exit: r.status, stdout: out, stderr: err };
}

function readJsonIfExists(fp) {
  try {
    if (!fp) return null;
    if (!fs.existsSync(fp)) return null;
    return JSON.parse(fs.readFileSync(fp, "utf8"));
  } catch (_) {
    return null;
  }
}

function appendHistory(fp, payload) {
  if (!fp) return;
  const line = JSON.stringify({
    ts: payload.checkedAt,
    base: payload.base,
    ok: payload.ok,
    failed: payload.failed,
  });
  fs.appendFileSync(fp, line + "\n", "utf8");
}

function main() {
  const args = parseArgs(process.argv);
  const base = String(args.base || "").trim() || "http://127.0.0.1:8787";
  const all = [
    run("doctor", "node", ["tools/risk_doctor.cjs", "--base", base]),
    run("dashboard", "node", [
      "tools/risk_dashboard.cjs",
      "--base",
      base,
      "--days",
      "7",
      "--top",
      "5",
      "--csvOut",
      "data/risk_dashboard_smoke.csv",
      "--markdownOut",
      "data/risk_dashboard_smoke.md",
    ]),
    run("alerts", "node", [
      "tools/risk_alerts.cjs",
      "--base",
      base,
      "--days",
      "7",
      "--minAcceptRate",
      "95",
      "--maxHighWarnings",
      "0",
      "--maxRejected",
      "0",
      "--minSpikeDelta",
      "2",
    ]),
  ];
  const failed = all.filter((x) => !x.ok).length;
  const payload = {
    checkedAt: new Date().toISOString(),
    base,
    failed,
    ok: failed === 0,
    steps: all,
  };
  const baselinePath = String(args.compareWith || "").trim();
  const baseline = readJsonIfExists(baselinePath);
  if (baseline && typeof baseline === "object") {
    const prevFailed = Number(baseline.failed) || 0;
    payload.compare = {
      baseline: baselinePath,
      prevFailed,
      currFailed: payload.failed,
      deltaFailed: payload.failed - prevFailed,
      prevOk: !!baseline.ok,
      currOk: payload.ok,
    };
    console.log(
      `risk_smoke_compare deltaFailed=${payload.compare.deltaFailed} prevOk=${payload.compare.prevOk} currOk=${payload.compare.currOk}`
    );
  }
  const maxFailedRaw = Number(args.maxFailed);
  const maxDeltaRaw = Number(args.maxDeltaFailed);
  const maxFailed = Number.isFinite(maxFailedRaw) ? maxFailedRaw : null;
  const maxDelta = Number.isFinite(maxDeltaRaw) ? maxDeltaRaw : null;
  const requireOk =
    String(args.requireOk || "").trim() === ""
      ? false
      : ["1", "true", "yes", "on"].includes(String(args.requireOk).trim().toLowerCase());
  const gateViolations = [];
  if (maxFailed != null && payload.failed > maxFailed)
    gateViolations.push(`failed ${payload.failed} > maxFailed ${maxFailed}`);
  if (payload.compare && maxDelta != null && payload.compare.deltaFailed > maxDelta)
    gateViolations.push(`deltaFailed ${payload.compare.deltaFailed} > maxDeltaFailed ${maxDelta}`);
  if (requireOk && !payload.ok) gateViolations.push("requireOk=true but smoke not ok");
  payload.gates = {
    maxFailed,
    maxDeltaFailed: maxDelta,
    requireOk,
    pass: gateViolations.length === 0,
    violations: gateViolations,
  };
  if (args.jsonOut && String(args.jsonOut).trim()) {
    fs.writeFileSync(String(args.jsonOut), JSON.stringify(payload, null, 2) + "\n", "utf8");
    console.log(`risk_smoke_json_saved=${args.jsonOut}`);
  }
  if (args.markdownOut && String(args.markdownOut).trim()) {
    const lines = [];
    lines.push("# Risk Smoke Report");
    lines.push("");
    lines.push(`- Checked At: ${payload.checkedAt}`);
    lines.push(`- Base: ${base}`);
    lines.push(`- Result: ${payload.ok ? "PASS" : `FAIL (${failed})`}`);
    if (payload.compare) {
      lines.push(
        `- Compare: deltaFailed=${payload.compare.deltaFailed}, prevOk=${payload.compare.prevOk}, currOk=${payload.compare.currOk}`
      );
    }
    if (payload.gates && payload.gates.maxFailed != null) lines.push(`- Gate maxFailed: ${payload.gates.maxFailed}`);
    if (payload.gates && payload.gates.maxDeltaFailed != null)
      lines.push(`- Gate maxDeltaFailed: ${payload.gates.maxDeltaFailed}`);
    if (payload.gates && payload.gates.requireOk) lines.push(`- Gate requireOk: true`);
    if (payload.gates && payload.gates.violations && payload.gates.violations.length > 0) {
      lines.push(`- Gate Result: FAIL`);
      for (const v of payload.gates.violations) lines.push(`  - ${v}`);
    } else {
      lines.push(`- Gate Result: PASS`);
    }
    lines.push("");
    lines.push("| Step | Status | Exit |");
    lines.push("|---|---|---:|");
    for (const s of all) lines.push(`| ${s.label} | ${s.ok ? "OK" : "FAIL"} | ${s.exit} |`);
    lines.push("");
    fs.writeFileSync(String(args.markdownOut), lines.join("\n"), "utf8");
    console.log(`risk_smoke_markdown_saved=${args.markdownOut}`);
  }
  const saveAsBaseline = String(args.saveAsBaseline || "").trim();
  if (saveAsBaseline) {
    fs.writeFileSync(saveAsBaseline, JSON.stringify(payload, null, 2) + "\n", "utf8");
    console.log(`risk_smoke_baseline_saved=${saveAsBaseline}`);
  }
  const historyOut = String(args.historyOut || "").trim();
  if (historyOut) {
    const resolved = path.resolve(historyOut);
    appendHistory(resolved, payload);
    console.log(`risk_smoke_history_appended=${resolved}`);
  }
  if (failed > 0) {
    console.log(`risk_smoke_result=FAIL failed=${failed}`);
    process.exit(1);
  }
  if (payload.gates && payload.gates.pass === false) {
    console.log(`risk_smoke_gate=FAIL violations=${payload.gates.violations.length}`);
    process.exit(1);
  }
  console.log("risk_smoke_result=PASS");
}

main();
