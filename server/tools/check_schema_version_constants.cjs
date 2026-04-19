/**
 * CI: client_sync_request.schema.json schemaVersion.const vs ClientSyncRequestSchemaVersion in Export.cs
 */
const fs = require("fs");
const path = require("path");

const schemaPath = path.join(__dirname, "..", "schemas", "client_sync_request.schema.json");
const exportPath = path.join(__dirname, "..", "..", "Assets", "PlayerStateExportSimple.Export.cs");

let schema;
try {
  schema = JSON.parse(fs.readFileSync(schemaPath, "utf8"));
} catch (e) {
  console.error("schema_read_failed", e.message);
  process.exit(1);
}
const constVal = schema && schema.properties && schema.properties.schemaVersion && schema.properties.schemaVersion.const;
if (typeof constVal !== "number") {
  console.error("schema_missing_properties.schemaVersion.const");
  process.exit(1);
}

let exportText;
try {
  exportText = fs.readFileSync(exportPath, "utf8");
} catch (e) {
  console.error("export_read_failed", exportPath, e.message);
  process.exit(1);
}
const m = exportText.match(/ClientSyncRequestSchemaVersion\s*=\s*(\d+)/);
if (!m) {
  console.error("export_missing_ClientSyncRequestSchemaVersion");
  process.exit(1);
}
const exportVal = Number(m[1]);
if (exportVal !== constVal) {
  console.error(`schemaVersion mismatch: schema.json const=${constVal}, Export.cs=${exportVal}`);
  process.exit(1);
}
console.log("check_schema_version_constants: OK", constVal);
