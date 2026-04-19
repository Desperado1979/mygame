const http = require("http");
const fs = require("fs");

function parseArgs(argv) {
  const out = {
    base: process.env.SYNC_BASE_URL || "http://127.0.0.1:8787",
    timeoutMs: "5000",
    retries: "0",
    parallel: "1",
    autoFallbackSerial: "1",
    only: "",
    simulateFail: "",
    exitCodeMode: "simple",
    jsonOut: "",
    markdownOut: "",
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

function getJson(url, timeoutMs) {
  return new Promise((resolve, reject) => {
    const req = http.get(url, (res) => {
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
    req.setTimeout(timeoutMs, () => {
      req.destroy(new Error(`timeout_after_${timeoutMs}ms`));
    });
  });
}

async function check(name, url, predicate, timeoutMs) {
  try {
    const r = await getJson(url, timeoutMs);
    const ok = r.status >= 200 && r.status < 300 && predicate(r.body || {});
    return { name, ok, status: r.status, detail: ok ? "ok" : "unexpected_body" };
  } catch (e) {
    return { name, ok: false, status: 0, detail: e.message };
  }
}

async function checkWithRetry(name, url, predicate, timeoutMs, retries) {
  let last = null;
  for (let attempt = 0; attempt <= retries; attempt++) {
    const result = await check(name, url, predicate, timeoutMs);
    last = result;
    if (result.ok) return { ...result, attempts: attempt + 1 };
  }
  return { ...(last || { name, ok: false, status: 0, detail: "unknown_error" }), attempts: retries + 1 };
}

function suggestionFor(checkName, detail) {
  if (checkName === "health") return "start/restart persist_sync server and verify port/base URL";
  if (checkName === "metrics_report") return "ensure metrics file has rows (trigger F3 POST) and query params are valid";
  if (checkName === "alerts") return "verify /metrics/alerts route is available in current server build";
  if (checkName === "dashboard") return "upgrade server to latest code with /metrics/dashboard endpoint";
  if (checkName === "player_alerts") return "verify /metrics/alerts/players route and query parameters";
  if (String(detail || "").includes("ECONNREFUSED")) return "server not reachable, check process and firewall";
  if (String(detail || "").includes("timeout_after_")) return "increase --timeoutMs or inspect server load and network";
  return "check server logs for stack traces and endpoint registration lines";
}

function toBool(v, fallback) {
  if (v == null || v === "") return fallback;
  const s = String(v).trim().toLowerCase();
  if (["1", "true", "yes", "y", "on"].includes(s)) return true;
  if (["0", "false", "no", "n", "off"].includes(s)) return false;
  return fallback;
}

function toMarkdown(payload) {
  const lines = [];
  lines.push("# Risk Doctor Report");
  lines.push("");
  lines.push(`- Checked At: ${payload.checkedAt}`);
  lines.push(`- Base: ${payload.base}`);
  lines.push(`- Timeout: ${payload.timeoutMs}ms`);
  lines.push(`- Retries: ${payload.retries}`);
  lines.push(`- Parallel: ${payload.parallel ? "on" : "off"}`);
  lines.push(`- Auto Fallback Serial: ${payload.autoFallbackSerial ? "on" : "off"}`);
  lines.push(`- Simulate Fail: ${payload.simulateFail || "none"}`);
  lines.push(`- Exit Code Mode: ${payload.exitCodeMode}`);
  lines.push(`- Result: ${payload.ok ? "PASS" : `FAIL (${payload.failed})`}`);
  if (payload.fallbackAttempted) lines.push(`- Fallback Attempted: yes`);
  if (payload.fallbackRecovered) lines.push(`- Fallback Recovered: yes`);
  lines.push("");
  lines.push("| Check | Status | HTTP | Detail | Attempts | Fix |");
  lines.push("|---|---|---:|---|---:|---|");
  for (const c of payload.checks || []) {
    lines.push(
      `| ${c.name} | ${c.ok ? "OK" : "FAIL"} | ${c.status} | ${String(c.detail || "").replace(/\|/g, "/")} | ${
        c.attempts || 1
      } | ${String(c.fix || "").replace(/\|/g, "/")} |`
    );
  }
  lines.push("");
  return lines.join("\n") + "\n";
}

function selectExitCode(checks, mode) {
  if (mode !== "ci") return 1;
  const failedNames = new Set((checks || []).filter((c) => !c.ok).map((c) => c.name));
  if (failedNames.has("health")) return 21;
  if (failedNames.has("metrics_report")) return 22;
  if (failedNames.has("alerts")) return 23;
  if (failedNames.has("player_alerts")) return 24;
  if (failedNames.has("dashboard")) return 25;
  return 20;
}

async function main() {
  const args = parseArgs(process.argv);
  const base = String(args.base || "").replace(/\/+$/, "");
  const timeoutMsRaw = Number(args.timeoutMs);
  const timeoutMs = Number.isFinite(timeoutMsRaw) && timeoutMsRaw > 0 ? Math.floor(timeoutMsRaw) : 5000;
  const retriesRaw = Number(args.retries);
  const retries = Number.isFinite(retriesRaw) && retriesRaw >= 0 ? Math.floor(retriesRaw) : 0;
  const parallel = toBool(args.parallel, true);
  const autoFallbackSerial = toBool(args.autoFallbackSerial, true);
  const simulateFail = String(args.simulateFail || "").trim();
  const exitCodeMode = String(args.exitCodeMode || "simple").trim().toLowerCase();
  const onlyRaw = String(args.only || "").trim();
  const onlySet = new Set(
    onlyRaw
      ? onlyRaw
          .split(",")
          .map((s) => s.trim())
          .filter(Boolean)
      : []
  );
  const shouldSimFail = (name) => simulateFail && (simulateFail === "all" || simulateFail === name);
  const allTaskDefs = [
    {
      name: "health",
      run: () =>
        checkWithRetry(
      "health",
      `${base}/health`,
      (b) => b.ok === true && !shouldSimFail("health"),
      timeoutMs,
      retries
    ),
    },
    {
      name: "metrics_report",
      run: () =>
        checkWithRetry(
      "metrics_report",
      `${base}/metrics/report?days=7&top=5`,
      (b) => b.ok === true && !shouldSimFail("metrics_report"),
      timeoutMs,
      retries
    ),
    },
    {
      name: "alerts",
      run: () =>
        checkWithRetry(
      "alerts",
      `${base}/metrics/alerts?days=7`,
      (b) => b.ok === true && !shouldSimFail("alerts"),
      timeoutMs,
      retries
    ),
    },
    {
      name: "player_alerts",
      run: () =>
        checkWithRetry(
      "player_alerts",
      `${base}/metrics/alerts/players?days=7&top=5`,
      (b) => b.ok === true && !shouldSimFail("player_alerts"),
      timeoutMs,
      retries
    ),
    },
    {
      name: "dashboard",
      run: () =>
        checkWithRetry(
      "dashboard",
      `${base}/metrics/dashboard?days=7&top=5`,
      (b) => b.ok === true && !shouldSimFail("dashboard"),
      timeoutMs,
      retries
    ),
    },
  ];
  const taskDefs =
    onlySet.size > 0 ? allTaskDefs.filter((t) => onlySet.has(t.name)) : allTaskDefs;
  const checkTasks = taskDefs.map((t) => t.run);
  let checks = parallel
    ? await Promise.all(checkTasks.map((fn) => fn()))
    : await (async () => {
        const out = [];
        for (const fn of checkTasks) out.push(await fn());
        return out;
      })();
  let fallbackAttempted = false;
  let fallbackRecovered = false;
  if (parallel && autoFallbackSerial) {
    const failedInParallel = checks.some((c) => !c.ok);
    if (failedInParallel) {
      fallbackAttempted = true;
      const serialChecks = [];
      for (const fn of checkTasks) serialChecks.push(await fn());
      const serialFailed = serialChecks.some((c) => !c.ok);
      if (!serialFailed) {
        checks = serialChecks.map((c) => ({ ...c, detail: c.detail === "ok" ? "ok_after_serial_fallback" : c.detail }));
        fallbackRecovered = true;
        console.log("fallback_serial_recovered=true");
      } else {
        checks = serialChecks;
      }
    }
  }
  let failed = 0;
  const output = [];
  for (const c of checks) {
    if (!c.ok) failed += 1;
    console.log(`${c.ok ? "OK" : "FAIL"} ${c.name} status=${c.status} detail=${c.detail}`);
    const fix = c.ok ? "" : suggestionFor(c.name, c.detail);
    if (!c.ok) console.log(`  fix: ${fix}`);
    output.push({ ...c, fix: fix || null, attempts: c.attempts || 1 });
  }
  const payloadBase = {
    checkedAt: new Date().toISOString(),
    base,
    timeoutMs,
    retries,
    parallel,
    autoFallbackSerial,
    only: onlySet.size > 0 ? Array.from(onlySet) : null,
    simulateFail: simulateFail || null,
    exitCodeMode,
    fallbackAttempted,
    fallbackRecovered,
    checks: output,
  };
  if (failed > 0) {
    console.log(`risk_doctor_result=FAIL failed=${failed}`);
    if (args.jsonOut && String(args.jsonOut).trim()) {
      const payload = {
        ...payloadBase,
        ok: false,
        failed,
      };
      fs.writeFileSync(String(args.jsonOut), JSON.stringify(payload, null, 2) + "\n", "utf8");
      console.log(`risk_doctor_json_saved=${args.jsonOut}`);
    }
    if (args.markdownOut && String(args.markdownOut).trim()) {
      const payload = { ...payloadBase, ok: false, failed };
      fs.writeFileSync(String(args.markdownOut), toMarkdown(payload), "utf8");
      console.log(`risk_doctor_markdown_saved=${args.markdownOut}`);
    }
    process.exit(selectExitCode(output, exitCodeMode));
  }
  console.log("risk_doctor_result=PASS");
  if (args.jsonOut && String(args.jsonOut).trim()) {
    const payload = {
      ...payloadBase,
      ok: true,
      failed: 0,
    };
    fs.writeFileSync(String(args.jsonOut), JSON.stringify(payload, null, 2) + "\n", "utf8");
    console.log(`risk_doctor_json_saved=${args.jsonOut}`);
  }
  if (args.markdownOut && String(args.markdownOut).trim()) {
    const payload = { ...payloadBase, ok: true, failed: 0 };
    fs.writeFileSync(String(args.markdownOut), toMarkdown(payload), "utf8");
    console.log(`risk_doctor_markdown_saved=${args.markdownOut}`);
  }
}

main().catch((e) => {
  console.error("risk doctor failed:", e.message);
  process.exit(1);
});
