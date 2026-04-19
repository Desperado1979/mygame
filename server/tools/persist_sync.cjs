/**
 * D5 minimal persistence: POST /sync writes full JSON body to server/data/<playerId>.json
 * GET /state?playerId=... returns last saved document. GET /players lists saved ids.
 * D16: ETag + If-Match (optimistic concurrency) on GET /state, POST /sync, POST /rehearsal/apply-patch.
 *
 * Run (repo root = EpochOfDawn):
 *   node server/tools/persist_sync.cjs
 *
 * Port: env PORT or default 8787 (same as Unity localSyncUrl default).
 */
const http = require("http");
const crypto = require("crypto");
const fs = require("fs");
const path = require("path");
const { URL } = require("url");
const {
  validateClientSyncPayload,
  replayGoldTailForCli,
  replayPotionInventoryTail,
} = require("./audit_validate.cjs");
const { validateSyncPayloadSchemas } = require("./schema_validate.cjs");
const { validatePatchRehearsalRequest } = require("./patch_validate_rehearsal.cjs");
const { syncAuditStateStrict } = require("./audit_validate.cjs");

let schemaSkipNoticeLogged = false;

const port = Number(process.env.PORT) || 8787;
const dataDir = path.join(__dirname, "..", "data");
const metricsFilePath = path.join(dataDir, "metrics.ndjson");
const rejectHighWarnings =
  String(process.env.REJECT_HIGH_WARNINGS || "").trim().toLowerCase() === "1" ||
  String(process.env.REJECT_HIGH_WARNINGS || "").trim().toLowerCase() === "true";

/** P3-2: reject POST /sync if audit[] contains any category starting with SrvVal_ (strict rehearsal / staging). */
const rejectAnySrvValAudit =
  String(process.env.REJECT_SRVVAL_AUDIT || "").trim().toLowerCase() === "1" ||
  String(process.env.REJECT_SRVVAL_AUDIT || "").trim().toLowerCase() === "true";

function getSrvValCategoryRejectList() {
  const raw = String(process.env.SRVVAL_REJECT_CATEGORIES || "").trim();
  if (!raw) return [];
  return raw
    .split(/[,|]/)
    .map((s) => s.trim())
    .filter(Boolean);
}

function parseJsonEnvObject(name) {
  const raw = String(process.env[name] || "").trim();
  if (!raw) return null;
  try {
    const o = JSON.parse(raw);
    return o && typeof o === "object" ? o : null;
  } catch (_) {
    return null;
  }
}

/**
 * P3-2: optional reject when this sync's auditSummary.byCategory[cat] >= threshold (number).
 * Example: SRVVAL_REJECT_THRESHOLD_JSON={"SrvVal_IllegalOperation":1}
 */
function evaluateSrvValThresholdReject(auditSummary) {
  const th = parseJsonEnvObject("SRVVAL_REJECT_THRESHOLD_JSON");
  if (!th) return null;
  const bc = auditSummary && auditSummary.byCategory;
  if (!bc || typeof bc !== "object") return null;
  const violations = [];
  for (const [cat, lim] of Object.entries(th)) {
    const n = Number(lim);
    if (!Number.isFinite(n) || n < 0) continue;
    const got = Number(bc[cat]) || 0;
    if (got >= n) violations.push({ category: cat, count: got, threshold: n });
  }
  return violations.length ? violations : null;
}

/** P3-2: log only — when count >= threshold for that category (does not reject). */
function logSrvValAuditAlerts(auditSummary) {
  const th = parseJsonEnvObject("SRVVAL_ALERT_THRESHOLD_JSON");
  if (!th) return;
  const bc = auditSummary && auditSummary.byCategory;
  if (!bc || typeof bc !== "object") return;
  for (const [cat, lim] of Object.entries(th)) {
    const n = Number(lim);
    if (!Number.isFinite(n) || n < 0) continue;
    const got = Number(bc[cat]) || 0;
    if (got >= n)
      console.warn(new Date().toISOString(), "srvval_audit_alert", cat, `count=${got}`, `threshold=${n}`);
  }
}

/**
 * P3-2: returns { error, detail } or null.
 * - SRVVAL_REJECT_CATEGORIES= comma list → reject if any audit row matches
 * - REJECT_SRVVAL_AUDIT=1 → reject if any audit row category starts with SrvVal_
 * - SRVVAL_REJECT_THRESHOLD_JSON → reject if byCategory counts exceed (see evaluateSrvValThresholdReject)
 */
function evaluateSrvValSyncReject(body, auditSummary) {
  const audit = body.audit;
  const list = getSrvValCategoryRejectList();
  if (list.length > 0 && Array.isArray(audit)) {
    const hits = [];
    for (const row of audit) {
      const c = row && typeof row.category === "string" ? row.category.trim() : "";
      if (c && list.includes(c)) hits.push(c);
    }
    if (hits.length)
      return {
        error: "srvval_category_block",
        detail: { blockedSrvValCategories: [...new Set(hits)] },
      };
  }

  if (rejectAnySrvValAudit && Array.isArray(audit)) {
    const hits = [];
    for (const row of audit) {
      const c = row && typeof row.category === "string" ? row.category.trim() : "";
      if (c.startsWith("SrvVal_")) hits.push(c);
    }
    if (hits.length)
      return {
        error: "srvval_audit_block",
        detail: { blockedSrvValCategories: [...new Set(hits)] },
      };
  }

  const viol = evaluateSrvValThresholdReject(auditSummary);
  if (viol && viol.length)
    return { error: "srvval_threshold_block", detail: { blockedBySrvValThreshold: viol } };

  return null;
}

function ensureDataDir() {
  if (!fs.existsSync(dataDir)) fs.mkdirSync(dataDir, { recursive: true });
}

function safePlayerId(id) {
  if (id == null || typeof id !== "string") return "anonymous";
  const t = id.trim();
  if (!t) return "anonymous";
  const s = t.replace(/[^a-zA-Z0-9_-]/g, "_").slice(0, 128);
  return s || "anonymous";
}

function filePathForPlayer(safeId) {
  return path.join(dataDir, `${safeId}.json`);
}

/** D16: SHA-256 of file bytes (strong ETag, hex). */
function sha256HexBuffer(buf) {
  return crypto.createHash("sha256").update(buf).digest("hex");
}

function etagForFilePath(fp) {
  try {
    if (!fs.existsSync(fp)) return null;
    return sha256HexBuffer(fs.readFileSync(fp));
  } catch (_) {
    return null;
  }
}

/** D16: undefined = absent, "*" = wildcard (skip check). */
function parseIfMatchHeader(req) {
  const raw = req.headers["if-match"];
  if (raw == null || raw === "") return undefined;
  const s = String(raw).trim();
  if (s === "*") return "*";
  return s.replace(/^W\//i, "").replace(/^"|"$/g, "").trim() || undefined;
}

/**
 * D16: If-Match present and SYNC_ETAG_DISABLED unset → 412 unless SHA-256(hex) matches current file bytes.
 * First write (no file): If-Match must be absent or *.
 */
function syncEtagCheckDisabled() {
  return (
    String(process.env.SYNC_ETAG_DISABLED || "").trim().toLowerCase() === "1" ||
    String(process.env.SYNC_ETAG_DISABLED || "").trim().toLowerCase() === "true"
  );
}

function ifMatchPreconditionFailed(req, fp) {
  if (syncEtagCheckDisabled()) return null;
  const want = parseIfMatchHeader(req);
  if (want === undefined || want === "*") return null;
  const have = etagForFilePath(fp);
  if (have == null) return { reason: "if_match_but_no_save", want };
  if (want !== have) return { reason: "if_match_mismatch", currentEtag: have };
  return null;
}

function etagHeaderValue(hex) {
  return hex ? `"${hex}"` : undefined;
}

function sendJson(res, status, obj, extraHeaders) {
  const body = JSON.stringify(obj);
  const h = {
    "Content-Type": "application/json; charset=utf-8",
    "Content-Length": Buffer.byteLength(body, "utf8"),
  };
  if (extraHeaders && typeof extraHeaders === "object") {
    for (const k of Object.keys(extraHeaders)) h[k] = extraHeaders[k];
  }
  res.writeHead(status, h);
  res.end(body);
}

function msSince(t0) {
  return Math.max(0, Date.now() - t0);
}

function tryPlayerIdFromJsonText(text) {
  try {
    const j = JSON.parse(text);
    if (j && typeof j.playerId === "string" && j.playerId.trim()) return j.playerId.trim();
  } catch (_) {
    /* ignore */
  }
  return "_";
}

function sendText(res, status, text, contentType = "text/plain; charset=utf-8") {
  const body = String(text || "");
  res.writeHead(status, {
    "Content-Type": contentType,
    "Content-Length": Buffer.byteLength(body, "utf8"),
  });
  res.end(body);
}

function buildWarningCodeMap(warnings) {
  const map = {};
  if (!Array.isArray(warnings)) return map;
  for (const w of warnings) {
    const code = w && typeof w.code === "string" && w.code.trim() ? w.code.trim() : "unknown_warning";
    map[code] = (map[code] || 0) + 1;
  }
  return map;
}

/** P3-1: summarize client audit[] by category (e.g. SrvVal_IllegalOperation). Does not judge validity — observation only. */
function summarizeAuditByCategory(audit) {
  const byCategory = Object.create(null);
  let total = 0;
  if (!Array.isArray(audit)) return { total: 0, byCategory: {} };
  for (const row of audit) {
    if (!row || typeof row.category !== "string") continue;
    total++;
    const c = row.category.trim() || "unknown";
    byCategory[c] = (byCategory[c] || 0) + 1;
  }
  return { total, byCategory };
}

function parsePositiveInt(raw, fallback) {
  const n = Number(raw);
  return Number.isFinite(n) && n > 0 ? Math.floor(n) : fallback;
}

function parseBool(raw, fallback = false) {
  if (raw == null) return fallback;
  const s = String(raw).trim().toLowerCase();
  if (["1", "true", "yes", "y", "on"].includes(s)) return true;
  if (["0", "false", "no", "n", "off"].includes(s)) return false;
  return fallback;
}

const maintenanceMode = parseBool(process.env.MAINTENANCE_MODE, false);
const syncRateLimitPerMinute = (() => {
  const n = Number(process.env.SYNC_RATE_LIMIT_PER_MINUTE || "0");
  return Number.isFinite(n) && n > 0 ? Math.floor(n) : 0;
})();
const syncIdempotencyTtlMs = (() => {
  const n = Number(process.env.SYNC_IDEMPOTENCY_TTL_MS || "0");
  return Number.isFinite(n) && n > 0 ? Math.floor(n) : 0;
})();
/** D12：将幂等 200 缓存落盘，进程重启后仍可重放（单实例；多进程请改 Redis）。 */
const syncIdempotencyPersist = parseBool(process.env.SYNC_IDEMPOTENCY_PERSIST, false);
const idempotencyPersistPath = path.join(dataDir, "idempotency-cache.json");
const syncHmacSecret = String(process.env.SYNC_HMAC_SECRET || "").trim();
const syncRequireStagingHeader = parseBool(process.env.SYNC_REQUIRE_STAGING_HEADER, false);
const syncIssuedAtMaxSkewSec = (() => {
  const n = Number(process.env.SYNC_ISSUED_AT_MAX_SKEW_SEC || "0");
  return Number.isFinite(n) && n > 0 ? n : 0;
})();

/** D10：控制台日志脱敏（邮箱、疑似卡号、IP 末段）；不改写 metrics 落盘内容。 */
const logRedactPii =
  String(process.env.LOG_REDACT_PII || "").trim().toLowerCase() === "1" ||
  String(process.env.LOG_REDACT_PII || "").trim().toLowerCase() === "true";

const complianceMetricsRetentionHintDays = (() => {
  const n = Number(process.env.COMPLIANCE_METRICS_RETENTION_HINT_DAYS || "0");
  return Number.isFinite(n) && n > 0 ? Math.floor(n) : null;
})();
const compliancePlayerStateRetentionHintDays = (() => {
  const n = Number(process.env.COMPLIANCE_PLAYER_STATE_RETENTION_HINT_DAYS || "0");
  return Number.isFinite(n) && n > 0 ? Math.floor(n) : null;
})();
const auditExportBucketPrefix = String(process.env.AUDIT_EXPORT_BUCKET_PREFIX || "persist-sync-local").trim() || "persist-sync-local";

/** D15：允许 POST /rehearsal/apply-patch 写盘（仅排练；默认关闭）。 */
const rehearsalPatchWrite = parseBool(process.env.REHEARSAL_PATCH_WRITE, false);

/** D11：将 `audit_validate` 的 `warnings[].code` 按表映射为 `SrvVal_*` 计数（观测；非服务端裁决）。 */
const syncWarningSrvValBridge = parseBool(process.env.SYNC_WARNING_SRVVAL_BRIDGE, false);
const defaultWarningCodeToSrvVal = Object.freeze({
  state_version_vs_schema_mismatch: "SrvVal_StateReject",
  gold_tail_mismatch: "SrvVal_WalletReject",
  inventory_hp_tail_mismatch: "SrvVal_StorageReject",
  inventory_mp_tail_mismatch: "SrvVal_StorageReject",
  player_id_mismatch: "SrvVal_StateReject",
  unexpected_schema_version: "SrvVal_StateReject",
});
const warningCodeToSrvValMapResolved = (() => {
  const raw = String(process.env.WARNING_CODE_TO_SRVVAL_JSON || "").trim();
  if (raw) {
    try {
      const o = JSON.parse(raw);
      if (o && typeof o === "object" && Object.keys(o).length > 0) return o;
      return null;
    } catch (_) {
      return null;
    }
  }
  if (syncWarningSrvValBridge) return { ...defaultWarningCodeToSrvVal };
  return null;
})();

function buildSrvValFromWarnings(warningsByCode, map) {
  if (!map || !warningsByCode || typeof warningsByCode !== "object") return null;
  const byCategory = Object.create(null);
  for (const [code, count] of Object.entries(warningsByCode)) {
    const n = Number(count);
    if (!Number.isFinite(n) || n <= 0) continue;
    const target = map[code];
    if (target == null || target === "") continue;
    const t = String(target).trim();
    if (!t.startsWith("SrvVal_")) continue;
    byCategory[t] = (byCategory[t] || 0) + n;
  }
  let total = 0;
  for (const k of Object.keys(byCategory)) total += byCategory[k];
  return total > 0 ? { total, byCategory } : null;
}

function attachSrvValFromWarningsToAuditSummary(auditSummary, warningsByCode) {
  const bridge = buildSrvValFromWarnings(warningsByCode, warningCodeToSrvValMapResolved);
  if (bridge) auditSummary.srvValFromWarnings = bridge;
  return auditSummary;
}

/** D10：仅用于 console 输出，避免排练日志误带敏感片段。 */
function redactPiiForLog(s) {
  if (!logRedactPii) return s;
  let t = String(s);
  t = t.replace(/\b[\w.%+-]+@[\w.-]+\.[A-Za-z]{2,}\b/g, "[redacted-email]");
  t = t.replace(/\b(?:\d{4}[-\s]?){3}\d{4}\b/g, "[redacted-digits]");
  t = t.replace(/(\d{1,3}\.\d{1,3}\.\d{1,3}\.)\d{1,3}\b/g, "$1*");
  return t;
}

function logPathForSyncSaved(fp, safe) {
  if (logRedactPii) return String(safe || "anonymous") + ".json";
  return fp;
}

const rateLimitTimestampsByIp = new Map();
const idempotencyResponseCache = new Map();

function clientIpFromReq(req) {
  const xf = req.headers["x-forwarded-for"];
  if (typeof xf === "string" && xf.trim()) return xf.split(",")[0].trim();
  return req.socket && req.socket.remoteAddress ? String(req.socket.remoteAddress) : "unknown";
}

function checkSyncRateLimit(ip) {
  if (!syncRateLimitPerMinute || syncRateLimitPerMinute <= 0) return { ok: true };
  const now = Date.now();
  const windowMs = 60 * 1000;
  let arr = rateLimitTimestampsByIp.get(ip);
  if (!arr) {
    arr = [];
    rateLimitTimestampsByIp.set(ip, arr);
  }
  while (arr.length && arr[0] < now - windowMs) arr.shift();
  if (arr.length >= syncRateLimitPerMinute)
    return { ok: false, retryAfterSec: Math.ceil((arr[0] + windowMs - now) / 1000) || 60 };
  arr.push(now);
  return { ok: true };
}

function idempotencyCompositeKey(headerKey, rawText) {
  if (!headerKey || syncIdempotencyTtlMs <= 0) return null;
  const hk = String(headerKey).trim();
  if (!hk) return null;
  return crypto.createHash("sha256").update(hk + "\n" + rawText, "utf8").digest("hex");
}

function idempotencyCacheGet(compositeKey) {
  if (!compositeKey) return null;
  const row = idempotencyResponseCache.get(compositeKey);
  if (!row) return null;
  if (row.expires < Date.now()) {
    idempotencyResponseCache.delete(compositeKey);
    return null;
  }
  try {
    return { status: row.status, bodyObj: JSON.parse(row.bodyJson) };
  } catch (_) {
    idempotencyResponseCache.delete(compositeKey);
    return null;
  }
}

function idempotencyPersistSnapshot() {
  if (!syncIdempotencyPersist || syncIdempotencyTtlMs <= 0) return;
  try {
    ensureDataDir();
    const now = Date.now();
    const entries = {};
    for (const [k, v] of idempotencyResponseCache.entries()) {
      if (!v || v.expires < now) continue;
      entries[k] = { expires: v.expires, status: v.status, bodyJson: v.bodyJson };
    }
    const payload =
      JSON.stringify({
        v: 1,
        savedAt: new Date().toISOString(),
        entries,
      }) + "\n";
    const tmp = idempotencyPersistPath + ".tmp";
    fs.writeFileSync(tmp, payload, "utf8");
    fs.renameSync(tmp, idempotencyPersistPath);
  } catch (e) {
    console.warn(
      new Date().toISOString(),
      "idempotency_persist_failed",
      String(e && e.message ? e.message : e)
    );
  }
}

function idempotencyLoadFromDisk() {
  if (!syncIdempotencyPersist || syncIdempotencyTtlMs <= 0) return;
  try {
    if (!fs.existsSync(idempotencyPersistPath)) return;
    const raw = fs.readFileSync(idempotencyPersistPath, "utf8");
    const j = JSON.parse(raw);
    const ent = j && j.entries && typeof j.entries === "object" ? j.entries : null;
    if (!ent) return;
    const now = Date.now();
    let n = 0;
    for (const [k, v] of Object.entries(ent)) {
      if (!v || typeof v !== "object") continue;
      if (typeof v.expires !== "number" || v.expires < now) continue;
      if (typeof v.bodyJson !== "string" || typeof v.status !== "number") continue;
      idempotencyResponseCache.set(k, {
        expires: v.expires,
        status: v.status,
        bodyJson: v.bodyJson,
      });
      n++;
    }
    if (n > 0) console.log(new Date().toISOString(), "idempotency_loaded_from_disk", `entries=${n}`);
    idempotencyPersistSnapshot();
  } catch (e) {
    console.warn(
      new Date().toISOString(),
      "idempotency_load_from_disk_failed",
      String(e && e.message ? e.message : e)
    );
  }
}

function idempotencyCacheSet(compositeKey, status, bodyObj) {
  if (!compositeKey || syncIdempotencyTtlMs <= 0 || status !== 200) return;
  if (idempotencyResponseCache.size > 800) {
    const now = Date.now();
    for (const [k, v] of idempotencyResponseCache.entries()) {
      if (v.expires < now) idempotencyResponseCache.delete(k);
    }
  }
  idempotencyResponseCache.set(compositeKey, {
    expires: Date.now() + syncIdempotencyTtlMs,
    status,
    bodyJson: JSON.stringify(bodyObj),
  });
  idempotencyPersistSnapshot();
}

function verifySyncHmacBody(rawText, req) {
  if (!syncHmacSecret) return true;
  const sig = req.headers["x-sync-signature"];
  if (!sig || typeof sig !== "string") return false;
  const expected = crypto.createHmac("sha256", syncHmacSecret).update(rawText, "utf8").digest("hex");
  const a = Buffer.from(sig.trim(), "utf8");
  const b = Buffer.from(expected, "utf8");
  if (a.length !== b.length) return false;
  return crypto.timingSafeEqual(a, b);
}

function isoOrNull(raw) {
  if (!raw) return null;
  const t = Date.parse(String(raw));
  return Number.isFinite(t) ? t : null;
}

function appendMetrics(entry) {
  try {
    ensureDataDir();
    fs.appendFileSync(metricsFilePath, JSON.stringify(entry) + "\n", "utf8");
  } catch (e) {
    console.warn(new Date().toISOString(), "metrics_append_failed", String(e && e.message ? e.message : e));
  }
}

function dateKeyFromTs(ts) {
  const t = Date.parse(String(ts || ""));
  if (!Number.isFinite(t)) return null;
  return new Date(t).toISOString().slice(0, 10);
}

function hourKeyFromTs(ts) {
  const t = Date.parse(String(ts || ""));
  if (!Number.isFinite(t)) return null;
  return new Date(t).toISOString().slice(0, 13) + ":00Z";
}

function readMetricsRows(options) {
  if (!fs.existsSync(metricsFilePath)) {
    return { ok: true, hasData: false, reason: "metrics_file_not_found", rows: [] };
  }
  const lines = fs.readFileSync(metricsFilePath, "utf8").split(/\r?\n/).filter(Boolean);
  if (lines.length === 0) {
    return { ok: true, hasData: false, reason: "metrics_file_empty", rows: [] };
  }

  const daysNum = Number(options.days);
  const playerFilter =
    typeof options.playerId === "string" && options.playerId.trim() ? options.playerId.trim() : null;
  const useDays = Number.isFinite(daysNum) && daysNum > 0 ? daysNum : null;
  const cutoffMs = useDays == null ? null : Date.now() - useDays * 24 * 60 * 60 * 1000;
  const sinceMs = isoOrNull(options.since);
  const untilMs = isoOrNull(options.until);
  const filterRejected = parseBool(options.rejectedOnly, false);

  const rows = [];
  for (const line of lines) {
    let row;
    try {
      row = JSON.parse(line);
    } catch (_) {
      continue;
    }
    if (!row || typeof row !== "object") continue;
    const t = Date.parse(String(row.ts || ""));
    if (!Number.isFinite(t)) continue;
    if (cutoffMs != null && t < cutoffMs) continue;
    if (sinceMs != null && t < sinceMs) continue;
    if (untilMs != null && t > untilMs) continue;
    if (playerFilter && row.playerId !== playerFilter) continue;
    if (filterRejected && row.accepted !== false) continue;
    rows.push(row);
  }
  return { ok: true, hasData: rows.length > 0, reason: rows.length ? null : "no_rows_in_window", rows };
}

/** Merge auditSummary.byCategory across metrics rows (P3 batch observability). */
function aggregateAuditCategoriesFromRows(rows) {
  const byCategory = Object.create(null);
  let rowsWithSummary = 0;
  if (!Array.isArray(rows)) return { rowsWithSummary: 0, total: 0, byCategory: {} };
  for (const row of rows) {
    const as = row && row.auditSummary;
    if (!as || typeof as !== "object") continue;
    rowsWithSummary++;
    const bc = as.byCategory;
    if (bc && typeof bc === "object") {
      for (const k of Object.keys(bc)) {
        const n = Number(bc[k]) || 0;
        byCategory[k] = (byCategory[k] || 0) + n;
      }
    }
    const sw = as.srvValFromWarnings;
    if (sw && sw.byCategory && typeof sw.byCategory === "object") {
      for (const k of Object.keys(sw.byCategory)) {
        const n = Number(sw.byCategory[k]) || 0;
        byCategory[k] = (byCategory[k] || 0) + n;
      }
    }
  }
  let total = 0;
  for (const k of Object.keys(byCategory)) total += byCategory[k];
  return { rowsWithSummary, total, byCategory };
}

/** SLO-ish: rows from metrics.ndjson that recorded POST /sync outcome (accepted boolean). */
function aggregateSyncSloFromRows(rows) {
  let accepted = 0;
  let rejected = 0;
  const byError = Object.create(null);
  if (!Array.isArray(rows)) {
    return {
      accepted: 0,
      rejected: 0,
      totalWithOutcome: 0,
      acceptRatePercent: null,
      byError: {},
    };
  }
  for (let i = 0; i < rows.length; i++) {
    const row = rows[i];
    if (row.accepted === true) accepted++;
    else if (row.accepted === false) {
      rejected++;
      const e = row.error && String(row.error).trim() ? String(row.error).trim() : "unknown_reject";
      byError[e] = (byError[e] || 0) + 1;
    }
  }
  const total = accepted + rejected;
  const acceptRatePercent = total > 0 ? Math.round((accepted / total) * 10000) / 100 : null;
  return { accepted, rejected, totalWithOutcome: total, acceptRatePercent, byError };
}

/** Wall-clock ms recorded on each POST /sync metrics row (handler time). */
function aggregateLatencyMsPercentiles(rows) {
  const arr = [];
  if (!Array.isArray(rows)) {
    return { sampleCount: 0, p50: null, p90: null, p99: null, mean: null };
  }
  for (let i = 0; i < rows.length; i++) {
    const row = rows[i];
    const n = row && row.durationMs;
    if (typeof n !== "number" || !Number.isFinite(n) || n < 0) continue;
    arr.push(n);
  }
  arr.sort((a, b) => a - b);
  const n = arr.length;
  if (n === 0) return { sampleCount: 0, p50: null, p90: null, p99: null, mean: null };
  const pick = (q) => arr[Math.min(n - 1, Math.max(0, Math.floor(q * (n - 1))))];
  let sum = 0;
  for (let i = 0; i < n; i++) sum += arr[i];
  return {
    sampleCount: n,
    p50: pick(0.5),
    p90: pick(0.9),
    p99: pick(0.99),
    mean: Math.round((sum / n) * 100) / 100,
  };
}

const mockSync200Example = {
  ok: true,
  saved: "rehearsal_mock.json",
  schemaVersion: 1,
  playerId: "rehearsal_mock",
  auditCount: 0,
  auditSummary: { total: 0, byCategory: {} },
  bytes: 0,
  validation: {
    ok: true,
    warningSummary: { low: 0, high: 0 },
    warningsByCode: {},
    warnings: [],
  },
};

/** 波 9：审计尾 vs state 观测（非权威裁定）。 */
function buildReplayObservation(body) {
  try {
    if (!body || !Array.isArray(body.audit) || !body.state) return null;
    const audits = [...body.audit].sort((a, b) => (a.seq || 0) - (b.seq || 0));
    const g = replayGoldTailForCli(audits);
    const p = replayPotionInventoryTail(audits);
    const st = body.state;
    const inv = st.inventory;
    const o = { v: 1 };
    if (g.chainErrors.length === 0 && g.goldTail != null && typeof st.gold === "number")
      o.goldMatch = g.goldTail === st.gold;
    else o.goldMatch = null;
    if (p.hp != null && inv && typeof inv.hpPotion === "number") o.hpPotionMatch = p.hp === inv.hpPotion;
    else o.hpPotionMatch = null;
    if (p.mp != null && inv && typeof inv.mpPotion === "number") o.mpPotionMatch = p.mp === inv.mpPotion;
    else o.mpPotionMatch = null;
    return o;
  } catch (_) {
    return null;
  }
}

const patchStrategyDoc = {
  ok: true,
  doc: "patch_strategy_draft_v0",
  intent: "Future full PATCH of player_state; current server is full-document replace only.",
  rules: [
    "D16: GET /state + POST /sync + POST /rehearsal/apply-patch expose ETag (SHA-256 of saved JSON bytes); send If-Match with hex (or quoted) for optimistic concurrency (412 on mismatch).",
    "Merge semantics: server wins on conflict unless audit proves replay tail.",
    "Audit ring buffer may truncate; PATCH must not assume complete audit tail.",
    "Rehearsal: POST /rehearsal/validate-patch applies whitelisted JSON-Patch-style ops to a state preview (no disk write).",
    "D15: POST /rehearsal/apply-patch merges ops into state on disk when REHEARSAL_PATCH_WRITE=1 (rehearsal gate).",
  ],
};

/** D10：合规/留存提示 + S3 兼容导出约定（ rehearsal；真实策略以部署与法务为准）。 */
function buildComplianceBundlePayload() {
  return {
    ok: true,
    doc: "compliance_bundle_draft_v1",
    ts: new Date().toISOString(),
    auditExportBucketPrefix,
    logRedactPii,
    retentionHints: {
      metricsDays: complianceMetricsRetentionHintDays,
      playerStateDays: compliancePlayerStateRetentionHintDays,
      note:
        "Hints only; enforcement = bucket lifecycle rules, DB TTL, or offline jobs — not implemented in persist_sync.",
    },
    pii: {
      console:
        "LOG_REDACT_PII=1 redacts emails, 16-digit-like sequences, and IPv4 last octet in selected console lines.",
      metricsFile:
        "metrics.ndjson may contain playerId; restrict filesystem ACL in production. Use npm run metrics-archive for cold copies.",
    },
    s3CompatibleExport: {
      description:
        "tools/audit_export_bundle.cjs writes manifest.json + metrics window + players index for upload to S3-compatible buckets.",
      multipartStyle: "One object per part; sha256 per part in manifest.parts[].sha256",
    },
    relatedPaths: {
      health: "/health",
      etagConcurrency: "/rehearsal/etag-concurrency",
      patchStrategy: "/rehearsal/patch-strategy",
      mockSync200: "/rehearsal/mock-sync-200",
      warningSrvValBridge: "/rehearsal/warning-srvval-bridge",
      idempotencyPersist: "/rehearsal/idempotency-persist",
      validatePatch: "POST /rehearsal/validate-patch",
      applyPatch: "POST /rehearsal/apply-patch (needs REHEARSAL_PATCH_WRITE=1)",
      auditStateStrict: "/rehearsal/audit-state-strict",
    },
  };
}

function escapePrometheusLabelValue(s) {
  return String(s).replace(/\\/g, "\\\\").replace(/\n/g, "\\n").replace(/"/g, '\\"');
}

/** OpenMetrics-style text for scrapers (Grafana Agent / Prometheus pushgateway patterns). */
function buildPrometheusAuditExport(rows, days) {
  const lines = [];
  const agg = aggregateAuditCategoriesFromRows(rows);
  lines.push("# HELP persist_sync_up Service reachable (1).");
  lines.push("# TYPE persist_sync_up gauge");
  lines.push("persist_sync_up 1");
  lines.push("# HELP persist_sync_audit_events_total Sum of auditSummary.total in selected window.");
  lines.push("# TYPE persist_sync_audit_events_total counter");
  lines.push(`persist_sync_audit_events_total ${agg.total}`);
  lines.push("# HELP persist_sync_metrics_rows_with_audit Rows in window that carried auditSummary.");
  lines.push("# TYPE persist_sync_metrics_rows_with_audit gauge");
  lines.push(`persist_sync_metrics_rows_with_audit ${agg.rowsWithSummary}`);
  lines.push("# HELP persist_sync_audit_category_events_total Per-category counts from aggregated auditSummary.");
  lines.push("# TYPE persist_sync_audit_category_events_total counter");
  const cats = Object.keys(agg.byCategory).sort();
  for (let i = 0; i < cats.length; i++) {
    const k = cats[i];
    const v = Number(agg.byCategory[k]) || 0;
    lines.push(`persist_sync_audit_category_events_total{category="${escapePrometheusLabelValue(k)}"} ${v}`);
  }
  lines.push("# HELP persist_sync_scrape_window_days Days parameter for this scrape.");
  lines.push("# TYPE persist_sync_scrape_window_days gauge");
  lines.push(`persist_sync_scrape_window_days ${days}`);
  const lat = aggregateLatencyMsPercentiles(rows);
  if (lat.sampleCount > 0) {
    lines.push("# HELP persist_sync_post_duration_ms Summary from metrics row durationMs (POST /sync handler).");
    lines.push("# TYPE persist_sync_post_duration_ms gauge");
    lines.push(`persist_sync_post_duration_ms{quantile="p50"} ${lat.p50}`);
    lines.push(`persist_sync_post_duration_ms{quantile="p90"} ${lat.p90}`);
    lines.push(`persist_sync_post_duration_ms{quantile="p99"} ${lat.p99}`);
    lines.push(`persist_sync_post_duration_ms{quantile="mean"} ${lat.mean}`);
    lines.push("# HELP persist_sync_post_duration_samples Count of rows with durationMs.");
    lines.push("# TYPE persist_sync_post_duration_samples gauge");
    lines.push(`persist_sync_post_duration_samples ${lat.sampleCount}`);
  }
  return lines.join("\n") + "\n";
}

function metricsToCsv(summary) {
  const lines = [];
  lines.push(
    [
      "scope",
      "bucket",
      "playerId",
      "total",
      "accepted",
      "rejected",
      "acceptRate",
      "warnLow",
      "warnHigh",
      "topCodes",
    ].join(",")
  );

  const playerId = summary.playerId || "all";
  const pushRow = (scope, bucket, req, warn) => {
    const topCodes = (warn.topCodes || []).map((x) => `${x.code}:${x.count}`).join("|");
    lines.push(
      [
        scope,
        bucket,
        csvCell(playerId),
        req.total,
        req.accepted,
        req.rejected,
        req.acceptRate,
        warn.low,
        warn.high,
        csvCell(topCodes),
      ].join(",")
    );
  };

  pushRow("overall", "all", summary.requests, summary.warnings);
  if (summary.groupBy === "day" && Array.isArray(summary.byDay)) {
    for (const b of summary.byDay)
      pushRow("day", b.key, b.requests, b.warnings);
  }
  if (summary.groupBy === "hour" && Array.isArray(summary.byHour)) {
    for (const b of summary.byHour)
      pushRow("hour", b.key, b.requests, b.warnings);
  }

  return lines.join("\n") + "\n";
}

function csvCell(v) {
  const s = String(v ?? "");
  if (/[",\n]/.test(s))
    return `"${s.replace(/"/g, '""')}"`;
  return s;
}

function readMetricsSummary(options) {
  const { top, playerId, groupBy, minSeverity } = options || {};
  const topNum = Number(top);
  const playerFilter = typeof playerId === "string" && playerId.trim() ? playerId.trim() : null;
  const groupMode = String(groupBy || "").trim().toLowerCase();
  const useGroupByDay = groupMode === "day";
  const useGroupByHour = groupMode === "hour";
  const useTop = Number.isFinite(topNum) && topNum > 0 ? Math.floor(topNum) : 10;
  const bucketTopN = Math.min(3, useTop);
  const severity = String(minSeverity || "").trim().toLowerCase();
  const filterLowOnly = severity === "low";
  const filterHighOnly = severity === "high";

  const rowResult = readMetricsRows(options || {});
  if (!rowResult.hasData) {
    return {
      ok: true,
      hasData: false,
      reason: rowResult.reason || "no_rows_in_window",
      file: metricsFilePath,
      window: options && options.days ? `last_${Number(options.days)}_days` : "all",
      playerId: playerFilter || "all",
      filters: {
        since: options && options.since ? options.since : null,
        until: options && options.until ? options.until : null,
        rejectedOnly: parseBool(options && options.rejectedOnly, false),
        minSeverity: filterLowOnly ? "low" : filterHighOnly ? "high" : "all",
      },
    };
  }

  let total = 0;
  let accepted = 0;
  let rejected = 0;
  let lowWarnings = 0;
  let highWarnings = 0;
  const codeCounts = {};
  const buckets = {};

  for (const row of rowResult.rows) {

    total++;
    if (row.accepted === true) accepted++;
    else if (row.accepted === false) rejected++;

    const ws = row.warningSummary || {};
    let low = Number(ws.low) || 0;
    let high = Number(ws.high) || 0;
    const rawByCode = row.warningsByCode || {};
    const byCode = {};
    for (const k of Object.keys(rawByCode)) {
      const c = Number(rawByCode[k]) || 0;
      if (c <= 0) continue;
      byCode[k] = c;
    }
    if (filterHighOnly) {
      low = 0;
      for (const k of Object.keys(byCode)) {
        if (!k.includes("high") && !k.includes("mismatch"))
          delete byCode[k];
      }
    } else if (filterLowOnly) {
      high = 0;
      for (const k of Object.keys(byCode)) {
        if (k.includes("high"))
          delete byCode[k];
      }
    }
    lowWarnings += low;
    highWarnings += high;

    for (const k of Object.keys(byCode)) {
      const c = Number(byCode[k]) || 0;
      if (c <= 0) continue;
      codeCounts[k] = (codeCounts[k] || 0) + c;
    }

    if (useGroupByDay || useGroupByHour) {
      const key = useGroupByDay ? dateKeyFromTs(row.ts) : hourKeyFromTs(row.ts);
      if (key) {
        if (!buckets[key]) {
          buckets[key] = { total: 0, accepted: 0, rejected: 0, low: 0, high: 0, codeCounts: {} };
        }
        const b = buckets[key];
        b.total += 1;
        if (row.accepted === true) b.accepted += 1;
        else if (row.accepted === false) b.rejected += 1;
        b.low += low;
        b.high += high;
        for (const k of Object.keys(byCode)) {
          const c = Number(byCode[k]) || 0;
          if (c <= 0) continue;
          b.codeCounts[k] = (b.codeCounts[k] || 0) + c;
        }
      }
    }
  }

  const topCodes = Object.entries(codeCounts)
    .sort((a, b) => b[1] - a[1])
    .slice(0, useTop)
    .map(([code, count]) => ({ code, count }));

  let grouped;
  if (useGroupByDay || useGroupByHour) {
    grouped = Object.keys(buckets)
      .sort()
      .map((key) => {
        const b = buckets[key];
        const bucketTopCodes = Object.entries(b.codeCounts || {})
          .sort((a, b2) => b2[1] - a[1])
          .slice(0, bucketTopN)
          .map(([code, count]) => ({ code, count }));
        return {
          key,
          requests: {
            total: b.total,
            accepted: b.accepted,
            rejected: b.rejected,
            acceptRate: Number(((b.accepted / b.total) * 100).toFixed(1)),
          },
          warnings: { low: b.low, high: b.high, topCodes: bucketTopCodes },
        };
      });
  }

  return {
    ok: true,
    hasData: true,
    file: metricsFilePath,
    window:
      options && Number.isFinite(Number(options.days)) && Number(options.days) > 0
        ? `last_${Math.floor(Number(options.days))}_days`
        : "all",
    playerId: playerFilter || "all",
    filters: {
      since: options && options.since ? options.since : null,
      until: options && options.until ? options.until : null,
      rejectedOnly: parseBool(options && options.rejectedOnly, false),
      minSeverity: filterLowOnly ? "low" : filterHighOnly ? "high" : "all",
    },
    requests: {
      total,
      accepted,
      rejected,
      acceptRate: Number(((accepted / total) * 100).toFixed(1)),
    },
    warnings: {
      low: lowWarnings,
      high: highWarnings,
      topCodes,
    },
    ...((useGroupByDay || useGroupByHour)
      ? {
          groupBy: useGroupByDay ? "day" : "hour",
          ...(useGroupByDay ? { byDay: grouped } : { byHour: grouped }),
        }
      : {}),
  };
}

function readTopPlayers(options) {
  const topN = parsePositiveInt(options && options.top, 10);
  const rowsResult = readMetricsRows(options || {});
  if (!rowsResult.hasData) return { ok: true, hasData: false, reason: rowsResult.reason, players: [] };
  const map = {};
  for (const r of rowsResult.rows) {
    const pid = typeof r.playerId === "string" && r.playerId.trim() ? r.playerId : "unknown";
    if (!map[pid]) map[pid] = { playerId: pid, requests: 0, accepted: 0, rejected: 0, low: 0, high: 0 };
    const item = map[pid];
    item.requests += 1;
    if (r.accepted === true) item.accepted += 1;
    else if (r.accepted === false) item.rejected += 1;
    item.low += Number(r.warningSummary && r.warningSummary.low) || 0;
    item.high += Number(r.warningSummary && r.warningSummary.high) || 0;
  }
  const list = Object.values(map)
    .map((x) => ({
      ...x,
      riskScore: x.high * 10 + x.low,
      acceptRate: Number(((x.accepted / x.requests) * 100).toFixed(1)),
    }))
    .sort((a, b) => b.riskScore - a.riskScore || b.requests - a.requests)
    .slice(0, topN);
  return { ok: true, hasData: true, players: list };
}

function readTopCodes(options) {
  const topN = parsePositiveInt(options && options.top, 10);
  const rowsResult = readMetricsRows(options || {});
  if (!rowsResult.hasData) return { ok: true, hasData: false, reason: rowsResult.reason, codes: [] };
  const counts = {};
  for (const r of rowsResult.rows) {
    const by = r.warningsByCode || {};
    for (const k of Object.keys(by)) {
      const c = Number(by[k]) || 0;
      if (c <= 0) continue;
      counts[k] = (counts[k] || 0) + c;
    }
  }
  const list = Object.entries(counts)
    .sort((a, b) => b[1] - a[1])
    .slice(0, topN)
    .map(([code, count]) => ({ code, count }));
  return { ok: true, hasData: true, codes: list };
}

function readRejectionReasons(options) {
  const rowsResult = readMetricsRows({ ...(options || {}), rejectedOnly: true });
  if (!rowsResult.hasData) return { ok: true, hasData: false, reason: rowsResult.reason, reasons: [] };
  const reasonCounts = {};
  const blockedCounts = {};
  for (const r of rowsResult.rows) {
    const reason = typeof r.error === "string" ? r.error : "unknown_rejection";
    reasonCounts[reason] = (reasonCounts[reason] || 0) + 1;
    if (Array.isArray(r.blockedBy)) {
      for (const c of r.blockedBy)
        blockedCounts[c] = (blockedCounts[c] || 0) + 1;
    }
  }
  return {
    ok: true,
    hasData: true,
    reasons: Object.entries(reasonCounts)
      .sort((a, b) => b[1] - a[1])
      .map(([reason, count]) => ({ reason, count })),
    blockedBy: Object.entries(blockedCounts)
      .sort((a, b) => b[1] - a[1])
      .map(([code, count]) => ({ code, count })),
  };
}

function readAnomalies(options) {
  const hours = parsePositiveInt(options && options.hours, 24);
  const compareHours = parsePositiveInt(options && options.compareHours, hours);
  const topN = parsePositiveInt(options && options.top, 5);
  const minCount = parsePositiveInt(options && options.minCount, 2);
  const nowMs = Date.now();
  const currentStartMs = nowMs - hours * 60 * 60 * 1000;
  const previousStartMs = currentStartMs - compareHours * 60 * 60 * 1000;
  const rowsResult = readMetricsRows({
    ...(options || {}),
    since: new Date(previousStartMs).toISOString(),
    until: new Date(nowMs).toISOString(),
  });
  if (!rowsResult.hasData) {
    return { ok: true, hasData: false, reason: rowsResult.reason, hours, compareHours, anomalies: [] };
  }

  const aggCurrent = { total: 0, accepted: 0, rejected: 0, codeCounts: {} };
  const aggPrev = { total: 0, accepted: 0, rejected: 0, codeCounts: {} };
  for (const row of rowsResult.rows) {
    const t = Date.parse(String(row.ts || ""));
    if (!Number.isFinite(t)) continue;
    const target = t >= currentStartMs ? aggCurrent : aggPrev;
    target.total += 1;
    if (row.accepted === true) target.accepted += 1;
    else if (row.accepted === false) target.rejected += 1;
    const by = row.warningsByCode || {};
    for (const k of Object.keys(by)) {
      const c = Number(by[k]) || 0;
      if (c <= 0) continue;
      target.codeCounts[k] = (target.codeCounts[k] || 0) + c;
    }
  }

  const allCodes = Array.from(
    new Set([...Object.keys(aggCurrent.codeCounts), ...Object.keys(aggPrev.codeCounts)])
  );
  const anomalies = [];
  for (const code of allCodes) {
    const current = aggCurrent.codeCounts[code] || 0;
    const previous = aggPrev.codeCounts[code] || 0;
    if (current < minCount) continue;
    const delta = current - previous;
    const ratio = previous > 0 ? Number((current / previous).toFixed(2)) : null;
    const isSpike = delta >= minCount || (ratio != null && ratio >= 2);
    if (!isSpike) continue;
    anomalies.push({
      code,
      current,
      previous,
      delta,
      ratio,
      trend: delta > 0 ? "up" : delta < 0 ? "down" : "flat",
    });
  }
  anomalies.sort((a, b) => b.delta - a.delta || b.current - a.current).slice(0, topN);

  const currentAcceptRate =
    aggCurrent.total > 0 ? Number(((aggCurrent.accepted / aggCurrent.total) * 100).toFixed(1)) : 0;
  const prevAcceptRate =
    aggPrev.total > 0 ? Number(((aggPrev.accepted / aggPrev.total) * 100).toFixed(1)) : 0;
  return {
    ok: true,
    hasData: aggCurrent.total > 0 || aggPrev.total > 0,
    hours,
    compareHours,
    window: {
      previous: {
        since: new Date(previousStartMs).toISOString(),
        until: new Date(currentStartMs).toISOString(),
      },
      current: {
        since: new Date(currentStartMs).toISOString(),
        until: new Date(nowMs).toISOString(),
      },
    },
    summary: {
      current: {
        requests: aggCurrent.total,
        accepted: aggCurrent.accepted,
        rejected: aggCurrent.rejected,
        acceptRate: currentAcceptRate,
      },
      previous: {
        requests: aggPrev.total,
        accepted: aggPrev.accepted,
        rejected: aggPrev.rejected,
        acceptRate: prevAcceptRate,
      },
      requestDelta: aggCurrent.total - aggPrev.total,
      rejectionDelta: aggCurrent.rejected - aggPrev.rejected,
      acceptRateDelta: Number((currentAcceptRate - prevAcceptRate).toFixed(1)),
    },
    anomalies,
  };
}

function readAlerts(options) {
  const minAcceptRate = Number(options && options.minAcceptRate);
  const maxHighWarnings = Number(options && options.maxHighWarnings);
  const maxRejected = Number(options && options.maxRejected);
  const minSpikeDelta = Number(options && options.minSpikeDelta);
  const days = parsePositiveInt(options && options.days, 7);
  const top = parsePositiveInt(options && options.top, 5);
  const hours = parsePositiveInt(options && options.hours, 24);
  const compareHours = parsePositiveInt(options && options.compareHours, hours);

  const report = readMetricsSummary({ ...(options || {}), days, top });
  if (!report.hasData) {
    return {
      ok: true,
      hasData: false,
      reason: report.reason || "no_rows_in_window",
      thresholds: {
        minAcceptRate: Number.isFinite(minAcceptRate) ? minAcceptRate : 95,
        maxHighWarnings: Number.isFinite(maxHighWarnings) ? maxHighWarnings : 0,
        maxRejected: Number.isFinite(maxRejected) ? maxRejected : 0,
        minSpikeDelta: Number.isFinite(minSpikeDelta) ? minSpikeDelta : 2,
      },
      alerts: [],
    };
  }

  const rejections = readRejectionReasons({ ...(options || {}), days });
  const anomalies = readAnomalies({ ...(options || {}), hours, compareHours, top, minCount: 1 });

  const thresholds = {
    minAcceptRate: Number.isFinite(minAcceptRate) ? minAcceptRate : 95,
    maxHighWarnings: Number.isFinite(maxHighWarnings) ? maxHighWarnings : 0,
    maxRejected: Number.isFinite(maxRejected) ? maxRejected : 0,
    minSpikeDelta: Number.isFinite(minSpikeDelta) ? minSpikeDelta : 2,
  };

  const alerts = [];
  if (report.requests.acceptRate < thresholds.minAcceptRate) {
    alerts.push({
      severity: "high",
      code: "accept_rate_below_threshold",
      message: `acceptRate ${report.requests.acceptRate}% < ${thresholds.minAcceptRate}%`,
      value: report.requests.acceptRate,
      threshold: thresholds.minAcceptRate,
    });
  }
  if (report.warnings.high > thresholds.maxHighWarnings) {
    alerts.push({
      severity: "high",
      code: "high_warnings_exceed_threshold",
      message: `high warnings ${report.warnings.high} > ${thresholds.maxHighWarnings}`,
      value: report.warnings.high,
      threshold: thresholds.maxHighWarnings,
    });
  }
  if (report.requests.rejected > thresholds.maxRejected) {
    alerts.push({
      severity: "medium",
      code: "rejected_requests_exceed_threshold",
      message: `rejected requests ${report.requests.rejected} > ${thresholds.maxRejected}`,
      value: report.requests.rejected,
      threshold: thresholds.maxRejected,
    });
  }
  if (anomalies && Array.isArray(anomalies.anomalies)) {
    for (const s of anomalies.anomalies) {
      if ((Number(s.delta) || 0) >= thresholds.minSpikeDelta) {
        alerts.push({
          severity: "medium",
          code: "warning_code_spike",
          message: `${s.code} spike delta=${s.delta}, current=${s.current}, prev=${s.previous}`,
          value: s.delta,
          threshold: thresholds.minSpikeDelta,
          spike: s,
        });
      }
    }
  }

  const topRejectionReason =
    rejections && Array.isArray(rejections.reasons) && rejections.reasons.length > 0
      ? rejections.reasons[0]
      : null;
  return {
    ok: true,
    hasData: true,
    window: report.window,
    thresholds,
    summary: {
      requests: report.requests,
      warnings: {
        low: report.warnings.low,
        high: report.warnings.high,
      },
      topRejectionReason,
    },
    alerts,
  };
}

function readPlayerAlerts(options) {
  const topN = parsePositiveInt(options && options.top, 10);
  const thresholds = {
    minAcceptRate: Number.isFinite(Number(options && options.minAcceptRate))
      ? Number(options.minAcceptRate)
      : 95,
    maxHighWarnings: Number.isFinite(Number(options && options.maxHighWarnings))
      ? Number(options.maxHighWarnings)
      : 0,
    maxRejected: Number.isFinite(Number(options && options.maxRejected))
      ? Number(options.maxRejected)
      : 0,
  };
  const players = readTopPlayers(options || {});
  if (!players.hasData) {
    return { ok: true, hasData: false, reason: players.reason || "no_rows_in_window", thresholds, players: [] };
  }
  const rows = [];
  for (const p of players.players || []) {
    const alerts = [];
    if (p.acceptRate < thresholds.minAcceptRate) {
      alerts.push(`acceptRate ${p.acceptRate}% < ${thresholds.minAcceptRate}%`);
    }
    if (p.high > thresholds.maxHighWarnings) {
      alerts.push(`high warnings ${p.high} > ${thresholds.maxHighWarnings}`);
    }
    if (p.rejected > thresholds.maxRejected) {
      alerts.push(`rejected ${p.rejected} > ${thresholds.maxRejected}`);
    }
    if (alerts.length === 0) continue;
    rows.push({
      playerId: p.playerId,
      riskScore: p.riskScore,
      requests: p.requests,
      rejected: p.rejected,
      acceptRate: p.acceptRate,
      high: p.high,
      low: p.low,
      alerts,
    });
  }
  rows.sort((a, b) => b.riskScore - a.riskScore || b.rejected - a.rejected);
  return {
    ok: true,
    hasData: true,
    thresholds,
    players: rows.slice(0, topN),
    totalFlagged: rows.length,
  };
}

function readDashboard(options) {
  const report = readMetricsSummary(options || {});
  if (!report.hasData) {
    return {
      ok: true,
      hasData: false,
      reason: report.reason || "no_rows_in_window",
      window: report.window || "all",
    };
  }
  const top = parsePositiveInt(options && options.top, 5);
  const hours = parsePositiveInt(options && options.hours, 24);
  const compareHours = parsePositiveInt(options && options.compareHours, hours);
  const players = readTopPlayers({ ...(options || {}), top });
  const codes = readTopCodes({ ...(options || {}), top });
  const rejections = readRejectionReasons(options || {});
  const anomalies = readAnomalies({ ...(options || {}), top, hours, compareHours, minCount: 1 });
  const alerts = readAlerts({ ...(options || {}), top, hours, compareHours });
  return {
    ok: true,
    hasData: true,
    window: report.window,
    filters: report.filters || {},
    summary: {
      requests: report.requests,
      warnings: report.warnings,
    },
    topPlayers: players.players || [],
    topCodes: codes.codes || [],
    rejectionReasons: rejections.reasons || [],
    anomalySpikes: anomalies.anomalies || [],
    alerts: alerts.alerts || [],
  };
}

function dashboardToCsvFlat(d) {
  const lines = [];
  lines.push("section,key,value");
  lines.push(["summary", "requests_total", d.summary.requests.total].join(","));
  lines.push(["summary", "accept_rate", d.summary.requests.acceptRate].join(","));
  lines.push(["summary", "warnings_low", d.summary.warnings.low].join(","));
  lines.push(["summary", "warnings_high", d.summary.warnings.high].join(","));
  for (const p of d.topPlayers || [])
    lines.push(["top_player", csvCell(p.playerId), csvCell(`risk=${p.riskScore}|req=${p.requests}|rej=${p.rejected}`)].join(","));
  for (const c of d.topCodes || []) lines.push(["top_code", csvCell(c.code), c.count].join(","));
  for (const r of d.rejectionReasons || []) lines.push(["rejection", csvCell(r.reason), r.count].join(","));
  for (const a of d.alerts || []) lines.push(["alert", csvCell(a.code), csvCell(a.message)].join(","));
  return lines.join("\n") + "\n";
}

function dashboardToCsvSectioned(d) {
  const lines = [];
  lines.push("section,key,value");
  lines.push("summary,requests_total," + d.summary.requests.total);
  lines.push("summary,accept_rate," + d.summary.requests.acceptRate);
  lines.push("summary,warnings_low," + d.summary.warnings.low);
  lines.push("summary,warnings_high," + d.summary.warnings.high);
  lines.push("");
  lines.push("section,player,risk_meta");
  for (const p of d.topPlayers || [])
    lines.push(["players", csvCell(p.playerId), csvCell(`risk=${p.riskScore}|req=${p.requests}|rej=${p.rejected}`)].join(","));
  lines.push("");
  lines.push("section,code,count");
  for (const c of d.topCodes || []) lines.push(["codes", csvCell(c.code), c.count].join(","));
  lines.push("");
  lines.push("section,reason,count");
  for (const r of d.rejectionReasons || []) lines.push(["rejections", csvCell(r.reason), r.count].join(","));
  lines.push("");
  lines.push("section,alert,message");
  for (const a of d.alerts || []) lines.push(["alerts", csvCell(a.code), csvCell(a.message)].join(","));
  return lines.join("\n") + "\n";
}

function dashboardToMarkdown(d, mode) {
  const lines = [];
  lines.push("# Risk Dashboard Snapshot");
  lines.push("");
  lines.push(`- Window: ${d.window || "unknown"}`);
  lines.push(`- Requests: ${d.summary.requests.total} (accepted ${d.summary.requests.accepted}, rejected ${d.summary.requests.rejected})`);
  lines.push(`- Accept Rate: ${d.summary.requests.acceptRate}%`);
  lines.push(`- Warnings: low=${d.summary.warnings.low}, high=${d.summary.warnings.high}`);
  lines.push("");
  lines.push("## Top Players");
  if (!d.topPlayers || d.topPlayers.length === 0) lines.push("- none");
  else for (const p of d.topPlayers) lines.push(`- ${p.playerId}: risk=${p.riskScore}, rejected=${p.rejected}`);
  lines.push("");
  lines.push("## Top Codes");
  if (!d.topCodes || d.topCodes.length === 0) lines.push("- none");
  else for (const c of d.topCodes) lines.push(`- ${c.code}: ${c.count}`);
  lines.push("");
  lines.push("## Alerts");
  if (!d.alerts || d.alerts.length === 0) lines.push("- none");
  else for (const a of d.alerts) lines.push(`- [${a.severity || "unknown"}] ${a.code}: ${a.message}`);
  if (String(mode || "full").toLowerCase() !== "compact") {
    lines.push("");
    lines.push("## Rejection Reasons");
    if (!d.rejectionReasons || d.rejectionReasons.length === 0) lines.push("- none");
    else for (const r of d.rejectionReasons) lines.push(`- ${r.reason}: ${r.count}`);
    lines.push("");
    lines.push("## Anomaly Spikes");
    if (!d.anomalySpikes || d.anomalySpikes.length === 0) lines.push("- none");
    else
      for (const s of d.anomalySpikes) {
        const ratio = s.ratio == null ? "new" : `${s.ratio}x`;
        lines.push(`- ${s.code}: delta=${s.delta}, current=${s.current}, previous=${s.previous}, ratio=${ratio}`);
      }
  }
  lines.push("");
  return lines.join("\n") + "\n";
}

ensureDataDir();

function normalizePath(pathname) {
  if (!pathname) return "/";
  const p = pathname.replace(/\/+$/, "");
  return p.length === 0 ? "/" : p;
}

http
  .createServer((req, res) => {
    const u = new URL(req.url || "/", `http://${req.headers.host || "127.0.0.1"}`);
    const pathname = normalizePath(u.pathname);

    if (req.method === "GET" && pathname === "/players") {
      ensureDataDir();
      const names = fs
        .readdirSync(dataDir)
        .filter((f) => f.endsWith(".json"))
        .map((f) => f.slice(0, -5));
      return sendJson(res, 200, { ok: true, players: names });
    }

    if (req.method === "GET" && pathname === "/state") {
      const raw = u.searchParams.get("playerId");
      const safe = safePlayerId(raw);
      const fp = filePathForPlayer(safe);
      if (!fs.existsSync(fp)) {
        return sendJson(res, 404, { ok: false, error: "not_found", playerId: raw, safeId: safe });
      }
      const text = fs.readFileSync(fp, "utf8");
      const etag = etagHeaderValue(sha256HexBuffer(Buffer.from(text, "utf8")));
      const h = {
        "Content-Type": "application/json; charset=utf-8",
        "Content-Length": Buffer.byteLength(text, "utf8"),
      };
      if (etag) h.ETag = etag;
      res.writeHead(200, h);
      return res.end(text);
    }

    if (req.method === "GET" && pathname === "/metrics/report") {
      const options = {
        days: u.searchParams.get("days"),
        top: u.searchParams.get("top"),
        playerId: u.searchParams.get("playerId"),
        groupBy: u.searchParams.get("groupBy"),
        since: u.searchParams.get("since"),
        until: u.searchParams.get("until"),
        rejectedOnly: u.searchParams.get("rejectedOnly"),
        minSeverity: u.searchParams.get("minSeverity"),
      };
      const summary = readMetricsSummary(options);
      const format = String(u.searchParams.get("format") || "").trim().toLowerCase();
      if (format === "csv" && summary && summary.hasData)
        return sendText(res, 200, metricsToCsv(summary), "text/csv; charset=utf-8");
      return sendJson(res, 200, summary);
    }

    if (req.method === "GET" && pathname === "/metrics/players") {
      const options = {
        days: u.searchParams.get("days"),
        top: u.searchParams.get("top"),
        playerId: u.searchParams.get("playerId"),
        since: u.searchParams.get("since"),
        until: u.searchParams.get("until"),
      };
      return sendJson(res, 200, readTopPlayers(options));
    }

    if (req.method === "GET" && pathname === "/metrics/codes") {
      const options = {
        days: u.searchParams.get("days"),
        top: u.searchParams.get("top"),
        playerId: u.searchParams.get("playerId"),
        since: u.searchParams.get("since"),
        until: u.searchParams.get("until"),
      };
      return sendJson(res, 200, readTopCodes(options));
    }

    if (req.method === "GET" && pathname === "/metrics/rejections") {
      const options = {
        days: u.searchParams.get("days"),
        playerId: u.searchParams.get("playerId"),
        since: u.searchParams.get("since"),
        until: u.searchParams.get("until"),
      };
      return sendJson(res, 200, readRejectionReasons(options));
    }

    if (req.method === "GET" && pathname === "/metrics/anomalies") {
      const options = {
        hours: u.searchParams.get("hours"),
        compareHours: u.searchParams.get("compareHours"),
        top: u.searchParams.get("top"),
        minCount: u.searchParams.get("minCount"),
        playerId: u.searchParams.get("playerId"),
      };
      return sendJson(res, 200, readAnomalies(options));
    }

    if (req.method === "GET" && pathname === "/metrics/alerts") {
      const options = {
        days: u.searchParams.get("days"),
        top: u.searchParams.get("top"),
        hours: u.searchParams.get("hours"),
        compareHours: u.searchParams.get("compareHours"),
        playerId: u.searchParams.get("playerId"),
        minAcceptRate: u.searchParams.get("minAcceptRate"),
        maxHighWarnings: u.searchParams.get("maxHighWarnings"),
        maxRejected: u.searchParams.get("maxRejected"),
        minSpikeDelta: u.searchParams.get("minSpikeDelta"),
      };
      const result = readAlerts(options);
      const format = String(u.searchParams.get("format") || "").trim().toLowerCase();
      if (format === "csv" && result && result.hasData) {
        const lines = ["severity,code,message,value,threshold"];
        for (const a of result.alerts || []) {
          lines.push(
            [a.severity, csvCell(a.code), csvCell(a.message), csvCell(a.value), csvCell(a.threshold)].join(",")
          );
        }
        return sendText(res, 200, lines.join("\n") + "\n", "text/csv; charset=utf-8");
      }
      return sendJson(res, 200, result);
    }

    if (req.method === "GET" && pathname === "/metrics/alerts/players") {
      const options = {
        days: u.searchParams.get("days"),
        top: u.searchParams.get("top"),
        since: u.searchParams.get("since"),
        until: u.searchParams.get("until"),
        minAcceptRate: u.searchParams.get("minAcceptRate"),
        maxHighWarnings: u.searchParams.get("maxHighWarnings"),
        maxRejected: u.searchParams.get("maxRejected"),
      };
      const result = readPlayerAlerts(options);
      const format = String(u.searchParams.get("format") || "").trim().toLowerCase();
      if (format === "csv" && result && result.hasData) {
        const lines = ["playerId,riskScore,requests,rejected,acceptRate,high,low,alerts"];
        for (const p of result.players || []) {
          lines.push(
            [
              csvCell(p.playerId),
              p.riskScore,
              p.requests,
              p.rejected,
              p.acceptRate,
              p.high,
              p.low,
              csvCell((p.alerts || []).join(" | ")),
            ].join(",")
          );
        }
        return sendText(res, 200, lines.join("\n") + "\n", "text/csv; charset=utf-8");
      }
      return sendJson(res, 200, result);
    }

    if (req.method === "GET" && pathname === "/metrics/dashboard") {
      const options = {
        days: u.searchParams.get("days"),
        top: u.searchParams.get("top"),
        playerId: u.searchParams.get("playerId"),
        since: u.searchParams.get("since"),
        until: u.searchParams.get("until"),
        hours: u.searchParams.get("hours"),
        compareHours: u.searchParams.get("compareHours"),
      };
      const result = readDashboard(options);
      const format = String(u.searchParams.get("format") || "").trim().toLowerCase();
      if (format === "csv" && result && result.hasData) {
        const csvMode = String(u.searchParams.get("csvMode") || "flat").trim().toLowerCase();
        const csv = csvMode === "sectioned" ? dashboardToCsvSectioned(result) : dashboardToCsvFlat(result);
        return sendText(res, 200, csv, "text/csv; charset=utf-8");
      }
      if ((format === "md" || format === "markdown") && result && result.hasData) {
        const mdMode = String(u.searchParams.get("mdMode") || "full").trim().toLowerCase();
        return sendText(res, 200, dashboardToMarkdown(result, mdMode), "text/markdown; charset=utf-8");
      }
      return sendJson(res, 200, result);
    }

    if (req.method === "GET" && pathname === "/health") {
      const hasMetrics = fs.existsSync(metricsFilePath);
      let metricsSizeBytes = 0;
      if (hasMetrics) {
        try {
          metricsSizeBytes = fs.statSync(metricsFilePath).size;
        } catch (_) {
          metricsSizeBytes = 0;
        }
      }
      return sendJson(res, 200, {
        ok: true,
        service: "persist_sync",
        ts: new Date().toISOString(),
        port,
        dataDir,
        rejectHighWarnings,
        rejectAnySrvValAudit,
        srvValCategoryRejectList: getSrvValCategoryRejectList(),
        srvValRejectThresholdJson: !!parseJsonEnvObject("SRVVAL_REJECT_THRESHOLD_JSON"),
        srvValAlertThresholdJson: !!parseJsonEnvObject("SRVVAL_ALERT_THRESHOLD_JSON"),
        metricsPrometheusPath: "/metrics/prometheus",
        metricsSyncSummaryPath: "/metrics/sync-summary",
        rehearsalMockSyncPath: "/rehearsal/mock-sync-200",
        rehearsalPatchStrategyPath: "/rehearsal/patch-strategy",
        rehearsalComplianceBundlePath: "/rehearsal/compliance-bundle",
        rehearsalWarningSrvValBridgePath: "/rehearsal/warning-srvval-bridge",
        rehearsalIdempotencyPersistPath: "/rehearsal/idempotency-persist",
        rehearsalValidatePatchPath: "/rehearsal/validate-patch",
        rehearsalAuditStateStrictPath: "/rehearsal/audit-state-strict",
        rehearsalApplyPatchPath: "/rehearsal/apply-patch",
        rehearsalEtagConcurrencyPath: "/rehearsal/etag-concurrency",
        syncEtagIfMatchEnabled: !syncEtagCheckDisabled(),
        rehearsalPatchWrite,
        syncAuditStateStrict,
        syncWarningSrvValBridge,
        warningCodeToSrvValMapConfigured: !!warningCodeToSrvValMapResolved,
        auditExportBucketPrefix,
        logRedactPii,
        complianceMetricsRetentionHintDays,
        compliancePlayerStateRetentionHintDays,
        syncIssuedAtMaxSkewSec,
        maintenanceMode,
        syncRateLimitPerMinute,
        syncIdempotencyTtlMs,
        syncIdempotencyPersist,
        idempotencyPersistPath,
        syncHmacRequired: !!syncHmacSecret,
        syncRequireStagingHeader,
        metrics: {
          path: metricsFilePath,
          exists: hasMetrics,
          sizeBytes: metricsSizeBytes,
        },
      });
    }

    if (req.method === "GET" && pathname === "/metrics/recent") {
      const limit = parsePositiveInt(u.searchParams.get("limit"), 20);
      if (!fs.existsSync(metricsFilePath))
        return sendJson(res, 200, { ok: true, hasData: false, reason: "metrics_file_not_found", items: [] });
      const lines = fs.readFileSync(metricsFilePath, "utf8").split(/\r?\n/).filter(Boolean);
      const items = [];
      for (let i = lines.length - 1; i >= 0 && items.length < limit; i--) {
        try {
          items.push(JSON.parse(lines[i]));
        } catch (_) {
          // ignore invalid line
        }
      }
      return sendJson(res, 200, { ok: true, hasData: items.length > 0, items });
    }

    if (req.method === "GET" && pathname === "/metrics/audit-categories") {
      const days = parsePositiveInt(u.searchParams.get("days"), 7);
      const playerId = u.searchParams.get("playerId");
      const { rows } = readMetricsRows({ days, playerId: playerId && playerId.trim() ? playerId.trim() : null });
      const agg = aggregateAuditCategoriesFromRows(rows);
      return sendJson(res, 200, {
        ok: true,
        days,
        playerId: playerId && playerId.trim() ? playerId.trim() : null,
        ...agg,
      });
    }

    if (req.method === "GET" && pathname === "/metrics/prometheus") {
      const days = parsePositiveInt(u.searchParams.get("days"), 7);
      const playerId = u.searchParams.get("playerId");
      const { rows } = readMetricsRows({ days, playerId: playerId && playerId.trim() ? playerId.trim() : null });
      const body = buildPrometheusAuditExport(rows, days);
      return sendText(res, 200, body, "text/plain; version=0.0.4; charset=utf-8");
    }

    if (req.method === "GET" && pathname === "/metrics/sync-summary") {
      const days = parsePositiveInt(u.searchParams.get("days"), 7);
      const playerId = u.searchParams.get("playerId");
      const { rows, hasData, reason } = readMetricsRows({
        days,
        playerId: playerId && playerId.trim() ? playerId.trim() : null,
      });
      const slo = aggregateSyncSloFromRows(rows);
      const latencyMs = aggregateLatencyMsPercentiles(rows);
      return sendJson(res, 200, {
        ok: true,
        days,
        playerId: playerId && playerId.trim() ? playerId.trim() : null,
        hasData,
        reason,
        slo,
        latencyMs,
      });
    }

    if (req.method === "GET" && pathname === "/rehearsal/patch-strategy") {
      return sendJson(res, 200, {
        ...patchStrategyDoc,
        ts: new Date().toISOString(),
      });
    }

    if (req.method === "GET" && pathname === "/rehearsal/etag-concurrency") {
      return sendJson(res, 200, {
        ok: true,
        doc: "etag_concurrency_d16_v1",
        ts: new Date().toISOString(),
        env: "SYNC_ETAG_DISABLED=1 disables If-Match validation (ETag response headers may still be sent).",
        syncEtagIfMatchEnabled: !syncEtagCheckDisabled(),
        algorithm: "SHA-256 hex of UTF-8 file bytes on disk (same bytes as GET /state body); ETag header value is quoted hex.",
        headers: {
          response: "ETag: \"<sha256hex>\"",
          request: 'If-Match: "<sha256hex>" | W/"<sha256hex>" | *',
        },
        endpoints: [
          "GET /state?playerId= → 200 + ETag",
          "POST /sync → 200 + ETag after write; 412 if If-Match does not match current save (or save missing but If-Match set)",
          "POST /rehearsal/apply-patch → same 412 + ETag rules when file exists",
        ],
        idempotencyReplay:
          "POST /sync idempotent 200 replays include ETag = hash of JSON-serialized response body (may differ from on-disk canonicalization).",
      });
    }

    if (req.method === "GET" && pathname === "/rehearsal/compliance-bundle") {
      return sendJson(res, 200, buildComplianceBundlePayload());
    }

    if (req.method === "GET" && pathname === "/rehearsal/warning-srvval-bridge") {
      return sendJson(res, 200, {
        ok: true,
        doc: "warning_srvval_bridge_v1",
        ts: new Date().toISOString(),
        intent:
          "Map validation warning codes (audit_validate.cjs) to SrvVal_* hint counts under auditSummary.srvValFromWarnings; observation only.",
        env: {
          SYNC_WARNING_SRVVAL_BRIDGE: "1 enables built-in default map when WARNING_CODE_TO_SRVVAL_JSON is unset",
          WARNING_CODE_TO_SRVVAL_JSON: "JSON object { warningCode: 'SrvVal_*' }; overrides defaults entirely when non-empty",
        },
        defaultMap: { ...defaultWarningCodeToSrvVal },
        activeMap: warningCodeToSrvValMapResolved,
        knownWarningCodes: [
          "unexpected_schema_version",
          "player_id_mismatch",
          "state_version_vs_schema_mismatch",
          "gold_tail_mismatch",
          "inventory_hp_tail_mismatch",
          "inventory_mp_tail_mismatch",
          "audit_contains_srvval",
        ],
      });
    }

    if (req.method === "GET" && pathname === "/rehearsal/audit-state-strict") {
      return sendJson(res, 200, {
        ok: true,
        doc: "audit_state_strict_v1",
        ts: new Date().toISOString(),
        env: "SYNC_AUDIT_STATE_STRICT=1",
        active: syncAuditStateStrict,
        promotesToErrors: [
          "unexpected_schema_version (≠1)",
          "player_id_mismatch_body_vs_state",
          "state_version_vs_schema_mismatch",
          "gold_tail_mismatch_vs_state",
          "inventory_hp_tail_mismatch_vs_state",
          "inventory_mp_tail_mismatch_vs_state",
        ],
        note: "Default OFF: tail mismatches are warnings (ring buffer). Strict ON: POST /sync returns 400 when replay tail ≠ state.",
      });
    }

    if (req.method === "GET" && pathname === "/rehearsal/idempotency-persist") {
      return sendJson(res, 200, {
        ok: true,
        doc: "idempotency_persist_v1",
        ts: new Date().toISOString(),
        requires: "SYNC_IDEMPOTENCY_TTL_MS > 0 and SYNC_IDEMPOTENCY_PERSIST=1",
        file: path.basename(idempotencyPersistPath),
        path: idempotencyPersistPath,
        note:
          "Persists Idempotency-Key 200 replay cache to disk (single process). Multi-instance production: use Redis or shared store.",
      });
    }

    if (req.method === "GET" && pathname === "/rehearsal/mock-sync-200") {
      return sendJson(res, 200, {
        ...mockSync200Example,
        rehearsal: true,
        ts: new Date().toISOString(),
      });
    }

    if (req.method === "GET" && pathname === "/rehearsal/apply-patch") {
      return sendJson(res, 200, {
        ok: true,
        doc: "apply_patch_rehearsal_v1",
        ts: new Date().toISOString(),
        requiresEnv: "REHEARSAL_PATCH_WRITE=1",
        method: "POST",
        body: { playerId: "string (must match saved file)", ops: "same whitelist as /rehearsal/validate-patch" },
        rehearsalPatchWrite,
        d16Etag:
          "Optional If-Match: use ETag from GET /state or prior write; 412 precondition_failed on mismatch (unless SYNC_ETAG_DISABLED=1).",
        note: "Loads data/<playerId>.json, applies ops to state only, re-validates like POST /sync, then writes.",
      });
    }

    if (req.method === "POST" && pathname === "/rehearsal/validate-patch") {
      const chunks = [];
      req.on("data", (c) => chunks.push(c));
      req.on("end", () => {
        const text = Buffer.concat(chunks).toString("utf8");
        let body;
        try {
          body = text ? JSON.parse(text) : {};
        } catch (_) {
          return sendJson(res, 400, {
            ok: false,
            error: "invalid_json",
            rehearsal: true,
            ts: new Date().toISOString(),
          });
        }
        const r = validatePatchRehearsalRequest(body);
        return sendJson(res, r.ok ? 200 : 400, {
          ...r,
          rehearsal: true,
          ts: new Date().toISOString(),
        });
      });
      return;
    }

    if (req.method === "POST" && pathname === "/rehearsal/apply-patch") {
      const chunks = [];
      req.on("data", (c) => chunks.push(c));
      req.on("end", () => {
        const t0 = Date.now();
        const hdr = () => ({ "X-Sync-Duration-Ms": String(msSince(t0)) });
        if (!rehearsalPatchWrite) {
          return sendJson(
            res,
            403,
            {
              ok: false,
              error: "rehearsal_patch_write_disabled",
              hint: "Set REHEARSAL_PATCH_WRITE=1",
              rehearsal: true,
              ts: new Date().toISOString(),
            },
            hdr()
          );
        }
        const text = Buffer.concat(chunks).toString("utf8");
        console.log(
          new Date().toISOString(),
          "POST /rehearsal/apply-patch",
          "bodyBytes",
          Buffer.byteLength(text, "utf8")
        );
        if (maintenanceMode) {
          appendMetrics({
            ts: new Date().toISOString(),
            playerId: "_",
            accepted: false,
            error: "maintenance_mode",
            rehearsalApplyPatch: true,
            httpStatus: 503,
            durationMs: msSince(t0),
          });
          return sendJson(res, 503, { ok: false, error: "maintenance_mode", rehearsal: true }, hdr());
        }
        const ip = clientIpFromReq(req);
        const rl = checkSyncRateLimit(ip);
        if (!rl.ok) {
          appendMetrics({
            ts: new Date().toISOString(),
            playerId: "_",
            accepted: false,
            error: "rate_limited",
            rehearsalApplyPatch: true,
            httpStatus: 429,
            durationMs: msSince(t0),
          });
          res.writeHead(429, {
            "Content-Type": "application/json; charset=utf-8",
            "Retry-After": String(rl.retryAfterSec || 60),
            "X-Sync-Duration-Ms": String(msSince(t0)),
          });
          return res.end(
            JSON.stringify({
              ok: false,
              error: "rate_limited",
              retryAfterSec: rl.retryAfterSec,
              rehearsal: true,
            })
          );
        }
        if (syncRequireStagingHeader) {
          const st = String(req.headers["x-sync-staging"] || "").trim();
          if (st !== "1") {
            appendMetrics({
              ts: new Date().toISOString(),
              playerId: "_",
              accepted: false,
              error: "staging_required",
              rehearsalApplyPatch: true,
              httpStatus: 403,
              durationMs: msSince(t0),
            });
            return sendJson(res, 403, { ok: false, error: "staging_required", rehearsal: true }, hdr());
          }
        }
        if (!verifySyncHmacBody(text, req)) {
          appendMetrics({
            ts: new Date().toISOString(),
            playerId: "_",
            accepted: false,
            error: "invalid_sync_signature",
            rehearsalApplyPatch: true,
            httpStatus: 401,
            durationMs: msSince(t0),
          });
          return sendJson(res, 401, { ok: false, error: "invalid_sync_signature", rehearsal: true }, hdr());
        }

        let reqBody;
        try {
          reqBody = text ? JSON.parse(text) : {};
        } catch (_) {
          return sendJson(res, 400, { ok: false, error: "invalid_json", rehearsal: true }, hdr());
        }
        if (!reqBody.playerId || typeof reqBody.playerId !== "string") {
          return sendJson(res, 400, { ok: false, error: "missing_playerId", rehearsal: true }, hdr());
        }
        if (!Array.isArray(reqBody.ops)) {
          return sendJson(res, 400, { ok: false, error: "missing_or_bad_ops", rehearsal: true }, hdr());
        }

        const safe = safePlayerId(reqBody.playerId);
        ensureDataDir();
        const fp = filePathForPlayer(safe);
        if (!fs.existsSync(fp)) {
          appendMetrics({
            ts: new Date().toISOString(),
            playerId: reqBody.playerId,
            accepted: false,
            error: "save_not_found",
            rehearsalApplyPatch: true,
            httpStatus: 404,
            durationMs: msSince(t0),
          });
          return sendJson(res, 404, { ok: false, error: "save_not_found", rehearsal: true }, hdr());
        }

        const applyPatchEtagFail = ifMatchPreconditionFailed(req, fp);
        if (applyPatchEtagFail) {
          appendMetrics({
            ts: new Date().toISOString(),
            playerId: reqBody.playerId,
            accepted: false,
            error: "precondition_failed",
            detail: applyPatchEtagFail.reason,
            rehearsalApplyPatch: true,
            httpStatus: 412,
            durationMs: msSince(t0),
          });
          const curHex =
            applyPatchEtagFail.currentEtag != null ? applyPatchEtagFail.currentEtag : etagForFilePath(fp);
          const h412 = { ...hdr() };
          if (curHex) h412.ETag = etagHeaderValue(curHex);
          return sendJson(
            res,
            412,
            {
              ok: false,
              error: "precondition_failed",
              detail: applyPatchEtagFail.reason,
              rehearsal: true,
              ...(applyPatchEtagFail.currentEtag
                ? { currentEtag: applyPatchEtagFail.currentEtag }
                : { ifMatchHint: "use ETag from GET /state or prior successful write" }),
            },
            h412
          );
        }

        let fullBody;
        try {
          fullBody = JSON.parse(fs.readFileSync(fp, "utf8"));
        } catch (e) {
          return sendJson(
            res,
            400,
            {
              ok: false,
              error: "saved_file_invalid_json",
              rehearsal: true,
              detail: String(e && e.message ? e.message : e),
            },
            hdr()
          );
        }

        const pr = validatePatchRehearsalRequest({
          baseState: fullBody.state,
          ops: reqBody.ops,
        });
        if (!pr.ok) {
          appendMetrics({
            ts: new Date().toISOString(),
            playerId: reqBody.playerId,
            accepted: false,
            error: "patch_rehearsal_rejected",
            rehearsalApplyPatch: true,
            httpStatus: 400,
            durationMs: msSince(t0),
          });
          return sendJson(res, 400, { ...pr, rehearsal: true, ts: new Date().toISOString() }, hdr());
        }
        fullBody.state = pr.mergedStatePreview;

        if (syncIssuedAtMaxSkewSec > 0 && fullBody.issuedAtUtc != null && String(fullBody.issuedAtUtc).trim() !== "") {
          const issuedMs = Date.parse(String(fullBody.issuedAtUtc));
          if (Number.isFinite(issuedMs)) {
            const skewSec = Math.abs(Date.now() - issuedMs) / 1000;
            if (skewSec > syncIssuedAtMaxSkewSec) {
              appendMetrics({
                ts: new Date().toISOString(),
                playerId: reqBody.playerId,
                accepted: false,
                error: "issued_at_skew",
                rehearsalApplyPatch: true,
                httpStatus: 400,
                durationMs: msSince(t0),
              });
              return sendJson(
                res,
                400,
                {
                  ok: false,
                  error: "issued_at_skew",
                  detail: { skewSec: Math.round(skewSec * 1000) / 1000, maxSec: syncIssuedAtMaxSkewSec },
                  rehearsal: true,
                },
                hdr()
              );
            }
          }
        }

        const schemaReport = validateSyncPayloadSchemas(fullBody);
        if (!schemaReport.ok) {
          appendMetrics({
            ts: new Date().toISOString(),
            playerId: fullBody.playerId,
            accepted: false,
            error: "schema_validation_failed",
            rehearsalApplyPatch: true,
            httpStatus: 400,
            durationMs: msSince(t0),
          });
          return sendJson(
            res,
            400,
            {
              ok: false,
              error: "schema_validation_failed",
              errors: schemaReport.errors,
              rehearsal: true,
            },
            hdr()
          );
        }

        const report = validateClientSyncPayload(fullBody);
        if (!report.ok) {
          appendMetrics({
            ts: new Date().toISOString(),
            playerId: fullBody.playerId,
            accepted: false,
            error: "validation_failed",
            rehearsalApplyPatch: true,
            httpStatus: 400,
            durationMs: msSince(t0),
          });
          return sendJson(
            res,
            400,
            {
              ok: false,
              error: "validation_failed",
              validation: report,
              rehearsal: true,
            },
            hdr()
          );
        }

        const auditLen = Array.isArray(fullBody.audit) ? fullBody.audit.length : 0;
        const lowCount = report.warnings.filter((w) => w && w.severity === "low").length;
        const highCount = report.warnings.filter((w) => w && w.severity === "high").length;
        const warningsByCode = buildWarningCodeMap(report.warnings);
        const auditSummary = attachSrvValFromWarningsToAuditSummary(
          summarizeAuditByCategory(fullBody.audit),
          warningsByCode
        );
        const blockedBy = report.warnings
          .filter((w) => w && w.severity === "high")
          .map((w) => w.code || "unknown_high_warning");
        const metricsBase = {
          ts: new Date().toISOString(),
          playerId: fullBody.playerId,
          schemaVersion: fullBody.schemaVersion,
          mode: fullBody.mode,
          auditCount: auditLen,
          auditSummary,
          warningSummary: { low: lowCount, high: highCount },
          warningsByCode,
          rejectHighWarnings,
          rejectAnySrvValAudit,
          srvValCategoryRejectList: getSrvValCategoryRejectList(),
          rehearsalApplyPatch: true,
        };

        if (rejectHighWarnings && highCount > 0) {
          appendMetrics({
            ...metricsBase,
            accepted: false,
            error: "high_warning_block",
            blockedBy,
            httpStatus: 400,
            durationMs: msSince(t0),
          });
          return sendJson(
            res,
            400,
            {
              ok: false,
              error: "high_warning_block",
              blockedBy,
              auditSummary,
              rehearsal: true,
              validation: {
                ok: false,
                warningSummary: { low: lowCount, high: highCount },
                warningsByCode,
                warnings: report.warnings,
              },
            },
            hdr()
          );
        }

        const srvBlock = evaluateSrvValSyncReject(fullBody, auditSummary);
        if (srvBlock) {
          appendMetrics({
            ...metricsBase,
            accepted: false,
            error: srvBlock.error,
            srvValRejectDetail: srvBlock.detail,
            httpStatus: 400,
            durationMs: msSince(t0),
          });
          return sendJson(
            res,
            400,
            {
              ok: false,
              error: srvBlock.error,
              detail: srvBlock.detail,
              auditSummary,
              rehearsal: true,
              validation: {
                ok: false,
                warningSummary: { low: lowCount, high: highCount },
                warningsByCode,
                warnings: report.warnings,
              },
            },
            hdr()
          );
        }

        const pretty = JSON.stringify(fullBody, null, 2) + "\n";
        fs.writeFileSync(fp, pretty, "utf8");
        console.log(new Date().toISOString(), "POST /rehearsal/apply-patch ok", logPathForSyncSaved(fp, safe));

        appendMetrics({
          ...metricsBase,
          accepted: true,
          saved: safe + ".json",
          bytes: Buffer.byteLength(pretty, "utf8"),
          httpStatus: 200,
          durationMs: msSince(t0),
        });

        const applyWrittenEtag = etagHeaderValue(sha256HexBuffer(Buffer.from(pretty, "utf8")));
        return sendJson(
          res,
          200,
          {
            ok: true,
            rehearsal: true,
            rehearsalPatchApplied: true,
            saved: safe + ".json",
            playerId: fullBody.playerId,
            opCount: pr.opCount,
            mergedStateHashPreview: pr.mergedStateHashPreview,
            patchWarnings: pr.warnings || [],
            auditSummary,
            validation: {
              ok: true,
              warningSummary: { low: lowCount, high: highCount },
              warningsByCode,
              warnings: report.warnings,
            },
            ts: new Date().toISOString(),
          },
          { ...hdr(), ...(applyWrittenEtag ? { ETag: applyWrittenEtag } : {}) }
        );
      });
      return;
    }

    if (req.method === "POST" && pathname === "/sync") {
      const chunks = [];
      req.on("data", (c) => chunks.push(c));
      req.on("end", () => {
        const t0 = Date.now();
        const hdr = () => ({ "X-Sync-Duration-Ms": String(msSince(t0)) });
        const text = Buffer.concat(chunks).toString("utf8");
        console.log(new Date().toISOString(), "POST /sync", "bodyBytes", Buffer.byteLength(text, "utf8"));
        if (maintenanceMode) {
          console.warn(new Date().toISOString(), "POST /sync rejected: maintenance_mode");
          appendMetrics({
            ts: new Date().toISOString(),
            playerId: "_",
            accepted: false,
            error: "maintenance_mode",
            httpStatus: 503,
            durationMs: msSince(t0),
          });
          return sendJson(res, 503, { ok: false, error: "maintenance_mode" }, hdr());
        }
        const idemHeaderRaw = req.headers["idempotency-key"];
        const idemComp = idempotencyCompositeKey(idemHeaderRaw, text);
        const idemHit = idempotencyCacheGet(idemComp);
        if (idemHit) {
          console.log(new Date().toISOString(), "POST /sync idempotent_cache_hit", idemComp.slice(0, 12));
          appendMetrics({
            ts: new Date().toISOString(),
            playerId: tryPlayerIdFromJsonText(text),
            accepted: true,
            idempotentReplay: true,
            httpStatus: idemHit.status,
            durationMs: msSince(t0),
          });
          const idemBody = JSON.stringify(idemHit.bodyObj);
          const idemEtag = etagHeaderValue(sha256HexBuffer(Buffer.from(idemBody, "utf8")));
          return sendJson(res, idemHit.status, idemHit.bodyObj, {
            ...hdr(),
            ...(idemEtag ? { ETag: idemEtag } : {}),
          });
        }
        const ip = clientIpFromReq(req);
        const rl = checkSyncRateLimit(ip);
        if (!rl.ok) {
          console.warn(new Date().toISOString(), "POST /sync rejected: rate_limited", redactPiiForLog(ip));
          appendMetrics({
            ts: new Date().toISOString(),
            playerId: tryPlayerIdFromJsonText(text),
            accepted: false,
            error: "rate_limited",
            httpStatus: 429,
            durationMs: msSince(t0),
          });
          res.writeHead(429, {
            "Content-Type": "application/json; charset=utf-8",
            "Retry-After": String(rl.retryAfterSec || 60),
            "X-Sync-Duration-Ms": String(msSince(t0)),
          });
          return res.end(
            JSON.stringify({ ok: false, error: "rate_limited", retryAfterSec: rl.retryAfterSec })
          );
        }
        if (syncRequireStagingHeader) {
          const st = String(req.headers["x-sync-staging"] || "").trim();
          if (st !== "1") {
            console.warn(new Date().toISOString(), "POST /sync rejected: staging_required");
            appendMetrics({
              ts: new Date().toISOString(),
              playerId: tryPlayerIdFromJsonText(text),
              accepted: false,
              error: "staging_required",
              httpStatus: 403,
              durationMs: msSince(t0),
            });
            return sendJson(res, 403, { ok: false, error: "staging_required" }, hdr());
          }
        }
        if (!verifySyncHmacBody(text, req)) {
          console.warn(new Date().toISOString(), "POST /sync rejected: invalid_sync_signature");
          appendMetrics({
            ts: new Date().toISOString(),
            playerId: tryPlayerIdFromJsonText(text),
            accepted: false,
            error: "invalid_sync_signature",
            httpStatus: 401,
            durationMs: msSince(t0),
          });
          return sendJson(res, 401, { ok: false, error: "invalid_sync_signature" }, hdr());
        }
        let body;
        try {
          body = JSON.parse(text);
        } catch (e) {
          console.warn(
            new Date().toISOString(),
            "POST /sync rejected: invalid_json",
            redactPiiForLog(String(e && e.message ? e.message : e))
          );
          appendMetrics({
            ts: new Date().toISOString(),
            playerId: "_",
            accepted: false,
            error: "invalid_json",
            httpStatus: 400,
            durationMs: msSince(t0),
          });
          return sendJson(res, 400, { ok: false, error: "invalid_json", detail: String(e.message) }, hdr());
        }
        if (!body.playerId || typeof body.playerId !== "string") {
          console.warn(new Date().toISOString(), "POST /sync rejected: missing_playerId");
          appendMetrics({
            ts: new Date().toISOString(),
            playerId: "_",
            accepted: false,
            error: "missing_playerId",
            httpStatus: 400,
            durationMs: msSince(t0),
          });
          return sendJson(res, 400, { ok: false, error: "missing_playerId" }, hdr());
        }

        ensureDataDir();
        const fpIfMatch = filePathForPlayer(safePlayerId(body.playerId));
        const etagFail = ifMatchPreconditionFailed(req, fpIfMatch);
        if (etagFail) {
          appendMetrics({
            ts: new Date().toISOString(),
            playerId: body.playerId,
            accepted: false,
            error: "precondition_failed",
            detail: etagFail.reason,
            httpStatus: 412,
            durationMs: msSince(t0),
          });
          const curHex = etagFail.currentEtag != null ? etagFail.currentEtag : etagForFilePath(fpIfMatch);
          const h412 = { ...hdr() };
          if (curHex) h412.ETag = etagHeaderValue(curHex);
          return sendJson(
            res,
            412,
            {
              ok: false,
              error: "precondition_failed",
              detail: etagFail.reason,
              ...(etagFail.currentEtag
                ? { currentEtag: etagFail.currentEtag }
                : etagFail.want != null
                  ? { ifMatchExpectedFirstWrite: "omit If-Match or use *" }
                  : {}),
            },
            h412
          );
        }

        if (syncIssuedAtMaxSkewSec > 0) {
          const rawIssued = body.issuedAtUtc;
          if (rawIssued == null || String(rawIssued).trim() === "") {
            appendMetrics({
              ts: new Date().toISOString(),
              playerId: body.playerId,
              accepted: false,
              error: "missing_issuedAtUtc",
              httpStatus: 400,
              durationMs: msSince(t0),
            });
            return sendJson(res, 400, { ok: false, error: "missing_issuedAtUtc" }, hdr());
          }
          const issuedMs = Date.parse(String(rawIssued));
          if (!Number.isFinite(issuedMs)) {
            appendMetrics({
              ts: new Date().toISOString(),
              playerId: body.playerId,
              accepted: false,
              error: "invalid_issuedAtUtc",
              httpStatus: 400,
              durationMs: msSince(t0),
            });
            return sendJson(res, 400, { ok: false, error: "invalid_issuedAtUtc" }, hdr());
          }
          const skewSec = Math.abs(Date.now() - issuedMs) / 1000;
          if (skewSec > syncIssuedAtMaxSkewSec) {
            appendMetrics({
              ts: new Date().toISOString(),
              playerId: body.playerId,
              accepted: false,
              error: "issued_at_skew",
              httpStatus: 400,
              durationMs: msSince(t0),
            });
            return sendJson(
              res,
              400,
              {
                ok: false,
                error: "issued_at_skew",
                detail: {
                  skewSec: Math.round(skewSec * 1000) / 1000,
                  maxSec: syncIssuedAtMaxSkewSec,
                },
              },
              hdr()
            );
          }
        }

        const schemaReport = validateSyncPayloadSchemas(body);
        if (!schemaReport.ok) {
          console.warn(
            new Date().toISOString(),
            "POST /sync rejected: schema_validation_failed",
            redactPiiForLog(JSON.stringify(schemaReport.errors))
          );
          appendMetrics({
            ts: new Date().toISOString(),
            playerId: body.playerId,
            accepted: false,
            error: "schema_validation_failed",
            httpStatus: 400,
            durationMs: msSince(t0),
          });
          return sendJson(
            res,
            400,
            {
              ok: false,
              error: "schema_validation_failed",
              errors: schemaReport.errors,
            },
            hdr()
          );
        }
        if (schemaReport.skipped && !schemaSkipNoticeLogged) {
          schemaSkipNoticeLogged = true;
          console.log(
            new Date().toISOString(),
            "JSON Schema validation skipped (install deps: cd EpochOfDawn/server && npm install)"
          );
        }

        const report = validateClientSyncPayload(body);
        if (!report.ok) {
          console.warn(
            new Date().toISOString(),
            "POST /sync rejected: validation_failed",
            redactPiiForLog(JSON.stringify(report.errors))
          );
          appendMetrics({
            ts: new Date().toISOString(),
            playerId: body.playerId,
            accepted: false,
            error: "validation_failed",
            httpStatus: 400,
            durationMs: msSince(t0),
          });
          return sendJson(
            res,
            400,
            {
              ok: false,
              error: "validation_failed",
              validation: report,
            },
            hdr()
          );
        }

        const safe = safePlayerId(body.playerId);
        const fp = filePathForPlayer(safe);
        const pretty = JSON.stringify(body, null, 2) + "\n";
        const auditLen = Array.isArray(body.audit) ? body.audit.length : 0;
        const lowCount = report.warnings.filter((w) => w && w.severity === "low").length;
        const highCount = report.warnings.filter((w) => w && w.severity === "high").length;
        const warningsByCode = buildWarningCodeMap(report.warnings);
        const auditSummary = attachSrvValFromWarningsToAuditSummary(
          summarizeAuditByCategory(body.audit),
          warningsByCode
        );
        const blockedBy = report.warnings
          .filter((w) => w && w.severity === "high")
          .map((w) => w.code || "unknown_high_warning");
        const metricsBase = {
          ts: new Date().toISOString(),
          playerId: body.playerId,
          schemaVersion: body.schemaVersion,
          mode: body.mode,
          auditCount: auditLen,
          auditSummary,
          warningSummary: { low: lowCount, high: highCount },
          warningsByCode,
          rejectHighWarnings,
          rejectAnySrvValAudit,
          srvValCategoryRejectList: getSrvValCategoryRejectList(),
        };
        if (rejectHighWarnings && highCount > 0) {
          appendMetrics({
            ...metricsBase,
            accepted: false,
            error: "high_warning_block",
            blockedBy,
            httpStatus: 400,
            durationMs: msSince(t0),
          });
          console.warn(
            new Date().toISOString(),
            "POST /sync rejected: high_warning_block",
            `high=${highCount}`,
            `codes=${blockedBy.join(",")}`
          );
          return sendJson(
            res,
            400,
            {
              ok: false,
              error: "high_warning_block",
              blockedBy,
              auditSummary,
              validation: {
                ok: false,
                warningSummary: { low: lowCount, high: highCount },
                warningsByCode,
                warnings: report.warnings,
              },
            },
            hdr()
          );
        }
        const srvBlock = evaluateSrvValSyncReject(body, auditSummary);
        if (srvBlock) {
          appendMetrics({
            ...metricsBase,
            accepted: false,
            error: srvBlock.error,
            srvValRejectDetail: srvBlock.detail,
            httpStatus: 400,
            durationMs: msSince(t0),
          });
          console.warn(
            new Date().toISOString(),
            "POST /sync rejected:",
            srvBlock.error,
            redactPiiForLog(JSON.stringify(srvBlock.detail))
          );
          return sendJson(
            res,
            400,
            {
              ok: false,
              error: srvBlock.error,
              detail: srvBlock.detail,
              auditSummary,
              validation: {
                ok: false,
                warningSummary: { low: lowCount, high: highCount },
                warningsByCode,
                warnings: report.warnings,
              },
            },
            hdr()
          );
        }
        fs.writeFileSync(fp, pretty, "utf8");
        logSrvValAuditAlerts(auditSummary);
        console.log(
          new Date().toISOString(),
          "POST /sync ok: saved",
          logPathForSyncSaved(fp, safe),
          "audit",
          auditLen
        );
        if (String(process.env.LOG_AUDIT_SUMMARY || "").trim() === "1") {
          console.log(
            new Date().toISOString(),
            "auditSummary",
            redactPiiForLog(JSON.stringify(auditSummary))
          );
        }
        if (report.warnings.length) {
          console.warn(
            new Date().toISOString(),
            "validation_warnings",
            `low=${lowCount}`,
            `high=${highCount}`,
            redactPiiForLog(JSON.stringify(report.warnings))
          );
        }
        appendMetrics({
          ...metricsBase,
          accepted: true,
          saved: safe + ".json",
          bytes: Buffer.byteLength(pretty, "utf8"),
          httpStatus: 200,
          durationMs: msSince(t0),
        });
        const okPayload = {
          ok: true,
          saved: safe + ".json",
          schemaVersion: body.schemaVersion,
          playerId: body.playerId,
          auditCount: auditLen,
          auditSummary,
          bytes: Buffer.byteLength(pretty, "utf8"),
          replayObservation: buildReplayObservation(body),
          validation: {
            ok: true,
            warningSummary: { low: lowCount, high: highCount },
            warningsByCode,
            warnings: report.warnings,
          },
        };
        idempotencyCacheSet(idemComp, 200, okPayload);
        const writtenEtag = etagHeaderValue(sha256HexBuffer(Buffer.from(pretty, "utf8")));
        return sendJson(res, 200, okPayload, { ...hdr(), ...(writtenEtag ? { ETag: writtenEtag } : {}) });
      });
      return;
    }

    if (req.method === "POST") {
      console.warn(
        new Date().toISOString(),
        "POST not handled (wrong path?)",
        JSON.stringify(u.pathname),
        "use POST http://127.0.0.1:" + port + "/sync"
      );
    }
    sendJson(res, 404, {
      ok: false,
      error: "not_found",
      hint: "POST /sync  |  GET /state?playerId=...  |  GET /players",
    });
  })
  .listen(port, "127.0.0.1", () => {
    ensureDataDir();
    idempotencyLoadFromDisk();
    console.log("Persist sync http://127.0.0.1:" + port);
    console.log("  reject high warnings:", rejectHighWarnings ? "ON" : "OFF");
    console.log(
      "  P3-2 REJECT_SRVVAL_AUDIT (any SrvVal_* in audit[]):",
      rejectAnySrvValAudit ? "ON" : "OFF"
    );
    const srvList = getSrvValCategoryRejectList();
    if (srvList.length) console.log("  P3-2 SRVVAL_REJECT_CATEGORIES:", srvList.join(", "));
    if (parseJsonEnvObject("SRVVAL_REJECT_THRESHOLD_JSON"))
      console.log("  P3-2 SRVVAL_REJECT_THRESHOLD_JSON: set");
    if (parseJsonEnvObject("SRVVAL_ALERT_THRESHOLD_JSON"))
      console.log("  P3-2 SRVVAL_ALERT_THRESHOLD_JSON: set (warn only)");
    console.log("  MAINTENANCE_MODE:", maintenanceMode ? "ON (503)" : "OFF");
    console.log("  SYNC_RATE_LIMIT_PER_MINUTE:", syncRateLimitPerMinute || "OFF");
    console.log("  SYNC_IDEMPOTENCY_TTL_MS:", syncIdempotencyTtlMs || "OFF");
    console.log(
      "  SYNC_IDEMPOTENCY_PERSIST:",
      syncIdempotencyPersist && syncIdempotencyTtlMs > 0 ? "ON -> " + path.basename(idempotencyPersistPath) : "OFF"
    );
    console.log("  SYNC_HMAC_SECRET:", syncHmacSecret ? "SET" : "OFF");
    console.log("  SYNC_REQUIRE_STAGING_HEADER:", syncRequireStagingHeader ? "ON" : "OFF");
    console.log(
      "  D16 SYNC_ETAG_DISABLED (If-Match validation):",
      syncEtagCheckDisabled() ? "ON (validation off)" : "OFF (If-Match enforced)"
    );
    console.log("  POST /sync");
    console.log("  GET  /health");
    console.log("  GET  /state?playerId=...");
    console.log("  GET  /players");
    console.log("  GET  /metrics/report?days=7&top=10");
    console.log("  GET  /metrics/players?days=7&top=10");
    console.log("  GET  /metrics/codes?days=7&top=10");
    console.log("  GET  /metrics/rejections?days=7");
    console.log("  GET  /metrics/anomalies?hours=24&compareHours=24&top=5");
    console.log("  GET  /metrics/alerts?days=7&minAcceptRate=95&maxHighWarnings=0");
    console.log("  GET  /metrics/alerts/players?days=7&top=10&minAcceptRate=95");
    console.log("  GET  /metrics/dashboard?days=7&top=5&hours=24&compareHours=24");
    console.log("  GET  /metrics/dashboard?...&format=csv&csvMode=sectioned");
    console.log("  GET  /metrics/dashboard?...&format=md");
    console.log("  GET  /metrics/recent?limit=20");
    console.log("  GET  /metrics/audit-categories?days=7&playerId=...");
    console.log("  GET  /metrics/prometheus?days=7&playerId=...  (OpenMetrics text)");
    console.log("  GET  /metrics/sync-summary?days=7&playerId=...  (SLO accept/reject)");
    console.log("  GET  /rehearsal/mock-sync-200  (fixed 200 JSON for clients)");
    console.log("  POST /rehearsal/validate-patch  (PATCH ops rehearsal, no write)");
    console.log(
      "  POST /rehearsal/apply-patch  (PATCH write to state, needs REHEARSAL_PATCH_WRITE=1):",
      rehearsalPatchWrite ? "ON" : "OFF"
    );
    console.log("  GET  /rehearsal/apply-patch  (doc)");
    console.log("  GET  /rehearsal/audit-state-strict  (SYNC_AUDIT_STATE_STRICT doc)");
    console.log("  GET  /rehearsal/patch-strategy  (PATCH draft doc)");
    console.log("  GET  /rehearsal/compliance-bundle  (D10 retention / PII / S3 export notes)");
    console.log("  GET  /rehearsal/etag-concurrency  (D16 ETag / If-Match doc)");
    if (syncIssuedAtMaxSkewSec > 0)
      console.log("  SYNC_ISSUED_AT_MAX_SKEW_SEC:", syncIssuedAtMaxSkewSec);
    console.log("  LOG_REDACT_PII:", logRedactPii ? "ON" : "OFF");
    if (complianceMetricsRetentionHintDays != null)
      console.log("  COMPLIANCE_METRICS_RETENTION_HINT_DAYS:", complianceMetricsRetentionHintDays);
    if (compliancePlayerStateRetentionHintDays != null)
      console.log("  COMPLIANCE_PLAYER_STATE_RETENTION_HINT_DAYS:", compliancePlayerStateRetentionHintDays);
    console.log("  AUDIT_EXPORT_BUCKET_PREFIX:", auditExportBucketPrefix);
    console.log(
      "  SYNC_WARNING_SRVVAL_BRIDGE:",
      syncWarningSrvValBridge ? "ON (defaults if no JSON)" : "OFF"
    );
    console.log(
      "  WARNING_CODE_TO_SRVVAL_JSON:",
      warningCodeToSrvValMapResolved ? "configured" : "off"
    );
    console.log("  SYNC_AUDIT_STATE_STRICT:", syncAuditStateStrict ? "ON (tail vs state => 400)" : "OFF");
    console.log("  data ->", dataDir);
  });
