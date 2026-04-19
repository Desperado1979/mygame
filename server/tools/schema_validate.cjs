/**
 * JSON Schema validation for POST /sync (Ajv 2020-12).
 * If `ajv` is not installed under server/node_modules, all checks are skipped (ok: true, skipped: true).
 */
const fs = require("fs");
const path = require("path");

const schemaDir = path.join(__dirname, "..", "schemas");

let disabled = false;
let validateClient;
let validateState;
let validateAuditItem;

function loadJson(name) {
  const fp = path.join(schemaDir, name);
  return JSON.parse(fs.readFileSync(fp, "utf8"));
}

function ensureCompilers() {
  if (disabled) return false;
  if (validateClient) return true;
  try {
    const { Ajv2020 } = require("ajv/dist/2020");
    const addFormats = require("ajv-formats");
    const ajv = new Ajv2020({ allErrors: true, strict: false });
    addFormats(ajv);
    const clientSchema = loadJson("client_sync_request.schema.json");
    const stateSchema = loadJson("player_state.schema.json");
    const auditItemSchema = loadJson("audit_event.schema.json");
    validateClient = ajv.compile(clientSchema);
    validateState = ajv.compile(stateSchema);
    validateAuditItem = ajv.compile(auditItemSchema);
    return true;
  } catch (e) {
    disabled = true;
    return false;
  }
}

function formatAjvErrors(errs) {
  if (!errs || !errs.length) return [];
  return errs.map((e) => `${e.instancePath || "/"} ${e.message}`.trim());
}

/**
 * @param {any} body parsed POST JSON
 * @returns {{ ok: boolean, skipped?: boolean, reason?: string, errors?: string[] }}
 */
function validateSyncPayloadSchemas(body) {
  if (!ensureCompilers())
    return { ok: true, skipped: true, reason: "ajv_unavailable" };

  if (!validateClient(body))
    return { ok: false, errors: formatAjvErrors(validateClient.errors) };

  if (body.state && !validateState(body.state))
    return { ok: false, errors: formatAjvErrors(validateState.errors).map((s) => "state" + s) };

  if (Array.isArray(body.audit)) {
    for (let i = 0; i < body.audit.length; i++) {
      if (!validateAuditItem(body.audit[i])) {
        return {
          ok: false,
          errors: formatAjvErrors(validateAuditItem.errors).map((s) => `audit[${i}] ${s}`),
        };
      }
    }
  }

  return { ok: true, skipped: false };
}

module.exports = { validateSyncPayloadSchemas };
