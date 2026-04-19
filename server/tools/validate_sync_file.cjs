/**
 * CLI: validate a client sync JSON file against schemas + audit_validate.
 * Usage (from repo root):
 *   node EpochOfDawn/server/tools/validate_sync_file.cjs path/to.json
 * Or from server/:
 *   node tools/validate_sync_file.cjs path/to.json
 */
const fs = require("fs");
const path = require("path");
const { validateSyncPayloadSchemas } = require("./schema_validate.cjs");
const { validateClientSyncPayload } = require("./audit_validate.cjs");

const fp = process.argv[2];
const failOnHigh = process.argv.includes("--fail-on-high");
if (!fp || fp.startsWith("--")) {
  console.error("Usage: node validate_sync_file.cjs <file.json> [--fail-on-high]");
  process.exit(2);
}
const text = fs.readFileSync(path.resolve(fp), "utf8");
let body;
try {
  body = JSON.parse(text);
} catch (e) {
  console.error("invalid_json", e.message);
  process.exit(1);
}

const sr = validateSyncPayloadSchemas(body);
console.log("schema:", sr.skipped ? `SKIPPED (${sr.reason})` : sr.ok ? "OK" : "FAIL");
if (sr.errors) console.log(sr.errors.join("\n"));
if (!sr.ok) process.exit(1);

const ar = validateClientSyncPayload(body);
console.log("audit:", ar.ok ? "OK" : "FAIL");
let highCount = 0;
const highCodes = [];
if (ar.warnings && ar.warnings.length) {
  console.log("warnings:");
  for (const w of ar.warnings) {
    if (w && typeof w === "object") {
      console.log(` - [${w.severity}] ${w.code}: ${w.message}`);
      if (w.severity === "high") {
        highCount++;
        highCodes.push(w.code || "unknown_high_warning");
      }
    } else {
      console.log(" -", String(w));
    }
  }
}
if (ar.errors && ar.errors.length) console.log(ar.errors.join("\n"));
if (failOnHigh && highCount > 0) {
  console.log(`blockedBy: ${highCodes.join(",")}`);
  console.log(`audit: FAIL due to --fail-on-high (high=${highCount})`);
  process.exit(1);
}
process.exit(ar.ok ? 0 : 1);
