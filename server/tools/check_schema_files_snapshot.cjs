/**
 * D10: Verify SHA-256 of server/schemas/*.schema.json against a committed snapshot (CI gate).
 * Update snapshot: node tools/check_schema_files_snapshot.cjs --write
 */
const fs = require("fs");
const path = require("path");
const crypto = require("crypto");

const schemasDir = path.join(__dirname, "..", "schemas");
const snapshotPath = path.join(__dirname, "schema_files_snapshot.json");

function sha256File(fp) {
  const buf = fs.readFileSync(fp);
  return crypto.createHash("sha256").update(buf).digest("hex");
}

function listSchemaFiles() {
  if (!fs.existsSync(schemasDir)) {
    console.error("schemas_dir_missing", schemasDir);
    process.exit(2);
  }
  const names = fs.readdirSync(schemasDir).filter((n) => n.endsWith(".schema.json"));
  names.sort();
  const files = {};
  for (const n of names) {
    const rel = `schemas/${n}`;
    files[rel] = sha256File(path.join(schemasDir, n));
  }
  return { v: 1, files };
}

const write = process.argv.includes("--write");

if (write) {
  const snap = listSchemaFiles();
  fs.writeFileSync(snapshotPath, JSON.stringify(snap, null, 2) + "\n", "utf8");
  console.log("check_schema_files_snapshot: wrote", snapshotPath);
  process.exit(0);
}

if (!fs.existsSync(snapshotPath)) {
  console.error("check_schema_files_snapshot: missing snapshot; run with --write");
  process.exit(2);
}

const expected = JSON.parse(fs.readFileSync(snapshotPath, "utf8"));
const actual = listSchemaFiles();
const expFiles = expected.files || {};
const actFiles = actual.files || {};
const keys = new Set([...Object.keys(expFiles), ...Object.keys(actFiles)]);
const mismatches = [];
for (const k of keys) {
  if (expFiles[k] !== actFiles[k]) mismatches.push({ file: k, expected: expFiles[k], actual: actFiles[k] });
}

if (mismatches.length) {
  console.error("check_schema_files_snapshot: FAIL", JSON.stringify({ mismatches }, null, 2));
  console.error("hint: node tools/check_schema_files_snapshot.cjs --write");
  process.exit(1);
}

console.log("check_schema_files_snapshot: OK", Object.keys(actFiles).length);
