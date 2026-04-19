/**
 * D13 — PATCH rehearsal: validate JSON-Patch-style ops against a whitelist (no disk write).
 * Used by POST /rehearsal/validate-patch and npm run validate-patch-rehearsal --smoke
 */
const crypto = require("crypto");

const ALLOWED_TOP = new Set(["gold", "level", "version", "playerId"]);
const INV_KEYS = new Set(["hpPotion", "mpPotion"]);

function splitPath(pathStr) {
  const t = String(pathStr || "").trim();
  if (!t || t[0] !== "/") return null;
  const parts = t.split("/").filter(Boolean);
  return parts.length ? parts : null;
}

function pathAllowed(parts) {
  if (!parts || parts.length === 0) return false;
  if (parts.length === 1 && ALLOWED_TOP.has(parts[0])) return true;
  if (parts.length === 2 && parts[0] === "inventory" && INV_KEYS.has(parts[1])) return true;
  return false;
}

function cloneState(base) {
  return JSON.parse(JSON.stringify(base || {}));
}

function setDeep(root, parts, value) {
  let cur = root;
  for (let i = 0; i < parts.length - 1; i++) {
    const k = parts[i];
    if (cur[k] == null || typeof cur[k] !== "object") cur[k] = {};
    cur = cur[k];
  }
  cur[parts[parts.length - 1]] = value;
}

function removeDeep(root, parts) {
  let cur = root;
  for (let i = 0; i < parts.length - 1; i++) {
    const k = parts[i];
    if (cur[k] == null || typeof cur[k] !== "object") return;
    cur = cur[k];
  }
  delete cur[parts[parts.length - 1]];
}

function defaultBaseState() {
  return {
    playerId: "rehearsal_default",
    gold: 0,
    level: 1,
    version: 1,
    inventory: { hpPotion: 0, mpPotion: 0 },
  };
}

/**
 * @param {any} body
 * @returns {{ ok: boolean, errors: string[], mergedStatePreview?: object, opCount?: number, doc?: string }}
 */
function validatePatchRehearsalRequest(body) {
  const errors = [];
  if (!body || typeof body !== "object") {
    return { ok: false, errors: ["body_not_object"], doc: "patch_rehearsal_v1" };
  }
  const ops = body.ops;
  if (!Array.isArray(ops)) {
    errors.push("missing_or_bad_ops_array");
    return { ok: false, errors, doc: "patch_rehearsal_v1" };
  }
  if (ops.length > 64) {
    errors.push("too_many_ops_max_64");
    return { ok: false, errors, doc: "patch_rehearsal_v1" };
  }

  const base = body.baseState && typeof body.baseState === "object" ? cloneState(body.baseState) : defaultBaseState();
  if (!base.inventory || typeof base.inventory !== "object") base.inventory = { hpPotion: 0, mpPotion: 0 };
  for (const k of ["hpPotion", "mpPotion"]) {
    if (typeof base.inventory[k] !== "number" || !Number.isFinite(base.inventory[k])) base.inventory[k] = 0;
  }

  for (let i = 0; i < ops.length; i++) {
    const op = ops[i];
    if (!op || typeof op !== "object") {
      errors.push(`op_${i}_not_object`);
      continue;
    }
    const opName = String(op.op || "").trim();
    const parts = splitPath(op.path);
    if (!parts) {
      errors.push(`op_${i}_bad_path`);
      continue;
    }
    if (!pathAllowed(parts)) {
      errors.push(`op_${i}_path_not_allowed:${op.path}`);
      continue;
    }
    if (opName === "replace" || opName === "add") {
      if (op.value === undefined) errors.push(`op_${i}_missing_value`);
    } else if (opName === "remove") {
      /* ok */
    } else {
      errors.push(`op_${i}_unsupported_op:${opName}`);
    }
  }

  if (errors.length) return { ok: false, errors, mergedStatePreview: cloneState(base), doc: "patch_rehearsal_v1" };

  for (let i = 0; i < ops.length; i++) {
    const op = ops[i];
    const opName = String(op.op || "").trim();
    const parts = splitPath(op.path);
    if (opName === "replace" || opName === "add") {
      setDeep(base, parts, op.value);
    } else if (opName === "remove") {
      removeDeep(base, parts);
    }
  }

  const w = [];
  if (typeof base.gold === "number" && base.gold < 0) w.push("preview_gold_negative");
  if (typeof base.level === "number" && base.level < 1) w.push("preview_level_below_1");
  for (const k of ["hpPotion", "mpPotion"]) {
    const n = base.inventory && base.inventory[k];
    if (typeof n === "number" && n < 0) w.push(`preview_inventory_${k}_negative`);
  }

  const mergedHash = crypto.createHash("sha256").update(JSON.stringify(base), "utf8").digest("hex").slice(0, 16);

  return {
    ok: true,
    errors: [],
    warnings: w,
    mergedStatePreview: base,
    opCount: ops.length,
    mergedStateHashPreview: mergedHash,
    doc: "patch_rehearsal_v1",
    note: "Rehearsal only; POST /sync remains full-document replace; POST /rehearsal/apply-patch writes state when REHEARSAL_PATCH_WRITE=1.",
  };
}

function runSmoke() {
  const r1 = validatePatchRehearsalRequest({
    ops: [{ op: "replace", path: "/gold", value: 50 }],
  });
  if (!r1.ok) {
    console.error("smoke_fail", r1);
    process.exit(1);
  }
  const r2 = validatePatchRehearsalRequest({
    ops: [{ op: "replace", path: "/forbidden", value: 1 }],
  });
  if (r2.ok) {
    console.error("smoke_fail_expected_reject");
    process.exit(1);
  }
  console.log(JSON.stringify({ ok: true, smoke: "patch_validate_rehearsal" }));
  process.exit(0);
}

if (require.main === module && process.argv.includes("--smoke")) {
  runSmoke();
}

module.exports = { validatePatchRehearsalRequest, splitPath, pathAllowed };
